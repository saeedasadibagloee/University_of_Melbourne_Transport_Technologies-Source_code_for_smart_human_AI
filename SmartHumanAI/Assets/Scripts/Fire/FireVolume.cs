using UnityEngine;

namespace Fire
{
    public class FireVolume
    {
        // These values are in decimeters.
        public int XMin;
        public int XMax;
        public int YMin;
        public int YMax;
        public int ZMin;
        public int ZMax;

        public FireVolume() { }

        public FireVolume(int xMin, int xMax, int yMin, int yMax, int zMin, int zMax)
        {
            XMin = xMin;
            XMax = xMax;
            YMin = yMin;
            YMax = yMax;
            ZMin = zMin;
            ZMax = zMax;
        }

        public float XWidth
        {
            get { return (XMax - XMin) / 10f; }
        }

        public float XMiddle
        {
            get { return (XMin + (XCells / 2)) / 10f; }
        }

        public float YWidth
        {
            get { return (YMax - YMin) / 10f; }
        }

        public float YMiddle
        {
            get { return (YMin + (YCells / 2)) / 10f; }
        }

        public float ZWidth
        {
            get { return (ZMax - ZMin) / 10f; }
        }

        public float ZMiddle
        {
            get { return (ZMin + (ZCells / 2)) / 10f; }
        }

        public int XCells
        {
            get { return XMax - XMin; }
        }

        public int YCells
        {
            get { return YMax - YMin; }
        }

        public int ZCells
        {
            get { return ZMax - ZMin; }
        }

        public int NumCells
        {
            get { return (1 + XMax - XMin) * (1 + YMax - YMin) * (1 + ZMax - ZMin); }
        }

        public Vector3 GetCentre()
        {
            return new Vector3((XMax + XMin) / 20f, (ZMax + ZMin) / 20f, (YMax + YMin) / 20f);
        }

        public string GetXB()
        {
            return XMin / 10f + "," +
                   XMax / 10f + "," +
                   YMin / 10f + "," +
                   YMax / 10f + "," +
                   ZMin / 10f + "," +
                   ZMax / 10f;
        }
    }
}
