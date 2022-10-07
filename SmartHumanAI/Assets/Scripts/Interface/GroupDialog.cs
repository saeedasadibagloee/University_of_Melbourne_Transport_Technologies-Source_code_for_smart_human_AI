using System;
using System.Collections.Generic;
using DataFormats;
using Info;
using UnityEngine;
using UnityEngine.UI;

public class GroupDialog : MonoBehaviour
{
    public GameObject fileDialog = null;
    public Button okayButton = null;
    public List<InputField> dialogFields = new List<InputField>();
    public Text GroupsOfX = null;
    private int largeGroupX = 0;

    private AgentDistInfo agentDist;

    private static GroupDialog _groupDialog;

    public static GroupDialog Instance
    {
        get { return _groupDialog ?? (_groupDialog = FindObjectOfType<GroupDialog>()); }
    }

    internal void Active(bool active, AgentDistInfo agentDistInfo = null)
    {
        agentDist = agentDistInfo;

        fileDialog.SetActive(active);

        if (!active)
            return;

        if (dialogFields.Count < 7)
        {
            Debug.LogError("Please initialise dialog fields array.");
            return;
        }

        foreach (var field in dialogFields)
            field.text = "";

        dialogFields[0].text = agentDistInfo.NumberOfAgents.ToString();
        dialogFields[5].interactable = false;
        GroupsOfX.text = "Groups of X";
        largeGroupX = 0;

        foreach (var group in agentDist.GroupNumbers)
        {
            if (group.groupNum <= 5)
                dialogFields[group.groupNum - 1].text = group.numGroups.ToString();
            else
            {
                // Process the larger group
                largeGroupX = group.groupNum;
                dialogFields[5].interactable = true;
                dialogFields[5].text = group.numGroups.ToString();
                GroupsOfX.text = "Groups of " + group.groupNum.ToString();
            }
        }

        UpdateFields();
    }

    public bool IsActive()
    {
        return fileDialog.activeInHierarchy;
    }

    public void CloseDialog()
    {
        fileDialog.SetActive(false);
    }

    public void UpdateFields()
    {
        int total = 0;

        if (!string.IsNullOrEmpty(dialogFields[0].text))
        {
            int number = 0;
            if (int.TryParse(dialogFields[0].text, out number))
                total += number;
        }

        if (!string.IsNullOrEmpty(dialogFields[1].text))
        {
            int number = 0;
            if (int.TryParse(dialogFields[1].text, out number))
                total += 2 * number;
        }

        if (!string.IsNullOrEmpty(dialogFields[2].text))
        {
            int number = 0;
            if (int.TryParse(dialogFields[2].text, out number))
                total += 3 * number;
        }

        if (!string.IsNullOrEmpty(dialogFields[3].text))
        {
            int number = 0;
            if (int.TryParse(dialogFields[3].text, out number))
                total += 4 * number;
        }

        if (!string.IsNullOrEmpty(dialogFields[4].text))
        {
            int number = 0;
            if (int.TryParse(dialogFields[4].text, out number))
                total += 5 * number;
        }

        if (largeGroupX != 0 && !string.IsNullOrEmpty(dialogFields[5].text))
        {
            int number = 0;
            if (int.TryParse(dialogFields[5].text, out number))
                total += largeGroupX * number;
        }

        dialogFields[6].text = total.ToString();
    }

    public void OkayButton()
    {

        int number = 0;
        int.TryParse(dialogFields[0].text, out number);
        agentDist.NumberOfAgents = number;

        List<Group> groupNumbers = new List<Group>();

        number = 0;
        int.TryParse(dialogFields[1].text, out number);
        if (number != 0)
            groupNumbers.Add(new Group(2, number));

        number = 0;
        int.TryParse(dialogFields[2].text, out number);
        if (number != 0)
            groupNumbers.Add(new Group(3, number));

        number = 0;
        int.TryParse(dialogFields[3].text, out number);
        if (number != 0)
            groupNumbers.Add(new Group(4, number));

        number = 0;
        int.TryParse(dialogFields[4].text, out number);
        if (number != 0)
            groupNumbers.Add(new Group(5, number));

        number = 0;
        int.TryParse(dialogFields[5].text, out number);
        if (largeGroupX != 0 && number != 0)
            groupNumbers.Add(new Group(largeGroupX, number));

        agentDist.GroupNumbers = groupNumbers;

        UIController.Instance.DialogWithFieldsApplyGroupNumbers(agentDist);
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