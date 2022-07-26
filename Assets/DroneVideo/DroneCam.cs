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

    bool initialized;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void loadDroneFile()
    {
        initialized = true;
        Record.AnimatedProperty bodyProperty = new Record.AnimatedProperty(droneBody.transform, droneBody.gameObject, Record.instance.clip);
        Record.AnimatedProperty gimbalProperty = new Record.AnimatedProperty(cameraGimbal.transform, cameraGimbal.gameObject, Record.instance.clip);


        string droneFilePath = Application.persistentDataPath + "/" + droneFileName + ".csv";
        string[] droneFile = File.ReadAllLines(droneFilePath);

        Record.TransformFrame frame0 = null;

        int i = 0;
        foreach (string line in droneFile)
        {

            string[] entries = line.Split(',');

            if (i == 1)
            {
                int e = 0;
                foreach (string entry in entries)
                {
                    print("entry: " + e + ":" + entry);
                    e++;
                }
            }

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

            bodyFrame.lPos.x = (float)System.Convert.ChangeType(entries[4], typeof(float)) * 111139;    // longitude/latitude are each 111,139 per degree
            bodyFrame.lPos.y = (float)System.Convert.ChangeType(entries[6], typeof(float)) * 0.3048f;   // feet to meters
            bodyFrame.lPos.z = (float)System.Convert.ChangeType(entries[5], typeof(float)) * 111139;

            bodyFrame.lScal = Vector3.one;

            // pitch roll yaw, is original input order
            // roll and pitch are inverted
            Vector3 rot = new Vector3(-(float)System.Convert.ChangeType(entries[18], typeof(float)), (float)System.Convert.ChangeType(entries[21], typeof(float)), -(float)System.Convert.ChangeType(entries[19], typeof(float)));

            //bodyFrame.lRot.eulerAngles = rot;
            bodyFrame.lRot.eulerAngles = Vector3.zero;


            if (frame0 != null)
            {
                // make sure the camera position is actually in my city lol
                bodyFrame.lPos -= frame0.lPos;
            }

            bodyProperty.frames.Add(bodyFrame);

            if (i == 2)
                frame0 = bodyFrame;




            // Gimbal Frame
            Record.TransformFrame gimbalFrame = new Record.TransformFrame(gimbalProperty);

            gimbalFrame.time = bodyFrame.time;

            gimbalFrame.lPos = Vector3.zero;

            gimbalFrame.lScal = Vector3.one;

            rot = new Vector3(-(float)System.Convert.ChangeType(entries[54], typeof(float)), (float)System.Convert.ChangeType(entries[57], typeof(float)), -(float)System.Convert.ChangeType(entries[55], typeof(float)));

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
        }

        if (!recorder.active || !recorder.recordingMode)
        {



        }
    }


    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        droneShaderMat.SetTexture("droneVideo", droneVideo);
            //Graphics.Blit(droneCam, droneVideo, droneShaderMat);
            Graphics.Blit(source, destination, droneShaderMat);

    }
}
