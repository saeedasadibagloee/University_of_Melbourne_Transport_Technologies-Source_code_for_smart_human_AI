using System.Collections.Generic;
using DataFormats;
using Info;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DialogDesignatedGate : MonoBehaviour
{
    public GameObject fileDialog = null;
    public Button okayButton = null;

    [FormerlySerializedAs("dialogFields")]
    public List<InputField> destinationDialogFields = new List<InputField>();
    public List<InputField> intermediateDialogFields = new List<InputField>();

    private const string OverTotalError = "Your designated gates cannot add to more than 100%.";
    private const float TotalAllowed = 100f;
    private List<int> _destinationGates;
    private List<int> _intermediateGates;
    private DesignatedGatesData _dGatesData = null;
    private GateInfo[] gates;

    private static DialogDesignatedGate _dialogDesignatedGate;

    public static DialogDesignatedGate Instance
    {
        get { return _dialogDesignatedGate ?? (_dialogDesignatedGate = FindObjectOfType<DialogDesignatedGate>()); }
    }

    public bool IsActive()
    {
        return fileDialog.activeInHierarchy;
    }

    public void Active(bool active, DesignatedGatesData dGatesData = null, int excludeId = -1)
    {
        if (dGatesData != null)
            _dGatesData = dGatesData;

        fileDialog.SetActive(active);

        if (!active) return;

        // Remove all fields already there, apart from the first one.
        for (int i = destinationDialogFields.Count - 1; i > 0; i--)
        {
            Destroy(destinationDialogFields[i].gameObject);
            destinationDialogFields.RemoveAt(i);
        }
        for (int i = intermediateDialogFields.Count - 1; i > 0; i--)
        {
            Destroy(intermediateDialogFields[i].gameObject);
            intermediateDialogFields.RemoveAt(i);
        }

        _destinationGates = Create.Instance.GetDestinationGates();
        _intermediateGates = Create.Instance.GetIntermediateGates();

        if (_dGatesData != null)
        {
            foreach (DesignatedGateData item in _dGatesData.Distribution)
            {
                if (!_destinationGates.Contains(item.GateID) && !_intermediateGates.Contains(item.GateID))
                {
                    _destinationGates.Add(item.GateID);
                }
            }
        }

        if (_destinationGates == null || _destinationGates.Count == 0)
        {
            SetLabel(destinationDialogFields[0], "No Gates Detected");
            destinationDialogFields[0].interactable = false;
        }
        else
        {
            RectTransform fdRt = fileDialog.GetComponent<RectTransform>();
            RectTransform scrollView = fileDialog.GetComponentInChildren<ScrollRect>().transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
            fdRt.sizeDelta = new Vector2(fdRt.sizeDelta.x, 215);

            gates = FindObjectsOfType<GateInfo>();

            for (int i = 0; i < _destinationGates.Count; i++)
            {
                if (i > 0)
                {
                    GameObject newField = Instantiate(destinationDialogFields[i - 1].gameObject, scrollView.transform);
                    RectTransform rt = newField.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y - 44);
                    destinationDialogFields.Add(newField.GetComponent<InputField>());
                }

                SetLabel(destinationDialogFields[i], GetTypeString(_destinationGates[i]) + ": " + _destinationGates[i]);
                destinationDialogFields[i].interactable = _destinationGates[i] != excludeId;

                if (_dGatesData != null)
                {
                    float fieldValue = _dGatesData.FindDataByGate(_destinationGates[i]);
                    destinationDialogFields[i].text = fieldValue > 0 ? fieldValue.ToString() : string.Empty;
                }
                else
                {
                    destinationDialogFields[i].text = string.Empty;
                }
            }

            for (int i = 0; i < _intermediateGates.Count; i++)
            {
                if (i > 0)
                {
                    GameObject newField = Instantiate(intermediateDialogFields[i - 1].gameObject, scrollView.transform);
                    RectTransform rt = newField.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y - 44);
                    intermediateDialogFields.Add(newField.GetComponent<InputField>());
                }

                SetLabel(intermediateDialogFields[i], GetTypeString(_intermediateGates[i]) + ": " + _intermediateGates[i]);
                intermediateDialogFields[i].interactable = _intermediateGates[i] != excludeId;

                if (_dGatesData != null)
                {
                    float fieldValue = _dGatesData.FindDataByGate(_intermediateGates[i]);
                    intermediateDialogFields[i].text = fieldValue > 0 ? fieldValue.ToString() : string.Empty;
                }
                else
                {
                    intermediateDialogFields[i].text = string.Empty;
                }
            }

            int maxNumFieldsHeight = Mathf.Max(destinationDialogFields.Count, intermediateDialogFields.Count) - 2;
            if (maxNumFieldsHeight < 0)
                maxNumFieldsHeight = 0;
            if (maxNumFieldsHeight > 10)
                maxNumFieldsHeight = 10;

            fdRt.sizeDelta = new Vector2(fdRt.sizeDelta.x, fdRt.sizeDelta.y + 44 * maxNumFieldsHeight);
            scrollView.sizeDelta = new Vector2(scrollView.sizeDelta.x, 44 * destinationDialogFields.Count);
        }

        Create.Instance.ViewGateIDs();
    }

    private string GetTypeString(int destinationGate)
    {
        foreach (var gate in gates)
        {
            if (gate.Id == destinationGate)
            {
                return gate.DesignatedOnly ? "Train" : "Gate";
            }
        }

        return "Gate";
    }

    public void CloseDialog()
    {
        Create.Instance.UnViewGateIDs();
        Active(false);
    }

    public void UpdateFields(string text)
    {
        okayButton.interactable = true;
        bool desOk = false;
        bool intOk = false;

        float sum = 0f;
        foreach (InputField field in destinationDialogFields)
        {
            float fieldValue = 0f;
            float.TryParse(field.text, out fieldValue);

            sum += Mathf.Abs(fieldValue);

            if (sum > TotalAllowed)
            {
                Debug.Log(OverTotalError);
                //field.text = (Mathf.Abs(fieldValue) - (sum - TotalAllowed)).ToString();
                okayButton.interactable = false;
                break;
            }
        }
    }

    public void OkayButton()
    {
        float sum = 0f;
        foreach (InputField field in destinationDialogFields)
        {
            float fieldValue = 0f;
            float.TryParse(field.text, out fieldValue);

            sum += Mathf.Abs(fieldValue);

            if (sum > TotalAllowed)
            {
                Debug.Log(OverTotalError);
                UIController.Instance.ShowGeneralDialog(OverTotalError, "Designated Total");
                return;
            }
        }

        if (_destinationGates == null)
            _destinationGates = Create.Instance.GetDestinationGates();
        if (_destinationGates.Count < 1)
            return;
        if (_intermediateGates == null)
            _intermediateGates = Create.Instance.GetIntermediateGates();

        _dGatesData = new DesignatedGatesData();

        for (int i = 0; i < _destinationGates.Count; i++)
        {
            InputField field = destinationDialogFields[i];

            float fieldValue = 0f;
            float.TryParse(field.text, out fieldValue);

            int gateId = _destinationGates[i];

            if (gateId >= 0)
            {
                if (fieldValue > 0)
                {
                    _dGatesData.AddData(gateId, fieldValue);
                }
            }
            else
            {
                Debug.LogError("Could not read gateID in label?");
            }
        }

        for (int i = 0; i < _intermediateGates.Count; i++)
        {
            InputField field = intermediateDialogFields[i];

            float fieldValue = 0f;
            float.TryParse(field.text, out fieldValue);

            int gateId = _intermediateGates[i];

            if (gateId >= 0)
            {
                if (fieldValue > 0)
                {
                    _dGatesData.AddData(gateId, fieldValue, true);
                }
            }
            else
            {
                Debug.LogError("Could not read gateID in label?");
            }
        }

        List<DesignatedGatesData> dGatesList = new List<DesignatedGatesData> { _dGatesData };
        UIController.Instance.DialogWithFieldsApplyAgentDestinationGates(dGatesList);
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