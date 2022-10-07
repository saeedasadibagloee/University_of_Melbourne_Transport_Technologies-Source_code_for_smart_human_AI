using DataFormats;
using Info;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.ObjectsInfo;

public class BenchInfo : BaseObject
{
    public int Id = -1;
    public float Angle = -1f;
    public List<Vertex> SitPoints = new List<Vertex>();

    public override void EndClick()
    {
        Id = ObjectInfo.Instance.ArtifactId++;
        Angle = transform.eulerAngles.y;
    }

    public override Vector3 PositionOffset()
    {
        return new Vector3(0f, -1.2f, 0f);
    }
    
    public override void StartClick()
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateInfo()
    {
        Id = ObjectInfo.Instance.ArtifactId++;
    }
}
