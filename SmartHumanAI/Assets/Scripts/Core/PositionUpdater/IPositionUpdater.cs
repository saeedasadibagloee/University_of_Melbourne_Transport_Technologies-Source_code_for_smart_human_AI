using System.Collections.Generic;
using Core.Signals;
using Core.Threat;
using Domain;

namespace Core.PositionUpdater
{
    //main position updater inferface
    interface IPositionUpdater
    {  
        uint GetGridAreaId(Domain.Agent pAgent);
        bool ReadyToUpdate(List<Agent> agents, int anAgent, UpdateSignals updateSignals, IThreatHandler _threatHandler);
    } 
}
