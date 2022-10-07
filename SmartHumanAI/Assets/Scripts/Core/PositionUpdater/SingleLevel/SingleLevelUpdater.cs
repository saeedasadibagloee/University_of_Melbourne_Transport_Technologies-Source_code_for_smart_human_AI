using System;
using Core.GateChoice;
using Core.Handlers;
using Core.Logger;
using Core.Signals;
using Core.Threat;
using Domain.Elements;

using System.Collections.Generic;
using Domain;
using Core.PositionUpdater.NextSteps;
using Core.PositionUpdater.ReactionTimeHandler;
using Core.PositionUpdater.EvacuationPathHandler;
using Core.PositionUpdater.HandlerFabric;
using UnityEngine;

namespace Core.PositionUpdater.SingleLevel
{
    internal class SingleLevelUpdater : IPositionUpdater
    {
        private readonly NextStepUpdater _nsUpdater = null;

        private readonly ILevelHandler _pLevelHdl = null;
        private readonly IReactionTimeHandler _pRTHandler = null;

        public SingleLevelUpdater(ICore pCore, ILevelHandler pLevelHdl, ISingleLevelHandlerFabric singleLevelFabric)
        {
            _nsUpdater = new NextStepUpdater(pCore);
            _pLevelHdl = pLevelHdl;

            var extraHandler = singleLevelFabric.SingleLevelHandler();
            _pRTHandler = extraHandler.ReactionTimeHandler();
        }

        public bool ReadyToUpdate(List<Agent> agents, int index, UpdateSignals uSignals, IThreatHandler tHandler)
        {
            Agent pAgent = agents[index];

#if TRYCATCH
            try
            {
#endif
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
                }
            }
            //Determine current location with respect to room index
            var currentRoomID = pAgent.CurrentLevel.GetRoomID(pAgent.X, pAgent.Y);

            if (currentRoomID == -1)
            {
                List<RoomElement> roomGates = pAgent.CurrentLevel.GetGatesByRoomID(pAgent.CurrentRoomID);

                if (roomGates != null)
                {
                    int closestGateIndex = Utils.ClosestGateIndex(pAgent.X, pAgent.Y, roomGates);

                    //update gate shares
                    uSignals.SendSignal(
                        CoreEventType.GateSharesUpdate,
                        new GateSharesUpdateData(pAgent.CurrentLevel.LevelId, roomGates[closestGateIndex].ElementId)
                    );
                }

                pAgent.LeaveEnvironment();
                return false;
            }

            switch (pAgent.ePhase)
            {
                case EvacuationPhase.Initial:
                    pAgent.OnDestinationLevel = true;
                    pAgent.EvacData.GateType = InnerType.EvacuationGate;
                    pAgent.EnvLocation = Location.MovingInsideRoom;

                    pAgent.InitialRoomID = currentRoomID;
                    pAgent.CurrentRoomID = currentRoomID;

                    var areaID = pAgent.CurrentLevel.GetGridAreaId(currentRoomID);

                    var evacuationPathIsFound =
                        pAgent.UseDesignatedGate ?
                            EvacuationHandler.GetPath<EvacPathToFixedGate>(pAgent, areaID, tHandler) :
                            EvacuationHandler.GetPath<GeneralPath>(pAgent, areaID, tHandler);

                    if (!evacuationPathIsFound)
                        return false;

                    var evacuationPathSize = pAgent.EvacData.Path.Count;

                    if (evacuationPathSize == 0)
                    {
                        pAgent.Active = pAgent.HasToWait;
                        return false;
                    }

                    var setGateImmediately = false;

                    if (!pAgent.ReactionTimeRequired)
                    {
                        setGateImmediately = true;
                    }
                    else
                    {
                        var gates =
                        evacuationPathSize == 1 ?
                            pAgent.CurrentLevel.GetDestinationGates(currentRoomID) :
                            pAgent.CurrentLevel.GetCommonGates(currentRoomID, pAgent.EvacData.Path[1]);

                        var agentReactionTimeData = _pRTHandler.ReactionTime(pAgent.X, pAgent.Y, pAgent.AgentId, gates);
                        uSignals.SendSignal(CoreEventType.ReactionTimeData, agentReactionTimeData);
                        
                        //reaction time [in cycles]
                        pAgent.ReactionTime = (int)(agentReactionTimeData.ReactionTime / Params.Current.TimeStep);
                       
                        if (pAgent.ReactionTime == 0)
                            setGateImmediately = true;
                        else
                            pAgent.ePhase = EvacuationPhase.Reaction;
                    }

                    if (setGateImmediately)
                    {
                        _nsUpdater.SetRoomDataForAgent(pAgent, currentRoomID);
                        _nsUpdater.ProcessUpdatedEvacuationPath(pAgent, tHandler, areaID, index);
                        pAgent.ePhase = EvacuationPhase.NextSteps;
                    }
                    break;

                case EvacuationPhase.Reaction:
                    if (++pAgent.NCycles >= pAgent.ReactionTime)
                    {
                        var areaID2 = pAgent.CurrentLevel.GetGridAreaId(currentRoomID);

                        _nsUpdater.SetRoomDataForAgent(pAgent, currentRoomID);
                        _nsUpdater.ProcessUpdatedEvacuationPath(pAgent, tHandler, areaID2, index);

                        pAgent.ePhase = EvacuationPhase.NextSteps;
                        break;
                    }
                    else
                    {
                        return false;
                    }

                case EvacuationPhase.NextSteps:
                    _nsUpdater.CheckLocation(index, currentRoomID, pAgent, uSignals, tHandler);
                    break;

                // Only happens with decision update.
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

#if TRYCATCH
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                LogWriter.Instance.WriteToLog(e.ToString());
            }
#endif

            // Sets the next desired point to be the next closest point on the path
            return !pAgent.EvacData.UpdatingPaths && !pAgent.HasToWait && pAgent.UpdatePath();
        }

        public uint GetGridAreaId(Agent pAgent)
        {
            return pAgent.CurrentLevel.GetGridAreaId(pAgent.CurrentRoomID);
        }
    }
}
