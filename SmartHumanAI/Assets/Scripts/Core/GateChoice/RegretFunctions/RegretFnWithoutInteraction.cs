using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Elements;

namespace Core.GateChoice.RegretFunctions
{
    /// <summary>
    /// Implementation of Regret Function
    /// </summary>
    internal class RegretData
    {
        public int   GateId;
        public float Distance;
        public int   Congestion;
        public int   Flow;
        public int   Visibility;

        public float Fltovis;
        public float Fltoinvis;

        public RegretData(int pGateId, float pDistance, int pCongest, int pFlow, int pVis)
        {
            GateId = pGateId;
            Distance = pDistance;
            Congestion = pCongest;
            Flow = pFlow;
            Visibility = pVis;

            Fltovis = Flow * Visibility;
            Fltoinvis = Flow * (1 - Visibility);
        }
    }

    internal class RegretFnWithoutInteraction : GateChoiceMethod
    {
        public override int GateIndex(int index, Utilities utils, List<RoomElement> gates, List<Domain.Agent> agents)
        {
            if (utils == null) return -1;

            List<RegretData> rData = new List<RegretData>();
            List<double> negativeRegretValues = new List<double>();
            List<float> probabilities = new List<float>();

            CheckIfNearGate(index, agents);

            foreach (var gate in gates)
            {
                //Calculate distance between the given agent and the given gate
                var distance = GetDistance(agents[index], gate);

                //Calculate visibility
                var visibility =
                    Utils.GateIsVisible(
                        agents[index].X, agents[index].Y, gate,
                        agents[index].Walls, agents[index].Poles) ? 1 : 0;

                Domain.FlowData newFlowData = new Domain.FlowData();
                MeasureFlowData(index, distance, gate, agents, ref newFlowData);
                //congestions.Add(newFlowData.congestion); 
               
                RegretData nRegData = 
                    new RegretData(
                        gate.ElementId, distance, 
                            newFlowData.Congestion, newFlowData.Nflows, visibility
                );
                rData.Add(nRegData);
            }

            foreach (var data in rData)
            {
                var regretValueDistance   = 0d;
                var regretValueCongestion = 0d;
                var regretValueFlow       = 0d;
                var regretValueVisibility = 0d;

                var otherRegretData = rData.Where(pData => pData.GateId != data.GateId).ToList();

                foreach (var d in otherRegretData)
                {
                    regretValueDistance += Math.Log(1 + Math.Exp(utils._distanceUtility * (d.Distance - data.Distance)));
                    //regretValueCongestion += Math.Log(1 + Math.Exp(utils._congestionUtility * (d.Congestion - data.Congestion)));
                    //regretValueFlow += Math.Log(1 + Math.Exp(utils._flowExitUtility * (d.Flow - data.Flow)));
                    //regretValueVisibility += Math.Log(1 + Math.Exp(utils._visibilityUtility * (d.Visibility - data.Visibility)));
                }

                var finalRegretValue = regretValueDistance + regretValueCongestion + regretValueFlow + regretValueVisibility;
                negativeRegretValues.Add(Math.Exp( -1 * finalRegretValue ));
            }

            //get the sum
            var regretSum = negativeRegretValues.Sum();
            //calculate probability
            foreach (var regretValue in negativeRegretValues)
                probabilities.Add((float)(regretValue / regretSum));

            return GateIndexHelper(probabilities);
        }
    }
}
