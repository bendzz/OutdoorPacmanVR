using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class DroneCam : MonoBehaviour
{
    public Record recorder;

    [Tooltip("A body for the drone so the camera can rotate freely")]
    public Transform droneBody;
    public Transform cameraGimbal;
    public Camera cam;
    public VideoPlayer videoPlayer;
    public RenderTexture droneCam;
    public RenderTexture droneVideo;
    public Shader droneShader;
    public Material droneShaderMat;
    public string droneFileName;

    public float pacmanStart = 5;
    public float droneStart = 20;
    public float droneVideoStart = 20;  // TODO

    public float playbackSpeed = 1;


    bool initialized;

    Record.AnimatedProperty bodyProperty;
    Record.AnimatedProperty gimbalProperty;

    // Start is called before the first frame update
    void Start()
    {
        
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
            double xPos = (double)System.Convert.ChangeType(entries[4], typeof(double));
            double yPos = (double)System.Convert.ChangeType(entries[6], typeof(double));
            double zPos = (double)System.Convert.ChangeType(entries[5], typeof(double));

            bodyFrame.lPos.x = (float)System.Convert.ChangeType(entries[4], typeof(float));
            bodyFrame.lPos.y = (float)System.Convert.ChangeType(entries[6], typeof(float));
            bodyFrame.lPos.z = (float)System.Convert.ChangeType(entries[5], typeof(float));





            bodyFrame.lScal = Vector3.one;

            // Enable this to bring in the drone body's rotation; note that this will screw up the gimbal angle
            // pitch roll yaw, is original input order
            // roll and pitch are inverted
            //Vector3 rot = new Vector3(-(float)System.Convert.ChangeType(entries[18], typeof(float)), (float)System.Convert.ChangeType(entries[21], typeof(float)), -(float)System.Convert.ChangeType(entries[19], typeof(float)));
            bodyFrame.lRot.eulerAngles = Vector3.zero;


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

                xPos = (xPos - startXpos) * 111139;     // longitude/latitude are each 111,139 per degree
                yPos = (yPos - startYpos);  // * 0.3048f;   // feet to meters
                zPos = (zPos - startZpos) * 111139;

                bodyFrame.lPos = new Vector3((float)xPos, (float)yPos, (float)zPos);
            }

            if (frame0 != bodyFrame) 
                bodyFrame.lPos.x = -bodyFrame.lPos.x;   // longitude is inverted

            bodyProperty.frames.Add(bodyFrame);






            // Gimbal Frame
            Record.TransformFrame gimbalFrame = new Record.TransformFrame(gimbalProperty);

            gimbalFrame.time = bodyFrame.time;

            gimbalFrame.lPos = Vector3.zero;

            gimbalFrame.lScal = Vector3.one;

            //rot = new Vector3(-(float)System.Convert.ChangeType(entries[54], typeof(float)), (float)System.Convert.ChangeType(entries[57], typeof(float)), -(float)System.Convert.ChangeType(entries[55], typeof(float)));
            Vector3 rot = new Vector3((float)System.Convert.ChangeType(entries[54], typeof(float)), (float)System.Convert.ChangeType(entries[57], typeof(float)), -(float)System.Convert.ChangeType(entries[55], typeof(float)));
            //rot = new Vector3(-(float)System.Convert.ChangeType(entries[54], typeof(float)), (float)System.Convert.ChangeType(entries[56], typeof(float)), -(float)System.Convert.ChangeType(entries[55], typeof(float)));

            // gimbal rotation is absolute value, not additive with the drone body
            gimbalFrame.lRot.eulerAngles = rot;

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

            Record.instance.clip.time = pacmanStart;
            videoPlayer.time = droneVideoStart;

            //bodyProperty.timeOffset = droneStart - pacmanStart;
            //gimbalProperty.timeOffset = droneStart - pacmanStart;



        }


        if (!recorder.active || !recorder.recordingMode)
        {
            Record.instance.playbackSpeed = playbackSpeed;

            bodyProperty.timeOffset = droneStart - pacmanStart;
            gimbalProperty.timeOffset = droneStart - pacmanStart;

            //videoPlayer.time = (Record.instance.clip.time - pacmanStart) + droneVideoStart;


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
        }
    }


    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        droneShaderMat.SetTexture("droneVideo", droneVideo);
            //Graphics.Blit(droneCam, droneVideo, droneShaderMat);
            Graphics.Blit(source, destination, droneShaderMat);

    }
}
