using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Assets.Scripts.PathfindingExtensions;
using Core;
using Core.Logger;
using DataFormats;
using Domain.Elements;
using Domain.Level;
using Domain.Stairway;
using Helper;
using Pathfinding;
using Pathfinding.RVO;
using UnityEngine;
using Gate = Domain.Elements.Gate;

namespace Domain
{
    public struct CpmPair
    {
        public float X;
        public float Y;

        public CpmPair(float pX = 0, float pY = 0)
        {
            X = pX;
            Y = pY;
        }

        public CpmPair(Vertex fromVertex)
        {
            X = fromVertex.X;
            Y = fromVertex.Y;
        }
    }

    internal struct FlowData
    {
        public FlowData(int pNFlows = 0, int pCongestion = 0)
        {
           
                    {
                        detourPosition =
                            currentWaitPoint.GeneratePointWithin(CurrentLevel, CurrentRoomID);
                    }
                    break;
            }

            if (!P.ModifierApplied)
            {
                P.ModifierApplied = true;

                pathOriginal.Clear();

                try
                {
                    for (int index = 0; index < P.vectorPath.Count; index++)
                    {
                        if (index > P.vectorPath.Count - 1)
                        {
                            pathOriginal.Clear();
                            break;
                        }
                        pathOriginal.Add(P.vectorPath[index]);
                    }
                }
                catch (Exception e)
                {
                    LogWriter.Instance.WriteToLog(e.ToString());
                }

                if (P.vectorPath.Count > 4)
                {
                    try
                    {
                        FunnelModifierSingleton.Instance.Apply(P);
                    }
                    catch (Exception)
                    {
                        LogWriter.Instance.WriteToLog("Funnel Modifier Error, discarding.");
                    }
                }

                pathModified = P.vectorPath;
            }

            float closestDistance = float.MaxValue;

            for (int i = Mathf.Max(0, _closestPointIndex - 1); i < P.vectorPath.Count; i++)
            {
                float currentDistance = Utils.DistanceBetween(X, Y, P.vectorPath[i].x, P.vectorPath[i].z);

                if (!(currentDistance < closestDistance)) continue;

                _closestPointIndex = i;
                closestDistance = currentDistance;
            }

            if (Nbrs.Count > 3)
                PathIndex = _closestPointIndex + 1;
            else
                PathIndex = _closestPointIndex + 1;


            if (P.vectorPath.Count - 1 > PathIndex)
            {
                // Midway through generated path
                DesiredPosition.X = P.vectorPath[PathIndex].x;
                DesiredPosition.Y = P.vectorPath[PathIndex].z;
            }
            else
            {
                // End of generated path
                if (DetourPhase == DetourPhase.Heading || DetourPhase == DetourPhase.Wondering)
                {
                    DesiredPosition = detourPosition;
                }
                else
                {
                    DesiredPosition.X = DesiredGateReal.X;
                    DesiredPosition.Y = DesiredGateReal.Y;

                    if (!Params.panicMode)
                    {
    
                            }
                        }
                    }
                }
            }
#if TRYCATCH
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                LogWriter.Instance.WriteToLog(e.ToString());
            }
#endif
            return true;
        }

        internal float RemainingDistance()
        {
            if (P == null || !P.IsDone())
                return -1f;

            var remainingDistance = 0f;
            for (int i = _closestPointIndex; i < P.vectorPath.Count - 1; i++)
                remainingDistance += Vector3.Distance(P.vectorPath[i], P.vectorPath[i + 1]);

            return remainingDistance;
        }

        internal bool IsZero()
        {
            return X == 0 && Y == 0;
        }

        internal float FindLevelHeight()
        {
            return Utils.FindLevelHeight(TempLevelId);
        }

        public void InitialiseRvo()
        {
            if (RVOSimulator.active == null)
            {
                Debug.LogError("No RVOSimulator component found in the scene. Please add one.");
            }
            else
            {
                RvoSimulator = RVOSimulator.active.GetSimulator();
                RvoAgent = RvoSimulator.AddAgent(Position, 1f);
                RvoAgent.Position = Position;
                RvoAgent.Radius = Mathf.Max(0.001f, Radius * 1.4f);
                RvoAgent.AgentTimeHorizon = Params.Current.RvoTimeHorizonAgent;
                RvoAgent.ObstacleTimeHorizon = Params.Current.RvoTimeHorizonObstacle;
                RvoAgent.Locked = RvoIsLocked;
                RvoAgent.MaxNeighbours = Params.Current.RvoMaxNeighbours;
                RvoAgent.DebugDraw = false;
                RvoAgent.Layer = RvoLayer;
                RvoAgent.CollidesWith = RvoCollidesLayer;
                RvoAgent.Priority = RvoPriority;
                RvoAgent.Position = Position;
                RvoAgent.Height = RvoHeight;
                //RvoAgent.ElevationCoordinate = 0;
            }
        }

        public void SetRvoValues()
        {
            if (RvoAgent != null)
            {
                RvoAgent.SetTarget(PositionDesired, MaxSpeed, MaxSpeed / 0.6f);
                RvoAgent.Position = Position;
                RvoAgent.CollidesWith = RvoAgent.Layer = (RVOLayer)(1 << TempLevelId); //LevelId;
            }
            
        }

        public Vector2 GetRvoTarget()
        {
            if (RvoAgent == null) return Vector2.zero;
            return RvoAgent.CalculatedTargetPoint;
        }

        public float GetRvoSpeed()
        {
            if (RvoAgent == null) return MaxSpeed;
            return RvoAgent.CalculatedSpeed;
        }

        public void RVORemove()
        {
            if (RvoRemoved || RvoAgent == null) return;
            RvoRemoved = true;
            RVOSimulator.active.GetSimulator().RemoveAgent(RvoAgent);
        }

        public void UpdateWaitingData(List<Agent> agents, Core.Core core)
        {
            if (!Active)
                return;

            if (!isInWaitingQueue)
                return;

            isPhysicallyInQueue = isBehindPersonInQueue && Speed < 1f;

            RoomElement element = CurrentLevel.FindGate(CurrentGateId);

            if (element.ElementId != CurrentGateId) Debug.Log("Got wrong gate");

            Gate gate = (Gate)element;

            if (gate.WaitingData == null)
            {
                Debug.Log("Waiting for a non-waiting gate??");
                return;
            }

            numberInQueue = gate.WaitingData.agentsInQueue.IndexOf(AgentIndex);

            if (numberInQueue == -1)
            {
                //Debug.Log("Can't find myself in the queue...");
                if (gate.WaitingData.agentsInQueue.Count == 0) return;

                var lastAgentInQueue = agents[gate.WaitingData.agentsInQueue.Last()];
                DesiredPosition.X = lastAgentInQueue.X;
                DesiredPosition.Y = lastAgentInQueue.Y;
                //DesiredPosition = DesiredGateReal;
                return;
            }

            }
    }
}
