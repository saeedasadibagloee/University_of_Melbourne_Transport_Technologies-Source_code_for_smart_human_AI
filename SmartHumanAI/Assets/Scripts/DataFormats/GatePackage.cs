using System;
using System.Collections.Generic;
using System.Linq;
using Domain;
using Info;
using UnityEngine;

namespace DataFormats
{
    [Serializable]
    public class Gate
    {
        public int id { get; set; }
        public float length { get; set; }
        public float angle { get; set; }
        public bool destination { get; set; }
        public bool counter { get; set; }
        public bool transparent { get; set; }
        public bool designatedOnly { get; set; }
        public List<Vertex> vertices { get; set; }

        public WaitingData waitingData = null;

        public TrainDoorData trainData = null;

        public Gate()
        {
            vertices = new List<Vertex>();
        }

        public Gate(Vertex v1, Vertex v2, int id = -1)
        {
            this.id = id > 0 ? ObjectInfo.Instance.ArtifactId++ : id;
            length = Mathf.Sqrt(Mathf.Pow(v2.X - v1.X, 2) + Mathf.Pow(v2.Y - v1.Y, 2));
            vertices = new List<Vertex> { v1, v2 };
        }

        public Vertex Middle
        {
            get
            {
                return new Vertex((vertices[0].X + vertices[1].X) / 2, (vertices[0].Y + vertices[1].Y) / 2);
            }
        }
    }

    [Serializable]
    public class WaitingData
    {
        public float waitTime { get; set; }
        public bool isBidirectional { get; set; }

        public float waitPosX { get; set; }
        public float waitPosY { get; set; }
        public float targetPosX { get; set; }
        public float targetPosY { get; set; }

        public float waitPos2X { get; set; }
        public float waitPos2Y { get; set; }
        public float targetPos2X { get; set; }
        public float targetPos2Y { get; set; }
    }

    [Serializable]
    public class TrainDoorData
    {
        public int trainID = -1;
        public List<Vector3s> waitingPositions = new List<Vector3s>();
    }

    [Serializable]
    public class GatePackage : EventArgs
    {
        public List<Gate> gates { get; set; }

        public GatePackage()
        {
            gates = new List<Gate>();
        }

        public Gate FindGate(int id)
        {
            return gates.FirstOrDefault(gate => gate.id == id);
        }
    }
}