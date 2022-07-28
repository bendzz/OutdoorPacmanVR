using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class DroneCam : MonoBehaviour
{
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

    [Tooltip("Draws a hole in the maze walls so I can see pacman")]
    public float wallHoleRadius = 1.5f;

    bool initialized;

    Record.AnimatedProperty bodyProperty;
    Record.AnimatedProperty gimbalProperty;

    /// <summary>
    /// All setup info for syncing game, drone and video replay data
    /// </summary>
    [System.Serializable]
    public class GameSession
    {
        [Tooltip("WARNING: This doesn't actually set the file number in Singletons->Recording! It's just a reminder. (Too lazy to set up)")]
        public int pacmanFileNumberREMINDER;
        
        public float pacmanStart;
        public float droneStart;
        public float droneVideoStart;
        public float globalStartOffset;

        [Tooltip("Applied to all frames once on startup")]
        public Vector3 doneCamRotationOffset;

        [Tooltip("local position and rotation")]
        public Vector3 droneCamPlatformPos;
        public Vector3 droneCamPlatformRotation;


        public string droneFileName;
        public VideoClip videoClip;

    };
    [Tooltip("LPT: Most game session properties, including which session, can only be changed before startup.")]
    public int activeGameSession;
    public List<GameSession> gameSessions;


    // Start is called before the first frame update
    void Start()
    {
        // sync all the data recordings
        GameSession gs = gameSessions[activeGameSession];

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

            rot += doneCamRotationOffset;

            // gimbal rotation is absolute value, not additive with the drone body
            gimbalFrame.lRot.eulerAngles = rot;
            //gimbalFrame.lRot *= Quaternion.Euler(doneCamRotationOffset);

            gimbalProperty.frames.Add(gimbalFrame);


            i++;
            //if (i > 3) break;
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (!initialized)
        {
            loadDroneFile();    // initialize after Record has loaded its stuff
             
            Record.instance.clip.time = pacmanStart + globalStartOffset;    // note: This offset affects all clip properties, including the drone ones.
            videoPlayer.time = droneVideoStart + globalStartOffset;
        }
         

        if (!recorder.active || !recorder.recordingMode)
        {
            Record.instance.playbackSpeed = playbackSpeed;

            bodyProperty.timeOffset = droneStart;
            gimbalProperty.timeOffset = bodyProperty.timeOffset;

            


            {
                // draw drone path 
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

                // draw headset path 
                i = 0;
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
        GPUInstancing.Bots.botMaterial.SetVector("pacmanPos", Game.instance.cam.transform.position);
        GPUInstancing.Bots.botMaterial.SetVector("camPos", transform.position);
        GPUInstancing.Bots.botMaterial.SetFloat("holeRadius", wallHoleRadius);


    }


    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        droneShaderMat.SetTexture("droneVideo", droneVideo);
            //Graphics.Blit(droneCam, droneVideo, droneShaderMat);
            Graphics.Blit(source, destination, droneShaderMat);

    }
}
