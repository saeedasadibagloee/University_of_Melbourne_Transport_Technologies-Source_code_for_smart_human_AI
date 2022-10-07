using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Core;
using Core.GateChoice;
using Core.Logger;
using Core.Threat;
using DataFormats;
using Domain;
using eDriven.Core.Signals;
using Helper;
using Info;
using InputOutput;
using Pathfinding.RVO;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using Core.PositionUpdater.ReactionTimeHandler;
using Fire;
using Pathfinding;
using System.Collections.Concurrent;
using Assets.Scripts.Networking;
using JetBrains.Annotations;
using TMPro;

public class SimulationController : MonoBehaviour
{
    // Public variables
    public List<GameObject> characters;
    public List<Text> HeatmapTexts;
    public GameObject GradientLegend;
    public GameObject GradientLegendImage;
    public Text GradientLabel;
    public GameObject heatmapPrefab;

    #region PRIVATE

    private static SimulationController _instance;
    private static readonly object Padlock = new object();

    private int _nInactiveAgents = -1;
    private readonly Signal _signal = new Signal();
    private SimulationThread _simThread;
    private readonly FileOperations _op = FileOperations.Instance;

    //Inputs from other thread:   
    public ConcurrentQueue<NetworkItem> itemsToProcess = new ConcurrentQueue<NetworkItem>();
    private readonly Queue<AgentUpdatePackage> _agentUpdateQueue = new Queue<AgentUpdatePackage>();
    private readonly object _agentUpdateQueueLock = new object();
    private readonly Queue<ThreatUpdate> _threatUpdateQueue = new Queue<ThreatUpdate>();
    private readonly object _threatUpdateQueueLock = new object();
    private ThreatUpdate _threatUpdate = null;
    private bool _leftInQueue = true;

    private readonly List<StairInfo> _stairs = new List<StairInfo>();
    public TextMeshProUGUI _simText;
    private readonly Dictionary<int, GameObject> _agents = new Dictionary<int, GameObject>();
    private readonly SortedDictionary<int, float> _evacuationTimes = new SortedDictionary<int, float>();
    private readonly object _evacuationTimesLock = new object();
    private readonly SortedDictionary<int, int> _generationCycles = new SortedDictionary<int, int>();
    public float LastSimulationTime = -1f;

    public int SimState = (int)Def.SimulationState.NotStarted;
    private float _evacuationTime = float.MinValue;
    private Def.ColorDisplay _colorDisplay = Def.ColorDisplay.Off;

    private bool _isPaused;
    private bool _agentsHidden;

    private bool _skipFrame;
    private bool _animationsOn = true;

    private readonly object _evacTimeLocker = new object();
    private bool _displayGateShares;
    private bool _lineLinear;
    private bool _pathO;
    private bool _pathDisplay;
    private bool _mode2D;

    private readonly List<GameObject> _heatmapObjects = new List<GameObject>();
    private System.Diagnostics.Stopwatch _simulationStopWatch;
    #endregion

    internal Def.HeatmapType ActiveHeatmapType = Def.HeatmapType.None;
    public HeatmapData heatmapData;

    /// <summary>
    /// Number of times to run the simulation before it finishes.
    /// </summary>
    public int NumberOfRuns = 1;
    /// <summary>
    /// Seconds before the simulation will cancel automatically.
    /// </summary>
    public int CancelAfter = int.MaxValue;
    internal int CurrentRunCount = 1;
    internal int CurrentAnalysisCycleCount = 1;
    internal Dictionary<int, Dictionary<int, int>> GateSharesData;
    private Dictionary<int, List<TrappedPerRoom>> _trappedAgentsInfo;
    internal List<AgentRectionTimeData> reactionTimeData = new List<AgentRectionTimeData>();

    private bool _macroView = false;
    private bool _recordingHeatmaps = false;
    private bool _recordingHeatmapsCompleted = false;
    private bool _recordingHeatmapsCompletedRoundOne = false;
    private int[] _heatmapTypes;
    private int _heatmapCounter = 1;
    private bool _isSmooth = false;
    private bool _wasTopDown = false;
    internal bool CmdLineControlled = false;
    private bool _simulationFailed = false;
    private bool restartRunningSimulation = false;
    private List<SpaceSyntaxData> SSxDatas = null;
    private int totalNodesApplied = 0;
    private List<GateInfo> counterGates;

    public static SimulationController Instance
    {
        get
        {
            lock (Padlock)
            {
                return _instance ?? (_instance = FindObjectOfType<SimulationController>());
            }
        }
    }
    public Def.HeatmapType ActiveHeatmap
    {
        get
        {
            return ActiveHeatmapType;
        }
    }

    public List<GameObject> HeatmapObjects
    {
        get { return _heatmapObjects; }
    }

    // Use this for initialization
    void Start()
    {
        _signal.Connect(OnSignal);
        _simThread = new SimulationThread(_signal);
        _instance = FindObjectOfType<SimulationController>();

        StartCoroutine(LateStart(1));
    }

    IEnumerator LateStart(float i)
    {
        yield return new WaitForSeconds(i);

        if (Application.isEditor)
            CmdLineControlled = ConfigurationFile.LoadAndVerifyFile();
        else
        {
            string[] arguments = Environment.GetCommandLineArgs();

            if (arguments.Length > 1) // Automatically open a model
            {
                ConfigurationFile.modelfile = arguments[1];
                if (!string.IsNullOrEmpty(ConfigurationFile.modelfile))
                    UIController.Instance.OpenModel(ConfigurationFile.modelfile);
            }

            if (arguments.Length > 2) // Automatically load parameter information
            {
                ConfigurationFile.fileName = arguments[2];
                CmdLineControlled = ConfigurationFile.LoadAndVerifyFile();
            }

            if (arguments.Length > 3) // Automatically run and output data
            {
                ConfigurationFile.outputFile = arguments[3];
            }
        }

        if (CmdLineControlled)
        {
            ConfigurationFile.ApplyParameters();
            string message = "Successfully applied " + ConfigurationFile.analysisValues.Count + " parameters." +
                             Environment.NewLine;
            if (ConfigurationFile.failedParameters > 0)
                message += ConfigurationFile.failedParameters + " lines failed, please check.";
            if (string.IsNullOrEmpty(ConfigurationFile.outputFile))
                message += Environment.NewLine + "Output file not specified.";
            UIController.Instance.ShowGeneralDialog(message, "Command Line Controlled");

            if (!string.IsNullOrEmpty(ConfigurationFile.outputFile))
            {
                StartCoroutine(LateAutoRunSim(0.5f));
                StartCoroutine(CloseDialogDelayed(5f));
            }
        }
    }

    private IEnumerator CloseDialogDelayed(float f)
    {
        yield return new WaitForSeconds(f);
        UIController.Instance.QuitGeneralDialog();
    }

    private IEnumerator LateAutoRunSim(float f)
    {
        yield return new WaitForSeconds(f);
        RunSimulation();
    }

    void Update()
    {
        // Finish the simulation once the queue is empty and we are in the finished state.
        if (SimState == (int)Def.SimulationState.Finished && _agentUpdateQueue.Count < 1)
        {
            _simulationFailed = false;
            SimulationsHaveCompleted();
        }

        // If the simulation goes overtime automatically cancel it.
        if (SimState == (int)Def.SimulationState.Started && _simulationStopWatch.Elapsed.TotalSeconds > CancelAfter)
        {
            Interlocked.Exchange(ref SimState, (int)Def.SimulationState.Finished);
            if (NetworkServer.Instance.isConnected)
                NetworkServer.Instance.UpdateSimStatus((int)Def.SimulationState.Finished);
            _simulationFailed = true;
            CurrentRunCount--;
            LogWriter.Instance.WriteToLog("AutoCancel Simulation. Over " + CancelAfter + " sec.");
        }

        ProcessAgentUpdateQueue();

        lock (_threatUpdateQueueLock)
        {
            if (_threatUpdateQueue.Count > 0)
            {
                ProcessThreatUpdate(_threatUpdateQueue.Dequeue());
            }
        }

        if (_nInactiveAgents > 0 && SimState == (int)Def.SimulationState.Started)
        {
            UIController.Instance.SetConsoleText("Agents Evacuated: " + _nInactiveAgents + " / " + _agents.Count);
            UIController.Instance.CompleteSimButton(true);
        }


        if (_trappedAgentsInfo != null)
        {
            StringBuilder builder = new StringBuilder();

            foreach (var levelData in _trappedAgentsInfo)
            {
                builder.Append("Level: " + levelData.Key + " " + Environment.NewLine);

                foreach (var roomData in levelData.Value)
                    builder.Append("Room: " + roomData.RoomID + " has " + roomData.NAgents + " trapped." + Environment.NewLine);
            }

            UIController.Instance.ShowGeneralDialog(builder.ToString(), "Trapped Agents");

            _trappedAgentsInfo = null;
        }

        if (SimState == (int)Def.SimulationState.Started)
        {
            _simText.text = Strings.SimLoadingText + " " + SimulationTimeKeeper.Instance.time.ToString("0.0");
        }

        while (!itemsToProcess.IsEmpty)
        {
            NetworkItem item = null;
            itemsToProcess.TryDequeue(out item);
            if (item != null)
                ProcessNetworkItem(item);
        }

        if (restartRunningSimulation)
            RunSimulation();
    }

    private void ProcessNetworkItem(NetworkItem item)
    {
        switch ((MsgType)item.msg_type)
        {
            case MsgType.STATUS:
                Def.SimulationState simStat = (Def.SimulationState)int.Parse(item.msg);
                if (simStat == Def.SimulationState.Started)
                    RunSimulation();
                else if (simStat == Def.SimulationState.NotStarted)
                    CancelSimulation();
                break;
            case MsgType.HEATMAP:
                var heatmapType = int.Parse(item.msg.Split(',')[0]);
                var isSmooth = int.Parse(item.msg.Split(',')[1]) == 1;
                ViewHeatmap((Def.HeatmapType)heatmapType, isSmooth);
                NetworkServer.Instance.SendHeatmap(heatmapData);
                break;
            case MsgType.WALL:
                Modify mods = JsonUtility.FromJson<Modify>(item.msg);
                foreach (var wall in FindObjectsOfType<WallInfo>())
                {
                    if (wall.Id == mods.id)
                    {
                        wall.ApplyMods(mods);
                        break;
                    }
                }
                break;
        }
    }

    private void ProcessAgentUpdateQueue()
    {
        _leftInQueue = true;

        while (_leftInQueue)
        {
            AgentUpdatePackage newAgentUpdate = null;
            lock (_agentUpdateQueueLock)
            {
                if (_agentUpdateQueue.Count < 1)
                {
                    _leftInQueue = false;
                    break;
                }
                newAgentUpdate = _agentUpdateQueue.Dequeue();
            }

            if (newAgentUpdate != null)
                ProcessAgentUpdatePackage(newAgentUpdate);
        }
    }

    private void ProcessThreatUpdate(ThreatUpdate threatUpdate)
    {
        foreach (var threat in FindObjectsOfType<ThreatInfo>())
        {
            if (threat.ThreatId == threatUpdate.ObjectId)
            {
                threat.UpdateStatus(threatUpdate.IsActive);
                return;
            }
        }
        Debug.LogError("Could not find threat with id " + threatUpdate.ObjectId);
    }

    void LateUpdate()
    {
        if (_recordingHeatmapsCompleted)
        {
            Debug.Log("All heatmap screenshots output successfully.");
            _recordingHeatmaps = false;
            UIController.Instance.KeyboardEnabled = true;
            UIController.Instance.ShowGeneralDialog(
                "Successfully output " + (CmdLineControlled ? NumberOfRuns * ConfigurationFile.analysisValues.Count : NumberOfRuns)
                + " runs! Please remember to use the Heatmaps & Simulation Data or you will lose them with the next run.", "Simulations Complete!");
            ToggleHeatmap(0, false);
            _recordingHeatmapsCompleted = false;
            UpdateAgentVisibility(false);
            if (!_wasTopDown) TopDownView.Instance.SetTopDown(false);

            if (!string.IsNullOrEmpty(ConfigurationFile.outputFile))
                Application.Quit(0);
        }

        if (_recordingHeatmapsCompletedRoundOne)
        {
            _isSmooth = true;
            _heatmapCounter = 1;
            _recordingHeatmapsCompletedRoundOne = false;
        }

        if (_recordingHeatmaps)
        {
            TopDownView.Instance.SetTopDown(true);

            int currentHeatmap = _heatmapCounter - 1;

            string path = Application.dataPath + "Export_" + UIController.Instance.ModelName;
            string suffix = _isSmooth ? "(Smooth).png" : ".png";

            if (string.IsNullOrEmpty(ConfigurationFile.outputFile))
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                path += "/Cumulative Heatmap ";
            }
            else
            {
                path = Environment.CurrentDirectory + "/" + ConfigurationFile.outputFile.Split('.')[0];
            }

            switch ((Def.HeatmapType)_heatmapTypes[currentHeatmap])
            {
                case Def.HeatmapType.None:
                    break;
                case Def.HeatmapType.Utilization:
                    ScreenCapture.CaptureScreenshot(path + (Def.HeatmapType)currentHeatmap + suffix, 1);
                    break;
                case Def.HeatmapType.MaxDensity:
                    ScreenCapture.CaptureScreenshot(path + (Def.HeatmapType)currentHeatmap + suffix, 1);
                    break;
                case Def.HeatmapType.AverageDensity:
                    ScreenCapture.CaptureScreenshot(path + (Def.HeatmapType)currentHeatmap + suffix, 1);
                    break;
                case Def.HeatmapType.AverageSpeed:
                    ScreenCapture.CaptureScreenshot(path + (Def.HeatmapType)currentHeatmap + suffix, 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_heatmapCounter > _heatmapTypes.Length - 1)
            {
                if (_isSmooth)
                    _recordingHeatmapsCompleted = true;
                else
                    _recordingHeatmapsCompletedRoundOne = true;
            }

            ToggleHeatmap((Def.HeatmapType)(_heatmapCounter - 1), _isSmooth);

            _heatmapCounter++;
        }

        // Skips calculating the colored agents when the frame rate drops too low.
        if (_skipFrame)
        {
            _skipFrame = false;
            return;
        }

        if (Time.smoothDeltaTime >= 0.07)
            _skipFrame = true;

        switch (_colorDisplay)
        {
            case Def.ColorDisplay.Density:
                #region Density
                List<Vector3> agentPositions = (
                    from agent in _agents.Values
                    where agent != null
                    where agent.GetComponent<IndividualAgent>().DisplayAgents
                    select agent.transform.position).ToList();

                foreach (GameObject agent in _agents.Values)
                {
                    if (agent == null)
                        continue;

                    int nearAgentCount = 0;
                    Vector3 currentPosition = agent.transform.position;
                    foreach (Vector3 otherAgentPos in agentPositions)
                        if (Mathf.Abs(otherAgentPos.y - currentPosition.y) < Consts.AgentDensityHeight)
                            if (Vector2.Distance(new Vector2(otherAgentPos.x, otherAgentPos.z), new Vector2(currentPosition.x, currentPosition.z)) < Consts.DensityRadius)
                                nearAgentCount++;

                    float density = nearAgentCount / (Mathf.PI * Consts.DensityRadius * Consts.DensityRadius);

                    IndividualAgent ia = agent.GetComponent<IndividualAgent>();

                    Color cl = Util.LerpToColor((density - Consts.MinDensity) / (Consts.MaxDensity - Consts.MinDensity), Def.Colors);
                    ia.UpdateColor(cl);
                }
                #endregion
                break;
            case Def.ColorDisplay.Speed:
                #region Speed
                foreach (GameObject agent in _agents.Values)
                {
                    if (agent == null)
                        continue;

                    IndividualAgent ia = agent.GetComponent<IndividualAgent>();

                    float speed = ia.GetCurrentSpeed();

                    Color cl = Util.LerpToColor(1f - (speed - Consts.MinSpeed) / (Consts.MaxSpeed - Consts.MinSpeed), Def.Colors);

                    ia.UpdateColor(cl);
                }

                #endregion
                break;
            case Def.ColorDisplay.JointThreshold:
                #region JointThreshold
                foreach (GameObject agent in _agents.Values)
                {
                    if (agent == null)
                        continue;

                    IndividualAgent ia = agent.GetComponent<IndividualAgent>();

                    float speed = ia.GetCurrentSpeed();
                    float density = ia.DensityFromGrid;

                    Color cl;

                    if (density > Params.Current.DensityFlowThreshold && speed < Params.Current.SpeedFlowThreshold)
                        cl = Color.white;
                    else
                        cl = Color.blue;

                    ia.UpdateColor(cl);
                }
                #endregion
                break;
            case Def.ColorDisplay.SpeedThreshold:
                #region SpeedThreshold
                foreach (GameObject agent in _agents.Values)
                {
                    if (agent == null)
                        continue;

                    IndividualAgent ia = agent.GetComponent<IndividualAgent>();

                    float speed = ia.GetCurrentSpeed();

                    Color cl = speed > Params.Current.SpeedFlowThreshold ? Color.red : Color.white;

                    ia.UpdateColor(cl);
                }
                #endregion
                break;
            case Def.ColorDisplay.DensityThreshold:
                #region DensityThreshold
                foreach (GameObject agent in _agents.Values)
                {
                    if (agent == null)
                        continue;

                    IndividualAgent ia = agent.GetComponent<IndividualAgent>();

                    float density = ia.DensityFromGrid;

                    Color cl = density < Params.Current.DensityFlowThreshold ? Color.green : Color.white;

                    ia.UpdateColor(cl);
                }
                #endregion
                break;
            case Def.ColorDisplay.Groups:
                #region Groups
                foreach (GameObject agent in _agents.Values)
                {
                    if (agent == null)
                        continue;

                    IndividualAgent ia = agent.GetComponent<IndividualAgent>();

                    switch (ia.type)
                    {
                        case AgentType.Individual:
                            ia.UpdateColor(Color.white);
                            break;
                        case AgentType.Leader:
                            ia.UpdateColor(Color.red);
                            break;
                        case AgentType.Follower:
                            ia.UpdateColor(Color.green);
                            break;
                    }
                }
                #endregion
                break;
            case Def.ColorDisplay.Off:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    internal void ReplaySimulation()
    {
        foreach (GameObject agent in _agents.Values)
        {
            if (agent != null)
                agent.GetComponent<IndividualAgent>().Restart();
        }

        SimulationTimeKeeper.Instance.Restart();

        _simText.text = Strings.SimReplayingText + GetEvacuationTimeString();

        if (_isPaused)
            TogglePause();
    }

    internal float FindMaxTime()
    {
        float fireSimTime = 0;
        int maxCycles = 0;

        if (FireDomain.Instance.isEnabled)
        {
            if (FireDomain.Instance.timeStorage != null && FireDomain.Instance.timeStorage.Count > 0)
            {
                fireSimTime = FireDomain.Instance.timeStorage[FireDomain.Instance.timeStorage.Count - 1];
            }
        }

        if (SimState == (int)Def.SimulationState.Replaying)
            maxCycles = (
                from agent in _agents.Values
                where agent != null
                select agent.GetComponent<IndividualAgent>() into ia
                select ia.generationCycle + ia.pathList.Count * Params.Current.UiUpdateCycle
                ).Concat(new[] { int.MinValue }).Max();

        return Mathf.Max(maxCycles * Params.Current.TimeStep, fireSimTime);
    }

    internal void ToggleGateShares()
    {
        _displayGateShares = !_displayGateShares;

        if (_displayGateShares)
        {
            if (GateSharesData == null)
            {
                Debug.Log("There is no gate shares data yet.");
                _displayGateShares = false;
                return;
            }

            foreach (KeyValuePair<int, Dictionary<int, int>> kvp in GateSharesData)
                foreach (var gateShare in kvp.Value)
                    Create.Instance.AddGateShare(ObjectInfo.Instance.FindGate(gateShare.Key), gateShare.Value);

            foreach (var counterGate in counterGates)
                Create.Instance.AddGateShare(counterGate, counterGate.Count);
        }
        else
        {
            Create.Instance.DestroyGateShares();
        }
    }

    internal void ToggleLineLinear()
    {
        _lineLinear = !_lineLinear;

        IndividualAgent ia;
        foreach (GameObject agent in _agents.Values)
        {
            if (agent == null) continue;
            ia = agent.GetComponent<IndividualAgent>();
            ia.ToggleLineDisplay(_lineLinear);
        }
    }

    internal void TogglePaths()
    {
        _pathDisplay = !_pathDisplay;

        IndividualAgent ia;
        foreach (GameObject agent in _agents.Values)
        {
            if (agent == null) continue;
            ia = agent.GetComponent<IndividualAgent>();
            ia.TogglePathFindingDisplay(_pathDisplay);
        }
    }

    internal void ViewPathsNow(bool display)
    {
        IndividualAgent ia;
        foreach (GameObject agent in _agents.Values)
        {
            if (agent == null) continue;
            ia = agent.GetComponent<IndividualAgent>();
            ia.TogglePathFindingDisplay(display);
        }
    }

    internal void ChangeColorDisplay(Def.ColorDisplay displayType)
    {
        if (displayType == _colorDisplay)
        {
            ToggleColorDisplayOff();
        }
        else
        {
            _colorDisplay = displayType;

            switch (_colorDisplay)
            {
                case Def.ColorDisplay.Density:
                    ShowGradientLegend(6f, "Density (p/m²)");
                    break;
                case Def.ColorDisplay.Speed:
                    ShowGradientLegend(-6f, "Speed (m/s)");
                    break;
                case Def.ColorDisplay.SpeedThreshold:
                case Def.ColorDisplay.DensityThreshold:
                case Def.ColorDisplay.JointThreshold:
                case Def.ColorDisplay.Groups:
                case Def.ColorDisplay.Off:
                    UIController.Instance.SetConsoleText("View " + _colorDisplay.ToString());
                    HideGradientLegend();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal void ToggleColorDisplayOff()
    {
        _colorDisplay = Def.ColorDisplay.Off;
        HideGradientLegend();
        SetAllAgentsWhite();
    }

    public void SetAllAgentsWhite()
    {
        foreach (GameObject agent in _agents.Values)
        {
            if (agent != null)
                agent.GetComponent<IndividualAgent>().UpdateColor(Color.white);
        }
    }

    internal void TogglePause()
    {
        _isPaused = !_isPaused;

        if (_isPaused)
            _simText.text = Strings.SimReplayingText + GetEvacuationTimeString();
        else
            _simText.text = Strings.SimPausedText + GetEvacuationTimeString();

        SimulationTimeKeeper.Instance.isPaused = _isPaused;

        foreach (GameObject agent in _agents.Values)
        {
            if (agent != null)
                agent.GetComponent<IndividualAgent>().TogglePause(_isPaused);
        }
    }

    internal void OutputHeatmaps()
    {
        if (SimState != (int)Def.SimulationState.Replaying)
        {
            Debug.Log(Strings.NoHeatmap);
            UIController.Instance.ShowGeneralDialog(Strings.NoHeatmap, "Export Heatmap");
            return;
        }

        int[] heatmapTypes = (int[])Enum.GetValues(typeof(Def.HeatmapType));

        foreach (Def.HeatmapType heatmapType in heatmapTypes)
        {
            if (heatmapType == (int)Def.HeatmapType.None)
                continue;

            Texture2D heatmapTex = FileOperations.Instance.GenerateHeatmap(heatmapType, false).Tex[0];
            Texture2D heatmapTexsmooth = FileOperations.Instance.GenerateHeatmap(heatmapType, true).Tex[0];
            FileOperations.Instance.ExportHeatmap(heatmapTex, heatmapType, false);
            FileOperations.Instance.ExportHeatmap(heatmapTexsmooth, heatmapType, true);
        }

        Debug.Log("All heatmaps output successfully.");
    }

    internal void OutputHeatmapScreenshots()
    {
        if (SimState != (int)Def.SimulationState.Replaying)
        {
            Debug.Log(Strings.NoHeatmap);
            UIController.Instance.ShowGeneralDialog(Strings.NoHeatmap, "Export Heatmap");
            return;
        }

        Directory.CreateDirectory(Application.dataPath + "Export/");
        UIController.Instance.LevelSetLevel(0);
        _wasTopDown = TopDownView.Instance.IsTopDown;
        TopDownView.Instance.SetTopDown(false);
        _recordingHeatmaps = true;
        UpdateAgentVisibility(true);
        UIController.Instance.toggleTaskBarLeft(UIController.Instance.anyActiveTaskBars());
        UIController.Instance.toggleTaskBarRight(UIController.Instance.anyActive2TaskBars());
        UIController.Instance.KeyboardEnabled = false;
        _heatmapTypes = (int[])Enum.GetValues(typeof(Def.HeatmapType));
        _heatmapCounter = 1;
        _isSmooth = false;
    }


    private GameObject CreateNewAgent(AgentUpdatePackage agPkg)
    {
        GameObject agent = Instantiate(GetRandomCharacter(),
            ToFh(new Vector3(agPkg.x, Consts.MinusOne, agPkg.y), agPkg.levelId),
            Quaternion.Euler(0, 0, 0));

        float agentScale = agPkg.radius / 0.23f;

        agent.transform.localScale = new Vector3(agentScale, agentScale, agentScale);

        IndividualAgent agentScript = agent.GetComponent<IndividualAgent>();

        agentScript.radius = agPkg.radius;
        agentScript.agentID = agPkg.agent_id;
        agentScript.levelIdCoreList.Add(agPkg.levelIdreal);
        agentScript.generationCycle = agPkg.generationCycle;
        agentScript.classID = agPkg.classID;
        agentScript.AddPathGatePosEsc(
            ToFh(new Vector3(agPkg.x, Consts.MinusOne, agPkg.y), agPkg.levelId),
            ToFh(new Vector3(agPkg.gate_x, Consts.MinusOne, agPkg.gate_y), agPkg.levelId), false);
        agentScript.pathOriginal = agPkg.pathOriginal;
        agentScript.pathModified = agPkg.pathModified;

        if (agPkg.classID == 2)
            agentScript.UpdateClass();

        agentScript.UpdateColor(agPkg.color);
        agentScript.colorList.Add(agPkg.color);
        agentScript.type = agPkg.type;

        return agent;
    }

    public void ProcessAgentUpdatePackage(AgentUpdatePackage agPkg)
    {
        if (agPkg == null)
        {
            Debug.LogError("AgentUpdatePackage is null.");
            LogWriter.Instance.WriteToLog(Strings.AgentUpdate + "null");
            return;
        }

        if (float.IsNaN(agPkg.x) || float.IsNaN(agPkg.y))
        {
            Debug.LogError("Recieved agent location of NaN. Discarding...");
            return;
        }

        if (NetworkServer.Instance.isConnected)
            NetworkServer.Instance.AgentUpdate(agPkg);

        if (!_macroView) // Don't create agents if in macro view
        {
            if (!_agents.ContainsKey(agPkg.agent_id))
            {
                GameObject person = CreateNewAgent(agPkg);
                _agents.Add(agPkg.agent_id, person);
            }
            else
            {
                ProcessAgent(agPkg);
            }
        }
    }

    public void UpdateStairInfoList()
    {
        _stairs.Clear();
        _stairs.AddRange(FindObjectsOfType<StairInfo>());
    }

    private void ProcessAgent(AgentUpdatePackage agPkg)
    {
        GameObject agentGameObject = _agents[agPkg.agent_id];
        if (agentGameObject == null)
            return;
        IndividualAgent agentScript = agentGameObject.GetComponent<IndividualAgent>();
        agentScript.isCurrentlyOnEscalator = false;

        if (!agPkg.isActive)
        {
            agentScript.HideAgent();

            lock (_evacuationTimesLock)
            {
                if (!_evacuationTimes.ContainsKey(agPkg.agent_id))
                    _evacuationTimes.Add(agPkg.agent_id, agPkg.evacuationTime);
            }

            if (!_generationCycles.ContainsKey(agPkg.agent_id))
                _generationCycles.Add(agPkg.agent_id, agPkg.generationCycle);

            agentScript.DensityFromGrid = 0;
        }
        else
        {
            Vector3 newAgentPos = ToFh(new Vector3(agPkg.x, Consts.MinusOne, agPkg.y), agPkg.levelId);

            if (agPkg.location == (int)Location.MovingInsideStairway)
            {
                foreach (StairInfo si in _stairs)
                    if (si.level == agPkg.levelId && si.PointInside(agPkg.x, agPkg.y))
                    {
                        newAgentPos = si.PlaceOnStairway(newAgentPos);
                        if (si.stairType == Def.StairType.Escalator)
                            agentScript.isCurrentlyOnEscalator = true;
                    }
            }
            else if (agPkg.location == (int)Location.None)
            {
                //Debug.LogError("Received a agent with no location?");
            }

            if (counterGates.Count > 0)
                ProcessVectorForCounterGates(agentScript.GetLatestPathPosition(), newAgentPos);

            agentScript.AddPathGatePosEsc(
                newAgentPos,
                ToFh(new Vector3(agPkg.gate_x, Consts.MinusOne, agPkg.gate_y), agPkg.levelId),
                agentScript.isCurrentlyOnEscalator);
            agentScript.pathOriginal = agPkg.pathOriginal;
            agentScript.pathModified = agPkg.pathModified;

            agentScript.DensityFromGrid = agPkg.density;
            agentScript.levelIdCoreList.Add(agPkg.levelIdreal);

            agentScript.UpdateColor(agPkg.color);
            agentScript.UpdateLineColor(agPkg.color);
            agentScript.colorList.Add(agPkg.color);
            agentScript.type = agPkg.type;

            if (UIController.Instance.ViewPathUpdates)
                agentScript.UpdateColor(agPkg.HasJustUpdatedPath ? Color.red : Color.white);
        }
    }

    /// <summary>
    /// Shifts the input position to the desired floor height.
    /// </summary>
    public Vector3 ToFh(Vector3 input, int selectedLevel)
    {
        return new Vector3(input.x, Statics.FloorHeight + Statics.LevelHeight * selectedLevel, input.z);
    }

    public void SimulationsHaveCompleted()
    {
#if DEBUG_PERF
        MethodTimer.OutputMethodTimesToCsv();
#endif
        CalculateAndDisplayStatistics();

        RVOSimulator.active.GetSimulator().ClearAgents();

        // If user running multiple simulations, or if user gave commandline outputfile.
        if (NumberOfRuns > 1 && !_simulationFailed || !string.IsNullOrEmpty(ConfigurationFile.outputFile))
            UIController.Instance.ExportData();

        CurrentRunCount++;
        if (CurrentRunCount <= NumberOfRuns)
        {
            ClearHeatMap();
            _simThread.StopSimulation();
            _simText.text = "Starting new run.";
            SimState = (int)Def.SimulationState.NotStarted;
            if (NetworkServer.Instance.isConnected)
                NetworkServer.Instance.UpdateSimStatus(SimState);
            DeletePlayback();
            RunSimulationAgain();
            // Run next simulations.
            return;
        }

        // All simulations and cycles have completed.

        CurrentRunCount = 1;
        SimState = (int)Def.SimulationState.Replaying;
        //if (NetworkServer.Instance.isConnected) NetworkServer.Instance.UpdateSimStatus(SimState);

        foreach (GameObject agent in _agents.Values)
            if (agent != null)
                agent.GetComponent<IndividualAgent>().SimulationCompleted();

        UIController.Instance.SetConsoleText("Agents Evacuated: " + _nInactiveAgents);

        SimulationTimeKeeper.Instance.Restart();

        if (NumberOfRuns > 1 || !string.IsNullOrEmpty(ConfigurationFile.outputFile))
        {
            UIController.Instance.QuitGeneralDialog();
            OutputHeatmaps();
            OutputHeatmapScreenshots();
        }
    }

    private void CalculateAndDisplayStatistics()
    {
        if (_evacuationTimes == null || _evacuationTimes.Count < 1)
            return;
        _simText.text = "Avg Individual Evac: " + _evacuationTimes.Values.Average() + ". Total Evac Time: " + _evacuationTimes.Values.Max() + ".";
    }

    private GameObject GetRandomCharacter()
    {
        if (characters == null) Debug.LogError("You must set some characters in the inspector.");
        int i = Random.Range(0, characters.Count);
        return characters[i];
    }

    internal int GetAgentCount()
    {
        return _agents.Count;
    }

    public bool InitialiseSimulator()
    {
        Model model = _op.ObjectsToModel();

        RVOSimulator.active.GetSimulator().ClearAgents();
        SimulationParams simParams = UIController.Instance.GetSimulationParameters();

        if (ContainsDistribution(model))
        {
            return _simThread.Initialize(model, simParams);
        }

        Debug.Log(Strings.NoDistributionError);
        _simText.text = Strings.NoDistributionError;
        return false;
    }

    private bool ContainsDistribution(Model model)
    {
        return model.levels.SelectMany(l => l.agent_pkg.distributions).Any();
    }

    public void SetSimText(string message)
    {
        _simText.text = message;
    }

    private void RunSimulationAgain()
    {
        _evacuationTimes.Clear();
        _generationCycles.Clear();
        _displayGateShares = false;

        if (SimState != (int)Def.SimulationState.NotStarted)
        {
            Debug.LogError(Strings.SimulationAlreadyStarted);
            CancelSimulation();
            return;
        }

        if (CmdLineControlled)
            ResetAllNodes();

        RVOSimulator.active.GetSimulator().ClearAgents();
        Create.Instance.SetAllColliders(true);

        if (InitialiseSimulator())
        {
            _simulationStopWatch = System.Diagnostics.Stopwatch.StartNew();
            _simThread.Start();
            SimState = (int)Def.SimulationState.Started;
            if (NetworkServer.Instance.isConnected)
                NetworkServer.Instance.UpdateSimStatus(SimState);
            _simText.text = Strings.SimRepeatingText + CurrentRunCount + " / " + NumberOfRuns + ". Cycle: " + CurrentAnalysisCycleCount + " / " + ConfigurationFile.analysisValues.Count;
        }
        else
        {
            Debug.LogError(Strings.CouldNotInitialiseSim);
        }
    }

    private static void ResetAllNodes()
    {
        int walkableNodes = 0;
        int size = -1;

        if (AstarPath.active.data.graphs[0] == null)
            return;

        if (((GridGraph)AstarPath.active.data.graphs[0]).nodes == null)
            return;

        foreach (GridNode node in ((GridGraph)AstarPath.active.data.graphs[0]).nodes)
        {
            if (node.Walkable)
            {
                if (size < 0)
                    size = node.GetSize();

                walkableNodes++;
                node.ResetAll();
            }
        }
        //LogWriter.Instance.WriteToLog("Cleared " + (size * walkableNodes) / 1024f + "kB of memory from heatmaps/penalty grid.");
    }

    public void RunSimulation()
    {
        Analytics.CustomEvent("RunSimulation");
        UIController.Instance.CloseActiveTaskbar();
        UIController.Instance.CentreTrains();
        UIController.Instance.CompleteSimButton(false);
        PathfindingGridViewerSquare.ResetMaxPenalty();
        _evacuationTimes.Clear();
        LastSimulationTime = -1f;
        _generationCycles.Clear();
        _displayGateShares = false;
        UpdateAgentVisibility(false);
        PrepareCounterGates();

        Constants.NeighborRadiusSqrd = Params.Current.NeighborRadius;

        if (SimState != (int)Def.SimulationState.NotStarted)
        {
            Debug.Log(Strings.SimulationAlreadyStarted);
            CancelSimulation();
            restartRunningSimulation = true;
            return;
        }

        Create.Instance.SetAllColliders(true);

        if (AstarPath.active.isScanning)
        {
            UIController.Instance.ShowGeneralDialog("Wait until the environment has finished scanning.", "Hangon!");
            return;
        }

        if (_macroView)
            PathfindingGridViewerHeat.Instance.EnableView();

        if (!restartRunningSimulation)
            AstarPath.active.Scan();
        else
            restartRunningSimulation = false;

        ToggleHeatmap(Def.HeatmapType.None, false);
        UpdateStairInfoList();

        if (InitialiseSimulator())
        {
            //Simulation Started
            Create.Instance.ChangeSelectedObject((int)Def.Object.None);
            _simulationStopWatch = System.Diagnostics.Stopwatch.StartNew();
            _simThread.Start();
            SimState = (int)Def.SimulationState.Started;
            if (NetworkServer.Instance.isConnected)
                NetworkServer.Instance.UpdateSimStatus(SimState);
            _simText.text = Strings.SimLoadingText;

            foreach (GridDisplay g in FindObjectsOfType<GridDisplay>())
                g.ToggleGridDisplay(false);

            UIController.Instance.GridDisplayEnabled = false;
        }
        else
        {
            Debug.LogError(Strings.CouldNotInitialiseSim);
        }
    }

    private void PrepareCounterGates()
    {
        counterGates = new List<GateInfo>();

        foreach (var gate in FindObjectsOfType<GateInfo>())
        {
            if (gate.IsCounter)
            {
                gate.Count = 0;
                counterGates.Add(gate);
            }
        }
    }

    private void ProcessVectorForCounterGates(Vector3 start, Vector3 end)
    {
        foreach (var counterGate in counterGates)
            if (VectorCrossesGate(counterGate, start, end))
            {
                counterGate.Count++;
                counterGate.ShowGateSharesCount();
            }
    }

    private bool VectorCrossesGate(GateInfo counterGate, Vector3 start, Vector3 end)
    {
        if (counterGate.Vertices.Count < 2)
            return false;

        float gateLevelHeight = Create.Instance.ToLh(Vector3.zero, counterGate.LevelId).y;
        float vectorLevelHeight = (start.y + end.y) / 2f;
        bool withinLevel = Mathf.Abs(gateLevelHeight - vectorLevelHeight) < Statics.LevelHeight * 0.6f;

        Vector2 intersection;

        return withinLevel && Core.Vector.LineSegmentsIntersection(
                counterGate.Vertices[0].GetVector2(),
                counterGate.Vertices[1].GetVector2(),
                new Vector2(start.x, start.z),
                new Vector2(end.x, end.z), out intersection);
    }

    public void CancelSimulation()
    {
        Analytics.CustomEvent("CancelSimulation");
        CurrentRunCount = 1;
        CurrentAnalysisCycleCount = 1;
        PathfindingGridViewerSquare.ResetMaxPenalty();
        UIController.Instance.CentreTrains();
        ClearHeatMap();
        _simThread.StopSimulation();
        _simText.text = Strings.SimStoppedText;
        SimState = (int)Def.SimulationState.NotStarted;
        if (NetworkServer.Instance.isConnected)
            NetworkServer.Instance.UpdateSimStatus(SimState);
        DeletePlayback();
        ResetAllNodes();
    }

    public void CompleteSimulation()
    {
        Analytics.CustomEvent("CompleteSimulation");
        _simThread.ForceCompleteSimulation();
        _simText.text = "Requesting to complete..";
    }

    private void OnSignal(params object[] parameters)
    {
        switch ((Def.Signal)parameters[0])
        {
            case Def.Signal.TimeData:
                TimePackage timePackage = parameters[1] as TimePackage;
                SimulationTimeKeeper.Instance.SetTime(timePackage);
                if (timePackage.cycleNum % 40 == 0)
                    NetworkServer.Instance.SendNetworkItem(MsgType.TIME, timePackage.cycleNum.ToString());
                break;

            case Def.Signal.AgentUpdate:
                lock (_agentUpdateQueueLock)
                    _agentUpdateQueue.Enqueue(parameters[1] as AgentUpdatePackage);
                break;

            case Def.Signal.SimulationStatus:

                var currentStatus = ((SimulationStatusPackage)parameters[1]).state;

                switch (currentStatus)
                {
                    case (int)Def.SimulationState.Interrupted:
                        _trappedAgentsInfo = ((TrappedAgentsInfo)parameters[2]).AgentsInfo;
                        lock (_evacTimeLocker) { _evacuationTime = float.NaN; }
                        break;
                    case (int)Def.SimulationState.Finished:
                        lock (_evacTimeLocker) { _evacuationTime = (float)parameters[2]; }
                        LastSimulationTime = _simulationStopWatch.ElapsedMilliseconds;
                        break;
                    default:
                        break;
                }

                if (NetworkServer.Instance.isConnected)
                    NetworkServer.Instance.UpdateSimStatus((int)Def.SimulationState.Finished);

                Interlocked.Exchange(ref SimState, (int)Def.SimulationState.Finished);
                break;

            case Def.Signal.InactiveAgents:
                InactiveAgentsCountPackage iacp = parameters[1] as InactiveAgentsCountPackage;
                //Debug.Log("Received InactiveAgentsCountPackage: " + iacp.nInactiveAgents);
                Interlocked.Exchange(ref _nInactiveAgents, iacp.nInactiveAgents);
                break;

            case Def.Signal.GateSharesData:
                GateSharesData =
                    new Dictionary<int, Dictionary<int, int>>(
                        parameters[1] as Dictionary<int, Dictionary<int, int>>);
                break;

            case Def.Signal.ThreatUpdate:
                lock (_threatUpdateQueueLock)
                    _threatUpdateQueue.Enqueue(parameters[1] as ThreatUpdate);
                break;

            case Def.Signal.ReactionTimeData:
                var reactionList = parameters[1] as List<AgentRectionTimeData>;
                reactionTimeData.Clear();
                reactionTimeData.AddRange(reactionList);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    internal IEnumerable<KeyValuePair<int, GameObject>> GetAgents()
    {
        return _agents;
    }

    internal SortedDictionary<int, float> GetEvacuationTimes()
    {
        //Debug.Log("Inactive: " + _nInactiveAgents);

        ProcessAgentUpdateQueue();

        lock (_evacuationTimesLock)
        {
            //Debug.Log("EvacTimes: " + _evacuationTimes.Count);
            return _evacuationTimes;
        }
    }

    internal SortedDictionary<int, int> GetGenerationCycles()
    {
        return _generationCycles;
    }

    internal float GetEvacuationTime()
    {
        if (_evacuationTime >= 0)
        {
            return _evacuationTime;
        }

        var evacuationTimes = GetEvacuationTimes();

        if (evacuationTimes.Count == 0)
            return -1;

        foreach (float time in evacuationTimes.Values)
        {
            if (time > _evacuationTime)
            {
                _evacuationTime = time;
            }
        }

        return _evacuationTime;
    }

    private string GetEvacuationTimeString()
    {
        return " (" + GetEvacuationTime().ToString("00.00") + ")";
    }

    internal void DeletePlayback()
    {
        foreach (GameObject agent in _agents.Values)
        {
            if (agent != null)
                Destroy(agent);
        }
        _agents.Clear();
        _agentUpdateQueue.Clear();
        Interlocked.Exchange(ref _nInactiveAgents, -1);
    }

    internal void ChangeModeAllAgents(bool lowPolyMode)
    {
        foreach (IndividualAgent ia in FindObjectsOfType<IndividualAgent>())
            ia.ChangeMode(lowPolyMode);

        _mode2D = lowPolyMode;
    }

    internal void ChangeModeAllAgents()
    {
        _mode2D = !_mode2D;
        ChangeModeAllAgents(_mode2D);
    }

    internal bool Is2D()
    {
        return _mode2D;
    }

    internal void ToggleAnimations(bool value)
    {
        _animationsOn = value;

        foreach (KeyValuePair<int, GameObject> kvp in _agents)
        {
            kvp.Value.GetComponent<Animator>().enabled = _animationsOn;
        }
    }

    internal bool IsSimulationComplete()
    {
        return
            SimState == (int)Def.SimulationState.Finished ||
            SimState == (int)Def.SimulationState.Replaying;
    }

    internal void ToggleHeatmap(Def.HeatmapType heatmapType, bool smooth)
    {
        if (ActiveHeatmapType == heatmapType || heatmapType == (int)Def.HeatmapType.None)
        {
            ClearHeatMap();
            HideGradientLegend();
            return;
        }

        if (heatmapType != (int)Def.HeatmapType.None)
            ViewHeatmap(heatmapType, smooth);
    }

    public void HideGradientLegend()
    {
        GradientLegend.SetActive(false);
    }

    public void ShowGradientLegend(float maxHeat, string label)
    {
        GradientLegend.SetActive(true);
        GradientLabel.text = label;
        SetHeatmapTexts(maxHeat);
    }

    private void SetHeatmapTexts(float maxHeat)
    {
        if (maxHeat == 0f)
        {
            foreach (Text text in HeatmapTexts)
                text.text = "0.0";
        }
        else
        {
            string decimalFormat = "0.0";

            if (maxHeat < 1.0f)
                decimalFormat = "0.00";

            for (int i = 0; i < HeatmapTexts.Count; i++)
                HeatmapTexts[i].text = (i / (float)(HeatmapTexts.Count - 1) * Math.Abs(maxHeat)).ToString(decimalFormat);
        }

        GradientLegendImage.GetComponent<RectTransform>().localScale = maxHeat > 0 ?
            new Vector3(GradientLegendImage.transform.localScale.x, 1, GradientLegendImage.transform.localScale.z) :
            new Vector3(GradientLegendImage.transform.localScale.x, -1, GradientLegendImage.transform.localScale.z);
    }

    private void ViewHeatmap(Def.HeatmapType heatmapType, bool smooth)
    {
        if (SimState != (int)Def.SimulationState.Replaying)
        {
            Debug.Log(Strings.NoHeatmap);
            UIController.Instance.ShowGeneralDialog(Strings.NoHeatmap, "Export Heatmap");
            ClearHeatMap();
            return;
        }

        ClearHeatMap();
        ActiveHeatmapType = heatmapType;

        heatmapData = FileOperations.Instance.GenerateHeatmap(heatmapType, smooth);

        int l = 0;

        foreach (var heatmapTex in heatmapData.Tex)
        {
            GameObject heatmapObject = Instantiate(heatmapPrefab);
            heatmapObject.transform.localScale = new Vector3(heatmapTex.width / 20f, 1f, heatmapTex.height / 20f);
            heatmapObject.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
            heatmapObject.transform.position = Create.Instance.ToFh(new Vector3(
                heatmapData.XMin / 2f + heatmapTex.width / 4f - 0.25f,
                -0.9f,
                heatmapData.YMin / 2f + heatmapTex.height / 4f - 0.25f), l) + new Vector3(0, 0.1f, 0);

            heatmapObject.GetComponent<MeshRenderer>().material.mainTexture = heatmapTex;

            HeatmapObjects.Add(heatmapObject);

            l++;
        }

        float scaleMax = FileOperations.Instance.MaxHeat;

        string label = "";

        switch (heatmapType)
        {
            case Def.HeatmapType.AverageDensity:
                label = "Average Density (p/m²)";
                break;
            case Def.HeatmapType.MaxDensity:
                label = "Maximum Density (p/m²)";
                break;
            case Def.HeatmapType.AverageSpeed:
                label = "Average Speed (m/s)";
                break;
            case Def.HeatmapType.Utilization:
                label = "Utlization (usage)";
                break;
        }

        ShowGradientLegend(scaleMax, label);
        Create.Instance.ChangeSelectedLevel(Create.Instance.SelectedLevel);
    }

    internal void ClearHeatMap()
    {
        foreach (var heatmap in HeatmapObjects)
            Destroy(heatmap);
        HeatmapObjects.Clear();

        ActiveHeatmapType = (int)Def.HeatmapType.None;
    }

    internal void UpdateHeatmap(bool smoothHeatmap)
    {
        ViewHeatmap(ActiveHeatmapType, smoothHeatmap);
    }

    public void HideAllAgents()
    {
        _agentsHidden = !_agentsHidden;
        UpdateAgentVisibility(_agentsHidden);
        UIController.Instance.SetConsoleText(_agentsHidden ? "Agents Hidden." : "Agents Unhidden.");
    }

    private void UpdateAgentVisibility(bool isHidden)
    {
        foreach (IndividualAgent ia in FindObjectsOfType<IndividualAgent>())
            ia.HideAgentWhileRunning(isHidden);
    }
}