using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ghost : MonoBehaviour
{
    Game game;

    float speedDefault;

    public enum Direction
    {
        up,right,down,left
    }

    public Direction direction;
    Vector2Int[] directions;

    Vector2Int oldPos;
    Vector2Int oldPosCenter;

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
    }

    // Update is called once per frame
    void Update()
    {
        Vector2Int pos = Game.worldToNav(transform.position);
        Vector2Int posCenter = Game.worldToNavCenters(transform.position);

        // TODO: Check if ghost is outside bounds, if it is put it back at spawn or the nearest traversible tile

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
                // Get valid directions to choose from
                int oppositeDirection = (int)direction - 2;
                if (oppositeDirection < 0) oppositeDirection = 4 + oppositeDirection;
                List<Direction> validDirections = new List<Direction>();
                for (int d = 0; d < 4; d++)
                {
                    //if (d == oppositeDirection || d == (int)direction)
                    if (d == oppositeDirection)
                        continue;
                    try   // sometimes checks out of bounds tiles
                    {
                        if (Game.nav(oldPos + directions[d]).traversible)
                        {
                            validDirections.Add((Direction)d);
                        }
                    } catch (System.Exception e) { }
                }

                if (validDirections.Count > 0)
                {
                    Direction d = validDirections[(int)(Random.value * (validDirections.Count - 1) + .5f)];
                    direction = d;

                    // This bit starts it off in the right place and prevents it from running 2 direction changes in a row
                    Vector2 offset = (Vector2)directions[(int)direction] * .0001f;
                    transform.position = Game.navToWorld(pos.x, pos.y) + new Vector3(offset.x, 0, offset.y);
                    oldPosCenter = posCenter = Game.worldToNavCenters(transform.position);
                }
            }
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
