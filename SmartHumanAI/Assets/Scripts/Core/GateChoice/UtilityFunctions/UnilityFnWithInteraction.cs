using System;
using System.Collections.Generic;
using System.Diagnostics;
using Domain.Elements;
using System.Linq;
using Domain;
using Domain.Level;

namespace Core.GateChoice.UtilityFunctions
{
    /// <summary>
    /// Implementation of Utility Function With Interraction parameter
    /// </summary>
    internal class UtilityFnWithInteraction : GateChoiceMethod
    {
        public override int GateIndex(int index, Utilities utils, List<RoomElement> gates, List<Agent> agents)
        {
            if (utils == null) return -1;

            List<double> utility = new List<double>();
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

                FlowData newFlowData = new FlowData();
                MeasureFlowData(index, distance, gate, agents, ref newFlowData);

                //var utility_power = visibility * utils._visibilityUtility + newFlowData.Congestion * utils._congestionUtility + visibility * newFlowData.Nflows * utils._fltovis + (1 - visibility) * newFlowData.Nflows * utils._fltoinvis + distance * utils._distanceUtility;

                var utility_power = distance * utils._distanceUtility;

                //Finally, determine the utility per gate
                utility.Add(Math.Pow(Math.E, utility_power));
            }

            var totalUtility = utility.Sum();

            foreach (var ut in utility)
                probabilities.Add((float)(ut / totalUtility));

            return GateIndexHelper(probabilities);
        }
    }
}
