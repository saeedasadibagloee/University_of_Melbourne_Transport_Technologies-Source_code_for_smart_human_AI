using Core.Threat;
using System.Collections.Generic;

namespace Core.PositionUpdater.EvacuationPathHandler
{
    internal interface IPath
    {
        List<int> GetPath(Domain.Agent pAgent, IThreatHandler tHandler);
    }
}
