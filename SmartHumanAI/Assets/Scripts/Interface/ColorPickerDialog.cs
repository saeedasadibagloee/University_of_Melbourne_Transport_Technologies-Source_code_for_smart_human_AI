using Info;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class ColorPickerDialog : MonoBehaviour
{

    private static ColorPickerDialog _colorPickerDialog;
    private AgentDistInfo agentDistInfo = null;
    private TrainInfo trainInfo = null;
    private IndividualAgent individualAgent = null;
    private Button colorButton = null;
    private Color pickedColor = Color.white;

    public static ColorPickerDialog Instance
    {
        get { return _colorPickerDialog ?? (_colorPickerDialog = FindObjectOfType<ColorPickerDialog>()); }
    }

    public GameObject window;

    internal void PickColor(AgentDistInfo agentDistInfo, Button colorButton)
    {
        // Save these in memory until the user has picked their color.
        this.colorButton = colorButton;
        this.agentDistInfo = agentDistInfo;
        individualAgent = null;
        window.SetActive(true);
    }

    internal void PickColor(TrainInfo trainInfo, Button colorButton)
    {
        // Save these in memory until the user has picked their color.
        this.colorButton = colorButton;
        this.trainInfo = trainInfo;
        individualAgent = null;
        agentDistInfo = null;
        window.SetActive(true);
    }

    internal void PickColor(IndividualAgent individualAgent, Button colorButton)
    {
        // Save these in memory until the user has picked their color.
        this.colorButton = colorButton;
        this.individualAgent = individualAgent;
        agentDistInfo = null;
        window.SetActive(true);
    }

    public void ColorPicked()
    {
        if (colorButton != null && (agentDistInfo != null || individualAgent != null || trainInfo != null))
        {
            pickedColor = GetComponentInChildren<ColorPicker>().pickedColor;
            ApplyColor();
        }

        Close();
    }

    public void RemoveColor()
    {
        if (colorButton != null && (agentDistInfo != null || individualAgent != null || trainInfo != null))
        {
            pickedColor = Color.white;
            ApplyColor();
        }

        Close();
    }

    private void ApplyColor()
    {
        if (agentDistInfo != null)
            // Save the color in the agent distribution.
            agentDistInfo.color = pickedColor;

        if (trainInfo != null)
            // Save the color in the train.
            trainInfo.color = pickedColor;

        if (individualAgent != null)
        {
            // Save the color in the individual agent.
            for (int index = 0; index < individualAgent.colorList.Count; index++)
            {
                individualAgent.colorList[index] = pickedColor;
                individualAgent.UpdateLineColor(pickedColor);
            }
        }

        // Display the color in the UI color button.
        ColorBlock colorBlock = colorButton.colors;
        colorBlock.normalColor = pickedColor;
        colorButton.colors = colorBlock;
    }


    public void Close()
    {
        window.SetActive(false);
        agentDistInfo = null;
        individualAgent = null;
        trainInfo = null;
        colorButton = null;
    }

}
