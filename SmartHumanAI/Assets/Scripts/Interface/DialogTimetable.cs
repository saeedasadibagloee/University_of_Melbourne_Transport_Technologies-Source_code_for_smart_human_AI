using DataFormats;
using Info;
using InputOutput;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Interface
{
    class DialogTimetable : MonoBehaviour
    {
        public GameObject fileDialog = null;
        public Button okayButton = null;
        public Text statusText = null;
        public Dropdown dropdown = null;

        private AgentDistInfo agentDist;

        private static DialogTimetable _dialogTimetable;

        public static DialogTimetable Instance
        {
            get { return _dialogTimetable ?? (_dialogTimetable = FindObjectOfType<DialogTimetable>()); }
        }

        internal void Active(bool active, AgentDistInfo agentDistInfo = null)
        {
            agentDist = agentDistInfo;

            fileDialog.SetActive(active);

            if (!active)
                return;

            UpdateDropdown();
            UpdateStatusText();
        }

        private void UpdateDropdown()
        {
            dropdown.value = (int)agentDist.TimetableType;
        }

        private void UpdateStatusText()
        {
            if (agentDist.PopulationTimetable == null)
                statusText.text = "No timetable loaded.";
            else
                statusText.text = agentDist.MaxAgents + " in timetable.";
        }

        public void SetGenerationType(int type)
        {
            agentDist.TimetableType = (Def.TimetableType)type;
        }

        public void CloseDialog()
        {
            fileDialog.SetActive(false);
            UIController.Instance.DialogWithFieldsOpenAgentDist();
        }

        public void LoadFile()
        {
            UIController.Instance.AgentsLoadTimetable();
        }

        public void ClearTimetable()
        {
            agentDist.PopulationTimetable = null;
            agentDist.NumberOfAgents = 1;
            CloseDialog();
            UIController.Instance.DialogWithFieldsClose();
        }

        public void OkayButton()
        {
            CloseDialog();
        }

        internal void OpenTimetable(string filePath)
        {
            agentDist.PopulationTimetable = FileOperations.Instance.ReadPopulationTimetable(filePath);
            agentDist.CalcMaxAgents();            
            agentDist.NumberOfAgents = 0;
            agentDist.AgentType = Def.DistributionType.Dynamic;
            UpdateStatusText();
            UIController.Instance.ShowGeneralDialog("Successfully loaded timetable with a total of " + agentDist.MaxAgents + " agents.", "Loaded Population Timetable");
        }
    }
}
