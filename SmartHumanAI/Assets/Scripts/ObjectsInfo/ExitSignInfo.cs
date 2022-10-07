using Info;
using UnityEngine;
using Assets.Scripts.ObjectsInfo;

public class ExitSignInfo : BaseObject
{
    public int Id = -1;
    public float Angle = -1f;

    public override void EndClick()
    {
        Id = ObjectInfo.Instance.ArtifactId++;
        Angle = transform.eulerAngles.y;

        
    }
}
