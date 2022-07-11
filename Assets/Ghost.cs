using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ghost : MonoBehaviour
{
    Game game;

    float speed;

    public enum Direction
    {
        up,right,down,left
    }

    public Direction direction;
    Vector2Int[] directions;

    Vector2Int oldPos;
    Vector2Int oldPosCenter;

    /// <summary>
    /// The position of the last 3D tile the ghost checked for turns etc, so it can countdown to check again
    /// </summary>
    //Vector3 previousCheck;

    // Start is called before the first frame update
    void Start()
    {
        game = Game.instance;

        speed = game.ghostSpeedDefault;

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

        if (posCenter != oldPosCenter)
        {
            int oppositeDirection = (int)direction - 2;
            if (oppositeDirection < 0) oppositeDirection = 4 + oppositeDirection;
            List<Direction> validDirections = new List<Direction>();
            for (int d = 0; d < 4; d++)
            {
                //if (d == oppositeDirection || d == (int)direction)
                if (d == oppositeDirection)
                    continue;
                //if (Game.nav(pos + directions[d]).traversible)
                if (Game.nav(oldPos + directions[d]).traversible)
                {
                    //print("d " + d + "direction " + (int)direction + " opposite " + oppositeDirection);
                    //direction = (Direction)d;
                    //transform.position = Game.navToWorld(pos.x, pos.y);
                    //break;
                    validDirections.Add((Direction)d);
                }
            }
            if (validDirections.Count > 0)
            {
                //print("direction " + direction);
                //foreach(Direction dd in validDirections)
                //{
                //    print("option " + dd);
                //}
                Direction d = validDirections[(int)(Random.value * (validDirections.Count - 1) + .5f)];
                direction = d;

                // This bit starts it off in the right place and prevents it from running 2 direction changes in a row
                Vector2 offset = (Vector2)directions[(int)direction] * .0001f;
                transform.position = Game.navToWorld(pos.x, pos.y) + new Vector3(offset.x, 0, offset.y);
                oldPosCenter = posCenter = Game.worldToNavCenters(transform.position);
                //print("final direction " + direction);
            }
        }

        //if (Vector3.Distance(transform.position, previousCheck) >= game.cellWidth * 8)
        //{
        //    int oppositeDirection = (int)direction - 2;
        //    if (oppositeDirection < 0) oppositeDirection = 4 + oppositeDirection;
        //    //print("direction " + (int)direction + " opposite " + oppositeDirection);
        //    for (int d = 0; d < 4; d++)
        //    {
        //        if (d == oppositeDirection || d == (int)direction)
        //            continue;
        //        if (Game.nav(pos + directions[d]).traversible)
        //        {
        //            direction = (Direction)d;
        //            //transform.position = Game.navToWorld(pos.x, pos.y);
        //        }
        //    }
        //    //previousCheck = transform.position;
        //}

        // If crossing over the boundary of 

        //if (pos != oldPos)
        //{
        //    Game.navTile tile = Game.nav(pos);
        //    print("new tile " + tile.traversible);
        //    if (tile.turn)
        //    {
        //        print("turning");
        //        for (int d = 0; d < 4; d++)
        //        {
        //            if (Game.nav(pos + directions[d]).traversible)
        //            {
        //                direction = (Direction)d;
        //            }
        //        }
        //    }
        //}

        transform.position += vec23(directions[(int)direction]) * speed * Time.deltaTime;


        oldPos = pos;
        oldPosCenter = posCenter;
    }

    public static Vector3 vec23(Vector2Int vec2)
    {
        return new Vector3(vec2.x, 0, vec2.y);
    }
}
