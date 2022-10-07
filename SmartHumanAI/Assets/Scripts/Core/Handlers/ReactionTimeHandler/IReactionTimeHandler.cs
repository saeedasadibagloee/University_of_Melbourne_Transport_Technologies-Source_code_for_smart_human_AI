using System.Collections.Generic;
using System.Linq;
using Core.Logger;
using Domain.Elements;

namespace Core.PositionUpdater.ReactionTimeHandler
{
    internal class AgentRectionTimeData
    {
        public int AgentID { get; set; }
        public float MinDistance { get; set; }

        public float ReactionTime { get; set; }

        public AgentRectionTimeData(int agentID, float minDistance, float reactionTime)
        {
            AgentID = agentID;
            MinDistance = minDistance;

            var isNaN = float.IsNaN(reactionTime);
            ReactionTime = isNaN ? 0 : reactionTime;

            if (isNaN) LogWriter.Instance.WriteToLog("ReactionTime was NaN, AgentID: " + agentID);
        }
    }

    interface IReactionTimeHandler
    {
        AgentRectionTimeData
        ReactionTime(float x, float y, int agentID, List<RoomElement> gates);
    }

    internal class DistanceToGates
    {
        public static float GetMin(float x, float y, List<RoomElement> gates)
        {
            List<float> distances = new List<float>();

            foreach (var gate in gates)
                distances.Add(Utils.DistanceBetween(x, y, gate.VMiddle.X, gate.VMiddle.Y));

            return distances.Min();
        }
    }
}