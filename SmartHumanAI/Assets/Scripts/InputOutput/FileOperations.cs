using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Assets.Scripts.DataFormats;
using Core;
using DataFormats;
using Helper;
using Info;
using Pathfinding;
using UnityEngine;
using UnityEngine.Analytics;
using Core.PositionUpdater.ReactionTimeHandler;
using Assets.Scripts;

namespace InputOutput
{
    public class FileOperations : MonoBehaviour
    {
        [DllImport("kernel32.dll")]
        public static extern int DeviceIoControl(IntPtr hDevice, int
            dwIoControlCode, ref short lpInBuffer, int nInBufferSize, IntPtr
            lpOutBuffer, int nOutBufferSize, ref int lpBytesReturned, IntPtr
            lpOverlapped);

        public static FileOperations Instance
        {
            get
            {
                lock (Padlock)
                {
                    return _instance ?? (_instance = new FileOperations());
                }
            }
        }


        }

        public float MaxHeat { get; private set; }

        private const string SSxMessage = "Need at least 9 cells in the timetable. X, Y, id, Depthmap_Ref, Vis_Connectivity, Acc_Connectivity, Acc_Integration	Isovist_Max_Radial, Isovist_Min_Radial";
        private List<DecisionUpdateOutput> decisionUpdateOutputs = new List<DecisionUpdateOutput>();
        private object decisionUpdateLock = new object();
        private Model _currentModel;
        private static FileOperations _instance;
        private static readonly object Padlock = new object();
        private Create _create;

        internal Model OpenModel(string path)
        {
            Analytics.CustomEvent("OpenModel");

            Model model = new Model();

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Model));
                StreamReader streamReader = new StreamReader(path);
                model = (Model)serializer.Deserialize(streamReader);
                streamReader.Close();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                return null;
            }

            return model;
        }

        internal bool SaveModel(Model model, string path)
        {
            Analytics.CustomEvent("SaveModel");

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Model));
                StreamWriter streamWriter = new StreamWriter(path);
                serializer.Serialize(streamWriter, model);
                streamWriter.Flush();
                streamWriter.Close();
                EnableNtfsCompression(path);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                return false;
            }
            return true;
        }

        internal Model ObjectsToModel()
        {
            if (_create == null)
            {
                _create = Create.Instance;
            }

            if (_create.CreatedObjects.Count == 0)
                Debug.Log("Model is empty.");

            Model model = new Model();

            if (Camera.main != null)
            {
                model.cameraPos = Vector3s.Convert(Camera.main.transform.position);
                model.cameraRot = Vector3s.Convert(Camera.main.transform.eulerAngles);
            }

           
                }

                foreach (Transform t in _create.CreationParent.GetChild(l))
                {
                    switch (t.name.Split('_')[0])
                    {
                        case Str.TicketGate:
                            TicketGateInfo tgi = t.GetComponent<TicketGateInfo>();

                            WaitingData wd = new WaitingData
                            {
                                waitTime = tgi.waitTime,
                                waitPosX = tgi.waitingPosition.position.x,
                                waitPosY = tgi.waitingPosition.position.z,
                                targetPosX = tgi.targetPosition.position.x,
                                targetPosY = tgi.targetPosition.position.z,
                                waitPos2X = tgi.waitingPosition2.position.x,
                                waitPos2Y = tgi.waitingPosition2.position.z,
                                targetPos2X = tgi.targetPosition2.position.x,
                                targetPos2Y = tgi.targetPosition2.position.z,
                                isBidirectional = tgi.isBidirectional
                            };

                            foreach (var gate in model.levels[l].gate_pkg.gates)
                            {
                                if (gate == null || tgi.gate == null)
                                    continue;

                                if (gate.id == tgi.gate.GetComponent<GateInfo>().Id)
                                {
                                    gate.waitingData = wd;
                                }
                            }

                            break;
                    }
                }
            }

            model.version = Application.version;
            model.validatedSuccessfully = ValidateModel(model);
            _create.ChangeSelectedLevel(_create.SelectedLevel);
            _currentModel = model;

            NetworkServer.Instance.SendModel(_currentModel);

            return model;
        }

        private static Gate GetGate(Gate gateWall, List<Stair> modelStairs, int level)
        {
            foreach (Stair stair in modelStairs)
            {
                if (stair.lower.level == level && ObjectInfo.AreTheSame(stair.lower.gate, gateWall))
                    return stair.lower.gate;

                if (stair.upper.level == level && ObjectInfo.AreTheSame(stair.upper.gate, gateWall))
                    return stair.upper.gate;
            }

            return gateWall;
        }

        private static Wall GetWall(Wall modelWall, IEnumerable<Stair> modelStairs, int level)
        {
            foreach (Stair stair in modelStairs)
            {
                if (stair.lower.level != level)
                    continue;

                foreach (Wall stairWall in stair.walls)
                {
                    if (ObjectInfo.AreTheSame(stairWall, modelWall))
                    {
                        return stairWall;
                    }
                }
            }

            return modelWall;
        }

        internal bool ValidateModel(Model model)
        {
            _create.ClearAllFlags();
            bool error = false;
            Dictionary<int, int> vertexCounts = new Dictionary<int, int>();
            Dictionary<int, Vertex> vertexIds = new Dictionary<int, Vertex>();

            bool isEmpty = true;
            int levelId = 0;
            foreach (Level level in model.levels)
            {
                List<Wall> walls = new List<Wall>();
                List<Gate> gates = new List<Gate>();

                foreach (Wall wall in level.wall_pkg.walls)
                {
                    isEmpty = false;
                    if (!walls.Contains(wall))
                    {
                        walls.Add(wall);
                        foreach (Vertex v in wall.vertices)
                        {
                            if (!vertexCounts.ContainsKey(v.id))
                            {
                                vertexCounts[v.id] = 1;
                            }
                            else
                            {
                                vertexCounts[v.id] += 1;
                            }
                            vertexIds[v.id] = v;
                        }
                    }
                    else
                    {
                        walls.Remove(wall);
                    }
                }
                foreach (Gate gate in level.gate_pkg.gates)
                {
                    if (gate.counter)
                        continue;

                    isEmpty = false;
                    if (!gates.Contains(gate))
                    {
                        gates.Add(gate);
                        foreach (Vertex v in gate.vertices)
                        {
                            if (!vertexCounts.ContainsKey(v.id))
                                vertexCounts[v.id] = 1;
                            else
                                vertexCounts[v.id] += 1;

                            vertexIds[v.id] = v;
                        }
                    }
                    else
                    {
                        Debug.LogError("Duplicate gate: " + gate);
                        error = true;
                    }

                }

                foreach (KeyValuePair<int, int> kvp in vertexCounts)
                {
                    if (kvp.Value >= 2) continue;
                    Vector3 flagPos = ToLh(new Vector3(vertexIds[kvp.Key].X, 0f, vertexIds[kvp.Key].Y), levelId);
                    _create.MakeErrorFlag(flagPos, levelId);
                    error = true;
                }
                levelId++;
                vertexCounts.Clear();
            }

            return !error && !isEmpty;
        }

        internal HeatmapData GenerateHeatmap(Def.HeatmapType heatmapType, bool smooth)
        {
            Debug.Log("GeneratingHeatmap of type " + heatmapType);

            int maxSize = Util.GetMaxSize();

            List<float[,]> heatmap = new List<float[,]>();

            for (int level = 0; level < UIController.Instance.NumLevels; level++)
            {
                heatmap.Add(new float[maxSize, maxSize]);

                foreach (GridNode node in ((GridGraph)AstarPath.active.data.graphs[level]).nodes)
                {
                    if (node.Walkable)
                    {
                        switch (heatmapType)
                        {
                            case Def.HeatmapType.Utilization:
                                heatmap[level][node.XCoordinateInGrid, node.ZCoordinateInGrid] =
                                    smooth ? node.GetUtilization_9Sq() : node.GetUtilization();
                                break;
                            case Def.HeatmapType.AverageDensity:
                                heatmap[level][node.XCoordinateInGrid, node.ZCoordinateInGrid] =
                                    smooth ? node.GetAverageDensity_9Sq() : node.GetAverageDensity();
                                break;
                            case Def.HeatmapType.MaxDensity:
                                heatmap[level][node.XCoordinateInGrid, node.ZCoordinateInGrid] =
                                    smooth ? node.GetMaxDensity_9Sq() : node.GetMaxDensity();
                                break;
                            case Def.HeatmapType.AverageSpeed:
                                heatmap[level][node.XCoordinateInGrid, node.ZCoordinateInGrid] =
                                    smooth ? node.GetAverageSpeed_9Sq() : node.GetAverageSpeed();
                                break;
                            case Def.HeatmapType.None:
                                Debug.LogError("Cannot generate a heatmap of type 'None'");
                                break;
                        }
                    }
                    else
                    {
                        heatmap[level][node.XCoordinateInGrid, node.ZCoordinateInGrid] = -1;
                    }
                }
            }

            int xMin = int.MaxValue;
            int yMin = int.MaxValue;
            int xMax = int.MinValue;
            int yMax = int.MinValue;
            MaxHeat = float.MinValue;

            for (int level = 0; level < UIController.Instance.NumLevels; level++)
            {
                for (int i = 0; i < maxSize; i++)
                {
                    for (int k = 0; k < maxSize; k++)
                    {
                        float value = heatmap[level][i, k];

                        if (!(value >= 0)) continue;
                        if (i < xMin)
                            xMin = i;
                        if (i > xMax)
                            xMax = i;
                        if (k < yMin)
                            yMin = k;
                        if (k > yMax)
                            yMax = k;
                        if (value > MaxHeat)
                            MaxHeat = value;
                    }
                }
            }

            if (Consts.HeatmapMaximums[(int)heatmapType] >= 0)
                MaxHeat = Consts.HeatmapMaximums[(int)heatmapType];

            int width = 1 + xMax - xMin;
            int height = 1 + yMax - yMin;

            List<Texture2D> textures = new List<Texture2D>();

            for (int level = 0; level < UIController.Instance.NumLevels; level++)
            {
                textures.Add(new Texture2D(width, height, TextureFormat.RGBA32, false));
                textures[level].wrapMode = TextureWrapMode.Clamp;

                for (int i = xMin; i <= xMax; i++)
                {
                    for (int k = yMin; k <= yMax; k++)
                    {
                        if (heatmap[level][i, k] < 0)
                        {
                            textures[level].SetPixel(i - xMin, k - yMin, new Color(0, 0, 0, 0));
                        }
                        else
                        {
                            float lerp = MaxHeat != 0 ? heatmap[level][i, k] / MaxHeat : 0;
                            textures[level].SetPixel(i - xMin, k - yMin, Util.LerpToColor(lerp, Def.Colors));
                        }
                    }
                }

                textures[level].Apply();
            }

            HeatmapData data = new HeatmapData
            {
                Tex = textures,
                XMin = xMin,
                YMin = yMin,
                width = width,
                height = height
            };

            return data;
        }

        internal void ExportHeatmap(Texture2D texture, Def.HeatmapType heatmapType, bool smooth)
        {
            string suffix = Enum.GetName(typeof(Def.HeatmapType), heatmapType);

            if (smooth)
                suffix += "(Smooth)";

            string path = Application.dataPath + "Export_" + UIController.Instance.ModelName + "/";

            if (string.IsNullOrEmpty(ConfigurationFile.outputFile))
            {
                Directory.CreateDirectory(path);
                path += "Heatmap " + suffix + ".png";
            }
            else
            {
                path = Environment.CurrentDirectory + "/" + ConfigurationFile.outputFile.Split('.')[0];
                path += suffix + "_pixelperfect.png";
            }

            if (File.Exists(path))
                File.Delete(path);

            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        internal void ModelToObjects(Model model)
        {
            if (model == null) return;

            UIController uic = UIController.Instance;

            _create = Create.Instance;
            _create.ClearAll();
            _create.UpdateCameraLocation(model.cameraPos.ToV3(), model.cameraRot.ToV3());

            if (model.cameraPos != Vector3s.zero)
            {
                Camera.main.transform.position = model.cameraPos.ToV3();
                Camera.main.transform.eulerAngles = model.cameraRot.ToV3();
            }

            BehaviourSwitcher behaviourSwitcher = FindObjectOfType<BehaviourSwitcher>();

            if (model.panicMode != Params.panicMode)
                UIController.Instance.ChangeMode(model.panicMode);

            Create.Instance.ChangeLevelHeight(model.levelHeight);

            foreach (Level level in model.levels)
            {
                if (level.width != -1 && level.height != -1)
                {
                    _create.Width = level.width;
                    _create.Height = level.height;
                }
                else
                {
                    _create.Width = _create.Height = 49;
                }

                uic.LevelAddLevel();

                foreach (Wall wall in level.wall_pkg.walls)
                    _create.WallToObject(wall);

                foreach (Gate gate in level.gate_pkg.gates)
                    _create.GateToObject(gate);

                foreach (Gate counterGate in level.counter_pkg.gates)
                    _create.GateToObject(counterGate);

                foreach (CircularObstacle obstacle in level.obstacle_pkg.Obstacles)
                    _create.ObstacleToObject(obstacle);

                foreach (DistributionData agent in level.agent_pkg.distributions)
                    _create.AgentToObject(agent);

                foreach (Wall barricade in level.barricade_pkg.barricade_walls)
                    _create.WallToObject(barricade, true);

                foreach (WaitPoint waitPoint in level.waitPoint_pkg.waitPoints)
                    _create.WaitPointToObject(waitPoint);

                foreach (Threat threat in model.threats)
                {
                    if (threat.LevelId == level.id)
                        _create.ThreatToObject(threat);
                }

                foreach (Train train in level.train_pkg.trains)
                    _create.TrainToObject(train);

                PostLevelActions();
            }

            foreach (Stair stair in model.stairs)
            {
                _create.StairToObject(stair);
            }

            foreach (AgentDistInfo adi in FindObjectsOfType<AgentDistInfo>())
            {
                adi.ID = ObjectInfo.Instance.ArtifactId++;
            }

           

        private void PostLevelActions()
        {
            #region TicketGates
            // Remove duplicate ticketgates barricades.
            foreach (var tgi in FindObjectsOfType<TicketGateInfo>())
            {
                foreach (var otherWall in FindObjectsOfType<WallInfo>())
                {
                    if (otherWall.transform.parent.name.Split('_')[0] == "Level")
                    {
                        for (int i = 0; i < tgi.barricades.Count; i++)
                        {
                            WallInfo ticketGateBarrier = tgi.barricades[i].GetComponent<WallInfo>();

                            if (ObjectInfo.AreTheSame(otherWall.Get, ticketGateBarrier.Get))
                            {
                                Destroy(otherWall.gameObject);
                            }
                        }
                    }
                }
            }

            // Move remaining barricades to the correct level.
            foreach (var tgi in FindObjectsOfType<TicketGateInfo>())
            {
                foreach (var barricade in tgi.barricades)
                {
                    barricade.SetParent(_create.CurrentLevelTransform());
                }
            }
            #endregion

            /*
             #region Trains/Trams

            List<GameObject> toDestroy = new List<GameObject>();

            foreach (var ti in FindObjectsOfType<TrainInfo>())
            {
                if (!ti.transform.parent.Equals(Create.Instance.CurrentLevelTransform()))
                    continue;

                foreach (var otherWall in FindObjectsOfType<WallInfo>())
                {
                    if (otherWall != null && otherWall.transform.parent.Equals(Create.Instance.CurrentLevelTransform()))
                    {
                        for (int i = 0; i < ti.wallList.Count; i++)
                        {
                            WallInfo trainWall = ti.wallList[i].GetComponent<WallInfo>();

                            if (ObjectInfo.AreTheSame(otherWall.Get, trainWall.Get))
                            {
                                toDestroy.Add(trainWall.gameObject);
                                ti.wallList[i] = otherWall.transform;
                                foreach (var meshR in ti.wallList[i].GetComponentsInChildren<MeshRenderer>())
                                    Destroy(meshR);
                            }
                        }
                    }
                }

                foreach (var otherGate in FindObjectsOfType<GateInfo>())
                {
                    if (otherGate != null && otherGate.transform.parent.Equals(Create.Instance.CurrentLevelTransform()))
                    {
                        for (int i = 0; i < ti.gateList.Count; i++)
                        {
                            GateInfo trainGate = ti.gateList[i].GetComponent<GateInfo>();

                            if (ObjectInfo.AreTheSame(otherGate.Get, trainGate.Get))
                            {
                                toDestroy.Add(trainGate.gameObject);

                                otherGate.IsTrainDoor = true;

                                ti.gateList[i] = otherGate.transform;
                                TrainDoorInfo trainDoorInfo = ti.gateList[i].GetComponent<TrainDoorInfo>();

                                if (trainDoorInfo == null)
                                    trainDoorInfo = ti.gateList[i].gameObject.AddComponent<TrainDoorInfo>();

                                trainDoorInfo.CopyFrom(trainGate.GetComponent<TrainDoorInfo>());
                                trainDoorInfo.trainID = ti.ObjectId;

                                foreach (var meshR in ti.gateList[i].GetComponentsInChildren<MeshRenderer>())
                                    Destroy(meshR);
                            }
                       
            {
                StreamWriter streamOut = new StreamWriter(path, true);

                List<string> line = new List<string>();

                if (SimulationController.Instance.CurrentRunCount == 1)
                {
                    line.Clear();
                    line.Add("Run Number");
                    line.Add("Agent ID");
                    line.Add("Time");
                    line.Add("X");
                    line.Add("Y");
                    line.Add("Z");
                    line.Add("Speed");
                    WriteLine(streamOut, line);
                }

                streamOut.Flush();
                streamOut.Close();
                EnableNtfsCompression(path);
                streamOut = new StreamWriter(path, true);

                foreach (KeyValuePair<int, GameObject> agentKvp in SimulationController.Instance.GetAgents())
                {
                    IndividualAgent agent = agentKvp.Value.GetComponent<IndividualAgent>();

                    Vector3 previous = Vector3.zero;
                    Vector3 current;

                    var timeMultiplier = Params.Current.TimeStep * Params.Current.UiUpdateCycle;

                    for (int time = 0; time < agent.pathList.Count; time++)
                    {
                        current = agent.pathList[time];

                        line.Clear();
                        line.Add(SimulationController.Instance.CurrentRunCount.ToString());
                        line.Add((agentKvp.Key + 1).ToString());
                        line.Add((time * timeMultiplier).ToString());
                        line.Add(current.x.ToString());
                        line.Add(current.z.ToString());
                        line.Add(current.y.ToString());

                        if (time > 0)
                        {
                            float speed = Vector3.Distance(previous, current) / timeMultiplier;
                            line.Add(speed.ToString());
                        }
                        else
                        {
                            line.Add("");
                        }

                        previous = current;

                        WriteLine(streamOut, line);
                    }
                }
                streamOut.Flush();
                streamOut.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("Exporting CSV Error: " + e);
                return false;
            }
            return true;
        }

        internal bool ExportDecisionUpdates(string path)
        {
            path = path.Insert(path.Length - 4, "(DecisionUpdate)");

            try
            {
                StreamWriter streamOut = new StreamWriter(path, true);

                List<string> line = new List<string>();

                if (SimulationController.Instance.CurrentRunCount == 1)
                {
                    line.Clear();
                    line.Add("RunNumber");
                    line.Add("Probability");
                    line.Add("Successful");
                    line.Add("Tried:");
                    line.Add("Success:");

                    WriteLine(streamOut, line);
                }

                streamOut.Flush();
                streamOut.Close();
                EnableNtfsCompression(path);
                streamOut = new StreamWriter(path, true);

                int count = 0;
                int success = 0;

                lock (decisionUpdateLock)
                {
                    foreach (var dUpdateData in decisionUpdateOutputs)
                    {
                        if (dUpdateData.wasSuccessful)
                            success++;
                        count++;
                    }
                }

                lock (decisionUpdateLock)
                {
                    for (int index = 0; index < decisionUpdateOutputs.Count; index++)
                    {
                        var dUpdateData = decisionUpdateOutputs[index];

                        line.Clear();
                        line.Add(SimulationController.Instance.CurrentRunCount.ToString());
                        line.Add(dUpdateData.probability.ToString());
                        line.Add(Convert.ToInt32(dUpdateData.wasSuccessful).ToString());

                        if (index == 0)
                        {
                            line.Add(count.ToString());
                            line.Add(success.ToString());
                        }

                        WriteLine(streamOut, line);
                    }
                }
                streamOut.Flush();
                streamOut.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("Exporting CSV Error: " + e);
                return false;
            }
            return true;
        }

        internal bool ExportReactionTimes(string path)
        {
            path = path.Insert(path.Length - 4, "(ReactionTimes)");

            try
            {
                StreamWriter streamOut = new StreamWriter(path, true);

                List<string> line = new List<string>();

                if (SimulationController.Instance.CurrentRunCount == 1)
                {
                    line.Clear();
                    line.Add("RunNumber");
                    line.Add("AgentID");
                    line.Add("MinDistance");
                    line.Add("ReactionTime");
                    WriteLine(streamOut, line);
                }

                streamOut.Flush();
                streamOut.Close();
                EnableNtfsCompression(path);
                streamOut = new StreamWriter(path, true);

                List<AgentRectionTimeData> reactionDatas =
                    new List<AgentRectionTimeData>(SimulationController.Instance.reactionTimeData);

                foreach (var reactionData in reactionDatas)
                {
                    line.Clear();
                    line.Add(SimulationController.Instance.CurrentRunCount.ToString());
                    line.Add(reactionData.AgentID.ToString());
                    line.Add(reactionData.MinDistance.ToString());
                    line.Add(reactionData.ReactionTime.ToString());
                    WriteLine(streamOut, line);

                }

                streamOut.Flush();
                streamOut.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("Exporting CSV Error: " + e);
                return false;
            }
            return true;
        }

        internal bool ExportDataEvacTimes(string path)
        {
            string originalPath = path;

            path = originalPath.Insert(originalPath.Length - 4, "(EvacTimes)");
            string pathStats = originalPath.Insert(originalPath.Length - 4, "(EvacTimeStats)");

            try
            {
                StreamWriter streamOut = new StreamWriter(path, true);
                StreamWriter streamOutStats = new StreamWriter(pathStats, true);

                List<string> line = new List<string>();
                List<string> lineStats = new List<string>();

                if (SimulationController.Instance.CurrentRunCount == 1)
                {
                    line.Clear();
                    lineStats.Clear();

                    line.Add("Run Number");
                    line.Add("Agent ID");
                    line.Add("Termination");
                    line.Add("Generation");
                    line.Add("Running Time");
                    line.Add("Total Evac");
                    line.Add("Average Evac");

                    lineStats.Add("Run Number");
                    lineStats.Add("Total Evac");
                    lineStats.Add("Average Evac");

                    WriteLine(streamOut, line);
                    WriteLine(streamOutStats, lineStats);
                }

                streamOut.Flush();
                streamOut.Close();
                EnableNtfsCompression(path);
                streamOut = new StreamWriter(path, true);

                streamOutStats.Flush();
                streamOutStats.Close();
                EnableNtfsCompression(pathStats);
                streamOutStats = new StreamWriter(pathStats, true);

                SortedDictionary<int, float> evacTimes = SimulationController.Instance.GetEvacuationTimes();
                SortedDictionary<int, int> generationCycles = SimulationController.Instance.GetGenerationCycles();
                SortedDictionary<int, float> runningTimes = new SortedDictionary<int, float>();

                if (evacTimes.Count != generationCycles.Count)
                    Debug.LogError("Mismatch between generationCycles and evactimes");

                foreach (var kvp in evacTimes)
                    runningTimes.Add(kvp.Key, kvp.Value - generationCycles[kvp.Key] * Params.Current.TimeStep);

                bool firstLineWritten = false;

                foreach (var kvp in evacTimes)
                {
                    line.Clear();
                    line.Add(SimulationController.Instance.CurrentRunCount.ToString());
                    line.Add(kvp.Key.ToString());
                    float evac = kvp.Value;
                    float genr = generationCycles[kvp.Key] * Params.Current.TimeStep;
                    line.Add(evac.ToString());
                    line.Add(genr.ToString());
                    line.Add((evac - genr).ToString());

                    if (!firstLineWritten)
                    {
                        lineStats.Clear();
                        lineStats.Add(SimulationController.Instance.CurrentRunCount.ToString());
                        lineStats.Add(TotalEvac(evacTimes).ToString());
                        lineStats.Add(AverageEvac(runningTimes).ToString());
                        WriteLine(streamOutStats, lineStats);

                        line.Add(TotalEvac(evacTimes).ToString());
                        line.Add(AverageEvac(runningTimes).ToString());

                        firstLineWritten = true;
                    }

                    WriteLine(streamOut, line);
                }

                streamOut.Flush();
                streamOut.Close();

                streamOutStats.Flush();
                streamOutStats.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("Exporting CSV Error: " + e);
                return false;
            }
            return true;
        }

        internal bool ExportDataGateShares(string path)
        {
            path = path.Insert(path.Length - 4, "(GateShares)");

            Dictionary<int, Dictionary<int, int>> gateSharesData =
                SimulationController.Instance.GateSharesData;

            if (gateSharesData == null || gateSharesData.Count < 1)
                return true;

            try
            {
                StreamWriter streamOut = new StreamWriter(path, true);

                List<string> line = new List<string>();

                if (SimulationController.Instance.CurrentRunCount == 1)
                {
                    line.Clear();
                    line.Add("Run Number");
                }

                if (SimulationController.Instance.CurrentRunCount == 1)
                {
                    foreach (var kvp in gateSharesData)
                    {
                        int levelID = kvp.Key;

                        foreach (Gate gate in CurrentModel.levels[levelID].gate_pkg.gates)
                            line.Add(gate.id.ToString());
                    }

                    WriteLine(streamOut, line);
                }

                streamOut.Flush();
                streamOut.Close();
                EnableNtfsCompression(path);
                streamOut = new StreamWriter(path, true);

                line.Clear();
                line.Add(SimulationController.Instance.CurrentRunCount.ToString());

                foreach (var kvp in gateSharesData)
                {
                    int levelID = kvp.Key;

                    foreach (Gate gate in CurrentModel.levels[levelID].gate_pkg.gates)
                    {
                        int usage = 0;

                        foreach (var gateShare in kvp.Value)
                        {
                            if (gateShare.Key == gate.id)
                            {
                                usage = gateShare.Value;
                                break;
                            }
                        }

                        line.Add(usage.ToString());
                    }
                }

                WriteLine(streamOut, line);

                streamOut.Flush();
                streamOut.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("Exporting CSV Error: " + e);
                return false;
            }
            return true;
        }

        public bool ExportSimulationTimesReal(string path)
        {
            path = path.Insert(path.Length - 4, "(RealTimes)");

            try
            {
                StreamWriter streamOut = new StreamWriter(path, true);

                List<string> line = new List<string>();
                if (SimulationController.Instance.CurrentRunCount == 1)
                {
                    line.Clear();
                    line.Add("Run Number");
                    line.Add("Time");
                    line.Add("Mem (MB)");
                    WriteLine(streamOut, line);
                }

                streamOut.Flush();
                streamOut.Close();
                EnableNtfsCompression(path);
                streamOut = new StreamWriter(path, true);

                if (SimulationController.Instance.LastSimulationTime > 0)
                {
                    line.Clear();
                    line.Add(SimulationController.Instance.CurrentRunCount.ToString());
                    line.Add(SimulationController.Instance.LastSimulationTime.ToString());
                    line.Add(((GC.GetTotalMemory(true) / 1024f) / 1024f).ToString());
                    WriteLine(streamOut, line);
                }

                streamOut.Flush();
                streamOut.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("Exporting CSV Error: " + e);
                return false;
            }
            return true;
        }

        internal void ClearDecisionUpdates()
        {
            lock (decisionUpdateLock)
                decisionUpdateOutputs.Clear();
        }

        private float TotalEvac(SortedDictionary<int, float> evacTimes)
        {
            float maxTime = float.MinValue;

            foreach (var kvp in evacTimes)
            {
                if (kvp.Value > maxTime)
                    maxTime = kvp.Value;
            }

            return maxTime;
        }

        private float AverageEvac(SortedDictionary<int, float> evacTimes)
        {
            float totalTime = 0f;

            foreach (var kvp in evacTimes)
            {
                totalTime += kvp.Value;
            }

            return totalTime / evacTimes.Count;
        }

        internal void AddDecisionUpdate(DecisionUpdateOutput decisionUpdateOutput)
        {
            lock (decisionUpdateLock)
                decisionUpdateOutputs.Add(decisionUpdateOutput);
        }

        private static void WriteLine(TextWriter streamOut, IEnumerable<string> line)
        {
            string wholeLine = "";
            foreach (string cell in line)
            {
                wholeLine += cell + ",";
            }
            streamOut.WriteLine(wholeLine);
        }

        private static int EnableNtfsCompression(string path)
        {
            int lpBytesReturned = 0;
            const int fsctlSetCompression = 0x9C040;
            short compressionFormatDefault = 1;
            int ret = 0;

            FileStream f;

            try
            {
                f = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                ret = DeviceIoControl(f.Handle, fsctlSetCompression,
                    ref compressionFormatDefault, 2, IntPtr.Zero, 0,
                    ref lpBytesReturned, IntPtr.Zero);
                f.Close();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }

            return ret;
        }

        public List<TimePopulation> ReadPopulationTimetable(string resultPathTimetable)
        {
            List<TimePopulation> timePopulations = new List<TimePopulation>();

            if (!File.Exists(resultPathTimetable))
                return timePopulations;

            StreamReader streamReader;

            try
            {
                streamReader = new StreamReader(resultPathTimetable);
            }
            catch (IOException)
            {
                UIController.Instance.ShowGeneralDialog("Error in reading " + resultPathTimetable + ". Please check that it is not open in another process.", "Reading Error");
                return timePopulations;
            }

            float time = 0f;
            int population = 0;

            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                string[] cells = line.Split(',');

                if (cells.Length < 2)
                {
                    Debug.Log("Need at least 2 cells in the timetable. (Time(s) : Population)");
                    timePopulations.Clear();
                    return timePopulations;
                }

                if (float.TryParse(cells[0], out time) && int.TryParse(cells[1], out population))
                {
                    if (timePopulations.Count > 0 && time < timePopulations[timePopulations.Count - 1].time)
                    {
                        Debug.Log("Detected decreasing time. Make sure timetable is in ascending time order.");
                        timePopulations.Clear();
                        return timePopulations;
                    }

                    timePopulations.Add(new TimePopulation(time, population));
                }
            }

            return timePopulations;
        }

        public Dictionary<int, List<TrainData>> ReadTrainTimetable(string resultPathTimetable)
        {
            Dictionary<int, List<TrainData>> trainData = new Dictionary<int, List<TrainData>>();

            if (!File.Exists(resultPathTimetable))
                return trainData;

            StreamReader streamReader;

            try
            {
                streamReader = new StreamReader(resultPathTimetable);
            }
            catch (IOException)
            {
                UIController.Instance.ShowGeneralDialog("Error in reading " + resultPathTimetable + ". Please check that it is not open in another process.", "Reading Error");
                return trainData;
            }

            int trainID = 0;
            float arrivalTime = 0f;
            float departureTime = 0f;
            int population = 0;

            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                string[] cells = line.Split(',');

                if (cells.Length < 4)
                {
                    Debug.Log("Need at least 4 cells in the timetable. (Train ID, Arriving, Departing, People)");
                    trainData.Clear();
                    return trainData;
                }

                if (int.TryParse(cells[0], out trainID) &&
                    float.TryParse(cells[1], out arrivalTime) &&
                    float.TryParse(cells[2], out departureTime) &&
                    int.TryParse(cells[3], out population))
                {
                    TrainData td = new TrainData();
                    td.arrivalTime = arrivalTime;
                    td.departureTime = departureTime;
                    td.passengers = population;

                    if (cells.Length > 4) // User has chosen to specify carriage population details
                    {
                        td.passengersInCarriages = new List<int>();

                        for (int i = 3; i < cells.Length; i++)
                        {
                            int passengers = 0;
                            if (int.TryParse(cells[i], out passengers))
                                td.passengersInCarriages.Add(passengers);
                        }

                        td.passengers = td.passengersInCarriages.Sum();
                    }

                    if (trainData.ContainsKey(trainID))
                        trainData[trainID].Add(td);
                    else
                        trainData.Add(trainID, new List<TrainData> { td });
                }
            }

            return trainData;
        }

        public Dictionary<int, DistributionInformation> ReadDistributionTable(string resultPathTimetable)
        {
            var final = new Dictionary<int, DistributionInformation>();
            var desDataDicts = new Dictionary<int, Dictionary<int, DesignatedGatesData>>();
            var totalPops = new Dictionary<int, List<int>>();

            if (!File.Exists(resultPathTimetable))
                return final;

            StreamReader streamReader;

            try
            {
                streamReader = new StreamReader(resultPathTimetable);
            }
            catch (IOException)
            {
                UIController.Instance.ShowGeneralDialog("Error in reading " + resultPathTimetable + ". Please check that it is not open in another process.", "Reading Error");
                return final;
            }

            List<int> targetIDs = new List<int>();
            List<int> times = new List<int>();
            List<int> distribution = new List<int>();
            bool newTimeSlice = true;
            int currentTime = -1;

            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();

                string[] oldCells = line.Split(',');
                List<string> cells = new List<string>();

                foreach (var str in oldCells)
                    if (!string.IsNullOrEmpty(str))
                        cells.Add(str);

                if (newTimeSlice)
                {
                    currentTime = int.Parse(cells[0]);

                    if (times.Count == 0 || currentTime > times[times.Count - 1])
                        times.Add(currentTime);

                    if (cells.Count == 1)
                        break;

                    for (int i = 1; i < cells.Count; i++)
                        targetIDs.Add(int.Parse(cells[i]));

                    newTimeSlice = false;
                }
                else
                {
                    if (cells.Count == 0)
                    {
                        newTimeSlice = true;
                        continue;
                    }

                    distribution.Clear();

                    int agentDistID = int.Parse(cells[0]);

                    if (!desDataDicts.ContainsKey(agentDistID))
                        desDataDicts[agentDistID] = new Dictionary<int, DesignatedGatesData>();

                    if (!totalPops.ContainsKey(agentDistID))
                        totalPops[agentDistID] = new List<int>();

                    if (!desDataDicts[agentDistID].ContainsKey(currentTime))
                        desDataDicts[agentDistID][currentTime] = new DesignatedGatesData();

                    for (int i = 1; i < cells.Count; i++)
                    {
                        distribution.Add(int.Parse(cells[i]));
                    }

                    int total = distribution.Sum();
                    totalPops[agentDistID].Add(total);

                    for (int i = 0; i < distribution.Count; i++)
                    {
                        desDataDicts[agentDistID][currentTime].AddData(targetIDs[i], (distribution[i] / (float)total) * 100f);
                    }
                }
            }

            foreach (var kvp in desDataDicts)
            {
                var di = new DistributionInformation();

                di.DesignatedGatesDatas = new List<DesignatedGatesData>();
                di.Times = times;
                di.PeoplePerSecond = new List<float>();
                di.TotalPopulation = totalPops[kvp.Key].Sum();

                foreach (var kvp2 in kvp.Value)
                {
                    var dgd = kvp2.Value;
                    dgd.startSeconds = kvp2.Key;
                    di.DesignatedGatesDatas.Add(dgd);
                }

                for (var i = 0; i < totalPops[kvp.Key].Count; i++)
                {
                    di.PeoplePerSecond.Add((float)totalPops[kvp.Key][i] / (times[i + 1] - times[i]));
                }

                final.Add(kvp.Key, di);
            }

                       break;
            }
        }

    }

    public class DistributionInformation
    {
        public List<int> Times;
        public List<DesignatedGatesData> DesignatedGatesDatas;
        public List<float> PeoplePerSecond;
        public int TotalPopulation;
    }

    public class DecisionUpdateOutput
    {
        public float probability = 0f;
        public bool wasSuccessful = false;

        public DecisionUpdateOutput()
        {
        }

        public DecisionUpdateOutput(float prob, bool success)
        {
            probability = prob;
            wasSuccessful = success;
        }
    }
}