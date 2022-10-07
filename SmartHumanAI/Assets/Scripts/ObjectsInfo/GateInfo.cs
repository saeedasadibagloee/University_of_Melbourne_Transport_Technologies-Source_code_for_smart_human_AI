using System;
using System.Collections.Generic;
using DataFormats;
using UnityEngine;
using Assets.Scripts.ObjectsInfo;
using static Create;

namespace Info
{
    public class GateInfo : BaseObject
    {
        public int Id = -1;
        public float Length = -1f;
        public float Angle = -1f;
        public bool IsDestination = false;
        public bool IsTrainDoor = false;
        public bool IsTransparent = false;
        public bool DesignatedOnly = false;
        public List<Vertex> Vertices = new List<Vertex>();

        public bool IsCounter = false;
        public int Count = 0;

        private GameObject _gateSharesText;

        public int LevelId
        {
            get { return (int)((transform.position.y - 1.25f) / Statics.LevelHeight); }
        }

        protected internal Gate Get
        {
            get
            {
                if (Vertices.Count < 2)
                    UpdateVertices();

                Gate gate = new Gate
                {
          
                };

                if (IsTrainDoor)
                {
                    var trainDoorInfo = GetComponent<TrainDoorInfo>();
                    if (trainDoorInfo != null)
                        gate.trainData = trainDoorInfo.GetTrainData();
                }

                return gate;
            }
        }

        internal void UpdateLength(float length)
        {
            Length = length;

            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, length);

            foreach (Transform child in transform)
            {
                child.localScale = new Vector3(child.localScale.x, child.localScale.y, 0.5f / length);
            }
        }

        internal void UpdateVertices()
        {
            Vertices.Clear();

            Vector2 vertex1 = new Vector2(
                Mathf.RoundToInt(transform.GetChild(0).position.x / Consts.GridResolution) * Consts.GridResolution,
                Mathf.RoundToInt(transform.GetChild(0).position.z / Consts.GridResolution) * Consts.GridResolution);
            Vector2 vertex2 = new Vector2(
                Mathf.RoundToInt(transform.GetChild(1).position.x / Consts.GridResolution) * Consts.GridResolution,
                Mathf.RoundToInt(transform.GetChild(1).position.z / Consts.GridResolution) * Consts.GridResolution);

            Vertex v1 = new Vertex(vertex1.x, vertex1.y);
            Vertex v2 = new Vertex(vertex2.x, vertex2.y);

            Vertices.Add(v1);
            Vertices.Add(v2);
        }

        protected internal void Apply(Gate gate)
        {
            Id = gate.id;
            Length = gate.length;
            Angle = gate.angle;
            IsDestination = gate.destination;
            IsCounter = gate.counter;
            IsTransparent = gate.transparent;
            Vertices = gate.vertices;

            if (gate.trainData != null)
            {
                IsTrainDoor = true;
                var trainDoorInfo = gameObject.GetComponent<TrainDoorInfo>();
                if (trainDoorInfo == null)
                    trainDoorInfo = gameObject.AddComponent<TrainDoorInfo>();
                trainDoorInfo.trainID = gate.trainData.trainID;

                if (gate.trainData.waitingPositions != null && gate.trainData.waitingPositions.Count > 1)
                {
                    trainDoorInfo.waitingPositions = new List<Vector3>();

                    foreach (var wPos in gate.trainData.waitingPositions)
                        trainDoorInfo.waitingPositions.Add(wPos.ToV3());

                          }
            }

            if (IsCounter)
            {
                foreach (var collider in GetComponents<BoxCollider>())
                {
                    if (collider.center.y < -1)
                        Destroy(collider);
                }
            }
        }

        public override void UpdateInfo()
        {
            Apply(ObjectInfo.Instance.ObjectToGate(gameObject));
        }

        public override Vector3 PositionOffset()
        {
            return Vector3.zero;
        }
        internal void Select()
        {
            Gate g = Get;
            string message = Str.IDString + g.id + Environment.NewLine;
            message += Str.MiddleString + g.Middle + Environment.NewLine;
            message += Str.LengthString + g.length + Str.Meter + Environment.NewLine;
            message += Str.Vertex1String + g.vertices[0] + " [" + g.vertices[0].id + "]" + Environment.NewLine;
            message += Str.Vertex2String + g.vertices[1] + " [" + g.vertices[1].id + "]";
            UIController.Instance.DialogPrefabs(message, "Selected Gate", gameObject);
        }

        internal void ToggleTransparency()
        {
            IsTransparent = !IsTransparent;
            ChangeMaterial(IsTransparent, IsCounter ? (int)Def.Mat.Counter : (int)Def.Mat.Gate);
        }

        public static GateInfo CreateNew()
        {
            var _editingObject = Instantiate(
                            Instance.Prefabs[(int)Prefab.Gate],
                            Instance.Prefabs[(int)Prefab.SPole].transform.position + Consts.GateOffset * (Statics.LevelHeight / 2.5f),
                            Quaternion.identity,
                            Instance.CurrentLevelTransform());
            return _editingObject.GetComponent<GateInfo>();
        }

        public override void StartClick()
        {
            Prefabs[(int)Prefab.SPole].transform.position = Instance.WallSnapEnabled ?
                ToLh(ClosestPole(Instance.GetWorldPoint(true))) :
                ToLh(GetWorldPoint(true));
            Instance.MeshRendererStartPole.material = Instance.MaterialsOpaque[IsCounter ? (int)Def.Mat.Counter : (int)Def.Mat.Gate];
            Instance.MeshRendererEndPole.material = Instance.MaterialsOpaque[IsCounter ? (int)Def.Mat.Counter : (int)Def.Mat.Gate];
            ChangeMaterial(false, IsCounter ? (int)Def.Mat.Counter : (int)Def.Mat.Gate);
        }

        public override void Adjust()
        {
            base.Adjust();
            endPolePos = Input.GetKey(KeyCode.LeftShift) ? ToLh(ClosestPole(GetWorldPoint(true))) : ToLh(GetWorldPoint(true));
            Instance.Prefabs[(int)Prefab.EPole].transform.position = endPolePos;
            Instance.Prefabs[(int)Prefab.SPole].transform.LookAt(endPolePos);
            Instance.Prefabs[(int)Prefab.EPole].transform.LookAt(startPolePos);
            Instance.Distance = Vector3.Distance(startPolePos, endPolePos);
            transform.position = startPolePos + Instance.Distance / 2 * Prefabs[(int)Prefab.SPole].transform.forward + Consts.GateOffset * (Statics.LevelHeight / 2.5f);
            transform.rotation = Prefabs[(int)Prefab.SPole].transform.rotation;
            transform.localScale = new Vector3(
                transform.localScale.x,
                Statics.LevelHeight / 10f,
                Instance.Distance);
        }

        public override void EndClick()
        {
            // For poles 0 and 1 (start and end)
            for (int i = 0; i < 2; i++)
            {
                GameObject pole = Instantiate(Instance.Prefabs[i]);
                pole.transform.SetParent(transform);
                pole.transform.localScale = Vector3.Scale(pole.transform.localScale, new Vector3(0.8f, 1f, 0.8f));
                pole.transform.position += Consts.GateOffset2;
                if (!IsCounter) pole.tag = Str.PolesTagString;
            }

            UpdateInfo();
        }

        internal int GetMaterialId()
        {
            if (IsDestination)
                return (int)Def.Mat.GateDestination;

            return IsCounter ? (int)Def.Mat.Counter : (int)Def.Mat.Gate;
        }

        internal void ShowGateSharesCount(int gateUsage = -1)
        {
            SetupTextObject();

            if (gateUsage > 0)
                _gateSharesText.GetComponent<TextMesh>().text = gateUsage.ToString();
            else
                _gateSharesText.GetComponent<TextMesh>().text = Count.ToString();
        }

        private void SetupTextObject()
        {
            if (_gateSharesText != null)
                Destroy(_gateSharesText);

            _gateSharesText = Instantiate(Prefabs[(int)Prefab.GateShares], transform);
            _gateSharesText.transform.localScale = new Vector3(
            0.5f / GetComponent<GateInfo>().Length,
            _gateSharesText.transform.localScale.y,
            _gateSharesText.transform.localScale.z);
        }

        internal void ViewGateID(bool display)
        {
            SetupTextObject();

            if (display)
                _gateSharesText.GetComponent<TextMesh>().text = Id.ToString();
            else
                _gateSharesText.GetComponent<TextMesh>().text = "";
        }
    }
}
