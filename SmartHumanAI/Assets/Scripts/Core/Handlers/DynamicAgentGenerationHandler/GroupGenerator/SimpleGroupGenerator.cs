using Domain.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Handlers.DynamicAgentGenerationHandler
{
    using DataFormats;
    using eDriven.Core.Signals;
    using GroupBehaviour;
    using GroupBehaviour.SimpleGroup;
    using System.Threading;
    using UnityEngine;
    using DynamicDistributions = Dictionary<int, List<IDistribution>>;

    internal class SimpleGroupGenerator : IDynamicGroupGenerator
    {
        private readonly Dictionary<int, float> 
            groupCumulators = new Dictionary<int, float>();

        private IGroupHandler _groupHandler = null;       
        private DynamicDistributions _dist = null;
        private Signal _updateSignal = null;

        private int _nCycle = -1;
        private float _nColourOffset = 1f;

        private int _nInactiveAgent = -1;
        public SimpleGroupGenerator(IGroupHandler groupHandler, Signal signal, DynamicDistributions dist)
        {
            _groupHandler = groupHandler;
            _updateSignal = signal;
            _dist = dist;
        }

        public void SetCurrentCycle(int nCycle)
        {
            _nCycle = nCycle;
        }

        public void GenerateMoreGroups(List<Domain.Agent> pAgents,
                                       float splitPercentage,
                                       ref int nPrevInactiveAgents,                                      
                                       ref long nTotalAgents)
        {
            _nColourOffset = 1;
            //var localInactiveAgents = nInactiveAgents;
            _nInactiveAgent = 0;

            foreach (var pair in _dist)
            {
                foreach (var dist in pair.Value)
                {
                    var dynamicGroupData = dist.GetDynamicDistributionData().DymanicGroupData;

                    if (dynamicGroupData == null)
                        continue;

                    foreach (var dData in dynamicGroupData)
                    {
                        if (!groupCumulators.ContainsKey(dData.groupNum))
                            groupCumulators.Add(dData.groupNum, 0f);
                        
                        groupCumulators[dData.groupNum] += dData.numGroups;

                        // Do we need to generate any groups this cycle?
                        if (groupCumulators[dData.groupNum]  < 1f)
                            continue;

                        // We need to generate some groups this cycle.
                        int numGroupsToGenerate = Mathf.FloorToInt(groupCumulators[dData.groupNum]);
                        groupCumulators[dData.groupNum] -= dData.numGroups;// numGroupsToGenerate;

                        for (int i = 0; i < numGroupsToGenerate; i++)
                        {
                            var currentPopulation = dist.Population();
                            var maxPopulation = dist.GetDynamicDistributionData().MaxPopulation;

                            //check how many agents we can generate
                            var delta = maxPopulation - currentPopulation;
                            if (delta == 0) continue;

                            var nAgentsToGenerate =
                                (delta >= dData.groupNum) ? dData.groupNum : delta;
                            
                            var newGroupID = _groupHandler.NewGroupID();
                            _nColourOffset = Utils.GetNextRnd();

                            var locations = GenerateMultipleSpots(dist);
                            var newGroupData = new GroupData(dData.groupNum, newGroupID);

                            //generate Leader
                            var leader =
                                Domain.AgentGenerator.Generate(splitPercentage, pair.Key, false, Domain.AgentType.Leader, _nCycle);

                            if (leader == null)
                                break;

                            //Set leader's internal data
                            SetAgentData(leader, newGroupID, locations.Last());
                            //Add leader to the rest of the crowd
                            AddNewAgentIntoCrowd(pAgents, leader);
                            locations.Remove(locations.Last());

                            for (var offset = 1; offset < nAgentsToGenerate; ++offset)
                            {
                                var pointWithMinDistance =
                                    locations.Aggregate((l1, l2) =>
                                        Utils.DistanceBetween(l1.X, l1.Y, leader.X, leader.Y) <
                                        Utils.DistanceBetween(l2.X, l2.Y, leader.X, leader.Y) ? l1 : l2);

                                //generate Folloder
                                var follower =
                                    Domain.AgentGenerator.Generate(splitPercentage, pair.Key, false, Domain.AgentType.Follower, _nCycle);

                                if (follower == null)
                                    break;

                                //Set leader's internal data
                                SetAgentData(follower, newGroupID, pointWithMinDistance);
                                //Add leader to the rest of the crowd
                                AddNewAgentIntoCrowd(pAgents, follower);
                                locations.Remove(pointWithMinDistance);
                            }

                            _groupHandler.AddNewGroup(newGroupData, true);
                            dist.SetPopulation(currentPopulation + nAgentsToGenerate);                            
                        }
                    }                                      
                }
            }

            if (_nInactiveAgent != 0)
                nPrevInactiveAgents += _nInactiveAgent;
            
            Interlocked.Exchange(ref nTotalAgents, pAgents.Count);
        }

        private void SetAgentData(Domain.Agent pAgent, int groupID, Domain.CpmPair cpmPair)
        {
            pAgent.X = cpmPair.X;
            pAgent.Y = cpmPair.Y;

            pAgent.XTmp = cpmPair.X;
            pAgent.YTmp = cpmPair.Y;

            pAgent.GroupID_C = (short)groupID;             
            pAgent.Color = Color.HSVToRGB(_nColourOffset, 0.92f, 0.92f);

            pAgent.MaxSpeed =
                Utils.GetNormalizedValue(
                    Params.Current.AgentMaxspeed * Params.Current.GroupSpeedMultiplier,
                    Params.Current.AgentMaxspeedDeviation
            );
        }

        private void AddNewAgentIntoCrowd(List<Domain.Agent> pAgents, 
                                          Domain.Agent newAgent)
        {
            var inActiveAgent = pAgents.Find(pAgent => pAgent.Active == false);
            if (inActiveAgent != null)
            {
                var index = pAgents.IndexOf(inActiveAgent);

                //Send last data update before using this entry
                var lastPkg = pAgents[index].LastUpdateData();
                _updateSignal.Emit(DataFormats.Def.Signal.AgentUpdate, lastPkg);

                newAgent.AgentIndex = index;
                pAgents[index] = newAgent;
                ++_nInactiveAgent;
            }
            else
            {
                newAgent.AgentIndex = pAgents.Count;
                pAgents.Add(newAgent);
            }
        }

        private List<Domain.CpmPair> GenerateMultipleSpots(IDistribution distribution)
        {
            var locations = new List<Domain.CpmPair>();

            for (int i = 0; i < 100; i++)
                locations.Add(distribution.GenerateLocation());

            return locations;
        }
    }
}
