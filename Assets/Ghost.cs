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
        panic,  // edible
        dead    // return home
    }

    public State state = State.unspawned;
    [Tooltip("if true the ghosts wander randomly in the ghost house, instead of the up down original animation.")]
    public bool wanderUnspawned = true;

    // TODO
    // Ghost
    // state spawning (with spawn point target outside door)
    // Ghosts can't go upward on green nav points!




    /// <summary>
    /// One ghost at a time tracks how many dots pacman has eaten to determine when it spawns https://www.gamedeveloper.com/design/the-pac-man-dossier
    /// </summary>
    int dotCounter;
    /// <summary>
    /// How big the dotCounter gets before the ghost spawns
    /// </summary>
    int dotLimit;

    /// <summary>
    /// For playing the unspawned animation and respawning
    /// </summary>
    Vector3 startPosition;
    Vector2Int oldPos;
    Vector2Int oldPosCenter;

    Material material;
    AudioSource audio;

    // Start is called before the first frame update
    void Start()
    {
        game = Game.instance;

        speedDefault = game.ghostSpeedDefault;

        directions = new Vector2Int[4];
        directions[0] = new Vector2Int(0, 1);
        directions[1] = new Vector2Int(1, 0);
        directions[2] = new Vector2Int(0, -1);
        directions[3] = new Vector2Int(-1, 0);

        // set dotLimits, from https://www.gamedeveloper.com/design/the-pac-man-dossier ghost house section
        dotCounter = 0;
        dotLimit = 0;
        if (ghost == Name.inky)
        {
            if (game.level == 1)
                dotLimit = 30;
        } else if (ghost == Name.clyde)
        {
            if (game.level == 1)
                dotLimit = 60;
            else if (game.level == 2)
                dotLimit = 50;
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

        audio.pitch = (4-(float)ghost) / 2;

        if (ghost == Name.blinky)
        {
            material.SetColor("_Color", Color.red);
            state = State.scatter;
        }
        else if (ghost == Name.pinky)
            material.SetColor("_Color", new Color(1,.5f,.7f,1));
        else if (ghost == Name.inky)
            material.SetColor("_Color", new Color(0, 1, 1, 1));
        else if (ghost == Name.clyde)
            material.SetColor("_Color", new Color(1, .5f, .1f, 1));

    }

    // Update is called once per frame
    void Update()
    {
        Vector2Int pos = Game.worldToNav(transform.position);
        Vector2Int posCenter = Game.worldToNavCenters(transform.position);

        // TODO: Check if ghost is outside bounds, if it is put it back at spawn or the nearest traversible tile

        Game.navTile currentTile = Game.nav(pos);

        if (state == State.unspawned)
            if (dotCounter >= dotLimit)
                state = State.spawning;

        if (state == State.unspawned && !currentTile.ghostStart)
            transform.position = startPosition;

        if (state == State.spawning && currentTile.traversible)
            state = State.scatter;  // spawned in proper

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
                            if (tile.noGhostUp && d == 0)
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
                        }
                    } catch (System.Exception e) { }
                    if (traversible)
                        validDirections.Add((Direction)d);

                }

                if (validDirections.Count > 0)
                {
                    // wander randomly; default
                    Direction d = validDirections[(int)(Random.value * (validDirections.Count - 1) + .5f)];

                    if (validDirections.Count > 1)
                    {
                        // Change direction?
                        Vector3 target = Vector3.zero;
                        if (state == State.spawning)
                            target = game.spawnTarget.position;


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

            float maxWander = game.cellWidth * 8;
            if (transform.position.z < startPosition.z - maxWander)
                direction = Direction.up;
            else if (transform.position.z > startPosition.z + maxWander)
                direction = Direction.down;
        }

        speedDefault = game.ghostSpeedDefault;
        transform.position += vec23(directions[(int)direction]) * speedDefault * Time.deltaTime;

        oldPos = pos;
        oldPosCenter = posCenter;
    }

    public static Vector3 vec23(Vector2Int vec2)
    {
        return new Vector3(vec2.x, 0, vec2.y);
    }
}
