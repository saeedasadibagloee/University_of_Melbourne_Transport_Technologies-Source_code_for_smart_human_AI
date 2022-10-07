using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReplaceFonts : MonoBehaviour
{
    public Font bold;
    public Font reg;
    public Font light;

    public void ReplaceAll()
    {
        //Debug.Log("Replacing Fonts");

        if (bold == null || reg == null || light == null)
            return;

        foreach (Text text in FindObjectsOfType<Text>())
        {
            text.fontStyle = FontStyle.Normal;

            switch (text.font.ToString())
            {
                case "OpenSans-Regular (UnityEngine.Font)":
                    text.font = reg;
                    break;
                case "OpenSans-Bold (UnityEngine.Font)":
                    text.font = bold;
                    break;
                case "OpenSans-Italic (UnityEngine.Font)":
                    text.font = reg;
                    break;
                case "OpenSans-Light (UnityEngine.Font)":
                    text.font = light;
                    break;
                case "Arial (UnityEngine.Font)":
                    text.font = reg;
                    break;

            }
        }
    }
}
