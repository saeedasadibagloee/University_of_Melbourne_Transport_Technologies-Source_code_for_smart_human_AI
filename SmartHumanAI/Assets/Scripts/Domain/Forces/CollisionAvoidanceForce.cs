using System;
using System.Collections.Generic;
using Core;

namespace Domain.Forces
{
    internal class Vector3D
    {
        public float X;
        public float Y;
        public float Z;
        internal static readonly Vector3D Zero = new Vector3D(0, 0, 0);

        public Vector3D()
        {
            X = Y = Z = 0;
        }

        public Vector3D(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3D CrossMultiply(Vector3D p)
        {
            float x = Y * p.Z - p.Y * Z;
            float y = (X * p.Z - p.X * Z) * -1;
            float z = X * p.Y - p.X * Y;

            return new Vector3D(x, y, z);
        }

     
                line.Add(numerator.Y.ToString(floatFormat));
                line.Add(numerator.Z.ToString(floatFormat));
                line.Add(denominator.ToString(floatFormat));
                line.Add(tj.X.ToString(floatFormat));
                line.Add(tj.Y.ToString(floatFormat));
                line.Add(dotProduct.ToString(floatFormat));
                line.Add(finalAvoidForceVector.X.ToString(floatFormat));
                line.Add(finalAvoidForceVector.Y.ToString(floatFormat));
                LogToCsvCollisionAvoidance.WriteLine(line);
            }

            x *= Constants.DefaultAgentCollavoidWeight;
            y *= Constants.DefaultAgentCollavoidWeight;

            return testAgents.Count > 0;
        }
    }

    */

}
