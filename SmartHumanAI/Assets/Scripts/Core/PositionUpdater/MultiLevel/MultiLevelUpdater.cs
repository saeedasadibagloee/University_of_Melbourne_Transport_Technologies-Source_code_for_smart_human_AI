using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core.Handlers;
using Core.Logger;
using Core.Signals;
using Domain.Elements;
using Domain.Stairway;
using Core.Threat;

using Core.PositionUpdater.NextSteps;
using Core.PositionUpdater.EvacuationPathHandler;

using Core.PositionUpdater.HandlerFabric;
using Core.PositionUpdater.ReactionTimeHandler;
using Domain;
using Helper;

namespace Core.PositionUpdater.MultiLevel
{
    internal class MultiLevelUpdater : IPositionUpdater
    {
        private readonly ILevelHandler _levelHandler = null;
        private readonly IStairwayHandler _stairWayHandler = null;

        private readonly IDestinationLevelIdentifier _destLevelIdentifier = null;
        private readonly IReactionTimeHandler _reactionTimeHandler = null;

        private readonly NextStepUpdater _nsUpdater = null;
        private readonly List<int> _destinationLevels = null;

        public MultiLevelUpdater(ICore pCore, ILevelHandler pLevelHandler, IStairwayHandler pStairWayHandler,
            IMultiLevelHandlerFabric pMultiLevelFabric)
        {
            _levelHandler = pLevelHandler;
            _stairWayHandler = pStairWayHandler;
            _stairWayHandler.Stairs();

            var extraLevelHandlerList = pMultiLevelFabric.MultilLevelHandler();
            _destLevelIdentifier = extraLevelHandlerList.DestinationLevelHandler();
            _reactionTimeHandler = extraLevelHandlerList.ReactionTimeHandler();

            _nsUpdater = new NextStepUpdater(pCore);
            _destinationLevels = _levelHandler.DestinationLevelIDs;
        }

        public bool ReadyToUpdate(List<Agent> agents, int index, UpdateSignals signals, IThreatHandler tHandler)
        {
            Agent pAgent = agents[index];

            //set current level
            if (pAgent.ePhase == EvacuationPhase.Initial && pAgent.CurrentLevel == null)
            {
                var level = _levelHandler.Find(pAgent.LevelId);
                if (level != null)
                {
                    pAgent.CurrentLevel = level;

                    if (pAgent.UseDesignatedGate)
                    {
                        foreach (var l in _levelHandler.Levels())
                        {
                            var destinationGate = l.FindGate(pAgent.DestinationGateId);
                            if (destinationGate != null)
                            {
                                pAgent.TargetLevelId = l.LevelId;
                                break;
                            }
                        }
                    }
                    else
                    {
                        pAgent.TargetLevelId =
                            (_destinationLevels.Count == 1) ? _destinationLevels.First()
                                : _destLevelIdentifier.LevelID(pAgent.CurrentLevel.LevelId, _destinationLevels);
                    }
                }
                else
                {
                    LogWriter.Instance.WriteToLog("Impossible to determine agent's current level....quit");
                    pAgent.Active = false;
                    return false;
                }
            }

            //try
            //{
            #region

            //Check is agent is on the stairway
            if (AgentIsOnStairway(pAgent))
            {
                if (pAgent.EnvLocation != Location.MovingInsideStairway)
                {
                    pAgent.EnvLocation = Location.MovingInsideStairway;
                    pAgent.DecisionUpdateIsRequired = true;
                }
            }
            else
            {
                if (pAgent.EnvLocation == Location.MovingInsideStairway)
                {
                    var lastStairwellEntry = pAgent.StInfo.StairEntries.Last();

                    var enterDistance =
                        Utils.DistanceBetween(pAgent.X, pAgent.Y,
                            lastStairwellEntry.Value.EntryPoint.X, lastStairwellEntry.Value.EntryPoint.Y
                    );

                    var exitDistance =
                        Utils.DistanceBetween(pAgent.X, pAgent.Y,
                            lastStairwellEntry.Value.ExitPoint.X, lastStairwellEntry.Value.ExitPoint.Y
                    );

                    if ((exitDistance - enterDistance) <= 0) //stepped into new level
                    {
                        pAgent.OnDestinationLevel = (lastStairwellEntry.Value.ExitPoint.LevelId == pAgent.TargetLevelId);

                        if (lastStairwellEntry.Value.IntentedToEvacuate)
                        {
                            //set new level object
                            var nextLevelId = (pAgent.OnDestinationLevel) ? pAgent.TargetLevelId :
                                (pAgent.Direction == Direction.Up ?
                                    pAgent.CurrentLevel.LevelId + pAgent.Stair.SpanFloors : pAgent.CurrentLevel.LevelId - pAgent.Stair.SpanFloors);

                            pAgent.CurrentLevel = _levelHandler.Find(nextLevelId);

                            if (pAgent.CurrentLevel == null)
                            {
                                LogWriter.Instance.WriteToLog("Impossible to update agent's current level....quit");
                                pAgent.Active = false;
                                return false;
                            }
                        }

                        pAgent.LevelId = pAgent.CurrentLevel.LevelId;
                        pAgent.TempLevelId = pAgent.CurrentLevel.LevelId;
                    }
                    else // pushed back to a level where it came from
                    {
                        pAgent.TempLevelId = lastStairwellEntry.Value.EntryPoint.LevelId;
                        pAgent.LevelId = lastStairwellEntry.Value.EntryPoint.LevelId;
                    }

                    if (!pAgent.DecisionUpdateIsRequired)
                        pAgent.DecisionUpdateIsRequired = true;

                    pAgent.Stair = null;
                    pAgent.EvacData.LookingForGates = true;

                    if (pAgent.PushedIntoWrongStairway)
                        pAgent.PushedIntoWrongStairway = false;
                }

                if (pAgent.EnvLocation != Location.MovingInsideRoom)
                    pAgent.EnvLocation = Location.MovingInsideRoom;

                if (pAgent.Direction != Direction.Straight)
                    pAgent.Direction = Direction.Straight;

            }

            switch (pAgent.EnvLocation)
            {
                case Location.MovingInsideStairway:
                    {
                        if (pAgent.DecisionUpdateIsRequired)
                        {
                            Debug.Assert(pAgent.Stair != null, "pAgent.Stair != null");
                            var stairwayId = pAgent.Stair.StairwayID;

                            if (!pAgent.StInfo.StairEntries.ContainsKey(stairwayId)) //agent has entered new stairway
                            {
                                //agent could have been accidentally pushed into a staircase as he was trying to leave the environment
                                if (pAgent.PushedIntoWrongStairway)
                                {
                                    var bottomLevelGate = pAgent.Stair.BottomLevelGate();
                                    var topLevelGate = pAgent.Stair.TopLevelGate();

                                    //agent must leave the staircase via the nearest port
                                    var lowLevelExit =
                                            Utils.DistanceBetween(pAgent.X, pAgent.Y,
                                            bottomLevelGate.VMiddle.X, bottomLevelGate.VMiddle.Y
                                    );

                                    var topLevelExit =
                                        Utils.DistanceBetween(pAgent.X, pAgent.Y,
                                            topLevelGate.VMiddle.X, topLevelGate.VMiddle.Y
                                    );

                                    //check the exit port type
                                    var exitPortType = (lowLevelExit < topLevelExit) ? PortType.LowerPort : PortType.UpperPort;
                                    //get corresponding exit gate                       
                                    var exitGate = pAgent.Stair.GetCorrespondingPort(exitPortType);

                                    if (exitGate != null)
                                    {
                                        var gateList = new List<RoomElement> { exitGate };

                                        //update stairway movement info data                                                      
                                        GateInfo entryPoint = new GateInfo(pAgent.CurrentLevel.LevelId);
                                        entryPoint.SetGateInfo(exitGate.ElementId, exitGate.VMiddle.X, exitGate.VMiddle.Y);

                                        StairEntries newStairEntriesInfo =
                                            new StairEntries(pAgent.Stair, entryPoint, entryPoint, false);

                                        pAgent.StInfo.StairEntries.Add(stairwayId, newStairEntriesInfo);

                                        var areaId = _stairWayHandler.GetAreaID(stairwayId);

                                        _nsUpdater.SetGate(index, areaId, gateList);
                                        pAgent.Walls = pAgent.Stair.Walls;

                                        var topLevelID = pAgent.Stair.TopLevelId();
                                        var bottomLevelID = pAgent.Stair.BottomLevelId();

                                        pAgent.TempLevelId = Math.Min(bottomLevelID, topLevelID);// (topLevelID > )//pAgent.CurrentLevel.LevelId;
                                    }
                                }
                                else
                                {
                                    //get direction (up, down)
                                    var destPortType = pAgent.CurrentLevel.LevelId > pAgent.TargetLevelId ? PortType.LowerPort : PortType.UpperPort;
                                    pAgent.Direction = destPortType == PortType.LowerPort ? Direction.Down : Direction.Up;

                                    //update current level ID
                                    var nextLevelId =
                                        pAgent.Direction == Direction.Up ?
                                            pAgent.CurrentLevel.LevelId + pAgent.Stair.SpanFloors : pAgent.CurrentLevel.LevelId - pAgent.Stair.SpanFloors;

                                    pAgent.TempLevelId = pAgent.Direction == Direction.Up ? pAgent.CurrentLevel.LevelId : nextLevelId;

                                    //get entry port type
                                    var entryPortType = destPortType == PortType.LowerPort ? PortType.UpperPort : PortType.LowerPort;
                                    //get corresponding entry gate
                                    var entryGate = pAgent.Stair.GetCorrespondingPort(entryPortType);

                                    //get corresponding destination gate                       
                                    var destinationGate = pAgent.Stair.GetCorrespondingPort(destPortType);

                                    if (entryGate != null && destinationGate != null)
                                    {
                                        var gateList = new List<RoomElement> { destinationGate };

                                        //update stairway movement info data                                                      
                                        GateInfo entryPoint = new GateInfo(pAgent.CurrentLevel.LevelId);
                                        entryPoint.SetGateInfo(entryGate.ElementId, pAgent.DesiredPosition.X, pAgent.DesiredPosition.Y);

                                        GateInfo exitPoint = new GateInfo(nextLevelId);
                                        exitPoint.SetGateInfo(destinationGate.ElementId, destinationGate.VMiddle.X, destinationGate.VMiddle.Y);

                                        StairEntries newStairEntriesInfo =
                                            new StairEntries(pAgent.Stair, entryPoint, exitPoint);

                                        pAgent.StInfo.StairEntries.Add(stairwayId, newStairEntriesInfo);

                                        var areaId = _stairWayHandler.GetAreaID(stairwayId);

                                        _nsUpdater.SetGate(index, areaId, gateList);
                                        pAgent.Walls = pAgent.Stair.Walls;
                                    }
                                }
                            }
                            else //pushed into the previous stairway
                            {
                                if (pAgent.StInfo.StairEntries[stairwayId].IntentedToEvacuate)
                                {
                                    var prevStairObject = pAgent.StInfo.StairEntries.Last();
                                    var entryLevelId = prevStairObject.Value.EntryPoint.LevelId;
                                    var exitLevelId = prevStairObject.Value.ExitPoint.LevelId;

                                    pAgent.Direction = entryLevelId > exitLevelId ? Direction.Down : Direction.Up;

                                    //restore the level ID from which this agent began to walk on this stairwell
                                    pAgent.CurrentLevel = _levelHandler.Find(entryLevelId);

                                    if (pAgent.CurrentLevel == null)
                                    {
                                        LogWriter.Instance.WriteToLog("Impossible to update agent's current level....quit");
                                        pAgent.Active = false;
                                        return false;
                                    }

                                    pAgent.LevelId = pAgent.CurrentLevel.LevelId;
                                    pAgent.TempLevelId = Math.Min(entryLevelId, exitLevelId);//exitLevelId < entryLevelId ? exitLevelId : entryLevelId;//Math.Min(entryLevelID, exitLevelID);                                          

                                    var portType = pAgent.CurrentLevel.LevelId > pAgent.TargetLevelId ? PortType.LowerPort : PortType.UpperPort;
                                    var destinationGate = pAgent.Stair.GetCorrespondingPort(portType);

                                    if (destinationGate != null)
                                    {
                                        var gateList = new List<RoomElement> { destinationGate };
                                        var areaId = _stairWayHandler.GetAreaID(prevStairObject.Key);
                                        _nsUpdater.SetGate(index, areaId, gateList);

                                        pAgent.Walls = pAgent.Stair.Walls;
                                    }
                                }
                                else
                                {
                                    var bottomLevelGate = pAgent.Stair.BottomLevelGate();
                                    var topLevelGate = pAgent.Stair.TopLevelGate();

                                    //agent must leave the staircase via the nearest port
                                    var lowLevelExit =
                                        Utils.DistanceBetween(pAgent.X, pAgent.Y,
                                            bottomLevelGate.VMiddle.X, bottomLevelGate.VMiddle.Y
                                    );

                                    var topLevelExit =
                                        Utils.DistanceBetween(pAgent.X, pAgent.Y,
                                            topLevelGate.VMiddle.X, topLevelGate.VMiddle.Y
                                    );

                                    var exitPortType = (lowLevelExit < topLevelExit) ? PortType.LowerPort : PortType.UpperPort;

                                    //get corresponding exit gate                       
                                    var exitGate = pAgent.Stair.GetCorrespondingPort(exitPortType);

                                    if (exitGate != null)
                                    {
                                        var gateList = new List<RoomElement> { exitGate };
                                        var areaId = _stairWayHandler.GetAreaID(stairwayId);
                                        _nsUpdater.SetGate(index, areaId, gateList);
                                        pAgent.Walls = pAgent.Stair.Walls;

                                        pAgent.TempLevelId = (lowLevelExit < topLevelExit) ? pAgent.Stair.BottomLevelId() : pAgent.Stair.TopLevelId();
                                    }
                                }
                            }

                            pAgent.DecisionUpdateIsRequired = false;
                        }
                    }
                    break;

                case Location.MovingInsideRoom:
                    {
                        var currentRoomID = pAgent.CurrentLevel.GetRoomID(pAgent.X, pAgent.Y);

                        if (currentRoomID == -1)
                        {
                            pAgent.LeaveEnvironment();
                            return false;
                        }

                        if (pAgent.DecisionUpdateIsRequired)
                        {
                            pAgent.StartLookingForNewStair = false;
                            pAgent.VisitedRooms.Clear();
                            pAgent.OnDestinationLevel = (pAgent.CurrentLevel.LevelId == pAgent.TargetLevelId);

                            pAgent.EvacData.GateType =
                                (pAgent.OnDestinationLevel) ? InnerType.EvacuationGate :
                                    (pAgent.CurrentLevel.LevelId > pAgent.TargetLevelId ? InnerType.StairwayGateDown : InnerType.StairwayGateUp);

                            //if (pAgent.UseMultilevelPath) pAgent.EvacData.GateType = pAgent.GetCurrentMultilevelPathItem().innerType;

                            var areaID = pAgent.CurrentLevel.GetGridAreaId(currentRoomID);
                            pAgent.InitialRoomID = currentRoomID;
                            pAgent.CurrentRoomID = currentRoomID;

                            var evacuationPathIsFound =
                                (pAgent.OnDestinationLevel) ? (pAgent.UseDesignatedGate ?
                                        EvacuationHandler.GetPath<EvacPathToFixedGate>(pAgent, areaID, tHandler) :
                                        EvacuationHandler.GetPath<GeneralPath>(pAgent, areaID, tHandler)) :
                                    EvacuationHandler.GetPath<GeneralPath>(pAgent, areaID, tHandler);

                            // Use multilevel path if satisfied.
                            if (pAgent.UseDesignatedGate && (!evacuationPathIsFound || pAgent.EvacData.Path.Count == 0 || !pAgent.OnDestinationLevel))
                            {
                                if (pAgent.UseMultilevelPath)
                                    // Use existing path calculated.
                                    UpdateMultiLevelPath(pAgent, tHandler);
                                else
                                {
                                    // Calculate new path.
                                    List<LevelRoomGate> multilevelPath = MultiLevelPathfind.Process(pAgent, Core.Instance);
                                    if (multilevelPath != null && multilevelPath.Count > 0)
                                    {
                                        pAgent.MultiLevelPath = multilevelPath;
                                        UpdateMultiLevelPath(pAgent, tHandler);
                                        //Print(multilevelPath);
                                    }
                                }
                            }
                            else if (!evacuationPathIsFound)
                            {
                                return false;
                            }
                            else if (pAgent.EvacData.Path.Count == 0)
                            {
                                pAgent.Active = pAgent.HasToWait;
                                return false;
                            }

                            pAgent.DecisionUpdateIsRequired = false;
                            pAgent.ePhase = EvacuationPhase.Initial;
                        }
                        else
                        {
                            switch (pAgent.ePhase)
                            {
                                case EvacuationPhase.Initial:
                                    {
                                        var gateShouldBeSetStraightAway = false;

                                        if (pAgent.ReactionTimeRequired)
                                        {
                                            var evacuationPathSize = pAgent.EvacData.Path.Count;
                                            var gates = (pAgent.OnDestinationLevel)
                                                ? //agent is on their destination level
                                                (evacuationPathSize == 1)
                                                    ? pAgent.CurrentLevel.GetDestinationGates(currentRoomID)
                                                    : pAgent.CurrentLevel.GetCommonGates(currentRoomID,
                                                        pAgent.EvacData.Path[1])
                                                :
                                                //intermediate level
                                                ((evacuationPathSize == 1)
                                                    ? pAgent.CurrentLevel.GetSpecificRoomGatesByRoomID(currentRoomID,
                                                        pAgent.EvacData.GateType)
                                                    : pAgent.CurrentLevel.GetCommonGates(currentRoomID,
                                                        pAgent.EvacData.Path[1])
                                                );

                                            var agentReactionTimeData =
                                                _reactionTimeHandler.ReactionTime(pAgent.X, pAgent.Y, pAgent.AgentId,
                                                    gates);
                                            signals.SendSignal(CoreEventType.ReactionTimeData, agentReactionTimeData);

                                            pAgent.ReactionTime =
                                                (int)(agentReactionTimeData.ReactionTime / Params.Current.TimeStep);

                                            if (pAgent.ReactionTime == 0)
                                                gateShouldBeSetStraightAway = true;
                                            else
                                                pAgent.ePhase = EvacuationPhase.Reaction;

                                            pAgent.ReactionTimeRequired = false;
                                        }
                                        else
                                        {
                                            gateShouldBeSetStraightAway = true;
                                        }

                                        if (gateShouldBeSetStraightAway)
                                        {
                                            var areaID = pAgent.CurrentLevel.GetGridAreaId(currentRoomID);
                                            _nsUpdater.SetRoomDataForAgent(pAgent, currentRoomID);
                                            _nsUpdater.ProcessUpdatedEvacuationPath(pAgent, tHandler, areaID, index);

                                            pAgent.StartLookingForNewStair = true;
                                            pAgent.ePhase = EvacuationPhase.NextSteps;
                                        }
                                    }
                                    break;

                                case EvacuationPhase.Reaction:
                                    {
                                        if (++pAgent.NCycles >= pAgent.ReactionTime)
                                        {
                                            var areaID = pAgent.CurrentLevel.GetGridAreaId(currentRoomID);

                                            _nsUpdater.SetRoomDataForAgent(pAgent, currentRoomID);
                                            _nsUpdater.ProcessUpdatedEvacuationPath(pAgent, tHandler, areaID, index);

                                            pAgent.StartLookingForNewStair = true;
                                            pAgent.ePhase = EvacuationPhase.NextSteps;
                                        }
                                    }
                                    break;

                                case EvacuationPhase.NextSteps:
                                    _nsUpdater.CheckLocation(index, currentRoomID, pAgent, signals, tHandler, false);
                                    break;
                            }
                        }
                    }
                    break;
            }

            #endregion
            //}
            /*
            catch (Exception ex)
            {
                LogWriter.Instance.WriteToLog(ex.ToString());
            }
            */
            // Sets the next desired point to be the next closest point on the path
            return !pAgent.EvacData.UpdatingPaths && !pAgent.HasToWait && pAgent.UpdatePath();
        }

        private static void Print(List<LevelRoomGate> multilevelPath)
        {
            string msg = "";
            foreach (var step in multilevelPath)
                msg += step + " | ";
            UnityEngine.Debug.Log(msg);
        }

        private void UpdateMultiLevelPath(Agent pAgent, IThreatHandler tHandler)
        {
            pAgent.UseMultilevelPath = true;

            for (int i = 0; i < pAgent.MultiLevelPath.Count; i++)
            {
                if (pAgent.LevelId != pAgent.MultiLevelPath[i].level)
                    continue;

                var evacPlanPerRoomMultiLevel = pAgent.CurrentLevel.basicRouteData.RoutePlan[pAgent.MultiLevelPath[i].innerType].evacPlanMap[pAgent.CurrentRoomID].Rooms2gates;
                var requiredPlans = evacPlanPerRoomMultiLevel.Where(pRoute => pRoute.RoomCombination.Last() == pAgent.MultiLevelPath[i].roomId).ToList();

                if (requiredPlans.Count > 0)
                {
                    pAgent.EvacData.GateType = pAgent.MultiLevelPath[i].innerType;
                    pAgent.OnDestinationLevel = pAgent.EvacData.GateType == InnerType.EvacuationGate;

                    if (pAgent.EvacData.GateType == InnerType.StairwayGateUp)
                        pAgent.TargetLevelId = pAgent.CurrentLevel.LevelId + 1;
                    else if (pAgent.EvacData.GateType == InnerType.StairwayGateDown)
                        pAgent.TargetLevelId = pAgent.CurrentLevel.LevelId - 1;

                    var evacuationPath = LogitPathIdentifier.Path(pAgent.CurrentLevel.LevelId, pAgent.EvacData, tHandler, requiredPlans);

                    if (evacuationPath != null)
                    {
                        if (pAgent.VisitedRooms.Count != 0)
                            pAgent.VisitedRooms.Clear();

                        pAgent.EvacData.UpdatePath(evacuationPath);
                    }
                    else
                    {
                        pAgent.HasToWait = true;
                    }
                    return;
                }
            }
        }

        public uint GetGridAreaId(Agent pAgent)
        {
            if (pAgent.EnvLocation == Location.MovingInsideStairway)
                return _stairWayHandler.GetAreaID(pAgent.Stair.StairwayID);

            return pAgent.CurrentLevel.GetGridAreaId(pAgent.CurrentRoomID);
        }

        private bool AgentIsOnStairway(Agent pAgent)
        {
            if (pAgent.Stair == null
                && pAgent.StartLookingForNewStair
                && pAgent.CurrentLevel.LevelId != pAgent.TargetLevelId)
            {
                pAgent.Stair = _stairWayHandler.StairwayToUse(
                    pAgent.X, pAgent.Y, pAgent.CurrentLevel.LevelId,
                    pAgent.EvacData.GateType, pAgent.CurrentGateId
                );
            }

            if (pAgent.Direction == Direction.Straight)
            {
                //agent can either be pushed to the previous staircase
                //or enter new staircase by accident                  
                if (pAgent.StInfo.StairEntries.Count != 0)
                {
                    var previousStairwayObject = pAgent.StInfo.StairEntries.Last();

                    if (previousStairwayObject.Value.Stair.PointIsInside(pAgent.X, pAgent.Y))
                    {
                        //calculate the distance between the current location and the enter port
                        var enterDistance =
                            Utils.DistanceBetween(pAgent.X, pAgent.Y,
                                previousStairwayObject.Value.EntryPoint.X, previousStairwayObject.Value.EntryPoint.Y
                        );

                        //calculate the distance between the current location and the exit port
                        var exitDistance =
                            Utils.DistanceBetween(pAgent.X, pAgent.Y,
                                previousStairwayObject.Value.ExitPoint.X, previousStairwayObject.Value.ExitPoint.Y
                        );

                        if (exitDistance < enterDistance)
                        {
                            //current stair object points to previously visited stairway
                            pAgent.Stair = previousStairwayObject.Value.Stair;
                            return true;
                        }
                    }
                }

                //check another stairscase that does not appear in StairEntries
                var strangeStaircase =
                    _stairWayHandler.StairwayToUse(pAgent.X, pAgent.Y,
                        pAgent.CurrentLevel.LevelId, pAgent.StInfo.StairEntries
                );

                if (strangeStaircase != null)
                {
                    if (!(pAgent.Stair != null && pAgent.Stair.StairwayID == strangeStaircase.StairwayID))
                    {
                        pAgent.Stair = strangeStaircase;
                        pAgent.PushedIntoWrongStairway = true;
                        return true;
                    }
                }
            }

            if (pAgent.Stair != null)
            {
                if (pAgent.StartLookingForNewStair)
                    pAgent.StartLookingForNewStair = false;

                if (pAgent.Stair.PointIsInside(pAgent.X, pAgent.Y))
                    return true;
            }


            return false;
        }

    }
}
