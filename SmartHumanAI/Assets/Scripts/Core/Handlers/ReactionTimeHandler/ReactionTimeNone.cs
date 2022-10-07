using System.Collections.Generic;
using Core.PositionUpdater.ReactionTimeHandler;
using Domain.Elements;

namespace Core.Handlers.ReactionTimeHandler
{
    internal class ReactionTimeNone : IReactionTimeHandler
    {
        public AgentRectionTimeData ReactionTime(float x, float y, int agentID, List<RoomElement> gates)
        {
            var minDistance = DistanceToGates.GetMin(x, y, gates);

            return new AgentRectionTimeData(agentID, minDistance, 0f);
        }
    }
}