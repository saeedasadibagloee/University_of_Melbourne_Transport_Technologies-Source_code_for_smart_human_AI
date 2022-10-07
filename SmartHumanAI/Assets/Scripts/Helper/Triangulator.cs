using System.Collections.Generic;
using UnityEngine;

namespace Helper
{
    public static class Triangulator
    {
        private static List<Vector2> _mPoints;

        /// <summary>
        /// Returns a list of triangles
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static int[] Triangulate(Vector2[] points)
        {
            _mPoints = new List<Vector2>(points);
            List<int> indices = new List<int>();

            int n = _mPoints.Count;
            if (n < 3)
                return indices.ToArray();

            int[] ve = new int[n];
            if (Area() > 0)
            {
                for (int v = 0; v < n; v++)
                {
                    ve[v] = v;
                }
            }
            else
            {
                for (int v = 0; v < n; v++)
                {
                    ve[v] = n - 1 - v;
                }
            }

            int nv = n;
            int count = 2 * nv;
            int m = 0;
            for (int v = nv - 1; nv > 2;)
            {
                if (count-- <= 0)
                    return indices.ToArray();
                int u = v;
                if (nv <= u)
                    u = 0;
                v = u + 1;
                if (nv <= v)
                    v = 0;
                int w = v + 1;
                if (nv <= w)
                    w = 0;

                if (!Snip(u, v, w, nv, ve)) continue;

                int t;
                int a = ve[u];
                int b = ve[v];
                int c = ve[w];
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
                m++;
                int s = v;
                for (t = v + 1; t < nv; t++)
                {
                    ve[s] = ve[t];
                    s++;
                }
                nv--;
                count = 2 * nv;
            }
            indices.Reverse();
            return indices.ToArray();
        }

        private static bool Snip(int u, int v, int w, int n, int[] ve)
        {
            int p;
            Vector2 a = _mPoints[ve[u]];
            Vector2 b = _mPoints[ve[v]];
            Vector2 c = _mPoints[ve[w]];
            if (Mathf.Epsilon > (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x))
                return false;
            for (p = 0; p < n; p++)
            {
                if (p == u || p == v || p == w)
                    continue;
                Vector2 P = _mPoints[ve[p]];
                if (InsideTriangle(a, b, c, P))
                    return false;
            }
            return true;
        }

        private static bool InsideTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            float ax = c.x - b.x; float ay = c.y - b.y;
            float bx = a.x - c.x; float @by = a.y - c.y;
            float cx = b.x - a.x; float cy = b.y - a.y;
            float apx = p.x - a.x; float apy = p.y - a.y;
            float bpx = p.x - b.x; float bpy = p.y - b.y;
            float cpx = p.x - c.x; float cpy = p.y - c.y;

            float aCrosSbp = ax * bpy - ay * bpx;
            float cCrosSap = cx * apy - cy * apx;
            float bCrosScp = bx * cpy - @by * cpx;

            return aCrosSbp >= 0.0 && bCrosScp >= 0.0 && cCrosSap >= 0.0;
        }

        public static float Area()
        {
            int n = _mPoints.Count;
            float a = 0;
            int q = 0;
            for (int p = n - 1; q < n; p = q++)
            {
                Vector2 pval = _mPoints[p];
                Vector2 qval = _mPoints[q];
                a += pval.x * qval.y - qval.x * pval.y;
            }
            return a * 0.5f;
        }
    }
}