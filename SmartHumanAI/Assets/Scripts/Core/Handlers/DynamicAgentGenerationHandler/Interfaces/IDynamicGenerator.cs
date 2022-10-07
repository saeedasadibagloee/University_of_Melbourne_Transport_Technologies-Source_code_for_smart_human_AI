using Domain.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Handlers.DynamicAgentGenerationHandler
{
    using DynamicDistributions = Dictionary<int, List<IDistribution>>;

    internal interface IDynamicGenerator
    {
        bool AllAgentWereGenerated(DynamicDistributions dynamicDistributions);

        void SetCurrentCycle(int nCycle);

        void GenerateMoreAgents(List<Domain.Agent> pagents,                                
                                float splitPercentage,                               
                                ref int nPrevInactiveAgents,                                
                                ref long nTotalAgents);
    }    
}
