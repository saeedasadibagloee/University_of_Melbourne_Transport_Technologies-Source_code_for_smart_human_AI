using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Assets.Scripts.DataFormats;
using Core.GateChoice;
using Core.Handlers;
using Core.Logger;
using Core.Signals;
using Core.Threat;
using Core.Validator;
using DataFormats;
using Domain;
using Domain.Distribution;
using Domain.Elements;
using Domain.Level;
using Domain.Stairway;
using eDriven.Core.Signals;
using Helper;
using Pathfinding;
using UnityEngine;
using Pathfinding.RVO;
using Core.PositionUpdater.ReactionTimeHandler;
using Core.GroupBehaviour.DecisionUpdate;
using Core.GroupBehaviour;
using Core.GroupBehaviour.SimpleGroup;
using Core.ForceModel;
using Core.Handlers.DynamicAgentGenerationHandler;
using Debug = UnityEngine.Debug;
using Gate = Domain.Elements.Gate;
using System.Diagnostics;

// ReSharper disable PossibleInvalidCastExceptionInForeachLoop

namespace Core
{
    
            //define model type
            _modelType =
                pModel.levels.Count == 1 ?
                    ModelType.SingleLevel : ModelType.MultiLevel;

            //Create corresponding Position Updater
            //_pUpdater = GenerateUpdater(pModel.levels.Count);

            _stairWayHandler.SetStairGridAreaMap(
                CreateStairAndGridAreaMap(_stairWayHandler.Stairs())
            );

            SaveDynamicDistributions();

            for (int i = 0; i < Constants.NCores; ++i)
            {
                var newWorkerThread = GetProperThread();

                Signal newThreatUpdateSignal = new Signal();
                newThreatUpdateSignal.Connect(newWorkerThread.HandleUpdateSignal);
                _coreUpdateSignals.Add(newThreatUpdateSignal);

                _workerThreads.Add(newWorkerThread);
            }

            //check for any threats
            foreach (Info.Threat threat in pModel.threats)
                ProcessNewThreat(threat);

            foreach (var level in _levelHandler.Levels())
                level.AssignWaitPointsToRooms();

            MaxPathfindingThisSimulation = Constants.PathfindingUpdateCycle;

#if DEBUG_PERF
            MethodTimer.AddTime("Initialize", s_Initialize);
#endif

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

        public void Execute()
        {
#if TRYCATCH
            try
            {
#endif
            mainToWorkerThreadsEvents = new AutoResetEvent[Constants.NCores];
            workerToMainEvents = new AutoResetEvent[Constants.NCores];

            workerToUpdaterEvents = new AutoResetEvent[Constants.NCores];
            gridUpdaterToWorkerEvents = new AutoResetEvent[Constants.NCores];

            for (var index = 0; index < Constants.NCores; ++index)
            {
                mainToWorkerThreadsEvents[index] = new AutoResetEvent(false);
                workerToMainEvents[index] = new AutoResetEvent(false);

                workerToUpdaterEvents[index] = new AutoResetEvent(false);
                gridUpdaterToWorkerEvents[index] = new AutoResetEvent(false);
            }

            GenerateAgents();

            _graphNodeChunks = null;

            if (!Consts.InitialPenaltiesCorrect)
                SaveInitialPenalties();

            Constants.PathfindingUpdateCycle = Constants.PathfindingUpdateCycleInitial;
            Params.Current.DecisionUpdateCycle = Params.CurrentDefaults.DecisionUpdateCycle;
            Params.Current.AgentUpdateCycle = (int)(1f / Params.Current.TimeStep);

            UpdateSignals signals = new UpdateSignals(_generalEventSignal);

            //Start grip updater thread
            //Thread gridUpdaterThread = new Thread(delegate() { RefreshAgentGrid(); } );
            gridUpdaterThread = new Thread(() => RefreshAgentGrid());
            gridUpdaterThread.Start();

            for (var i = 0; i < Constants.NCores; ++i)
            {
                var newParams =
                    _groupsShouldBeHandled ?
                    new ThreadParams(i, _modelType, signals, _groupHandler) :
                        new ThreadParams(i, _modelType, signals);

                newParams.width = _width; newParams.height = _height;

                _threadParamsList.Add(newParams);
                _workerThreads[i].SetThreadParams(newParams);
            }

            var nRepeatetiveCycles = 0;
            globalSimTime = 0;

            var simulationIsOn = true;

            //Check if we need to generate agents dynamically
            var allAgentsWereGenerated = _dynamicDistributions.Count == 0;

            if (!allAgentsWereGenerated)
            {
                var groupsMustAlsoBeGenerated =
                    _dynamicDistributions.Any(
                    pair => pair.Value.Any(
                        pDist => pDist.GetDynamicDistributionData() != null
                        && pDist.GetDynamicDistributionData().DymanicGroupData.Count != 0)
                );
                if (groupsMustAlsoBeGenerated)
                    _dynamicAgentGenerator =
                        new ComplexDynamicGenerator(_groupHandler, _updateSignal, _dynamicDistributions);
                else
                    _dynamicAgentGenerator = new SimpleDynamicGenerator(_updateSignal, _dynamicDistributions);
            }

            _nPrevInactiveAgents = 0;
            bool tempThreatsAreGone = false;
            bool agentsAreTrapped = false;

            Params.Current.RVOUpdateCycle = 16;
            if (Params.Current.TimeStep >= 0.005)
            {
                // Aim for around 50 times a second, or as often as possible given larger timesteps.
                Params.Current.RVOUpdateCycle = (int)(1f / Params.Current.TimeStep / 50f);
                if (Params.Current.RVOUpdateCycle < 1)
                    Params.Current.RVOUpdateCycle = 1;
            }

            while (simulationIsOn)
            {
#if DEBUG_PERF
                Stopwatch stopwatch = Stopwatch.StartNew();
#endif
                WaitHandle.WaitAll(workerToMainEvents);
#if DEBUG_PERF
                MethodTimer.AddTime("WaitHandle.WaitAll(Threads)", stopwatch);
                stopwatch = Stopwatch.StartNew();
#endif

                ++_nCycles;
                ++globalSimTime;

                //Handle groups: update worker threads' group handlers
                if (_groupsShouldBeHandled)
                {
                    //Handle new evacuation related updates
                    if (_groupHandler.NewUpdatesExist())
                    {
                        var groupUpdates = _groupHandler.GetUpdatedEntries();

                        for (var uIndex = 0; uIndex < groupUpdates.Count; ++uIndex)
                        {
                            foreach (var coreUpdateSignal in _coreUpdateSignals)
                                coreUpdateSignal.Emit(ThreadSignal.GroupUpdate, groupUpdates[uIndex]);
                        }
                    }

                    //handle new groups
                    if (_groupHandler.NewGroupsWereCreated())
                    {
                        var newGroups = _groupHandler.RecentGroups();
                        for (var gIndex = 0; gIndex < newGroups.Count; ++gIndex)
                        {
                            foreach (var coreUpdateSignal in _coreUpdateSignals)
                                coreUpdateSignal.Emit(ThreadSignal.NewGroup, newGroups[gIndex]);
                        }
                    }
                }

#if DEBUG_PERF
                MethodTimer.AddTime("Handle Groups", stopwatch);
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

                //check if any of the existing threats should be activated
                for (var index = 0; index < _threats.Count; ++index)
                {
                    if (!_threats[index].Active && _nCycles >= _threats[index].StartTime)
                    {
                        _threats[index].Active = true;
                        //activate threat
                        foreach (var coreUpdateSignal in _coreUpdateSignals)
                            coreUpdateSignal.Emit(ThreadSignal.EnableThreat, _threats[index]);

                        //send threat update back to Unity
                        _updateSignal.Emit(Def.Signal.ThreatUpdate,
                            new ThreatUpdate(_threats[index].ThreatId)
                        );
                    }
                    else if (_threats[index].Duration >= 0
                             && _nCycles >= _threats[index].StartTime + _threats[index].Duration)
                    {
                        //delete threat
                        foreach (var coreUpdateSignal in _coreUpdateSignals)
                            coreUpdateSignal.Emit(ThreadSignal.DisableThreat, _threats[index]);

                        //send threat update back to Unity
                        _updateSignal.Emit(Def.Signal.ThreatUpdate,
                            new ThreatUpdate(_threats[index].ThreatId, false)
                        );

                        //remove threat object
                        _threats.RemoveAt(index);
                    }

                    //check if temp threats still remain in the threatlist
                    if (_threats.FindAll(threat => threat.Duration >= 0).ToList().Count == 0)
                        tempThreatsAreGone = true;
                }

                if (_agents.Any(pAgent => pAgent.HasToWait)
                    && _threats.Any(pThreat => pThreat.Duration < 0)
                    && tempThreatsAreGone)
                {
                    if (++nRepeatetiveCycles >= 15000) //15 sec
                    {
                        //agents are permanently trapped
                        agentsAreTrapped = true;
                        break;
                    }
                }

#if DEBUG_PERF
                MethodTimer.AddTime("Handle Threats", stopwatch);
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

                #region GroupSpeedLimiter
                if (_groupsShouldBeHandled && globalSimTime % Params.Current.GroupCheckCycle == 0)
                {
                    var groups = _groupHandler.GroupDataMap();

                    foreach (var group in groups.Values)
                    {
                        var leader =
                            _agents.FirstOrDefault(pAgent => pAgent.AgentId == group.LeaderID);

                        if (leader == null) continue;

                        var lRemainingDistance = leader.RemainingDistance();

                        foreach (var fID in group.Followers)
                        {
                            var follower =
                                _agents.FirstOrDefault(pAgent => pAgent.AgentId == fID);

                            if (follower == null) continue;
                            var fDistance = follower.RemainingDistance();

                            if (leader.Active
                                && lRemainingDistance != -1
                                && fDistance != -1
                                && follower.CurrentGateId == leader.CurrentGateId
                                && fDistance < lRemainingDistance)
                            {
                                if (follower.SavedMaxSpeedValue == 0)
                                    follower.SavedMaxSpeedValue = follower.MaxSpeed;

                                if (follower.MaxSpeed > follower.SavedMaxSpeedValue * Params.Current.AgentFollowerInFrontSpeed)
                                    follower.MaxSpeed = follower.MaxSpeed * Params.Current.AgentFollowerInFrontSpeed;
                            }
                            else
                            {
                                if (follower.SavedMaxSpeedValue != 0)
                                    follower.MaxSpeed = follower.SavedMaxSpeedValue;
                            }
                        }
                    }
                }
                #endregion

#if DEBUG_PERF
                MethodTimer.AddTime("Group Speed Limiter", stopwatch);
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

                #region Dynamic Agent Generation               
                //Check if more agents have to be generated
                if (!allAgentsWereGenerated
                    && _dynamicAgentGenerator != null
                    && globalSimTime % Params.Current.AgentUpdateCycle == 0)
                {
                    allAgentsWereGenerated =
                        _dynamicAgentGenerator.AllAgentWereGenerated(_dynamicDistributions);

                    if (!allAgentsWereGenerated)
                    {
                        var splitPercentage = _classChoice.UtilData(1)._split / 100f;
                        //set current nCycle
                        _dynamicAgentGenerator.SetCurrentCycle(_nCycles);
                        //actual generation
                        _dynamicAgentGenerator.GenerateMoreAgents(
                            _agents, splitPercentage,
                            ref _nPrevInactiveAgents, ref _nTotalAgents
                        );
                    }
                }
                #endregion

#if DEBUG_PERF
                MethodTimer.AddTime("Dynamic Agent Gen", stopwatch);
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

                if (globalSimTime % Constants.PenaltyWeightUpdateCycle == 0)
                    UpdatePenaltyWeightsAndRecordHeatmap();


#if DEBUG_PERF
                MethodTimer.AddTime("UPWARH", stopwatch);
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

                // Update GUI
                if (globalSimTime % Params.Current.UiUpdateCycle == 0)
                    UpdateUI();

                foreach (var agent in _agents) agent.UpdateWaitingData(_agents, this);

                if (globalSimTime == 500 || globalSimTime % 800 == 0)
                    QueueFormerAlgorithm();

                UpdateTrainSignallingAgents();

                if (globalSimTime % 200 == 0)
                    UpdateTrainDoorStatus();

                if (!Params.panicMode && globalSimTime % 100 == 0)
                {
                    DoubleCheckIsWLWGR();
                    CategorizeBidirGates();
                }

                if (Params.Current.TicketGateQueuesEnabled && globalSimTime % 50 == 0) CheckForVisibleWaitingGates();

                // Send latest time.
                _updateSignal.Emit(Def.Signal.TimeData, new TimePackage(_nCycles));

#if DEBUG_PERF
                MethodTimer.AddTime("UpdateUI", stopwatch);
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

                // Update RVO Quadtrees if necessary, 62.5 times/s
                if ((globalSimTime < 5 || globalSimTime % Params.Current.RVOUpdateCycle == 0) && !Consts.RVODisabled) RVOSimulator.active.GetSimulator().UpdateSimulation();

#if DEBUG_PERF
                MethodTimer.AddTime("Collision Avoidance", stopwatch);
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

                UpdatePaths(globalSimTime);

#if DEBUG_PERF
                MethodTimer.AddTime("UpdatePaths", stopwatch);
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

                //Provide GUI with Num Agents that have completed.
                if (_nInactiveAgents > 0)
                    SendInactiveAgentsData(_nInactiveAgents + _nPrevInactiveAgents);

                if (Consts.deleteAnAgent > 0)
                {
                    for (int i = 0; i < _agents.Count; i++)
                    {
                        if (_agents[i].AgentId == Consts.deleteAnAgent)
                        {
                            _agents[i].LeaveEnvironment();
                            Consts.deleteAnAgent = -1;
                            break;
                        }
                    }
                }

                if (Interlocked.Read(ref _forceComplete) == 1 || _nInactiveAgents == _nTotalAgents && allAgentsWereGenerated)
                {
                    var totalEvacuationTime = _nCycles * Params.Current.TimeStep;

                    UpdateUI();

                    //Emit Reaction Time Data
                    _updateSignal.Emit(Def.Signal.ReactionTimeData,
                        _reactionTimeDataHandler.Data()
                    );

                    _updateSignal.Emit(Def.Signal.GateSharesData, _gateSharesDataMap);

                    _updateSignal.Emit(Def.Signal.SimulationStatus,
                        new SimulationStatusPackage((int)Def.SimulationState.Finished),
                        totalEvacuationTime
                    );

                    Interlocked.Exchange(ref _processing, 0);
                    LogWriter.Instance.WriteToLog("SIMULATION FINISHED.");
                }

#if DEBUG_PERF
                MethodTimer.AddTime("Check Simulation Complete", stopwatch);
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

                simulationIsOn = Interlocked.Read(ref _processing) == 1;
                _nInactiveAgents = 0;

                if (!simulationIsOn)
                    continue;

                foreach (var mEvent in mainToWorkerThreadsEvents)
                    mEvent.Set();

#if DEBUG_PERF
                MethodTimer.AddTime("Semaphores Release", stopwatch);
#endif
            }

            CleanupEndSimulation();

            //MultiAgentLogger.FlushLog();

            // Handle trapped agents:
            if (!agentsAreTrapped) return;

            TrappedAgentsInfo info = new TrappedAgentsInfo();

            foreach (var agent in _agents)
            {
                if (agent.HasToWait)
                    info.UpdateData(agent.CurrentLevel.LevelId, agent.CurrentRoomID);
            }

            _updateSignal.Emit(Def.Signal.SimulationStatus,
                new SimulationStatusPackage((int)Def.SimulationState.Interrupted), info
            );
#if TRYCATCH
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                LogWriter.Instance.WriteToLog(e.ToString());
            }
#endif
        }


        private void DoubleCheckIsWLWGR()
        {
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                if (!agent.Active) continue;
                agent.IsWLWGR = false;

                try
                {
                    var currentGates = agent.GetCurrentGates();
                    if (currentGates == null) continue;
                    foreach (var element in agent.GetCurrentGates())
                    {
                        var gate = element as Gate;

                        if (gate == null)
                            continue;

                        if (gate.WaitingData != null)
                        {
                            var targetX = gate.WaitingData.targetPosX;
                            var targetY = gate.WaitingData.targetPosY;

                            if (gate.WaitingData.isBidirectional && gate.WaitingData.currentDirection == -1)
                            {
                                targetX = gate.WaitingData.targetPos2X;
                                targetY = gate.WaitingData.targetPos2Y;
                            }

                            float distance = Utils.DistanceBetween(agent.X, agent.Y, targetX, targetY);

                            if (distance < 1.0f)
                            {
                                agent.IsWLWGR = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString()); // ignored
                }

                if (agent.IsWLWGR) continue;

                if (agent.PreviousGate != null && agent.PreviousGate.WaitingData != null)
                {
                    // Just walked through a waiting gate, and within a certain distance.
                    if (Math.Abs(agent.PreviousGate.VMiddle.X - agent.X) < 1.2f &&
                        Math.Abs(agent.PreviousGate.VMiddle.Y - agent.Y) < 1.2f)
                    {
                        agent.IsWLWGR = true;
                        continue;
                    }
                }

                if (agent.CurrentGate != null && agent.CurrentGate.WaitingData != null)
                {
                    // Is the first person in the line at the waiting gate.
                    if (agent.numberInQueue == 0)
                    {
                        agent.IsWLWGR = true;
                    }
                }
            }
        }

        private void CheckForVisibleWaitingGates()
        {
            foreach (var agent in _agents)
            {
                if (!agent.Active || agent.CurrentGateId == -1 ||
                    agent.isInWaitingQueue || agent.hasWaitedAndLeaving)
                    continue;

                var gate = agent.CurrentLevel.FindGate(agent.CurrentGateId) as Gate;

                if (gate == null || gate.WaitingData == null)
                    continue;

                if (gate.WaitingData.agentsInQueue == null ||
                    gate.WaitingData.agentsInQueue.Count < 1 ||
                    !Params.Current.QueueAroundCorners)
                {
                    if (Utils.GateIsVisible(agent.X, agent.Y, gate,
                        agent.Walls, agent.Poles,
                        agent.CurrentLevel.GetAllGates()))
                    {
                        agent.hasWaitedAndLeaving = false;
                        agent.isInWaitingQueue = true;
                    }
                }
                else
                {
                    var lastAgent = _agents[gate.WaitingData.agentsInQueue.Last()];
                    if (Utils.PointIsVisible(agent.X, agent.Y, lastAgent.X, lastAgent.Y,
                        agent.Walls, agent.Poles,
                        agent.CurrentLevel.GetAllGates()))
                    {
                        agent.hasWaitedAndLeaving = false;
                        agent.isInWaitingQueue = true;
                    }
                }
            }
        }

        private void CategorizeBidirGates()
        {
            Dictionary<int, int> unidirGates = new Dictionary<int, int>();
            bidirGates = new Dictionary<int, bool>();

            foreach (var agent in _agents)
            {
                if (!agent.Active || agent.CurrentGateId == -1)
                    continue;

                if (bidirGates.ContainsKey(agent.CurrentGateId))
                    continue;

                RoomElement gate = agent.CurrentLevel.FindGate(agent.CurrentGateId);

                if (agent.EnvLocation == Location.MovingInsideStairway)
                {
                    if (agent.Direction == Direction.Up)
                    {
                        gate = agent.Stair.TopLevelGate();
                    }
                    else if (agent.Direction == Direction.Down)
                    {
                        gate = agent.Stair.BottomLevelGate();
                    }
                }

                if (gate != null)
                {
                    var gatePos = gate.VMiddle;
                    var gateDistance = Utils.DistanceBetween(agent.X, agent.Y, gatePos.X, gatePos.Y);

                    if (gateDistance <= 4f && gateDistance <= gate.Length)
                    {
                        if (unidirGates.ContainsKey(agent.CurrentGateId))
                        {
                            // If we're coming from a different room, the gate is then bidir.
                            if (unidirGates[agent.CurrentGateId] != agent.CurrentRoomID)
                                bidirGates.Add(agent.CurrentGateId, true);
                        }
                        else
                        {
                            unidirGates.Add(agent.CurrentGateId, agent.CurrentRoomID);
                        }
                    }
                }
            }
        }

        private void QueueFormerAlgorithm()
        {
            foreach (var level in _levelHandler.Levels())
            {
                var gateRoomIds = new List<int>();

                // Get gate rooms
                foreach (var gate in level.GetAllGates())
                {
                    Gate gate1 = (Gate)gate;
                    if (gate1.WaitingData == null || gate1.TrainData != null) continue;

                    if (!gate1.WaitingData.isBidirectional)
                    {
                        int roomIndex = level.GetRoomID(gate1.WaitingData.waitPosX, gate1.WaitingData.waitPosY);
                        if (roomIndex != -1 && !gateRoomIds.Contains(roomIndex))
                            gateRoomIds.Add(roomIndex);
                    }
                    else
                    {
                        int roomIndex = -1;
                        switch (gate1.WaitingData.currentDirection)
                        {
                            case 0:
                                roomIndex = level.GetRoomID(gate1.WaitingData.waitPosX, gate1.WaitingData.waitPosY);
                                if (roomIndex != -1 && !gateRoomIds.Contains(roomIndex))
                                    gateRoomIds.Add(roomIndex);
                                roomIndex = level.GetRoomID(gate1.WaitingData.waitPos2X, gate1.WaitingData.waitPos2Y);
                                break;
                            case 1:
                                roomIndex = level.GetRoomID(gate1.WaitingData.waitPosX, gate1.WaitingData.waitPosY);
                                break;
                            case -1:
                                roomIndex = level.GetRoomID(gate1.WaitingData.waitPos2X, gate1.WaitingData.waitPos2Y);
                                break;
                        }
                        if (roomIndex != -1 && !gateRoomIds.Contains(roomIndex))
                            gateRoomIds.Add(roomIndex);
                    }
                }

                // For each set of gates.
                foreach (var roomId in gateRoomIds)
                {
                    var gateActuals = new List<Gate>();
                    var gateLocations = new List<CpmPair>();

                    foreach (var gate in level.GetAllGates())
                    {
                        Gate gate1 = (Gate)gate;
                        if (gate1.WaitingData == null || gate1.TrainData != null) continue;

                        if (!gate1.WaitingData.isBidirectional)
                        {
                            if (level.GetRoomID(gate1.WaitingData.waitPosX, gate1.WaitingData.waitPosY) == roomId)
                            {
                                gateLocations.Add(new CpmPair(gate1.VMiddle));
                                gateActuals.Add(gate1);
                            }
                        }
                        else
                        {
                            switch (gate1.WaitingData.currentDirection)
                            {
                                case 0:
                                    gateLocations.Add(new CpmPair(gate1.VMiddle));
                                    gateActuals.Add(gate1);
                                    break;
                                case 1:
                                    if (level.GetRoomID(gate1.WaitingData.waitPosX, gate1.WaitingData.waitPosY) == roomId)
                                    {
                                        gateLocations.Add(new CpmPair(gate1.VMiddle));
                                        gateActuals.Add(gate1);
                                    }
                                    break;
                                case -1:
                                    if (level.GetRoomID(gate1.WaitingData.waitPos2X, gate1.WaitingData.waitPos2Y) == roomId)
                                    {
                                        gateLocations.Add(new CpmPair(gate1.VMiddle));
                                        gateActuals.Add(gate1);
                                    }
                                    break;
                            }
                        }
                    }

                    var agentActuals = new List<Agent>();
                    var agentLocations = new List<CpmPair>();

                    foreach (var agent in _agents)
                    {
                        if (!agent.Active)
                            continue;

                        if (agent.CurrentRoomID != roomId)
                            continue;

                        if (agent.isInWaitingQueue)
                        {
                            agentLocations.Add(new CpmPair(agent.X, agent.Y));
                            agentActuals.Add(agent);
                        }
                    }

                    var roomIds = new List<int>();

                    foreach (var agent in _agents)
                    {
                        if (!roomIds.Contains(agent.CurrentRoomID))
                            roomIds.Add(agent.CurrentRoomID);
                    }

                    var wallsList = new List<LineSeg>();

                    foreach (var wall in level._Rooms[roomId].RoomWalls)
                        wallsList.Add(new LineSeg(new CpmPair(wall.VStart), new CpmPair(wall.VEnd)));

                    var newQueues = QueueFormer.Execute(gateLocations, agentLocations, wallsList);

                    for (int queueIndex = 0; queueIndex < newQueues.Count; queueIndex++)
                    {
                        gateActuals[queueIndex].WaitingData.agentsInQueue.Clear();

                        if (gateActuals[queueIndex].WaitingData.isBidirectional &&
                            gateActuals[queueIndex].WaitingData.currentDirection == 0 && newQueues[queueIndex].Count > 0)
                        {
                            gateActuals[queueIndex].WaitingData.currentDirection =
                            agentActuals[newQueues[queueIndex][0]].CurrentRoomID ==
                            level.GetRoomID(gateActuals[queueIndex].WaitingData.waitPosX,
                                gateActuals[queueIndex].WaitingData.waitPosY)
                                ? 1
                                : -1;
                        }

                        for (int i = 0; i < newQueues[queueIndex].Count; i++)
                        {
                            agentActuals[newQueues[queueIndex][i]].CurrentGateId = gateActuals[queueIndex].ElementId;
                            gateActuals[queueIndex].WaitingData.agentsInQueue.Add(agentActuals[newQueues[queueIndex][i]].AgentIndex);
                        }
                    }
                }

                // Make empty ticketgates bidirectional again if needed
                foreach (var gate in level.GetAllGates())
                {
                    Gate gate1 = (Gate)gate;
                    if (gate1.WaitingData == null || gate1.TrainData != null) continue;

                    if (gate1.WaitingData.isBidirectional && gate1.WaitingData.agentsInQueue.Count == 0)
                    {
                        if (gate1.WaitingData.PreviousAgent == null)
                            gate1.WaitingData.currentDirection = 0;
                        else if (!gate1.WaitingData.PreviousAgent.IsWLWGR)
                            gate1.WaitingData.currentDirection = 0;
                    }

                }
            }
        }

        private void UpdateTrainDoorStatus()
        {
            var previousGatesBlocked = new List<int>();
            previousGatesBlocked.AddRange(trainDoorsToWait);

            trainDoorsToWait.Clear();

            // Find train doors that are blocked, and tell the appropriate agents
            foreach (var agent in _agents)
            {
                if (!agent.Active) continue;
                if (agent.TrainPhase != TrainPhase.Alighting) continue;

                Gate gate = agent.CurrentLevel.FindGate(agent.CurrentGateId) as Gate;
                if (gate == null || gate.TrainData == null)
                {
                    agent.TrainPhase = TrainPhase.None;
                }
                else
                {
                    if (!trainDoorsToWait.Contains(agent.CurrentGateId))
                    {
                        if (Utils.DistanceBetween(agent.X, agent.Y, gate.VMiddle.X, gate.VMiddle.Y) < 5f)
                        {
                            TellBoardingToWait(agent.CurrentGateId);
                            trainDoorsToWait.Add(agent.CurrentGateId);
                        }
                    }

                }
            }

            // For train doors that have been unblocked, tell waitingatdoor agents to move into train.
            foreach (var previousBlockedGate in previousGatesBlocked)
            {
                if (!trainDoorsToWait.Contains(previousBlockedGate))
                {
                    TellWaitingToBoard(previousBlockedGate);
                }
            }
        }

        private void TellWaitingToBoard(int gateId)
        {
            foreach (var agent in _agents)
            {
                if (!agent.Active) continue;
                if (agent.TrainPhase != TrainPhase.WaitingAtDoor) continue;
                if (agent.CurrentGateId != gateId) continue;

                HeadTowardsGate(agent.CurrentLevel.FindGate(agent.CurrentGateId) as Gate, agent);
            }
        }

        private void HeadTowardsGate(Gate gate, Agent agent)
        {
            agent.DesiredGateReal.X = gate.VMiddle.X;
            agent.DesiredGateReal.Y = gate.VMiddle.Y;
            agent.TrainPhase = TrainPhase.Boarding;
            if (agent.CurrentGate.PlatformQ != null)
                agent.CurrentGate.PlatformQ.ClearQueue();
            agent.DesiredToGateReal();
        }

        private void TellBoardingToWait(int gateId)
        {
            foreach (var agent in _agents)
            {
                if (!agent.Active) continue;
                if (agent.TrainPhase == TrainPhase.Waiting) continue;
                if (agent.CurrentGateId != gateId) continue;

                WaitAtDoor(agent.CurrentLevel.FindGate(agent.CurrentGateId) as Gate, agent);
            }
        }

        private void UpdateTrainSignallingAgents()
        {
            foreach (var level in _levelHandler.Levels())
            {
                foreach (var train in level.Trains)
                {
                    if (train.trainDataTimetable != null && train.trainDataTimetable.Count > 0)
                    {
                        foreach (var trainData in train.trainDataTimetable)
                        {
                            if (_nCycles == Mathf.RoundToInt(trainData.arrivalTime / Params.Current.TimeStep))
                            {
                                TrainSignalToBoard(train.destinationGateID);
                            }
                            else if (_nCycles == Mathf.RoundToInt(trainData.departureTime / Params.Current.TimeStep))
                            {
                                TrainSignalToWait(train.destinationGateID);
                            }
                        }
                    }
                }
            }

        }

        private void TrainSignalToWait(int trainDestinationGateId)
        {
            UpdateTrainStatus(trainDestinationGateId, TrainPhase.Waiting);

            for (int index = 0; index < _agents.Count; index++)
            {
                var agent = _agents[index];
                if (!agent.Active) continue;

                // Only notify agents who are boarding this particular train.
                if (agent.DestinationGateId == trainDestinationGateId && agent.HeadingTowardsTrainGate())
                {
                    // Make sure we're on the right level.
                    Train train = agent.CurrentLevel.FindTrain(trainDestinationGateId);
                    if (train == null)
                        continue;

                    CpmPair waitingLocation = GenerateTrainWaitingLocation(train, agent, agent.CurrentGate);
                    agent.DesiredGateReal.X = waitingLocation.X;
                    agent.DesiredGateReal.Y = waitingLocation.Y;
                    agent.DesiredToGateReal();
                    agent.TrainPhase = TrainPhase.Waiting;
                }

                // For agents who haven't yet alighted by the time the train is leaving
                if (agent.HeadingTowardsTrainGate(trainDestinationGateId))
                    agent.LeaveEnvironment();
            }
        }

        private static CpmPair GenerateTrainWaitingLocation(Train train, Agent agent, Gate gate)
        {
            if (gate.TrainData.waitingPositions != null && gate.TrainData.waitingPositions.Count > 1)
            {
                // Use the user defined queue line.
                if (gate.PlatformQ == null) gate.PlatformQ = new PlatformQueueCore(gate.TrainData.waitingPositions);
                return gate.PlatformQ.JoinQueue(agent.AgentId);
            }

            int count = 0;
            while (count < 1000)
            {
                count++;
                var loc = Utils.GenerateLocation(train.waitingAreaVertices, null);
                // Wait near the carriage door chosen
                if (Utils.DistanceBetween(loc.X, loc.Y, agent.DesiredGateReal.X, agent.DesiredGateReal.Y) < 6.5f)
                    return loc;
            }

            return Utils.GenerateLocation(train.waitingAreaVertices, null);
        }

        private void UpdateTrainStatus(int trainID, TrainPhase phase)
        {
            if (!currentTrainPhases.ContainsKey(trainID))
                currentTrainPhases.Add(trainID, phase);
            else
                currentTrainPhases[trainID] = phase;
        }

       

            // Each agent has a recorded density, update the probabilities of updating paths accordingly.
            UpdateRandomWeightsPath();

            for (int i = 0; i < agentsToGenRound; i++)
            {
                float randomCumulation = (float)Utils.Rand.NextDouble() * _updatePathProbabilityWeightTotal;
                float cumulatedWeightSoFar = 0;

                // Pick an agent to update their path.
                foreach (var agent in _agents)
                {
                    if (!agent.Active)
                        continue;

                    if (agent.UpdatePathProbabilityWeight > 0)
                        cumulatedWeightSoFar += agent.UpdatePathProbabilityWeight;

                    if (!(cumulatedWeightSoFar > randomCumulation)) continue;

                    Path p = agent.P;

                    if (p != null && !p.IsDone())
                    {
                        // Increase the pathfinding update cycle, as obviously the pathfinding is too slow (big map)
                        Constants.PathfindingUpdateCycle = (int)(Constants.PathfindingUpdateCycle * 1.1f);

                        if (Constants.PathfindingUpdateCycle > 5 * Constants.PathfindingUpdateCycleInitial)
                            Constants.PathfindingUpdateCycle = 5 * Constants.PathfindingUpdateCycleInitial;

                        if (MaxPathfindingThisSimulation < Constants.PathfindingUpdateCycle)
                        {
                            MaxPathfindingThisSimulation = Constants.PathfindingUpdateCycle;
                            LogWriter.Instance.WriteToLog(PathfindingCycleIncreaseNotification + Constants.PathfindingUpdateCycle);
                        }
                    }
                    else
                    {
                        agent.NewPath();
                    }

                    break;
                }
            }
        }

        private void UpdateRandomWeightsPath()
        {
            _updatePathProbabilityWeightTotal = 0;

            foreach (Agent agent in _agents)
            {
                agent.CouldPathUpdate = false;

                if (!agent.Active)
                    continue;

                agent.UpdatePathProbabilityWeight = 3.5f - agent.DensityFromGrid;

                if (agent.UpdatePathProbabilityWeight > 0)
                {
                    _updatePathProbabilityWeightTotal += agent.UpdatePathProbabilityWeight;
                    agent.CouldPathUpdate = true;
                }
            }
        }

        private void SaveDynamicDistributions()
        {
            var levels = _levelHandler.Levels();

            foreach (var level in levels)
            {
                var distributions = level.Distributions;
                List<IDistribution> dynDist = new List<IDistribution>();

                foreach (var dist in distributions)
                {
                    if (dist.Type == DistributionType.Dynamic)
                        dynDist.Add(dist);
                }

                if (dynDist.Count == 0) continue;

                if (_dynamicDistributions.ContainsKey(level.LevelId))
                {
                    _dynamicDistributions[level.LevelId] = new List<IDistribution>(dynDist);
                }
                else
                {
                    _dynamicDistributions.Add(level.LevelId, new List<IDistribution>(dynDist));
                }
            }
        }

        public void CancelRunningSimulation(params object[] parameters)
        {
            Interlocked.Exchange(ref _processing, 0);
        }

        public void ForceCompleteSimulation(params object[] parameters)
        {
            Interlocked.Exchange(ref _forceComplete, 1);
        }

        private void ProcessNewThreat(params object[] parameters)
        {
            var newThreats = GetNewThreats(parameters[0] as Info.Threat);
            _threats.AddRange(newThreats);

            foreach (var threat in newThreats)
            {
                if (threat.StartTime == 0) //should be considered immediatelly
                {
                    threat.Active = true;

                    //Notify all working threads about new threat
                    foreach (var threatUpdateSignal in _coreUpdateSignals)
                        threatUpdateSignal.Emit(ThreadSignal.EnableThreat, threat);
                }
            }
        }

        private List<GeneralThreatData> GetNewThreats(Info.Threat nThreat)
        {
            List<GeneralThreatData> newThreats = new List<GeneralThreatData>();

            switch (nThreat.ThreatType)
            {
                case (int)Def.ThreatType.DangerInRoom:
                    {
                        //get room ID based on level id and coordinates
                        var levelRooms = _levelHandler.GetRoomsByLevelId(nThreat.LevelId);

                        foreach (var room in levelRooms)
                        {
                            if (Utils.PointIsInside(nThreat.X, nThreat.Y, room.Coordinates))
                            {
                                newThreats.Add(CreateNewThreatObject(ThreatType.RoomBlockade, nThreat, room.RoomId));
                                break;
                            }
                        }

                        break;
                    }

                case (int)Def.ThreatType.GateObstruction:
                    {
                        newThreats.Add(CreateNewThreatObject(ThreatType.GateBlockade, nThreat));
                        break;
                    }

                case (int)Def.ThreatType.StairObstruction:
                    {
                        //get staircase object
                        var allStairs = _stairWayHandler.Stairs();
                        var properStair = allStairs.FirstOrDefault(pStair => pStair.StairwayID == nThreat.ElementId);

                        if (properStair != null)
                        {
                            var topLevelPortID = properStair.TopLevelGate().ElementId;
                            var topLevelID = properStair.TopLevelId();

                            var bottomLevelPortID = properStair.BottomLevelGate().ElementId;
                            var bottomLevelID = properStair.BottomLevelId();

                            //create and add new gate blocked threats
                            newThreats.Add(
                                CreateNewThreatObject(
                                    ThreatType.GateBlockade, nThreat, topLevelPortID, topLevelID
                                )
                            );
                            newThreats.Add(
                                CreateNewThreatObject(
                                    ThreatType.GateBlockade, nThreat, bottomLevelPortID, bottomLevelID
                                )
                            );
                        }

                        break;
                    }

                default:
                    break;
            }


            return newThreats;
        }

        private GeneralThreatData CreateNewThreatObject(ThreatType type, Info.Threat nThreat, int pObjectID = -1, int pLevelID = -1)
        {
            var objectID = pObjectID == -1 ? nThreat.ElementId : pObjectID;
            var levelID = pLevelID == -1 ? nThreat.LevelId : pLevelID;

            var newThreat = new GeneralThreatData(type, nThreat.ThreatId, objectID);
            newThreat.ThreatLevelID = levelID;
            newThreat.StartTime = (int)(nThreat.StartTime * (0.001 / Params.Current.TimeStep));
            newThreat.Duration = (int)(nThreat.Duration * (0.001 / Params.Current.TimeStep));

            return newThreat;
        }

        private static Dictionary<int, uint> CreateRoomAndGridAreaMap(SimpleLevel pLevel)
        {
            Dictionary<int, uint> roomGridMap = new Dictionary<int, uint>();

            foreach (GridGraph gg in AstarPath.active.data.GetUpdateableGraphs())
            {
                float floorHeight = Statics.FloorHeight + Statics.FloorOffsetGrid + Statics.LevelHeight * pLevel.LevelId;

                if (gg.center.y == floorHeight)
                {
                    foreach (var room in pLevel.TempRooms)
                    {
                        PointF[] points = new PointF[room.Coordinates.Count];

                        for (int k = 0; k < room.Coordinates.Count; k++)
                        {
                            float x = room.Coordinates[k].X;
                            float y = room.Coordinates[k].Y;

                            points[k] = new PointF(x, y);
                        }

                        PolygonHelper polyH = new PolygonHelper(points);

                        foreach (GridNode node in gg.nodes)
                        {
                            if (node.Area == 0)
                                continue;

                            if (!polyH.PointInPolygon(node.position.x / 1000f, node.position.z / 1000f)) continue;
                            if (roomGridMap.ContainsKey(room.RoomId))
                            {
                                LogWriter.Instance.WriteToLog("Aready contains key? + " + room.RoomId);
                            }
                            else
                            {
                                roomGridMap.Add(room.RoomId, node.Area);
                                break;
                            }
                        }
                    }
                }
            }

            return roomGridMap;
        }

        private static Dictionary<int, uint> CreateStairAndGridAreaMap(List<AStairway> pStairs)
        {
            Dictionary<int, uint> stairGridMap = new Dictionary<int, uint>();

            foreach (var stair in pStairs)
            {
                int stairId = stair.StairwayID;
                int graphId = -1;

               
                };

                //updatePkg.color = _agents[anAgent].IsWithinLWGRange() ? Color.blue : Color.white;

                if (_agents[anAgent].HasJustUpdatedPath)
                    _agents[anAgent].HasJustUpdatedPath = false;

                if (!updatePkg.isActive)
                    updatePkg.evacuationTime = _agents[anAgent].EvacuationTime;

                _updateSignal.Emit(Def.Signal.AgentUpdate, updatePkg);
            }
        }

        private void GenerateAgents()
        {
            AgentGenerator.SetToZero();

            _agents.Clear();
            var levels = _levelHandler.Levels();

            float splitPercentage = _classChoice.UtilData(1)._split / 100f;

            foreach (var level in levels)
            {
                foreach (var distribution in level.Distributions)
                {
                    var designatedList = distribution.GetDesignatedGatesData();
                    var designedGatesAreUsed = designatedList != null && designatedList.Count > 0;
                    bool intermediateGatesAreUsed = false;

                    DesignatedGatesData designatedGatesInfo = null;
                    List<float> prob = null;

                    var reactionTimeRequired =
                        distribution.Type == DistributionType.Uniform ? true : false;

                    List<Agent> agentsInDist = new List<Agent>();

                    for (var i = 0; i < nPopulation; ++i)
                    {
                        Agent newAgent = AgentGenerator.Generate(splitPercentage, level.LevelId, reactionTimeRequired);

                        if (newAgent == null)
                            break;

                        TryNewSpot(distribution, ref newAgent);

                        int counter = 0;
                        int tryAmount = 500;

                        while (!AgentInUniqueSpot(newAgent))
                        {
                            TryNewSpot(distribution, ref newAgent);

                            if (counter++ > tryAmount)
                                break;
                        }

                        newAgent.Color = distribution.GetColor();

                        if (designedGatesAreUsed)
                        {
                            var newRandomValue = Utils.GetNextRnd();
                            var index = Utils.ProbabilisticIndex(prob, newRandomValue);

                            newAgent.DestinationGateId = designatedGatesInfo.Distribution[index].GateID;
                            newAgent.DestinationGateId2 = newAgent.DestinationGateId;
                            newAgent.UseDesignatedGate = true;
                        }

                        if (intermediateGatesAreUsed)
                        {
                            newAgent.IntermediateGates = new List<int>();
                            newAgent.GenerateIntermediateGates(designatedGatesInfo.Distribution);
                        }

                        newAgent.AgentIndex = _agents.Count;
                        _agents.Add(newAgent);
                        agentsInDist.Add(newAgent);
                    }

                    #region Assign Groups

                    int count = agentsInDist.Count;

                    float colorStep = 1f / distribution.GroupIDs().Count;
                    int colorStepCount = 0;

                    foreach (int groupID in distribution.GroupIDs())
                    {
                        GroupData groupData = _groupHandler.GetGroupData(groupID);

                        Color groupColor = Color.HSVToRGB(colorStep * colorStepCount, 0.92f, 0.92f);
                        colorStepCount++;

                        // Randomly pick a leader for each group.
                        float randomValue = Utils.GetNextRnd(0, count);
                        Agent leader = agentsInDist[Mathf.FloorToInt(randomValue)];

                                                  int index = GetIndexFromID(leader.AgentId);
                            _agents[index].GroupID_C = (short)groupID;
                            _agents[index].Type = AgentType.Leader;
                            _agents[index].Color = groupColor;
                            index = GetIndexFromID(leader.AgentId, agentsInDist);
                            agentsInDist[index].GroupID_C = (short)groupID;
                            agentsInDist[index].Type = AgentType.Leader;
                            groupData.LeaderID = leader.AgentId;

                            agentsInDist[index].MaxSpeed =
                                Utils.GetNormalizedValue(
                                    Params.Current.AgentMaxspeed * Params.Current.GroupSpeedMultiplier,
                                    Params.Current.AgentMaxspeedDeviation
                            );
                        }

                        Vector2 leaderPosition = leader.Position;

                        // For each group add agents near leaders to group as required.
                        for (int i = 1; i < groupData.NAgents; i++)
                        {
                            Agent closestAgent = null;
                            float closestDistance = float.MaxValue;

                            foreach (var agent in agentsInDist)
                            {
                                if (agent.Type == AgentType.Individual)
                                {
                                    float newDistance = Vector2.Distance(agent.Position, leaderPosition);

                                    if (closestAgent == null)
                                    {
                                        closestAgent = agent;
                                        closestDistance = newDistance;
                                    }
                                    else
                                    {
                                        if (newDistance < closestDistance)
                                        {
                                            closestDistance = newDistance;
                                            closestAgent = agent;
                                        }
                                    }
                                }
                            }

                            int index = GetIndexFromID(closestAgent.AgentId);
                            _agents[index].GroupID_C = (short)groupID;
                            _agents[index].Type = AgentType.Follower;
                            _agents[index].Color = groupColor;

                            _agents[index].MaxSpeed =
                                Utils.GetNormalizedValue(
                                    Params.Current.AgentMaxspeed * Params.Current.GroupSpeedMultiplier,
                                    Params.Current.AgentMaxspeedDeviation
                            );
                            groupData.SaveFollowerID(_agents[index].AgentId);
                        }
                    }
                    #endregion
                }
            }

            _nTotalAgents = _agents.Count;
        }

        private int GetIndexFromID(int agentID)
        {
            return GetIndexFromID(agentID, _agents);
        }

        private int GetIndexFromID(int agentID, List<Agent> agents)
        {
            for (int i = 0; i < agents.Count; i++)
                if (agents[i].AgentId == agentID)
                    return i;

            Debug.LogError("No agent exists with the ID " + agentID);
            throw new IndexOutOfRangeException();


        public int SetGate(int agentIndex, uint areaID, RoomElement gate)
        {
            return SetGate(agentIndex, areaID, new List<RoomElement> { gate });
        }

        public int SetGate(int agentIndex, uint areaId, List<RoomElement> gates)
        {
            var gateIndex = -1;
            if (gates.Count == 0)
                return -1;
            if (gates.Count == 1)
                gateIndex = 0;
            else
            {
                // If intermediate gates are required, make sure they are chosen.
                if (_agents[agentIndex].IntermediateGates != null && _agents[agentIndex].IntermediateGates.Count > 0)
                {
                    List<RoomElement> applicableIntermediateGates = new List<RoomElement>();

                    foreach (var intermediateGateId in _agents[agentIndex].IntermediateGates)
                        foreach (var tempGate in gates)
                            if (tempGate.ElementId == intermediateGateId)
                                applicableIntermediateGates.Add(tempGate);

                    if (applicableIntermediateGates.Count > 0)
                        gates = applicableIntermediateGates;
                }

                if (gates.Count > 0)
                {
                    Gate trainGate = (Gate)gates[0];
                    if (trainGate.TrainData != null)
                    {
                        var train = _agents[agentIndex].CurrentLevel.FindTrain(trainGate.TrainData.trainID);

                        if (BoardDistValid(train.boardDistributionList))
                            gateIndex = ChooseTrainGateToBoard(train, gates);

                        if (gateIndex >= gates.Count)
                            gateIndex = -1;
                    }
                }

                bool normalGateExists = false;
                foreach (Gate gate1 in gates)
                {
                    if (gate1.WaitingData == null)
                        normalGateExists = true;
                }

                if (gateIndex < 0)
                    gateIndex = normalGateExists ? SetGateHelper(agentIndex, gates) : SetGateHelperWaitingOnly(agentIndex, gates);

            }

            _agents[agentIndex].hasWaitedAndLeaving = false;

            Gate gate = (Gate)gates[gateIndex];

            _agents[agentIndex].DesiredGateReal.X = gate.VMiddle.X;
            _agents[agentIndex].DesiredGateReal.Y = gate.VMiddle.Y;

            if (Params.Current.TicketGateQueuesEnabled && gate.WaitingData != null &&
                Utils.GateIsVisible(_agents[agentIndex].X, _agents[agentIndex].Y, gate, _agents[agentIndex].Walls, _agents[agentIndex].Poles, _agents[agentIndex].CurrentLevel.GetAllGates()))
            {
                if (agentIndex >= _agents.Count)
                {
                    Debug.Log("Cannot have index above agent count.");
                }

                _agents[agentIndex].isInWaitingQueue = true;
                _agents[agentIndex].hasWaitedAndLeaving = false;
       
                    // Check if we're heading towards the train.
                    if (_agents[agentIndex].DestinationGateId == train.destinationGateID)
                    {
                        if (train.trainDataTimetable == null || CanBoardTrainNow(gate.TrainData.trainID))
                        {
                            _agents[agentIndex].TrainPhase = TrainPhase.Boarding;
                            if (_agents[agentIndex].CurrentGate != null && _agents[agentIndex].CurrentGate.PlatformQ != null)
                                _agents[agentIndex].CurrentGate.PlatformQ.ClearQueue();

                            if (WaitForDoorEmpty(gate.ElementId))
                            {
                                var agent = _agents[agentIndex];
                                WaitAtDoor(gate, agent);
                            }
                        }
                        else
                        {
                            var waitingLocation = GenerateTrainWaitingLocation(train, _agents[agentIndex], gate);
                            _agents[agentIndex].DesiredGateReal.X = waitingLocation.X;
                            _agents[agentIndex].DesiredGateReal.Y = waitingLocation.Y;
                            _agents[agentIndex].TrainPhase = TrainPhase.Waiting;
                        }
                    }
                    else
                    {
                        _agents[agentIndex].TrainPhase = TrainPhase.Alighting;
                        //Debug.Log("Agent alighting.");
                    }
                }
                else
                {
                    _agents[agentIndex].TrainPhase = TrainPhase.None;
                }

                _agents[agentIndex].DesiredToGateReal();
            }

            _agents[agentIndex].UpdateCurrentGateId(gate.ElementId);
            _agents[agentIndex].NewPath(areaId);

            return gateIndex;
        }

        private bool BoardDistValid(List<float> trainBoardDistributionList)
        {
            if (trainBoardDistributionList == null)
                return false;

            if (trainBoardDistributionList.Count < 1)
                return false;

            foreach (var item in trainBoardDistributionList)
                if (item > 0f)
                    return true;

            return false;
        }

        private int ChooseTrainGateToBoard(Train train, List<RoomElement> gates)
        {
            float sum = 0;
            foreach (var prob in train.boardDistributionList)
                if (prob > 0) sum += prob;

            List<float> probabilities = new List<float>();

            foreach (var prob in train.boardDistributionList)
            {
                if (prob < 0)
                    probabilities.Add(0);
                else
                    probabilities.Add(prob / sum);
            }

            int carriageChosen = Utils.ProbabilisticIndex(probabilities, Utils.GetNextRnd());
            var gatesAvailable = train.gateIDsByCarriage[carriageChosen];

            Debug.Log("Carriage Chosen: " + carriageChosen);

            List<float> newProbabilities = new List<float>();

            foreach (var gate in gates)
            {
                if (gatesAvailable.Contains(gate.ElementId))
                    newProbabilities.Add(1f / gatesAvailable.Count);
                else
                    newProbabilities.Add(0);
            }

            return Utils.ProbabilisticIndex(newProbabilities, Utils.GetNextRnd());
        }

        private static void WaitAtDoor(Gate gate, Agent agent)
        {
            // Find the closest side of the door.
            float minDistance = float.MaxValue;
            CpmPair doorWaitPos = new CpmPair(gate.VMiddle.X, gate.VMiddle.Y);

            agent.DesiredGateReal.X = doorWaitPos.X;
            agent.DesiredGateReal.Y = doorWaitPos.Y;
            agent.TrainPhase = TrainPhase.WaitingAtDoor;
        }

        private bool WaitForDoorEmpty(int gateElementId)
        {
            return trainDoorsToWait.Contains(gateElementId);
        }

        private bool CanBoardTrainNow(int trainID)
        {
            if (currentTrainPhases.ContainsKey(trainID))
                if (currentTrainPhases[trainID] == TrainPhase.Boarding)
                    return true;
            return false;
        }

        private int SetGateHelperWaitingOnly(int agentIndex, List<RoomElement> gates)
        {
            float smallestDistance = float.MaxValue;
            int gateIndex = 0;

            for (int i = 0; i < gates.Count; i++)
            {
                Gate gate = (Gate)gates[i];

                bool considerGate = !Params.Current.TicketGateQueuesEnabled;

                if (!gate.WaitingData.isBidirectional && _agents[agentIndex].CurrentLevel
                        .GetRoomID(gate.WaitingData.waitPosX, gate.WaitingData.waitPosY) ==
                    _agents[agentIndex].CurrentRoomID)
                    considerGate = true;

                if (gate.WaitingData.isBidirectional)
                {
                    switch (gate.WaitingData.currentDirection)
                    {
                        case 0:
                            considerGate = true;
                            break;
                        case 1:
                            if (_agents[agentIndex].CurrentLevel
                                    .GetRoomID(gate.WaitingData.waitPosX, gate.WaitingData.waitPosY) ==
                                _agents[agentIndex].CurrentRoomID)
                                considerGate = true;
                            break;
                        case -1:
                            if (_agents[agentIndex].CurrentLevel
                                    .GetRoomID(gate.WaitingData.waitPos2X, gate.WaitingData.waitPos2Y) ==
                                _agents[agentIndex].CurrentRoomID)
                                considerGate = true;
                            break;
                    }
                }

                if (considerGate)
                {
                    float distance = Utils.DistanceBetween(_agents[agentIndex].X, _agents[agentIndex].Y, gate.VMiddle.X,
                        gate.VMiddle.Y);

                    if (distance < smallestDistance)
                    {
                        smallestDistance = distance;
                        gateIndex = i;
                    }
                }
            }

            return gateIndex;
        }

        private int SetGateHelper(int agentIndex, List<RoomElement> gates)
        {
            // Remove waiting gates...
            for (int i = gates.Count - 1; i >= 0; i--)
                if (((Gate)gates[i]).WaitingData != null)
                    gates.RemoveAt(i);

            Utilities utils = _classChoice.UtilData(_agents[agentIndex].ClassId);
            var index = -1;

            if (utils != null)
            {
                index = _gateChoiceMethod.GateIndex(agentIndex, utils, gates, _agents);
            }
            else
            {
                LogWriter.Instance.WriteToLog("Impossible to extract utils's data....");
            }

            return index;
        }

        private void ClearData()
        {
            _workerThreads.Clear();
            _threadParamsList.Clear();
            _coreUpdateSignals.Clear();
            _threats.Clear();

            _nTotalAgents = 0;
            _nCycles = 0;
        }

        public List<AStairway> GetStairs()
        {
            return _stairWayHandler != null ? _stairWayHandler.Stairs() : null;
        }

        public AStairway GetStairFromGate(int id)
        {
            var stairs = GetStairs();

            if (stairs == null)
                return null;

            foreach (var stair in stairs)
            {
                if (stair.Ports[0].Data.ElementId == id ||
                    stair.Ports[1].Data.ElementId == id)
                    return stair;
            }

            return null;
        }

        public List<SimpleLevel> GetLevels()
        {
            return _levelHandler != null ? _levelHandler.Levels() : null;
        }
    }
}
