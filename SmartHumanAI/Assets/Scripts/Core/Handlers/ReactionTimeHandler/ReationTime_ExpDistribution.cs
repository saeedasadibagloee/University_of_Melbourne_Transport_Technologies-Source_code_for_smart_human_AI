using System;
using System.Collections.Generic;
using Core.PositionUpdater.ReactionTimeHandler;
using Domain.Elements;

namespace Core.Handlers.ReactionTimeHandler
{
    internal class GeneralExpDistribution : IReactionTimeHandler
    {
        public AgentRectionTimeData ReactionTime(float x, float y, int agentID, List<RoomElement> gates)
        {
            var minDistance = DistanceToGates.GetMin(x, y, gates);
            var randomValue = Utils.GetNextRnd();
            
            var reactionTime = 
                -(float)((minDistance <= Params.Current.Radius_RT ? 
                 Params.Current.Beta1 : Params.Current.Beta2) * Math.Log10(1 - randomValue));

            return new AgentRectionTimeData(agentID, minDistance, reactionTime);
        }
    }
}

