using Assets.Scripts.PathfindingExtensions;
using Pathfinding;
using UnityEngine;

public class DistanceTester : MonoBehaviour
{
    public Transform EndTransform;
    public Transform StartTransform;

    private Vector3 oldEnd = Vector3.zero;
    private Vector3 oldStart = Vector3.zero;

	void Update () {
	    if (oldEnd != EndTransform.position || oldStart != StartTransform.position)
	    {
	        oldEnd = EndTransform.position;
	        oldStart = StartTransform.position;

	        if (AstarPath.active == null)
	            return;

	        Path p = ABPath.Construct(oldStart, oldEnd, OnPathComplete);
            AstarPath.StartPath(p);
	    }		
	}

    private void OnPathComplete(Path p)
    {
        FunnelModifierSingleton.Instance.Apply(p);
        Debug.Log("opc " + p.GetTotalLength());
    }
}
