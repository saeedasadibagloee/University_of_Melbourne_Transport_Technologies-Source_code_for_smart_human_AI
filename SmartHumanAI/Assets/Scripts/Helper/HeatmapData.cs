using System;
using System.Collections.Generic;
using UnityEngine;

namespace Helper
{
    public struct HeatmapData
    {
        public List<Texture2D> Tex;
        public int XMin;
        public int YMin;
        public int width;
        public int height;
    }

    public class HeatmapDataH
    {
        public List<string> Tex; // Raw texture data stores as a base64 string.
        public int XMin;
        public int YMin;
        public int width;
        public int height;

        public HeatmapDataH() { }

        public HeatmapDataH(HeatmapData hmd)
        {
            XMin = hmd.XMin;
            YMin = hmd.YMin;
            width = hmd.width;
            height = hmd.height;

            Tex = new List<string>();

            foreach (var tx2d in hmd.Tex)
                Tex.Add(Convert.ToBase64String(tx2d.GetRawTextureData()));
        }
    }
}
