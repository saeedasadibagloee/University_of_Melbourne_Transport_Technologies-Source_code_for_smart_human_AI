using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core.GateChoice;
using Core.Logger;
using Core.Signals;
using Domain.Elements;
using Core.Threat;
using Core.PositionUpdater.EvacuationPathHandler;
using Domain;
using Domain.Room;
using Domain.Stairway;

namespace Core.PositionUpdater.NextSteps
{
    internal class NextStepUpdater
    {
        private readonly ICore _pCore = null;

        public NextStepUpdater(ICore pCore)
        {
            _pCore = pCore;
        }

        public void ProcessUpdatedEvacuationPath(Agent pAgent, IThreatHandler tHandler, uint areaID, int index)
        {
            if (pAgent.EvacData.Path == null || pAgent.EvacData.Path.Count == 0)
            {
                UnityEngine.Debug.Log("Agent cannot determine an evac path, destroying.");
                pAgent.LeaveEnvironment();
            }
            else
            {
                pAgent.InitialRoomID = pAgent.EvacData.Path.First();
                pAgent.VisitedRooms.Add(pAgent.CurrentRoomID, pAgent.EvacIndex);
                var evacuationPathSize = pAgent.EvacData.Path.Count;

                //different action should be taken based on the lengh of evacuation path
                if (evacuationPathSize == 1)
                {
                    CheckDestinationRoom(pAgent, index, areaID, tHandler);
                }
                else
                {
                    CheckNextRoom(pAgent, index, areaID, tHandler);
                }
            }
        }

        public void CheckLocation(int agentIndex, int roomID, Agent pAgent, UpdateSignals signals, IThreatHandler tHandler, bool singleLevelCase = true)
        {
            /*if (pAgent.PreviousGateId != pAgent.CurrentGateId)
            {
                MultiAgentLogger.Instance.WriteToLog(pAgent.AgentId, "TargetGate changed from " + pAgent.PreviousGateId + " to " + pAgent.CurrentGateId);
                pAgent.PreviousGateId = pAgent.CurrentGateId;
            }*/

            var areaID = pAgent.CurrentLevel.GetGridAreaId(roomID);

            //check if new threats appear/disappear in the system
            if (pAgent.HasToWait)
            {
                if (tHandler.ThreatCollectionIsChanged())
                {
                    pAgent.HasToWait = false;
                    UseAlternativePaths(pAgent, agentIndex, areaID, tHandler);
                }

                return;
            }

            //conditions to check while travelling in the same room
            if (pAgent.CurrentRoomID == roomID)
            {
                if (tHandler.AnotherChoiceShouldBeMade() && !pAgent.EvacData.UpdatingPaths)
                {
                    if (pAgent.EvacData.Path.Count == 1 || pAgent.EvacData.DestinationRoomID == roomID)
                    {
                        CheckDestinationRoom(pAgent, agentIndex, areaID, tHandler);
                    }
                    else
                    {
                        CheckNextRoom(pAgent, agentIndex, areaID, tHandler);
                    }
                }
                else if (pAgent.EvacData.UpdatingPaths)
                {
                    if (EvacuationHandler.GetPath<GeneralPath>(pAgent, areaID, tHandler))
                    {
                        if (pAgent.HasToWait)
                            return;

                        SetRoomDataForAgent(pAgent, roomID);
                        ProcessUpdatedEvacuationPath(pAgent, tHandler, areaID, agentIndex);
                    }
                }

                return;
            }

            //MultiAgentLogger.Instance.WriteToLog(pAgent.AgentId, "\n=== AGENT LEFT ROOM ===");

            //Because of the force imposed on agents, they may simply return to the previous room
            if (pAgent.VisitedRooms.ContainsKey(roomID))
            {
                /*string message = "Path ";
                foreach (var it in pAgent.EvacData.Path)
                    message += "[" + it + "]";
                MultiAgentLogger.Instance.WriteToLog(pAgent.AgentId, message + ", Returned to previous room: " + roomID);*/

                //get all gates
                var roomGates = pAgent.CurrentLevel.GetGatesByRoomID(pAgent.CurrentRoomID);
                //find the gate that was accidentally used, and update it's share
                var closestGateIndex = Utils.ClosestGateIndex(pAgent.X, pAgent.Y, roomGates);

                pAgent.CurrentRoomID = roomID;
                SetRoomDataForAgent(pAgent, roomID);

                //Agent returned to the initial room
                if (pAgent.InitialRoomID == roomID)
                {
                    //MultiAgentLogger.Instance.WriteToLog(pAgent.AgentId, "Initial. TargetGate: " + pAgent.CurrentGateId + " Used: " + roomGates[closestGateIndex].ElementId);
                    /*
                    if (pAgent.CurrentGateId != roomGates[closestGateIndex].ElementId)
                    {
                        UnityEngine.Debug.Log("hmmm");
                    }*/

                    /*string path = "Path: ";
                    foreach (var location in pAgent.pathModified)
                        path += location.ToString();
                    MultiAgentLogger.Instance.WriteToLog(pAgent.AgentId, path);*/

                    pAgent.EvacIndex = 0;

                    if (pAgent.EvacData.Path.Count == 1)
                    {
                        if (!singleLevelCase)
                        {
                            if (!pAgent.StartLookingForNewStair)
                                pAgent.StartLookingForNewStair = true;
                        }

                        CheckDestinationRoom(pAgent, agentIndex, areaID, tHandler);
                    }
                    else
                    {
                        //update gate shares
                        signals.SendSignal(
                            CoreEventType.GateSharesUpdate,
                            new GateSharesUpdateData(pAgent.CurrentLevel.LevelId, roomGates[closestGateIndex].ElementId)
                        );

                        CheckNextRoom(pAgent, agentIndex, areaID, tHandler);
                    }
                }
                else //agent has stepped into some intermediate room again
                {
                    // MultiAgentLogger.Instance.WriteToLog(pAgent.AgentId, "Intermediate. TargetGate: " + pAgent.CurrentGateId + " Used: " + roomGates[closestGateIndex].ElementId);

                    //update gate shares
                    signals.SendSignal(
                        CoreEventType.GateSharesUpdate,
                        new GateSharesUpdateData(pAgent.CurrentLevel.LevelId, roomGates[closestGateIndex].ElementId)
                    );

                    if (!singleLevelCase)
                    {
                        if (!pAgent.StartLookingForNewStair)
                            pAgent.StartLookingForNewStair = true;
                    }

                    if (roomID == pAgent.EvacData.DestinationRoomID) //we have stepped into the last room
                    {
                        CheckDestinationRoom(pAgent, agentIndex, areaID, tHandler);
                    }
                    else
                    {
                        CheckNextRoom(pAgent, agentIndex, areaID, tHandler);
                    }
                }
            }
            else //no, an agent moved to a completely new environment
            {
                /*string message = "Path ";
                foreach (var it in pAgent.EvacData.Path)
                    message += "[" + it + "]";
                message += ", Moved to new room: " + roomID;
                MultiAgentLogger.Instance.WriteToLog(pAgent.AgentId, message);*/

                pAgent.CurrentRoomID = roomID;
                SetRoomDataForAgent(pAgent, roomID);

                //get all gates
                var roomGates = pAgent.CurrentLevel.GetGatesByRoomID(pAgent.CurrentRoomID);

                if (roomGates.Count > 0)
                {
                    //find the gate that was accidentally used, and update it's share
                    var closestGateIndex = Utils.ClosestGateIndex(pAgent.X, pAgent.Y, roomGates);

                    //update gate shares
                    signals.SendSignal(
                        CoreEventType.GateSharesUpdate,
                        new GateSharesUpdateData(pAgent.CurrentLevel.LevelId, roomGates[closestGateIndex].ElementId)
                    );
                }
                else
                {
                    UnityEngine.Debug.Log("Couldn't find any gates in room " + pAgent.CurrentRoomID);
                }

                if (pAgent.EvacData.Path.Contains(roomID))
                {
                    pAgent.VisitedRooms.Add(pAgent.CurrentRoomID, pAgent.EvacIndex);

                    if (roomID == pAgent.EvacData.DestinationRoomID) //we have finally reached the last room
                    {
                        CheckDestinationRoom(pAgent, agentIndex, areaID, tHandler);
                    }
                    else //agent is in the intermediate room, it leads to another room
                    {
                        CheckNextRoom(pAgent, agentIndex, areaID, tHandler);
                    }
                }
                else
                {
                    //check if this agent should get back to the original path
                    //or choose an alternative
                    if (pAgent.UseDesignatedGate)
                    {
                        if (roomGates.Count > 0)
                        {
                            var destinationGates = new List<RoomElement> { roomGates[Utils.ClosestGateIndex(pAgent.X, pAgent.Y, roomGates)] };
                            SetGate(agentIndex, areaID, destinationGates);
                        }
                    }
                    else
                    {
                        if (!pAgent.EvacData.UpdatingPaths)
                            UseAlternativePaths(pAgent, agentIndex, areaID, tHandler);
                    }
                }
            }
        }

        public void SetGate(int agentIndex, uint areaId, List<RoomElement> gates)
        {
            _pCore.SetGate(agentIndex, areaId, gates);
        }

        public void SetRoomDataForAgent(Agent pAgent, int roomID)
        {
            pAgent.Walls = pAgent.CurrentLevel.GetWallsAndBarricades(roomID);
            pAgent.Poles = pAgent.CurrentLevel.GetPoles(roomID);
        }

        public void UseAlternativePaths(Agent pAgent, int agentIndex, uint areaID, IThreatHandler tHandler)
        {
            //check if there are any alternative ways to evacuate                   
            if (!pAgent.CurrentLevel.basicRouteData.AlternativeRoutesAreAvailable(
                pAgent.CurrentRoomID, pAgent.EvacData.GateType,
                tHandler.BlockedObjects(new RoomThreatHandlingStrategy(), pAgent.CurrentLevel.LevelId))
            )
            {
                pAgent.HasToWait = true;
            }
            else
            {
                if (!pAgent.EvacData.UpdatingPaths)
                    pAgent.EvacData.UpdatingPaths = true;

                if (!pAgent.EvacData.LookingForGates)
                    pAgent.EvacData.LookingForGates = true;

                pAgent.EvacIndex = 0;
                EvacuationHandler.GetPath<GeneralPath>(pAgent, areaID, tHandler);
            }
        }

        /// <summary>
        /// Called when entering the final destination room (as opposed to CheckNextRoom())
        /// </summary>
        private void CheckDestinationRoom(Agent pAgent, int agentIndex, uint areaID, IThreatHandler tHandler)
        {
            //MultiAgentLogger.Instance.WriteToLog(pAgent.AgentId, "CheckDestinationRoom");

            var destinationGates = new List<RoomElement>();

            destinationGates.AddRange(
                pAgent.CurrentLevel.GetSpecificRoomGatesByRoomID(
                    pAgent.CurrentRoomID, pAgent.EvacData.GateType)
            );

            //check if destination gates are affected by threats
            IThreatHandlingStrategy strategy = new GateThreatHandlingStrategy();

            destinationGates.RemoveAll(
                pGate => tHandler.IsObjectBlocked(
                    strategy, pGate.ElementId, pAgent.CurrentLevel.LevelId)
            );


            if (pAgent.UseMultilevelPath)
            {
                destinationGates.RemoveAll(pGate => pGate.ElementId != pAgent.GetCurrentMultilevelPathItem().gateId);

                if (destinationGates.Count > 0 && pAgent.GetCurrentMultilevelPathItem().gateId == pAgent.DestinationGateId)
                    InactivateIfInTrain(pAgent);
            }
            else if (pAgent.UseDesignatedGate && pAgent.OnDestinationLevel)
            {
                destinationGates.RemoveAll(pGate => pGate.ElementId != pAgent.DestinationGateId);

                if (destinationGates.Count > 0)
                    InactivateIfInTrain(pAgent);
            }

            #region Doublecheck staircase direction
            var destinationGatesToCheck = destinationGates.ToArray();

            foreach (var gate in destinationGatesToCheck)
            {
                foreach (var stair in _pCore.GetStairs())
                {
                    if (stair.Ports.Count != 2)
                        UnityEngine.Debug.LogError("Error");

                    for (int i = 0; i < 2; i++)
                    {
                        if (stair.Ports[i].Data.ElementId == gate.ElementId)
                        {
                            // Found the matching staircase
                            if (stair.GetPortEntryID() < 0)
                                break;

                            // Remove the gate if it's against the direction of the staircase.
                            if (gate.ElementId != stair.GetPortEntryID())
                                destinationGates.Remove(gate);

                            break;
                        }
                    }
                }
            }
            #endregion

            if (destinationGates.Count == 0)
                UseAlternativePaths(pAgent, agentIndex, areaID, tHandler);
            else
                SetGate(agentIndex, areaID, destinationGates);
        }

        private static void InactivateIfInTrain(Agent pAgent)
        {
            Gate destGate = pAgent.CurrentLevel.FindGate(pAgent.DestinationGateId) as Gate;

            if (destGate != null && destGate.DesignatedOnly)
            {
                pAgent.LeaveEnvironment();
            }
        }

        /// <summary>
        /// Usually called when entering a new room (not the final destination however)
        /// </summary>
        private void CheckNextRoom(Agent pAgent, int agentIndex, uint areaID, IThreatHandler tHandler)
        {
            var shortestPathLen = pAgent.EvacData.Path.Count;
            var pathIndex = pAgent.EvacData.Path.IndexOf(pAgent.CurrentRoomID);

            if (pathIndex != -1 && pathIndex < shortestPathLen - 1)
            {
                pAgent.EvacIndex = pathIndex;
                var nextRoomID = pAgent.EvacData.Path[++pAgent.EvacIndex];

                var roomGatesToExit = new List<RoomElement>();
                roomGatesToExit.AddRange(pAgent.CurrentLevel.GetCommonGates(pAgent.CurrentRoomID, nextRoomID));

                //make sure we are not going into a room with some threats
                IThreatHandlingStrategy strategy = new GateThreatHandlingStrategy();

                roomGatesToExit.RemoveAll(
                    pGate => tHandler.IsObjectBlocked(
                        strategy, pGate.ElementId, pAgent.CurrentLevel.LevelId));

                if (roomGatesToExit.Count == 0
                    || tHandler.IsObjectBlocked(
                        new RoomThreatHandlingStrategy(), nextRoomID, pAgent.CurrentLevel.LevelId)) //next room is blocked
                {
                    UseAlternativePaths(pAgent, agentIndex, areaID, tHandler);
                }
                else
                {
                    SetGate(agentIndex, areaID, roomGatesToExit);
                }
            }
        }
    }
}
