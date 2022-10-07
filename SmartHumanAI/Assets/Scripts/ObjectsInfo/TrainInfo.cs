using System.Collections.Generic;
using Assets.Scripts.DataFormats;
using DataFormats;
using InputOutput;
using UnityEngine;
using Assets.Scripts.ObjectsInfo;

namespace Info
{
    public class TrainInfo : BaseObject
    {
        public static int travelDistance = 200;
        public float arrivalSpeed = 18;

        public int ObjectId = -1;
        public int level = -1;
        public int rotation = 0;
        public Def.TrainType type = Def.TrainType.Train;

        private bool doorsOpen = false;

        public List<Transform> wallList = new List<Transform>();
        public List<Transform> gateList = new List<Transform>();

        public Vector3 StartVector3;
        public Vector3 MiddleVector3;
        public Vector3 EndVector3;

        private Vector3 rayPos = Vector3.zero;


        public List<TrainData> TrainTimetableData { get; set; }

        public TrainGenerator trainGen = null;
        public Color color = Color.white;

        public void Start()
        {
            trainGen = GetComponent<TrainGenerator>();

            if (ObjectId < 0)
                ObjectId = ObjectInfo.Instance.ArtifactId++;
        }

        public void Update()
        {
            if (SimulationController.Instance.SimState == (int)Def.SimulationState.NotStarted ||
                SimulationController.Instance.SimState == (int)Def.SimulationState.Interrupted)
            {
                if (doorsOpen)
                    SetDoorsOpen(false);
                trainGen.SetHidden(false);
                return;
            }

            if (TrainTimetableData == null)
                return;

            trainGen.SetHidden(true);

            foreach (TrainData td in TrainTimetableData)
            {
                float currentTime = SimulationTimeKeeper.Instance.time;

                // Iterate until we have the right timetable entry.
                if (currentTime > td.departureTime + arrivalSpeed)
                    continue;

                float startArrivingTime = td.arrivalTime - arrivalSpeed;

                if (currentTime < startArrivingTime)
                {
                    // No train.
                    trainGen.SetHidden(true);

                    if (transform.position != StartVector3)
                        transform.position = StartVector3;

                    if (doorsOpen)
                        SetDoorsOpen(false);
                }
                else if (currentTime > startArrivingTime && currentTime < td.arrivalTime)
                {
                    // Arriving
                    trainGen.SetHidden(false);

                    float lerpFraction = (currentTime - startArrivingTime) / arrivalSpeed;
                    lerpFraction = InverseQuadraticFunction(lerpFraction);
                    transform.position = Vector3.Lerp(StartVector3, MiddleVector3, lerpFraction);

                    if (doorsOpen)
                        SetDoorsOpen(false);
                }
                else if (currentTime >= td.arrivalTime && currentTime <= td.departureTime)
                {
                    // Waiting
                    trainGen.SetHidden(false);

                    if (transform.position != MiddleVector3)
                        transform.position = MiddleVector3;

                    if (!doorsOpen)
                        SetDoorsOpen(true);
                }
                else if (currentTime > td.departureTime)
                {
                    // Departing
                    trainGen.SetHidden(false);

                    float lerpFraction = (currentTime - td.departureTime) / arrivalSpeed;
                    lerpFraction = lerpFraction * lerpFraction;
                    transform.position = Vector3.Lerp(MiddleVector3, EndVector3, lerpFraction);

                    if (lerpFraction >= 1)
                        trainGen.SetHidden(true);

                    if (doorsOpen)
                        SetDoorsOpen(false);
                }

                break;
            }
        }

        public Train Get
        {
            get
            {
                var waitingVerticesActual = new List<Vertex>();

                foreach (var transf in trainGen.defaultWaitingAreaPoints)
                    waitingVerticesActual.Add(new Vertex(transf.position.x, transf.position.z));

                Train train =
                    new Train
                    {
                        ID = ObjectId,
                        trainDataTimetable = TrainTimetableData,
                        waitingAreaVertices = waitingVerticesActual,
                        StartVector3 = StartVector3,
                        MiddleVector3 = MiddleVector3,
                        EndVector3 = EndVector3,
                        rotation = rotation,
                        type = (int) type,
                        destinationGateID = ObjectId,
                        numberCarriages = trainGen.numCarriages,
                        numDoorsList = trainGen.numDoorsList,
                        doorWidth = trainGen.doorWidth,
                        carriageLengthsList = trainGen.carriageLengthsList,
                        boardDistributionList = trainGen.boardDistributionList,
                        gateIDsByCarriage = GenerateGateIDByCarriage(),
                        wallGatesIds = new List<int>(),
                        agentDistIds = new List<int>()
                    };
                
                foreach (var wallOrGate in trainGen.trainWallsGates)
                {
                    var wallInfo = wallOrGate.GetComponent<WallInfo>();
                    train.wallGatesIds.Add(wallInfo == null ? wallOrGate.GetComponent<GateInfo>().Id : wallInfo.Id);
                }

                foreach (var agentDist in trainGen.trainAgentDists)
                    train.agentDistIds.Add(agentDist.GetComponent<AgentDistInfo>().ID);

                return train;
            }
        }

        public override Vector3 PositionOffset()
        {
            return new Vector3(0, -1.225f, 0) + new Vector3(0.5f, 0f, 0.5f);
        }

        private List<List<int>> GenerateGateIDByCarriage()
        {
            List<List<int>> gateIDbyCarriage = new List<List<int>>();
            int carriage = 0;
            int doorNum = 0;

            foreach (var gate in GetTrainDoors())
            {
                if (doorNum == 0)
                    gateIDbyCarriage.Add(new List<int>());

                gateIDbyCarriage[carriage].Add(gate.Id);

                doorNum++;

                if (doorNum == trainGen.numDoorsList[carriage])
                {
                    doorNum = 0;
                    carriage++;
                }
            }

            return gateIDbyCarriage;
        }

        private void SetDoorsOpen(bool b)
        {
            doorsOpen = b;
            trainGen.SetDoorOpen(doorsOpen);
        }

        private float InverseQuadraticFunction(float lerpFraction)
        {
            float toSquare = lerpFraction - 1;
            return -(toSquare * toSquare) + 1;
        }

        public void ApplyTrainData(List<TrainData> trainDatas)
        {
            TrainTimetableData = trainDatas;
            ApplySavedTrainData();
        }

        public void ApplySavedTrainData()
        {
            trainGen.ResetAgentDists();

            foreach (var trainData in TrainTimetableData)
            {
                if (trainData.passengersInCarriages != null && trainData.passengersInCarriages.Count > 1)
                {
                    // Apply carriage populations as specified by user.
                    for (int i = 0; i < trainData.passengersInCarriages.Count; i++)
                        if (i < trainGen.trainAgentDists.Count)
                            trainGen.trainAgentDists[i].GetComponent<AgentDistInfo>().PopulationTimetable.Add(
                                new TimePopulation(trainData.arrivalTime - 0.5f, trainData.passengersInCarriages[i]));
                }
                else
                {
                    // Evenly distribute carriage populations (as user has not specified)
                    for (int i = 0; i < trainGen.trainAgentDists.Count; i++)
                        trainGen.trainAgentDists[i].GetComponent<AgentDistInfo>().PopulationTimetable.Add(
                            new TimePopulation(trainData.arrivalTime - 0.5f, trainData.passengers / trainGen.trainAgentDists.Count));
                }
            }

            trainGen.CalcMaxAgents();
        }

        public void CentreTrain()
        {
            if (transform.position != MiddleVector3)
                transform.position = MiddleVector3;

            if (doorsOpen)
                SetDoorsOpen(false);
        }

        public void ReverseDirection()
        {
            Vector3 start = StartVector3;
            StartVector3 = EndVector3;
            EndVector3 = start;
        }

        public void Apply(Train train)
        {
            TrainTimetableData = train.trainDataTimetable;
            StartVector3 = train.StartVector3;
            MiddleVector3 = train.MiddleVector3;
            EndVector3 = train.EndVector3;
            rotation = train.rotation;
            type = (Def.TrainType)train.type;
            ObjectId = train.ID;

            trainGen.Apply(train);
            trainGen.GetDestinationGate().DesignatedOnly = true;
        }

        public void ApplyDesignatedData(DistributionInformation distributionTableData)
        {
            trainGen.trainAgentDists[0].GetComponent<AgentDistInfo>().ApplyDesignatedData(distributionTableData);
        }

        internal void InitialiseTrain()
        {
            trainGen.UnParentWallsGates();
            trainGen.InitialiseTrain();
        }

        public void FixAgentDistPoints()
        {
            trainGen.FixAgentDistPoints();
        }

        public void HideFloor()
        {
            Vector3 startPos = transform.position;
            if (trainGen != null && trainGen.trainAgentDists != null && trainGen.trainAgentDists.Count > 0) 
                startPos = trainGen.trainAgentDists[0].transform.position;
            
            var hits = Physics.RaycastAll(startPos, Vector3.down, 1f, LayerMask.GetMask("Ground"));
            foreach (var hit in hits)
                if (hit.transform.name.Split('_')[0] == "RoomFloor")
                    hit.transform.GetComponent<MeshRenderer>().enabled = false;
        }

        public void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(rayPos, 0.3f);
        }

        public List<GateInfo> GetTrainDoors()
        {
            return trainGen.GetTrainDoors();
        }

        public override void EndClick()
        {
            level = Create.Instance.SelectedLevel;
            rotation = (int)transform.localEulerAngles.y;
            MiddleVector3 = transform.position;
            InitialiseTrain();
            FixAgentDistPoints();

            while (rotation > 360)
                rotation -= 360;

            while (rotation < 0)
                rotation += 360;

            switch (rotation)
            {
                case 0:
                    StartVector3 = transform.position + travelDistance * Vector3.back;
                    EndVector3 = transform.position + travelDistance * Vector3.forward;
                    break;
                case 90:
                    StartVector3 = transform.position + travelDistance * Vector3.left;
                    EndVector3 = transform.position + travelDistance * Vector3.right;
                    break;
                case 180:
                    StartVector3 = transform.position + travelDistance * Vector3.forward;
                    EndVector3 = transform.position + travelDistance * Vector3.back;
                    break;
                case 270:
                    StartVector3 = transform.position + travelDistance * Vector3.right;
                    EndVector3 = transform.position + travelDistance * Vector3.left;
                    break;
                default:
                    Debug.LogError("Could not determine train angle.");
                    break;
            }

            CentreTrain();
        }

        public override void UpdateInfo()
        {
            throw new System.NotImplementedException();
        }

        public override void StartClick()
        {
            throw new System.NotImplementedException();
        }
    }
}
