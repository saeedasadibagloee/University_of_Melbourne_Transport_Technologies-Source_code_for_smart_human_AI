using Domain.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Core.GroupBehaviour;

namespace Core.Handlers.DynamicAgentGenerationHandler
{
    using eDriven.Core.Signals;
    using DynamicDistributions = Dictionary<int, List<IDistribution>>;

    internal class ComplexDynamicGenerator : IDynamicGenerator
    {
        private IDynamicIndividualsGenerator _individualsGenerator = null;
        private IDynamicGroupGenerator _groupGenerator = null;

        public ComplexDynamicGenerator(IGroupHandler groupHandler, 
                                       Signal updateSignal, 
                                       DynamicDistributions dist)
        {            
            _individualsGenerator = new SimpleIndividualsGenerator(updateSignal, dist);
            _groupGenerator = new SimpleGroupGenerator(groupHandler, updateSignal, dist);
        }

        public void SetCurrentCycle(int nCycle)
        {
            _individualsGenerator.SetCurrentCycle(nCycle);
            _groupGenerator.SetCurrentCycle(nCycle);
        }

        public bool AllAgentWereGenerated(DynamicDistributions dynamicDistributions)
        {
            return DistributionsTester.AllAgentWereGenerated(dynamicDistributions);
        }

        public void GenerateMoreAgents(List<Domain.Agent> pagents,
                                       float splitPercentage,                                      
                                       ref int nPrevInactiveAgents,                                      
                                       ref long nTotalAgents)
        {
            //generate individuals where possible
            _individualsGenerator.GenerateMoreAgents(
                pagents, splitPercentage, ref nPrevInactiveAgents,
                ref nTotalAgents
            );

            //generate groups where possible
            _groupGenerator.GenerateMoreGroups(
                 pagents, splitPercentage, ref nPrevInactiveAgents,
                 ref nTotalAgents
            );
        }
    }
}
