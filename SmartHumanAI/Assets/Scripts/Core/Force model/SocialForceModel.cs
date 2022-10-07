using Domain.Forces;
using System;
using Gate = Domain.Elements.Gate;
#if DEBUG_PERF
using System.Diagnostics;
#endif

namespace Core.ForceModel
{
    /// <summary>
    /// Implementation of Social Force Model    /// 
    /// </summary>
    internal class SocialForceModel : IForceModel
    {
        public void Apply(Agent pAgent)
        {
                     
#if DEBUG_PERF
            Stopwatch watch = Stopwatch.StartNew();
#endif
            #region Calculate Neighbor Forces

            float neighborForceX = 0f;
            float neighborForceY = 0f;

          #endregion

#if DEBUG_PERF
            Helper.MethodTimer.AddTime("NeighborForce.CalculateForce", watch);
            watch = Stopwatch.StartNew();
#endif
                            // Control staircase speed if necessitated
                if (pAgent.EnvLocation == Location.MovingInsideStairway)
                {
                    if (pAgent.Stair.Speed > 0)
                        rvoSpeed = pAgent.Stair.Speed;
                    else if (Consts.HFEnabled)
                        rvoSpeed = HeightFatigue(pAgent.Stair.BottomLevelId());
                }

                float agentGateDistance = Utils.DistanceBetween(pAgent.DesiredGateReal.X, pAgent.DesiredGateReal.Y, pAgent.X, pAgent.Y);

                // Disregard RVO / Pathfinding when close to the final gate point.
                if (agentGateDistance < 2f && (!pAgent.isInWaitingQueue || pAgent.pathOriginal.Count < 4))
                    target = pAgent.PositionDesired;
                else if (Consts.RVODisabled)
                    target = pAgent.PositionDesired;

                if (!Params.panicMode) // Normal mode only
                {
                    // If waiting for a train, stop moving.
                    if (pAgent.TrainPhase == TrainPhase.Waiting && agentGateDistance <= 0.5f)
                        target = pAgent.Position;

                    // If waiting for a train door, stop moving.
                    if (pAgent.TrainPhase == TrainPhase.WaitingAtDoor && agentGateDistance <= 2.5f)
                        target = pAgent.Position;

                    // If alighting a train, ignore all CA & pathfinding and just alight.
                    if (pAgent.TrainPhase == TrainPhase.Alighting)
                        target = pAgent.PositionGate;

                    // If waiting at a waiting point, stop moving.
                    if (pAgent.DetourPhase == DetourPhase.Waiting)
                        target = pAgent.Position;

                            }

                    // When leaving a waiting gate
                    if (pAgent.PreviousGate != null)
                    {
                        if (pAgent.PreviousGate.WaitingData != null)
                        {
                            if (Math.Abs(pAgent.PreviousGate.VMiddle.X - pAgent.X) < 0.8f &&
                                Math.Abs(pAgent.PreviousGate.VMiddle.Y - pAgent.Y) < 0.8f)
                            {
                                List<RoomElement> walls = pAgent.Walls.Where(wall => !wall.IsIWLWG()).ToList();
                                if (Utils.GateIsVisible(pAgent.X, pAgent.Y, pAgent.PreviousGate, walls, pAgent.Poles))
                                {
                                    target =
                                    pAgent.PreviousGate.WaitingData.isBidirectional &&
                                    pAgent.PreviousGate.WaitingData.currentDirection == -1
                                        ? new Vector2(pAgent.PreviousGate.WaitingData.targetPos2X,
                                            pAgent.PreviousGate.WaitingData.targetPos2Y)
                                        : new Vector2(pAgent.PreviousGate.WaitingData.targetPosX,
                                            pAgent.PreviousGate.WaitingData.targetPosY);
                                }
                            }
                        }
                    }

                               }
                    }
                }

                targetDistance =
                    Utils.DistanceBetween(target.x, target.y, pAgent.X, pAgent.Y);

                targetDistance = targetDistance == 0f ? 0.001f : targetDistance;

                pAgent.GateXDirection = (target.x - pAgent.X) / targetDistance;
                pAgent.GateYDirection = (target.y - pAgent.Y) / targetDistance;

                if (float.IsNaN(pAgent.GateXDirection) ||
                float.IsNaN(pAgent.GateYDirection))
                {
                    UnityEngine.Debug.LogError("GateDirections were NaN.");
                    UnityEngine.Debug.LogError("targetDistance: " + targetDistance);
                }
                

#if DEBUG_PERF
            Helper.MethodTimer.AddTime("CalculateDesiredForce", watch);
            watch = Stopwatch.StartNew();
#endif

            var wallForce = WallForce.CalculateForce(pAgent);

#if DEBUG_PERF
            Helper.MethodTimer.AddTime("WallForce.CalculateForce", watch);
            watch = Stopwatch.StartNew();
#endif
            // Cumulate forces
            pAgent.XForce = desiredXForce + neighborForceX + wallForce.X;
            pAgent.YForce = desiredYForce + neighborForceY + wallForce.Y;

            #region Calculate Current Velocity and Acceleration

            //Calculat Current Velocity
            pAgent.XVelocity =
                    oldXVelocity +
                    (float)((pAgent.XAcceleration +
                              pAgent.XForce / pAgent.Mass) / 2.0 * Params.Current.TimeStep);
            pAgent.YVelocity =
                oldYVelocity +
                (float)((pAgent.YAcceleration +
                          pAgent.YForce / pAgent.Mass) / 2.0 * Params.Current.TimeStep);

            //at this stage we should make sure that an agent is not exceeding its max speed
            var speedMagnitude = Math.Sqrt(pAgent.XVelocity * pAgent.XVelocity +
                                           pAgent.YVelocity * pAgent.YVelocity);

            if (speedMagnitude > rvoSpeed)
            {
                pAgent.XVelocity = (float)(pAgent.XVelocity * rvoSpeed / speedMagnitude);
                pAgent.YVelocity = (float)(pAgent.YVelocity * rvoSpeed / speedMagnitude);
            }

            // Fix agent speed to escalator speed
            if (!Params.panicMode && pAgent.EnvLocation == Location.MovingInsideStairway &&
                pAgent.Stair.GetStairwayType() == StarwayType.Escalator)
            {
                CpmPair dir = pAgent.Stair.GetDirectionVector();
                pAgent.XVelocity = dir.X * Params.Current.AgentEscalatorSpeed;
                pAgent.YVelocity = dir.Y * Params.Current.AgentEscalatorSpeed;
            }

            //Calculate Acceleration
            pAgent.XAcceleration = pAgent.XForce / pAgent.Mass;
            pAgent.YAcceleration = pAgent.YForce / pAgent.Mass;

            if (wallForce.Collision && (pAgent.XVelocity < 0 && wallForce.XDirection > 0 ||
                                        pAgent.XVelocity > 0 && wallForce.XDirection < 0))
            {
                pAgent.XVelocity = 0;
                oldXVelocity = 0;
            }

            if (wallForce.Collision && (pAgent.YVelocity < 0 && wallForce.YDirection > 0 ||
                                        pAgent.YVelocity > 0 && wallForce.YDirection < 0))
            {
                pAgent.YVelocity = 0;
                oldYVelocity = 0;
            }

            pAgent.XTmp += (float)((pAgent.XVelocity + oldXVelocity) / 2.0) * Params.Current.TimeStep;
            pAgent.YTmp += (float)((pAgent.YVelocity + oldYVelocity) / 2.0) * Params.Current.TimeStep;

            #endregion

#if DEBUG_PERF
            Helper.MethodTimer.AddTime("Calculate Velocity & Acceleration", watch);
            watch = Stopwatch.StartNew();
#endif

            if (wallForce.Collision && (pAgent.XVelocity < 0 && wallForce.XDirection > 0 ||
                                        pAgent.XVelocity > 0 && wallForce.XDirection < 0))
            {
                pAgent.XTmp = pAgent.X;
                pAgent.XVelocity = 0;
                oldXVelocity = 0;
            }

            if (wallForce.Collision && (pAgent.YVelocity < 0 && wallForce.YDirection > 0 ||
                                        pAgent.YVelocity > 0 && wallForce.YDirection < 0))
            {

                pAgent.XTmp = pAgent.X;
                pAgent.YTmp = pAgent.Y;

                pAgent.XVelocity = 0;
                oldXVelocity = 0;

                pAgent.YVelocity = 0;
                oldYVelocity = 0;
            }

#if DEBUG_PERF
            Helper.MethodTimer.AddTime("WallForce.CalculateForce", watch);
#endif
        }

        internal float HeightFatigue(int bottomLevelId)
        {
            float height = bottomLevelId * Statics.LevelHeight;

                      return Params.Current.HFFatigue * height + Params.Current.HFIntialVelocity;
        }
    }
}
