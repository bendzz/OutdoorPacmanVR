using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class DroneCam : MonoBehaviour
{
    public Record recorder;

    public Camera cam;
    public VideoPlayer videoPlayer;
    public RenderTexture droneCam;
    public RenderTexture droneVideo;
    public Shader droneShader;
    public Material droneShaderMat;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
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
