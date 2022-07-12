using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    /// <summary>
    /// singleton; only one should exist
    /// </summary>
    public static Game instance;

    public Texture2D map;
    public Texture2D mapNav;
    public Transform mapEmpty;
    public GameObject wallPiece;
    public GameObject dot;
    public GameObject PowerPill;
    //public GameObject ghostRef;
    public float cellWidth = .1f;

    // Ghosts
    public List<Ghost> ghosts;
    public float ghostSpeedDefault = 5;


    List<GPUInstancing.Bots.bot> walls;
    List<GPUInstancing.Bots.bot> dots;
    List<GPUInstancing.Bots.bot> pills;

    public struct navTile
    {
        public Color color;
        public bool traversible;
        public bool teleporter;
        // ghosts only
        public bool ghostStart;
        public bool noGhostUp;
        public bool turn;
    }

    /// <summary>
    /// 28x36 navigation grid
    /// </summary>
    public navTile[,] navs;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        Color[] pixels = map.GetPixels();
        Color[] pixelsNav = mapNav.GetPixels();
        Vector2Int dims = new Vector2Int(map.width, map.height);

        walls = new List<GPUInstancing.Bots.bot>();
        dots = new List<GPUInstancing.Bots.bot>();
        pills = new List<GPUInstancing.Bots.bot>();

        navs = new navTile[28, 36];
        for (int x = 0; x < 28; x++)
        {
            for (int y = 0; y < 36; y++)
            {
                navs[x,y] = new navTile();
            }
        }

        for (int x = 0; x < dims.x; x++)
        {
            for (int y = 0; y < dims.y; y++)
            {
                int i = x + y * dims.x;
                //print("x " + x + " y " + y + " = " + pixels[i]);
                // Map
                {
                    Color p = pixels[i];
                    if (p.b > .5 && p.r < .5)
                    {
                        GPUInstancing.Bots.bot item = new GPUInstancing.Bots.bot();
                        item.pos = new Vector3(x * cellWidth, 1, y * cellWidth);
                        walls.Add(item);


                    }
                    else if (p.g > .5 && p.b < .8 && p.g < 1)
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
                // Nav
                {
                    Color p = pixelsNav[i];
                    if (p != Color.black)
                    {
                        int pxCount = clearAllTouchingPixels(x, y, pixelsNav, dims);
                        navTile tile = new navTile();
                        tile.color = p;
                        if (p == Color.blue)
                        {
                            tile.traversible = true;
                            tile.ghostStart = true;
                        }
                        else
                        {
                            tile.traversible = true;
                            if (p == Color.white)
                                tile.teleporter = true;
                            if (p == Color.green)
                            {
                                tile.noGhostUp = true;
                                tile.turn = true;
                            }
                            if (p == Color.yellow)
                                tile.turn = true;
                        }
                        //print("x " + x + " y " + y + " color " + p + " nav " + tile.traversible);
                        //navs[(x + 4) / 8, (y+4)/8] = tile;
                        navs[(x + 0) / 8, (y+0)/8] = tile;
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

    public static Vector3 navToWorld(int x, int y)
    {
        //return new Vector3(x * Game.instance.cellWidth * 8, 1, y * Game.instance.cellWidth * 8);
        return new Vector3((x + .5f) * Game.instance.cellWidth * 8, 1, (y + .5f) * Game.instance.cellWidth * 8);
    }
    /// <summary>
    /// The number will change as the position crossed over the edges between tiles
    /// </summary>
    public static Vector2Int worldToNav(Vector3 pos)
    {
        return new Vector2Int((int)(pos.x / (Game.instance.cellWidth * 8)), (int)(pos.z / (Game.instance.cellWidth * 8)));
    }
    /// <summary>
    /// The number will change as it crosses over the CENTER of tiles; grid is offset half a tile down/left
    /// </summary>
    public static Vector2Int worldToNavCenters(Vector3 pos)
    {
        pos -= new Vector3(Game.instance.cellWidth * 4, 0, Game.instance.cellWidth * 4);
        return new Vector2Int((int)((pos.x) / (Game.instance.cellWidth * 8)), (int)(pos.z / (Game.instance.cellWidth * 8)));
    }

    public static navTile nav(Vector2Int pos)
    {
        return Game.instance.navs[pos.x, pos.y];
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
                    if (x + xx >= dims.x || y + yy >= dims.y || x+xx < 0 || y+yy < 0)
                        continue;
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
        //foreach(GPUInstancing.Bots.bot pill in pills)
        //{
        //    ghostPositions.Add(pill.pos);
        //}
        //ghostPositions.Add(ghostRef.transform.position);
        foreach(Ghost ghost in ghosts)
        {
            ghostPositions.Add(ghost.transform.position);
        }


        GPUInstancing.Bots.botMaterial.SetVectorArray("ghostPositions", ghostPositions);
    }
}
