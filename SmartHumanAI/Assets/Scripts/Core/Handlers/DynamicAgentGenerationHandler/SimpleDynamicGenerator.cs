using Domain.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Handlers.DynamicAgentGenerationHandler
{
    using eDriven.Core.Signals;
    using DynamicDistributions = Dictionary<int, List<IDistribution>>;

    internal class SimpleDynamicGenerator : IDynamicGenerator
    {
        private IDynamicIndividualsGenerator _individualsGenerator = null;

        public SimpleDynamicGenerator(Signal updateSignal, DynamicDistributions dist)
        {
            _individualsGenerator = new SimpleIndividualsGenerator(updateSignal, dist);
        }

        public void SetCurrentCycle(int nCycle)
        {
            _individualsGenerator.SetCurrentCycle(nCycle);
        }

        public  bool AllAgentWereGenerated(DynamicDistributions dynamicDistributions)
        {
            return DistributionsTester.AllAgentWereGenerated(dynamicDistributions);
        }     

        public void GenerateMoreAgents(List<Domain.Agent> pagents,
                                       float splitPercentage,                                      
                                       ref int nPrevInactiveAgents,                                      
                                       ref long nTotalAgents)
        {
            _individualsGenerator.GenerateMoreAgents(
                pagents, splitPercentage, ref nPrevInactiveAgents,
                ref nTotalAgents
            );
        }
    }
}
