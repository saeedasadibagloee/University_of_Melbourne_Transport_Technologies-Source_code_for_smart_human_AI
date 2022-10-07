using System;
using System.Collections.Generic;
using System.Diagnostics;
using Core.Logger;
using Domain.Elements;
using UnityEngine.Assertions;

namespace Core.PositionUpdater.ReactionTimeHandler
{
    internal class WeibullHazard : IReactionTimeHandler
    {
        public AgentRectionTimeData ReactionTime(float x, float y, int agentID, List<RoomElement> gates)
        {
            int count = 0;

            while (true)
            {
                var minDistance = DistanceToGates.GetMin(x, y, gates);
                var randomValue = Utils.GetNextRnd();

                var baseValue = -Math.Log10(randomValue) / (Params.Current.Weibull_Hazard_Lambda * Math.Exp(Params.Current.Weibull_Hazard_Mu * minDistance));

                Assert.AreNotEqual(Params.Current.Weibull_Hazard_Nu, 0f);

                var reactionTime = (float) Math.Pow(baseValue, 1 / Params.Current.Weibull_Hazard_Nu);

                var gammaVarOne = alglib.gammafunction(1 / Params.Current.Weibull_Hazard_Nu + 1);
                var mean = Math.Pow(Params.Current.Weibull_Hazard_Lambda, -1 / Params.Current.Weibull_Hazard_Nu) * gammaVarOne;
                var variance = Math.Pow(Params.Current.Weibull_Hazard_Lambda * Params.Current.Weibull_Hazard_Lambda, -1 / Params.Current.Weibull_Hazard_Nu) * (alglib.gammafunction(2 / Params.Current.Weibull_Hazard_Nu + 1) - (gammaVarOne * gammaVarOne));

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