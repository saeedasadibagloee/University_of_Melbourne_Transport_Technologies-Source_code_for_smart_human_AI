using UnityEngine;
using UnityEngine.UI;

public class AdvancedDropDown : MonoBehaviour
{
    public int GetValue()
    {
        return GetComponent<Dropdown>().value;
    }
}