using System;
using System.Threading;
using Core.Logger;
using Core.Threat;
using Core.PositionUpdater;
using Core.PositionUpdater.SingleLevel;
using Core.PositionUpdater.HandlerFabric;
using Core.PositionUpdater.MultiLevel;
using Core.ForceModel;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using Domain;

namespace Core
{
    internal partial class Core
    {
        internal class SimpleWorkerThread : IWorkerThread
        {
            private int _width = -1;
            private int _height = -1;

            private IPositionUpdater _positionUpdater = null;
            private IThreatHandler _threatHandler = new ThreatHandler();

            private Thread _workerThread = null;
            private readonly Core _core = null;
            private bool _isOn = true;

            private IForceModel _forceModel = null;

#if DEBUG_PERF
            private Stopwatch watch;
#endif
            public SimpleWorkerThread(Core pParentAnalyser, IForceModel model)
            {
                _core = pParentAnalyser;
                _forceModel = model;
            }

            public bool IsOn
            {
                get { return _isOn; }
                set { _isOn = value; }
            }

            public void Join() { _workerThread.Join(); _threatHandler.ClearThreatCollection(); }

            public void SetThreadParams(ThreadParams threadParams)
            {
                _positionUpdater = GenerateUpdater(threadParams.MType);

                _workerThread = new Thread(new ParameterizedThreadStart(CalculateNextStep));
                _workerThread.Start(threadParams);
            }

            private IPositionUpdater GenerateUpdater(ModelType type)
            {
                if (type == ModelType.SingleLevel)
                    return new SingleLevelUpdater(
                        _core, _core._levelHandler,
                            new SimpleSingleLevelHanderFabric());

                return new MultiLevelUpdater(
                    _core, _core._levelHandler,
                    _core._stairWayHandler, new SimpleMultiLevelHandlerFabric()
                );
            }

            private void CalculateNextStep(object threadData)
            {
#if TRYCATCH
                try
                {
#endif
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


#if DEBUG_PERF
                        watch = Stopwatch.StartNew();
#endif

                        //Send signal to GripUpdaterThread
                        _core.workerToUpdaterEvents[threadParams.Id].Set();

                        //Wait till it responds
                        _core.gridUpdaterToWorkerEvents[threadParams.Id].WaitOne();

#if DEBUG_PERF
                        Helper.MethodTimer.AddTime("gridUpdaterToWorkerEvents", watch);
                        watch = Stopwatch.StartNew();
#endif

                        for (var agentIndex = beginRange - 1; agentIndex < endRange; ++agentIndex)
                            if (!agents[agentIndex].Active)
                                Interlocked.Increment(ref _core._nInactiveAgents);

#if DEBUG_PERF
                        Helper.MethodTimer.AddTime("Count _nInactiveAgents", watch);
                        //watch = Stopwatch.StartNew();
#endif

                        if (globalSimTime % 10 == 0)
                        {
                            for (var agentIndex = beginRange - 1; agentIndex < endRange; ++agentIndex)
                                if (agents[agentIndex].Active)
                                    RecalculateNeighborsGrid(agents[agentIndex]);
                        }
                        else if(globalSimTime % 1 == 0)
                        {
                            for (var agentIndex = beginRange - 1; agentIndex < endRange; ++agentIndex)
                                if (agents[agentIndex].Active)
                                    UpdateNeighborsValues(agents[agentIndex]);
                        }

#if DEBUG_PERF
                        //Helper.MethodTimer.AddTime("SetNeighborsGrid", watch);
                        watch = Stopwatch.StartNew();
#endif

                        for (var anAgent = beginRange - 1; anAgent < endRange; ++anAgent)
                        {
                            if (!agents[anAgent].Active)
                                continue;
#if DEBUG_PERF
                            watch = Stopwatch.StartNew();
#endif
                            if (!_positionUpdater.ReadyToUpdate(agents, anAgent, updateSignals, _threatHandler))
                                continue;

                            if (agents[anAgent].HasToWait)
                                continue;
#if DEBUG_PERF
                            Helper.MethodTimer.AddTime("ReadyToUpdate", watch);
                            watch = Stopwatch.StartNew();
#endif
                            //Apply Force Model to calculate next possition
                            _forceModel.Apply(agents[anAgent]);
#if DEBUG_PERF
                            Helper.MethodTimer.AddTime("ForceModel", watch);
#endif

                            ++agents[anAgent].NCycles;
                        } // end of for loop

#if DEBUG_PERF
                        watch = Stopwatch.StartNew();
#endif
                        //main process         
                        _threatHandler.SetDefaultState();

                        _core.workerToMainEvents[threadParams.Id].Set();
                        _core.mainToWorkerThreadsEvents[threadParams.Id].WaitOne();
#if DEBUG_PERF
                        Helper.MethodTimer.AddTime("mainToWorkerThreadsEvents", watch);
#endif
                    }
#if TRYCATCH
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                    LogWriter.Instance.WriteToLog(e.ToString());
                }
#endif
            }

            private void UpdateNeighborsValues(Agent agent)
            {
#if DEBUG_PERF
                watch = Stopwatch.StartNew();
#endif
                List<Neighbor> newNbrs = new List<Neighbor>();

                foreach (var nbr in agent.Nbrs)
                    newNbrs.Add(new Neighbor(_core._agents[nbr.agentIndex]));

#if DEBUG_PERF
                Helper.MethodTimer.AddTime("UpdateNeighborsValues", watch);
#endif
            }

            private void RecalculateNeighborsGrid(Domain.Agent agent)
            {
#if DEBUG_PERF
                watch = Stopwatch.StartNew();
#endif
                agent.Nbrs.Clear();

                int x = (int)Math.Round(agent.XTmp);
                int y = (int)Math.Round(agent.YTmp);

                for (int xpos = x - 2; xpos < x + 2; xpos++)
                {
                    for (int ypos = y - 2; ypos < y + 2; ypos++)
                    {
                        if (OutsideGrid(xpos, ypos) || _core._agentsGrid[xpos, ypos] == null)
                            continue;

                        foreach (var testAgent in _core._agentsGrid[xpos, ypos])
                        {
                            if (!testAgent.Active ||
                                testAgent.TempLevelId != agent.TempLevelId ||
                                testAgent.EnvLocation != agent.EnvLocation ||
                                testAgent.AgentId == agent.AgentId) continue;

                            if (Utils.AgentIsNeighbor(agent.XTmp, agent.YTmp, testAgent.XTmp, testAgent.YTmp))
                                agent.Nbrs.Add(new Domain.Neighbor(testAgent));
                        }
                    }
                }
#if DEBUG_PERF
                Helper.MethodTimer.AddTime("RecalculateNeighborsGrid", watch);
#endif
            }

            private bool OutsideGrid(int xpos, int ypos)
            {
                if (xpos >= _width || xpos < 0)
                    return true;
                return ypos >= _height || ypos < 0;
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
                    default:
                        LogWriter.Instance.WriteToLog("Unkown type of [threat_update] signal");
                        break;
                }
            }
        }
    }
}
