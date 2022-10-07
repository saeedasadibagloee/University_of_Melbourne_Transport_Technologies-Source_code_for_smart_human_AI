using System;
using System.Collections.Generic;
using Core;
using DataFormats;
using Helper;
using UnityEngine;
using Domain;

public class IndividualAgent : MonoBehaviour
{
    public const int two = 2;
    public short classID = 1;
    public int agentID = -1;
    public float radius = 0.23f;
    public int generationCycle = 0;
    public float agentRunTime = 0f;
    public bool DisplayAgents = true;
    public bool HideAgentsWhileRunning = false;
    public List<Vector3> gateList = new List<Vector3>();
    public List<Vector3> pathList = new List<Vector3>();
    public List<bool> escalatorList = new List<bool>();
    public List<Color> colorList = new List<Color>();
    public List<ushort> levelIdCoreList = new List<ushort>();

    //Move these to a common class to save memory
    public Material class2Material;
    public Material class2MaterialArrow;
    public Material linearLineMat;
    public Material pathOMat;
    public Material pathMMat;

    private Vector3[] path = new Vector3[0];

    public float DensityFromGrid = 0;
    public float runSpeed = 0;
    public float percentage = 0;

    private bool _isPaused = false;
    private Vector3 _velocity = Vector3.zero;

    private int _currentGate;
    private Animator _anim = null;
    private Vector3 _newPosition;
    private Vector3 _aheadPoint;
    private LineRenderer _lineLinear = null;
    private LineRenderer _linePathO = null;
    private LineRenderer _linePathM = null;
    private GameObject _childLinear;
    private GameObject _childPathO;
    private GameObject _childPathM;

    public List<Vector3> pathOriginal = new List<Vector3>();
    public List<Vector3> pathModified = new List<Vector3>();

    public AgentType type = AgentType.Individual;

    private bool _lowPolyMode = false;
    private GameObject _barneyLoDs = null;
    private GameObject _circleLowPoly = null;

    private SimulationController _simController;
    public bool isCurrentlyOnEscalator = false;

    void Start()
    {
        _anim = GetComponent<Animator>();
        Statics.AnimHashForward = Animator.StringToHash("Forward");
        _childLinear = new GameObject("LinearLine");
        _childPathO = new GameObject("PathOriginal");
        _childPathM = new GameObject("PathModified");
        _childLinear.transform.parent = transform;
        _childPathO.transform.parent = transform;
        _childPathM.transform.parent = transform;
        _lineLinear = _childLinear.AddComponent<LineRenderer>();
        _linePathO = _childPathO.AddComponent<LineRenderer>();
        _linePathM = _childPathM.AddComponent<LineRenderer>();
        _lineLinear.startWidth = _lineLinear.endWidth = Consts.LineWidth;
        _linePathO.startWidth = _linePathO.endWidth = Consts.LineWidth;
        _linePathM.startWidth = _linePathO.endWidth = Consts.LineWidth;
        _lineLinear.material = linearLineMat;
        _linePathO.material = pathMMat;
        _linePathM.material = pathOMat;

        _barneyLoDs = transform.GetChild(0).gameObject;
        _circleLowPoly = transform.GetChild(1).gameObject;

        _lowPolyMode = SimulationController.Instance.Is2D();
        ChangeMode(_lowPolyMode);

        _simController = SimulationController.Instance;
    }

    void Update()
    {
        float rotateSpeed = 8f * Consts.SpeedMultiplier;

        if (SimulationController.Instance.SimState == (int)Def.SimulationState.Replaying)
        {
            #region SIM DONE

            if (!Params.Current.RecordingEnabled)
            {
                HideAgent();
                return;
            }

            var m = 1 / agentRunTime;
            var c = -m * generationCycle * Params.Current.TimeStep;

            percentage = SimulationTimeKeeper.Instance.time * m + c;
            runSpeed = 1f / pathList.Count;

            if (percentage > 1 || percentage < 0)
            {
                HideAgent();
                return;
            }

            if (!DisplayAgents) ShowAgent();

            if (!_isPaused && DisplayAgents)
            {
                if (path.Length != pathList.Count)
                {
                    path = pathList.ToArray();
                }
                try
                {
                    _newPosition = LineInterpolator.Linear(path, percentage);
                    _aheadPoint = LineInterpolator.Linear(path, percentage + Consts.PathLookAhead * runSpeed);
                }
                catch (IndexOutOfRangeException) { }

                Vector3 currentPos = transform.position;

                CalculateCurrentGateNum();

                if (Vector3.Distance(currentPos, gateList[_currentGate]) < Consts.FaceGateDistance)
                {
                    // Face next gate.
                    Quaternion rotation = Quaternion.LookRotation(gateList[_currentGate] - currentPos);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * rotateSpeed);
                }
                else
                {
                    // Face path.
                    Quaternion rotation = Quaternion.identity;
                    rotation = _aheadPoint == currentPos ? transform.rotation : Quaternion.LookRotation(_aheadPoint - currentPos);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * rotateSpeed);
                }

                UpdateColor(colorList[_currentGate]);
            }

            if (_anim != null)
            {
                float speed = 0;

                if (Params.panicMode || !escalatorList[_currentGate])
                    //Calculate speed by looking at the distance between it's current point and a spot slighly ahead on the path:
                    speed = Vector3.Distance(_newPosition, _aheadPoint) * Consts.AnimMultiplier;

                _anim.SetFloat(Statics.AnimHashForward, SpeedToAnim(speed), 0.15f, Time.deltaTime);
                _anim.speed = Consts.SpeedMultiplier;

                //Debug.Log(speedToAnim(speed));
            }

            transform.position = _newPosition;
            #endregion
        }
        else
        {
            #region WHILE SIMULATING

            int pathCount = pathList.Count;

            // Simulation is still being received.
            if (pathList.Count > 0)
            {
                //transform.position = pathList[pathCount - 1];
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    pathList[pathCount - 1],
                    ref _velocity,
                    Consts.AgentCountToSmoothDampRatio * _simController.GetAgentCount());
            }

            // As long as we've got some information to work with.
            if (pathList.Count > 1)
            {
                Vector3 currentPoint = pathList[pathCount - 1];
                Vector3 previousPoint = pathList[pathCount - two];

                // If within gate face distance.
                if (Vector3.Distance(transform.position, gateList[_currentGate]) < Consts.FaceGateDistance)
                {
                    // Face next gate.
                    Quaternion rotation1 = Quaternion.LookRotation(gateList[gateList.Count - 1] - transform.position);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotation1, Time.deltaTime * rotateSpeed);
                }
                else
                {
                    // Set the rotation to the opposite of the previous point, if there is one.
                    _aheadPoint = currentPoint + (currentPoint - previousPoint);
                    Quaternion rotation = _aheadPoint == transform.position
                        ? transform.rotation
                        : Quaternion.LookRotation(_aheadPoint - transform.position);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * rotateSpeed);
                }

                float speed = 0;

                if (Params.panicMode || !escalatorList[escalatorList.Count - 1])
                    //Calculate speed by looking at the distance between it's current point and a spot slighly BEHIND on the path:
                    speed = Vector3.Distance(currentPoint, previousPoint) * Consts.AnimMultiplier * 0.075f;

                _anim.SetFloat(Statics.AnimHashForward, SpeedToAnim(speed), 0.15f, Time.deltaTime);
                _anim.speed = Consts.SpeedMultiplier * 275f / SimulationTimeKeeper.Instance.SimTimeDelta;
            }
            else
            {
                // Set an arbitrary forward animation.
                _anim.SetFloat(Statics.AnimHashForward, 0.4f);
            }
            #endregion
        }
        // Apply only rotation in the y axis.
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);

        if (!Params.Current.RecordingEnabled)
        {
            if (pathList.Count > two)
                pathList.RemoveAt(0);
            if (colorList.Count > two)
                colorList.RemoveAt(0);
            if (gateList.Count > two)
                gateList.RemoveAt(0);
        }

    }

    private GameObject BarneyLODs
    {
        get { return _barneyLoDs ?? (_barneyLoDs = transform.GetChild(0).gameObject); }
    }

    private GameObject CircleLowPoly
    {
        get { return _circleLowPoly ?? (_circleLowPoly = transform.GetChild(1).gameObject); }
    }

    internal float GetCurrentSpeed()
    {
        int currentStep = 0;

        if (SimulationController.Instance.SimState == (int)Def.SimulationState.Replaying)
            currentStep = Mathf.RoundToInt((pathList.Count - 1) * percentage);
        else
            currentStep = pathList.Count - 1;

        int previousStep = currentStep - 1;

        if (previousStep < 0 || percentage > 1)
            return 0f;

        float distance = 0, time = Params.Current.UiUpdateCycle / (1f / Params.Current.TimeStep);

        try
        {
            distance = Vector3.Distance(pathList[currentStep], pathList[previousStep]);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        return distance / time;
    }

    internal void HideAgent()
    {
        DisplayAgents = false;
        UpdateAgentVisibility();
    }

    internal bool GetLowPoly()
    {
        return _lowPolyMode;
    }

    internal void HideAgentWhileRunning(bool isTrue)
    {
        HideAgentsWhileRunning = isTrue;
        UpdateAgentVisibility();
    }

    internal void ShowAgent()
    {
        DisplayAgents = true;
        UpdateAgentVisibility();
    }

    internal void ChangeMode(bool lowPolyMode)
    {
        _lowPolyMode = lowPolyMode;
        UpdateAgentVisibility();
    }

    private void UpdateAgentVisibility()
    {
        if (_anim != null)
            _anim.enabled = !_lowPolyMode;

        if (DisplayAgents && !HideAgentsWhileRunning)
        {
            BarneyLODs.SetActive(!_lowPolyMode);
            CircleLowPoly.SetActive(_lowPolyMode);
        }
        else
        {
            BarneyLODs.SetActive(false);
            CircleLowPoly.SetActive(false);
            TogglePathFindingDisplay(false);
            pathOriginal.Clear();
            pathModified.Clear();
        }

        UpdateClass();
    }

    internal void SimulationCompleted()
    {
        UpdateColor(Color.white);
        Restart();
    }

    internal void AddPathGatePosEsc(Vector3 pathPos, Vector3 gatePos, bool isOnEscalator)
    {
        pathList.Add(pathPos);
        gateList.Add(gatePos);
        escalatorList.Add(isOnEscalator);
    }

    internal void UpdateColor(Color updatedColor)
    {
        GetComponent<DensityDisplay>().UpdateColor(updatedColor, _lowPolyMode);
    }

    /// <summary>
    /// Finds the gate the agent is about to come across from the percentage along its path.
    /// </summary>
    private void CalculateCurrentGateNum()
    {
        if (percentage > 1)
            _currentGate = gateList.Count - 1;
        else
            _currentGate = (int)Math.Round((gateList.Count - 1) * percentage);
    }

    /// <summary>
    /// This converts speed into an animation speed, because animation curve is not linear.
    /// </summary>
    /// <param name="speed">Should be between 0 and 5.661.</param>
    /// <returns>If input is correct, will return values between 0-1.</returns>
    private static float SpeedToAnim(float speed)
    {
        if (speed < 1.559)
        {
            return speed * 0.320718f;
        }
        return speed * 0.121892f + 0.309971f;
    }

    void OnDrawGizmos()
    {
        if (SimulationController.Instance.SimState == (int)Def.SimulationState.Replaying)
        {
            Gizmos.DrawLine(transform.position, gateList[_currentGate]);
            Gizmos.DrawLine(transform.position, gateList[_currentGate]);
            Gizmos.DrawLine(transform.position, gateList[_currentGate]);
        }
        else
        {
            if (gateList.Count == 0)
                return;

            Gizmos.DrawLine(transform.position, gateList[gateList.Count - 1]);
            Gizmos.DrawLine(transform.position, gateList[gateList.Count - 1]);
            Gizmos.DrawLine(transform.position, gateList[gateList.Count - 1]);
        }

    }

    internal void UpdateClass()
    {
        if (classID != two) return;
        if (_lowPolyMode)
        {
            MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
            if (mr != null)
                mr.material = class2MaterialArrow;
        }
        else
        {
            SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null)
                smr.material = class2Material;
        }
    }

    /// <summary>
    /// Pauses the animations and path movement.
    /// </summary>
    public void TogglePause(bool isPaused)
    {
        _isPaused = isPaused;

        if (_anim != null)
            _anim.enabled = !isPaused;
    }

    internal void ToggleLineDisplay(bool lineOn)
    {
        if (_lineLinear == null)
        {
            Debug.LogError("LineLinear Not initiated yet... continuing..");
            return;
        }

        if (!lineOn)
        {
            _lineLinear.enabled = false;
        }
        else
        {
            Vector3[] newPath = LineInterpolator.FixLineWithExtraPoints(pathList.ToArray());

            //Raise line by it's height.
            for (int i = 0; i < newPath.Length; i++)
            {
                newPath[i] = newPath[i] + Consts.LineDisplayHeightOffset;
            }

            _lineLinear.positionCount = newPath.Length;
            _lineLinear.SetPositions(newPath);
            _lineLinear.enabled = true;
            _lineLinear.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineLinear.numCapVertices = two;

            if (classID == two) return;
            _lineLinear.startColor = Color.white;
            _lineLinear.endColor = Color.white;
        }
    }

    internal void TogglePathFindingDisplay(bool lineOn)
    {
        if (_linePathO == null || _linePathM == null)
        {
            //Debug.LogError("Path objects haven't initialised.");
            return;
        }

        if (!lineOn)
        {
            _linePathO.enabled = false;
            _linePathM.enabled = false;
        }
        else
        {
            if (!DisplayAgents) return;

            if (pathOriginal.Count < 1)
            {
                //Debug.LogError("There is no path Original.");
                return;
            }

            if (pathModified.Count < 1)
            {
                //Debug.LogError("There is no path Original.");
                return;
            }

            Vector3[] newPath = LineInterpolator.FixLineWithExtraPoints(pathOriginal.ToArray());

            //Raise line by it's height.
            for (int i = 0; i < newPath.Length; i++)
            {
                newPath[i] = newPath[i] + Consts.LineDisplayHeightOffset - new Vector3(0f, 0.05f, 0f);
            }

            _linePathO.positionCount = newPath.Length;
            _linePathO.SetPositions(newPath);
            _linePathO.enabled = true;
            _linePathO.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _linePathO.numCapVertices = two;

            newPath = LineInterpolator.FixLineWithExtraPoints(pathModified.ToArray());

            //Raise line by it's height.
            for (int i = 0; i < newPath.Length; i++)
            {
                newPath[i] = newPath[i] + Consts.LineDisplayHeightOffset;
            }

            _linePathM.positionCount = newPath.Length;
            _linePathM.SetPositions(newPath);
            _linePathM.enabled = true;
            _linePathM.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _linePathM.numCapVertices = two;
        }
    }

    /// <summary>
    /// Call to replay the simulation.
    /// </summary>
    public void Restart()
    {
        agentRunTime = pathList.Count * Params.Current.UiUpdateCycle * Params.Current.TimeStep;

        DisplayAgents = true;

        ChangeMode(_lowPolyMode);
    }

    public void UpdateLineColor(Color newColor)
    {
        if (_lineLinear == null) return;
        _lineLinear.material.color = newColor;
    }

    public ushort GetCoreLevel()
    {
        if (_currentGate > levelIdCoreList.Count)
            return 0;
        return levelIdCoreList[_currentGate];
    }

    internal void Select()
    {
        string message = "AgentID: " + agentID + Environment.NewLine;
        message += "Path length: " + pathList.Count + Environment.NewLine;
        message += "Percentage: " + (percentage * 100).ToString("00.0") + "%" + Environment.NewLine;
        message += "Position: " + transform.position.ToString("F2") + Environment.NewLine;
        message += "Radius: " + radius.ToString("0.000") + Environment.NewLine;
        message += "CoreLevelID: " + GetCoreLevel();
        UIController.Instance.AgentsClickDialogOpen(message, "Selected Agent", gameObject);
    }

    internal Vector3 GetLatestPathPosition()
    {
        if (pathList.Count > 0)
            return pathList[pathList.Count - 1];

        return Vector3.zero;
    }
}