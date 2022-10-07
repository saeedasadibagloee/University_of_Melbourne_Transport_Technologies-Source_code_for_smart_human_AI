using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core;
using DataFormats;
using UnityEngine;

namespace Domain.Elements
{
    internal class Gate : RoomElement
    {

        public Gate(float startX, float startY, float endX, float endY)
            : base(startX, startY, endX, endY)
        {
            ElementType = ElementType.Gate;
            InnerType = InnerType.EvacuationGate;
        }

        public WaitingDataCore WaitingData = null;
        public TrainDoorData TrainData = null;
        public PlatformQueueCore PlatformQ = null;
        public bool DesignatedOnly = false;

        public Vertex CAPoint1;
        public Vertex CAPoint2;

        public override int Type() { return (int)ElementType; }

        public override void SetAsDestination()
        {
            Destination = true;
        }

        public void CalcCAPoints()
        {
            CAPoint1 = new Vertex((VMiddle.X + VStart.X) / 2f, (VMiddle.Y + VStart.Y) / 2f);
            CAPoint2 = new Vertex((VMiddle.X + VEnd.X) / 2f, (VMiddle.Y + VEnd.Y) / 2f);
        }
    }

    internal class PlatformQueueCore
    {
        public Queue<int> agentQueue = new Queue<int>();
        public List<CpmPair> qPositions = new List<CpmPair>();

        public CpmPair JoinQueue(int agentId)
        {
            agentQueue.Enqueue(agentId);

            if (agentQueue.Count > qPositions.Count)
                return qPositions.Last();

            return qPositions[agentQueue.Count - 1];
        }

        public void ClearQueue()
        {
            agentQueue.Clear();
        }

        public PlatformQueueCore(List<Vector3s> waitingPos)
        {
            if (waitingPos.Count < 1)
            {
                UnityEngine.Debug.LogError("Cannot have a plaform queue shorter than 2!");
                return;
            }

            qPositions.Add(new CpmPair(waitingPos[0].x, waitingPos[0].z));

            float iAgentDist = 0.66f;

            for (int i = 0; i < waitingPos.Count - 1; i++)
            {
                float segDist = Utils.DistanceBetween(waitingPos[i].x, waitingPos[i].z,
                                                      waitingPos[i + 1].x, waitingPos[i + 1].z);
                int segs = Mathf.RoundToInt(segDist / iAgentDist);

                for (int j = 1; j <= segs; j++)
                {
                    var pointOnLine = Vector3.Lerp(waitingPos[i].ToV3(), waitingPos[i + 1].ToV3(), j / (float) segs);
                    qPositions.Add(new CpmPair(pointOnLine.x, pointOnLine.z));
                }
            }
        }
    }

    internal class WaitingDataCore
    {
        public int currentDirection = 0;
        public float waitTime { get; set; }
        public bool isBidirectional { get; set; }

        public float waitPosX { get; set; }
        public float waitPosY { get; set; }
        public float targetPosX { get; set; }
        public float targetPosY { get; set; }
        public float waitPos2X { get; set; }
        public float waitPos2Y { get; set; }
        public float targetPos2X { get; set; }
        public float targetPos2Y { get; set; }

        public bool personAtGate = false;
        public Agent PreviousAgent = null;

        public int nextCycle = -1;

        /// <summary>
        /// List of agent indexes
        /// </summary>
        public List<int> agentsInQueue = new List<int>();

        internal void StartTimer(int currentCycle)
        {
            nextCycle = currentCycle + (int)(waitTime / Params.Current.TimeStep);
        }

        public void UpdateAgentQueuePos(List<Agent> agents)
        {
            int queuePos = 0;

            foreach (var agentIndex in agentsInQueue)
            {
                agents[agentIndex].numberInQueue = queuePos;
                queuePos++;
            }
        }
    }
}
