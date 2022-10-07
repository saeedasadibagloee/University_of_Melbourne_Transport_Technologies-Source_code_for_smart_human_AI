using System;
using System.Collections.Generic;
using Core.Logger;
using Domain.Elements;

namespace Core.PositionUpdater.ReactionTimeHandler
{
    internal class ExponentialHazard : IReactionTimeHandler
    {
        public AgentRectionTimeData ReactionTime(float x, float y, int agentID, List<RoomElement> gates)
        {
            int count = 0;

            while (true)
            {
                var minDistance = DistanceToGates.GetMin(x, y, gates);
                var randomValue = Utils.GetNextRnd();

                var reactionTime = (float) (-Math.Log10(randomValue) /
                                            (Params.Current.Exp_Hazard_Lambda *
                                             Math.Exp(Params.Current.Exp_Hazard_Mu * minDistance)));

                var mean = 1.0 / Params.Current.Exp_Hazard_Lambda;
                var variance = 1.0 / (Params.Current.Exp_Hazard_Lambda * Params.Current.Exp_Hazard_Lambda);

                var reactionTimeCutoff = mean + variance * Params.Current.VarianceMultiplierCutoff;

                if (reactionTime < reactionTimeCutoff)
                    return new AgentRectionTimeData(agentID, minDistance, reactionTime);

                count++;

                if (count < Params.Current.CutoffMaxRetries)
                    continue;

                LogWriter.Instance.WriteToLog("Retried " + count + " times.");

                return new AgentRectionTimeData(agentID, minDistance, (float) reactionTimeCutoff);
            }
        }
    }
}