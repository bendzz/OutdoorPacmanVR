using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ghost : MonoBehaviour
{
    /// <summary>
    /// Red, pink, blue, orange
    /// </summary>
    public enum Name
    {
        blinky,
        pinky,
        inky,
        clyde
    }

    [Tooltip("blinky, pinky, inky, clyde == red, pink, blue, orange ghosts.")]
    public Name ghost = Name.blinky;

    Game game;

    float speedDefault;

    public enum Direction
    {
        up,right,down,left
    }

    public Direction direction;
    Vector2Int[] directions;

    public enum State   // TODO
    {
        unspawned,
        spawning,
        chase,
        scatter,
        frightened,  // edible
        dead    // return home
    }

    public State state = State.unspawned;
    [Tooltip("if true the ghosts wander randomly in the ghost house, instead of the up down original animation.")]
    public bool wanderUnspawned = true;

    // TODO
    // Ghost
    // state spawning (with spawn point target outside door)

    [Tooltip("Need a ref to blinky to calculate Inky's movements")]
    public static Ghost blinkyRef;


    /// <summary>
    /// One ghost at a time tracks how many dots pacman has eaten to determine when it spawns https://www.gamedeveloper.com/design/the-pac-man-dossier
    /// </summary>
    public int spawnDotCounter;
    /// <summary>
    /// How big the dotCounter gets before the ghost spawns
    /// </summary>
    public int spawnDotLimit;

    /// <summary>
    /// For playing the unspawned animation and respawning
    /// </summary>
    Vector3 startPosition;
    Vector2Int oldPos;
    Vector2Int oldPosCenter;

    Material material;
    AudioSource audio;

    Vector3 startScale;

    /// <summary>
    /// What the ghost is currently aiming at
    /// </summary>
    Vector3 target;

    public List<Transform> eyes;

    // Start is called before the first frame update
    void Start()
    {
        game = Game.instance;

        if (ghost == Name.blinky)
            blinkyRef = this;

        speedDefault = game.ghostSpeedDefault;

        startScale = transform.localScale;

        directions = new Vector2Int[4];
        directions[0] = new Vector2Int(0, 1);
        directions[1] = new Vector2Int(1, 0);
        directions[2] = new Vector2Int(0, -1);
        directions[3] = new Vector2Int(-1, 0);

        // set dotLimits, from https://www.gamedeveloper.com/design/the-pac-man-dossier ghost house section
        spawnDotCounter = 0;
        spawnDotLimit = 0;
        if (ghost == Name.inky)
        {
            if (game.level == 1)
                spawnDotLimit = 30;
        } else if (ghost == Name.clyde)
        {
            if (game.level == 1)
                spawnDotLimit = 60;
            else if (game.level == 2)
                spawnDotLimit = 50;
        }

        // Spawn in a valid position somewhere
        /*
        //print("game" + game + " navs " + game.navs);
        for (int x = 15; x < 28; x++)
        {
            if (game.navs[x, 15].traversible)
            {
                transform.position = Game.navToWorld(x, 15);
                print("traversible " + x);
                break;
            }
        }
        */

        startPosition = transform.position;

        //previousCheck = transform.position;
        Vector2Int pos = Game.worldToNav(transform.position);
        //print("pos " + pos);
        for (int d = 0; d<4; d++)
        {
            if (Game.nav(pos + directions[d]).traversible)
            {
                direction = (Direction)d;
            }
        }
        oldPosCenter = Game.worldToNavCenters(transform.position);

        MeshRenderer renderer = this.GetComponent<MeshRenderer>();
        material = renderer.material;

        audio = this.GetComponent<AudioSource>();

        //audio.pitch = (4-(float)ghost) / 2;
        audio.pitch = (4 - (float)ghost) / 4 + .0f;
        //audio.pitch = ((float)ghost) / 2;

    }

    // Update is called once per frame
    void Update()
    {
        Vector2Int pos = Game.worldToNav(transform.position);
        Vector2Int posCenter = Game.worldToNavCenters(transform.position);


        // Check if out of bounds 
        Game.navTile currentTile;
        try
        {
            currentTile = Game.nav(pos);
        } catch(System.Exception e)
        {
            Debug.LogError("ghost " + ghost + " out of bounds at " + pos + ", returning to bounds.");
            transform.position = Game.navToWorld(game.getNearestTraversibleTile(transform.position));

            pos = Game.worldToNav(transform.position);
            posCenter = Game.worldToNavCenters(transform.position);
        }
        currentTile = Game.nav(pos);


        if (state == State.unspawned)
        {
            if (spawnDotCounter >= spawnDotLimit)
            {
                state = State.spawning;
                print("spawnDotLimit " + spawnDotLimit + " reached! Spawning " + ghost);
            }
            //if (ghost == Name.blinky)
            //    state = State.spawning;
        }

        if (state == State.unspawned && !currentTile.ghostStart)
            transform.position = startPosition;

        if (state == State.spawning && currentTile.traversible)
            //state = State.scatter;  // spawned in proper
            state = game.getDefaultGhostState();

        if (posCenter != oldPosCenter)  // moved past the center of a tile, check for new directions
        {
            // see if it's out of bounds and teleporting
            if (pos.x > 26)
            {
                transform.position = Game.navToWorld(1, pos.y);
                oldPosCenter = posCenter = Game.worldToNavCenters(transform.position);
                oldPos = pos = Game.worldToNav(transform.position);
            } else if (pos.x < 1)
            {
                transform.position = Game.navToWorld(26, pos.y);
                oldPosCenter = posCenter = Game.worldToNavCenters(transform.position);
                oldPos = pos = Game.worldToNav(transform.position);
            } else   // Still within bounds
            {
                // Get valid travel directions to choose from
                int oppositeDirection = (int)direction - 2;
                if (oppositeDirection < 0) oppositeDirection = 4 + oppositeDirection;
                List<Direction> validDirections = new List<Direction>();
                for (int d = 0; d < 4; d++)
                {
                    if (d == oppositeDirection)
                        continue;
                    bool traversible = false;
                    try   // sometimes checks out of bounds tiles
                    {
                        Game.navTile tile = Game.nav(oldPos + directions[d]);
                        if (tile.traversible)
                        {
                            traversible = true;
                            if (tile.noGhostUp && d == 0)   // ghosts can't turn upward at 4 intersections
                                traversible = false;
                        }
                        if (state == State.unspawned && wanderUnspawned)
                        {
                            traversible = false;
                            if (tile.ghostStart)
                                traversible = true;
                        } else if (state == State.spawning)
                        {
                            if (tile.ghostStart)
                                traversible = true;
                        } else if (state == State.dead)
                            traversible = tile.ghostStart || tile.traversible;
                    } catch (System.Exception e) { }
                    if (traversible)
                        validDirections.Add((Direction)d);

                }

                // Set direction
                {
                    if (validDirections.Count > 0)
                    {
                        // wander randomly; default
                        Direction d = validDirections[(int)(Random.value * (validDirections.Count - 1) + .5f)];

                        if (validDirections.Count > 1)
                        {
                            // Change direction?
                            //Vector3 target = Vector3.zero;
                            target = Vector3.zero;
                            if (state == State.spawning)
                                target = game.spawnTarget.position;

                            bool clydeScatter = false;
                            if (state == State.chase)
                            {
                                // https://www.gamedeveloper.com/design/the-pac-man-dossier
                                //Vector3 ppos = game.packman.transform.position;
                                Vector3 ppos = game.packman.transform.position;
                                if (ghost == Name.blinky)
                                    //target = Game.navToWorld(game.packmanPos);  // completely authentic
                                    target = ppos; // completely authentic
                                if (ghost == Name.pinky)
                                    //target = ppos + Vector3.Normalize(game.packmanFacingTarget.position - ppos) * game.pixelSize * 8 * 4; // mostly authentic; doesn't round to nearest tile, and the VR player can more easily change looking direction than original packman could
                                    target = ppos + Vector3.Normalize(game.packman.transform.forward) * game.pixelSize * 8 * 4; // mostly authentic; doesn't round to nearest tile, and the VR player can more easily change looking direction than original packman could
                                if (ghost == Name.inky)
                                {
                                    // Find the spot 2 tiles ahead of packman. Get a vector from blinky to that. extend it 2x further. That's the target.
                                    Vector3 ahead = ppos + Vector3.Normalize(game.packman.transform.forward) * game.pixelSize * 8 * 2;
                                    target = ahead + (ahead - Ghost.blinkyRef.transform.position);
                                }
                                if (ghost == Name.clyde)
                                {
                                    if (Vector3.Distance(transform.position, ppos) > (game.pixelSize * 8) * 8)
                                        target = Game.navToWorld(game.packmanPos);  // chase
                                    else
                                    {
                                        clydeScatter = true;// Scatter (back to his corner)
                                    }
                                }
                            }
                            if (state == State.scatter || clydeScatter)
                            {
                                if (ghost == Name.blinky)
                                    target = Game.navToWorld(new Vector2Int(25, 35));
                                if (ghost == Name.pinky)
                                    target = Game.navToWorld(new Vector2Int(2, 35));
                                if (ghost == Name.inky)
                                    target = Game.navToWorld(new Vector2Int(27, 0));
                                if (ghost == Name.clyde)
                                    target = Game.navToWorld(new Vector2Int(0, 0));
                            }
                            if (state == State.dead)
                            {
                                target = startPosition;
                                if (Game.worldToNav(startPosition) == Game.worldToNav(transform.position))
                                {
                                    //state = State.spawning; // TODO not quite accurate
                                    state = State.unspawned; // TODO not quite accurate
                                }
                            }

                            if (game.pausedBlinkyDemo.paused)
                            {
                                if (ghost == Name.blinky)
                                {
                                    target = game.pausedBlinkyDemo.oBLinkyPos;
                                }
                            }

                            // execute
                            if (target != Vector3.zero)
                            {
                                // choose the direction tile nearest to the target point
                                float nearestTile = 100000000;
                                foreach (Direction dir in validDirections)
                                {
                                    Vector3 tilePos = Game.navToWorld(oldPos + directions[(int)dir]);
                                    float distance = Vector3.Distance(target, tilePos);
                                    //print("turn option " + dir + " distance " + distance);
                                    if (distance < nearestTile)
                                    {
                                        d = dir;
                                        nearestTile = distance;
                                    }
                                }
                                //print("CHOSE DIRECTION " + d);
                            }
                        }

                        direction = d;
                    }
                    // This bit starts it off in the right place and prevents it from running 2 direction changes in a row
                    Vector2 offset = (Vector2)directions[(int)direction] * .0001f;
                    transform.position = Game.navToWorld(pos.x, pos.y) + new Vector3(offset.x, 0, offset.y);
                    oldPosCenter = posCenter = Game.worldToNavCenters(transform.position);
                }
            }
        }

        if (state == State.unspawned && !wanderUnspawned)   // original pacman ghost house animation
        {
            if (direction != Direction.up && direction != Direction.down)
                direction = Direction.up;

            float maxWander = game.pixelSize * 8;
            if (transform.position.z < startPosition.z - maxWander)
                direction = Direction.up;
            else if (transform.position.z > startPosition.z + maxWander)
                direction = Direction.down;
        }

        // ghost rotations
        transform.rotation = Quaternion.Euler(0, (float)direction * 90, 0);

        foreach(Transform eye in eyes)
        {
            if (target == Vector3.zero)
            {
                break;
                eye.rotation = transform.rotation;
            }
            eye.LookAt(target);
            eye.rotation = Quaternion.Slerp(transform.rotation, eye.rotation, .75f);
        }


        // move ghost
        {
            // TODO more detailed movement speeds
            // Slower in teleporting tunnel
            // Level speeds, timer speeds
            // https://www.gamedeveloper.com/design/the-pac-man-dossier
            speedDefault = game.ghostSpeedDefault;
            if (state == State.dead)
                speedDefault = game.ghostSpeedDefault * 2;
            if (state == State.frightened)
                speedDefault = game.ghostSpeedDefault * .65f;
            if (!game.paused)
            {
                transform.position += vec23(directions[(int)direction]) * speedDefault * Time.deltaTime;
            } else if (game.pausedBlinkyDemo.paused)
            {
                if (ghost == Name.blinky)
                {
                    transform.position += vec23(directions[(int)direction]) * speedDefault * Time.deltaTime;
                }
            }
        }


        bool scaredColored = false;
        if (state != State.frightened && state != State.dead)
        {
            // make em big n scary when they're close
            float pacDis = Vector3.Distance(game.packman.transform.position, transform.position);
            float growRadius = 8f;
            float bigger = Mathf.Clamp01((growRadius - pacDis) / growRadius);
            bigger = 1 + bigger * .7f;
            transform.localScale = startScale * bigger;
        } else
        {
            if (state == State.frightened)
            {
                if (game.frightened > 3)
                    scaredColored = true;
                else
                {
                    if ((game.frightened * 2) % 2 < 1)
                        scaredColored = true;
                }
            }
            transform.localScale = startScale * .6f;
        }

        if (scaredColored)
            material.SetColor("_Color", new Color(0, 0, .5f, 1));
        else
        {
            // ghost colors
            if (ghost == Name.blinky)
                material.SetColor("_Color", Color.red);
            else if (ghost == Name.pinky)
                material.SetColor("_Color", new Color(1, .5f, .7f, 1));
            else if (ghost == Name.inky)
                material.SetColor("_Color", new Color(0, 1, 1, 1));
            else if (ghost == Name.clyde)
                material.SetColor("_Color", new Color(1, .5f, .1f, 1));
        }

        if (state == State.dead)
            material.SetColor("_Color", new Color(.1f, 0, 0f, .3f));



        oldPos = pos;
        oldPosCenter = posCenter;
    }

    public static Vector3 vec23(Vector2Int vec2)
    {
        return new Vector3(vec2.x, 0, vec2.y);
    }
}
