using System;
using System.Collections.Generic;
using Info;
using UnityEngine;

namespace DataFormats
{
    [Serializable]
    public class Wall
    {
        public int id { get; set; }
        public float length { get; set; }
        public float angle { get; set; }
        public bool isLow { get; set; }
        public bool isTransparent { get; set; }
        public bool iWlWG { get; set; }
        public List<Vertex> vertices { get; set; }

        public Wall()
        {
            vertices = new List<Vertex>();
        }

        public Wall(Vertex v1, Vertex v2, int id = -1)
        {
            this.id = id < 0 ? ObjectInfo.Instance.ArtifactId++ : id;
            length = Mathf.Sqrt(Mathf.Pow(v2.X - v1.X, 2) + Mathf.Pow(v2.Y - v1.Y, 2));
            vertices = new List<Vertex> { v1, v2 };
        }

        public override string ToString()
        {
            string str = "W(id:" + id + ",len:" + length + ",ang:" + angle;
            foreach (Vertex v in vertices)
            {
                str += " " + v;
            }
            str += ")";
            return str;
        }
    }

    [Serializable]
    public class WallPackage : EventArgs
    {
        public List<Wall> walls { get; set; }

        public WallPackage()
        {
            walls = new List<Wall>();
        }

        public override string ToString()
        {
            string str = "";
            foreach (Wall w in walls)
            {
                str += w.ToString();
            }
            return str;
        }
    }

    [Serializable]
    public class Modify
    {
        public int id = -1;
        public float X = -1f;
        public float Y = -1f;
        public float L = -1f;
        public float R = -1f;
    }
}