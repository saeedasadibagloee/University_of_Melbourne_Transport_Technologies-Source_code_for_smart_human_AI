using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using Assets.Scripts;
using Core.GateChoice;
using Core.Handlers;
using Core.Logger;
using DataFormats;
using Domain;
using Domain.ElementFinder;
using Domain.Elements;
using Domain.Level;
using Domain.Room;
using Helper;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace Core
{
    
    public class Parallel
    {
        public static void ForEach<T>(IEnumerable<T> items, Action<T> action)
        {
            ThreadPool.SetMaxThreads(Environment.ProcessorCount, Environment.ProcessorCount);

            List<ManualResetEvent> resetEvents = new List<ManualResetEvent>();

            foreach (var item in items)
            {
                var evt = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(i =>
                {
                    action((T)i);
                    evt.Set();
                }, item);
                resetEvents.Add(evt);
            }

            foreach (var re in resetEvents)
                re.WaitOne();
        }
    }

    public static class Params
    {
        public static bool panicMode = false;

        public static Variables Current = new Variables(GenType.NormalDefault);
        public static Variables CurrentDefaults = new Variables(GenType.NormalDefault);

        public static Variables Maximum = new Variables(GenType.NormalMax);
        public static Variables Minimum = new Variables(GenType.NormalMin);
        
        public static void ApplyMode(bool normalMode)
        {
            panicMode = !normalMode;

            CurrentDefaults = new Variables(normalMode ? GenType.NormalDefault : GenType.PanicDefault);
            Current.SetType(normalMode ? GenType.NormalDefault : GenType.PanicDefault);
            Maximum =         new Variables(normalMode ? GenType.NormalMax : GenType.PanicMax);
            Minimum =         new Variables(normalMode ? GenType.NormalMin : GenType.PanicMin);
            VariablePresetHandler.Instance.UpdateAllFields();
        }
        
        public enum GenType
        {
            PanicDefault, PanicMax, PanicMin, NormalDefault, NormalMax, NormalMin
        }

        public static string GetModeTxt()
        {
            if (panicMode)
                return "PANIC";
            return "NORMAL";
        }
    }

    /// <summary>
    /// Variables inside this class are saved inside the model file.
    /// </summary>
    [Serializable]
    public class Variables
    {
        public bool WidenPaths = false;
        public bool TrainEnabled = false;
        public bool TicketGateQueuesEnabled = false;
        public bool ThreatsEnabled = false;
        public bool FireEnabled = true;
        public bool QueueAroundCorners = false;

        #region Reaction Time
        public Def.ReactionMethod ReactionMethod = Def.ReactionMethod.WeibullHazard;

        public float Radius_RT = 2.0f;
        public float Beta1 = 0.3f;
        public float Beta2 = 0.6f;

        public float Exp_Hazard_Mu = -0.2f;
        public float Exp_Hazard_Lambda = 5.0f;

        public float Weibull_Hazard_Mu = -0.15f;
        public float Weibull_Hazard_Nu = 1.5f;
        public float Weibull_Hazard_Lambda = 1;

        public float VarianceMultiplierCutoff = 20.0f;
        public int CutoffMaxRetries = 1000;
        #endregion

        #region Decision Update
        /// <summary>
        /// Utility multiplier for (CONG / Min(CONG)) in the decision update process.
        /// </summary>
        public float CongestionVarianceUtility = 3.5f;
        /// <summary>
        /// Utility multiplier for whether any gates are visible in the decision update process.
        /// </summary>
        public float GateVisibityUtility = 0.8f;
        /// <summary>
        /// Utility multiplier for whether any agents have decided to leave this gate in the decision update process.
        /// </summary>
        public float SocialInfluenceUtility = 0.8f;
        /// <summary>
        /// How long the social influence factor records for. Default 3000 (3 sec).
        /// </summary>
        public int SocialInfluenceTime = 4000;
        /// <summary>
        /// Utility for intertia constant in the decision update process.
        /// </summary>
        public float InertiaUtility = -7.5f;
        public float DUSpeedUpper = 3f;
        public float DUSpeedLower = 0f;
        public float DUDensityUpper = 3f;
        public float DUDensityLower = 1f;
        #endregion

        public bool CollisionAvoidanceOLD = false;

        public float DestinationLevelTheta1 = -0.5f;
        public float DestinationLevelTheta2 = -0.5f;

        public float Theta = 0.5f;
        public float AgentMaxspeed = 3.0f;
        public float GroupSpeedMultiplier = 0.8f;
        public float AgentMaxspeedDeviation = 0f;

        /// <summary>
        /// Follower slows down by this factor if he is ahead of his leader.
        /// </summary>
        public float AgentFollowerInFrontSpeed = 0.6f;

        public float AgentEscalatorSpeed = 0.225f;

        public float AgentRadius = 0.20f;
        public float AgentRadiusDeviation = 0f;

        public float AgentWeight = 71f;
        public float AgentWeightDeviation = 0f;

        public float TimeStep = 0.001f;                     // Core
        public float DefaultReactionTime = 0.1f;           // Core
        public float RandomSeed = DateTime.Now.Millisecond;

        public float MaxRadius = 0.2f;                      // Utils

        public float DefaultSafeWallDistance = 0.25f;       // Wall Force
        public int DefaultWallForce = 1000;                 // Wall Force

        public double DensityFactor = 3.82842;              // Neighbor
        public int DefaultAlpha1NormalForce = 200;          // Neighbor
        public int DefaultAlpha2NormalForce = 170000;       // Neighbor
        public int DefaultFrictionalForce = 7000;           // Neighbor
        public float DefaultRepultionDistance = 0.56f;       // Neighbor
        public int DefaultAttractiveForce = 2;              // Neighbor
        public int DefaultRepulsiveForce = 3000;            // Neighbor
        public float NeighborRadius = 1.5f;                 // Neighbor

        #region Gate Choice
        public Def.Function FunctionType = Def.Function.Utility;
        public Def.Class ClassType = Def.Class.Single;
        public Def.Interaction InterationType = Def.Interaction.Enabled;
        public bool SpaceSyntaxEnabled = false;
        public Utilities UtilitiesClass1 = new Utilities();
        public Utilities UtilitiesClass2 = new Utilities();
        #endregion

        public int UiUpdateCycle = 30;
        public int RVOUpdateCycle = 16;
        public int AgentUpdateCycle = 1000;
        public int GroupCheckCycle = 50;
        public int DecisionUpdateCycle = 100;

        public uint PenaltyWeight = 4500;
        public float AgentWallPaddingDistance = 0.2f;

        public float SpeedFlowThreshold = 1f;
        public float DensityFlowThreshold = 2f;

        public float CongestionRadius = 4f;
        public float RvoTimeHorizonAgent = 8;       // How far into the future to look for collisions with other agents (in seconds)
        public float RvoTimeHorizonObstacle = 3;    // How far into the future to look for collisions with obstacles (in seconds)
        public int RvoMaxNeighbours = 18;           // Max number of other agents to take into account. A smalL value can reduce CPU load, a high value can lead to better avoidance quality.

        public float HFFatigue = -0.02721f;
        public float HFHeightLimit = 39.84f;
        public float HFIntialVelocity = 1.759f;
        public float HFSteadySpeed = 0.68f;

        public bool RecordingEnabled = true;

        public Variables(Params.GenType generationType)
        {
            SetType(generationType);
        }

        public void SetType(Params.GenType generationType)
        {
            // Set normal specific values.
            if (generationType == Params.GenType.NormalDefault || generationType == Params.GenType.NormalMax ||
                generationType == Params.GenType.NormalMin)
            {
                TrainEnabled = true;
                WidenPaths = true;
                FireEnabled = false;
                TicketGateQueuesEnabled = true;
                ReactionMethod = Def.ReactionMethod.None;
            }

            switch (generationType)
            {
                case Params.GenType.PanicDefault:
                    // Do nothing, defaults already set.
                    break;
                case Params.GenType.PanicMin:
                    Weibull_Hazard_Mu = -0.3f;
                    Weibull_Hazard_Nu = 1.5f;
                    Weibull_Hazard_Lambda = 0.5f;
                    CongestionVarianceUtility = 2f;
                    GateVisibityUtility = 0.5f;
                    SocialInfluenceUtility = 0.5f;
                    SocialInfluenceTime = 2000;
                    InertiaUtility = -9;
                    AgentMaxspeed = 2.5f;
                    DefaultReactionTime = 0.08f;
                    DefaultFrictionalForce = 5500;

                    UtilitiesClass1._distanceUtility = -0.4f;
                    UtilitiesClass1._congestionUtility = -0.3f;
                    UtilitiesClass1._fltovis = -0.04f;
                    UtilitiesClass1._fltoinvis = 0f;
                    UtilitiesClass1._visibilityUtility = 0.5f;
                    UtilitiesClass2 = UtilitiesClass1.Copy();
                    break;
                case Params.GenType.PanicMax:
                    Weibull_Hazard_Mu = -0.05f;
                    Weibull_Hazard_Nu = 1.5f;
                    Weibull_Hazard_Lambda = 1.5f;
                    CongestionVarianceUtility = 5f;
                    GateVisibityUtility = 2f;
                    SocialInfluenceUtility = 2f;
                    SocialInfluenceTime = 5000;
                    InertiaUtility = -6.5f;
                    AgentMaxspeed = 3.5f;
                    DefaultReactionTime = 0.12f;
                    DefaultFrictionalForce = 8000;
                    TimeStep = 0.004f;

                    UtilitiesClass1._distanceUtility = -0.1f;
                    UtilitiesClass1._congestionUtility = -0.05f;
                    UtilitiesClass1._fltovis = 0f;
                    UtilitiesClass1._fltoinvis = 0.1f;
                    UtilitiesClass1._visibilityUtility = 1.5f;
                    UtilitiesClass2 = UtilitiesClass1.Copy();
                    break;
                case Params.GenType.NormalDefault:
                    CongestionVarianceUtility = 2.5f;
                    GateVisibityUtility = 0.6f;
                    SocialInfluenceUtility = 0.5f;
                    SocialInfluenceTime = 4000;
                    InertiaUtility = -8.5f;
                    AgentMaxspeed = 1.5f;
                    DefaultReactionTime = 0.09f;
                    DefaultFrictionalForce = 5000;

                    UtilitiesClass1._distanceUtility = -0.700f;
                    UtilitiesClass1._congestionUtility = -0.2f;
                    UtilitiesClass1._fltovis = -0.185f;
                    UtilitiesClass1._fltoinvis = -0.08f;
                    UtilitiesClass1._visibilityUtility = 2.20f;
                    UtilitiesClass2 = UtilitiesClass1.Copy();
                    break;
                case Params.GenType.NormalMin:
                    CongestionVarianceUtility = 1.5f;
                    GateVisibityUtility = 0f;
                    SocialInfluenceUtility = 0f;
                    SocialInfluenceTime = 2000;
                    InertiaUtility = -11f;
                    AgentMaxspeed = 1.2f;
                    DefaultReactionTime = 0.08f;
                    DefaultFrictionalForce = 4000;

                    UtilitiesClass1._distanceUtility = -0.9f;
                    UtilitiesClass1._congestionUtility = -0.3f;
                    UtilitiesClass1._fltovis = -0.3f;
                    UtilitiesClass1._fltoinvis = -0.5f;
                    UtilitiesClass1._visibilityUtility = 0.4f;
                    UtilitiesClass2 = UtilitiesClass1.Copy();
                    break;
                case Params.GenType.NormalMax:
                    CongestionVarianceUtility = 3.0f;
                    GateVisibityUtility = 1f;
                    SocialInfluenceUtility = 0.8f;
                    SocialInfluenceTime = 5000;
                    InertiaUtility = -7.5f;
                    AgentMaxspeed = 2.2f;
                    DefaultReactionTime = 0.11f;
                    DefaultFrictionalForce = 6000;
                    TimeStep = 0.004f;

                    UtilitiesClass1._distanceUtility = -0.6f;
                    UtilitiesClass1._congestionUtility = -0.05f;
                    UtilitiesClass1._fltovis = -0.07f;
                    UtilitiesClass1._fltoinvis = 0f;
                    UtilitiesClass1._visibilityUtility = 2.5f;
                    UtilitiesClass2 = UtilitiesClass1.Copy();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("generationType", generationType, null);
            }
        }

        public Variables()
        {
        }
    }

    /// <summary>
    /// These variables are ones that do not need to be save in the model file.
    /// </summary>
    internal class Constants
    {
        public static int NCores = Mathf.Clamp(Environment.ProcessorCount - 2, 2, 6);
        // The reason to -2 is we need to take into account main thread and gridUpdater thread
        // 6 threads seems to be the optimal

        public static float Bottomfloor = -1f;
        public static float Floorheight = 2.5f;

        public static float NeighborRadiusSqrd = 2.25f;

        public static int PenaltyWeightUpdateCycle = 50;
        public static int StoppingUpdateCycle = 100;
        public static int PathfindingUpdateCycle = -1;

        public const int PathfindingUpdateCycleInitial = 500;

        public const float AgentWallpaddingdistance = 0.2f;
    }

  

            return random * (max - min) + min;
        }

        public static float GetNormalizedValue(float sigma, float deviation)
        {
            var u1 = GetNextRnd();
            var u2 = GetNextRnd();

            return (float)(sigma + deviation * (Math.Sqrt(-2 * Math.Log10(u1)) * Math.Cos(2 * Math.PI * u2)));
        }

        public static bool AgentIsNeighbor(float agentX, float agentY, float candidateX, float candidateY)
        {
            var x = candidateX - agentX;
            var y = candidateY - agentY;

            var deltaX = x * x;
            var deltaY = y * y;

            var sum = deltaX + deltaY;

            return sum < Constants.NeighborRadiusSqrd;
        }

        public static float DistanceBetween(float x1, float y1, float x2, float y2)
        {
            var x = x1 - x2;
            var y = y1 - y2;

            var deltaX = x * x;
            var deltaY = y * y;

            return (float)Math.Sqrt(deltaX + deltaY);
        }

        /// <summary>
        /// Returns sign of determinant between Vectors A & B with query point M.
        /// </summary>
        /// <returns>1 if M is on right of Vector AB, 0 of M is on left.</returns>
        public static int SignOfDeterminant(Vertex A, Vertex B, Vertex M)
        {
            return (int)Mathf.Sign((B.X - A.X) * (M.Y - A.Y) - (B.Y - A.Y) * (M.X - A.X));
        }

        public static CpmPair GenerateLocation(List<Vertex> vertices, List<RoomElement> walls)
        {
            CpmPair location = new CpmPair();

            PointF[] points = new PointF[vertices.Count];
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;

            for (int i = 0; i < vertices.Count; i++)
            {
                float x = vertices[i].X;
                float y = vertices[i].Y;

                points[i] = new PointF(x, y);

                if (x < xMin)
                    xMin = x;
                if (x > xMax)
                    xMax = x;
                if (y < yMin)
                    yMin = y;
                if (y > yMax)
                    yMax = y;
            }

            PolygonHelper poly = new PolygonHelper(points);

            for (int i = 0; i < 1000; i++)
            {
                float x = GetNextRnd(xMin, xMax);
                float y = GetNextRnd(yMin, yMax);

                if (poly.PointInPolygon(x, y) && PointAwayFromWalls(x, y, walls))
                {
                    location.X = x;
                    location.Y = y;
                    return location;
                }
            }

            Debug.LogError("Tried 1000 times to find a point in the polygon, could not.");

            return location;
        }

        private static bool PointAwayFromWalls(float x, float y, List<RoomElement> walls)
        {
            if (walls != null)
            {
                foreach (var wall in walls)
                {
                    CpmPair point = new CpmPair(x, y);
                    CpmPair vStart = new CpmPair(wall.VStart.X, wall.VStart.Y);
                    CpmPair vEnd = new CpmPair(wall.VEnd.X, wall.VEnd.Y);
                    CpmPair closestWallPoint = MyTarget(ref point, ref vStart, ref vEnd);

                    if (DistanceBetween(closestWallPoint.X, closestWallPoint.Y, x, y) < Params.Current.AgentWallPaddingDistance)
                        return false;
                }
            }

            return true;
        }

        public static bool PointIsInside(float x, float y, List<Vertex> pVertices)
        {
            bool result = false;
            var nVertices = pVertices.Count;

            for (var i = 0; i < nVertices; ++i)
            {
                var j = (i + 1) % nVertices;

                if (
                    (pVertices[j].Y <= y && y < pVertices[i].Y ||
                    pVertices[i].Y <= y && y < pVertices[j].Y) &&
                        // is p to the left of edge?
                        x < pVertices[j].X + (pVertices[i].X - pVertices[j].X) * (y - pVertices[j].Y) /
                        (pVertices[i].Y - pVertices[j].Y)
                    )
                    result = !result;
            }

            return result;
        }

        public static bool PointIsInside(float x, float y, List<Vertex> poly,
                                         int currentRoomIndex, int testingRoomIndex)
        {
            var coef = poly.Skip(1).Select((p, i) =>
                                            (x - poly[i].X) * (p.X - poly[i].X)
                                          - (x - poly[i].X) * (p.Y - poly[i].Y))
                                    .ToList();

            if (coef.Any(p => p == 0))
            {
                LogWriter.Instance.WriteToLog("on the line");
                LogWriter.Instance.WriteToLog("current room index: " + currentRoomIndex);
                LogWriter.Instance.WriteToLog("testing room index: " + testingRoomIndex);

                if (currentRoomIndex != testingRoomIndex)
                    return true;
                return false;
            }

            for (int i = 1; i < coef.Count(); i++)
            {
                if (coef[i] * coef[i - 1] < 0)
                    return false;
            }

            return true;
        }

        public static int ClosestGateIndex(float x, float y, List<RoomElement> gates)
        {
            var gatesCount = gates.Count;
            var dis2Gates = new SortedDictionary<float, int>();

            if (gates.Count == 0)
            {
                Debug.Log("Gates can't be empty");
                return 0;
            }

            for (var index = 0; index < gatesCount; ++index)
            {
                float distance = DistanceBetween(x, y, gates[index].VMiddle.X, gates[index].VMiddle.Y);
                if (!dis2Gates.ContainsKey(distance))
                    dis2Gates.Add(distance, index);
            }

            return dis2Gates.First().Value;
        }

        public static bool GateIsVisible(float agentX, float agentY, RoomElement gate, List<RoomElement> obstacles, List<RoomElement> poles, List<RoomElement> gates = null)
        {
            Line agentToGate = new Line(agentX, agentY, gate.VMiddle.X, gate.VMiddle.Y);
            Vertex point = null;

            foreach (var obstacle in obstacles)
            {
                if (obstacle.IsIWLWG() || obstacle.IsLow()) continue;

                Line line = new Line(obstacle.VStart.X, obstacle.VStart.Y, obstacle.VEnd.X, obstacle.VEnd.Y);
                bool hasIntersect = agentToGate.Intersection(line, out point);

                if (hasIntersect && line.PointWithinBounds(point) && agentToGate.PointWithinBounds(point))
                    return false;
            }

            foreach (var pole in poles)
            {
                Line line = new Line(pole.VMiddle.X, pole.VMiddle.Y, pole.Length, agentToGate);
                bool hasIntersect = agentToGate.Intersection(line, out point);

                if (hasIntersect && line.PointWithinBounds(point) && agentToGate.PointWithinBounds(point))
                    return false;
            }

            if (gates != null)
            {
                foreach (var otherGate in gates)
                {
                    if (otherGate.Equals(gate) || otherGate.ElementId == gate.ElementId)
                        continue;

                    Line line = new Line(otherGate.VStart.X, otherGate.VStart.Y, otherGate.VEnd.X, otherGate.VEnd.Y);
                    bool hasIntersect = agentToGate.Intersection(line, out point);

                    if (hasIntersect && line.PointWithinBounds(point) && agentToGate.PointWithinBounds(point))
                        return false;
                }
            }

            return true;
        }

        public static bool PointIsVisible(float agentX, float agentY, float X, float Y, List<RoomElement> obstacles, List<RoomElement> poles, List<RoomElement> gates = null)
        {
            Line agentToGate = new Line(agentX, agentY, X, Y);
            Vertex point = null;

            foreach (var obstacle in obstacles)
            {
                if (obstacle.IsIWLWG() || obstacle.IsLow()) continue;

                Line line = new Line(obstacle.VStart.X, obstacle.VStart.Y, obstacle.VEnd.X, obstacle.VEnd.Y);
                bool hasIntersect = agentToGate.Intersection(line, out point);

                if (hasIntersect && line.PointWithinBounds(point) && agentToGate.PointWithinBounds(point))
                    return false;
            }

            foreach (var pole in poles)
            {
                Line line = new Line(pole.VMiddle.X, pole.VMiddle.Y, pole.Length, agentToGate);
                bool hasIntersect = agentToGate.Intersection(line, out point);

                if (hasIntersect && line.PointWithinBounds(point) && agentToGate.PointWithinBounds(point))
                    return false;
            }

            if (gates != null)
            {
                foreach (var otherGate in gates)
                {
                    Line line = new Line(otherGate.VStart.X, otherGate.VStart.Y, otherGate.VEnd.X, otherGate.VEnd.Y);
                    bool hasIntersect = agentToGate.Intersection(line, out point);

                    if (hasIntersect && line.PointWithinBounds(point) && agentToGate.PointWithinBounds(point))
                        return false;
                }
            }

            return true;
        }

        public static int ProbabilisticIndex(List<float> probabilities, double randomValue)
        {
            //Determine index
            var index = 0;
            var nProbSize = probabilities.Count;

            var leftBoundary = 0f;//probabilities[0];

            for (var pEndIndex = 0; pEndIndex < nProbSize; ++pEndIndex)
            {
                var rightBoudary = leftBoundary + probabilities[pEndIndex];

                if (randomValue > leftBoundary && randomValue < rightBoudary)
                {
                    index = pEndIndex;
                    break;
                }

                if (pEndIndex == nProbSize - 1 && randomValue > rightBoudary && rightBoudary < 1.0)
                {
                    index = pEndIndex;
                    break;
                }

                leftBoundary = rightBoudary;
            }

            return index;
        }

        public static CpmPair MyTarget(ref CpmPair aPoint,
                                              ref CpmPair lineP1,
                                              ref CpmPair lineP2,
                                              bool absolute = true)
        {
            float u1, u2, v1, v2, scalar;
            CpmPair result;

            v1 = lineP2.X - lineP1.X;
            v2 = lineP2.Y - lineP1.Y;
            u1 = aPoint.X - lineP1.X;
            u2 = aPoint.Y - lineP1.Y;

            scalar = (u1 * v1 + u2 * v2) / (v1 * v1 + v2 * v2);

            result.X = scalar * v1 + lineP1.X;
            result.Y = scalar * v2 + lineP1.Y;

            if (absolute) return result;

            if (lineP1.Y < lineP2.Y)
            {
                if (result.Y < lineP1.Y)
                {
                    result.Y = lineP1.Y + Params.Current.MaxRadius * 2;
                    result.X = (result.Y - lineP1.Y) * (lineP2.X - lineP1.X) / (lineP2.Y - lineP1.Y) + lineP1.X;
                }
                else
                {
                    if (result.Y > lineP2.Y)
                    {
                        result.Y = lineP2.Y - Params.Current.MaxRadius * 2;
                        result.X = (result.Y - lineP1.Y) * (lineP2.X - lineP1.X) / (lineP2.Y - lineP1.Y) + lineP1.X;
                    }
                }
            }

            if (lineP1.Y > lineP2.Y)
            {
                if (result.Y > lineP1.Y)
                {
                    result.Y = lineP1.Y - Params.Current.MaxRadius * 2;
                    result.X = (result.Y - lineP1.Y) * (lineP2.X - lineP1.X) / (lineP2.Y - lineP1.Y) + lineP1.X;
                }
                else
                {
                    if (result.Y < lineP2.Y)
                    {
                        result.Y = lineP2.Y + Params.Current.MaxRadius * 2;
                        result.X = (result.Y - lineP1.Y) * (lineP2.X - lineP1.X) / (lineP2.Y - lineP1.Y) + lineP1.X;
                    }
                }
            }
            if (lineP1.Y == lineP2.Y)
            {
                result.Y = lineP1.Y;
                if (result.X >= lineP1.X && result.X >= lineP2.X)
                {
                    if (lineP1.X > lineP2.X) result.X = lineP1.X - Params.Current.MaxRadius * 2;
                    else result.X = lineP2.X - Params.Current.MaxRadius * 2;
                }
                if (result.X <= lineP1.X && result.X <= lineP2.X)
                {
                    if (lineP1.X < lineP2.X) result.X = lineP1.X + Params.Current.MaxRadius * 2;
                    else result.X = lineP2.X + Params.Current.MaxRadius * 2;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns a list of gates in a specific level, either destination or intermediate.
        /// </summary>
        /// <param name="destinationOnly">Setting this to true will return destination gates only, otherwise only intermediate gates will be returned.</param>
        /// <returns></returns>
        public static List<int> ListOfGates(Level level, bool destinationOnly = true)
        {
            List<RoomElement> generalElements = new List<RoomElement>();

            foreach (var wall in level.wall_pkg.walls)
            {
                if (wall.vertices.Count < 2) //there should be 2 points
                    return new List<int>();

                var newWall = Builder.CreateWall(wall);
                generalElements.Add(newWall);
         

            List<List<int>> regions = new List<List<int>>();

            foreach (var room in roomRegions)
            {
                List<int> newRegion = new List<int>();
                newRegion.AddRange(room);
                regions.Add(newRegion);
            }

            int roomID = 0;
            List<SimpleRoom> rooms = new List<SimpleRoom>();

            foreach (var region in regions)
            {
                var regionSize = region.Count;
                //we must have at least 3 vertices to create a room
                if (regionSize < 3)
                    continue;

                var intialVertixId = region[0];
                var vertexStartId = intialVertixId;

                SimpleRoom newRoom = new SimpleRoom(roomID++);
                newRoom.AssignCoordinates(Finder.FindAllCoordinates(generalElements, region));

                for (var index = 1; index < regionSize; ++index)
                {
                    var vertexEndId = region[index];
                    var roomElement = Finder.Find(generalElements, vertexStartId, vertexEndId);

                    if (roomElement != null)
                        newRoom.AddElement(roomElement);

                    if (index == regionSize - 1) //last vertex
                    {
                        if (region[index] != intialVertixId)
                        {
                            var rElement = Finder.Find(generalElements, vertexEndId, intialVertixId);
                            if (rElement != null) newRoom.AddElement(rElement);
                        }
                    }

                    vertexStartId = vertexEndId;
                }

                rooms.Add(newRoom);
            }

            List<int> listOfGates = new List<int>();

            foreach (var room in rooms)
            {
                var roomGates = room.Gates;
                var remainingRooms = rooms.Where(pRoom => pRoom.RoomId != room.RoomId).ToList();

                foreach (var roomGate in roomGates)
                {
                    if (destinationOnly)
                    {
                        if (remainingRooms.Any(pRoom => pRoom.Gates.Any(pGate => pGate.ElementId == roomGate.ElementId)))
                            continue;

                        var innerGateType = roomGate.GetInnerType();

                        if (innerGateType != InnerType.StairwayGateDown || innerGateType != InnerType.StairwayGateUp)
                            listOfGates.Add(roomGate.ElementId);
                    }
                    else
                    {
                        if (!listOfGates.Contains(roomGate.ElementId) && remainingRooms.Any(pRoom => pRoom.Gates.Any(pGate => pGate.ElementId == roomGate.ElementId)))
                            listOfGates.Add(roomGate.ElementId);
                    }
                }
            }

            return listOfGates;
        }

        public static List<List<int>> DestinationGates(Model model)
        {
            var gatesListList = new List<List<int>>();

            foreach (var level in model.levels)
                gatesListList.Add(ListOfGates(level));

            return gatesListList;
        }

        public static List<List<int>> IntermediateGates(Model model)
        {
            var gatesListList = new List<List<int>>();

            foreach (var level in model.levels)
                gatesListList.Add(ListOfGates(level, false));

            return gatesListList;
        }

    }

    internal struct TypeSizeProxy<T>
    {
        public T PublicField;
    }

    internal static class SizeCalculator
    {
        public static int SizeOf<T>()
        {
            try
            {
                return Marshal.SizeOf(typeof(T));
            }
            catch (ArgumentException)
            {
                return Marshal.SizeOf(new TypeSizeProxy<T>());
            }
        }

        public static int GetSize(this object obj)
        {
            return Marshal.SizeOf(obj);
        }
    }

    internal class Line
    {
        internal bool IsVertical;
        internal float A;
        internal float C;

        internal float Xmin = float.MaxValue;
        internal float Xmax = float.MinValue;
        internal float Ymin = float.MaxValue;
        internal float Ymax = float.MinValue;

        public Line()
        {
            IsVertical = false;
            A = 1;
            C = 0;
        }

        // Creates a line directly from two points.
        public Line(float x1, float y1, float x2, float y2)
        {
            Initialise(x1, y1, x2, y2);
        }

        private void Initialise(float x1, float y1, float x2, float y2)
        {
            if (x2 == x1)
                IsVertical = true;
            else
                A = (y2 - y1) / (x2 - x1);

            C = y1 - A * x1;

            SetBounds(x1, y1, x2, y2);
        }

        // Creates a line directly from the center, slope and radius (circle)
        public Line(float x, float y, float radius, Line other)
        {
            Vector rightPoint = new Vector(x + radius, y);
            Vector leftPoint = new Vector(x - radius, y);

            if (!other.IsVertical)
            {
                float radians = (float)Math.Atan(-1.0 / other.A);
                rightPoint.RotateAroundPoint(radians, x, y);
                leftPoint.RotateAroundPoint(radians, x, y);
            }

            Initialise(leftPoint.X, leftPoint.Y, rightPoint.X, rightPoint.Y);
        }

        public void SetBounds(float x1, float y1, float x2, float y2)
        {
            Xmin = Math.Min(x1, x2);
            Xmax = Math.Max(x1, x2);
            Ymin = Math.Min(y1, y2);
            Ymax = Math.Max(y1, y2);
        }

        public float Linear(float x)
        {
            if (IsVertical)
                return 0f;

            return A * x + C;
        }

        public bool Intersection(Line otherLine, out Vertex vertex)
        {
            float x = 0f;
            float y = 0f;
            vertex = null;

            if (IsVertical)
            {
                if (otherLine.IsVertical)
                    return false;
                x = Xmin;
                y = otherLine.Linear(x);
            }
            else
            {
                if (otherLine.IsVertical)
                {
                    x = otherLine.Xmin;
                    y = Linear(x);
                }
                else
                {
                    float b = otherLine.A;
                    float d = otherLine.C;

                    if (b == A)
                        return false;

                    x = (d - C) / (A - b);
                    y = A * (d - C) / (A - b) + C;
                }
            }

            vertex = new Vertex(x, y);
            return true;
        }

        public bool PointWithinBounds(Vertex v)
        {
            if (v.X <= Xmax && v.X >= Xmin)
                if (v.Y <= Ymax && v.Y >= Ymin)
                    return true;

            return false;
        }
    }
}

