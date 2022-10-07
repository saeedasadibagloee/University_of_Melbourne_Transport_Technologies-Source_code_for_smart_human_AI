using DataFormats;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StairTaskbar : MonoBehaviour
{
    public List<GameObject> halfWinderOnlyItems = new List<GameObject>();
    public InputField fieldSlope;
    public InputField fieldLength;
    public InputField fieldWidth;
    public InputField fieldLandingWidth;
    public InputField fieldFloors;

    private float currentLevelHeight = Statics.LevelHeight;

    private Def.StairType currentType = Def.StairType.Straight;

    public void UpdateStairType(Def.StairType stairType)
    {
        currentType = stairType;

        foreach (var item in halfWinderOnlyItems)
        {
            item.SetActive(currentType == Def.StairType.HalfLanding);
        }

        switch (currentType)
        {
            case Def.StairType.Straight:
                Consts.StairLength = Consts.StairLength_DefaultStraight;
                currentLevelHeight = Statics.LevelHeight;
                break;
            case Def.StairType.Escalator:
                Consts.StairLength = Consts.StairLength_DefaultEscalator;
                currentLevelHeight = Statics.LevelHeight;
                break;
            case Def.StairType.HalfLanding:
                Consts.StairLength = Consts.StairLength_DefaultHalfLanding;
                currentLevelHeight = Statics.LevelHeight / 2f;
                break;
        }

        fieldLength.text = Consts.StairLength.ToString();
        fieldSlope.text = (currentLevelHeight / Consts.StairLength).ToString("0.####");
    }

    public void UpdateSlopeField()
    {
        float newSlope = 0;

        if (string.IsNullOrEmpty(fieldSlope.text))
        {
            UpdateLengthField();
        }
        else
        {
            if (!float.TryParse(fieldSlope.text, out newSlope))
            {
                fieldSlope.text = (currentLevelHeight * Consts.StairHeight / Consts.StairLength).ToString();
                return;
            }

            float newLength = currentLevelHeight / newSlope;

            if (newLength < 1)
            {
                fieldSlope.text = (currentLevelHeight * Consts.StairHeight / Consts.StairLength).ToString();
                return;
            }

            Consts.StairLength = Mathf.RoundToInt(newLength * 10f) / 10f;
            fieldLength.text = Consts.StairLength.ToString();
            fieldSlope.text = (currentLevelHeight * Consts.StairHeight / Consts.StairLength).ToString("0.####");
        }
    }

    public void UpdateLengthField()
    {
        float newLength = 0;

        if (string.IsNullOrEmpty(fieldLength.text))
        {
            Consts.StairLength = 5.0f;
            fieldSlope.text = "";
        }
        else
        {
            if (!float.TryParse(fieldLength.text, out newLength) || newLength < 1)
            {
                fieldLength.text = Consts.StairLength.ToString("0.0");
                return;
            }

            Consts.StairLength = Mathf.RoundToInt(newLength * 10f) / 10f;
            fieldLength.text = Consts.StairLength.ToString("0.0");
            fieldSlope.text = (currentLevelHeight * Consts.StairHeight / Consts.StairLength).ToString("0.0###");
        }

        switch (currentType)
        {
            case Def.StairType.Straight:
                Consts.StairLength_DefaultStraight = Consts.StairLength;
                break;
            case Def.StairType.HalfLanding:
                Consts.StairLength_DefaultHalfLanding = Consts.StairLength;
                break;
        }
    }

    public void UpdateWidthField()
    {
        float newWidth = 0;

        if (string.IsNullOrEmpty(fieldWidth.text))
        {
            Consts.StairWidth = 2.0f;
        }
        else
        {
            if (!float.TryParse(fieldWidth.text, out newWidth) || newWidth < 1)
            {
                fieldWidth.text = Consts.StairWidth.ToString("0.0");
                return;
            }

            Consts.StairWidth = Mathf.RoundToInt(newWidth * 10f) / 10f;
            fieldWidth.text = Consts.StairWidth.ToString("0.0");
        }
    }

    public void UpdateFloorsField()
    {
        int newHeight = 0;

        if (string.IsNullOrEmpty(fieldFloors.text))
        {
            Consts.StairHeight = 1;
        }
        else
        {
            if (!int.TryParse(fieldFloors.text, out newHeight) || newHeight < 1)
            {
                fieldFloors.text = Consts.StairHeight.ToString("0");
                return;
            }

            Consts.StairHeight = newHeight;
            fieldFloors.text = Consts.StairHeight.ToString("0");
            fieldSlope.text = (currentLevelHeight * Consts.StairHeight / Consts.StairLength).ToString("0.0###");
        }
    }

    public void UpdateLandingWidthField()
    {
        float newWidth = 0;

        if (string.IsNullOrEmpty(fieldLandingWidth.text))
        {
            Consts.StairLandingWidth = 2.0f;
        }
        else
        {
            if (!float.TryParse(fieldLandingWidth.text, out newWidth) || newWidth < 1)
            {
                fieldLandingWidth.text = Consts.StairLandingWidth.ToString("0.0");
                return;
            }

            Consts.StairLandingWidth = Mathf.RoundToInt(newWidth * 10f) / 10f;
            fieldLandingWidth.text = Consts.StairLandingWidth.ToString("0.0");
        }
    }
}
