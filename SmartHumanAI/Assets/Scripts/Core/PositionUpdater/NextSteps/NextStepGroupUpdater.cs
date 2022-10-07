using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core.GateChoice;
using Core.Logger;
using Core.Signals;
using Domain.Elements;
using Core.Threat;
using Domain.Level;
using Core.PositionUpdater.EvacuationPathHandler;
using Core.GroupBehaviour;
using Core.GroupBehaviour.DecisionUpdate;

namespace Core.PositionUpdater.NextSteps
{
    internal class NextStepGroupUpdater
    {
        private readonly ICore _pCore = null;
        private readonly IGroupHandler _groupHandler = null;
        private UpdateSignals _signals = null;

        public NextStepGroupUpdater(ICore pCore, IGroupHandler pGroupHandler)
        {
            _pCore = pCore;
            _groupHandler = pGroupHandler;
        }

        public void SetSignals(UpdateSignals pSignals) { _signals = pSignals; }         

        public void ProcessUpdatedEvacuationPath(Domain.Agent pAgent, 
                                                 IThreatHandler tHandler, 
                                                 uint areaID, int index)
        {
            pAgent.InitialRoomID = pAgent.EvacData.Path.First();
            //pAgent.EvacData.DestinationRoomID = pAgent.EvacData.Path[evacuationPathSize - 1];
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

        public void CheckLocation(int agentIndex, int roomID, Domain.Agent pAgent, UpdateSignals signals, IThreatHandler tHandler, bool singleLevelCase = true)
        {
            var areaID = pAgent.CurrentLevel.GetGridAreaId(roomID);

            //condition for a follower to keep an eye on their leader
            if (pAgent.Type == Domain.AgentType.Follower
                && pAgent.ePhase == Domain.EvacuationPhase.WatchForLeader
                && pAgent.CurrentRoomID == roomID)
            {
                if (_groupHandler.IsGateSet(
                        Convert.ToInt32(pAgent.GroupID_C),
                            pAgent.CurrentLevel.LevelId, pAgent.CurrentRoomID))
                {
                    UpdateGateForFollower(pAgent, agentIndex, areaID);
                    pAgent.ePhase = Domain.EvacuationPhase.NextSteps;
                }
            }

            //check if new threats appear/disappear in the system
            if (pAgent.HasToWait)
            {
                if (pAgent.Type == Domain.AgentType.Follower)
                {
                    //check if new gate is available
                    if (_groupHandler.IsGateSet(
                        Convert.ToInt32(pAgent.GroupID_C),
                            pAgent.CurrentLevel.LevelId, pAgent.CurrentRoomID))
                    {
                        UpdateGateForFollower(pAgent, agentIndex, areaID);
                        pAgent.HasToWait = false;
                    }
                }
                else
                {
                    if (tHandler.ThreatCollectionIsChanged())
                    {
                        pAgent.HasToWait = false;
                        UseAlternativePaths(pAgent, agentIndex, areaID, tHandler);
                    }
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

            //Because of the force imposed on agents, they may simply return to the previous room
            if (pAgent.VisitedRooms.ContainsKey(roomID))
            {
                pAgent.CurrentRoomID = roomID;
                SetRoomDataForAgent(pAgent, roomID);

                //agent got back to the initial room
                if (pAgent.InitialRoomID == roomID)
                {
                    pAgent.EvacIndex = 0;

                    if (pAgent.EvacData.Path.Count == 1)
                    {
                        if (!singleLevelCase)
                        {
                            if (!pAgent.StartLookingForNewStair)
                                pAgent.StartLookingForNewStair = true;
                        }

                        if (pAgent.Type == Domain.AgentType.Follower)
                        {
                            if (_groupHandler.IsGateSet(
                                Convert.ToInt32(pAgent.GroupID_C),
                                    pAgent.CurrentLevel.LevelId, pAgent.CurrentRoomID))
                            {
                                UpdateGateForFollower(pAgent, agentIndex, areaID);                                
                            }
                        }
                        else
                        {
                            CheckDestinationRoom(pAgent, agentIndex, areaID, tHandler);
                        }                        
                    }
                    else
                    {
                        //get all gates
                        var roomGates = pAgent.CurrentLevel.GetGatesByRoomID(pAgent.CurrentRoomID);
                        //find the gate that was accidentally used, and update it's share
                        var closestGateIndex = Utils.ClosestGateIndex(pAgent.X, pAgent.Y, roomGates);

                        //update gate shares
                        signals.SendSignal(
                            CoreEventType.GateSharesUpdate,
                            new GateSharesUpdateData(pAgent.CurrentLevel.LevelId, roomGates[closestGateIndex].ElementId)
                        );

                        if (pAgent.Type == Domain.AgentType.Follower)
                        {
                            if (_groupHandler.IsGateSet(
                                Convert.ToInt32(pAgent.GroupID_C),
                                    pAgent.CurrentLevel.LevelId, pAgent.CurrentRoomID))
                            {
                                UpdateGateForFollower(pAgent, agentIndex, areaID);
                            }
                        }
                        else
                        {
                            CheckNextRoom(pAgent, agentIndex, areaID, tHandler);
                        }                        
                    }
                }
                else //agent has stepped into some intermediate room again
                {
                    //get all gates
                    var roomGates = pAgent.CurrentLevel.GetGatesByRoomID(pAgent.CurrentRoomID);
                    //find the gate that was accidentally used, and update it's share
                    var closestGateIndex = Utils.ClosestGateIndex(pAgent.X, pAgent.Y, roomGates);

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
                pAgent.CurrentRoomID = roomID;
                SetRoomDataForAgent(pAgent, roomID);

                //get all gates
                var roomGates = pAgent.CurrentLevel.GetGatesByRoomID(pAgent.CurrentRoomID);
                //find the gate that was accidentally used, and update it's share
                var closestGateIndex = Utils.ClosestGateIndex(pAgent.X, pAgent.Y, roomGates);

                //update gate shares
                signals.SendSignal(
                    CoreEventType.GateSharesUpdate,
                    new GateSharesUpdateData(pAgent.CurrentLevel.LevelId, roomGates[closestGateIndex].ElementId)
                );

                if (pAgent.Type != Domain.AgentType.Follower)
                {
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
                        //check if this agent shouldget back to the original path
                        //or choose an alternative
                        if (pAgent.UseDesignatedGate || pAgent.Type == Domain.AgentType.Leader)
                        {
                            var destinationGates = new List<RoomElement> { roomGates[closestGateIndex] };
                            SetGate(agentIndex, areaID, destinationGates);
                        }
                        else
                        {
                            UseAlternativePaths(pAgent, agentIndex, areaID, tHandler);
                        }
                    }
                }
                else
                {
                    //check if new gate is available
                    if (_groupHandler.IsGateSet(
                        Convert.ToInt32(pAgent.GroupID_C), 
                            pAgent.CurrentLevel.LevelId, pAgent.CurrentRoomID))
                    {
                        UpdateGateForFollower(pAgent, agentIndex, areaID);
                    }
                    else
                    {
                        //wait till leader updates gate entry
                        //pAgent.HasToWait = true;
                        pAgent.ePhase = Domain.EvacuationPhase.WatchForLeader;

                        if (pAgent.EvacData.Path.Contains(roomID))
                        {
                            pAgent.VisitedRooms.Add(pAgent.CurrentRoomID, ++pAgent.EvacIndex);

                            if (roomID == pAgent.EvacData.DestinationRoomID) //we have finally reached the last room
                            {
                                CheckDestinationRoom(pAgent, agentIndex, areaID, tHandler);
                            }
                            else //agent is in the intermediate room, it leads to another room
                            {
                                CheckNextRoom(pAgent, agentIndex, areaID, tHandler);
                            }
                        }
                    }
                }
            }
        }

        public int SetGate(int agentIndex, uint areadID, List<RoomElement> gates)
        {
            return _pCore.SetGate(agentIndex, areadID, gates);
        }

        public void SetRoomDataForAgent(Domain.Agent pAgent, int roomID)
        {
            pAgent.Walls = pAgent.CurrentLevel.GetWallsAndBarricades(roomID);
            pAgent.Poles = pAgent.CurrentLevel.GetPoles(roomID);
        }

        public void UseAlternativePaths(Domain.Agent pAgent, int agentIndex, uint areaID, IThreatHandler tHandler)
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

        private void UpdateGateForFollower(Domain.Agent pAgent, int agentIndex, uint areaID)
        {
            var evacGate =
                _groupHandler.Gate(
                    Convert.ToInt32(pAgent.GroupID_C),
                    pAgent.CurrentLevel.LevelId, pAgent.CurrentRoomID);

            SetRoomDataForAgent(pAgent, pAgent.CurrentRoomID);
            SetGate(agentIndex, areaID, new List<RoomElement> { evacGate });
        }

        private void CheckDestinationRoom(Domain.Agent pAgent, int agentIndex, uint areaID, IThreatHandler tHandler)
        {
            var destinationGates = new List<RoomElement>();

            destinationGates.AddRange(
                pAgent.CurrentLevel.GetSpecificRoomGatesByRoomID(
                    pAgent.EvacData.DestinationRoomID, pAgent.EvacData.GateType)
            );

            //check if destination gates are affected by threats
            IThreatHandlingStrategy strategy = new GateThreatHandlingStrategy();

            destinationGates.RemoveAll(
                pGate => tHandler.IsObjectBlocked(
                    strategy, pGate.ElementId, pAgent.CurrentLevel.LevelId)
            );

            //just in case if this agent should use designated gate
            if (pAgent.UseDesignatedGate && pAgent.OnDestinationLevel)
                destinationGates.RemoveAll(pGate => pGate.ElementId != pAgent.DestinationGateId);

            if (destinationGates.Count == 0)
                UseAlternativePaths(pAgent, agentIndex, areaID, tHandler);
            else
            {
                var index = SetGate(agentIndex, areaID, destinationGates);

                //send update gate index signal
                if (index != -1 && pAgent.Type == Domain.AgentType.Leader)
                {
                    _signals.SendSignal(
                        CoreEventType.GroupDataUpdate,
                        new GateChoiceUpdate(
                            Convert.ToInt32(pAgent.GroupID_C), pAgent.CurrentLevel.LevelId, 
                            pAgent.CurrentRoomID, destinationGates[index])
                    );
                }
            }  
        }

        private void CheckNextRoom(Domain.Agent pAgent, int agentIndex, uint areaID, IThreatHandler tHandler)
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
                    var gateIndex = SetGate(agentIndex, areaID, roomGatesToExit);

                    //send update gate index signal
                    if (gateIndex != -1 && pAgent.Type == Domain.AgentType.Leader)
                    {
                        _signals.SendSignal(
                            CoreEventType.GroupDataUpdate,
                            new GateChoiceUpdate(
                                Convert.ToInt32(pAgent.GroupID_C), pAgent.CurrentLevel.LevelId,
                                pAgent.CurrentRoomID, roomGatesToExit[gateIndex])
                        );
                    }
                }
            }
        }
    }
}
