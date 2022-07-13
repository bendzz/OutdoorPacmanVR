using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using static OVRManager;
//using static OVRPlugin;

public class Game : MonoBehaviour
{
    /// <summary>
    /// singleton; only one should exist
    /// </summary>
    public static Game instance;

    [Tooltip("Player can pause the game")]
    public bool paused;

    public Texture2D map;
    public Texture2D mapNav;
    [Tooltip("An empty to parent any dynamically spawned entities to, to keep the hierarchy clean")]
    public Transform mapEmpty;
    [Tooltip("Where the ghosts head while in spawning mode, to exit the ghost house")]
    public Transform spawnTarget;
    public GameObject wallPiece;
    public GameObject dot;
    public GameObject PowerPill;
    //public GameObject ghostRef;
    public float pixelSize = .1f;

    public GameObject packman;
    //[Tooltip("an empty 1 meter in front of packman so ghosts can run in front of where he's facing.")]
    //public Transform packmanFacingTarget;
    public Vector2Int packmanPos;
    [Tooltip("Will be spawned and play a short segment of its audiosource every time a dot is eaten")]
    public AudioSource waka1Player;
    public AudioSource waka2Player;
    bool waka2;

    [Tooltip("The VR player's head. This movement controls pacman etc")]
    public Camera cam;
    public Transform OVRCameraRig;

    [Tooltip("Resizing the game board for better play")]
    public Vector3 mapScale = Vector3.one;
    [Tooltip("Offsetting the map to enhance IRL player movement")]
    public Vector3 mapOffset = Vector3.zero;
    [Tooltip("The rotation and scaling axis/centerpoint of the map; set it to the player camera position so the map doesn't shift around")]
    public Vector3 mapCenter = Vector3.zero;

    // Ghosts
    public List<Ghost> ghosts;
    public float ghostSpeedDefault = 5;

    [Tooltip("Current level; when this script starts it resets to 1")]
    public int level;

    List<GPUInstancing.Bots.bot> walls;
    List<GPUInstancing.Bots.bot> dots;
    List<GPUInstancing.Bots.bot> pills;

    /// <summary>
    /// Counter in seconds for how long the ghosts can remain frightened
    /// </summary>
    public float frightened;

    public float timer;
    public int timerStep;
    public List<float> timerSequence;

    [Tooltip("if true, the OVR playspace is no longer shifted to make the player move twice as fast.")]
    public bool disableEnhancedPlayerMovement;


    public struct navTile
    {
        public Color color;
        public bool traversible;
        public bool teleporter;
        // ghosts only
        public bool ghostStart;
        public bool noGhostUp;
        public bool turn;
        /// <summary>
        /// Reference to the dot or pill on this tile (points to the "bots" GPU instancing array/buffer)
        /// </summary>
        public int dotRef;
        /// <summary>
        /// If there's a dot or pill here, this tells what type
        /// </summary>
        public bool isDot;
        /// <summary>
        /// If the dot or pill in "dotRef" is still "alive" or eaten
        /// </summary>
        public bool hasItem;
    }

    /// <summary>
    /// 28x36 navigation grid
    /// </summary>
    public navTile[,] navs;

    Vector3 rigStartPos;


    /// <summary>
    /// For adjusting settings like ghost speed while paused (while blinky whips around demoing the speed)
    /// </summary>
    public struct PausedBlinkyDemo
    {
        public bool paused;
        public Vector3 oOVRScale;
        public Vector3 oBLinkyPos;
        public Ghost.State oState;
        public Ghost.Direction oDirection;
    }
    public PausedBlinkyDemo pausedBlinkyDemo;
    //Vector3 pausedOriginalOVRSettings;
    //Vector3 pausedBlinkyOriginalPosition;

    // Start is called before the first frame update
    void Start()
    {
        level = 1;

        instance = this;

        // Wanted FFR but it's just not gonna turn on -__- glitchy POS https://forum.unity.com/threads/fixed-foveated-rendering-on-oculus-quest-not-working.686662/page-4
        //fixedFoveatedRenderingLevel = FixedFoveatedRenderingLevel.HighTop; // it's the maximum foveation level

        rigStartPos = OVRCameraRig.position;

        // https://www.gamedeveloper.com/design/the-pac-man-dossier
        // level 1 sequence
        // scatter, chase, scatter, chase, scatter, chase, scatter, chase forever
        timerSequence = new List<float> { 7, 20, 7, 20, 5, 20 };


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
                        item.pos = new Vector3(x * pixelSize, 1, y * pixelSize);
                        walls.Add(item);


                    }
                    else if (p.g > .5 && p.b < .8 && p.g < 1)
                    {
                        int pxCount = clearAllTouchingPixels(x, y, pixels, dims);

                        if (pxCount < 5)
                        {
                            GPUInstancing.Bots.bot item = new GPUInstancing.Bots.bot();
                            item.pos = new Vector3(x * pixelSize, 1, y * pixelSize);
                            item.pos += new Vector3(pixelSize, 0, pixelSize);
                            dots.Add(item);
                            //navs[x / 8, y / 8].dot = item; 
                        }
                        else
                        {
                            GPUInstancing.Bots.bot item = new GPUInstancing.Bots.bot();
                            item.pos = new Vector3(x * pixelSize, 1, y * pixelSize);
                            item.pos += new Vector3(pixelSize * 4, 0, pixelSize * 2);
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
                        //navTile tile = new navTile();
                        navTile tile = navs[(x + 0) / 8, (y + 0) / 8];
                        tile.color = p;
                        if (p == Color.blue)
                        {
                            tile.traversible = false;
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


        // Add the dots and pills to their tiles
        for (int i = 0; i < GPUInstancing.Bots.bots.list.Count; i++)
        {
            GPUInstancing.Bots.bot bot = GPUInstancing.Bots.bots.list[i];
            if (i >= walls.Count)
            {
                Vector2Int pos = Game.worldToNav(bot.pos);
                navTile tile = Game.nav(pos);
                tile.hasItem = true;
                tile.isDot = true;
                if (i >= walls.Count + dots.Count)
                    tile.isDot = false;
                tile.dotRef = i;
                //print("item found at " + Game.worldToNav(bot.pos) + ", isDot " + tile.isDot + " ref " + i);
                navs[pos.x, pos.y] = tile;
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


    public Vector2Int getNearestTraversibleTile(Vector3 targetPos)
    {
        Vector2Int result = new Vector2Int(-1, -1);
        float minDistance = 1000000000;
        for (int x = 0; x < navs.GetLength(0); x++)
        {
            for (int y = 0; y < navs.GetLength(1); y++)
            {
                navTile tile = navs[x, y];
                if (tile.traversible)
                {
                    Vector3 pos = navToWorld(x, y);
                    float distance = Vector3.Distance(targetPos, pos);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        result = new Vector2Int(x, y);
                    }
                }
            }
        }
        return result;
    }

    // Doesn't work. TODO
    //public Vector3 getNearestTraversiblePosition(Vector3 targetPos)
    //{
    //    Vector2Int result = new Vector2Int(-1, -1);
    //    float minDistance = 1000000000;

    //    Vector2Int previousResult = result;
    //    float previousDistance = minDistance;
    //    for (int x = 0; x < navs.GetLength(0); x++)
    //    {
    //        for (int y = 0; y < navs.GetLength(1); y++)
    //        {
    //            navTile tile = navs[x, y];
    //            if (tile.traversible)
    //            {
    //                Vector3 pos = navToWorld(x, y);
    //                float distance = Vector3.Distance(targetPos, pos);
    //                if (distance < minDistance)
    //                {
    //                    previousResult = result;
    //                    previousDistance = minDistance;

    //                    minDistance = distance;
    //                    result = new Vector2Int(x, y);
    //                }
    //            }
    //        }
    //    }
    //    return Vector3.Lerp(navToWorld(previousResult), navToWorld(result), previousDistance / (previousDistance + minDistance));
    //}

    /// <summary>
    /// set oppositeDirection to false if this is just a state reaffirming thing
    /// </summary>
    /// <param name="state"></param>
    /// <param name="oppositDirection"></param>
    public void ghostsModeSwitch(Ghost.State state, bool oppositDirection)
    {
        foreach(Ghost ghost in ghosts)
        {
            if (ghost.state == Ghost.State.dead || ghost.state == Ghost.State.unspawned || ghost.state == Ghost.State.spawning)
                continue;
            ghost.state = state;

            if (oppositDirection)
            {
                int oppositeDirection = (int)ghost.direction - 2;
                if (oppositeDirection < 0) oppositeDirection = 4 + oppositeDirection;
                ghost.direction = (Ghost.Direction)oppositeDirection;
            }
        }
        if (oppositDirection)
            print("Switched ghost modes to " + state);
    }

    /// <summary>
    /// Returns if the ghosts should be chasing or scattering, based on the game timer
    /// </summary>
    /// <returns></returns>
    public Ghost.State getDefaultGhostState()
    {
        if (timerStep % 2 == 1)
            return Ghost.State.scatter;
        else
            return Ghost.State.chase;
    }

    // Update is called once per frame
    void Update()
    {
        if (cam.transform.localPosition != Vector3.zero && !disableEnhancedPlayerMovement)    // to allow moving the character to test
            OVRCameraRig.position = rigStartPos + Vector3.Scale(cam.transform.localPosition, new Vector3(1, 0, 1));

        //packman.transform.position = new Vector3(cam.transform.position.x, 0, cam.transform.position.z);
        //packman.transform.position = navToWorld(worldToNav(cam.transform.position));
        packman.transform.position = navToWorld(getNearestTraversibleTile(cam.transform.position));
        //packman.transform.position = getNearestTraversiblePosition(cam.transform.position);
        packman.transform.rotation = Quaternion.Euler(new Vector3(0, cam.transform.rotation.eulerAngles.y, 0));
        packmanPos = Game.worldToNav(packman.transform.position);

        if (!paused)
        {
            // scatter chase scatter chase...
            if (frightened <= 0)
            {
                if (timerStep < timerSequence.Count)
                {
                    timer += Time.deltaTime;
                    if (timer >= timerSequence[timerStep])
                    {
                        ghostsModeSwitch(getDefaultGhostState(), true);
                        //if (timerStep % 2 == 1)
                        //    ghostsModeSwitch(Ghost.State.scatter);
                        //else
                        //    ghostsModeSwitch(Ghost.State.chase);
                        timerStep++;
                        timer = 0;
                    }
                }
                else
                    ghostsModeSwitch(Ghost.State.chase, false); // TODO This should flip them opposite the first time it changes, then never again
            }

            if (frightened > 0)
            {
                frightened -= Time.deltaTime;
                if (frightened <= 0)
                {
                    //foreach(Ghost ghost in ghosts)
                    //{
                    //    if (ghost.state == Ghost.State.frightened)
                    //    {
                    //        //ghost.state = Ghost.State.chase;    // TODO not accurate
                    //        if (timerStep % 2 == 0)
                    //            ghostsModeSwitch(Ghost.State.scatter);
                    //        else
                    //            ghostsModeSwitch(Ghost.State.chase);
                    //    }
                    //}
                    //if (timerStep % 2 == 1)
                    //    ghostsModeSwitch(Ghost.State.scatter);
                    //else
                    //    ghostsModeSwitch(Ghost.State.chase);
                    ghostsModeSwitch(getDefaultGhostState(), true);
                }

                foreach (Ghost ghost in ghosts)
                {
                    if (ghost.state == Ghost.State.frightened)
                    {
                        if (worldToNav(ghost.transform.position) == worldToNav(packman.transform.position))
                        {
                            ghost.state = Ghost.State.dead;
                        }
                    }
                }
            }


            // eat stuff
            navTile pacTile = Game.nav(packmanPos);
            if (pacTile.hasItem)
            {
                pacTile.hasItem = false;
                print("nommed " + packmanPos + pacTile.isDot);
                GPUInstancing.Bots.bot bot = GPUInstancing.Bots.bots.list[pacTile.dotRef];

                AudioSource player = waka1Player;
                if (waka2)
                {
                    player = waka2Player;
                    waka2 = false;
                }
                else
                {
                    waka2 = true;
                }

                player.transform.position = bot.pos;
                IEnumerator coroutine = PlayPauseCoroutine(player, 0, 3);
                StartCoroutine(coroutine);

                //bot.pos += new Vector3(0, 3, 0);
                bot.pos += new Vector3(0, -1, 0);
                bot.alive = -1;

                if (!pacTile.isDot)
                {
                    //FrightenGhosts(5);
                    //ghostsModeSwitch(Ghost.State.frightened);
                    ghostsModeSwitch(Ghost.State.frightened, true);
                    frightened = 10;
                }

                GPUInstancing.Bots.bots.list[pacTile.dotRef] = bot;
                GPUInstancing.Bots.bots.fillBuffer();
                navs[packmanPos.x, packmanPos.y] = pacTile;
            }
        }

        // Settings
        paused = false;
        if (OVRInput.Get(OVRInput.Button.One))
            disableEnhancedPlayerMovement = true;
        else
            disableEnhancedPlayerMovement = false;
        //if (OVRInput.Get(OVRInput.Button.One))
        //    paused = true;
        //else
        //    paused = false;
        
        // Adjust ghost speed
        if (OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > .5f)
        {
            if (!pausedBlinkyDemo.paused)
            {
                // record unpaused state for restoration
                pausedBlinkyDemo.paused = true;
                pausedBlinkyDemo.oBLinkyPos = Ghost.blinkyRef.transform.position;
                pausedBlinkyDemo.oDirection = Ghost.blinkyRef.direction;
                pausedBlinkyDemo.oState = Ghost.blinkyRef.state;
                pausedBlinkyDemo.oOVRScale = OVRCameraRig.localScale;
                Ghost.blinkyRef.state = Ghost.State.chase;  // only to make sure it's not dead
            }
            paused = true;
            OVRCameraRig.localScale = Vector3.one * 10;

            //controls
            float stick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;
            ghostSpeedDefault += stick * 2 * Time.deltaTime;
            if (ghostSpeedDefault > 11)
                ghostSpeedDefault = 11;
            if (ghostSpeedDefault < 1)
                ghostSpeedDefault = 1;
        } else
        {
            if (pausedBlinkyDemo.paused)
            {
                pausedBlinkyDemo.paused = false;
                Ghost.blinkyRef.transform.position = pausedBlinkyDemo.oBLinkyPos;
                Ghost.blinkyRef.direction = pausedBlinkyDemo.oDirection;
                Ghost.blinkyRef.state = pausedBlinkyDemo.oState;
                OVRCameraRig.localScale = pausedBlinkyDemo.oOVRScale;
            }

            // scale world
            if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > .5f)
            {
                // main hand trigger + joystick up/down scales the world (ie scales your playspace object)
                float stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
                if (stick > 0)
                    OVRCameraRig.localScale = OVRCameraRig.localScale * (1 + stick * Time.deltaTime);
                else if (stick < 0)
                    OVRCameraRig.localScale = OVRCameraRig.localScale * (1 + (stick * Time.deltaTime * .5f));

                paused = true;
            }
        }




        List<Vector4> ghostPositions = new List<Vector4>();
        foreach(Ghost ghost in ghosts)
        {
            if (ghost.state != Ghost.State.frightened && ghost.state != Ghost.State.dead)
            {
                ghostPositions.Add(ghost.transform.position);
            }
            else
            {
                ghostPositions.Add(Vector3.one * -10000);
            }
        }
        GPUInstancing.Bots.botMaterial.SetVectorArray("ghostPositions", ghostPositions);

        //mapCenter = cam.transform.position;
        GPUInstancing.Bots.botMaterial.SetVector("mapScale", mapScale);
        GPUInstancing.Bots.botMaterial.SetVector("mapOffset", mapOffset);
        GPUInstancing.Bots.botMaterial.SetVector("mapCenter", mapCenter);

    }


    /// <summary>
    /// plays a sound for the set time without holding up the main game thread
    /// </summary>
    IEnumerator PlayPauseCoroutine(AudioSource source, float startTime, float playTime)
    {
        // https://forum.unity.com/threads/play-audio-for-x-seconds-and-pause-for-x-seconds.456176/
        source.time = startTime;
        source.Play();
        yield return new WaitForSeconds(playTime);
        //source.Stop(); // or source.Stop()
    }


    // Nav helper functions
    public static Vector3 navToWorld(Vector2Int navPos)
    {
        return navToWorld(navPos.x, navPos.y);
    }
    public static Vector3 navToWorld(int x, int y)
    {
        //return new Vector3(x * Game.instance.cellWidth * 8, 1, y * Game.instance.cellWidth * 8);
        return new Vector3((x + .5f) * Game.instance.pixelSize * 8, 1, (y + .5f) * Game.instance.pixelSize * 8);
    }
    /// <summary>
    /// The number will change as the position crossed over the edges between tiles
    /// </summary>
    public static Vector2Int worldToNav(Vector3 pos)
    {
        return new Vector2Int((int)(pos.x / (Game.instance.pixelSize * 8)), (int)(pos.z / (Game.instance.pixelSize * 8)));
    }
    /// <summary>
    /// The number will change as it crosses over the CENTER of tiles; grid is offset half a tile down/left
    /// </summary>
    public static Vector2Int worldToNavCenters(Vector3 pos)
    {
        pos -= new Vector3(Game.instance.pixelSize * 4, 0, Game.instance.pixelSize * 4);
        return new Vector2Int((int)((pos.x) / (Game.instance.pixelSize * 8)), (int)(pos.z / (Game.instance.pixelSize * 8)));
    }
    public static navTile nav(Vector2Int pos)
    {
        return Game.instance.navs[pos.x, pos.y];
    }
}
