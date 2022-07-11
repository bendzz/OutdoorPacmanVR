using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{

    public Texture2D map;
    public Transform mapEmpty;
    public GameObject wallPiece;
    public GameObject dot;
    public GameObject PowerPill;

    public float cellWidth = .1f;

    List<GPUInstancing.Bots.bot> walls;
    List<GPUInstancing.Bots.bot> dots;
    List<GPUInstancing.Bots.bot> pills;

    // Start is called before the first frame update
    void Start()
    {
        Color[] pixels = map.GetPixels();
        Vector2Int dims = new Vector2Int(map.width, map.height);

        walls = new List<GPUInstancing.Bots.bot>();
        dots = new List<GPUInstancing.Bots.bot>();
        pills = new List<GPUInstancing.Bots.bot>();

        for (int x = 0; x < dims.x; x++)
        {
            for (int y = 0; y < dims.y; y++)
            {
                int i = x + y * dims.x;
                //print("x " + x + " y " + y + " = " + pixels[i]);
                Color p = pixels[i];
                if (p.b > .5 && p.r < .5)
                {
                    GPUInstancing.Bots.bot item = new GPUInstancing.Bots.bot();
                    item.pos = new Vector3(x * cellWidth, 1, y * cellWidth);
                    walls.Add(item);


                } else if (p.g > .5 && p.b < .8)
                {
                    int pxCount = clearAllTouchingPixels(x, y, pixels, dims);

                    if (pxCount < 5)
                    {
                        GPUInstancing.Bots.bot item = new GPUInstancing.Bots.bot();
                        item.pos = new Vector3(x * cellWidth, 1, y * cellWidth);
                        item.pos += new Vector3(cellWidth, 0, cellWidth);
                        dots.Add(item);
                    }
                    else
                    {
                        GPUInstancing.Bots.bot item = new GPUInstancing.Bots.bot();
                        item.pos = new Vector3(x * cellWidth, 1, y * cellWidth);
                        item.pos += new Vector3(cellWidth * 4, 0, cellWidth * 2);
                        pills.Add(item);
                    }
                }
            }
        }


        GPUInstancing.Bots.bots = new CSBuffer<GPUInstancing.Bots.bot>("bots");
        GPUInstancing.Bots.bots.list.AddRange(walls);
        GPUInstancing.Bots.bots.list.AddRange(dots);
        GPUInstancing.Bots.bots.list.AddRange(pills);

        GPUInstancing.Bots.submeshInstances = new List<int>();

        GPUInstancing.Bots.submeshInstances.Add(walls.Count * 1);
        GPUInstancing.Bots.submeshInstances.Add(dots.Count * 1);
        GPUInstancing.Bots.submeshInstances.Add(pills.Count * 1);


        //GPUInstancing.Bots.bots = new CSBuffer<GPUInstancing.Bots.bot>("bots");
        //GPUInstancing.Bots.bots.list = walls;
        GPUInstancing.Bots.bots.fillBuffer();
    }

    int clearAllTouchingPixels(int inX, int inY, Color[] pixels, Vector2Int dims)
    {
        int pixelCount = 0;
        Queue<Vector2Int> neighbors = new Queue<Vector2Int>();

        neighbors.Enqueue(new Vector2Int(inX, inY));


        while (neighbors.Count > 0)
        {
            Vector2Int px = neighbors.Dequeue();
            int x = px.x;
            int y = px.y;

            for (int xx = -1; xx <= 1; xx++)
            {
                for (int yy = -1; yy <= 1; yy++)
                {
                    int i = (x + xx) + (y + yy) * dims.x;
                    Color p = pixels[i];
                    if (p.g > .5)
                    {
                        pixels[i] = Color.black;
                        neighbors.Enqueue(new Vector2Int(x + xx, y + yy));
                        pixelCount++;
                    }
                }

            }
        }

        return pixelCount;
    }


    // Update is called once per frame
    void Update()
    {
        List<Vector4> ghostPositions = new List<Vector4>();
        foreach(GPUInstancing.Bots.bot pill in pills)
        {
            ghostPositions.Add(pill.pos);
        }

        GPUInstancing.Bots.botMaterial.SetVectorArray("ghostPositions", ghostPositions);
    }
}
