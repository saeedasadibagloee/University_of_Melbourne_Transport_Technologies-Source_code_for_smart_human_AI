using System;
using System.Collections.Generic;

namespace DataFormats
{
    [Serializable]
    public class BarricadePackage : EventArgs
    {
        public List<Wall> barricade_walls { get; set; }

        public BarricadePackage()
        {
            barricade_walls = new List<Wall>();
        }
    }
}