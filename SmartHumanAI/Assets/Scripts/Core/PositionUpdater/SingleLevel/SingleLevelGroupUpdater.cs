using System;
using Core.GateChoice;
using Core.Handlers;
using Core.Logger;
using Core.Signals;
using Core.Threat;
using Domain.Elements;

using System.Collections.Generic;
using System.Linq;
using Domain;
using Core.PositionUpdater.NextSteps;
using Core.PositionUpdater.ReactionTimeHandler;
using Core.PositionUpdater.EvacuationPathHandler;
using Core.PositionUpdater.HandlerFabric;
using Core.GroupBehaviour;
using Core.GroupBehaviour.DecisionUpdate;

namespace Core.PositionUpdater.SingleLevel
{
    internal class SingleLevelGroupUpdater : IGroupPositionUpdater
    {
        private readonly ILevelHandler _pLevelHdl = null;       
        private readonly IGroupHandler _groupHandler = null;
        private readonly IReactionTimeHandler _pRTHandler = null;
       
        private readonly NextStepGroupUpdater _nsUpdater = null;

        public SingleLevelGroupUpdater(ICore pCore, 
                                       ILevelHandler pLevelHdl, 
                                       IGroupHandler pGroupHander,                   
                                       ISingleLevelHandlerFabric singleLevelFabric)
        {
            _pLevelHdl = pLevelHdl;
            _groupHandler = pGroupHander;

            var extraHandler = singleLevelFabric.SingleLevelHandler();
            _pRTHandler = extraHandler.ReactionTimeHandler();

            _nsUpdater = new NextStepGroupUpdater(pCore, pGroupHander);             
        }

        public void SetSignals(UpdateSignals signals)
        {
            if (_nsUpdater != null)
                _nsUpdater.SetSignals(signals);
        }

        public bool ReadyToUpdate(Domain.Agent pAgent, int index, UpdateSignals uSignals, IThreatHandler tHandler)
        {            
            if (pAgent.IsZero())
                LogWriter.Instance.WriteToLog("ReadyToUpdate, Agent location is ZERO!");

            if (pAgent.ePhase == EvacuationPhase.Initial && pAgent.CurrentLevel == null)
            {
                pAgent.CurrentLevel = _pLevelHdl.Find(pAgent.LevelId);

                if (pAgent.CurrentLevel == null)
                {
                    LogWriter.Instance.WriteToLog("Impossible to determine agent's current level....quit");
                    pAgent.Active = false;
                    return false;
                    //var agentObjectSize = SizeCalculator.SizeOf<Domain.Agent>();
                }
            }
            //Determine current location with respect to room index
            var currentRoomID = pAgent.CurrentLevel.GetRoomID(pAgent.X, pAgent.Y);

            if (currentRoomID == -1)
            {
                var roomGates = pAgent.CurrentLevel.GetGatesByRoomID(pAgent.CurrentRoomID);
                var closestGateIndex = Utils.ClosestGateIndex(pAgent.X, pAgent.Y, roomGates);

                //update gate shares
                uSignals.SendSignal(
                    CoreEventType.GateSharesUpdate,
                    new GateSharesUpdateData(pAgent.CurrentLevel.LevelId, roomGates[closestGateIndex].ElementId)
                );

                pAgent.LeaveEnvironment();
                return false;
            }

            switch (pAgent.ePhase)
            {
                case EvacuationPhase.Initial:
                    {
                        pAgent.OnDestinationLevel = true;
                        pAgent.EvacData.GateType = InnerType.EvacuationGate;
                        pAgent.EnvLocation = Location.MovingInsideRoom;

                        pAgent.InitialRoomID = currentRoomID;
                        pAgent.CurrentRoomID = currentRoomID;
                        uint areaID = 0;

                        if (pAgent.Type == AgentType.Follower)
                        {
                            //check if evacuation path was updated by the leader
                            if (!_groupHandler.EvacuationPathIsReady(
                                Convert.ToInt32(pAgent.GroupID_C), pAgent.CurrentLevel.LevelId))
                            {
                                return false;
                            }

                            pAgent.EvacData.Path.AddRange(
                                _groupHandler.EvacPath(
                                    Convert.ToInt32(pAgent.GroupID_C),
                                    pAgent.CurrentLevel.LevelId)
                            );
                            pAgent.EvacData.DestinationRoomID = pAgent.EvacData.Path.Last();
                        }
                        else
                        {
                            areaID = pAgent.CurrentLevel.GetGridAreaId(currentRoomID);

                            var evacuationPathIsFound =
                                pAgent.UseDesignatedGate ?
                                    EvacuationHandler.GetPath<EvacPathToFixedGate>(pAgent, areaID, tHandler) :
                                    EvacuationHandler.GetPath<GeneralPath>(pAgent, areaID, tHandler);

                            if (!evacuationPathIsFound)
                                return false;                            

                            if (pAgent.EvacData.Path.Count == 0)
                            {
                                pAgent.Active = pAgent.HasToWait;
                                return false;
                            }
                        }

                        //send evac path updated msg
                        if (pAgent.Type == AgentType.Leader)                       
                            SendEvacPathUpdateMsg(pAgent, uSignals); 

                        var setGateImmediately = false;

                        if (!pAgent.ReactionTimeRequired)
                        {
                            setGateImmediately = true;
                        }
                        else
                        {
                            if (pAgent.Type != AgentType.Follower)
                            {
                                var evacuationPathSize = pAgent.EvacData.Path.Count;

                                var gates =
                                (evacuationPathSize == 1) ?
                                    pAgent.CurrentLevel.GetDestinationGates(currentRoomID) :
                                    pAgent.CurrentLevel.GetCommonGates(currentRoomID, pAgent.EvacData.Path[1]);

                                var agentReactionTimeData = _pRTHandler.ReactionTime(pAgent.X, pAgent.Y, pAgent.AgentId, gates);
                                uSignals.SendSignal(CoreEventType.ReactionTimeData, agentReactionTimeData);

                                pAgent.ReactionTime = (int)(agentReactionTimeData.ReactionTime / Params.Current.TimeStep);

                                if (pAgent.ReactionTime == 0)
                                    setGateImmediately = true;
                                else
                                    pAgent.ePhase = EvacuationPhase.Reaction;
                            }
                            else
                            {
                                if (_groupHandler.IsGateSet(
                                    Convert.ToInt32(pAgent.GroupID_C), pAgent.CurrentLevel.LevelId, currentRoomID)
                                    )
                                {
                                    var evacGate =
                                        _groupHandler.Gate(
                                            Convert.ToInt32(pAgent.GroupID_C),
                                            pAgent.CurrentLevel.LevelId, currentRoomID);

                                    _nsUpdater.SetRoomDataForAgent(pAgent, currentRoomID);
                                    _nsUpdater.SetGate(index, areaID, new List<RoomElement> { evacGate });

                                    pAgent.VisitedRooms.Add(pAgent.CurrentRoomID, pAgent.EvacIndex);
                                    pAgent.ePhase = EvacuationPhase.NextSteps;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }                            

                        if (setGateImmediately)
                        {
                            if (pAgent.Type != AgentType.Follower)
                            {
                                _nsUpdater.SetRoomDataForAgent(pAgent, currentRoomID);
                                _nsUpdater.ProcessUpdatedEvacuationPath(pAgent, tHandler, areaID, index);
                            }
                            else
                            {
                                if (_groupHandler.IsGateSet(
                                    Convert.ToInt32(pAgent.GroupID_C), pAgent.CurrentLevel.LevelId, currentRoomID)
                                    )
                                {
                                    var evacGate =
                                        _groupHandler.Gate(
                                            Convert.ToInt32(pAgent.GroupID_C),
                                            pAgent.CurrentLevel.LevelId, currentRoomID);

                                    _nsUpdater.SetRoomDataForAgent(pAgent, currentRoomID);
                                    _nsUpdater.SetGate(index, areaID, new List<RoomElement> { evacGate });
                                    pAgent.VisitedRooms.Add(pAgent.CurrentRoomID, pAgent.EvacIndex);
                                }
                                else
                                {
                                    return false;
                                }
                            }
                           
                            pAgent.ePhase = EvacuationPhase.NextSteps;
                        }
                    }
                    break;

                case EvacuationPhase.Reaction:                    
                    if (++pAgent.NCycles >= pAgent.ReactionTime)
                    {
                        var areaID = pAgent.CurrentLevel.GetGridAreaId(currentRoomID);

                        _nsUpdater.SetRoomDataForAgent(pAgent, currentRoomID);
                        _nsUpdater.ProcessUpdatedEvacuationPath(pAgent, tHandler, areaID, index);

                        pAgent.ePhase = EvacuationPhase.NextSteps;
                        break;
                    }
                    else
                    {
                        return false;
                    } 

                case EvacuationPhase.WatchForLeader:
                case EvacuationPhase.NextSteps:
                    {                        
                        _nsUpdater.CheckLocation(index, currentRoomID, pAgent, uSignals, tHandler);
                    }
                    break;

                case EvacuationPhase.MakeNewGateDecision:
                    var roomGates = new List<RoomElement>();
                    roomGates.AddRange(pAgent.CurrentLevel.GetDecisionUpdateGates(pAgent));
                    roomGates.RemoveAll(pGate => pGate.ElementId == pAgent.CurrentGateId);

                    //TODO: Tim, have a look and add ThreatHandling logic
                    var AreaID = pAgent.CurrentLevel.GetGridAreaId(currentRoomID);
                    _nsUpdater.SetGate(index, AreaID, roomGates);
                    pAgent.ePhase = EvacuationPhase.NextSteps;
                    break;
            }
            //}
            /*
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e.ToString());
                    LogWriter.Instance.WriteToLog(e.ToString());
                    throw;
                }*/

            // Sets the next desired point to be the next closest point on the path
            return !pAgent.EvacData.UpdatingPaths && !pAgent.HasToWait && pAgent.UpdatePath();
        }

        public uint GetGridAreaId(Domain.Agent pAgent)
        {
            return pAgent.CurrentLevel.GetGridAreaId(pAgent.CurrentRoomID);
        }

        private void SendEvacPathUpdateMsg(Domain.Agent pAgent, UpdateSignals signals)
        {
            //send [evac path found] signal
            signals.SendSignal(
                CoreEventType.GroupDataUpdate,
                new EvacuationPathUpdate(
                    Convert.ToInt32(pAgent.GroupID_C), pAgent.CurrentLevel.LevelId, pAgent.EvacData.Path)
            );
        }
    }
}
