using System;
using System.Collections.Generic;

namespace DataFormats
{
    [Serializable]
    public class CircularObstacle
    {
        public float XPosition { get; set; }
        public float YPosition { get; set; }
        public float Radius { get; set; }
        public int Weight = -1;
    }

    [Serializable]
    public class CircularObstaclePackage : EventArgs
    {
        public List<CircularObstacle> Obstacles { get; set; }

        public CircularObstaclePackage()
        {
            Obstacles = new List<CircularObstacle>();
        }
    }
}