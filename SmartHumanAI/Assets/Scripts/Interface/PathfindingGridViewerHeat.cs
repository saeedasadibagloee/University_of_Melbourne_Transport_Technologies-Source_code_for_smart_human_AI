using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DataFormats;
using Pathfinding;
using UnityEngine;

public class PathfindingGridViewerHeat : MonoBehaviour
{
    private static PathfindingGridViewerHeat _instance = null;
    public float MaxHeat = 6f;
    public List<GameObject> heatmapObjects = new List<GameObject>();

    public static PathfindingGridViewerHeat Instance
    {
        get { return _instance ?? (_instance = FindObjectOfType<PathfindingGridViewerHeat>()); }
    }

    private bool _isDisplayed = false;

    public void Update()
    {
        if (_isDisplayed)
            DrawHeatmaps();
    }

    public void EnableView()
    {
        _isDisplayed = true;
    }

    private void DrawHeatmaps()
    {
        bool smooth = true;

        ClearHeatMaps();

        int maxSize = Util.GetMaxSize();

        List<float[,]> heatmap = new List<float[,]>();

        for (int level = 0; level < UIController.Instance.NumLevels; level++)
        {
            heatmap.Add(new float[maxSize, maxSize]);

            foreach (GridNode node in ((GridGraph)AstarPath.active.data.graphs[level]).nodes)
            {
                if (node.Walkable)
                    heatmap[level][node.XCoordinateInGrid, node.ZCoordinateInGrid] = smooth ? node.currentUtilizationSmooth : node.currentUtilization;
                else
                    heatmap[level][node.XCoordinateInGrid, node.ZCoordinateInGrid] = -1;
            }
        }

        int xMin = int.MaxValue;
        int yMin = int.MaxValue;
        int xMax = int.MinValue;
        int yMax = int.MinValue;

        for (int level = 0; level < UIController.Instance.NumLevels; level++)
        {
            for (int i = 0; i < maxSize; i++)
            {
                for (int k = 0; k < maxSize; k++)
                {
                    float value = heatmap[level][i, k];

                    if (!(value >= 0)) continue;
                    if (i < xMin)
                        xMin = i;
                    if (i > xMax)
                        xMax = i;
                    if (k < yMin)
                        yMin = k;
                    if (k > yMax)
                        yMax = k;
                    //if (value > MaxHeat) MaxHeat = value;
                }
            }
        }

        int width = 1 + xMax - xMin;
        int height = 1 + yMax - yMin;

        List<Texture2D> textures = new List<Texture2D>();

        for (int level = 0; level < UIController.Instance.NumLevels; level++)
        {
            textures.Add(new Texture2D(width, height, TextureFormat.RGBA32, false));
            textures[level].wrapMode = TextureWrapMode.Clamp;

            for (int i = xMin; i <= xMax; i++)
            {
                for (int k = yMin; k <= yMax; k++)
                {
                    if (heatmap[level][i, k] < 0)
                    {
                        textures[level].SetPixel(i - xMin, k - yMin, new Color(0, 0, 0, 0));
                    }
                    else
                    {
                        float lerp = MaxHeat != 0 ? heatmap[level][i, k] / MaxHeat : 0;
                        textures[level].SetPixel(i - xMin, k - yMin, Util.LerpToColor(lerp, Def.Colors));
                    }
                }
            }

            textures[level].Apply();
        }

        int l = 0;

        foreach (var heatmapTex in textures)
        {
            GameObject heatmapObject = Instantiate(SimulationController.Instance.heatmapPrefab);
            heatmapObject.transform.localScale = new Vector3(heatmapTex.width / 20f, 1f, heatmapTex.height / 20f);
            heatmapObject.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
            heatmapObject.transform.position = Create.Instance.ToFh(new Vector3(
                xMin / 2f + heatmapTex.width / 4f - 0.25f,
                -0.9f,
                yMin / 2f + heatmapTex.height / 4f - 0.25f), l) + new Vector3(0, 0.1f, 0);

            heatmapObject.GetComponent<MeshRenderer>().material.mainTexture = heatmapTex;

            heatmapObjects.Add(heatmapObject);

            l++;
        }

        SimulationController.Instance.ShowGradientLegend(MaxHeat, "Density (p/m²)");
    }

    internal void ClearHeatMaps()
    {
        foreach (var heatmap in heatmapObjects)
            Destroy(heatmap);
        heatmapObjects.Clear();
    }
}