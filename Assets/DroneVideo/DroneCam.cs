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
        Record.AnimatedProperty property = new Record.AnimatedProperty(droneBody.transform, droneBody.gameObject, Record.instance.clip);


        string droneFilePath = Application.persistentDataPath + "/" + droneFileName + ".csv";
        string[] droneFile = File.ReadAllLines(droneFilePath);

        Record.TransformFrame frame0 = null;

        int i = 0;
        foreach (string line in droneFile)
        {
            if (i < 2)
            {
                i++;
                continue;
            }
            string[] entries = line.Split(',');

            //int e = 0;
            //foreach(string entry in entries)
            //{
            //    //print("entry: " + e + ":" + entry);
            //    e++;
            //} 

            Record.TransformFrame frame = new Record.TransformFrame(property);
            //print("Line " + i);
            //print("3" + entries[3]);
            //print(entries[4]);
            //print(entries[5]);
            //print(entries[6]);
            //print(entries[18]);
            //print(entries[19]);
            //print(entries[20]);
            //print(entries[21]);

            frame.time = (float)System.Convert.ChangeType(entries[3], typeof(float));

            frame.lPos.x = (float)System.Convert.ChangeType(entries[4], typeof(float)) * 111139;    // longitude/latitude are each 111,139 per degree
            frame.lPos.y = (float)System.Convert.ChangeType(entries[6], typeof(float)) * 0.3048f;   // feet to meters
            frame.lPos.z = (float)System.Convert.ChangeType(entries[5], typeof(float)) * 111139;

            frame.lPos *= .1f;

            frame.lScal = Vector3.one;

            // pitch roll yaw, is original input
            Vector3 rot = new Vector3((float)System.Convert.ChangeType(entries[18], typeof(float)), (float)System.Convert.ChangeType(entries[21], typeof(float)), (float)System.Convert.ChangeType(entries[19], typeof(float)));

            frame.lRot.eulerAngles = rot;


            if (frame0 != null)
            {
                // make sure the camera position is actually in my city lol
                frame.lPos -= frame0.lPos;
            }

            property.frames.Add(frame);

            if (i == 2)
                frame0 = frame;

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
