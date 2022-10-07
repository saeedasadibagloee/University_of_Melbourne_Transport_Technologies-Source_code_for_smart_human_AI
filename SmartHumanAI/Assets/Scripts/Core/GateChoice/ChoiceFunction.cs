using System;
using System.Collections.Generic;
using Core.GateChoice.RegretFunctions;
using Core.GateChoice.UtilityFunctions;
using Core.Logger;
using Domain;
using Domain.Elements;
using Domain.Level;

namespace Core.GateChoice
{
        {
            lock (Mutex)
            {
                MeasureCongFlow(index, distance, gate, agents, ref fdata);
            }

            var gateLen2 = gate.Length / 2;

            fdata.Density = (float)(fdata.Congestion / (3.14 * 0.5 * gateLen2 * gateLen2));
        }

        protected float GetDistance(Agent agent, RoomElement gate)
        {

            }
        }

        internal static void MeasureCongFlow(int index, float distance, RoomElement gate, List<Domain.Agent> agents, ref Domain.FlowData fdata)
        {
            var gateLen = gate.Length;

            foreach (var agent in agents)
            {
                var OtherAgentDistanceToGate
                    = Utils.DistanceBetween(agent.X, agent.Y, gate.VMiddle.X, gate.VMiddle.Y);
                var speedMaginitude =
                    (float)Math.Sqrt(Math.Pow(agent.XVelocity, 2) + Math.Pow(agent.YVelocity, 2));

                var baseCondition =
                    agent.AgentId != agents[index].AgentId
                    && agent.CurrentRoomID == agents[index].CurrentRoomID
                    && agent.CurrentGateId == gate.ElementId
                    && OtherAgentDistanceToGate <= distance;

                if (!baseCondition) continue;

                if (OtherAgentDistanceToGate < Params.Current.CongestionRadius
                    && speedMaginitude <= Params.Current.SpeedFlowThreshold
                    && agent.DensityFromGrid > Params.Current.DensityFlowThreshold)
                    ++fdata.Congestion;
                else
                    ++fdata.Nflows;
            }
        }

        /// <summary>
        /// Function to return gate index using Probabilites approach
        /// </summary>
        protected int GateIndexHelper(List<float> probabilities)
        {
            var randomValue = Rand.NextDouble();
            return Utils.ProbabilisticIndex(probabilities, randomValue);
        }

        /// <summary>
        /// Virtual function to get a gate index from the set of gates based on the gate choice algorithm
        /// </summary>
        public virtual int GateIndex(int index, Utilities utils, List<RoomElement> gates, List<Domain.Agent> agents)
        {
            return gateIndex;
        }
    }

    /// <summary>
    /// Factory class to generate required Gate Choice functions
    /// </summary>
    internal abstract class FunctionFactory
    {
        public virtual GateChoiceMethod GetFunction(FunctionChoice fchoice)
        {
            return null;
        }
    }

    /// <summary>
    /// Factory to generate classes that take into account interraction parameter
    /// </summary>
    internal class WithInteractionFactory : FunctionFactory
    {
    }

    /// <summary>
    /// Factory to generate classes that do not take into account interraction parameter
    /// </summary>
    internal class WithoutInteractionFactory : FunctionFactory
    {
        public override GateChoiceMethod GetFunction(FunctionChoice fchoice)
        {
            switch (fchoice)
            {
                case FunctionChoice.Utility:
                    return new UtilityFnWithoutInteraction();

                case FunctionChoice.Regret:
                    return new RegretFnWithoutInteraction();
                case FunctionChoice.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("fchoice", fchoice, null);
            }

            return null;
        }
    }
}
