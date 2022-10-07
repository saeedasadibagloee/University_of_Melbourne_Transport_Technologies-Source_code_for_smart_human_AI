using UnityEngine;

namespace Helper
{
    public static class LineInterpolator
    {
        public static float Alpha = 0.5f; //Set from 0-1, default 0.5

        /// <summary>
        /// Takes an input path and returns a point a percentage along the way, Catmul Rom style :)
        /// </summary>
        /// <param name="path">Input path</param>
        /// <param name="percentage">Percentage along the path.</param>
        internal static Vector3 CatmulRom(Vector3[] path, float percentage)
        {
            if (percentage >= 1f)
            {
                return path[path.Length - 1];
            }

            float targetLen = (path.Length - 1) * percentage;
            int arrayIndex = Mathf.FloorToInt(targetLen);
            float percBetweenPoints = targetLen - arrayIndex;

            Vector3 a0;
            Vector3 a1 = path[arrayIndex];
            Vector3 a2 = path[arrayIndex + 1];
            Vector3 a3;

            if (arrayIndex == 0)
                a0 = a1 + (a1 - a2);
            else
                a0 = path[arrayIndex - 1];

            if (arrayIndex >= path.Length - 3)
                a3 = a2 + (a2 - a1);
            else
                a3 = path[arrayIndex + 2];

            Vector2 p0 = new Vector2(a0.x, a0.z);
            Vector2 p1 = new Vector2(a1.x, a1.z);
            Vector2 p2 = new Vector2(a2.x, a2.z);
            Vector2 p3 = new Vector2(a3.x, a3.z);

            const float t0 = 0.0f;
            float t1 = GetT(t0, p0, p1);
            float t2 = GetT(t1, p1, p2);
            float t3 = GetT(t2, p2, p3);

            float t = (t2 - t1) * percBetweenPoints + t1;

            Vector2 A1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
            Vector2 A2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
            Vector2 A3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;

            Vector2 b1 = (t2 - t) / (t2 - t0) * A1 + (t - t0) / (t2 - t0) * A2;
            Vector2 b2 = (t3 - t) / (t3 - t1) * A2 + (t - t1) / (t3 - t1) * A3;

            Vector2 c = (t2 - t) / (t2 - t1) * b1 + (t - t1) / (t2 - t1) * b2;

            return new Vector3(c.x, -0.8f, c.y);
        }

        internal static Vector3 Linear(Vector3[] path, float percentage)
        {
            if (percentage >= 1)
            {
                return path[path.Length - 1];
            }
            if (percentage <= 0)
            {
                return path[0];
            }
            float targetLen = (path.Length - 1) * percentage;
            int arrayIndex = Mathf.FloorToInt(targetLen);
            float percAlongLine = targetLen - arrayIndex;
            Vector3 position = Vector3.Lerp(path[arrayIndex], path[arrayIndex + 1], percAlongLine);
            return position;
        }

        private static float GetT(float t, Vector2 p0, Vector2 p1)
        {
            float x = p1.x - p0.x;
            float y = p1.y - p0.y;
            float a = x * x + y * y;
            float b = Mathf.Sqrt(a);
            float c = Mathf.Sqrt(b);
            //float c = Mathf.Pow(b, alpha);

            return c + t;
        }

        /// <summary>
        /// Basically it puts extra points before and after every positions, so the lines between them will be rendered correctly.
        /// See http://answers.unity3d.com/questions/909428/linerenderer-end-width-bug.html
        /// </summary>
        /// <param name="original"></param>
        /// <returns>New fixed Vector3 array.</returns>
        public static Vector3[] FixLineWithExtraPoints(Vector3[] original)
        {
            Vector3[] res = new Vector3[original.Length * 3 - 2];
            for (int i = 0; i < res.Length; i++)
            {
                switch (i % 3)
                {
                    case 0:
                        res[i] = original[i / 3];
                        break;
                    case 1:
                        res[i] = Vector3.Lerp(original[(i - 1) / 3], original[(i + 2) / 3], 0.0001f);
                        break;
                    case 2:
                        res[i] = Vector3.Lerp(original[(i + 1) / 3], original[(i - 2) / 3], 0.0001f);
                        break;
                }
            }
            return res;
        }

    }
}