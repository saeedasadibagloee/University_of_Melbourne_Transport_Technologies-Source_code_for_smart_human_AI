using System.Collections.Generic;
using System.Linq;
using Core;
using Domain;

namespace Helper
{
    public static class QueueFormer
    {
        private static System.Random rand = new System.Random();
        private static List<CpmPair> gatePositions;
        private static List<CpmPair> agentPositions;

        private static List<List<int>> queues;
        private static List<int> usedAgents;
        private static List<int> exclusionListEoq;
        private static List<int> exclusionListUnequal;
        private static List<LineSeg> wallsList;

        private static int queueHysteresis = 2;
        private static int maxQueueLength = 4;

        public static List<List<int>> Execute(List<CpmPair> gates, List<CpmPair> agents, List<LineSeg> wallsList = null)
        {
            gatePositions = gates;
            agentPositions = agents;
            usedAgents = new List<int>();
            exclusionListEoq = new List<int>();
            exclusionListUnequal = new List<int>();

            // Initialise queues
            queues = new List<List<int>>();
            for (int i = 0; i < gates.Count; i++)
                queues.Add(new List<int>());

            QueueFormer.wallsList = wallsList;

            while (true)
            {
                //if (exclusionListEoq.Count >= gatePositions.Count) break;

                if (CheckUnequalQueues())
                    continue;

                int gateIndex = PickMostEmptyGate(queues);
                if (gateIndex == -1)
                    break;

                if (queues[gateIndex].Count > maxQueueLength)
                {
                    if (!exclusionListEoq.Contains(gateIndex))
                        exclusionListEoq.Add(gateIndex);

                    if (exclusionListEoq.Count >= queues.Count)
                    {
                        //UnityEngine.Debug.Log("break");
                        break;
                    }
                }

                int agentIndex = FindAgentToAddToEOQ(gateIndex);
                if (agentIndex == -1)
                {
                    exclusionListEoq.Add(gateIndex);
                }
                else
                {
                    if (queues[gateIndex].Count == 0)
                        gateIndex = MaybeFindCloserEmptyGate(gateIndex, agentIndex);

                    queues[gateIndex].Add(agentIndex);
                    usedAgents.Add(agentIndex);
                    exclusionListEoq.Clear();
                    exclusionListUnequal.Clear();

                    CheckSwapAt(gateIndex);
                }
            }

            return queues;
        }

        private static int MaybeFindCloserEmptyGate(int gateIndex, int agentIndex)
        {
            float minDistance = float.MaxValue;
            int closestEmptyGate = -1;

            for (int i = 0; i < gatePositions.Count; i++)
            {
                if (queues[i].Count > 0) continue;

                float distance = DistanceBetween(gatePositions[i], agentPositions[agentIndex]);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEmptyGate = i;
                }
            }

            return closestEmptyGate == -1 ? gateIndex : closestEmptyGate;
        }

        private static bool CheckUnequalQueues()
        {
            int maxQueueLength = int.MinValue;
            int maxQueueIndex = 0;

            for (int i = 0; i < queues.Count; i++)
            {
                if (queues[i].Count > maxQueueLength)
                {
                    maxQueueIndex = i;
                    maxQueueLength = queues[i].Count;
                }
            }

            float closestMinQueueDistance = int.MaxValue;
            int closestMinQueueIndex = -1;

            for (int i = 0; i < queues.Count; i++)
            {
                if (queues[i].Count <= maxQueueLength - queueHysteresis)
                {
                    float distance = DistanceBetween(EoQpos(i), EoQpos(maxQueueIndex));

                    if (distance <= closestMinQueueDistance)
                    {
                        closestMinQueueDistance = distance;
                        closestMinQueueIndex = i;
                    }
                }
            }

            if (closestMinQueueIndex == -1)
                return false;

            var EoMinQ = EoQpos(closestMinQueueIndex);
            var usedLargeQueueIndex = new List<int>();

            bool swapSuccess = false;

            while (true)
            {
                // Find closest agent in large queue (excl used)
                float minDistance = float.MaxValue;
                int queueIndexToTryPull = -1;
                for (int i = 1; i < queues[maxQueueIndex].Count; i++)
                {
                    if (usedLargeQueueIndex.Contains(i)) continue;

                    float distance = DistanceBetween(EoMinQ, agentPositions[queues[maxQueueIndex][i]]);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        queueIndexToTryPull = i;
                    }
                }

                if (queueIndexToTryPull == -1)
                    break;

                // Check line segment 1
                if (CrossesExistingQueue(EoMinQ, agentPositions[queues[maxQueueIndex][queueIndexToTryPull]]))
                {
                    usedLargeQueueIndex.Add(queueIndexToTryPull);
                    continue;
                }

                // If its not at the end of the queue
                if (queueIndexToTryPull < queues[maxQueueIndex].Count - 1)
                {
                    // Check line segment 2
                    if (CrossesExistingQueue(agentPositions[queues[maxQueueIndex][queueIndexToTryPull - 1]], agentPositions[queues[maxQueueIndex][queueIndexToTryPull + 1]]))
                    {
                        usedLargeQueueIndex.Add(queueIndexToTryPull);
                        continue;
                    }

                    var midAgent = agentPositions[queues[maxQueueIndex][queueIndexToTryPull]];
                    var postAgent = agentPositions[queues[maxQueueIndex][queueIndexToTryPull + 1]];
                    var preAgent = agentPositions[queues[maxQueueIndex][queueIndexToTryPull - 1]];

                    // Finally check if the two new line segments will cross
                    if (LineSegment.LineSegementsIntersect(
                        new Vector(EoMinQ.X, EoMinQ.Y), new Vector(midAgent.X, midAgent.Y),
                        new Vector(postAgent.X, postAgent.Y), new Vector(preAgent.X, preAgent.Y)))
                    {
                        usedLargeQueueIndex.Add(queueIndexToTryPull);
                        continue;
                    }
                }

                // Now able to swap
                queues[closestMinQueueIndex].Add(queues[maxQueueIndex][queueIndexToTryPull]);
                queues[maxQueueIndex].RemoveAt(queueIndexToTryPull);
                swapSuccess = true;
                break;
            }

            if (!swapSuccess)
                exclusionListUnequal.Add(maxQueueIndex);

            return swapSuccess;
        }

        private static void CheckSwapAt(int gateIndex)
        {
            int EoQ_ID = queues[gateIndex].Count - 1;

            if (EoQ_ID < 1)
                return;

            if (DistanceBetween(gatePositions[gateIndex], agentPositions[queues[gateIndex][EoQ_ID]]) >
                DistanceBetween(gatePositions[gateIndex], agentPositions[queues[gateIndex][EoQ_ID - 1]]))
                return;

            bool canSwap = false;

            if (EoQ_ID > 1)
            {
                canSwap = !CrossesExistingQueue(
                    agentPositions[queues[gateIndex][EoQ_ID]],
                    agentPositions[queues[gateIndex][EoQ_ID - 2]]);
            }
            else
            {
                canSwap = !CrossesExistingQueue(
                    agentPositions[queues[gateIndex][EoQ_ID]],
                    gatePositions[gateIndex]);
            }

            if (canSwap)
            {
                int lastAgent = queues[gateIndex][EoQ_ID];
                queues[gateIndex][EoQ_ID] = queues[gateIndex][EoQ_ID - 1];
                queues[gateIndex][EoQ_ID - 1] = lastAgent;
            }
        }

        private static int FindAgentToAddToEOQ(int gateIndex)
        {
            var EoQPos = EoQpos(gateIndex);

            float smallestDistance = float.MaxValue;
            int closestValidAgent = -1;

            for (int i = 0; i < agentPositions.Count; i++)
            {
                if (usedAgents.Contains(i)) continue;

                var distance = DistanceBetween(EoQPos, agentPositions[i]);

                if (distance < smallestDistance && !CrossesExistingQueue(EoQPos, agentPositions[i]))
                {
                    smallestDistance = distance;
                    closestValidAgent = i;
                }
            }

            return closestValidAgent;
        }

        /// <summary>
        /// Finds the smallest queue size then randomly picks a queue of that size.
        /// </summary>
        /// <returns></returns>
        private static int PickMostEmptyGate(List<List<int>> queues)
        {
            int smallestQueueSize = int.MaxValue;

            for (int i = 0; i < queues.Count; i++)
            {
                if (queues[i].Count < smallestQueueSize && !exclusionListEoq.Contains(i))
                    smallestQueueSize = queues[i].Count;
            }

            List<int> possibleQueues = new List<int>();

            for (int i = 0; i < queues.Count; i++)
                if (queues[i].Count == smallestQueueSize)
                    possibleQueues.Add(i);

            if (possibleQueues.Count == 0)
                return -1;

            int possibleQueueIndex = rand.Next(possibleQueues.Count);

            return possibleQueues[possibleQueueIndex];
        }

        private static float DistanceBetween(CpmPair point1, CpmPair point2)
        {
            return Utils.DistanceBetween(point1.X, point1.Y, point2.X, point2.Y);
        }

        private static CpmPair EoQpos(int gateIndex)
        {
            if (queues[gateIndex].Count == 0)
                return gatePositions[gateIndex];

            return agentPositions[queues[gateIndex].Last()];
        }

        private static bool CrossesExistingQueue(CpmPair point1, CpmPair point2)
        {
            foreach (var wall in wallsList)
            {
                if (LineSegment.LineSegementsIntersect(
                    new Vector(wall.point1.X, wall.point1.Y),
                    new Vector(wall.point2.X, wall.point2.Y),
                    new Vector(point1.X, point1.Y),
                    new Vector(point2.X, point2.Y)))
                    return true;
            }

            for (int gateId = 0; gateId < queues.Count; gateId++)
            {
                for (int i = 0; i < queues[gateId].Count; i++)
                {
                    if (i == 0)
                    {
                        if (LineSegment.LineSegementsIntersect(
                            new Vector(gatePositions[gateId].X, gatePositions[gateId].Y),
                            new Vector(agentPositions[queues[gateId][0]].X, agentPositions[queues[gateId][0]].Y),
                            new Vector(point1.X, point1.Y),
                            new Vector(point2.X, point2.Y)))
                            return true;
                    }
                    else
                    {
                        if (LineSegment.LineSegementsIntersect(
                            new Vector(agentPositions[queues[gateId][i - 1]].X, agentPositions[queues[gateId][i - 1]].Y),
                            new Vector(agentPositions[queues[gateId][i]].X, agentPositions[queues[gateId][i]].Y),
                            new Vector(point1.X, point1.Y),
                            new Vector(point2.X, point2.Y)))
                            return true;
                    }
                }
            }

            return false;
        }
    }

    public class LineSeg
    {
        public CpmPair point1;
        public CpmPair point2;

        public LineSeg(CpmPair point1, CpmPair point2)
        {
            this.point1 = point1;
            this.point2 = point2;
        }
    }
}
