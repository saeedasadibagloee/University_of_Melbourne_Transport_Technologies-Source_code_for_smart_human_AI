using Domain.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataFormats;

namespace Core.Handlers.DynamicAgentGenerationHandler
{
    using DynamicDistributions = Dictionary<int, List<IDistribution>>;

    internal class DistributionsTester
    {
        public static bool AllAgentWereGenerated(DynamicDistributions dynamicDistributions)
        {
            foreach (var distList in dynamicDistributions.Values)
            {
                foreach (var dist in distList)
                {
                    DynamicData dynData = dist.GetDynamicDistributionData();

                    if (dynData.PopulationTimetable != null)
                    {
                        if (dynData.PopulationTimetable.Count > 0)
                            return false;
                    }
                    else
                    {
                        var currentPopulation = dist.Population();
                        var maxPopulation = dynData.MaxPopulation;

                        if (currentPopulation < maxPopulation)
                            return false;
                    }
                }
            }

            return true;
        }

        public static void NewSpotInDistribution(IDistribution distribution, Domain.Agent newAgent)
        {
            Domain.CpmPair pair = distribution.GenerateLocation();

            newAgent.X = pair.X;
            newAgent.Y = pair.Y;

            newAgent.XTmp = pair.X;
            newAgent.YTmp = pair.Y;
        }
    }
}
