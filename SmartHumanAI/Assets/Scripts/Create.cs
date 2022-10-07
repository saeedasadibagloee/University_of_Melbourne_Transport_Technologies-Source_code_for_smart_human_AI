using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Core;
using DataFormats;
using Helper;
using Info;
using InputOutput;
using Pathfinding;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using Assets.Scripts.DataFormats;
using Assets.Scripts.ObjectsInfo;
using Core.Logger;
using TMPro;

public class Create : MonoBehaviour
{
    private const float UpDownAngle = 90;
    //private const float LeftRightAngle = 0;
    private static Create _instance = null;
    private static readonly object Padlock = new object();

    private readonly List<GateInfo> gateShareInfos = new List<GateInfo>();

    public List<Material> MaterialsOpaque;
    public List<Material> MaterialsTransparent;
    public List<GameObject> Prefabs;

    public List<List<int>> DestinationGates = null;
    public List<List<int>> IntermediateGates = null;

    public Dictionary<int, Vertex> currentVIDs = null;
    public List<List<int[]>> currentRoomRegions = null;

    public enum Prefab
    {
        SPole, EPole, Wall, Gate, Barrier, Obstacle, Agent, ErrorFlag, StairsHalfLanding, StairsStraight, AvoidCircle, Danger, GateShares, AvoidSquare, AgentSquare, FireSource, Train, TicketGate, Tram, StairsEscalator,
        Road, WaitPoint, PlatformQueue, BenchSeat, ExitSign
    }

    public Transform CreationParent;
    public int NumLevels = 0;

    public int Width = 49;
    public int Height = 49;
    public TextMeshProUGUI _creationInfoText;
    public List<Text> SelectedObjectTexts = new List<Text>();
    public Text SelectedLevelText;
    public TextMeshProUGUI MouseCoordinatesText;

    private readonly Stack<GameObject> _createdObjects = new Stack<GameObject>();
    private readonly Dictionary<int, GameObject> _levels = new Dictionary<int, GameObject>();
    private readonly List<GameObject> _roomFloors = new List<GameObject>();
    private readonly Dictionary<int, Dictionary<int, float>> _roomAreas = new Dictionary<int, Dictionary<int, float>>();
    internal bool _smallGridSnap = false;
    internal bool WallSnapEnabled = true;
    private bool _creating = false;
    private GameObject _editingObject;
    private Vector3 prefabStart;
    private BaseObject _editingInfo;

    public Def.Object _selectedObject = Def.Object.None;
    private Vector3 _cameraPos = Vector3.zero;
    private Vector3 _cameraRot = Vector3.zero;

    public readonly Vector3 trainOffset = new Vector3(0, -1.225f, 0);
    private readonly Vector3 WaitPointOffset = new Vector3(0, -0.33f, 0);

    private readonly float _creationHeight = -1f;
    public int SelectedLevel = 0;
    public float Distance = 0;
    private readonly Dictionary<int, List<Vector3>> _snappingPolesWalls = new Dictionary<int, List<Vector3>>();
    private readonly Dictionary<int, List<Vector3>> _snappingPolesBarricades = new Dictionary<int, List<Vector3>>();
    private readonly List<GameObject> _errorFlags = new List<GameObject>();
    private bool _reset = false;
    private int fillFloors = 0;

    private Vector3 _savedMousePosition = Vector3.zero;
    //private bool _removeDuplicateStairWallsAndGates = false;

    private GraphCollision _graphCollisionSettings = null;

    internal bool displayAllLevels = false;
    private Renderer[] prevRenderers;
    private Color[] previousColor;

    public bool lowBarricade = false;

    public Stack<GameObject> CreatedObjects
    {
        get
        {
            return _createdObjects;
        }
    }

    /// <summary>
    /// Singleton pattern for referencing the Create class staticly.
    /// </summary>
    public static Create Instance
    {
        get
        {
            lock (Padlock)
            {
                return _instance ?? (_instance = FindObjectOfType<Create>());
            }
        }
    }

    private MeshRenderer _meshRendererStartPole;
    private MeshRenderer _meshRendererEndPole;
    private bool continuePlatformQueue = false;

    public MeshRenderer MeshRendererStartPole
    {
        get
        {
            return _meshRendererStartPole ?? (_meshRendererStartPole = Prefabs[(int)Prefab.SPole].GetComponent<MeshRenderer>());
        }
    }
    public MeshRenderer MeshRendererEndPole
    {
        get
        {
            return _meshRendererEndPole ?? (_meshRendererEndPole = Prefabs[(int)Prefab.EPole].GetComponent<MeshRenderer>());
        }
    }

    private bool notificationUnshown = false;

    // Use this for initialization
    public void Start()
    {
        _creationInfoText.text += Application.version;
        SelectedLevel = 0;

        Transform camera1 = Camera.main.transform;
        UpdateCameraLocation(camera1.position, camera1.eulerAngles);

        string message = "Application started. Environment: " + Environment.Version + " Threads: " + Constants.NCores;
        //Debug.Log(message);
        LogWriter.logDir = Application.dataPath;
        LogWriter.Instance.WriteToLog(message);
    }

    // Update is called once per frame
    public void Update()
    {
        RemovePriorHighlighting();

        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (!_creating && Input.GetMouseButtonDown(0))
            {
                StartClick();
                UIController.Instance.CloseAllMenus();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                EndClick();
            }
            else
            {
                if (_creating)
                    Adjust();
                else if (!Input.GetMouseButton(1))
                    HighlightUnderMouse();
                UpdateMouseCoordinates();
            }
        }

        if (_reset)
            ResetNow();

        if (fillFloors == 1)
            fillFloors = 2;
        else if (fillFloors == 2)
            FillFloors();
    }

    /// <summary>
    /// Mouseover function
    /// </summary>
    private void HighlightUnderMouse()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        int layerMask = 1 << 8; //Created
        int layerMask2 = 1 << 9; //Ground
        int layerMask3 = 1 << 10; //Clickable

        Physics.Raycast(ray, out hit, float.PositiveInfinity, layerMask | layerMask2 | layerMask3);

        if (hit.transform == null)
            return;

        Transform target = hit.transform;

        if (hit.transform.name.Split('_')[0] == Str.Stairs1 && hit.transform.parent.name.Split('_')[0] == Str.Escalator)
        {
            target = hit.transform.parent;
        }

        if (target.name == Str.Agent && !target.GetComponent<IndividualAgent>().GetLowPoly())
            prevRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>();
        else
            prevRenderers = target.GetComponentsInChildren<MeshRenderer>();

        if (target.name.Split('_')[0] == Str.WaitPoint)
        {
            var array = prevRenderers.ToList();
            array.AddRange(target.GetComponentsInChildren<ParticleSystemRenderer>());
            prevRenderers = array.ToArray();
        }

        if (prevRenderers.Length > 0)
        {
            DensityDisplay densityDisplay = target.GetComponent<DensityDisplay>();
            if (densityDisplay != null)
                densityDisplay.Highlighted(true);

            previousColor = new Color[prevRenderers.Length];

            if (target.name.Split('_')[0] != Str.RoomFloor &&
                target.name.Split('_')[0] != Str.Agent &&
                target.name.Split('_')[0] != Str.WaitPoint &&
                target.name.Split('_')[0] != Str.Train &&
                target.name.Split('_')[0] != Str.Tram &&
                target.name.Split('_')[0] != Str.Escalator &&
                target.name.Split('_')[0] != Str.Stairs &&
                target.name.Split('_')[0] != Str.Stairs1 &&
                target.name.Split('_')[0] != Str.Stairs2)
                target.localScale = target.localScale + new Vector3(0.01f, 0.01f, 0.01f);

            for (int i = 0; i < prevRenderers.Length; i++)
            {
                if (prevRenderers[i].material.HasProperty("_Color"))
                {
                    previousColor[i] = prevRenderers[i].material.color;
                    prevRenderers[i].material.color = Color.red;
                }
            }
        }
    }

    private void RemovePriorHighlighting()
    {
        if (prevRenderers != null)
        {
            for (int i = 0; i < prevRenderers.Length; i++)
            {
                if (prevRenderers[i] != null)
                    if (prevRenderers[i].material.HasProperty("_Color"))
                        prevRenderers[i].material.color = previousColor[i];
            }

            if (prevRenderers.Length > 0 && prevRenderers[0] != null)
            {
                if (prevRenderers[0].transform.name.Split('_')[0] != Str.RoomFloor &&
                    prevRenderers[0].transform.name != "commMan_:Barney_LOD3" && //Agent (3D)
                    prevRenderers[0].transform.name != "CircleLowPoly" && //Agent (2D)
                    prevRenderers[0].transform.name != "BarneyAgentLODLow(Clone)" && //Agent
                    prevRenderers[0].transform.name != "door01_01_open" && //Tram
                    prevRenderers[0].transform.name != "Black_01" && //Tram
                    prevRenderers[0].transform.name != "Loft102" &&
                    prevRenderers[0].transform.name.Split('_')[0] != "escalator" &&
                    prevRenderers[0].transform.name.Split('_')[0] != Str.WaitPoint &&
                    prevRenderers[0].transform.name.Split('_')[0] != Str.Stairs &&
                    prevRenderers[0].transform.name.Split('_')[0] != Str.Train &&
                    prevRenderers[0].transform.name.Split('_')[0] != "TrainCarriage" &&
                    prevRenderers[0].transform.name.Split('_')[0] != Str.Stairs1 &&
                    prevRenderers[0].transform.name.Split('_')[0] != Str.Stairs2) //Train
                    prevRenderers[0].transform.localScale = prevRenderers[0].transform.localScale - new Vector3(0.01f, 0.01f, 0.01f);

                DensityDisplay densityDisplay = prevRenderers[0].transform.GetComponentInParent<DensityDisplay>();
                if (densityDisplay != null)
                    densityDisplay.Highlighted(false);
            }
        }

        prevRenderers = null;
    }

    private void UpdateMouseCoordinates()
    {
        Vector3 mousePoint = GetWorldPoint(false);
        MouseCoordinatesText.text = "(" + mousePoint.x.ToString("0.0") + ", " + mousePoint.z.ToString("0.0") + ")";
    }

    /// <summary>
    /// This saves the options on the navmesh graph edited in the Editor 
    /// in case we delete the graphs when removing levels.
    /// </summary>
    internal void SaveGraphCollisionOptions()
    {
        _graphCollisionSettings = AstarPath.active.data.gridGraph.collision;
        foreach (GridGraph gg in AstarData.active.data.graphs)
            AstarPath.active.data.RemoveGraph(gg);
    }

    internal void SetGridSize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    internal int GetGridSize()
    {
        return Mathf.Abs(Width + Height) / 2;
    }

    /// <summary>
    /// The exact frame when the user has clicked down on the mouse and begun their action.
    /// </summary>
    private void StartClick()
    {
        _savedMousePosition = Input.mousePosition;
        _creating = true;
        UpdatePoleLocations();

        Vector3 clickPointNoSnap = GetWorldPoint(false);

        Analytics.CustomEvent("StartClick", clickPointNoSnap);

        float distance = Mathf.Infinity;
        float currentDistance = Mathf.Infinity;

        switch (_selectedObject)
        {
            case Def.Object.None:
                break;
            case Def.Object.Prefabrication:
                Distance = 0;
                prefabStart = ToLh(GetWorldPoint(true));
                var objects = DialogPrefabrications.Instance.GenerateSelectedPrefab(prefabStart);
                _editingObject = new GameObject("Prefab");
                _editingObject.transform.position = prefabStart;
                foreach (var obj in objects)
                    obj.transform.SetParent(_editingObject.transform);
                break;
            #region FireSource
            case Def.Object.FireSource:
                Prefabs[(int)Prefab.SPole].transform.position = ToLh(clickPointNoSnap);
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.FireSource],
                    Prefabs[(int)Prefab.SPole].transform.position + Consts.AgentOffset * (Statics.LevelHeight / 2.5f),
                    Quaternion.identity,
                    CurrentLevelTransform());
                MeshRendererStartPole.material = MaterialsOpaque[(int)Def.Mat.ErrorRed];
                MeshRendererEndPole.material = MaterialsOpaque[(int)Def.Mat.ErrorRed];
                break;
            #endregion
            #region Wall
            case Def.Object.Wall:
                _editingInfo = WallInfo.CreateNew();
                _editingInfo.StartClick();
                break;
            #endregion
            #region Gate
            case Def.Object.Gate:
                _editingInfo = GateInfo.CreateNew();
                _editingInfo.StartClick();
                break;
            #endregion
            #region Counter
            case Def.Object.Counter:
                Prefabs[(int)Prefab.SPole].transform.position =
                    WallSnapEnabled ? ToLh(GetWorldPoint(true)) : ToLh(GetWorldPoint(true));
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.Gate],
                    Prefabs[(int)Prefab.SPole].transform.position + Consts.GateOffset * (Statics.LevelHeight / 2.5f),
                    Quaternion.identity,
                    CurrentLevelTransform());
                _editingInfo = _editingObject.GetComponent<GateInfo>();
                MeshRendererStartPole.material = MaterialsOpaque[(int)Def.Mat.Counter];
                MeshRendererEndPole.material = MaterialsOpaque[(int)Def.Mat.Counter];
                ((GateInfo)_editingInfo).IsCounter = true;
                break;
            #endregion
            #region Barricade
            case Def.Object.Barricade:
                Prefabs[(int)Prefab.SPole].transform.position = ToLh(GetWorldPoint(true)) - (lowBarricade ? new Vector3(0f, 0.3F * Statics.LevelHeight, 0f) : Vector3.zero);
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.Barrier],
                    Prefabs[(int)Prefab.SPole].transform.position,
                    Quaternion.identity,
                    CurrentLevelTransform());
                float height = Statics.LevelHeight - 0.01f;
                if (lowBarricade) height *= 0.4f;
                _editingObject.transform.localScale = new Vector3(
                    _editingObject.transform.localScale.x,
                    height,
                    _editingObject.transform.localScale.z);
                _editingInfo = _editingObject.GetComponent<WallInfo>();
                MeshRendererStartPole.material = MaterialsOpaque[(int)Def.Mat.Barricade];
                MeshRendererEndPole.material = MaterialsOpaque[(int)Def.Mat.Barricade];
                break;
            #endregion
            #region PoleObstacle
            case Def.Object.PoleObstacle:
                Prefabs[(int)Prefab.SPole].transform.position = ToLh(clickPointNoSnap);
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.Obstacle],
                    Prefabs[(int)Prefab.SPole].transform.position,
                    Quaternion.identity,
                    CurrentLevelTransform());
                MeshRendererStartPole.material = MaterialsOpaque[(int)Def.Mat.CircularObstacle];
                MeshRendererEndPole.material = MaterialsOpaque[(int)Def.Mat.CircularObstacle];
                break;
            #endregion
            #region AgentSquare/Agents
            case Def.Object.AgentSquare:
            case Def.Object.Agents:
                Prefabs[(int)Prefab.SPole].transform.position = ToLh(clickPointNoSnap);

                if (UIController.Instance.AgentGenerationType == Def.AgentPlacement.Rectangle)
                {
                    _editingObject = Instantiate(Prefabs[(int)Prefab.AgentSquare],
                        Prefabs[(int)Prefab.SPole].transform.position + Consts.AgentOffset * (Statics.LevelHeight / 2.5f),
                        Quaternion.identity,
                        CurrentLevelTransform());
                    _selectedObject = Def.Object.AgentSquare;
                }
                else
                {
                    _editingObject = Instantiate(Prefabs[(int)Prefab.Agent],
                        Prefabs[(int)Prefab.SPole].transform.position + Consts.AgentOffset * (Statics.LevelHeight / 2.5f),
                        Quaternion.identity,
                        CurrentLevelTransform());
                }
                MeshRendererStartPole.material = MaterialsOpaque[(int)Def.Mat.Agent];
                MeshRendererEndPole.material = MaterialsOpaque[(int)Def.Mat.Agent];
                break;
            #endregion
            #region AgentSquare2
            case Def.Object.AgentSquare2:
                Prefabs[(int)Prefab.SPole].transform.position = _editingObject.GetComponent<AgentDistInfo>().SquarePoint2;
                break;
            #endregion
            #region Train
            case Def.Object.Train:
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.Train],
                    ToLh(GetWorldPoint(true)) + trainOffset + new Vector3(0.5f, 0f, 0.5f),
                    Quaternion.identity,
                    CurrentLevelTransform());
                TrainInfo trainInfo = _editingObject.GetComponent<TrainInfo>();
                _editingInfo = trainInfo;
                var trainGen = trainInfo.trainGen;
                trainGen.BuildTrain();
                trainGen.HideItems();
                MovePolesBack();
                break;
            #endregion
            #region Tram
            case Def.Object.Tram:
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.Tram],
                    ToLh(GetWorldPoint(true)) + trainOffset,
                    Quaternion.identity,
                    CurrentLevelTransform());
                break;
            #endregion
            #region StairsHalfLanding
            case Def.Object.StairsHalfLanding:
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.StairsHalfLanding],
                    ToLh(GetWorldPoint(true)),
                    Quaternion.identity,
                    CurrentLevelTransform());

                StairInfo stairInfo = _editingObject.GetComponent<StairInfo>();
                stairInfo.stairType = Def.StairType.HalfLanding;

                // Also save the walls in a list
                for (int i = _editingObject.transform.childCount - 1; i >= 0; i--)
                {
                    Transform t = _editingObject.transform.GetChild(i);
                    switch (t.name.Split('_')[0])
                    {
                        case Str.Wall:
                            stairInfo.wallList.Add(t);

                            break;
                        case Str.Seperator:
                            stairInfo.seperatorList.Add(t);
                            break;
                    }
                }

                stairInfo.UpdateSize();
                break;
            #endregion
            #region StairsStraight
            case Def.Object.StairsStraight:
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.StairsStraight],
                    ToLh(GetWorldPoint(true)),
                    Quaternion.identity,
                    CurrentLevelTransform());

                StairInfo stairInfo2 = _editingObject.GetComponent<StairInfo>();
                stairInfo2.stairType = Def.StairType.Straight;
                stairInfo2.spanFloors = Consts.StairHeight;

                // Also save the walls in a list
                for (int i = _editingObject.transform.childCount - 1; i >= 0; i--)
                {
                    Transform t = _editingObject.transform.GetChild(i);
                    switch (t.name.Split('_')[0])
                    {
                        case Str.Wall:
                            stairInfo2.wallList.Add(t);

                            break;
                        case Str.Seperator:
                            stairInfo2.seperatorList.Add(t);
                            break;
                    }
                }

                stairInfo2.UpdateSize();
                break;
            #endregion
            #region AvoidCircle
            case Def.Object.AvoidCircle:
                Prefabs[(int)Prefab.SPole].transform.position = ToLh(clickPointNoSnap);
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.AvoidCircle],
                    Prefabs[(int)Prefab.SPole].transform.position + Consts.AgentOffset * (Statics.LevelHeight / 2.5f),
                    Quaternion.identity,
                    CurrentLevelTransform());
                MeshRendererStartPole.material = MaterialsOpaque[(int)Def.Mat.AvoidCircle];
                MeshRendererEndPole.material = MaterialsOpaque[(int)Def.Mat.AvoidCircle];
                break;
            #endregion
            #region AvoidSquare
            case Def.Object.AvoidSquare:
                Prefabs[(int)Prefab.SPole].transform.position = ToLh(GetWorldPointHalfSnap());
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.AvoidSquare],
                    Prefabs[(int)Prefab.SPole].transform.position + Consts.AgentOffset * (Statics.LevelHeight / 2.5f),
                    Quaternion.Euler(-90f, 0, 0),
                    CurrentLevelTransform());
                MeshRendererStartPole.material = MaterialsOpaque[(int)Def.Mat.AvoidCircle];
                MeshRendererEndPole.material = MaterialsOpaque[(int)Def.Mat.AvoidCircle];
                break;
            #endregion
            #region StairObstruction
            case Def.Object.StairObstruction:

                distance = Mathf.Infinity;
                StairInfo[] stairs = FindObjectsOfType<StairInfo>();
                StairInfo threatStair = null;

                if (stairs.Length > 0)
                {
                    foreach (var stair in stairs)
                    {
                        if (stair.level != SelectedLevel)
                            continue;

                        currentDistance = Vector3.Distance(ToLh(stair.Centre()), clickPointNoSnap);

                        if (!(currentDistance < distance))
                            continue;

                        distance = currentDistance;
                        threatStair = stair;
                    }

                    Prefabs[(int)Prefab.SPole].transform.position = ToLh(threatStair.Centre());
                    PlaceDanger(clickPointNoSnap);
                    ThreatInfo ti3 = _editingObject.GetComponent<ThreatInfo>();
                    ti3.Copy(UIController.Instance.GetThreatDetails());
                    ti3.ThreatType = Def.ThreatType.StairObstruction;
                    ti3.LevelId = SelectedLevel;
                    ti3.ThreatId = ObjectInfo.Instance.ArtifactId++;
                    ti3.ElementId = threatStair.StairId;
                    ti3.X = _editingObject.transform.position.x;
                    ti3.Y = _editingObject.transform.position.z;
                }
                break;
            #endregion
            #region GateObstruction
            case Def.Object.GateObstruction:

                distance = Mathf.Infinity;
                GateInfo[] gates = FindObjectsOfType<GateInfo>();
                GateInfo threatGate = null;

                if (gates.Length > 0)
                {
                    foreach (var gate in gates)
                    {
                        if (gate.LevelId != SelectedLevel)
                            continue;

                        currentDistance = Vector3.Distance(ToLh(gate.transform.position), clickPointNoSnap);

                        if (!(currentDistance < distance))
                            continue;

                        distance = currentDistance;
                        threatGate = gate;
                    }

                    Prefabs[(int)Prefab.SPole].transform.position = ToLh(threatGate.transform.position);
                    PlaceDanger(clickPointNoSnap);
                    ThreatInfo ti2 = _editingObject.GetComponent<ThreatInfo>();
                    ti2.Copy(UIController.Instance.GetThreatDetails());
                    ti2.ThreatType = Def.ThreatType.GateObstruction;
                    ti2.LevelId = SelectedLevel;
                    ti2.ThreatId = ObjectInfo.Instance.ArtifactId++;
                    ti2.ElementId = threatGate.Id;
                    ti2.X = _editingObject.transform.position.x;
                    ti2.Y = _editingObject.transform.position.z;
                }
                break;
            #endregion
            #region DangerInRoom
            case Def.Object.DangerInRoom:
                Prefabs[(int)Prefab.SPole].transform.position = ToLh(clickPointNoSnap);
                PlaceDanger(clickPointNoSnap);
                ThreatInfo ti = _editingObject.GetComponent<ThreatInfo>();
                ti.Copy(UIController.Instance.GetThreatDetails());
                ti.ThreatType = Def.ThreatType.DangerInRoom;
                ti.LevelId = SelectedLevel;
                ti.ThreatId = ObjectInfo.Instance.ArtifactId++;
                ti.X = _editingObject.transform.position.x;
                ti.Y = _editingObject.transform.position.z;
                break;
            #endregion
            #region TicketGate
            case Def.Object.TicketGate:
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.TicketGate],
                    ToLh(GetWorldPoint(true)) + new Vector3(0.5f, 0f, 0.5f),
                    Quaternion.identity,
                    CurrentLevelTransform());
                _editingInfo = _editingObject.GetComponent<TicketGateInfo>();
                _editingObject.GetComponent<TicketGateInfo>().UpdateSize(Consts.TicketGateWidth);
                break;
            #endregion
            default:
                Debug.LogError("I don't know what object " + _selectedObject + " is.");
                break;
        }
    }

    /// <summary>
    /// The frames where the user has clicked and is still holding down the mouse button in the process of clicking.
    /// </summary>
    private void Adjust()
    {
        Vector3 endPolePos = Vector3.zero;
        Vector3 startPolePos = Prefabs[(int)Prefab.SPole].transform.position;

        switch (_selectedObject)
        {
            case Def.Object.None:
                break;
            case Def.Object.Prefabrication:
                var prefabEnd = ToLh(GetWorldPoint(true));
                if (Distance == 0)
                    Distance = Vector3.Distance(prefabStart, prefabEnd);
                _editingObject.transform.position = prefabEnd;
                if (Input.GetKeyDown(KeyCode.UpArrow))
                    _editingObject.transform.Rotate(Vector3.up, UpDownAngle);
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                    _editingObject.transform.Rotate(Vector3.up, -UpDownAngle);
                MovePolesBack();
                break;

            case Def.Object.Wall:
            case Def.Object.Gate:
            case Def.Object.Counter:
                _editingInfo.Adjust();
                break;

            #region Barricade
            case Def.Object.Barricade:
                endPolePos = (Input.GetKey(KeyCode.LeftShift) ? ToLh(ClosestPole(GetWorldPoint(true), true)) : ToLh(GetWorldPoint(true))) - (lowBarricade ? new Vector3(0f, 0.3f * Statics.LevelHeight, 0f) : Vector3.zero);
                Prefabs[(int)Prefab.EPole].transform.position = endPolePos;
                Prefabs[(int)Prefab.SPole].transform.LookAt(endPolePos);
                Prefabs[(int)Prefab.EPole].transform.LookAt(startPolePos);
                Distance = Vector3.Distance(startPolePos, endPolePos);
                _editingObject.transform.position = startPolePos + Distance / 2 * Prefabs[(int)Prefab.SPole].transform.forward;
                _editingObject.transform.rotation = Prefabs[(int)Prefab.SPole].transform.rotation;
                _editingObject.transform.localScale = new Vector3(_editingObject.transform.localScale.x, _editingObject.transform.localScale.y, Distance);
                WallInfo wallInfo = _editingObject.GetComponent<WallInfo>();
                wallInfo.IsLow = lowBarricade;
                wallInfo.IsBarricade = true;
                wallInfo.UpdateTextureScale(Distance);
                break;
            #endregion
            #region PoleObstacle/Agents/AvoidCircle/FireSource
            case Def.Object.PoleObstacle:
            case Def.Object.Agents:
            case Def.Object.AvoidCircle:
            case Def.Object.FireSource:
                Distance = Vector3.Distance(startPolePos, ToLh(GetWorldPoint(false)));

                if (Distance < Consts.MinCircularRadius)
                    Distance = Consts.MinCircularRadius;

                _editingObject.transform.localScale = new Vector3(
                    Distance * 2,
                    _editingObject.transform.localScale.y,
                    Distance * 2);
                break;
            #endregion
            #region AgentSquare
            case Def.Object.AgentSquare:
                endPolePos = ToLh(GetWorldPoint(false));
                Prefabs[(int)Prefab.EPole].transform.position = endPolePos;
                Prefabs[(int)Prefab.SPole].transform.LookAt(endPolePos);
                Prefabs[(int)Prefab.EPole].transform.LookAt(startPolePos);
                Distance = Vector3.Distance(startPolePos, endPolePos);
                _editingObject.transform.position = startPolePos + Distance / 2 * Prefabs[(int)Prefab.SPole].transform.forward + Consts.AgentOffset * (Statics.LevelHeight / 2.5f);
                _editingObject.transform.rotation = Prefabs[(int)Prefab.SPole].transform.rotation;
                _editingObject.transform.localScale = new Vector3(
                    _editingObject.transform.localScale.x,
                    _editingObject.transform.localScale.y,
                    Distance);
                break;
            #endregion
            #region AgentSquare2
            case Def.Object.AgentSquare2:
                endPolePos = ToLh(GetWorldPoint(false));
                Prefabs[(int)Prefab.EPole].transform.position = endPolePos;
                Prefabs[(int)Prefab.SPole].transform.LookAt(endPolePos);
                Prefabs[(int)Prefab.EPole].transform.LookAt(startPolePos);
                Distance = Vector3.Distance(startPolePos, endPolePos);
                _editingObject.GetComponent<AgentDistInfo>().UpdateSquare(endPolePos);
                break;
            #endregion
            #region AvoidSquare
            case Def.Object.AvoidSquare:
                endPolePos = ToLh(GetWorldPointHalfSnap());
                Prefabs[(int)Prefab.EPole].transform.position = endPolePos;
                Prefabs[(int)Prefab.SPole].transform.LookAt(endPolePos);
                Prefabs[(int)Prefab.EPole].transform.LookAt(startPolePos);
                Distance = Vector3.Distance(startPolePos, endPolePos);
                _editingObject.transform.position = startPolePos + Distance / 2 * Prefabs[(int)Prefab.SPole].transform.forward + Consts.AgentOffset * (Statics.LevelHeight / 2.5f);
                _editingObject.transform.localScale = new Vector3(
                    Math.Abs(startPolePos.x - endPolePos.x) / 2f, Math.Abs(startPolePos.z - endPolePos.z) / 2f,
                    _editingObject.transform.localScale.z);
                break;
            #endregion
            #region DangerInRoom/Obstruction
            case Def.Object.DangerInRoom:
            case Def.Object.GateObstruction:
            case Def.Object.StairObstruction:
                Distance = Vector3.Distance(startPolePos, ToLh(GetWorldPoint(false)));
                break;
            #endregion
            #region StairsHalfLanding/Straight
            case Def.Object.StairsHalfLanding:
            case Def.Object.StairsStraight:
                _editingObject.transform.position = ToLh(GetWorldPoint(true));
                if (Input.GetKeyDown(KeyCode.UpArrow))
                    _editingObject.transform.Rotate(Vector3.up, UpDownAngle);
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                    _editingObject.transform.Rotate(Vector3.up, -UpDownAngle);
                break;
            #endregion
            #region Train/Bench
            case Def.Object.Train:
                _editingObject.transform.position = ToLh(GetWorldPoint(true)) + _editingInfo.PositionOffset();
                if (Input.GetKeyDown(KeyCode.UpArrow))
                    _editingObject.transform.Rotate(Vector3.up, UpDownAngle);
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                    _editingObject.transform.Rotate(Vector3.up, -UpDownAngle);
                MovePolesBack();
                break;
            #endregion
            #region TicketGate
            case Def.Object.TicketGate:
                _editingObject.transform.position = ToLh(GetWorldPoint(true)) + new Vector3(0.5f, 0.05f, 0.5f);
                if (Input.GetKeyDown(KeyCode.UpArrow))
                    _editingObject.transform.Rotate(Vector3.up, UpDownAngle);
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                    _editingObject.transform.Rotate(Vector3.up, -UpDownAngle);

                if (_editingObject.transform.rotation.eulerAngles.y == 0f ||
                    _editingObject.transform.rotation.eulerAngles.y % 180f == 0)
                    _editingObject.transform.position -= new Vector3(0f, 0f, 0.5f);
                else
                    _editingObject.transform.position -= new Vector3(0.5f, 0f, 0f);
                break;
                #endregion
        }

        _creationInfoText.text = Distance >= 100f ? "Length " + Distance.ToString("000.00") : "Length " + Distance.ToString(Str.DecimalFormat);

        // If the creation is lined up with either axis (straight)
        if (Math.Abs(endPolePos.x - startPolePos.x) < 0.001f ||
            Math.Abs(endPolePos.z - startPolePos.z) < 0.001f)
        {
            _creationInfoText.color = Color.green;
        }
        else
        {
            _creationInfoText.color = Consts.textColor;
        }
    }

    /// <summary>
    /// The frame where the use has lifted the mouse button and finished clicking.
    /// </summary>
    private void EndClick()
    {
        _creating = false;
        _creationInfoText.color = Consts.textColor;

        // If the mouse position is the same as the start position, the user didn't move the mouse while clicking, 
        // thus he was trying to click on something not create something.
        if (Input.mousePosition == _savedMousePosition)
        {
            #region CLICK
            // User tried to click on something, lets find out what it is.
            // First close all taskbars:
            //UIController.Instance.toggleTaskBarLeft(UIController.Instance.anyActiveTaskBars());
            //_selectedObject = (int)Def.Object.None;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray).OrderBy(h => h.distance).ToArray();

            if (hits.Length > 0)
            {
                // User single clicked on something. Process click.
                ProcessHitObject(hits, 0);
            }
            else
            {
                Debug.LogError("Could not hit ray?");
                EndClickClick();
            }
            #endregion
        }
        else
            EndClickDragged();
    }

    private void EndClickClick()
    {
        MovePolesBack();
        // If ther user was creating something, delete it
        if (_editingObject != null)
            Destroy(_editingObject);
    }

    private void EndClickDragged()
    {
        // User was dragging his click, probably creating something.
        switch (_selectedObject)
        {
            #region Prefab
            case Def.Object.Prefabrication:
                if (_editingObject != null)
                {
                    for (int i = _editingObject.transform.childCount - 1; i >= 0; i--)
                    {
                        Transform child = _editingObject.transform.GetChild(i);

                        var wallInfo = child.GetComponent<WallInfo>();
                        var gateInfo = child.GetComponent<GateInfo>();

                        if (wallInfo != null)
                        {
                            wallInfo.UpdateInfo();
                            foreach (Transform otherChild in CurrentLevelTransform())
                                if (otherChild.name.Split('_')[0] == Str.Wall)
                                    if (ObjectInfo.AreTheSame(otherChild.GetComponent<WallInfo>().Get, wallInfo.Get))
                                        NotifyAndDestroy(otherChild.gameObject);
                        }
                        else if (gateInfo != null)
                        {
                            gateInfo.UpdateInfo();
                            foreach (Transform otherChild in CurrentLevelTransform())
                                if (otherChild.name.Split('_')[0] == Str.Gate)
                                    if (ObjectInfo.AreTheSame(otherChild.GetComponent<GateInfo>().Get, gateInfo.Get))
                                        NotifyAndDestroy(gateInfo.gameObject); // Keep existing gates where possible (eg stair gates)
                        }

                        child.SetParent(CurrentLevelTransform());
                    }
                    Destroy(_editingObject);
                }
                break;
            #endregion
            case Def.Object.Gate:
            case Def.Object.Counter:
            case Def.Object.Wall:
            case Def.Object.Barricade:
                _editingInfo.EndClick();
                _editingObject = _editingInfo.gameObject;
                break;
            #region Agents
            case Def.Object.Agents:
                _editingObject.GetComponent<AgentDistInfo>().ApplyAgentDist(UIController.Instance.GetAgentDetails());
                break;
            #endregion
            #region AgentSquare
            case Def.Object.AgentSquare:
                _editingObject.GetComponent<AgentDistInfo>().ApplySquarePartOne(Prefabs[(int)Prefab.SPole].transform.position, Prefabs[(int)Prefab.EPole].transform.position);
                _selectedObject = Def.Object.AgentSquare2;
                break;
            #endregion
            #region AgentSquare2
            case Def.Object.AgentSquare2:
                _editingObject.GetComponent<AgentDistInfo>().ApplyAgentDist(UIController.Instance.GetAgentDetails());
                _selectedObject = Def.Object.Agents;
                break;
            #endregion
            #region AvoidCircle
            case Def.Object.AvoidCircle:
                DangerArea avoidanceCircle = _editingObject.GetComponent<DangerArea>();
                avoidanceCircle.WeightModifier = Consts.DangerWeightModifier;
                avoidanceCircle.dangerArea = Def.DangerArea.Circle;
                break;
            #endregion
            #region AvoidSquare
            case Def.Object.AvoidSquare:
                DangerArea avoidanceSquare = _editingObject.GetComponent<DangerArea>();
                avoidanceSquare.WeightModifier = Consts.DangerWeightModifier;
                avoidanceSquare.dangerArea = Def.DangerArea.Square;
                avoidanceSquare.ApplyPoles(Prefabs[(int)Prefab.SPole].transform.position, Prefabs[(int)Prefab.EPole].transform.position);
                break;
            #endregion
            #region Train/ExitSign/Bench/Ticket
            case Def.Object.Train:
            case Def.Object.TicketGate:
                Distance = 1;
                _editingInfo.EndClick();
                break;
            #endregion
            #region StairsHalfLanding/Straight
            case Def.Object.StairsHalfLanding:
            case Def.Object.StairsStraight:

                //So we don't delete the object later.
                Distance = 1;

                int currentLevel = SelectedLevel;

                int count = 0;
                while (NumLevels <= SelectedLevel + Consts.StairHeight)
                {
                    count++; if (count > Consts.StairHeight) break;
                    UIController.Instance.LevelAddLevel();
                    UIController.Instance.LevelSetLevel(currentLevel);
                }

                StairInfo si = _editingObject.transform.GetComponent<StairInfo>();

                // Initialise both gates
                si.upperGate.GetComponent<GateInfo>().UpdateInfo();
                si.lowerGate.GetComponent<GateInfo>().UpdateInfo();

                // Override existing gates if placed ontop of any
                foreach (var gateInfo in FindObjectsOfType<GateInfo>())
                {
                    if (gateInfo.Id == si.upperGate.GetComponent<GateInfo>().Id ||
                        gateInfo.Id == si.lowerGate.GetComponent<GateInfo>().Id)
                        continue;

                    if (gateInfo.transform.position.Equals(si.upperGate.position))
                    {
                        Debug.Log("Linking existing gate " + gateInfo.Id);
                        Destroy(si.upperGate.gameObject);
                        si.upperGate = gateInfo.transform;
                        continue;
                    }

                    if (gateInfo.transform.position.Equals(si.lowerGate.position))
                    {
                        Debug.Log("Linking existing gate " + gateInfo.Id);
                        Destroy(si.lowerGate.gameObject);
                        si.lowerGate = gateInfo.transform;
                        continue;
                    }
                }

                // Attach the stair gates to the right levels.
                si.upperGate.SetParent(_levels[SelectedLevel + Consts.StairHeight].transform);
                si.lowerGate.SetParent(CurrentLevelTransform());

                // Also attach walls to the right levels.
                foreach (var wall in si.wallList.ToArray())
                {
                    if (WallIsInUpperStairGate(wall))
                    {
                        si.wallList.Remove(wall);
                        Destroy(wall.gameObject);
                    }
                    else
                    {
                        wall.SetParent(CurrentLevelTransform());
                        wall.GetComponent<WallInfo>().UpdateInfo();
                    }
                }

                si.UpdateBoundaries(SelectedLevel, (int)_editingObject.transform.localEulerAngles.y);
                si.StairId = ObjectInfo.Instance.ArtifactId++;
                break;
                #endregion
        }

        _createdObjects.Push(_editingObject);

        if (_selectedObject != Def.Object.AgentSquare2)
            _editingObject = null;

        MovePolesBack();

        if (_createdObjects.Count > 0)
            FillFloors();

        if (!continuePlatformQueue && Distance == 0)
            Undo();
        else
            _creationInfoText.text = _selectedObject + " created of length " + Distance;

    }

    private void NotifyAndDestroy(GameObject gameObject)
    {
        Destroy(gameObject);
        if (!notificationUnshown)
        {
            UIController.Instance.ShowGeneralDialog("If you have overlapped walls/gates, please ensure they are the same.", "Please note");
            notificationUnshown = true;
        }
    }

    /// <summary>
    /// Main processing distributor for clicking on an object. 
    /// </summary>
    private void ProcessHitObject(RaycastHit[] hits, int index)
    {
        if (index >= hits.Length)
        {
            EndClickDragged();
            return;
        }

        string message = "";
        string[] splitName = hits[index].transform.name.Split('_');
        GameObject hitGameObject = hits[index].transform.gameObject;
        _creationInfoText.text = "Select: " + hits[index].transform.name;

        if (_editingObject != null && hitGameObject.Equals(_editingObject)
            || _editingInfo != null && hitGameObject.Equals(_editingInfo.gameObject))
        {
            ProcessHitObject(hits, index + 1);
            Debug.Log("Skipping new wall");
            return;
        }

        switch (splitName[0])
        {
            case "PlatformQueue":
                hitGameObject.GetComponent<PlatformQueueInfo>().Select();
                break;
            case "EscalatorSimple":
                StairInfo esi = hitGameObject.GetComponentInParent<StairInfo>();
                UIController.Instance.DialogWithFieldsOpen(esi, esi.gameObject);
                break;
            case Str.Agent:
                hitGameObject.GetComponent<IndividualAgent>().Select();
                break;
            case Str.Barricade:
            case Str.Wall:
                hitGameObject.GetComponent<WallInfo>().Select();
                break;
            case Str.Gate:
                hitGameObject.GetComponent<GateInfo>().Select();
                break;

            case Str.CircularObstacle:
                CircularObstacle c = ObjectInfo.Instance.ObjectToPoleObstacle(hitGameObject);
                message = Str.Vertex1String + "(" +
                          c.XPosition.ToString(Str.DecimalFormat) + "," +
                          c.YPosition.ToString(Str.DecimalFormat) + ")" + Environment.NewLine;
                message += Str.RadiusString + c.Radius.ToString(Str.DecimalFormat);
                UIController.Instance.DialogPrefabs(message, "Selected Obstacle", hitGameObject);
                break;

            case Str.FireSource:
                UIController.Instance.DialogWithFieldsOpen(hitGameObject.GetComponent<FireSource>(), hitGameObject);
                break;

            case Str.WaitPoint:
                UIController.Instance.DialogWithFieldsOpen(hitGameObject.GetComponent<WaitPointInfo>(), hitGameObject);
                break;

            case Str.Agents:
                UIController.Instance.DialogWithFieldsOpen(hitGameObject.GetComponent<AgentDistInfo>(), hitGameObject);
                break;

            case Str.RoomFloor:
                HitRoomFloor(hits[index], splitName);
                break;

            case Str.Danger:
                UIController.Instance.DialogWithFieldsOpen(hitGameObject.GetComponent<ThreatInfo>(), hitGameObject);
                break;

            case Str.Danger1:
                UIController.Instance.DialogWithFieldsOpen(hitGameObject.transform.parent.gameObject.GetComponent<ThreatInfo>(), hitGameObject.transform.parent.gameObject);
                break;

            case Str.Stairs1:
            case Str.Stairs2:
            case Str.Landing:
                UIController.Instance.DialogWithFieldsOpen(hitGameObject.transform.parent.gameObject.GetComponent<StairInfo>(), hitGameObject.transform.parent.gameObject);
                break;

            case Str.AvoidCircle:
                CircularObstacle c2 = ObjectInfo.Instance.ObjectToPoleObstacle(hitGameObject);
                message = Str.Vertex1String + "(" +
                          c2.XPosition.ToString(Str.DecimalFormat) + "," +
                          c2.YPosition.ToString(Str.DecimalFormat) + ")" + Environment.NewLine;
                message += Str.RadiusString + c2.Radius.ToString(Str.DecimalFormat);
                message += Environment.NewLine + "Weight: " + c2.Weight;
                UIController.Instance.DialogPrefabs(message, "Selected Avoidance Circle", hitGameObject);
                break;

            case Str.AvoidSquare:
                DangerArea da = hitGameObject.GetComponent<DangerArea>();
                message = "Pos: " + hitGameObject.transform.position;
                message += Environment.NewLine + "Weight: " + da.WeightModifier;
                UIController.Instance.DialogPrefabs(message, "Selected Avoidance Square", hitGameObject);
                break;

            case Str.Train:
            case Str.Tram:
                UIController.Instance.DialogWithFieldsOpen(hitGameObject.GetComponent<TrainInfo>(), hitGameObject);
                break;

            case Str.TicketGate1:
            case Str.TicketGate2:
                hitGameObject = hitGameObject.transform.parent.gameObject;
                UIController.Instance.DialogWithFieldsOpen(hitGameObject.GetComponent<TicketGateInfo>(), hitGameObject);
                break;

            case Str.Level:
                ProcessHitObject(hits, index + 1);
                return;

            default:
                Debug.Log("Sorry I don't even know what it is you clicked on...");
                break;
        }

        EndClickClick();
    }

    private string HitRoomFloor(RaycastHit hit, string[] splitName)
    {
        float nearestNodeDist = float.MaxValue;
        GraphNode nNode = null;

        foreach (NavGraph navGraph in AstarPath.active.data.graphs)
        {
            GridGraph gg = (GridGraph)navGraph;
            var nearestNode = gg.GetNearest(hit.point);
            if (nearestNode.node != null)
            {
                var nodePos = (Vector3)nearestNode.node.position;
                var distance = Vector3.Distance(nodePos, hit.point);
                if (distance < nearestNodeDist)
                {
                    nNode = nearestNode.node;
                    nearestNodeDist = distance;
                }
            }
        }

        string message = Str.AreaString + GetRoomArea(int.Parse(splitName[1])) + Str.SqrMeter + Environment.NewLine;

        if (nNode != null)
        {
            message += "Grid Node Details: " + Environment.NewLine;
            message += "Position: " + (Vector3)nNode.position + Environment.NewLine;
            message += "Penalty: " + nNode.Penalty + ", AreaID: " + nNode.Area + Environment.NewLine;
            UIController.Instance.DialogPrefabs(message, "Selected Room", hit.transform.gameObject);
        }

        return message;
    }

    public Transform CurrentLevelTransform()
    {
        return _levels[SelectedLevel].transform;
    }

    internal float GetRoomArea(int roomId)
    {
        if (!_roomAreas.ContainsKey(SelectedLevel)) return float.MaxValue / 10;
        if (_roomAreas[SelectedLevel].ContainsKey(roomId))
            return Mathf.Abs(_roomAreas[SelectedLevel][roomId]);
        return float.MaxValue / 10;
    }

    private void PlaceDanger(Vector3 worldPointNoSnap)
    {
        _editingObject = Instantiate(
            Prefabs[(int)Prefab.Danger],
            Prefabs[(int)Prefab.SPole].transform.position,
            Quaternion.identity,
            CurrentLevelTransform());
        MeshRendererStartPole.material = MaterialsOpaque[(int)Def.Mat.Danger];
        MeshRendererEndPole.material = MaterialsOpaque[(int)Def.Mat.Danger];
    }

    internal List<int> GetDestinationGates()
    {
        List<int> allDGates = new List<int>();

        if (DestinationGates != null)
        {
            foreach (var item in DestinationGates)
            {
                foreach (var gate in item)
                {
                    allDGates.Add(gate);
                }
            }
        }

        return allDGates;
    }

    internal List<int> GetIntermediateGates()
    {
        List<int> intermediateGates = new List<int>();

        if (IntermediateGates != null)
        {
            foreach (var item in IntermediateGates)
            {
                foreach (var gate in item)
                {
                    intermediateGates.Add(gate);
                }
            }
        }

        return intermediateGates;
    }

    internal void DestroyGateShares()
    {
        foreach (var gateShare in gateShareInfos)
            Destroy(gateShare);
        gateShareInfos.Clear();
    }

    internal void AddGateShare(GateInfo gateInfo, int gateUsage)
    {
        if (gateInfo != null)
            gateInfo.ShowGateSharesCount(gateUsage);
    }

    internal void ViewGateIDs()
    {
        foreach (var gi in ObjectInfo.Instance.AllGates())
            gi.ViewGateID(true);
    }

    internal void UnViewGateIDs()
    {
        foreach (var gi in ObjectInfo.Instance.AllGates())
            gi.ViewGateID(false);
    }

    internal void UpdateCameraLocation(Vector3 cameraPos, Vector3 cameraRot)
    {
        _cameraPos = cameraPos;
        _cameraRot = cameraRot;
    }

    /// <summary>
    /// Duplicates the top level by adding a level and copying the level ontop.
    /// </summary>
    internal void DuplicateLevel()
    {
        UIController.Instance.LevelAddLevel();

        foreach (Transform t in _levels[SelectedLevel - 1].transform)
            Instantiate(t.gameObject, t.position + new Vector3(0f, Statics.LevelHeight, 0f), t.localRotation, CurrentLevelTransform());

        foreach (Transform t in _levels[_levels.Count - 1].transform)
        {
            var oInfo = t.GetComponent<BaseObject>();
            if (oInfo != null)
                oInfo.UpdateInfo();
        }

        // Change to the top level
        UIController.Instance.LevelSetLevel(SelectedLevel);
    }

    /// <summary>
    /// Checks the provided wall as to whether there are any stairs 
    /// which have gates in the place of the wall.
    /// </summary>
    /// <param name="t">Transform component of the wall to check.</param>
    /// <returns></returns>
    private static bool WallIsInUpperStairGate(Transform t)
    {
        Vector3 wallPos = t.transform.position + Vector3.up;

        return FindObjectsOfType<StairInfo>().Any(si => wallPos.Equals(si.upperGate.transform.position));
    }

    /// <summary>
    /// Moves editing poles back when they are not in use.
    /// </summary>
    internal void MovePolesBack()
    {
        Prefabs[(int)Prefab.SPole].transform.position = new Vector3(0, -1000, 0);
        Prefabs[(int)Prefab.EPole].transform.position = new Vector3(0, -1000, 0);
    }

    /// <summary>
    /// Finds all poles in the environment and records their locations so that 
    /// the user when editing can snap to the wall endpoints easily.
    /// </summary>
    private void UpdatePoleLocations()
    {
        _snappingPolesWalls[SelectedLevel].Clear();
        _snappingPolesBarricades[SelectedLevel].Clear();

        if (_createdObjects.Count <= 0) return;

        foreach (GameObject g in GameObject.FindGameObjectsWithTag(Str.PolesTagString))
            if (g.transform.IsChildOf(CurrentLevelTransform()))
                _snappingPolesWalls[SelectedLevel].Add(g.transform.position);
        foreach (GameObject g in GameObject.FindGameObjectsWithTag(Str.PolesBarricadeTagString))
            if (g.transform.IsChildOf(CurrentLevelTransform()))
                _snappingPolesBarricades[SelectedLevel].Add(g.transform.position);
    }

    internal void ResetNow()
    {
        _reset = false;
        _editingObject = null;
        MovePolesBack();
        UIController.Instance.LevelSetLevel(0);
        FillFloors();
        SetAllColliders(true);
        RescanGraphs();
    }

    internal void ResetNextFrame()
    {
        _reset = true;
    }

    public void RescanGraphs()
    {
        StartCoroutine("ScanGraphs");
    }

    private IEnumerator ScanGraphs()
    {
        foreach (Progress progress in AstarPath.active.ScanAsync())
        {
            string message = "Scanning Environment: " + (progress.progress * 100f).ToString("00") + "%";
            if (progress.progress >= 0.95f)
            {
                // Async scanning of graphs is complete.
                message = "Scanning Environment: Done!";
                UIController.Instance.LevelSetLevel(0);
            }
            SimulationController.Instance.SetSimText(message);
            UIController.Instance.SetConsoleText(progress.description);
            yield return null;
        }
    }

    /// <summary>
    /// Converts a position to a specified level id.
    /// </summary>
    /// <param name="input">Initial position.</param>
    /// <param name="level">Level ID.</param>
    /// <returns>Position on the level.</returns>
    public Vector3 ToLh(Vector3 input, int level = -1)
    {
        return level == Consts.MinusOne ?
            new Vector3(input.x, Statics.FloorHeight + Statics.LevelHeight / 2 + Statics.LevelHeight * SelectedLevel, input.z) :
            new Vector3(input.x, Statics.FloorHeight + Statics.LevelHeight / 2 + Statics.LevelHeight * level, input.z);
    }

    /// <summary>
    /// Snaps a position to the grid (int).
    /// </summary>
    /// <param name="originalPosition">Position to snap.</param>
    /// <returns>Snapped position.</returns>
    private Vector3 GridSnap(Vector3 originalPosition)
    {
        if (_smallGridSnap)
            return new Vector3(
                Mathf.RoundToInt(originalPosition.x / 0.01f) * 0.01f,
                originalPosition.y,
                Mathf.RoundToInt(originalPosition.z / 0.01f) * 0.01f);

        return new Vector3(
            Mathf.RoundToInt(originalPosition.x),
            originalPosition.y,
            Mathf.RoundToInt(originalPosition.z));
    }

    public Vector3 GetWorldPoint(bool gridSnapEnabled)
    {
        Vector3 point = gridSnapEnabled ? GridSnap(GetRayCastHit().point) : GetRayCastHit().point;
        if (point.x > Width)
            point = new Vector3(Width, point.y, point.z);
        if (point.z > Height)
            point = new Vector3(point.x, point.y, Height);
        if (point.x < 0)
            point = new Vector3(0, point.y, point.z);
        if (point.z < 0)
            point = new Vector3(point.x, point.y, 0);
        return point;
    }

    private Vector3 GetWorldPointHalfSnap()
    {
        Vector3 point = GetRayCastHit().point;

        return new Vector3(
            Mathf.RoundToInt(point.x * 2) / 2f,
            point.y,
            Mathf.RoundToInt(point.z * 2) / 2f);
    }

    private static RaycastHit GetRayCastHit()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        int layerMask = 1 << 8;
        layerMask = ~layerMask;

        return Physics.Raycast(ray, out hit, float.PositiveInfinity, layerMask) ? hit : hit;
    }

    /// <summary>
    /// Finds a pole (if any) that the user's wall can snap to. Very important as vertex ends of walls need to be connected.
    /// </summary>
    public Vector3 ClosestPole(Vector3 worldPoint, bool inclBarricades = false)
    {
        Vector3 closest = Vector3.zero;
        float distance = Mathf.Infinity;
        float currentDistance;

        if (inclBarricades && _snappingPolesWalls[SelectedLevel].Count <= 0 && _snappingPolesBarricades[SelectedLevel].Count <= 0)
            return worldPoint;

        if (!inclBarricades && _snappingPolesWalls[SelectedLevel].Count <= 0)
            return worldPoint;

        foreach (Vector3 p in _snappingPolesWalls[SelectedLevel])
        {
            currentDistance = Vector3.Distance(worldPoint, p);
            if (!(currentDistance < distance)) continue;
            distance = currentDistance;
            closest = p;
        }
        if (inclBarricades)
        {
            foreach (Vector3 p in _snappingPolesBarricades[SelectedLevel])
            {
                currentDistance = Vector3.Distance(worldPoint, p);
                if (!(currentDistance < distance)) continue;
                distance = currentDistance;
                closest = p;
            }
        }
        return new Vector3(closest.x, _creationHeight, closest.z);
    }

    internal void FillFloors()
    {
        fillFloors = 0;

        FileOperations op = FileOperations.Instance;
        Model model = op.ObjectsToModel();

        if (!model.validatedSuccessfully)
            return;

        ClearPolygonRoofs();

        currentVIDs = RoomGeneration.SetupVertexIDs(model);
        currentRoomRegions = RoomGeneration.GenerateRooms(model, currentVIDs);

        DestinationGates = Utils.DestinationGates(model);
        IntermediateGates = Utils.IntermediateGates(model);

        if (DestinationGates != null && DestinationGates.Count > 0)
        {
            // Remove stairgates from destination gates.
            foreach (StairInfo stairInfo in FindObjectsOfType<StairInfo>())
            {
                var lowerGateID = stairInfo.lowerGate.GetComponent<GateInfo>().Id;
                var upperGateID = stairInfo.upperGate.GetComponent<GateInfo>().Id;

                foreach (var level in DestinationGates)
                {
                    if (level.Contains(lowerGateID))
                        level.Remove(lowerGateID);
                    if (level.Contains(upperGateID))
                        level.Remove(upperGateID);
                }
            }
        }

        var gates = GetDestinationGates();

        foreach (GateInfo gateInfo in FindObjectsOfType<GateInfo>())
            gateInfo.IsDestination = gates.Contains(gateInfo.Id);


        // For all rooms on each floor.
        for (int levelId = 0; levelId < currentRoomRegions.Count; levelId++)
            MakeFloorMeshForLevel(currentVIDs, levelId, currentRoomRegions[levelId]);

        foreach (var wpi in FindObjectsOfType<WaitPointInfo>())
            wpi.TryGetPolyH();

        ChangeSelectedLevel(SelectedLevel);

        foreach (var trainInfo in FindObjectsOfType<TrainInfo>())
            trainInfo.HideFloor();
    }

    private void MakeFloorMeshForLevel(Dictionary<int, Vertex> vertexIds, int levelId, List<int[]> roomRegions)
    {
        if (_roomAreas.ContainsKey(levelId))
            _roomAreas[levelId].Clear();
        else
            _roomAreas.Add(levelId, new Dictionary<int, float>());


        for (int roomNum = 0; roomNum < roomRegions.Count; roomNum++)
        {
            int[] room = roomRegions[roomNum];
            //Create Mesh
            Mesh mesh = new Mesh { name = "FloorMesh" };
            List<Vector3> v3 = new List<Vector3>();
            List<Vector2> v2 = new List<Vector2>();
            List<Vertex> vertexList = new List<Vertex>();

            foreach (int i in room)
                vertexList.Add(vertexIds[i]);

            foreach (Vertex v in vertexList)
            {
                v3.Add(ToFh(new Vector3(v.X, 0f, v.Y), levelId));
                v2.Add(new Vector2(v.X, v.Y));
            }

            mesh.vertices = v3.ToArray();
            mesh.uv = v2.ToArray();
            mesh.triangles = Triangulator.Triangulate(v2.ToArray());
            mesh.RecalculateNormals();
            DoubleNormals(ref mesh);

            _roomAreas[levelId].Add(roomNum, Triangulator.Area());

            GameObject roomFloor = new GameObject("RoomFloor_" + roomNum);
            MeshFilter meshFilter = (MeshFilter)roomFloor.AddComponent(typeof(MeshFilter));
            meshFilter.mesh = mesh;

            MeshRenderer renderer1 = roomFloor.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
            if (renderer1 != null) renderer1.material = MaterialsOpaque[(int)Def.Mat.Floor];
            roomFloor.isStatic = true;
            roomFloor.AddComponent<MeshCollider>();
            roomFloor.layer = 9;
            roomFloor.transform.SetParent(_levels[levelId].transform);
            _roomFloors.Add(roomFloor);
        }
    }

    internal PolygonHelper GetRoomPoly(float x, float y, int levelId = -1)
    {
        if (levelId < 0)
            levelId = SelectedLevel;

        PolygonHelper roomPoly = new PolygonHelper();

        if (currentRoomRegions == null || currentVIDs == null)
            return roomPoly;

        foreach (var room in currentRoomRegions[levelId])
        {
            PointF[] points = new PointF[room.Length];

            for (int k = 0; k < room.Length; k++)
            {
                float px = currentVIDs[room[k]].X;
                float py = currentVIDs[room[k]].Y;
                points[k] = new PointF(px, py);
            }

            roomPoly = new PolygonHelper(points);

            if (roomPoly.PointInPolygon(x, y))
                return roomPoly;
        }

        return roomPoly;
    }

    /// <summary>
    /// Destroys all the room floors generated.
    /// </summary>
    private void ClearPolygonRoofs()
    {
        foreach (GameObject g in _roomFloors)
            Destroy(g);

        _roomFloors.Clear();
    }

    internal void ChangeSelectedObject(int objectId)
    {
        if (!_creating)
        {
            _selectedObject = (Def.Object)objectId;

            if (_selectedObject != Def.Object.None && displayAllLevels)
                DisplayAllLevels();

            foreach (var txt in SelectedObjectTexts)
                txt.text = ((Def.Object)objectId).ToString();
        }
    }

    internal void ChangeSelectedLevel(int level)
    {
        if (level == 0 && NumLevels < 1)
        {
            UIController.Instance.LevelAddLevel();
            Debug.Log("Adding level 0 (bug).");
        }

        if (!_creating && level < NumLevels)
        {
            RemovePriorHighlighting();
            SelectedLevel = level;

            if (NetworkServer.Instance != null && NetworkServer.Instance.isConnected)
                NetworkServer.Instance.SendDisplayUpdate(displayAllLevels, UIController.Instance.GridDisplayEnabled, SelectedLevel);

            if (SelectedLevelText == null)
            {
                SelectedLevelText = GameObject.Find("SLevel").GetComponent<Text>();
                Debug.Log("LevelText was not initialized, initializing...");
            }
            SelectedLevelText.text = level.ToString();

            for (int l = 0; l < NumLevels; l++)
            {
                Transform levelTransform = CreationParent.GetChild(l);

                if (l == SelectedLevel)
                {
                    //Make the selected level opaque.
                    SetTransparencyOfLevel(false, levelTransform);
                    SetColliderOfLevel(true, levelTransform);

                    if (l < SimulationController.Instance.HeatmapObjects.Count)
                        SimulationController.Instance.HeatmapObjects[l].GetComponent<MeshRenderer>().material.color = Color.white;
                }
                else
                {
                    //Make the unselected levels transparent.
                    SetTransparencyOfLevel(true, levelTransform);
                    SetColliderOfLevel(false, levelTransform);

                    if (l < SimulationController.Instance.HeatmapObjects.Count)
                        SimulationController.Instance.HeatmapObjects[l].GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f, 0f);
                }
            }
            foreach (GridGraph gg in AstarPath.active.data.GetUpdateableGraphs())
            {
                float graphHeight = Statics.FloorHeight + Statics.FloorOffsetGrid + Statics.LevelHeight * level;
                gg.drawGizmos = Mathf.Abs(gg.center.y - graphHeight) < 0.01f;
            }
            //SimulationController.Instance.UpdateLineColors();
        }
        else
        {
            Debug.Log("There is no level " + level + " to change to: " + _creating + " " + NumLevels);
        }
    }

    internal void SetAllColliders(bool v)
    {
        for (int l = 0; l < NumLevels; l++)
        {
            if (CreationParent.childCount > 0)
            {
                Transform levelTransform = CreationParent.GetChild(l);
                SetColliderOfLevel(v, levelTransform);
            }
        }
    }

    /// <summary>
    /// Changes the state of all the colliders in the specified level.
    /// </summary>
    /// <param name="collide">Whether the level should collide or not.</param>
    /// <param name="levelTransform">The level to modify.</param>
    private void SetColliderOfLevel(bool collide, Component levelTransform)
    {
        if (displayAllLevels)
            collide = true;

        foreach (Collider c in levelTransform.GetComponentsInChildren<Collider>())
            c.enabled = collide;
    }

    private void SetTransparencyOfLevel(bool transparent, Transform level)
    {
        if (displayAllLevels)
            transparent = false;

        float lineWidth = transparent ? Consts.LineWidthThin : Consts.LineWidthThick;
        level.GetComponent<BoxCollider>().enabled = !transparent;
        level.GetComponent<GridDisplay>().ChangeLineWidth(lineWidth);
        level.GetComponentInChildren<LineRenderer>().material = !transparent ? MaterialsOpaque[(int)Def.Mat.Line] : MaterialsTransparent[(int)Def.Mat.Line];

        for (int child = 0; child < level.childCount; child++)
        {
            Transform obj = level.GetChild(child);

            string[] splitName = obj.name.Split('_');

            switch (splitName[0])
            {
                case Str.Seperator:
                case Str.Wall:
                    ChangeMaterial(obj.GetComponent<WallInfo>().IsTransparent ? true : transparent, obj, (int)Def.Mat.Wall);
                    obj.GetComponent<WallInfo>().UpdateTextureScale();
                    break;
                case Str.Gate:
                    GateInfo gateInfo = obj.GetComponent<GateInfo>();
                    ChangeMaterial(gateInfo.IsTransparent ? true : transparent, obj, gateInfo.GetMaterialId());
                    break;
                case Str.CircularObstacle:
                    ChangeMaterial(transparent, obj, (int)Def.Mat.CircularObstacle);
                    break;
                case Str.Agents:
                    ChangeMaterial(transparent, obj, (int)Def.Mat.Agent);
                    break;
                case Str.Barricade:
                    ChangeMaterial(obj.GetComponent<WallInfo>().IsTransparent ? true : transparent, obj, (int)Def.Mat.Barricade);
                    obj.GetComponent<WallInfo>().UpdateTextureScale();
                    break;
                case Str.Danger:
                    ChangeMaterial(transparent, obj, (int)Def.Mat.Danger);
                    break;
                case Str.Floor:
                    ChangeMaterial(transparent, obj, (int)Def.Mat.Floor);
                    break;
                case Str.ErrorFlag:
                case Str.Stairs:
                    for (int deepChild = 0; deepChild < obj.childCount; deepChild++)
                    {
                        Transform deepObj = obj.GetChild(deepChild);

                        switch (deepObj.name.Split('_')[0])
                        {
                            case Str.Gate:
                                ChangeMaterial(transparent, deepObj, (int)Def.Mat.Gate);
                                break;
                            case Str.Wall:
                            case Str.Seperator:
                                ChangeMaterial(transparent, deepObj, (int)Def.Mat.Wall);
                                break;
                            case Str.Landing:
                                ChangeMaterial(transparent, deepObj, (int)Def.Mat.Floor);
                                break;
                            case Str.Stairs1:
                            case Str.Stairs2:
                                switch (splitName[1])
                                {
                                    case Str.HalfLanding:

                                        int material = (int)Def.Mat.StairsHalfLanding;

                                        if (deepObj.rotation.eulerAngles.x < 180)
                                        {
                                            if (deepObj.rotation.eulerAngles.x < Consts.RampAngle)
                                            {
                                                material = (int)Def.Mat.Floor;
                                            }
                                        }
                                        else
                                        {
                                            if (deepObj.rotation.eulerAngles.x - 360 > -Consts.RampAngle)
                                            {
                                                material = (int)Def.Mat.Floor;
                                            }
                                        }

                                        ChangeMaterial(transparent, deepObj, material);
                                        break;
                                    case Str.Straight:
                                        if (deepObj.rotation.eulerAngles.x - 360 > -Consts.RampAngle)
                                            ChangeMaterial(transparent, deepObj, (int)Def.Mat.Floor);
                                        else
                                            ChangeMaterial(transparent, deepObj, (int)Def.Mat.StairsStraight);
                                        break;
                                }
                                break;
                        }
                    }
                    break;
            }
        }
    }

    public void ChangeMaterial(bool transparent, Component obj, int materialId)
    {
        foreach (MeshRenderer mR in obj.gameObject.GetComponentsInChildren<MeshRenderer>())
            mR.material = transparent ? MaterialsTransparent[materialId] : MaterialsOpaque[materialId];
    }

    internal void DisplayAllLevels()
    {
        displayAllLevels = !displayAllLevels;

        NetworkServer.Instance.SendDisplayUpdate(displayAllLevels, UIController.Instance.GridDisplayEnabled, SelectedLevel);

        if (displayAllLevels)
        {
            if (UIController.Instance.GridDisplayEnabled)
                UIController.Instance.ViewToggleGridDisplay();

            for (int l = 0; l < NumLevels; l++)
            {
                SetTransparencyOfLevel(false, CreationParent.GetChild(l));

                if (l < SimulationController.Instance.HeatmapObjects.Count)
                    SimulationController.Instance.HeatmapObjects[l].GetComponent<MeshRenderer>().material.color = Color.white;
            }

            foreach (GridGraph gg in AstarPath.active.data.GetUpdateableGraphs())
                gg.drawGizmos = true;
        }
        else
        {
            if (!UIController.Instance.GridDisplayEnabled)
                UIController.Instance.ViewToggleGridDisplay();

            ChangeSelectedLevel(SelectedLevel);
        }

        SetAllColliders(true);
    }

    internal void RemakeGrids()
    {
        foreach (var level in _levels)
            level.Value.GetComponent<GridDisplay>().MakeGrid(Width, Height, -1 + level.Key * Statics.LevelHeight);
    }

    internal void AddLevel()
    {
        if (_creating)
            return;

        GameObject newLevel = new GameObject("Level_" + NumLevels);
        if (CreationParent == null)
        {
            Debug.LogError("Creation Parent not initialised, initialising...");
            CreationParent = GameObject.Find("CreatedObjects").transform;
        }
        newLevel.transform.SetParent(CreationParent);
        newLevel.isStatic = true;

        GridDisplay gridMaker = newLevel.AddComponent<GridDisplay>();
        gridMaker.Initialise();

        gridMaker.MakeGrid(Width, Height, -1 + NumLevels * Statics.LevelHeight);
        _levels[NumLevels] = newLevel;
        NumLevels++;

        ChangeSelectedLevel(NumLevels - 1);
        _snappingPolesWalls[SelectedLevel] = new List<Vector3>();
        _snappingPolesBarricades[SelectedLevel] = new List<Vector3>();

        bool levelExistsAtThisHeight = false;

        foreach (GridGraph gg2 in AstarPath.active.data.GetUpdateableGraphs())
            if (gg2.center.y == Statics.FloorHeight + Statics.FloorOffsetGrid + Statics.LevelHeight * (NumLevels - 1))
                levelExistsAtThisHeight = true;

        if (levelExistsAtThisHeight) return;

        GridGraph gg = (GridGraph)AstarPath.active.data.AddGraph(typeof(GridGraph));
        //GridGraph gg = new GridGraph { collision = _graphCollisionSettings };
        gg.collision = _graphCollisionSettings;
        gg.center = new Vector3(gg.center.x, Statics.FloorHeight + Statics.FloorOffsetGrid + Statics.LevelHeight * (NumLevels - 1), gg.center.z);
        //AstarPath.active.data.AddGraph(gg);

        GroundHelper.Instance.MakeGround();
    }

    internal void RemoveLevel()
    {
        if (_creating) return;

        NumLevels--;
        Destroy(GameObject.Find("Level_" + NumLevels));
        if (SelectedLevel > NumLevels - 1)
            SelectedLevel = NumLevels - 1;

        _levels.Remove(NumLevels);

        foreach (GridGraph gg in AstarPath.active.data.GetUpdateableGraphs())
        {
            float floorHeight = Statics.FloorHeight + Statics.FloorOffsetGrid + Statics.LevelHeight * NumLevels;

            if (gg.center.y == floorHeight)
                AstarPath.active.data.RemoveGraph(gg);
        }
    }

    internal void Undo()
    {
        if (_createdObjects.Count > 0)
        {
            GameObject objectToDelete = _createdObjects.Pop();

            if (objectToDelete == null)
            {
                Debug.Log("No object to undo?");
                return;
            }


            if (objectToDelete.name.Split('_')[0].Equals(Str.Stairs))
            {
                StairInfo si = objectToDelete.GetComponent<StairInfo>();

                foreach (Transform t in si.wallList)
                    Destroy(t.gameObject);
                foreach (Transform t in si.seperatorList)
                    Destroy(t.gameObject);

                Destroy(si.upperGate.gameObject);
                Destroy(si.lowerGate.gameObject);
            }

            Destroy(objectToDelete);
        }

        if (_createdObjects.Count > 0)
            fillFloors = 1;
    }

    internal void ClearAll()
    {
        while (UIController.Instance.NumLevels > 0)
            UIController.Instance.LevelRemoveLevel();

        _creating = false;

        foreach (Transform t in CreationParent)
            Destroy(t.gameObject);

        _createdObjects.Clear();
        ChangeSelectedObject(0);
        ObjectInfo.Instance.ResetIdCounter();
    }

    internal void GridSnapping(bool enabled)
    {
        _smallGridSnap = enabled;
    }

    internal void WallSnapping(bool enabled)
    {
        WallSnapEnabled = enabled;
    }

    /// <summary>
    /// Converts a wall class into a physical wall object in the environment.
    /// </summary>
    /// <param name="wall">Wall class to be created.</param>
    /// <param name="isBarricade">Whether to treat this wall as a barricade.</param>
    internal GameObject WallToObject(Wall wall, bool isBarricade = false)
    {
        Vector3 vertex1 = ToLh(new Vector3(wall.vertices[0].X, _creationHeight, wall.vertices[0].Y))
                          - (wall.isLow ? new Vector3(0f, 0.3f * Statics.LevelHeight, 0f) : Vector3.zero);
        Vector3 vertex2 = ToLh(new Vector3(wall.vertices[1].X, _creationHeight, wall.vertices[1].Y))
                          - (wall.isLow ? new Vector3(0f, 0.3f * Statics.LevelHeight, 0f) : Vector3.zero);

        if (wall.id < 0)
            wall.id = ObjectInfo.Instance.ArtifactId++;
        if (wall.vertices[0].id < 0)
            wall.vertices[0].id = ObjectInfo.Instance.AssignOrGetVID(wall.vertices[0]);
        if (wall.vertices[1].id < 0)
            wall.vertices[1].id = ObjectInfo.Instance.AssignOrGetVID(wall.vertices[1]);

        Prefabs[(int)Prefab.SPole].transform.position = vertex1;
        Prefabs[(int)Prefab.EPole].transform.position = vertex2;

        Prefabs[(int)Prefab.SPole].GetComponent<MeshRenderer>().material =
            isBarricade ? MaterialsOpaque[(int)Def.Mat.Barricade] : MaterialsOpaque[(int)Def.Mat.Wall];
        Prefabs[(int)Prefab.EPole].GetComponent<MeshRenderer>().material =
            isBarricade ? MaterialsOpaque[(int)Def.Mat.Barricade] : MaterialsOpaque[(int)Def.Mat.Wall];

        Prefabs[(int)Prefab.SPole].transform.LookAt(vertex2);
        Prefabs[(int)Prefab.EPole].transform.LookAt(vertex1);

        Distance = Vector3.Distance(vertex1, vertex2);

        GameObject _editingObject = Instantiate(isBarricade ? Prefabs[(int)Prefab.Barrier] : Prefabs[(int)Prefab.Wall],
            vertex1 + Distance / 2 * Prefabs[(int)Prefab.SPole].transform.forward,
            Quaternion.identity,
            CurrentLevelTransform());

        float height = _editingObject.transform.localScale.y * (Statics.LevelHeight / 2.5f);
        if (wall.isLow) height *= 0.4f;

        _editingObject.transform.rotation = Prefabs[(int)Prefab.SPole].transform.rotation;
        _editingObject.transform.localScale = new Vector3(_editingObject.transform.localScale.x, height, Distance);
        _editingObject.isStatic = true;

        // Extend the collider box of barricades to fix pathfinding issues (thx JianYu)
        if (Consts.extendWallColliderToPoles)
        {
            var boxC = _editingObject.GetComponent<BoxCollider>();
            boxC.size = new Vector3(boxC.size.x, boxC.size.y, (Distance + 0.5f) / Distance);
        }

        WallInfo wallInfo = _editingObject.GetComponent<WallInfo>();
        wallInfo.Apply(wall, isBarricade);
        ObjectInfo.Instance.ProcessNewWall(wall);
        _createdObjects.Push(_editingObject);

        // For poles 0 and 1 (start and end)
        for (int i = 0; i < 2; i++)
        {
            GameObject pole = Instantiate(Prefabs[i]);
            pole.transform.SetParent(_createdObjects.Peek().transform);

            if (isBarricade)
            {
                pole.transform.localScale = Vector3.Scale(pole.transform.localScale, new Vector3(1f, 0.99f, 1f));
                pole.tag = Str.PolesBarricadeTagString;
                if (wall.isLow)
                    pole.transform.localScale = Vector3.Scale(pole.transform.localScale, new Vector3(1f, 0.4f, 1f));
            }
            else
                pole.tag = Str.PolesTagString;
        }

        return _editingObject;
    }

    /// <summary>
    /// Converts a stair class into a physical stair object in the environment.
    /// </summary>
    /// <param name="stair">Stair class to be created.</param>
    internal void StairToObject(Stair stair)
    {
        StairInfo si;

        switch (stair.type)
        {
            case (int)Def.StairType.HalfLanding:
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.StairsHalfLanding],
                    ToLh(new Vector3(stair.x, Consts.MinusOne, stair.y), stair.lower.level),
                    Quaternion.identity,
                    _levels[stair.lower.level].transform);
                _editingObject.transform.Rotate(Vector3.up, stair.rotation);
                si = _editingObject.GetComponent<StairInfo>();
                si.stairType = Def.StairType.HalfLanding;
                break;

            case (int)Def.StairType.Straight:
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.StairsStraight],
                    ToLh(new Vector3(stair.x, Consts.MinusOne, stair.y), stair.lower.level),
                    Quaternion.identity,
                    _levels[stair.lower.level].transform);
                _editingObject.transform.Rotate(Vector3.up, stair.rotation);
                si = _editingObject.GetComponent<StairInfo>();
                si.stairType = Def.StairType.Straight;
                break;

            case (int)Def.StairType.Escalator:
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.StairsEscalator],
                    ToLh(new Vector3(stair.x, Consts.MinusOne, stair.y), stair.lower.level),
                    Quaternion.identity,
                    _levels[stair.lower.level].transform);
                _editingObject.transform.Rotate(Vector3.up, stair.rotation);
                si = _editingObject.GetComponent<StairInfo>();
                si.stairType = Def.StairType.Escalator;
                break;
        }
        _editingObject.isStatic = true;
        _createdObjects.Push(_editingObject);

        si = _editingObject.GetComponent<StairInfo>();
        si.stairDirection = (Def.StairDirection)stair.direction;
        si.ApplyDirection();
        si.StairId = stair.id == -1 ? ObjectInfo.Instance.ArtifactId++ : stair.id;
        si.speed = stair.speed;
        si.spanFloors = stair.spanFloors;
        si.seperatorList.Clear();
        si.wallList.Clear();

        for (int i = _createdObjects.Peek().transform.childCount - 1; i >= 0; i--)
        {
            Transform t = _createdObjects.Peek().transform.GetChild(i);
            switch (t.name.Split('_')[0])
            {
                case Str.Wall:
                    si.wallList.Add(t);
                    break;

                case Str.Seperator:
                    si.seperatorList.Add(t);
                    break;
            }
        }

        if (stair.length > 0)
            si.UpdateSize(stair.length, stair.width, stair.widthLanding);
        si.UpdateBoundaries(stair.lower.level, stair.rotation);

        // Initialise both gates
        var gateInfo = si.upperGate.GetComponent<GateInfo>();
        var gateInfo1 = si.lowerGate.GetComponent<GateInfo>();
        gateInfo.UpdateInfo();
        gateInfo1.UpdateInfo();

        foreach (Transform child in _levels[stair.upper.level].transform)
        {
            if (child.name.Split('_')[0] == Str.Gate)
            {
                if (ObjectInfo.AreTheSame(child.GetComponent<GateInfo>().Get, gateInfo.Get))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        foreach (Transform child in _levels[stair.lower.level].transform)
        {
            switch (child.name.Split('_')[0])
            {
                case Str.Gate:
                    if (ObjectInfo.AreTheSame(child.GetComponent<GateInfo>().Get, gateInfo1.Get))
                    {
                        Destroy(child.gameObject);
                    }
                    break;
            }
        }

        // Attach the stair gates to the right levels.
        si.upperGate.SetParent(_levels[stair.upper.level].transform);
        si.lowerGate.SetParent(_levels[stair.lower.level].transform);

        for (var i = 0; i < si.wallList.Count; i++)
        {
            var wall = si.wallList[i];
            wall.GetComponent<WallInfo>().UpdateInfo();

            foreach (Transform child in _levels[stair.lower.level].transform)
            {
                if (child.name.Split('_')[0] == Str.Wall)
                {
                    if (ObjectInfo.AreTheSame(child.GetComponent<WallInfo>().Get,
                                               wall.GetComponent<WallInfo>().Get))
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }

        // Also attach walls to the right levels.
        foreach (var wall in si.wallList.ToArray())
        {
            if (WallIsInUpperStairGate(wall))
            {
                si.wallList.Remove(wall);
                Destroy(wall.gameObject);
            }
            else
            {
                wall.SetParent(_levels[stair.lower.level].transform);
            }
        }
    }

    /// <summary>
    /// Converts a gate class into a physical gate object in the environment.
    /// </summary>
    /// <param name="gate"></param>
    internal GameObject GateToObject(Gate gate)
    {
        Vector3 vertex1 = ToLh(new Vector3(gate.vertices[0].X, _creationHeight, gate.vertices[0].Y));
        Vector3 vertex2 = ToLh(new Vector3(gate.vertices[1].X, _creationHeight, gate.vertices[1].Y));

        Prefabs[(int)Prefab.SPole].transform.position = vertex1;
        Prefabs[(int)Prefab.EPole].transform.position = vertex2;

        Prefabs[(int)Prefab.SPole].GetComponent<MeshRenderer>().material = MaterialsOpaque[(int)Def.Mat.Gate];
        Prefabs[(int)Prefab.EPole].GetComponent<MeshRenderer>().material = MaterialsOpaque[(int)Def.Mat.Gate];

        Prefabs[(int)Prefab.SPole].transform.LookAt(vertex2);
        Prefabs[(int)Prefab.EPole].transform.LookAt(vertex1);

        Distance = Vector3.Distance(vertex1, vertex2);

        GameObject _editingObject = Instantiate(
            Prefabs[(int)Prefab.Gate],
            vertex1 + Distance / 2 * Prefabs[(int)Prefab.SPole].transform.forward + Consts.GateOffset * (Statics.LevelHeight / 2.5f),
            Quaternion.identity,
            CurrentLevelTransform());

        _editingObject.transform.rotation = Prefabs[(int)Prefab.SPole].transform.rotation;

        _editingObject.transform.localScale = new Vector3(
            _editingObject.transform.localScale.x,
            _editingObject.transform.localScale.y * (Statics.LevelHeight / 2.5f),
            Distance);

        _editingObject.isStatic = true;
        GateInfo gateInfo = _editingObject.GetComponent<GateInfo>();
        gateInfo.Apply(gate);
        ObjectInfo.Instance.ProcessNewGate(gate);
        _createdObjects.Push(_editingObject);

        if (gate.waitingData != null)
        {
            GameObject ticketGateObject = Instantiate(
                Prefabs[(int)Prefab.TicketGate],
                ToLh(gate.vertices[1].GetVector3() + 0.5f * Vector3.Normalize(gate.vertices[0].GetVector3() - gate.vertices[1].GetVector3())),
                Quaternion.identity,
                CurrentLevelTransform());

            TicketGateInfo tgi = ticketGateObject.transform.GetComponent<TicketGateInfo>();
            tgi.UpdateSize(gate.length);

            ticketGateObject.transform.Rotate(Vector3.up, gate.angle + 90f);

            Destroy(tgi.gate.gameObject);
            tgi.gate = _editingObject.transform;
            tgi.level = SelectedLevel;
            tgi.isBidirectional = gate.waitingData.isBidirectional;
            tgi.UpdateInfo();

            foreach (var barricade in tgi.barricades)
            {
                if (barricade == null)
                    continue;

                barricade.GetComponent<WallInfo>().UpdateInfo();
            }

            foreach (var box in _editingObject.GetComponents<BoxCollider>())
            {
                if (box.center.y < -3f)
                    box.size = new Vector3(2.5f, box.size.y, box.size.z);
            }
        }
        else
        {
            if (_createdObjects.Count <= 0) return _editingObject;

            // For poles 0 and 1 (start and end)
            for (int i = 0; i < 2; i++)
            {
                GameObject pole = Instantiate(Prefabs[i]);
                pole.transform.SetParent(_createdObjects.Peek().transform);
                pole.transform.localScale = Vector3.Scale(pole.transform.localScale, new Vector3(0.8f, 1, 0.8f));
                pole.transform.position += Consts.GateOffset2;
                pole.tag = Str.PolesTagString;
            }
        }

        return _editingObject;
    }

    /// <summary>
    /// Converts a obstacle class into a physical obstacle object in the environment.
    /// </summary>
    /// <param name="obstacle"></param>
    internal void ObstacleToObject(CircularObstacle obstacle)
    {
        Vector3 vertex1 = new Vector3(obstacle.XPosition, _creationHeight, obstacle.YPosition);

        Distance = obstacle.Radius;
        GameObject _editingObject;

        if (obstacle.Weight > 0)
        {
            _editingObject = Instantiate(
                Prefabs[(int)Prefab.AvoidCircle],
                ToLh(vertex1),
                Quaternion.identity,
                CurrentLevelTransform());
            _editingObject.GetComponent<DangerArea>().WeightModifier = obstacle.Weight;
        }
        else
        {
            _editingObject = Instantiate(
                Prefabs[(int)Prefab.Obstacle],
                ToLh(vertex1),
                Quaternion.identity,
                CurrentLevelTransform());
        }

        _editingObject.transform.localScale = new Vector3(2 * Distance, _editingObject.transform.localScale.y, 2 * Distance);
        _editingObject.isStatic = true;
        _createdObjects.Push(_editingObject);
    }

    /// <summary>
    /// Converts a agent generation class into a physical agent generation object in the environment.
    /// </summary>
    /// <param name="agent"></param>
    internal GameObject AgentToObject(DistributionData agent)
    {
        Vector3 vertex1 = new Vector3(agent.xPosition, _creationHeight, agent.yPosition);

        Distance = agent.radius;
        GameObject _editingObject;

        switch ((Def.AgentPlacement)agent.placement)
        {
            case Def.AgentPlacement.Circle:
            case Def.AgentPlacement.Room:
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.Agent],
                    ToLh(vertex1) + Consts.AgentOffset * (Statics.LevelHeight / 2.5f),
                    Quaternion.identity,
                    CurrentLevelTransform());
                _editingObject.transform.localScale = new Vector3(Distance, _editingObject.transform.localScale.y, Distance);
                break;
            case Def.AgentPlacement.Rectangle:
                _editingObject = Instantiate(Prefabs[(int)Prefab.AgentSquare],
                    Prefabs[(int)Prefab.SPole].transform.position + Consts.AgentOffset * (Statics.LevelHeight / 2.5f),
                    Quaternion.identity,
                    CurrentLevelTransform());
                Prefabs[(int)Prefab.SPole].transform.position = ToLh(new Vector3(agent.RectangleVertices[0].X, 0, agent.RectangleVertices[0].Y));
                Prefabs[(int)Prefab.EPole].transform.position = ToLh(new Vector3(agent.RectangleVertices[1].X, 0, agent.RectangleVertices[1].Y));
                Prefabs[(int)Prefab.SPole].transform.LookAt(Prefabs[(int)Prefab.EPole].transform.position);
                _editingObject.transform.rotation = Prefabs[(int)Prefab.SPole].transform.rotation;
                _editingObject.transform.position = ToLh(new Vector3((
                    agent.RectangleVertices[1].X + agent.RectangleVertices[3].X) / 2f, _editingObject.transform.position.y, (
                    agent.RectangleVertices[1].Y + agent.RectangleVertices[3].Y) / 2f)) + Consts.AgentOffset * (Statics.LevelHeight / 2.5f);
                float distance2 = Utils.DistanceBetween(
                    agent.RectangleVertices[1].X, agent.RectangleVertices[1].Y,
                    agent.RectangleVertices[2].X, agent.RectangleVertices[2].Y);
                float distance1 = Utils.DistanceBetween(
                    agent.RectangleVertices[2].X, agent.RectangleVertices[2].Y,
                    agent.RectangleVertices[3].X, agent.RectangleVertices[3].Y);
                _editingObject.transform.localScale = new Vector3(distance2, _editingObject.transform.localScale.y, distance1);

                AgentDistInfo ap2 = _editingObject.GetComponent<AgentDistInfo>();
                ap2.SquarePoint1 = ToLh(new Vector3(agent.RectangleVertices[0].X, 0, agent.RectangleVertices[0].Y));
                ap2.SquarePoint2 = ToLh(new Vector3(agent.RectangleVertices[1].X, 0, agent.RectangleVertices[1].Y));
                ap2.SquarePoint3 = ToLh(new Vector3(agent.RectangleVertices[2].X, 0, agent.RectangleVertices[2].Y));
                ap2.SquarePoint4 = ToLh(new Vector3(agent.RectangleVertices[3].X, 0, agent.RectangleVertices[3].Y));
                ap2.distance1 = distance1;
                ap2.distance2 = distance2;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        AgentDistInfo ap = _editingObject.GetComponent<AgentDistInfo>();
        ap.NumberOfAgents = agent.population;
        ap.ID = agent.id;
        if (agent.dynamicDistributionData != null)
        {
            ap.MaxAgents = agent.dynamicDistributionData.MaxPopulation;
            ap.NumberOfAgentsIncremental = agent.dynamicDistributionData.PopulationIncremental;
            ap.GroupDynamicNumbers = agent.dynamicDistributionData.DymanicGroupData;
            ap.TimetableType = (Def.TimetableType)agent.dynamicDistributionData.TimetableType;
            ap.PopulationTimetable = agent.dynamicDistributionData.PopulationTimetable;
            if (ap.PopulationTimetable != null && ap.PopulationTimetable.Count < 1)
                ap.PopulationTimetable = null;
        }
        if (agent.dGatesData != null)
        {
            ap.DGatesData = agent.dGatesData;
        }
        ap.AgentPlacement = (Def.AgentPlacement)agent.placement;
        ap.AgentType = (Def.DistributionType)agent.distributionType;
        ap.color = agent.color.ToColor();
        ap.GroupNumbers = agent.GroupNumbers;
        _editingObject.isStatic = true;
        _createdObjects.Push(_editingObject);

        return _editingObject;
    }

    public void ThreatToObject(Threat threat)
    {
        GameObject _editingObject = Instantiate(
            Prefabs[(int)Prefab.Danger],
            ToLh(new Vector3(threat.X, 0, threat.Y)),
            Quaternion.identity,
            CurrentLevelTransform());

        ThreatInfo ti = _editingObject.GetComponent<ThreatInfo>();
        ti.Copy(threat);

        if (ti.Duration != int.MaxValue)
            ti.Duration /= 1000;

        if (ti.StartTime != int.MaxValue)
            ti.StartTime /= 1000;
        _editingObject.isStatic = true;
        _createdObjects.Push(_editingObject);
    }

    public void WaitPointToObject(WaitPoint waitPoint)
    {
        _editingObject = Instantiate(
            Prefabs[(int)Prefab.WaitPoint],
            ToLh(new Vector3(waitPoint.x, 0, waitPoint.y)) + WaitPointOffset,
            Quaternion.identity,
            CurrentLevelTransform());

        WaitPointInfo wp = _editingObject.GetComponent<WaitPointInfo>();

        wp.CopyFrom(waitPoint);

        _editingObject.isStatic = true;
        _createdObjects.Push(_editingObject);
    }

    public void TrainToObject(Train train)
    {
        switch (train.type)
        {
            case (int)Def.TrainType.Train:
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.Train],
                    ToLh(GetWorldPoint(true)) + trainOffset,
                    Quaternion.identity,
                    CurrentLevelTransform());
                break;
            case (int)Def.TrainType.Tram:
                _editingObject = Instantiate(
                    Prefabs[(int)Prefab.Tram],
                    ToLh(GetWorldPoint(true)) + trainOffset,
                    Quaternion.identity,
                    CurrentLevelTransform());
                break;
        }

        TrainInfo ti = _editingObject.transform.GetComponent<TrainInfo>();

        _editingObject.transform.position = train.MiddleVector3;
        _editingObject.transform.rotation = Quaternion.Euler(
            _editingObject.transform.localEulerAngles.x,
            train.rotation,
            _editingObject.transform.localEulerAngles.z);

        if (_selectedObject == Def.Object.Tram)
            ti.type = Def.TrainType.Tram;

        ti.Apply(train);
        ti.level = SelectedLevel;
        ti.CentreTrain();
        ti.trainGen.BuildTrain(false);
        ti.trainGen.HideItems();
        ti.trainGen.DisconnectTracks();
    }

    /// <summary>
    /// Instantiates a error flag at the specfied position on the specified level.
    /// </summary>
    /// <param name="message">Message to display on the error flag.</param>
    /// <param name="position">Position to place the error flag.</param>
    /// <param name="levelId">Level ID to put the flag on.</param>
    internal void MakeErrorFlag(Vector3 position, int levelId)
    {
        GameObject errorFlag = Instantiate(
            Prefabs[(int)Prefab.ErrorFlag],
            position,
            Quaternion.identity,
            _levels[levelId].transform);
        errorFlag.transform.localScale = new Vector3(errorFlag.transform.localScale.x, errorFlag.transform.localScale.y * (Statics.LevelHeight / 2.5f), errorFlag.transform.localScale.z);
        //errorFlag.GetComponentInChildren<TextMesh>().text = message;
        _errorFlags.Add(errorFlag);
    }

    /// <summary>
    /// Removes all flags from the scene.
    /// </summary>
    internal void ClearAllFlags()
    {
        foreach (GameObject g in _errorFlags)
            Destroy(g);

        _errorFlags.Clear();
    }

    public Vector3 ToFh(Vector3 input, int selectedLevel)
    {
        return new Vector3(input.x, Statics.FloorHeight + Statics.FloorOffset + Statics.LevelHeight * selectedLevel, input.z);
    }

    private static void DoubleNormals(ref Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector2[] uv = mesh.uv;
        Vector3[] normals = mesh.normals;
        int szV = vertices.Length;
        Vector3[] newVerts = new Vector3[szV * 2];
        Vector2[] newUv = new Vector2[szV * 2];
        Vector3[] newNorms = new Vector3[szV * 2];
        int j;
        for (j = 0; j < szV; j++)
        {
            // duplicate vertices and uvs:
            newVerts[j] = newVerts[j + szV] = vertices[j];
            newUv[j] = newUv[j + szV] = uv[j];
            // copy the original normals...
            newNorms[j] = normals[j];
            // and revert the new ones
            newNorms[j + szV] = -normals[j];
        }
        int[] triangles = mesh.triangles;
        int szT = triangles.Length;
        int[] newTris = new int[szT * 2]; // double the triangles
        for (int i = 0; i < szT; i += 3)
        {
            // copy the original triangle
            newTris[i] = triangles[i];
            newTris[i + 1] = triangles[i + 1];
            newTris[i + 2] = triangles[i + 2];
            // save the new reversed triangle
            j = i + szT;
            newTris[j] = triangles[i] + szV;
            newTris[j + 2] = triangles[i + 1] + szV;
            newTris[j + 1] = triangles[i + 2] + szV;
        }
        mesh.vertices = newVerts;
        mesh.uv = newUv;
        mesh.normals = newNorms;
        mesh.triangles = newTris; // assign triangles last!
    }

    internal void DestroyCreatedObject(GameObject g)
    {
        if (g == null)
            return;

        // We could be deleting a staircase, thus we need to delete child walls / gates.
        StairInfo si = g.GetComponent<StairInfo>();
        if (si != null)
        {
            foreach (Transform t in si.seperatorList)
                if (t != null)
                    Destroy(t.gameObject);
            foreach (Transform t in si.wallList)
                if (t != null)
                    Destroy(t.gameObject);
            if (si.upperGate != null)
                Destroy(si.upperGate.gameObject);
            if (si.lowerGate != null)
                Destroy(si.lowerGate.gameObject);
        }

        TrainGenerator tg = g.GetComponent<TrainGenerator>();
        if (tg != null)
            tg.ClearTrain();

        PlatformQueueInfo pqi = g.GetComponent<PlatformQueueInfo>();
        if (pqi != null)
            pqi.Destroy();

        TicketGateInfo tgi = g.GetComponent<TicketGateInfo>();
        if (tgi != null)
        {
            if (tgi.gate != null)
                Destroy(tgi.gate.gameObject);
            foreach (Transform t in tgi.barricades)
            {
                if (t != null)
                    Destroy(t.gameObject);
            }

        }

        Destroy(g);

        fillFloors = 1;
    }
    internal void ResetCamera()
    {
        UIController.Instance.SwitchBackToMainCamera();

        Transform camera = Camera.main.transform;

        camera.position = _cameraPos;
        camera.eulerAngles = _cameraRot;
    }

    public void ChangeLevelHeight(float newHeight)
    {
        Statics.LevelHeight = newHeight;

        Prefabs[(int)Prefab.SPole].transform.localScale = new Vector3(Prefabs[(int)Prefab.SPole].transform.localScale.x, 1.25f * (newHeight / 2.5f), Prefabs[(int)Prefab.SPole].transform.localScale.z);
        Prefabs[(int)Prefab.EPole].transform.localScale = new Vector3(Prefabs[(int)Prefab.EPole].transform.localScale.x, 1.25f * (newHeight / 2.5f), Prefabs[(int)Prefab.EPole].transform.localScale.z);
    }

    public void AbortCreation()
    {
        _creating = false;
        ChangeSelectedObject(0);
        Destroy(_editingObject);
        MovePolesBack();
    }
}