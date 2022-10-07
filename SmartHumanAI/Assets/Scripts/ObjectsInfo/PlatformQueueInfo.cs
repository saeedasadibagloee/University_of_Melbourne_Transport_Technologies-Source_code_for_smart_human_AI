using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Assets.Scripts.ObjectsInfo;
using UnityEngine;

namespace Info
{
    public class PlatformQueueInfo : BaseObject
    {
        private const float lineWidth = 0.65f;
        private static Vector3 lineOffset = new Vector3(0, -0.9f, 0);

        public List<Vector3> pointsList = new List<Vector3>();
        public LineRenderer lineRenderer;
        public SphereCollider collider;

        public int trainID = -1;
        public TrainInfo train;
        public GateInfo gate;

        public List<GateInfo> otherGates = new List<GateInfo>();
        public List<PlatformQueueInfo> otherQueues = new List<PlatformQueueInfo>();

        public void Start()
        {
            lineRenderer.widthMultiplier = lineWidth;
            collider.radius = lineWidth / 2f;
            collider.center = new Vector3(0, -0.89f, 0);
        }

        public override void EndClick()
        {
            EndClickWorker();

            foreach (var pqi in otherQueues)
                pqi.EndClickWorker();
        }

        public void EndClickWorker()
        {
            pointsList.Add(lineRenderer.GetPosition(lineRenderer.positionCount - 1));

        }

        public void ApplyQueue()
        {
            if (gate == null)
                gate = GetComponentInParent<GateInfo>();

            gate.GetComponent<TrainDoorInfo>().waitingPositions = pointsList.ToArray().ToList();
        }

        public void UnApplyQueue()
        {
            if (gate == null)
                gate = GetComponentInParent<GateInfo>();

            gate.GetComponent<TrainDoorInfo>().waitingPositions = null;
        }

        public void StartQueue()
        {
            StartQueueWorker();
            InitialiseLeader();
        }

        public void StartQueueWorker()
        {
            pointsList = new List<Vector3>();
            pointsList.Add(transform.position + lineOffset);
        }

        private void InitialiseLeader()
        {
            var trains = FindObjectsOfType<TrainInfo>();

            if (trains.Length < 1)
            {
                UIController.Instance.ShowGeneralDialog("NB: Cannot find a train", "No Trains");
                Create.Instance.AbortCreation();
                return;
            }

            float distance = float.MaxValue;

            foreach (var train in trains)
            {
                if (train.level != Create.Instance.SelectedLevel)
                    continue;

                foreach (GateInfo door in train.GetTrainDoors())
                {
                    var newDistance = Utils.DistanceBetween(door.transform.position.x, door.transform.position.z,
                        transform.position.z, transform.position.z);

                    if (newDistance < distance)
                    {
                        distance = newDistance;
                        trainID = train.ObjectId;
                        this.train = train;
                        gate = door;
                    }
                }
            }

            if (distance > 20f)
                UIController.Instance.ShowGeneralDialog("NB: The nearest train door detected was more than 20 metres away. Are you sure this is correct?", "Train Far Away");

            otherGates = new List<GateInfo>();
            otherQueues = new List<PlatformQueueInfo>();

            foreach (var trainDoor in train.GetTrainDoors())
                if (trainDoor.Id != gate.Id)
                    if (trainDoor.GetComponentInChildren<PlatformQueueInfo>() == null)
                        otherGates.Add(trainDoor);

            foreach (var otherGate in otherGates)
            {
                var pqi = Instantiate(Create.Instance.Prefabs[(int)Create.Prefab.PlatformQueue]).GetComponent<PlatformQueueInfo>();
                pqi.transform.position = transform.position - (gate.transform.position - otherGate.transform.position);
                pqi.transform.SetParent(otherGate.transform);
                pqi.trainID = trainID;
                pqi.train = train;
                pqi.gate = otherGate;
                pqi.StartQueueWorker();
                otherQueues.Add(pqi);
            }

            transform.SetParent(gate.transform);
        }

        public void Adjust(Vector3 newPos)
        {
            var prevPos = pointsList.Last();

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                float xDiff = Mathf.Abs(prevPos.x - newPos.x);
                float yDiff = Mathf.Abs(prevPos.z - newPos.z);

                if (xDiff < yDiff)
                    newPos = new Vector3(prevPos.x, newPos.y, newPos.z);
                else
                    newPos = new Vector3(newPos.x, newPos.y, prevPos.z);
            }

            AdjustNewPosition(newPos);

            for (int i = 0; i < otherGates.Count; i++)
            {
                var newPosTranslated = newPos - (gate.transform.position - otherGates[i].transform.position);
                otherQueues[i].AdjustNewPosition(newPosTranslated);
            }
        }

        private void AdjustNewPosition(Vector3 newPos)
        {
            List<Vector3> newLine = pointsList.ToArray().ToList();
            newLine.Add(newPos + lineOffset);
            lineRenderer.positionCount = newLine.Count;
            for (int i = 0; i < newLine.Count; i++)
                lineRenderer.SetPosition(i, newLine[i]);
        }

        public bool IsReal()
        {
            return pointsList.Count > 1;
        }

        public void OnDrawGizmos()
        {
            foreach (var point in pointsList)
                Gizmos.DrawSphere(point, 0.2f);
        }

        public void CompleteQueue()
        {
            CompleteQueueWorker();

            foreach (var pqi in otherQueues)
                pqi.CompleteQueueWorker();
        }

        private void CompleteQueueWorker()
        {
            pointsList.RemoveAt(pointsList.Count - 1);
            UpdateLine();
            ApplyQueue();
        }

        public void UpdateLine()
        {
            lineRenderer.positionCount = pointsList.Count;
            for (int i = 0; i < pointsList.Count; i++)
                lineRenderer.SetPosition(i, pointsList[i]);
        }

        public void Select()
        {
            string message = "";
            message += "Train ID: " + trainID + "\n";
            message += pointsList.Count + " points: ";
            foreach (var point in pointsList)
                message += "(" + point.x.ToString("0.#") + ", " + point.z.ToString("0.#") + "), ";
            UIController.Instance.DialogPrefabs(message, "Selected Platform Queue", gameObject);
        }

        internal void Destroy()
        {
            foreach (var otherQ in otherQueues)
                if (otherQ != null)
                    otherQ.Destroy();

            UnApplyQueue();
            Destroy(gameObject);
        }

        public override Vector3 PositionOffset()
        {
            return Vector3.zero;
        }

        public override void UpdateInfo()
        {
            //throw new NotImplementedException();
        }

        public override void StartClick()
        {
            throw new NotImplementedException();
        }
    }
}
