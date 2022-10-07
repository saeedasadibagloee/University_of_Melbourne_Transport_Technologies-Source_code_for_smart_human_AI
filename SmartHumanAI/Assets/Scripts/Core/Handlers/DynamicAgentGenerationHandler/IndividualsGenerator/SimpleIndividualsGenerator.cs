using Domain.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataFormats;
using Domain;
using eDriven.Core.Signals;
using UnityEngine;

namespace Core.Handlers.DynamicAgentGenerationHandler
{
    using System.Threading;
    using DynamicDistributions = Dictionary<int, List<IDistribution>>;

    internal class SimpleIndividualsGenerator : IDynamicIndividualsGenerator
    {
        private int _nCycle = -1;
        private Signal _updateSignal = null;
        private DynamicDistributions _dynamicDistributions = null;
        private int _nInactiveAgent = -1;

        private List<List<float>> distributionAccumulator;

        public SimpleIndividualsGenerator(Signal updateSignal, DynamicDistributions dist)
        {
            _updateSignal = updateSignal;
            _dynamicDistributions = dist;
            InitialiseAccumulator();
        }

        private void InitialiseAccumulator()
        {
            distributionAccumulator = new List<List<float>>();

            foreach (var dict in _dynamicDistributions)
            {
                distributionAccumulator.Add(new List<float>());
                foreach (var list in dict.Value)
                {
                    distributionAccumulator[dict.Key].Add(0f);
                }
            }
        }

        public void SetCurrentCycle(int nCycle) { _nCycle = nCycle; }

        public void GenerateMoreAgents(List<Agent> pAgents,
                                       float splitPercentage,
                                       ref int nPrevInactiveAgents,
                                       ref long nTotalAgents)
        {
            _nInactiveAgent = 0;

            foreach (var keyValuePair in _dynamicDistributions)
            {
                for (int i1 = 0; i1 < keyValuePair.Value.Count; i1++)
                {
                    var dist = keyValuePair.Value[i1];

                    int currentPopulation = dist.Population();
                    int maxPopulation = dist.GetDynamicDistributionData().MaxPopulation;

                    int nIncrease = 0;

                    // Check if timetable generation or not.
                    if (dist.GetDynamicDistributionData().PopulationTimetable != null)
                    {
                        if (dist.GetDynamicDistributionData().PopulationTimetable.Count == 0)
                            continue;

                        // If we should generate based on the timetable.
                        nIncrease = dist.AmountToGenerate(_nCycle);
                    }
                    else
                    {
                        var dynamicDistributionData = dist.GetDynamicDistributionData();

                        int index = 0;

                        for (var i = 0; i < dynamicDistributionData.Times.Count; i++)
                        {
                            if (dynamicDistributionData.Times[i] >= _nCycle * Params.Current.TimeStep)
                                break;
                            index = i;
                        }

                        if (dynamicDistributionData.PopulationIncremental != null &&
                            dynamicDistributionData.PopulationIncremental.Count > 0)
                        {
                            if (index >= dynamicDistributionData.PopulationIncremental.Count)
                                index = dynamicDistributionData.PopulationIncremental.Count - 1;

                            distributionAccumulator[keyValuePair.Key][i1] +=
                                dynamicDistributionData.PopulationIncremental[index];

                            nIncrease = Mathf.FloorToInt(distributionAccumulator[keyValuePair.Key][i1]);

                            distributionAccumulator[keyValuePair.Key][i1] -= nIncrease;
                        }
                    }

                    List<DesignatedGatesData> designatedList = dist.GetDesignatedGatesData();

                    bool designedGatesAreUsed = designatedList != null && designatedList.Count > 0;
                    bool intermediateGatesAreUsed = false;

                    List<float> prob = null;
                    DesignatedGatesData designatedGatesInfo = null;

                    if (designedGatesAreUsed)
                    {
                        // Find the designated gate data applicable to the current time-step
                        int currentID = 0;
                        for (var i = 0; i < designatedList.Count; i++)
                        {
                            if (designatedList[i].startSeconds >= _nCycle * Params.Current.TimeStep)
                                break;
                            currentID = i;
                        }

                        designatedGatesInfo = designatedList[currentID];

                        bool atLeastOneDesignatedGateExists = false;
                        
                        foreach (var desGate in designatedGatesInfo.Distribution)
                        {
                            if (desGate.IntermediateGate)
                                intermediateGatesAreUsed = true;
                            else
                                atLeastOneDesignatedGateExists = true;
                        }

                        if (atLeastOneDesignatedGateExists)
                        {
                            prob = new List<float>();

                            // Allocate the probabilities for choosing each designated gate from the list
                            foreach (var desGate in designatedGatesInfo.Distribution)
                            {
                                if (desGate.IntermediateGate) // Do not pick intermediate gates as designated gates.
                                    prob.Add(0f);
                                else
                                    prob.Add(desGate.Percentage / 100); // Designated gates have probabilities.
                            }
                        }
                        else
                        {
                            // In the case where intermediate gates are used, but no designated gates are present this time step.
                            designedGatesAreUsed = false;
                        }
                    }

                    #region GenerateIndividuals

                    if (currentPopulation < maxPopulation)
                    {
                        var delta = maxPopulation - currentPopulation;
                        var nAgentsToGenerate = delta > nIncrease ? nIncrease : delta;

                        for (var i = 0; i < nAgentsToGenerate; ++i)
                        {
                            var newAgent =
                                AgentGenerator.Generate(splitPercentage, keyValuePair.Key, false, AgentType.Individual, _nCycle);

                            if (newAgent == null)
                                break;

                            DistributionsTester.NewSpotInDistribution(dist, newAgent);
                            newAgent.Color = dist.GetColor();

                            if (designedGatesAreUsed)
                            {
                                // Using a random number pick an index using the probabilities supplied
                                var newRandomValue = Utils.GetNextRnd();
                                var index = Utils.ProbabilisticIndex(prob, newRandomValue);

                                newAgent.DestinationGateId = designatedGatesInfo.Distribution[index].GateID;
                                newAgent.DestinationGateId2 = newAgent.DestinationGateId;
                                newAgent.UseDesignatedGate = true;
                            }

                            if (intermediateGatesAreUsed)
                            {
                                newAgent.IntermediateGates = new List<int>();

                                foreach (var desGate in designatedGatesInfo.Distribution)
                                {
                                    if (!desGate.IntermediateGate) continue;

                                    // Add intermediate gates to specific agent if the probability is satisfied
                                    var newRandomValue = Utils.GetNextRnd();
                                    if (newRandomValue <= desGate.Percentage / 100f)
                                        newAgent.IntermediateGates.Add(desGate.GateID);
                                }
                            }

                            var inActiveAgent = pAgents.Find(pAgent => pAgent.Active == false);
                            if (inActiveAgent != null)
                            {
                                var index = pAgents.IndexOf(inActiveAgent);

                                //Send last data update before using this entry
                                var lastPkg = pAgents[index].LastUpdateData();
                                _updateSignal.Emit(Def.Signal.AgentUpdate, lastPkg);

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

                        dist.SetPopulation(currentPopulation + nAgentsToGenerate);
                    }

                    #endregion
                }
            }

            if (_nInactiveAgent != 0)
                nPrevInactiveAgents += _nInactiveAgent;

            Interlocked.Exchange(ref nTotalAgents, pAgents.Count);
        }
    } //end of class
}
