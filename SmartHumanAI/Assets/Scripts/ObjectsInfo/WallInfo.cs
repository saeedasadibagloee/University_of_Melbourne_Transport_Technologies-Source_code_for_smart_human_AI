using System;
using System.Collections.Generic;
using Assets.Scripts.ObjectsInfo;
using DataFormats;
using UnityEngine;
using static Create;

namespace Info
{
    public class WallInfo : BaseObject
    {
        public int Id = -1;
        public float Length = -1f;
        public float Angle = -1f;
        public List<Vertex> Vertices = new List<Vertex>();
        public bool IsBarricade = false;
        public bool iWLWG = false;
        public bool IsLow = false;
        public bool IsTransparent = false;

        public void Select()
        {
            Wall w = Get;
            string message = Str.IDString + w.id + Environment.NewLine;
            if (w.vertices != null && w.vertices.Count > 1)
            {
                message += Str.Vertex1String + w.vertices[0] + " [" + w.vertices[0].id + "]" + Environment.NewLine;
                message += Str.Vertex2String + w.vertices[1] + " [" + w.vertices[1].id + "]" + Environment.NewLine;
            }
            message += Str.LengthString + w.length.ToString(Str.DecimalFormat) + Str.Meter;
            UIController.Instance.DialogPrefabs(message, "Selected Wall/Barricade", gameObject);
        }

        protected internal void Apply(Wall wall, bool barricade)
        {

        }

        protected internal Wall Get
        {
            get
            {
                if (Vertices.Count < 2)
                    UpdateVertices();

                return new Wall
                {
                    id = Id,
                    angle = Angle,
                    length = Length,
                    vertices = Vertices,
                    isLow = IsLow,
                    iWlWG = iWLWG,
                    isTransparent = IsTransparent
                };
            }
        }

        public override Vector3 PositionOffset()
        {
            return Vector3.zero;
        }

        internal void UpdateVertices()
        {
            if (transform.childCount < 1)
            {
                Debug.Log("Removing corrupted wall.");
                Destroy(gameObject);
                return;
            }

            Vertices.Clear();

            // Round to nearest millimeter.
            Vector2 vertex1 = new Vector2(
                (float)(Math.Round(transform.GetChild(0).position.x / Consts.GridResolution) * Consts.GridResolution),
                (float)(Math.Round(transform.GetChild(0).position.z / Consts.GridResolution) * Consts.GridResolution));
            Vector2 vertex2 = new Vector2(
               (float)(Math.Round(transform.GetChild(1).position.x / Consts.GridResolution) * Consts.GridResolution),
               (float)(Math.Round(transform.GetChild(1).position.z / Consts.GridResolution) * Consts.GridResolution));

            Vertex v1 = new Vertex(vertex1.x, vertex1.y);
            Vertex v2 = new Vertex(vertex2.x, vertex2.y);

            Vertices.Add(v1);
            Vertices.Add(v2);
        }

        public override void UpdateInfo()
        {
            Apply(ObjectInfo.Instance.ObjectToWall(gameObject), IsBarricade);
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

        internal void UpdateTextureScale(float distance = -1)
        {
            if (distance > 0)
                Length = distance;

            float yTile = transform.localScale.y / Statics.LevelHeight;
            //if (IsBarricade && IsLow) yTile /= 2.5f;

            var meshFilter = GetComponent<MeshFilter>();
            var uvs = meshFilter.mesh.uv;

            for (int i = 0; i < uvs.Length; i++)
                uvs[i] = new Vector2(
                    (uvs[i].x == 0) ? 0 : (Length / 2.5f),
                    (uvs[i].y == 0) ? 0 : yTile);

            meshFilter.mesh.uv = uvs;

            foreach (Transform child in transform)
            {
                var meshChild = child.GetComponent<MeshFilter>();
                var uvsChild = meshChild.mesh.uv;

                for (int i = 0; i < uvsChild.Length; i++)
                    uvsChild[i] = new Vector2(Consts.endPoleUVs[i * 2] * 0.5f, Consts.endPoleUVs[(i * 2) + 1] * yTile);

                meshChild.mesh.uv = uvsChild;
            }
        }

        internal void ApplyMods(Modify mods)
        {
            transform.position = new Vector3(mods.X, transform.position.y, mods.Y);
            transform.rotation = Quaternion.Euler(0, mods.R, 0);
            UpdateLength(mods.L);
            UpdateVertices();
        }

        internal void ToggleTransparency()
        {
            IsTransparent = !IsTransparent;
            Instance.ChangeMaterial(IsTransparent, this, IsBarricade ? (int)Def.Mat.Barricade : (int)Def.Mat.Wall);
        }

        public override void StartClick()
        {
            Prefabs[(int)Prefab.SPole].transform.position =
                    Instance.WallSnapEnabled ? ToLh(ClosestPole(GetWorldPoint(true))) : ToLh(GetWorldPoint(true));
            Instance.MeshRendererStartPole.material = Instance.MaterialsOpaque[(int)Def.Mat.Wall];
            Instance.MeshRendererEndPole.material = Instance.MaterialsOpaque[(int)Def.Mat.Wall];
            transform.localScale = new Vector3(
                transform.localScale.x,
                Statics.LevelHeight,
                transform.localScale.z);
        }

        public override void Adjust()
        {
            base.Adjust();
            endPolePos = Input.GetKey(KeyCode.LeftShift) ? ToLh(ClosestPole(GetWorldPoint(true))) : ToLh(GetWorldPoint(true));
            Prefabs[(int)Prefab.EPole].transform.position = endPolePos;
            Prefabs[(int)Prefab.SPole].transform.LookAt(endPolePos);
            Prefabs[(int)Prefab.EPole].transform.LookAt(startPolePos);
            Instance.Distance = Vector3.Distance(startPolePos, endPolePos);
            transform.position = startPolePos + Instance.Distance / 2 * Prefabs[(int)Prefab.SPole].transform.forward;
            transform.rotation = Prefabs[(int)Prefab.SPole].transform.rotation;
            transform.localScale = new Vector3(transform.localScale.x, Statics.LevelHeight, Instance.Distance);
            UpdateTextureScale(Instance.Distance);
        }

        public override void EndClick()
        {
            if (Instance._selectedObject == Def.Object.Barricade)
                IsLow = Instance.lowBarricade;

            // For poles 0 and 1 (start and end)
            for (int i = 0; i < 2; i++)
            {
                GameObject pole = Instantiate(Instance.Prefabs[i]);
                pole.transform.SetParent(transform);
                pole.tag = IsBarricade ? Str.PolesBarricadeTagString : Str.PolesTagString;
                pole.transform.localScale = new Vector3(pole.transform.localScale.x * (IsBarricade ? 0.99f : 1f), 0.5f, pole.transform.localScale.z * (IsBarricade ? 0.99f : 1f));
            }

            UpdateInfo();

            // If the wall ends on a ticketGate, make the wall end (pole) hidden.
            foreach (var ticketGate in FindObjectsOfType<TicketGateInfo>())
            {
                foreach (var vertex in ticketGate.gate.GetComponent<GateInfo>().Vertices)
                {
                    if (vertex.id == Vertices[0].id)
                        Destroy(transform.GetChild(0).GetComponent<MeshRenderer>());
                    if (vertex.id == Vertices[1].id)
                        Destroy(transform.GetChild(1).GetComponent<MeshRenderer>());
                }
            }

            if (Consts.extendWallColliderToPoles)
            {
                var boxC = GetComponent<BoxCollider>();
                boxC.size = new Vector3(boxC.size.x, boxC.size.y, (Length + 0.5f) / Length);
            }
        }

        internal static WallInfo CreateNew()
        {
            
            var _editingObject = Instantiate(
                Instance.Prefabs[(int)Prefab.Wall],
                Instance.Prefabs[(int)Prefab.SPole].transform.position,
                Quaternion.identity,
                Instance.CurrentLevelTransform());
            return _editingObject.GetComponent<WallInfo>();
        }
    }
}
