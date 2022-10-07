using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DataFormats;
using Pathfinding;
using UnityEngine;

public class PathfindingGridViewer : MonoBehaviour
{
    private const float lineWidth = 0.05f;
    private static PathfindingGridViewer _instance = null;
    private readonly List<NodeConnection> nodesConnections = new List<NodeConnection>();
    private Dictionary<Vector3, List<LineRenderer>> linesStart = new Dictionary<Vector3, List<LineRenderer>>();
    private Dictionary<Vector3, List<LineRenderer>> linesEnd = new Dictionary<Vector3, List<LineRenderer>>();
    public Shader particleShader;

    public static PathfindingGridViewer Instance
    {
        get { return _instance ?? (_instance = FindObjectOfType<PathfindingGridViewer>()); }
    }

   
                        if (!AlreadyExists(nodesConnections, nodeConnection))
                            nodesConnections.Add(nodeConnection);
                        else
                            connectionDuplicates++;
                    });
                }
            }

            if (!atLeastOneGraph)
            {
                _isDisplayed = false;
                return;
            }


            Debug.Log(connectionDuplicates + " Connection Duplicate(s).");

            DestroyGrid();

            foreach (var nodeConnection in nodesConnections)
            {
                if (!linesStart.ContainsKey(nodeConnection.start))
                    linesStart.Add(nodeConnection.start, new List<LineRenderer>());

                if (!linesEnd.ContainsKey(nodeConnection.end))
                    linesEnd.Add(nodeConnection.end, new List<LineRenderer>());

                var lineRenderer = DrawLine(nodeConnection.start, nodeConnection.end, Color.green);

                linesStart[nodeConnection.start].Add(lineRenderer);
                linesEnd[nodeConnection.end].Add(lineRenderer);
            }

        }
        else
        {
            DestroyGrid();
        }
    }

    private void DestroyGrid()
    {
        for (int i = transform.childCount - 1; i > -1; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    private void GenerateMesh()
    {
        int startIndex = 0;
        int endIndex = nodesConnections.Count;

        var vertices = new List<Vector3>();
        var colors = new List<Color32>();
        var normals = new List<Vector3>();
        var uv = new List<Vector2>();
        var tris = new List<int>();

        foreach (var nodeConnection in nodesConnections)
        {
          
    }

    private LineRenderer DrawLine(Vector3 start, Vector3 end, Color color)
    {
        GameObject myLine = new GameObject("Line");
        myLine.transform.position = start;
        myLine.transform.parent = transform;
        LineRenderer lr = myLine.AddComponent<LineRenderer>();
        lr.material = new Material(particleShader);
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        return lr;
    }

    private static bool AlreadyExists(List<NodeConnection> nodeConnections, NodeConnection nodeConnection)
    {
        foreach (var node in nodeConnections)
        {
            if (node.start == nodeConnection.start &&
                node.end == nodeConnection.end)
                return true;

            if (node.end == nodeConnection.start &&
                node.start == nodeConnection.end)
                return true;
        }

        return false;
    }
}

public class NodeConnection
{
    public Vector3 start = Vector3.zero;
    public Vector3 end = Vector3.zero;

    public NodeConnection(Vector3 position1, Vector3 position2)
    {
        start = position1;
        end = position2;
    }

    public NodeConnection() { }
}