using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DataFormats;
using Pathfinding;
using UnityEngine;

public class PathfindingGridViewerSquare : MonoBehaviour
{
    private static PathfindingGridViewerSquare _instance = null;
    public GameObject gridSquare;
    public Dictionary<Vector3, GameObject> gridSquares = new Dictionary<Vector3, GameObject>();

    private static float maxPenalty = 8000f;

    public static PathfindingGridViewerSquare Instance
    {
        get { return _instance ?? (_instance = FindObjectOfType<PathfindingGridViewerSquare>()); }
    }

    private bool _isDisplayed = false;

    public void Update()
    {
        if (_isDisplayed)
            DrawGrid();
    }

    public static void ResetMaxPenalty()
    {
        maxPenalty = 6000f;
    }

    public void EnableView()
    {
        DestroyGrid();
        _isDisplayed = true;
        DrawGrid();
    }

    public void ToggleDisplayGrid()
    {
        if (SimulationController.Instance.SimState == (int)Def.SimulationState.Started)
        {
            UIController.Instance.ShowGeneralDialog("Please wait until the simulation has completed.", "Simulation Processing");
            return;
        }

        _isDisplayed = !_isDisplayed;

        if (_isDisplayed)
            DrawGrid();
        else
            DestroyGrid();
    }

    private void DrawGrid()
    {
        foreach (var navGraph in AstarPath.active.graphs)
        {
            var gridGraph = (GridGraph)navGraph;

            if (gridGraph == null || gridGraph.nodes == null)
                continue;

            foreach (GridNode node in gridGraph.nodes)
            {
                if (node.Walkable)
                {
                    if (node.Penalty > maxPenalty)
                        maxPenalty = node.Penalty;
                    DrawSquare(((Vector3)node.position) + new Vector3(0, 0.2f, 0),
                        node.Penalty == 0 ? Def.ColorsGrid[0] : Util.LerpToColor(node.Penalty / maxPenalty, Def.ColorsGrid, true));
                }
            }
        }
    }

    private void DestroyGrid()
    {
        for (int i = transform.childCount - 1; i > -1; i--)
            Destroy(transform.GetChild(i).gameObject);

        gridSquares.Clear();
    }

    private void DrawSquare(Vector3 position, Color color)
    {
        if (gridSquare != null)
        {
            if (gridSquares.ContainsKey(position) && gridSquares[position] != null)
                gridSquares[position].GetComponent<MeshRenderer>().material.color = new Color(color.r, color.g, color.b, 0.45f);
            else
            {
                GameObject square = Instantiate(gridSquare, transform);
                square.transform.position = position;
                square.GetComponent<MeshRenderer>().material.color = new Color(color.r, color.g, color.b, 0.45f);
                gridSquares.Add(position, square);
            }
        }
        return;
    }
}