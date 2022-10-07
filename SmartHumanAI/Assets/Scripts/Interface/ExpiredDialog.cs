using UnityEngine;
using UnityEngine.UI;

public class ExpiredDialog : MonoBehaviour
{
    public Text MessageText;
    public Text HostIdText;
    
    
    public void SetErrorInfo(string errorInfo)
    {
        MessageText.text = "(" + errorInfo + ")";
    }

    public void SetHostId(string hostID)
    {
        HostIdText.text = hostID;
    }
}
