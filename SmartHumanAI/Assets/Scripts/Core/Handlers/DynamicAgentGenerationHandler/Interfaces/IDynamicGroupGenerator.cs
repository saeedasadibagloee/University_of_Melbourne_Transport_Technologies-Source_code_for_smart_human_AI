using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Handlers.DynamicAgentGenerationHandler
{
    internal interface IDynamicGroupGenerator
    {
        void SetCurrentCycle(int nCycle);

        void GenerateMoreGroups(List<Domain.Agent> pagents,
                                float splitPercentage,
                                ref int nPrevInactiveAgents,                               
                                ref long nTotalAgents);
    }
}
