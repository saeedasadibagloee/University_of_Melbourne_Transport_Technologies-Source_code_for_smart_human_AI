using System.Threading;
using Core.Logger;
using Core.Threat;
using Core.PositionUpdater;

using Core.GroupBehaviour;
using Core.GroupBehaviour.DecisionUpdate;
using System.Collections.Generic;
using Core.PositionUpdater.SingleLevel;
using Core.PositionUpdater.HandlerFabric;
using Core.PositionUpdater.MultiLevel;
using Core.ForceModel;
using Core.GroupBehaviour.SimpleGroup;
using System;
using UnityEngine;

namespace Core
{
    internal partial class Core
    {
        /// <summary>
        /// Worker thread to handle group behaviour 
        /// </summary>
        internal class GroupBehaviourThread : IWorkerThread
        {
            private int _width = -1;
            private int _height = -1;

            private IGroupPositionUpdater _positionUpdater = null;
            private IThreatHandler _threatHandler = new ThreatHandler();
            private IGroupHandler _groupHandler = null;

            private Thread _workerThread = null;
            private readonly Core _core = null;
            private bool _isOn = true;

            private IForceModel _forceModel = null;

            public GroupBehaviourThread(Core pParentAnalyser, IForceModel model)
            {
                _core = pParentAnalyser;
                _forceModel = model;
            }

            public bool IsOn
            {
                get { return _isOn; }
                set { _isOn = value; }
            }

            public void Join()
            {
                _workerThread.Join();
                _threatHandler.ClearThreatCollection();
                _groupHandler.Clear();
            }

            public void SetThreadParams(ThreadParams threadParams)
            {
                if (threadParams.ObjectData != null)
                    _groupHandler = new GroupHandler((GroupHandler)threadParams.ObjectData);

                LogWriter.Instance.WriteToLog("_groupHandler is set");

                _positionUpdater = GenerateUpdater(threadParams.MType);
                _positionUpdater.SetSignals(threadParams.USignals);

                _workerThread = new Thread(new ParameterizedThreadStart(CalculateNextStep));
                _workerThread.Start(threadParams);
            }

            private IGroupPositionUpdater GenerateUpdater(ModelType type)
            {
                if (type == ModelType.SingleLevel)
                    return new SingleLevelGroupUpdater(
                        _core, _core._levelHandler, _groupHandler, new SimpleSingleLevelHanderFabric());

                return new MultiLevelGroupUpdater(
                    _core, _core._levelHandler, _core._stairWayHandler, 
                    _groupHandler, new SimpleMultiLevelHandlerFabric()
                );
            }

            private void CalculateNextStep(object threadData)
            {       
                ThreadParams threadParams = threadData as ThreadParams;
                var nThreadLocalTotalAgents = Interlocked.Read(ref _core._nTotalAgents);

                var divider = (int)nThreadLocalTotalAgents / Constants.NCores;
                var beginRange = divider * threadParams.Id + 1;
                var endRange = divider * (threadParams.Id + 1);

                if (threadParams.Id == Constants.NCores - 1)
                    endRange = (int)nThreadLocalTotalAgents;

                var updateSignals = threadParams.USignals;              

                var agents = _core._agents;
                
                _width = threadParams.width;
                _height = threadParams.height;

                while (Interlocked.Read(ref threadParams._IsOn) == 1)
                {
                    var nAgents = Interlocked.Read(ref _core._nTotalAgents);

                    if (nAgents != nThreadLocalTotalAgents)
                    {
                        nThreadLocalTotalAgents = nAgents;

                        divider = (int)nThreadLocalTotalAgents / Constants.NCores;
                        beginRange = divider * threadParams.Id + 1;
                        endRange = divider * (threadParams.Id + 1);

                        if (threadParams.Id == Constants.NCores - 1)
                            endRange = (int)nThreadLocalTotalAgents;
                    }

                    //check if new threats appeared in the system
                    _threatHandler.ThreatCollectionIsChanged();

                    //check if new threats appeared in the system
                    _threatHandler.ThreatCollectionIsChanged();
                  
                    //Send signal to GripUpdaterThread
                    _core.workerToUpdaterEvents[threadParams.Id].Set();

                    //Wait till it responds
                    _core.gridUpdaterToWorkerEvents[threadParams.Id].WaitOne();

                    if (globalSimTime % 10 == 0)
                    {
                        for (var anAgent = beginRange - 1; anAgent < endRange; ++anAgent)
                            RecalculateNeighborsGrid(agents[anAgent]);
                    }
                    else
                    {
                        for (var anAgent = beginRange - 1; anAgent < endRange; ++anAgent)
                            UpdateNeighborsValues(agents[anAgent]);
                    }

                    for (var anAgent = beginRange - 1; anAgent < endRange; ++anAgent)
                    {
                        if (!agents[anAgent].Active)
                            continue;

                        if (!_positionUpdater.ReadyToUpdate(agents[anAgent], anAgent, updateSignals, _threatHandler))
                            continue;

                        if (agents[anAgent].HasToWait)
                            continue;

                        //Apply Force Model to calculate next possition
                        _forceModel.Apply(agents[anAgent]);

                        ++agents[anAgent].NCycles;
                    } // end of for loop

                    //main process         
                    _threatHandler.SetDefaultState();

                    _core.workerToMainEvents[threadParams.Id].Set();
                    _core.mainToWorkerThreadsEvents[threadParams.Id].WaitOne();
                }               
            }

            private void UpdateNeighborsValues(Domain.Agent agent)
            {
                if (agent.Active)
                {
                    List<Domain.Neighbor> neighbors = new List<Domain.Neighbor>();
                    foreach (var nbs in agent.Nbrs)
                        neighbors.Add(nbs);

                    agent.Nbrs.Clear();

                    foreach (var nbr in neighbors)
                    {
                        if (nbr.agentIndex < _core._agents.Count - 1)
                            agent.Nbrs.Add(new Domain.Neighbor(_core._agents[nbr.agentIndex]));
                        else
                            agent.Nbrs.Add(nbr);
                    }
                }
            }

            private void RecalculateNeighborsGrid(Domain.Agent agent)
            {

                agent.Nbrs.Clear();

                if (agent.Active)
                {
                    int x = (int)Math.Round(agent.XTmp);
                    int y = (int)Math.Round(agent.YTmp);

                    for (int xpos = x - 2; xpos < x + 2; xpos++)
                    {
                        for (int ypos = y - 2; ypos < y + 2; ypos++)
                        {
                            if (OutsideGrid(xpos, ypos) || _core._agentsGrid[xpos, ypos] == null)
                                continue;

                            foreach (var testAgent in _core._agentsGrid[xpos, ypos])
                                ProcessAndAddNearbyAgent(agent, testAgent);
                        }
                    }
                }
                else
                {
                    Interlocked.Increment(ref _core._nInactiveAgents);
                    agent.RVORemove();
                }
            }

            private bool OutsideGrid(int xpos, int ypos)
            {
                if (xpos >= _width || xpos < 0)
                    return true;
                return ypos >= _height || ypos < 0;
            }

            private void ProcessAndAddNearbyAgent(Domain.Agent agent, Domain.Agent testAgent)
            {
                if (!testAgent.Active ||
                    testAgent.TempLevelId != agent.TempLevelId ||
                    testAgent.EnvLocation != agent.EnvLocation ||
                    testAgent.AgentId == agent.AgentId) return;

                if (Utils.AgentIsNeighbor(agent.XTmp, agent.YTmp, testAgent.XTmp, testAgent.YTmp))
                    agent.Nbrs.Add(new Domain.Neighbor(testAgent));

                return;
            }

            public void HandleUpdateSignal(object[] parameters)

            {
                switch ((int)parameters[0])
                {
                    case (int)ThreadSignal.EnableThreat:
                        {
                            _threatHandler.AddNewThreat(parameters[1] as GeneralThreatData);
                            break;
                        }

                    case (int)ThreadSignal.DisableThreat:
                        {
                            _threatHandler.RemoveThreat(parameters[1] as GeneralThreatData);
                            break;
                        }
                    case (int)ThreadSignal.GroupUpdate:
                        {
                            var groupUpdateData = parameters[1] as GroupUpdateData;
                            var groupUpdateSignalType = groupUpdateData.UpdateType();

                            switch (groupUpdateSignalType)
                            {
                                case GroupUpdateType.EvacPathIsFound:
                                    {
                                        _groupHandler.UpdateEvacPath(
                                           groupUpdateData.GetGroupID,
                                           groupUpdateData.LevelID,
                                           (List<int>)groupUpdateData.UpdateInfo()
                                        );
                                    }
                                    break;

                                case GroupUpdateType.GateIsSet:
                                    {
                                        var gateUpdateInfo = (RoomGateUpdate)groupUpdateData.UpdateInfo();

                                        _groupHandler.UpdateGateInfo(
                                            groupUpdateData.GetGroupID,
                                            groupUpdateData.LevelID,
                                            gateUpdateInfo.RoomID,
                                            gateUpdateInfo.Gate
                                        );

                                        LogWriter.Instance.WriteToLog("ThreadID: " + Thread.CurrentThread.ManagedThreadId.ToString());
                                    }
                                    break;

                                case GroupUpdateType.TargetLevelID:
                                    _groupHandler.SetEvacuationLevelID(
                                        groupUpdateData.GetGroupID, groupUpdateData.LevelID
                                    );
                                    break;
                            }

                            break;
                        }

                    case (int)ThreadSignal.NewGroup:
                        {
                            var newGroup = parameters[1] as GroupData;
                            _groupHandler.AddNewGroup(newGroup);
                            break;
                        }

                    default:
                        LogWriter.Instance.WriteToLog("Unkown type of [threat_update] signal");
                        break;
                }
            }  
        }
    }
}
