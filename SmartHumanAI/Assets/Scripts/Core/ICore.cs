using System.Collections.Generic;
using Core.GateChoice;
using DataFormats;
using Domain.Elements;
using Domain.Level;
using Domain.Stairway;

namespace Core
{
    internal interface ICore
    {
        bool Initialize(Model pModel, SimulationParams simParams);
        int SetGate(int agentIndex, uint areaID, List<RoomElement> gates);
        List<AStairway> GetStairs();
        List<SimpleLevel> GetLevels();
    }
}
