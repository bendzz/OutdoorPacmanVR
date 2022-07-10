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

    List<GameObject> walls;
    List<GameObject> dots;
    List<GameObject> pills;

    // Start is called before the first frame update
    void Start()
    {
        Color[] pixels = map.GetPixels();
        Vector2Int dims = new Vector2Int(map.width, map.height);

        walls = new List<GameObject>();
        dots = new List<GameObject>();
        pills = new List<GameObject>();

        for (int x = 0; x < dims.x; x++)
        {
            for (int y = 0; y < dims.y; y++)
            {
                int i = x + y * dims.x;
                //print("x " + x + " y " + y + " = " + pixels[i]);
                Color p = pixels[i];
                if (p.b > .5 && p.r < .5)
                {
                    //print("xy " + x + " " + y);

                     GameObject wall = Instantiate(wallPiece);
                    wall.transform.position = new Vector3(x * cellWidth, 1, y * cellWidth);
                    wall.transform.parent = mapEmpty;
                    walls.Add(wall);
                     
                } else if (p.g > .5 && p.b < .8)
                {
                    int pxCount = clearAllTouchingPixels(x, y, pixels, dims);

                    if (pxCount < 5)
                    {
                        GameObject d = Instantiate(dot);
                        d.transform.position = new Vector3(x * cellWidth, 1, y * cellWidth);
                        d.transform.position += new Vector3(cellWidth, 0, cellWidth);
                        d.transform.parent = mapEmpty;
                        dots.Add(d);
                    } else
                    {
                        GameObject d = Instantiate(PowerPill);
                        d.transform.position = new Vector3(x * cellWidth, 1, y * cellWidth);
                        d.transform.position += new Vector3(cellWidth * 4, 0, cellWidth * 2);
                        d.transform.parent = mapEmpty;
                        pills.Add(d);
                    }
                }
            }
        }
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
        
    }
}
