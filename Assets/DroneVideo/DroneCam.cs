using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class DroneCam : MonoBehaviour
{
    [Tooltip("This is just a mirror of what the Record clip time is; you can't set it. (It's for easier animating)")]
    public float clipTime;

    public Record recorder;
     
    public Transform droneCamPlatform;
    [Tooltip("A body for the drone so the camera can rotate freely")]
    public Transform droneBody;
    public Transform cameraGimbal;
    public Camera cam;
    public VideoPlayer videoPlayer;
    public RenderTexture droneCam;
    public RenderTexture droneVideo;
    public Shader droneShader;
    public Material droneShaderMat;
    private string droneFileName;

    private float pacmanStart = 5;
    private float droneStart = 20;
    private float droneVideoStart = 20;  // TODO
    private float globalStartOffset = 0;
    [Tooltip("Applied to all frames once on startup")]
    private Vector3 doneCamRotationOffset = Vector3.zero;

    public float playbackSpeed = 1;
    [Tooltip("Fast forwards X seconds on click")]
    public bool fastFoward = false;
    public bool rewind = false;
    [Tooltip("Lets you pause to sync up the transform locations a bit better. (Note: video start time is only updated on fastforward/rewind)")]
    public bool pausePlayback = false;
    [Tooltip("And fastForward length")]
    public float rewindLength = 5f;

    [Tooltip("Add a little excitement by making all the eaten dots normal again for re-eating- Or fix it after replaying section")]
    public bool resetAllDots = false;


    [Tooltip("Lowers the walls between the camera and pacman so you can see the player")]
    public bool cutHoleInWalls = true;
    [Tooltip("Draws a hole in the maze walls so I can see pacman")]
    public float wallHoleRadius = 1.5f;

    public bool showDronePath = true;
    public bool showHeadsetPath = true;

    [Tooltip("Of the OVR rig")]
    public Transform CenterEyeAnchor;
    [Tooltip("Enables or disables the in game VR camera")]
    public bool switchToHeadsetView = false;


    bool initialized;

    Record.AnimatedProperty bodyProperty;
    Record.AnimatedProperty gimbalProperty;

    /// <summary>
    /// All setup info for syncing game, drone and video replay data
    /// </summary>
    [System.Serializable]
    public class GameSession
    {
        //[Tooltip("WARNING: This doesn't actually set the file number in Singletons->Recording! It's just a reminder. (Too lazy to set up)")]
        public int pacmanFileNumber;
        
        public float pacmanStart;
        [Tooltip("WARNING: This is *in addition* to pacmanStart; if you add to that, you have to subtract from this one")]
        public float droneStart;
        [Tooltip("This one only syncs when you rewind/fast forward (and it syncs poorly, often randomly off by a bit)")]
        public float droneVideoStart;
        [Tooltip("Starts the whole scene playing later")]
        public float globalStartOffset;

        [Tooltip("Applied to all frames once on startup")]
        public Vector3 doneCamRotationOffset;

        [Tooltip("local position and rotation")]
        public Vector3 droneCamPlatformPos;
        public Vector3 droneCamPlatformRotation;


        public string droneFileName;
        public VideoClip videoClip;

        //[Tooltip("A new, optional unity animation you can slap on a camera or platform to help sync it better. This script will set its 'time' parameter to match the Clips")]
        //public Animator animator;
        [Tooltip("A last minute ad hoc idea to fix the phone cam clip I found")]
        public Animation animation;

    };
    [Tooltip("LPT: Most game session properties, including which session, can only be changed before startup.")]
    public int activeGameSession;
    public List<GameSession> gameSessions;








    public static DroneCam instance;

    // Start is called before the first frame update
    void Start()
    {
        if (instance == null)
            instance = this;
        else
            Debug.LogError("ERROR: Only one instance of DroneCam allowed! This instance is on " + gameObject);
            
        syncGameSessionSettings();

        //Record.instance.fileNumber = gameSessions[activeGameSession].pacmanFileNumber;
    }

    /// <summary>
    /// Sets script settings to the correct gameSessions settings. (yeah it's inefficient to duplicate these variables. Too lazy to fix)
    /// </summary>
    void syncGameSessionSettings()
    {
        // sync all the data recordings
        GameSession gs = gameSessions[activeGameSession];

        Record.instance.fileNumber = gs.pacmanFileNumber;

        pacmanStart = gs.pacmanStart;
        droneStart = gs.droneStart;
        droneVideoStart = gs.droneVideoStart;
        globalStartOffset = gs.globalStartOffset;
        doneCamRotationOffset = gs.doneCamRotationOffset;

        droneCamPlatform.localPosition = gs.droneCamPlatformPos;
        droneCamPlatform.transform.localEulerAngles = gs.droneCamPlatformRotation;

        droneFileName = gs.droneFileName;
        videoPlayer.clip = gs.videoClip;
    }

    public void loadDroneFile()
    {
        initialized = true;
        bodyProperty = new Record.AnimatedProperty(droneBody.transform, droneBody.gameObject, Record.instance.clip);
        gimbalProperty = new Record.AnimatedProperty(cameraGimbal.transform, cameraGimbal.gameObject, Record.instance.clip);


        string droneFilePath = Application.persistentDataPath + "/" + droneFileName + ".csv";
        string[] droneFile = File.ReadAllLines(droneFilePath);

        Record.TransformFrame frame0 = null;

        double startXpos = 0;
        double startYpos = 0;
        double startZpos = 0;


        int i = 0;
        foreach (string line in droneFile)
        {

            string[] entries = line.Split(',');

            //if (i == 1)
            //{
            //    int e = 0;
            //    foreach (string entry in entries)
            //    {
            //        print("entry: " + e + ":" + entry);
            //        e++;
            //    }
            //}

            if (i < 2)
            {
                i++;
                continue;
            }

            // Body Frame
            Record.TransformFrame bodyFrame = new Record.TransformFrame(bodyProperty);
            //print("Line " + i);
            //print("3" + entries[3]);
            //print(entries[4]);
            //print(entries[5]);
            //print(entries[6]);
            //print(entries[18]);
            //print(entries[19]);
            //print(entries[20]);
            //print(entries[21]);

            bodyFrame.time = (float)System.Convert.ChangeType(entries[3], typeof(float));




            //bodyFrame.lPos.x = (float)System.Convert.ChangeType(entries[4], typeof(float)) * 111139;    // longitude/latitude are each 111,139 per degree
            ////bodyFrame.lPos.y = (float)System.Convert.ChangeType(entries[6], typeof(float)) * 0.3048f;   // feet to meters
            //bodyFrame.lPos.y = (float)System.Convert.ChangeType(entries[6], typeof(float)) *1f;   // feet to meters
            //bodyFrame.lPos.z = (float)System.Convert.ChangeType(entries[5], typeof(float)) * 111139;

            // longitude and latitude are too big for floats; you lose precision
            double xPos = (double)System.Convert.ChangeType(entries[5], typeof(double));
            double yPos = (double)System.Convert.ChangeType(entries[6], typeof(double));
            double zPos = (double)System.Convert.ChangeType(entries[4], typeof(double));

            //bodyFrame.lPos.x = (float)System.Convert.ChangeType(entries[4], typeof(float));
            //bodyFrame.lPos.y = (float)System.Convert.ChangeType(entries[6], typeof(float));
            //bodyFrame.lPos.z = (float)System.Convert.ChangeType(entries[5], typeof(float));





            bodyFrame.lScal = Vector3.one;

            // Enable this to bring in the drone body's rotation; note that this will screw up the gimbal angle
            // pitch roll yaw, is original input order
            // roll and pitch are inverted
            //Vector3 rot = new Vector3(-(float)System.Convert.ChangeType(entries[18], typeof(float)), (float)System.Convert.ChangeType(entries[21], typeof(float)), -(float)System.Convert.ChangeType(entries[19], typeof(float)));
            //bodyFrame.lRot.eulerAngles = rot;


            if (i == 2)
            {
                startXpos = xPos;
                startYpos = yPos;
                startZpos = zPos;

                frame0 = bodyFrame; // for rotation (?)
            }

            if (frame0 != null && frame0 != bodyFrame)
            {
                //print("frame0.lPos " + frame0.lPos);
                //bodyFrame.lPos -= frame0.lPos;

                // convert longitude and latitude to game coords

                //xPos = (xPos - startXpos) * 111139;     // longitude/latitude are each 111,139 per degree
                //yPos = (yPos - startYpos) * 0.3048f;   // feet to meters
                //zPos = (zPos - startZpos) * 111139;

                // https://stackoverflow.com/questions/639695/how-to-convert-latitude-or-longitude-to-meters
                // https://en.wikipedia.org/wiki/Geographic_coordinate_system#Length_of_a_degree (unused)
                double longitudeMulti = 40075000 * Mathf.Cos((float)Mathf.Deg2Rad * (float)zPos) / 360;
                
                xPos = (xPos - startXpos) * longitudeMulti;     // longitude/latitude are each 111,139 per degree
                yPos = (yPos - startYpos) * 0.3048f;   // feet to meters
                zPos = (zPos - startZpos) * 111320;
                //zPos = (zPos - startZpos) * 111329.2;

                //bodyFrame.lPos = new Vector3((float)xPos, (float)yPos, (float)zPos);
                bodyFrame.lPos = new Vector3(-(float)xPos, (float)yPos, (float)zPos);
            }

            if (frame0 != bodyFrame) 
                bodyFrame.lPos.x = -bodyFrame.lPos.x;   // longitude is inverted

            bodyProperty.frames.Add(bodyFrame);






            // Gimbal Frame
            Record.TransformFrame gimbalFrame = new Record.TransformFrame(gimbalProperty);

            gimbalFrame.time = bodyFrame.time;

            gimbalFrame.lPos = Vector3.zero;

            gimbalFrame.lScal = Vector3.one;

            Vector3 rot = new Vector3(-(float)System.Convert.ChangeType(entries[54], typeof(float)), (float)System.Convert.ChangeType(entries[57], typeof(float)), -(float)System.Convert.ChangeType(entries[55], typeof(float)));

            //rot += doneCamRotationOffset;

            // gimbal rotation is absolute value, not additive with the drone body
            gimbalFrame.lRot.eulerAngles = rot;
            //gimbalFrame.lRot *= Quaternion.Euler(doneCamRotationOffset);

            gimbalProperty.frames.Add(gimbalFrame);


            i++;
            //if (i > 3) break;
        }
    }






    /// <summary>
    /// Have to run this after the Record Clip is initialized
    /// </summary>
    public static void clipInitialized()
    {
        //print("222 Record and its clip object were initialized! Attached delegates are now called.");

        if (DroneCam.instance.droneFileName != "")
        {
            DroneCam.instance.loadDroneFile();    // initialize after Record has loaded its stuff
        } else
        {
            Debug.LogWarning("WARNING: Drone file name is blank. Not loading file");
        }

        //DroneCam.instance.syncGameSessionSettings();    // 
    }

    


    // Update is called once per frame
    void Update()
    {
        if (!initialized)
        {
            initialized = true;

            //Record.instance.clip.time = pacmanStart + globalStartOffset;    // note: This offset affects all clip properties, including the drone ones.
            videoPlayer.time = droneVideoStart + globalStartOffset;

            Record.instance.clipInitializedDelegate += clipInitialized;

            DroneCam.instance.syncGameSessionSettings();

            return;
        }
         

        if (!recorder.active || !recorder.recordingMode)
        {
            Record.instance.playbackSpeed = playbackSpeed;

            CenterEyeAnchor.gameObject.SetActive(switchToHeadsetView);

            // adjust times
            syncGameSessionSettings();
            Record.instance.clip.timeOffset = pacmanStart + globalStartOffset;
            bodyProperty.timeOffset = droneStart;
            gimbalProperty.timeOffset = bodyProperty.timeOffset;

            clipTime = Record.instance.clip.time;

            //Animator anim = gameSessions[activeGameSession].animator;
            ////Animation ani = gameSessions[activeGameSession].animation;
            ////if (ani != null && anim != null)
            //if (anim != null)
            //{
            //    //anim.SetFloat("time", Record.instance.clip.time);
            //    anim.playbackTime = Record.instance.clip.time;
            //}

            Animation ani = gameSessions[activeGameSession].animation;
            if (ani != null)
            {
                foreach (AnimationState state in ani)
                {
                    //state.speed = 0.5F;
                    state.time = Record.instance.clip.time;
                }
            }

            // playback controls
            if (rewind)
            {
                Record.instance.clip.time -= rewindLength;
                //videoPlayer.time -= rewindLength;
                //videoPlayer.frame = (long)(videoPlayer.frame - videoPlayer.frameRate * rewindLength);   // MAYBE more precise..?
                videoPlayer.time = Record.instance.clip.time + droneVideoStart + globalStartOffset; // to try and resync the video

                rewind = false;
            } 
            if (fastFoward)
            {
                Record.instance.clip.time += rewindLength;
                //videoPlayer.frame = (long)(videoPlayer.frame + videoPlayer.frameRate * rewindLength);   // MAYBE more precise..?
                videoPlayer.time = Record.instance.clip.time + droneVideoStart + globalStartOffset; // to try and resync the video

                fastFoward = false;
            }
            if (pausePlayback)
            {
                //Record.instance.active = false;
                Record.instance.playbackSpeed = 0;
                Game.instance.paused = true;
                videoPlayer.Pause();
            } else
            {
                //Record.instance.active = true;
                //Record.instance.playbackSpeed = 0;
                Game.instance.paused = false;
                videoPlayer.Play();
            }
            if (resetAllDots)
            {
                for (int x = 0; x < Game.instance.navs.GetLength(0); x++)
                {
                    for (int y = 0; y < Game.instance.navs.GetLength(1); y++)
                    {
                        Game.navTile tile = Game.instance.navs[x, y];
                        if (tile.dotRef != 0)
                        {
                            GPUInstancing.Bots.bot bot = GPUInstancing.Bots.bots.list[tile.dotRef];
                            bot.alive = 1;
                            if (bot.pos.y < 1)
                                bot.pos.y = 1;
                            GPUInstancing.Bots.bots.list[tile.dotRef] = bot;

                            tile.hasItem = true;
                            Game.instance.navs[x, y] = tile;
                        }
                    }
                }
                GPUInstancing.Bots.bots.fillBuffer();


                print("Reset all pacman dots");
                resetAllDots = false;
            }


            // paths to help syncing
            if (showDronePath)
            {
                int i = 0;
                foreach (Record.TransformFrame frame in bodyProperty.frames)
                {
                    if (i < bodyProperty.frames.Count - 1)
                    {
                        //Debug.DrawLine(frame.lPos, ((Record.TransformFrame)bodyProperty.frames[i + 1]).lPos, Color.red, 200);
                        Vector3 pos1 = droneBody.parent.TransformPoint(frame.lPos);
                        Vector3 pos2 = droneBody.parent.TransformPoint(((Record.TransformFrame)bodyProperty.frames[i + 1]).lPos);

                        Debug.DrawLine(pos1, pos2, Color.red);
                    }
                    i++;
                }
            }
            if (showHeadsetPath)
            { 
                int i = 0;
                Record.AnimatedProperty headProp = Record.instance.clip.animatedProperties[1];
                Transform headset = (Transform)headProp.obj;
                foreach (Record.TransformFrame frame in headProp.frames)
                {
                    if (i < headProp.frames.Count - 1)
                    {
                        Vector3 pos1 = headset.parent.TransformPoint(frame.lPos);
                        Vector3 pos2 = headset.parent.TransformPoint(((Record.TransformFrame)headProp.frames[i + 1]).lPos);

                        Debug.DrawLine(pos1, pos2, Color.green);
                    }
                    i++;
                }
            }
        }

        //cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(82.1f, cam.aspect);    // set drone FOV https://www.dji.com/ca/mini-3-pro/specs

        // for drawing a hole through the walls
        if (cutHoleInWalls)
        {
            GPUInstancing.Bots.botMaterial.SetVector("pacmanPos", Game.instance.cam.transform.position);
            GPUInstancing.Bots.botMaterial.SetVector("camPos", transform.position);
            GPUInstancing.Bots.botMaterial.SetFloat("holeRadius", wallHoleRadius);
        } else
        {
            GPUInstancing.Bots.botMaterial.SetVector("camPos", Vector3.zero);
        }

    }

    private void LateUpdate()
    {
        //if (!pausePlayback)
            ((Transform)gimbalProperty.obj).localEulerAngles += doneCamRotationOffset;

        if (pausePlayback)
        {
            // pause game after recording unpaused it
            Game.instance.paused = true;
        }
    }


    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        droneShaderMat.SetTexture("droneVideo", droneVideo);
            //Graphics.Blit(droneCam, droneVideo, droneShaderMat);
            Graphics.Blit(source, destination, droneShaderMat);

    }
}
