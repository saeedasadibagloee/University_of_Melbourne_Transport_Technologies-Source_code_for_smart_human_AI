using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.DataFormats;
using DataFormats;
using Info;
using UnityEngine;

public class TrainGenerator : MonoBehaviour
{
    public bool doorOpen = true;
    public bool doorOpenLeft = true;
    public int numCarriages = 1;
    public float doorWidth = 1.638f;
    public float defaultCarriageLength = 28f;
    public List<int> numDoorsList = new List<int>();
    public List<float> carriageLengthsList = new List<float>();
    public List<float> boardDistributionList = new List<float>();

    // Sections of the train:
    public List<GameObject> trainDoorsOpen = new List<GameObject>();
    public List<GameObject> trainDoorsClosed = new List<GameObject>();
    public List<GameObject> trainParts = new List<GameObject>();
    public List<GameObject> trainWallsGates = new List<GameObject>();
    public List<GameObject> trainAgentDists = new List<GameObject>();
    public List<Transform> defaultWaitingAreaPoints= new List<Transform>();
    private GameObject trainTrackStorage;

    public GameObject Train_Front;
    public GameObject Train_Window;
    public GameObject Train_Door;
    public GameObject Train_DoorOpen;
    public GameObject Train_Back;
    public GameObject Train_Track;

    public enum CarriageParts { Front, Door, Window, Back, BackMirrored }
    private Dictionary<int, float> carriageWindowLengthModifier = new Dictionary<int, float>();

    private float intercarriageLength = 0.3f;
    private const float length_front = 1.404816f;
    private const float length_window = 1.575546f;
    private const float length_back = 0.218736f;

    public TrainInfo trainInfo = null;
    public BoxCollider collider = null;
    private int oldTrainID = -1;

    private float LengthDoorReal
    {
        get { return length_door / defaultDoorWidth * doorWidth; }
    }
    private const float length_door = 1.844346f;
    private const float defaultDoorWidth = 1.638f;
    private const float hTW = 1.5f; // Half of the train width (3.0m)

    public void BuildTrain(bool inclObjects = true)
    {
        var origRot = transform.localRotation;
        transform.localRotation = Quaternion.identity;

        ClearTrain(inclObjects);

        float sX = transform.position.x; // Start X
        float sY = transform.position.z; // Start Y

        float tL = 0f; // Cumulative train length

        for (int i = 0; i < numCarriages; i++)
        {
            List<CarriageParts> carriageParts = GenerateCarriage(i);
            bool makeWindowWall = false;
            float windowWallStartZLocation = -1f;
            float agentCarriageDistributionStartZLocation = -1f;

            foreach (var carriagePart in carriageParts)
            {
                switch (carriagePart)
                {
                    case CarriageParts.Front:
                        trainParts.Add(Instantiate(Train_Front, new Vector3(sX, transform.position.y, sY - tL), Quaternion.identity, transform));
                        if (inclObjects)
                        {
                            trainWallsGates.Add(Create.Instance.WallToObject(new Wall(
                                new Vertex(sX - hTW, sY - tL),
                                new Vertex(sX + hTW, sY - tL))));
                            trainWallsGates.Add(Create.Instance.WallToObject(new Wall(
                                new Vertex(doorOpenLeft ? sX - 1.5f : sX + 1.5f, sY - tL),
                                new Vertex(doorOpenLeft ? sX - hTW : sX + hTW, sY - tL - length_front))));
                        }
                        agentCarriageDistributionStartZLocation = sY - tL - length_front * 0.3f;
                        tL += length_front;
                        var go1 = new GameObject("WaitingAreaCorner1");
                        var go2 = new GameObject("WaitingAreaCorner2");
                        go1.transform.position = new Vector3(doorOpenLeft ? sX - 2f : sX + 2f, transform.position.y, sY - tL);
                        go2.transform.position = new Vector3(doorOpenLeft ? sX - 5f : sX + 5f, transform.position.y, sY - tL);
                        defaultWaitingAreaPoints.Add(go1.transform);
                        defaultWaitingAreaPoints.Add(go2.transform);
                        break;
                    case CarriageParts.Door:
                        if (inclObjects)
                        {
                            if (makeWindowWall)
                            {
                                makeWindowWall = false; // Make a wall representing all windows since previous door
                                trainWallsGates.Add(Create.Instance.WallToObject(new Wall(
                                    new Vertex(doorOpenLeft ? sX - hTW : sX + hTW, windowWallStartZLocation),
                                    new Vertex(doorOpenLeft ? sX - hTW : sX + hTW, sY - tL))));
                            }
                            trainWallsGates.Add(Create.Instance.GateToObject(new Gate(
                                new Vertex(doorOpenLeft ? sX - hTW : sX + hTW, sY - tL),
                                new Vertex(doorOpenLeft ? sX - hTW : sX + hTW, sY - tL - LengthDoorReal))));
                        }
                        var door_Clos = Instantiate(Train_Door, new Vector3(sX, transform.position.y, sY - tL), Quaternion.identity, transform).transform;
                        var door_Open = Instantiate(Train_DoorOpen, new Vector3(sX, transform.position.y, sY - (doorOpenLeft ? tL : tL + LengthDoorReal)), Quaternion.identity, transform).transform;
                        if (!doorOpenLeft)
                            door_Open.transform.Rotate(Vector3.up, 180f);
                        door_Clos.localScale = new Vector3(door_Clos.localScale.x, door_Clos.localScale.y, LengthDoorReal / length_door);
                        door_Open.localScale = new Vector3(door_Open.localScale.x, door_Open.localScale.y, LengthDoorReal / length_door);
                        door_Clos.gameObject.SetActive(false);
                        trainDoorsOpen.Add(door_Open.gameObject);
                        trainDoorsClosed.Add(door_Clos.gameObject);
                        tL += LengthDoorReal;
                        break;
                    case CarriageParts.Window:
                        if (!makeWindowWall)
                        {
                            makeWindowWall = true;
                            windowWallStartZLocation = sY - tL;
                        }
                        var window = Instantiate(Train_Window, new Vector3(sX, transform.position.y, sY - tL), Quaternion.identity, transform).transform;
                        window.localScale = new Vector3(window.localScale.x, window.localScale.y, carriageWindowLengthModifier[i]);
                        trainParts.Add(window.gameObject);
                        tL += carriageWindowLengthModifier[i] * length_window;
                        break;
                    case CarriageParts.Back:
                        if (inclObjects)
                        {
                            // Create carriage's agent distribution rectangle.
                            var dData = new DistributionData();
                            dData.color = Color3.Convert(trainInfo.color);
                            dData.distributionType = (int)Def.DistributionType.Dynamic;
                            dData.placement = (int)Def.AgentPlacement.Rectangle;
                            dData.RectangleVertices = new List<Vertex>();
                            dData.RectangleVertices.Add(new Vertex(sX - hTW * 0.83f, agentCarriageDistributionStartZLocation));
                            dData.RectangleVertices.Add(new Vertex(sX + hTW * 0.83f, agentCarriageDistributionStartZLocation));
                            dData.RectangleVertices.Add(new Vertex(sX + hTW * 0.83f, sY - tL));
                            dData.RectangleVertices.Add(new Vertex(sX - hTW * 0.83f, sY - tL));
                            trainAgentDists.Add(Create.Instance.AgentToObject(dData));
                            trainWallsGates.Add(Create.Instance.WallToObject(new Wall(
                                new Vertex(doorOpenLeft ? sX - hTW : sX + hTW, sY - tL),
                                new Vertex(doorOpenLeft ? sX - hTW : sX + hTW, sY - tL - length_back))));

                            if (i == numCarriages - 1)
                            {
                                // End of the train: make a destination gate, and the wall opposite the doors.
                                var destGate = Create.Instance.GateToObject(new Gate(
                                    new Vertex(doorOpenLeft ? sX - hTW : sX + hTW, sY - tL - length_back),
                                    new Vertex(!doorOpenLeft ? sX - hTW : sX + hTW, sY - tL - length_back)));
                                var gateInfo = destGate.GetComponent<GateInfo>();
                                gateInfo.DesignatedOnly = true;
                                gateInfo.Id = trainInfo.ObjectId;
                                trainWallsGates.Add(Create.Instance.WallToObject(new Wall(
                                    new Vertex(!doorOpenLeft ? sX - hTW : sX + hTW, sY - tL - length_back),
                                    new Vertex(!doorOpenLeft ? sX - hTW : sX + hTW, sY))));
                                trainWallsGates.Add(destGate);
                            }
                        }
                        if (i == numCarriages - 1)
                        {
                            // End of the train
                            var go3 = new GameObject("WaitingAreaCorner3");
                            var go4 = new GameObject("WaitingAreaCorner4");
                            go3.transform.position = new Vector3(doorOpenLeft ? sX - 5f : sX + 5f, transform.position.y, sY - tL);
                            go4.transform.position = new Vector3(doorOpenLeft ? sX - 2f : sX + 2f, transform.position.y, sY - tL);
                            defaultWaitingAreaPoints.Add(go3.transform);
                            defaultWaitingAreaPoints.Add(go4.transform);
                        }
                        trainParts.Add(Instantiate(Train_Back, new Vector3(sX, transform.position.y, sY - tL), Quaternion.identity, transform));
                        tL += length_back + intercarriageLength;
                        break;
                    case CarriageParts.BackMirrored:
                        if (inclObjects)
                            trainWallsGates.Add(Create.Instance.WallToObject(new Wall(
                                new Vertex(doorOpenLeft ? sX - hTW : sX + hTW, sY - tL + intercarriageLength),
                                new Vertex(doorOpenLeft ? sX - hTW : sX + hTW, sY - tL - length_back))));
                        tL += length_back;
                        agentCarriageDistributionStartZLocation = sY - tL;
                        var back = Instantiate(Train_Back, new Vector3(sX, transform.position.y, sY - tL), Quaternion.identity, transform);
                        back.transform.Rotate(Vector3.up, 180f);
                        trainParts.Add(back);
                        break;
                }
            }
        }

        trainTrackStorage = new GameObject("TrainTracks");
        trainTrackStorage.transform.SetParent(transform);
        //trainParts.Add(trainTrackStorage);

        for (int i = (int)-(tL / 2f) - 20; i < 20; i++)
        {
            var track = Instantiate(Train_Track, new Vector3(sX, transform.position.y, sY + 2 * i), Quaternion.identity, transform);
            track.transform.localPosition = new Vector3(track.transform.localPosition.x, 0.03985f, track.transform.localPosition.z);
            track.transform.localScale = new Vector3(0.423f, 0.2116f, 0.4231f);
            track.transform.SetParent(trainTrackStorage.transform);
        }

        if (inclObjects)
        {
            foreach (var go in trainWallsGates)
                go.transform.SetParent(transform);
            foreach (var go in trainAgentDists)
                go.transform.SetParent(transform);
        }

        foreach(var go in defaultWaitingAreaPoints)
            go.transform.SetParent(transform);

        // Place collider (for selecting train) in the middle of the train
        collider.center = new Vector3(collider.center.x, collider.center.y, 0.15f - tL / 2f);
        collider.size = new Vector3(collider.size.x, collider.size.y, tL + 0.5f);

        transform.localRotation = origRot;

        if (inclObjects)
        {
            foreach (var go in trainAgentDists)
                go.GetComponent<AgentDistInfo>().ReCalculateCorners();
        }

        Create.Instance.MovePolesBack();
    }

    public GateInfo GetDestinationGate()
    {
        // The destination gate should always be the last one
        return trainWallsGates[trainWallsGates.Count - 1].GetComponent<GateInfo>();
    }

    public void UnParentWallsGates()
    {
        foreach (var go in trainWallsGates)
            go.transform.SetParent(Create.Instance.CurrentLevelTransform());
        foreach (var go in trainAgentDists)
            go.transform.SetParent(Create.Instance.CurrentLevelTransform());

        UpdateInfo();

        foreach (var go in trainAgentDists)
            go.GetComponent<AgentDistInfo>().Initiate();

        DisconnectTracks();
    }

    private void UpdateInfo()
    {
        foreach (var go in trainWallsGates)
        {
            var wallInfo = go.GetComponent<WallInfo>();
            if (wallInfo == null)
                go.GetComponent<GateInfo>().UpdateInfo();
            else
                wallInfo.UpdateInfo();
        }
    }

    public void SetDoorOpen(bool open)
    {
        doorOpen = open;

        foreach (var door in trainDoorsOpen)
            door.SetActive(open);
        foreach (var door in trainDoorsClosed)
            door.SetActive(!open);
    }

    private List<CarriageParts> GenerateCarriage(int c)
    {
        var carriageList = new List<CarriageParts>();
        float carriageLength = defaultCarriageLength;

        if (carriageLengthsList != null && carriageLengthsList.Count > c)
            carriageLength = carriageLengthsList[c];
        else
        {
            Debug.Log("No lengths specified for carriage " + c + ". Setting to all default of " +
                      defaultCarriageLength);
            carriageLengthsList = new List<float>();
            for (int i = 0; i < numCarriages; i++)
                carriageLengthsList.Add(defaultCarriageLength);
        }

        if (numDoorsList == null || c > numDoorsList.Count - 1)
        {
            Debug.Log("No doors specified for carriage " + c + ". Setting to all default of 2.");
            numDoorsList = new List<int>();
            for (int i = 0; i < numCarriages; i++)
                numDoorsList.Add(2);
        }

        if (c == 0) // First carriage
        {
            carriageList.Add(CarriageParts.Front);
            carriageLength -= length_front;
        }
        else // Subsequent carriages don't have a driver's compartment.
        {
            carriageList.Add(CarriageParts.BackMirrored);
            carriageLength -= length_back;
        }

        carriageLength -= length_back;

        if (carriageLength < 0)
        {
            Debug.Log("Carriage Length of " + carriageLength + " too short!");
            return carriageList;
        }

        float remainingLengthExclDoors = carriageLength - numDoorsList[c] * LengthDoorReal;

        if (remainingLengthExclDoors < 0)
        {
            string message = "Carriage " + (c + 1) + " is too short for the amount of doors it has! Please fix this.";
            Debug.Log(message);
            UIController.Instance.ShowGeneralDialog(message, "Train Error");
            return carriageList;
        }

        float lengthBetweenDoors = remainingLengthExclDoors / (numDoorsList[c] - 1);
        int numWindowsBetweenDoors = Mathf.RoundToInt(lengthBetweenDoors / length_window);
        if (numWindowsBetweenDoors == 0)
            numWindowsBetweenDoors = 1;

        carriageWindowLengthModifier.Add(c, lengthBetweenDoors / numWindowsBetweenDoors / length_window);

        for (int j = 0; j < numDoorsList[c] - 1; j++)
        {
            carriageList.Add(CarriageParts.Door);
            for (int k = 0; k < numWindowsBetweenDoors; k++)
                carriageList.Add(CarriageParts.Window);
        }

        carriageList.Add(CarriageParts.Door);
        carriageList.Add(CarriageParts.Back);

        return carriageList;
    }

    public void HideItems()
    {
        foreach (var obj in trainWallsGates)
            foreach (var mr in obj.GetComponentsInChildren<MeshRenderer>())
                mr.enabled = false;
        foreach (var obj in trainAgentDists)
            foreach (var mr in obj.GetComponentsInChildren<MeshRenderer>())
                mr.enabled = false;
    }

    internal void ClearTrain(bool inclObject = true)
    {
        foreach (var gameObject in trainDoorsOpen)
            Destroy(gameObject);
        foreach (var gameObject in trainDoorsClosed)
            Destroy(gameObject);
        foreach (var gameObject in trainParts)
            Destroy(gameObject);
        foreach(var tf in defaultWaitingAreaPoints)
            Destroy(tf.gameObject);
        Destroy(trainTrackStorage);

        if (inclObject)
        {
            foreach (var gameObject in trainWallsGates)
                Destroy(gameObject);
            foreach (var gameObject in trainAgentDists)
                Destroy(gameObject);
            trainWallsGates.Clear();
            trainAgentDists.Clear();
        }

        trainDoorsOpen.Clear();
        trainDoorsClosed.Clear();
        trainParts.Clear();
        defaultWaitingAreaPoints.Clear();
        doorOpen = false;
        carriageWindowLengthModifier.Clear();
    }

    public void SetHidden(bool hidden)
    {
        foreach (var go in trainParts)
            go.SetActive(!hidden);

        if (!hidden)
        {
            if (doorOpen)
                foreach (var go in trainDoorsOpen)
                    go.SetActive(!hidden);
            else
                foreach (var go in trainDoorsClosed)
                    go.SetActive(!hidden);
        }
        else
        {
            foreach (var go in trainDoorsOpen)
                go.SetActive(!hidden);
            foreach (var go in trainDoorsClosed)
                go.SetActive(!hidden);
        }
    }

    public void DisconnectTracks()
    {
        trainTrackStorage.transform.SetParent(Create.Instance.CurrentLevelTransform());
    }

    public void InitialiseTrain()
    {
        oldTrainID = trainInfo.ObjectId; //To reconnect designated gates in agents later.

        trainInfo.ObjectId = GetDestinationGate().Id;
        trainInfo.level = GetDestinationGate().LevelId;

        foreach (var gate in trainWallsGates)
        {
            var gateInfo = gate.GetComponent<GateInfo>();
            if (gateInfo != null)
            {
                gateInfo.IsTrainDoor = true;

                if (!gateInfo.DesignatedOnly)
                    gateInfo.gameObject.AddComponent<TrainDoorInfo>().trainID = trainInfo.ObjectId;
            }
        }
    }

    public void FixAgentDistPoints()
    {
        foreach (var agentDist in trainAgentDists)
            agentDist.GetComponent<AgentDistInfo>().ReCalculateCorners();
    }

    public void ResetAgentDists()
    {
        foreach (var agentDist in trainAgentDists)
            agentDist.GetComponent<AgentDistInfo>().PopulationTimetable = new List<TimePopulation>();
    }

    public void CalcMaxAgents()
    {
        foreach (var agentDist in trainAgentDists)
            agentDist.GetComponent<AgentDistInfo>().CalcMaxAgents();
    }

    public void Apply(Train train)
    {
        doorWidth = train.doorWidth;
        numCarriages = train.numberCarriages;
        numDoorsList = train.numDoorsList;
        carriageLengthsList = train.carriageLengthsList;
        boardDistributionList = train.boardDistributionList;

        defaultWaitingAreaPoints = new List<Transform>();
        foreach (var vert in train.waitingAreaVertices)
        {
            var go = new GameObject("WaitingAreaCorner");
            go.transform.position = new Vector3(vert.X, transform.position.y, vert.Y);
            defaultWaitingAreaPoints.Add(go.transform);
        }

        trainWallsGates = new List<GameObject>();
        trainAgentDists = new List<GameObject>();

        foreach (var wallGateId in train.wallGatesIds)
        {
            bool idFound = false;

            foreach (var possibleWall in FindObjectsOfType<WallInfo>())
            {
                if (possibleWall.Id == wallGateId)
                {
                    idFound = true;
                    trainWallsGates.Add(possibleWall.gameObject);
                    break;
                }
            }

            if (!idFound)
            {
                foreach (var possibleGate in FindObjectsOfType<GateInfo>())
                {
                    if (possibleGate.Id == wallGateId)
                    {
                        idFound = true;
                        trainWallsGates.Add(possibleGate.gameObject);
                        break;
                    }
                }
            }

            if (!idFound)
                Debug.LogError("Could not find a matching gate/wall ID as specified by the train.");
        }

        foreach (var agentDistId in train.agentDistIds)
        {
            bool idFound = false;

            foreach (var possibleAgentDist in FindObjectsOfType<AgentDistInfo>())
            {
                if (possibleAgentDist.ID == agentDistId)
                {
                    idFound = true;
                    trainAgentDists.Add(possibleAgentDist.gameObject);
                    break;
                }
            }

            if (!idFound)
                Debug.LogError("Could not find a matching agentDistribution ID as specified by the train.");
        }
    }

    public List<GateInfo> GetTrainDoors()
    {
        List<GateInfo> gates = new List<GateInfo>();

        for (int i = 0; i < trainWallsGates.Count - 1; i++)
        {
            var gateInfo = trainWallsGates[i].GetComponent<GateInfo>();
            if (gateInfo != null)
                gates.Add(gateInfo);
        }

        return gates;
    }

    public void ReconnectAgentDistributions()
    {
        if (oldTrainID < 0)
            return;

        foreach (var agentDistInfo in FindObjectsOfType<AgentDistInfo>())
        {
            if (agentDistInfo.DGatesData == null)
                continue;

            foreach (var dDataTime in agentDistInfo.DGatesData)
            {
                if (dDataTime.Distribution == null)
                    continue;

                foreach(var dGate in dDataTime.Distribution)
                    if (dGate.GateID == oldTrainID)
                        dGate.GateID = trainInfo.ObjectId;
            }
        }
    }
}
