using System;
using UnityEngine;

namespace DataFormats
{
    [Serializable]
    public class Vertex
    {
        private const double roundingError = 0.00001;

        public float X { get; set; }
        public float Y { get; set; }
        public int id { get; set; }

        public Vertex(float xPos, float yPos, int vID = -1)
        {
            X = (float)(Math.Round(xPos / 0.001) * 0.001);
            Y = (float)(Math.Round(yPos / 0.001) * 0.001);
            id = vID;
        }

        public Vertex(double xPos, double yPos, int vID = -1)
        {
            X = (float)(Math.Round(xPos / 0.001) * 0.001);
            Y = (float)(Math.Round(yPos / 0.001) * 0.001);
            id = vID;
        }

        public Vertex() { }

        public override string ToString()
        {
            return "(" + X.ToString(Str.DecimalFormat) + ", " + Y.ToString(Str.DecimalFormat) + ")";
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
                return false;

            Vertex v = (Vertex)obj;
            if (X.Equals(v.X) && Y.Equals(v.Y))
                return true;

            return (Math.Abs(X - v.X) < roundingError && Math.Abs(Y - v.Y) < roundingError);
        }

        public override int GetHashCode()
        {
            var hx = Math.Round(X * roundingError) / roundingError;
            var hy = Math.Round(Y * roundingError) / roundingError;
            return hx.GetHashCode() - hy.GetHashCode();
        }

        public Vector3 GetVector3()
        {
            return new Vector3(X, 0, Y);
        }

        public Vector2 GetVector2()
        {
            return new Vector2(X, Y);
        }
    }
}