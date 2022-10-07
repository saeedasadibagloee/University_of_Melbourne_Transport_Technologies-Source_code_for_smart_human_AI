using System;
using DataFormats;
using UnityEngine;

namespace Info
{
    public class ThreatInfo : MonoBehaviour
    {
        public Def.ThreatType ThreatType = Def.ThreatType.Unknown;

        public int ThreatId = -1;
        public int ElementId = -1;

        public float X = -1f;
        public float Y = -1f;

        public int LevelId = -1;

        public int StartTime = 0;
        public int Duration = -1;

        public string DurationString
        {
            get
            {
                return Duration < 0 ? "∞" : Duration.ToString();
            }
        }

        protected internal void Copy(ThreatInfo ti)
        {
            Copy(ti.Threat);
        }

        protected internal void Copy(Threat ti)
        {
            ThreatType = (Def.ThreatType)ti.ThreatType;
            ThreatId = ti.ThreatId;
            LevelId = ti.LevelId;
            StartTime = ti.StartTime;
            Duration = ti.Duration;
            ElementId = ti.ElementId;
            X = ti.X;
            Y = ti.Y;
        }

        protected internal Threat Threat
        {
            get
            {
                return new Threat
          
                    X = X,
                    Y = Y
                };
            }
        }

        public void UpdateStatus(bool isActive)
        {
            RaycastHit hit;
            Ray ray;

            switch (ThreatType)
            {
                case Def.ThreatType.GateObstruction:

                    foreach (var gate in FindObjectsOfType<GateInfo>())
                        if (gate.LevelId == LevelId && gate.Id == ElementId)
                            gate.GetComponent<MeshRenderer>().material =
                                    Create.Instance.MaterialsOpaque[(int)(isActive ? Def.Mat.Wall :
                                        gate.IsDestination ? Def.Mat.GateDestination : Def.Mat.Gate)];

                    foreach (var meshRenderer in transform.GetComponentsInChildren<MeshRenderer>())
                        meshRenderer.enabled = !isActive;

                    break;

                case Def.ThreatType.DangerInRoom:
                    ray = new Ray(transform.position, Vector3.down);
                    Physics.Raycast(ray, out hit);
                    hit.transform.GetComponent<MeshRenderer>().material =
                        Create.Instance.MaterialsOpaque[(int)(isActive ? Def.Mat.FloorRed : Def.Mat.Floor)];
                    break;

                case Def.ThreatType.StairObstruction:

                    foreach (var stair in FindObjectsOfType<StairInfo>())
                    {
                        if (stair.StairId == ElementId)
                        {
                            for (int i = 0; i < stair.transform.childCount; i++)
                            {
                                Transform t = stair.transform.GetChild(i);

                                switch (t.name.Split('_')[0])
                                {
                                    case Str.Landing:
                                        t.GetComponent<MeshRenderer>().material =
                                            Create.Instance.MaterialsOpaque[(int)(isActive ? Def.Mat.FloorRed : Def.Mat.Floor)];
                                        break;
                                    case Str.Stairs1:
                                    case Str.Stairs2:
                                        t.GetComponent<MeshRenderer>().material =
                                            Create.Instance.MaterialsOpaque[(int)(isActive ? Def.Mat.FloorRed :
                                            (stair.stairType == Def.StairType.HalfLanding) ? Def.Mat.StairsHalfLanding : Def.Mat.StairsStraight)];
                                        break;
                                }
                            }
                        }
                    }




                    foreach (var meshRenderer in transform.GetComponentsInChildren<MeshRenderer>())
                        meshRenderer.enabled = !isActive;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [Serializable]
    public class Threat
    {
        public int ThreatType = (int)Def.ThreatType.Unknown;

        public float X = -1f;
        public float Y = -1f;

        public int LevelId = -1;
        public int ThreatId = -1;
        public int ElementId = -1;

        public int StartTime = 0;
        public int Duration = -1;
    }
}