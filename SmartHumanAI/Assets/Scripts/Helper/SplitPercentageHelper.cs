using System;
using UnityEngine;
using UnityEngine.UI;

public class SplitPercentageHelper : MonoBehaviour
{

    public InputField Field1;
    public InputField Field2;

    public const float TotalAmount = 100f;

    public void Field1Changed()
    {
        float field1Value = 0f;
        float.TryParse(Field1.text, out field1Value);
        float field2Value = TotalAmount - field1Value;
        Clamp0100(ref field1Value, ref field2Value);
        Field1.text = field1Value.ToString();
        Field2.text = field2Value.ToString();
        UIController.Instance.AdvanvedGC_SPLIT1(field1Value.ToString());
        UIController.Instance.AdvanvedGC_SPLIT2(field2Value.ToString());
    }

    public void Field2Changed()
    {
        float field2Value = 0f;
        float.TryParse(Field2.text, out field2Value);
        float field1Value = TotalAmount - field2Value;
        Clamp0100(ref field1Value, ref field2Value);
        Field1.text = field1Value.ToString();
        Field2.text = field2Value.ToString();
        UIController.Instance.AdvanvedGC_SPLIT1(field1Value.ToString());
        UIController.Instance.AdvanvedGC_SPLIT2(field2Value.ToString());
    }

    private void Clamp0100(ref float field1Value, ref float field2Value)
    {
        if (field1Value > TotalAmount) field1Value = TotalAmount;
        if (field1Value < 0) field1Value = 0;
        if (field2Value > TotalAmount) field2Value = TotalAmount;
        if (field2Value < 0) field2Value = 0;
    }
}