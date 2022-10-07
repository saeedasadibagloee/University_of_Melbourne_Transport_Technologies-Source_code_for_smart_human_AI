using System.Collections.Generic;
using System.Globalization;
using DataFormats;
using Info;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DialogCarriageEditor : MonoBehaviour
{
    public GameObject fileDialog = null;
    public Button okayButton = null;

    public List<InputField> numberDoorsFields = new List<InputField>();
    public List<InputField> carriageLengthFields = new List<InputField>();
    public List<InputField> boardingDistFields = new List<InputField>();

    private TrainGenerator _trainGen = null;

    private static DialogCarriageEditor _dialogCarriageEditor;

    public static DialogCarriageEditor Instance
    {
        get { return _dialogCarriageEditor ?? (_dialogCarriageEditor = FindObjectOfType<DialogCarriageEditor>()); }
    }

    public bool IsActive()
    {
        return fileDialog.activeInHierarchy;
    }

    public void Active(bool active, TrainGenerator trainGen = null)
    {
        fileDialog.SetActive(active);

        if (!active) return;

        _trainGen = trainGen;

        // Remove all fields already there, apart from the first one.
        for (int i = numberDoorsFields.Count - 1; i > 0; i--)
        {
            Destroy(numberDoorsFields[i].gameObject);
            numberDoorsFields.RemoveAt(i);
        }
        for (int i = carriageLengthFields.Count - 1; i > 0; i--)
        {
            Destroy(carriageLengthFields[i].gameObject);
            carriageLengthFields.RemoveAt(i);
        }
        for (int i = boardingDistFields.Count - 1; i > 0; i--)
        {
            Destroy(boardingDistFields[i].gameObject);
            boardingDistFields.RemoveAt(i);
        }

        RectTransform fdRt = fileDialog.GetComponent<RectTransform>();
        fdRt.sizeDelta = new Vector2(fdRt.sizeDelta.x, 215);

        for (int i = 0; i < _trainGen.numCarriages; i++)
        {
            if (i > 0)
            {
                GameObject newField = Instantiate(numberDoorsFields[i - 1].gameObject, fileDialog.transform);
                RectTransform rt = newField.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y - 44);
                numberDoorsFields.Add(newField.GetComponent<InputField>());

                GameObject newField1 = Instantiate(carriageLengthFields[i - 1].gameObject, fileDialog.transform);
                RectTransform rt1 = newField1.GetComponent<RectTransform>();
                rt1.anchoredPosition = new Vector2(rt1.anchoredPosition.x, rt1.anchoredPosition.y - 44);
                carriageLengthFields.Add(newField1.GetComponent<InputField>());

                GameObject newField2 = Instantiate(boardingDistFields[i - 1].gameObject, fileDialog.transform);
                RectTransform rt2 = newField2.GetComponent<RectTransform>();
                rt2.anchoredPosition = new Vector2(rt2.anchoredPosition.x, rt2.anchoredPosition.y - 44);
                boardingDistFields.Add(newField2.GetComponent<InputField>());
            }

            SetLabel(numberDoorsFields[i], "Carriage: " + (i + 1));

            if (_trainGen != null && i < _trainGen.numDoorsList.Count)
                numberDoorsFields[i].text = _trainGen.numDoorsList[i].ToString();
            else
                numberDoorsFields[i].text = string.Empty;

            if (_trainGen != null && i < _trainGen.carriageLengthsList.Count)
                carriageLengthFields[i].text = _trainGen.carriageLengthsList[i].ToString();
            else
                carriageLengthFields[i].text = string.Empty;

            if (_trainGen != null && i < _trainGen.boardDistributionList.Count)
            {
                var boardDist = _trainGen.boardDistributionList[i];
                boardingDistFields[i].text = boardDist < 0 ? string.Empty : boardDist.ToString(CultureInfo.InvariantCulture);
            }
            else
                boardingDistFields[i].text = string.Empty;
        }

        int maxNumFieldsHeight = Mathf.Max(numberDoorsFields.Count, carriageLengthFields.Count) - 2;
        if (maxNumFieldsHeight < 0)
            maxNumFieldsHeight = 0;
        fdRt.sizeDelta = new Vector2(fdRt.sizeDelta.x, fdRt.sizeDelta.y + 44 * maxNumFieldsHeight);
    }

    public void CloseDialog()
    {
        Active(false);
    }

    public void UpdateFields(string text)
    {
        FieldLimitRange();
    }

    private void FieldLimitRange()
    {
        foreach (InputField field in numberDoorsFields)
        {
            float fieldValue = 0f;
            if (float.TryParse(field.text, out fieldValue) && fieldValue < 2)
                field.text = 2.ToString();
        }

        foreach (InputField field in carriageLengthFields)
        {
            float fieldValue = 0f;
            if (float.TryParse(field.text, out fieldValue) && fieldValue < 1)
                field.text = 1.ToString();
        }

        foreach (InputField field in boardingDistFields)
        {
            float fieldValue = 0f;
            if (float.TryParse(field.text, out fieldValue) && fieldValue > 100f)
                field.text = 100.ToString();
        }
    }

    public void OkayButton()
    {
        FieldLimitRange();

        List<int> numCarriageDoors = _trainGen.numDoorsList;
        while (numCarriageDoors.Count < _trainGen.numCarriages)
            numCarriageDoors.Add(2);

        List<float> lengthCarriages = _trainGen.carriageLengthsList;
        while (lengthCarriages.Count < _trainGen.numCarriages)
            lengthCarriages.Add(28);

        List<float> boardDists = new List<float>();
        while (boardDists.Count < _trainGen.numCarriages)
            boardDists.Add(-1f);

        for (int i = 0; i < _trainGen.numCarriages; i++)
        {
            int fieldValue;
            if (int.TryParse(numberDoorsFields[i].text, out fieldValue))
                numCarriageDoors[i] = fieldValue;
            else
                Debug.Log("Could not parse " + numberDoorsFields[i].text);

            float fieldValue2;
            if (float.TryParse(carriageLengthFields[i].text, out fieldValue2))
                lengthCarriages[i] = fieldValue2;
            else
                Debug.Log("Could not parse " + carriageLengthFields[i].text);

            float fieldValue3;
            if (float.TryParse(boardingDistFields[i].text, out fieldValue3))
                boardDists[i] = fieldValue3;
            else
                Debug.Log("Could not parse " + boardingDistFields[i].text);
        }

        UIController.Instance.DialogWithFieldsApplyTrainGeneration(numCarriageDoors, lengthCarriages, boardDists);

        CloseDialog();
    }

    private static void SetLabel(InputField g, string label)
    {
        foreach (Text t in g.transform.GetComponentsInChildren<Text>())
        {
            if (t.transform.name == "FileText")
                t.text = label;
        }
    }

}