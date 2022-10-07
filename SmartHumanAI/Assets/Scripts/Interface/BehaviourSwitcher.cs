using System.Collections;
using System.Collections.Generic;
using Core;
using DataFormats;
using I2.Loc;
using UnityEngine;
using UnityEngine.UI;

public class BehaviourSwitcher : MonoBehaviour
{
    public Text buttonText;

    private bool panicMode = false;

    private readonly Color panicNormal = new Color(1, 210 / 255f, 210 / 255f);
    private readonly Color panicHighlighted = new Color(1, 180 / 255f, 180 / 255f);
    private readonly Color panicPressed = new Color(120 / 255f, 1, 120 / 255f);
    private readonly Color normalNormal = new Color(210 / 255f, 1, 210 / 255f);
    private readonly Color normalHighlighted = new Color(180 / 255f, 1, 180 / 255f);
    private readonly Color normalPressed = new Color(1, 120 / 255f, 120 / 255f);

    public void Start()
    {
        ApplyMode();
        UIController.Instance.ChangeMode(panicMode);
    }

    public void SwitchMode()
    {

    public void ApplyMode()
    {
        Button btn = GetComponent<Button>();
        var colors = btn.colors;

        if (panicMode)
        {
            buttonText.text = LocalizationManager.GetTermTranslation("Emergency Behaviour");
            colors.normalColor = panicNormal;
            colors.highlightedColor = panicHighlighted;
            colors.pressedColor = panicPressed;
        }
        else
        {
            buttonText.text = LocalizationManager.GetTermTranslation("Normal Behaviour");
            colors.normalColor = normalNormal;
            colors.highlightedColor = normalHighlighted;
            colors.pressedColor = normalPressed;
        }
        btn.colors = colors;
    }

    public bool GetMode()
    {
        return panicMode;
    }
}
