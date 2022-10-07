using DataFormats;
using Helper;
using Info;
using netDxf;
using netDxf.Header;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace InputOutput
{
    internal class DxfReader
    {
        /// <param name="file">Location of file.</param>
        /// <param name="type">0: Wall, 1: Barricade, 2: Low Barricade</param>
        internal static void Read(string file, int type = 0)
        {
            // check if the dxf actually exists
            FileInfo fileInfo = new FileInfo(file);

           

            foreach (var line in dxf.LwPolylines)
            {
                for (int i = 0; i < line.Vertexes.Count - 1; i++)
                {
                    Wall w = new Wall(
                        new Vertex(line.Vertexes[i].Position.X, line.Vertexes[i].Position.Y),
                        new Vertex(line.Vertexes[i + 1].Position.X, line.Vertexes[i + 1].Position.Y));
                    w.isLow = type == 2;
                    Create.Instance.WallToObject(w, type != 0);
                }
            }

            foreach (var line in dxf.Lines)
            {
                Wall w = new Wall(
                    new Vertex(line.StartPoint.X, line.StartPoint.Y),
                    new Vertex(line.EndPoint.X, line.EndPoint.Y));
                w.isLow = type == 2;
                Create.Instance.WallToObject(w, type != 0);
            }

            Create.Instance.ResetNextFrame();
            SimulationController.Instance.ClearHeatMap();
            SimulationController.Instance.CancelSimulation();
        }

        private static void FindBoundaries(DxfDocument dxf, ref float xMax, ref float yMax)
        {
            foreach (var line in dxf.LwPolylines)
            {
                foreach (var vertex in line.Vertexes)
                {
                    if (vertex.Position.X > xMax)
                        xMax = (float)vertex.Position.X;
                    if (vertex.Position.Y > yMax)
                        yMax = (float)vertex.Position.Y;
                }
            }
            foreach (var line in dxf.Lines)
            {
                if (line.StartPoint.X > xMax)
                    xMax = (float)line.StartPoint.X;
                if (line.StartPoint.Y > yMax)
                    yMax = (float)line.StartPoint.Y;

                if (line.EndPoint.X > xMax)
                    xMax = (float)line.EndPoint.X;
                if (line.EndPoint.Y > yMax)
                    yMax = (float)line.EndPoint.Y;
            }
        }
    }
}