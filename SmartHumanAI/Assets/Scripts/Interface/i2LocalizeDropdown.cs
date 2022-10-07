using I2.Loc;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Dropdown))]
public class i2LocalizeDropdown : MonoBehaviour
{
    public List<string> DropdownTerms = new List<string>();

    public void LocalizeDropDown()
    {
        Dropdown dropdown = GetComponent<Dropdown>();
        if (dropdown == null)
            return;

        var newOptionData = dropdown.options;
        for (int i = 0; i < DropdownTerms.Count; i++)
        {
            if (newOptionData[i] != null)
                newOptionData[i].text = LocalizationManager.GetTermTranslation(DropdownTerms[i]);
        }
        dropdown.options = newOptionData;
    }
}
