using System;
using Core;
using DataFormats;
using Pathfinding;
using UnityEngine;

public class DangerArea : MonoBehaviour
{
    public int WeightModifier = Consts.DangerWeightModifier;
    public Def.DangerArea dangerArea = Def.DangerArea.Unknown;

    public Vector2 min = Vector3.zero;
    public Vector2 max = Vector3.zero;

    public void ApplyPenalty()
    {
        foreach (NavGraph navGraph in AstarPath.active.data.graphs)
        {
            GridGraph gg = (GridGraph)navGraph;
            foreach (GridNode node in gg.nodes)
            {
                bool nodeWithin = (dangerArea == Def.DangerArea.Circle) ? WithinCircle(node) : WithinSquare(node);
                if (nodeWithin)
                    node.Penalty += (uint)WeightModifier;
            }
        }
    }

    private bool WithinCircle(GridNode node)
    {
        float radius = transform.localScale.x / 2;
        Vector3 pos = (Vector3)node.position;

        return Utils.DistanceBetween(pos.x, pos.z, transform.position.x, transform.position.z) < radius;
    }

    private bool WithinSquare(GridNode node)
    {

    }

    internal void ApplyPoles(Vector3 position1, Vector3 position2)
    {
        min = new Vector2(Mathf.Min(position1.x, position2.x), Mathf.Min(position1.z, position2.z));
        max = new Vector2(Mathf.Max(position1.x, position2.x), Mathf.Max(position1.z, position2.z));
    }
}