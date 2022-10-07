using System;
using Core;
using System.Diagnostics;
using DataFormats;
using Domain.Elements;
using Helper;

namespace Domain.Forces
{
    internal class WallForce
    {
        public static ForceData CalculateForce(Agent currentAgent)
        {
            int forceXDirection, forceYDirection;
            CpmPair point1, point2, currAgent, projection;
            float wallLen, deltaX, deltaY, distance;

            ForceData wallForce = new ForceData();

            #region Poles
            foreach (var pole in currentAgent.Poles)
            {
                distance = Utils.DistanceBetween(currentAgent.X, currentAgent.Y, pole.VMiddle.X, pole.VMiddle.Y) - currentAgent.Radius - pole.Length;

                if (!(distance < Params.Current.DefaultSafeWallDistance)) continue;

                if (currentAgent.X - pole.VMiddle.X < 0)
         
                }
            }
            #endregion

            foreach (var wall in currentAgent.Walls)
            {
                if (wall.IsIWLWG())
                {
                    if (currentAgent.IsWithinLWGRange())
                        continue;
                }

                point1.X = wall.VStart.X;
                point1.Y = wall.VStart.Y;
                point2.X = wall.VEnd.X;
                point2.Y = wall.VEnd.Y;
                currAgent.X = currentAgent.X;
                currAgent.Y = currentAgent.Y;

                projection = Utils.MyTarget(ref currAgent, ref point1, ref point2);

                wallLen = wall.Length;

                if (!(Utils.DistanceBetween(projection.X, projection.Y, wall.VStart.X, wall.VStart.Y) <= wallLen) ||
                    !(Utils.DistanceBetween(projection.X, projection.Y, wall.VEnd.X, wall.VEnd.Y) <= wallLen)) continue;

                distance = Utils.DistanceBetween(currentAgent.X, currentAgent.Y, projection.X, projection.Y) - currentAgent.Radius;

                if (!(distance < Params.Current.DefaultSafeWallDistance)) continue;

                wallForce.IsApplied = true;

                deltaX = currentAgent.X - projection.X;
                deltaY = currentAgent.Y - projection.Y;

                if (deltaX > 0)
                    forceXDirection = 1;
                else if (deltaX < 0)
                    forceXDirection = -1;
                else
                    forceXDirection = 0;

                if (deltaY > 0)
                    forceYDirection = 1;
                else if (deltaY < 0)
                    forceYDirection = -1;
                else
                    forceYDirection = 0;

                wallForce.X += Math.Abs((deltaY + -currentAgent.Radius) / Params.Current.DefaultSafeWallDistance) * forceXDirection * Params.Current.DefaultWallForce;
                wallForce.Y += Math.Abs((deltaX + -currentAgent.Radius) / Params.Current.DefaultSafeWallDistance) * forceYDirection * Params.Current.DefaultWallForce;

                if (wall.GetType() == typeof(Barricade))
                {
                    wallForce.X *= 1.2f; // Barricade multiplier
                    wallForce.Y *= 1.2f; 
                }

                if (!(distance <= 0)) continue;

                wallForce.Collision = true;
                wallForce.XDirection = forceXDirection;
                wallForce.YDirection = forceYDirection;
                /*
                        //Recent addition
                        force_d.x_direction = force_xDirection;
                        force_d.y_direction = force_yDirection;

                        //ORIGINAL
                        force_d.x = (float)abs(((AgentY - projection.y + -AgentRadius)) / SAFE_WALL_DISTANCE) * force_xDirection  * WALL_FORCE;
                        force_d.y = (float)abs(((AgentX - projection.x + -AgentRadius)) / SAFE_WALL_DISTANCE) * force_yDirection  * WALL_FORCE;

                        if (distance <= 0)	force_d.collision = true;
                        */
            }

            return wallForce;
        }
    }
}
