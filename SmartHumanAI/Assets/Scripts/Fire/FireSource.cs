using UnityEngine;

public class FireSource : MonoBehaviour
{
    public float flowRate = 1.97f;
    public float velocity = 10f;
    
    public string GetXYZ()
    {
        return transform.position.x + "," + transform.position.z + "," + fireHeight;
    }
}
