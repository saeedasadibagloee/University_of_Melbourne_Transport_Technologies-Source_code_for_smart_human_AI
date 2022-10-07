using Core.Signals;
using Core.Threat;
using Core.GroupBehaviour;

namespace Core.PositionUpdater
{ 
    interface IGroupPositionUpdater
    {
        void SetSignals(UpdateSignals signals);

        bool ReadyToUpdate(Domain.Agent pAgent, 
                           int index,
                           UpdateSignals uSignals, 
                           IThreatHandler tHandler);

        uint GetGridAreaId(Domain.Agent pAgent);
    }
}
