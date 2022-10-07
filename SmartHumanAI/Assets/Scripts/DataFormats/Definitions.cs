using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using I2.Loc;
using Pathfinding;
using UnityEngine;

namespace DataFormats
{
    internal static class Str
    {
        public const string PolesTagString = "Poles";
        public const string PolesBarricadeTagString = "PolesBarricade";

        public const string Agent = "BarneyAgentLODLow(Clone)";
        public const string Wall = "Wall";
        public const string Gate = "Gate";
        public const string Danger = "Danger";
        public const string Danger1 = "Danger1";
        public const string CircularObstacle = "PoleObstacle";
        public const string Agents = "Agents";
        public const string Barricade = "Barricade";
        public const string WaitPoint = "WaitPoint";
        public const string Floor = "RoomFloor";
        public const string ErrorFlag = "ErrorFlag";
        public const string RoomFloor = "RoomFloor";
        public const string Stairs = "Stairs";
        public const string Stairs1 = "Stairs1";
        public const string Stairs2 = "Stairs2";
        public const string Seperator = "Seperator";
        public const string Landing = "Landing";
        public const string HalfLanding = "HalfLanding";
        public const string Straight = "Straight";
        public const string DecimalFormat = "0.###";
        public const string DegreesSymbol = "°";
        public const string MiddleString = "Gate Middle: ";
        public const string IDString = "ID: ";
        public const string Vertex1String = "Vertex 1: ";
        public const string Vertex2String = "Vertex 2: ";
        public const string LengthString = "Length: ";
        public const string RadiusString = "Radius: ";
        public const string AngleString = "Angle: ";
        public const string AreaString = "Room Area: ";
        public const string SqrMeter = " m²";
        public const string Meter = " m";
        public const string AvoidCircle = "AvoidCircle";
        public const string AvoidSquare = "AvoidSquare";
        public const string FireSource = "FireSource";
        public const string Train = "Train";
        public const string Tram = "Tram";
        public const string TicketGate1 = "Gate1";
        public const string TicketGate2 = "Gate2";
        public const string TicketGate = "TicketGate";
        public const string TrainTracks = "TrainTracks";
        public const string Level = "Level";
        public const string Escalator = "Escalator";
    }

    public static class Def
    {
        public static Color[] Colors = { Color.blue, Color.cyan, Color.green, Color.yellow, new Color(1, 137f / 255f, 0), Color.red, new Color(151f / 255f, 0, 0) };
        public static Color[] ColorsTwo = { Color.white, Color.blue };
        public static Color[] ColorsGrid = { Color.green, Color.red };
        public enum Mat { Wall, Gate, Barricade, Floor, CircularObstacle, Agent, Line, StairsHalfLanding, StairsStraight, AvoidCircle, Danger, GateDestination, FloorRed, ErrorRed, WaitPoint, Counter };
        public enum ColorDisplay { Off, Density, Speed, SpeedThreshold, DensityThreshold, JointThreshold, Groups }
        public enum Signal { SimulationStatus, AgentUpdate, InactiveAgents, GateSharesData, Heatmap, ThreatUpdate, ReactionTimeData, TimeData }
        public enum SimulationState { NotStarted, Started, Interrupted, Finished, Replaying }
        public enum AgentPlacement { Circle, Room, Rectangle, Random }
        public enum DistributionType { Static, Dynamic }
        public enum TimetableType { Discrete, Continuous, Poisson }
        public enum GroupStatus { No, Yes }
        public enum StairType { Unknown, Straight, HalfLanding, DoubleLanding, Winder, HalfWinder, Escalator }
        public enum StairDirection { Bidirectional, UpOnly, DownOnly }
        public enum TrainType { Train, Tram }
        public enum ThreatType { Unknown, GateObstruction, DangerInRoom, StairObstruction }
        public enum Object
        {
            None, Wall, Gate, Barricade, PoleObstacle, Agents, StairsHalfLanding, StairsStraight,
            AvoidCircle, GateObstruction, DangerInRoom, StairObstruction, AvoidSquare, AgentSquare, AgentSquare2,
            FireSource, Train, TicketGate, Tram, Prefabrication, Counter
        }
        public enum GcValues { Dist, Cong, Flow, Tovis, Toinvis, Vis, SSxVC, SSxA, SSxLoadButton }
        public enum Function { Utility, Regret }
        public enum Class { Single, Double }
        public enum Interaction { Disabled, Enabled }
        public enum HeatmapType { None, MaxDensity, AverageDensity, AverageSpeed, Utilization }
        public enum DialogType { Unknown, AgentPrefab, ThreatInfo, SimulationOptions, FireSource, TrainInfo, TicketGate, StairInfo, WaitPoint }
        public enum DangerArea { Unknown, Circle, Square }
        public enum ReactionMethod { WeibullHazard, ExpHazard, ExpDist, None }
    }

    internal static class Consts
    {
        internal const int maxNumAgentLimit = 30;
        internal const int NumberOfRunsDefault = 1;
        internal const int CancelAfterSecDefault = int.MaxValue;
        internal const int DangerWeightModifierDefault = 10000;
        internal const int MinusOne = -1;
        internal const float DensityRadius = 1.2f;
        internal const float MaxDensity = 6f;
        internal const float MinDensity = 0f;
        internal const float MinSpeed = 0f;
        internal const float MaxSpeed = 6f;
        internal const float MaxAgentsDensity = 6f;

        internal const float ButtonDiff = 1.5f;
        internal const int MaxSizeDialogPath = -1;

        internal const float FaceGateDistance = 4f;  //Radius around the gate in which agents will face it instead of their current path.
        internal const int SmoothLineDetail = 800;   //Set this to the detail of the curved line (default 1000)
        internal const float AnimMultiplier = 600;
        internal const float PathLookAhead = 0.048f;

        internal const bool extendWallColliderToPoles = true;
        internal const float TransparentAmount = 0.0f;
        internal const float LineWidth = 0.025f;
        internal const float LineWidthThin = 0.03f;
        internal const float LineWidthThick = 0.10f;
        internal const float MinCircularRadius = 0.5f;
        internal const float AgentCountToSmoothDampRatio = 0.0003f;

        internal static Color textColor = new Color(9f / 255, 65f / 255, 131f / 255);

        internal static bool holoLensEnabled = true;

        internal static readonly float[] endPoleUVs = { 0.1f, 0.0f, 0.2f, 0.0f, 0.3f, 0.0f, 0.4f, 0.0f, 0.5f, 0.0f, 0.6f, 0.0f, 0.7f, 0.0f, 0.8f, 0.0f, 0.9f, 0.0f, 1.0f, 0.0f, 0.1f, 0.0f, 0.2f, 0.0f, 0.3f, 0.0f, 0.4f, 0.0f, 0.5f, 0.0f, 0.6f, 0.0f, 0.7f, 0.0f, 0.8f, 0.0f, 0.9f, 0.0f, 1.0f, 0.0f, 0.1f, 1.0f, 0.2f, 1.0f, 0.3f, 1.0f, 0.4f, 1.0f, 0.5f, 1.0f, 0.6f, 1.0f, 0.7f, 1.0f, 0.8f, 1.0f, 0.9f, 1.0f, 1.0f, 1.0f, 0.1f, 1.0f, 0.2f, 1.0f, 0.3f, 1.0f, 0.4f, 1.0f, 0.5f, 1.0f, 0.6f, 1.0f, 0.7f, 1.0f, 0.8f, 1.0f, 0.9f, 1.0f, 1.0f, 1.0f, 0.5f, 0.5f, 0.5f, 0.5f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.1f, 1.0f, 0.1f, 0.0f, 0.1f, 0.8f, 0.0f, 0.7f, 0.2f, 0.9f, 0.3f, 1.0f, 0.5f, 1.0f, 0.7f, 1.0f, 0.8f, 0.9f, 0.9f, 0.8f, 1.0f, 0.7f, 1.0f, 0.5f, 1.0f, 0.3f, 0.9f, 0.2f, 0.8f, 0.1f, 0.7f, 0.0f, 0.5f, 0.0f, 0.3f, 0.0f, 0.2f, 0.1f, 0.1f, 0.2f, 0.0f, 0.3f, 0.0f, 0.5f, 1.0f, 0.7f, 0.9f, 0.8f, 0.8f, 0.9f, 0.7f, 1.0f, 0.5f, 1.0f, 0.3f, 1.0f, 0.2f, 0.9f, 0.1f, 0.8f, 0.0f, 0.7f, 0.0f, 0.5f, 0.0f, 0.3f, 0.1f, 0.2f, 0.2f, 0.1f, 0.3f, 0.0f, 0.5f, 0.0f, 0.7f, 0.0f, 0.8f, 0.1f, 0.9f, 0.2f, 1.0f, 0.3f, 1.0f, 0.5f };

        internal static int deleteAnAgent = -1;
        internal static int MaxNumAgents = 2000;

        internal static bool HFEnabled = false; // Height fatigue
        internal static bool RVODisabled = false; // Collision avoidance (RVO)
        internal static int UIScaleHeighChange1 = 1080;
        internal static int UIScaleHeighChange2 = 1440;
        internal static Color defaultButtonColor = new Color(231f / 255f, 231f / 255f, 231f / 255f);
        internal static readonly Vector3 LineDisplayHeightOffset = new Vector3(0f, 0.3f, 0f);
        internal static readonly Vector3 GateOffset = new Vector3(0, 1f, 0);
        internal static readonly Vector3 GateOffset2 = new Vector3(0, -0.05f, 0);
        internal static readonly Vector3 AgentOffset = new Vector3(0, -1.2f, 0);
        internal static float MaxCameraAngle = 85f;
        internal static float Angle90 = 90f;
        internal static float AgentDensityHeight = 2f;
        internal static float GridResolution = 0.001f;
        internal static float SpeedMultiplier = 1f;
        internal static int DangerWeightModifier = 7500;
        internal static List<float> HeatmapMaximums = new List<float> { -1f, -1f, -1f, -1f, -1f };
        internal static bool HeatmapsEnabled = true;

        internal static int StairHeight = 1;
        internal static float StairLength = 5.0f;
        internal static float StairLength_DefaultStraight = 5.0f;
        internal static float StairLength_DefaultEscalator = 6.0f;
        internal static float StairLength_DefaultHalfLanding = 2.0f;
        internal static float StairWidth = 2.0f;
        internal static float StairLandingWidth = 2.0f;

        internal static float TicketGateWidth = 1.0f;

        internal static uint[,] InitialPenalties;
        internal static bool InitialPenaltiesCorrect = false;

        internal static bool ExportDataPositions = true;
        internal static bool ExportDecisionUpdates = true;
        internal static bool ExportReactionTimes = true;
        internal static bool ExportDataEvacTimes = true;
        internal static bool ExportDataGateShares = true;
        internal static bool ExportSimulationRealTimes = true;

        internal static float RampAngle = 18f;
    }

    internal static class Strings
    {
        internal static string NoHeatmap { get { return LocalizationManager.GetTermTranslation(noHeatmap); } }
        internal const string noHeatmap = "The heatmap hasn't been completely recorded yet. Wait until the simulation is complete.";
        internal const string SimLoadingText = "(Simulation Computing In Progress)";
        internal const string SimRepeatingText = "Simulation Repeating ";
        internal const string SimPausedText = "Simulation Playback Paused";
        internal const string SimEvacTime = "Evacuation Time: ";
        internal const string SimReplayingText = "Simulation Playback";
        internal const string SimStoppedText = "Simulation Removed";
        internal const string NoDistributionError = "You cannot run a simulation without any distributions.";
        internal const string SimulationAlreadyStarted = "Simulation already started! Cancelling current simulation...";
        internal const string CouldNotInitialiseSim = "Could not initialise simulator.";
        internal const string ValidatingErrorString = "There were error(s) with validating the model you created, continuing...";
        internal const string AgentUpdate = "AgentUpdatePackage: ";
        internal static string AboutText =
            "Pedestride Multimodal " + Application.version + Environment.NewLine
            + "majid.sarvi@unimelb.edu.au" + Environment.NewLine
            + LocalizationManager.GetTermTranslation("copyright");

        internal const string FileDialogCancel = "[FileSelector] Dialog canceled";
        internal const string FileDialogEnded = "[FileSelector] Dialog ended, result: ";
        internal const string ExtensionImport = ".png|.jpg|.dxf";
        internal const string ExtensionCpm = ".cpm";
        internal const string ExtensionCsv = ".csv";
        internal const string CSVParseError = "Error parsing CSV file, discarding...";
    }

    internal static class Statics
    {
        public static int AnimHashForward;
        public static float FloorHeight = -1f;
        public static float LevelHeight = 2.5f;
        public static float FloorOffset = 0.04f;
        public static float FloorOffsetGrid = 0.03f;
    }

    public static class MemberInfoGetting
    {
        public static string GetMemberName<T>(Expression<Func<T>> memberExpression)
        {
            MemberExpression expressionBody = (MemberExpression)memberExpression.Body;
            return expressionBody.Member.Name;
        }
    }

    public static class Util
    {
        public static Color LerpToColor(float lerpAmount, Color[] colors, bool useLog = false)
        {
            if (float.IsNaN(lerpAmount))
            {
                //Debug.LogError("You can't lerp a 'NaN' amount sorry.");
                return Color.magenta;
            }

            float t = Mathf.Clamp01(lerpAmount);

            if (useLog)
                t = Mathf.Pow(t, 0.5f);

            Color updatedColor = Color.black;

            if (t == 1.0f)
            {
                updatedColor = colors[colors.Length - 1];
            }
            else
            {
                try
                {
                    float scaledT = t * (colors.Length - 1);
                    Color prevC = colors[(int)scaledT];
                    Color nextC = colors[(int)(scaledT + 1f)];
                    float newT = scaledT - (int)scaledT;
                    updatedColor = Color.Lerp(prevC, nextC, newT);
                }
                catch (Exception e)
                {
                    Debug.LogError("Problem Lerping Colors. t: " + t + " " + e);
                }
            }

            return updatedColor;
        }

        public static int GetMaxSize()
        {
            GridGraph gg = (GridGraph)AstarPath.active.data.graphs[0];
            int maxSize = int.MinValue;

            foreach (GridNode node in gg.nodes)
            {
                if (node.XCoordinateInGrid > maxSize)
                    maxSize = node.XCoordinateInGrid;
                if (node.ZCoordinateInGrid > maxSize)
                    maxSize = node.ZCoordinateInGrid;
            }

            return maxSize + 1;
        }
    }
}
