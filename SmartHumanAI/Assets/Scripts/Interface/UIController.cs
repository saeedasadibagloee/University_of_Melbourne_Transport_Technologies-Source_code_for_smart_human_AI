using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Assets.Scripts;
using Assets.Scripts.Interface;
using Core;
using Core.GateChoice;
using DataFormats;
using Fire;
using Helper;
using I2.Loc;
using Info;
using InputOutput;
using OxOD;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    //Import the following.
    [DllImport("user32.dll", EntryPoint = "SetWindowText")]
    public static extern bool SetWindowText(IntPtr hwnd, string lpString);

    [DllImport("user32.dll", EntryPoint = "FindWindow")]
    public static extern IntPtr FindWindow(string className, string windowName);

    public enum Languages { English, Chinese }

    public List<GameObject> levelButtonStorage = new List<GameObject>();
    public List<GameObject> advancedValuesStorage = new List<GameObject>();
    public List<GameObject> advancedValuesClass2Storage = new List<GameObject>();
    public List<GameObject> fireSimFields = new List<GameObject>();
    public List<InputField> CAFields = new List<InputField>();
    public List<InputField> HFFields = new List<InputField>();

    public Dropdown behaviouralMode;

    public FileDialog DialogFile;
    public GameObject DialogQuit;
    public GameObject DialogGeneral;
    public GameObject DialogNewFile;
    public GameObject DialogWithFields;
    public GameObject DialogPrefab;
    public GameObject DialogIndividualAgent;
    public GameObject DialogFire;
    public GameObject DialogDxf;
    public GameObject DialogLanguage;

    [HideInInspector]
    public int NumLevels = 0;

    private AgentDistInfo ag;

    public GameObject followCameraPrefab;

    public bool GridDisplayEnabled = true;
    public bool DialogOpen = false;
    public bool KeyboardEnabled = true;

    // Text constants
    private const string SetTo = " set to ";
    private const string timetableEnabledString = "Timetabled.";
    private const string OOR1 = "Note, the recommended range for ";
    private const string OOR15 = " in ";
    private const string OOR2 = " behaviour is between ";
    private const string OOR3 = " and ";
    private const string OOR4 = ".";
    private const string OOR5 = "Outside Recommended Range";

    private static UIController _instance = null;
    private static readonly object Padlock = new object();

    private static readonly string _windowNameDefault = "Pedestride Multimodal";
    private static string _windowNameCurrent = "Pedestride Multimodal";

    private Camera _followCamera = null;
    private Camera _mainCamera = null;

    private string _resultPath = null;
    private string _resultPathImport = null;
    private string _resultPathTimetable = null;
    private string _resultPathSave = null;
    private string _resultPathExportData = null;

    private GameObject _dialogFieldsEditingObject;
    private Def.DialogType _dialogFieldsEditingType = Def.DialogType.Unknown;
    private GameObject _currentlyEditingPrefab;

    public InputField ActivationInputField;
    public InputField TicketGateWidthField;

    [Header("Dialog With Fields")]
    public Button timetableButton;
    public Button groupsButton;
    public Button designatedButton;
    public Button colorButton_fieldsDialog;
    public Button colorButton_iaDialog;
    private InputField _dialogField1;
    private InputField _dialogField2;
    private InputField _dialogField3;
    private Text _dialogText1;
    private Text _dialogText2;
    private Text _dialogText3;
    private InputField DialogField1
    {
        get
        { return _dialogField1 ?? (_dialogField1 = GameObject.Find("DialogField1").GetComponent<InputField>()); }
    }
    private InputField DialogField2
    {
        get
        { return _dialogField2 ?? (_dialogField2 = GameObject.Find("DialogField2").GetComponent<InputField>()); }
    }
    private InputField DialogField3
    {
        get
        { return _dialogField3 ?? (_dialogField3 = GameObject.Find("DialogField3").GetComponent<InputField>()); }
    }
    private Text DialogText1
    {
        get
        {
            if (_dialogText1 != null) return _dialogText1;
            foreach (Text text in DialogField1.GetComponentsInChildren<Text>())
                if (text.name == "FileText")
                    _dialogText1 = text;
            return _dialogText1;
        }
    }
    private Text DialogText2
    {
        get
        {
            if (_dialogText2 != null) return _dialogText2;
            foreach (Text text in DialogField2.GetComponentsInChildren<Text>())
                if (text.name == "FileText")
                    _dialogText2 = text;
            return _dialogText2;
        }
    }
    private Text DialogText3
    {
        get
        {
            if (_dialogText3 != null) return _dialogText3;
            foreach (Text text in DialogField3.GetComponentsInChildren<Text>())
                if (text.name == "FileText")
                    _dialogText3 = text;
            return _dialogText3;
        }
    }
    private Text _dialogTitle;
    private Text DialogTitle
    {
        get
        { return _dialogTitle ?? (_dialogTitle = GameObject.Find("DialogWithFieldsTitle").GetComponent<Text>()); }
    }

    [Header("Other")]
    public TextMeshProUGUI _consoleText;
    public InputField _dangerStartTime;
    public InputField _dangerDuration;
    public GameObject GridSnapTick;
    public GameObject WallSnapTick;
    public GameObject AdvancedSettingsTick;
    public Dropdown _agentTypeDropdown;
    public Dropdown _agentGroupDropdown;
    public List<InputField> agentInitialFields;
    public List<InputField> agentSecFields;
    public InputField agentGroupSize;
    public Text TimetableStatusTextTrains;
    public Text TableStatusDistribution;
    public InputField agentLimitField;
    public GameObject SSxLoadedText;
    public Button CompleteButton;

    public List<GameObject> ReactionTimePages = new List<GameObject>();

    public Def.AgentPlacement AgentGenerationType = Def.AgentPlacement.Circle;

    public List<GameObject> _taskbarItems = new List<GameObject>();
    public List<GameObject> _taskbar2Items = new List<GameObject>();
    public List<GameObject> MenuItems = new List<GameObject>();
    private readonly List<GameObject> _levelButtons = new List<GameObject>();

    private readonly bool _saveLastPath = true;
    private bool[] _taskbarStates;
    private bool[] _taskbar2States;
    private bool[] _menuStates;
    private bool _smallerGridSnap = false;
    private bool _wallSnapEnabled = true;
    private bool _viewPathUpdates = false;
    private bool _smoothHeatmap = true;
    private bool _showAdvancedSettings = false;

    [Tooltip("Items that should be disabled during PANIC mode.")]
    public List<GameObject> panicButtonsFields = new List<GameObject>();
    [Tooltip("Items that should be disabled during NORMAL mode.")]
    public List<GameObject> normalButtonsFields = new List<GameObject>();
    [Tooltip("Items that should be enabled with advanced settings.")]
    public List<GameObject> advancedSettingsObjects = new List<GameObject>();
    [Tooltip("Items that should be enabled with stochastic paths (expanded normal).")]
    public List<InputField> expandedNormalItems = new List<InputField>();
    [Tooltip("Items that should be enabled with stochastic stopping.")]
    public List<InputField> stochasticStoppingItems = new List<InputField>();
    [Tooltip("Items that should be enabled with stochastic detours.")]
    public List<InputField> stochasticDetourItems = new List<InputField>();

    public CanvasScaler canvasScaler = null;

    public string modelName = "";

    public string ModelName
    {
        get
        {
            return string.IsNullOrEmpty(modelName) ? "Untitled" : modelName;
        }
    }

    private bool _viewPathfinding = false;

    public bool saveParemeters = false;

    public static UIController Instance
    {
        get
        {
            lock (Padlock)
            {
                return _instance ?? (_instance = FindObjectOfType<UIController>());
            }
        }
    }

    public bool ViewPathUpdates
    {
        get { return _viewPathUpdates; }
    }


    // Use this for initialization
    public void Start()
    {
        FindObjectOfType<ReplaceFonts>().ReplaceAll();

        _mainCamera = Camera.main;
        InitialiseMenuAndTaskbars();
        GridSnapTick.SetActive(_smallerGridSnap);
        Create.Instance.SaveGraphCollisionOptions();
        LevelAddLevel();

        Application.targetFrameRate = 60;

        FireSetEnabledState(0);

        // Set UI to panic mode correctly.
        ChangeMode(Params.panicMode);
        SetActiveAdvancedItems(_showAdvancedSettings);
    }

    public void Update()
    {
        if (_viewPathfinding)
            SimulationController.Instance.ViewPathsNow(true);
        if (Camera.main.scaledPixelHeight > Consts.UIScaleHeighChange2)
            canvasScaler.scaleFactor = 2f;
        else if (Camera.main.scaledPixelHeight > Consts.UIScaleHeighChange1)
            canvasScaler.scaleFactor = 1.5f;
        else
            canvasScaler.scaleFactor = 1f;
    }

    internal void SetConsoleText(string message)
    {
        _consoleText.text = message;
    }

    public void KeyboardEnable()
    {
        KeyboardEnabled = true;
    }
    public void KeyboardDisable()
    {
        KeyboardEnabled = false;
    }

    private void InitialiseMenuAndTaskbars()
    {
        _menuStates = new bool[MenuItems.Count];
        for (var i = 0; i < MenuItems.Count; i++)
        {
            foreach (Transform t in MenuItems[i].transform)
                t.gameObject.SetActive(false);

            _menuStates[i] = false;
        }

        _taskbarStates = new bool[_taskbarItems.Count];
        for (var i = 0; i < _taskbarItems.Count; i++)
        {
            foreach (Transform t in _taskbarItems[i].transform)
                t.gameObject.SetActive(false);
            _taskbarStates[i] = false;
        }

        _taskbar2States = new bool[_taskbar2Items.Count];
        for (var i = 0; i < _taskbar2Items.Count; i++)
        {
            foreach (Transform t in _taskbar2Items[i].transform)
                t.gameObject.SetActive(false);
            _taskbar2States[i] = false;
        }


        AgentsUpdateType();
    }

    private void toggleMenu(int menu)
    {
        if (_menuStates[menu])
        {
            foreach (Transform t in MenuItems[menu].transform)
                t.gameObject.SetActive(false);

            _menuStates[menu] = false;
        }
        else
        {
            CloseAllMenus();
            MenuItems[menu].SetActive(true);
            foreach (Transform t in MenuItems[menu].transform)
                t.gameObject.SetActive(true);

            _menuStates[menu] = true;
        }
    }

    public void CloseAllMenus()
    {
        int activeMenu = anyActiveMenus();

        if (activeMenu != Consts.MinusOne)
            toggleMenu(activeMenu);

    }

    /// <summary>
    /// Called to tell which (if any) taskbars are active.
    /// </summary>
    /// <returns>If no active taskbars: -1, otherwise the number of the active taskbar.</returns>
    public int anyActiveTaskBars()
    {
        for (int i = 0; i < _taskbarStates.Length; i++)
            if (_taskbarStates[i])
                return i;

        return Consts.MinusOne;
    }

    public int anyActive2TaskBars()
    {
        for (int i = 0; i < _taskbar2States.Length; i++)
            if (_taskbar2States[i])
                return i;

        return Consts.MinusOne;
    }

    /// <summary>
    /// Called to tell which (if any) menus are active.
    /// </summary>
    /// <returns>If no active menus: -1, otherwise the number of the active menu.</returns>
    private int anyActiveMenus()
    {
        for (int i = 0; i < _menuStates.Length; i++)
            if (_menuStates[i])
                return i;

        return Consts.MinusOne;
    }

    public void toggleTaskBarLeft(int taskbar)
    {
        if (taskbar == Consts.MinusOne)
            return;

        if (_taskbarStates[taskbar])
        {
            foreach (Transform t in _taskbarItems[taskbar].transform)
                t.gameObject.SetActive(false);

            _taskbarStates[taskbar] = false;

            CreateNone();
        }
        else
        {
            CloseActiveTaskbar();

            _taskbarItems[taskbar].SetActive(true);
            foreach (Transform t in _taskbarItems[taskbar].transform)
                t.gameObject.SetActive(true);

            _taskbarStates[taskbar] = true;

            switch (taskbar)
            {
                case 0:
                    CreateWall();
                    break;
                case 2:
                    StairsStraight();
                    break;
                case 3:
                    CreateAgents();
                    break;
                case 4:
                    //Debug.Log("Applied panic mode " + Params.panicMode);
                    ApplyPanicModeUI(Params.panicMode);
                    break;
            }

            foreach (var i2dropdown in FindObjectsOfType<i2LocalizeDropdown>())
                i2dropdown.LocalizeDropDown();
        }
    }

    public void CloseActiveTaskbar()
    {
        int activeTaskBar = anyActiveTaskBars();

        if (activeTaskBar != Consts.MinusOne)
            toggleTaskBarLeft(activeTaskBar);
    }

    public void toggleTaskBarRight(int taskbar)
    {
        if (taskbar == Consts.MinusOne)
            return;

        if (_taskbar2States[taskbar])
        {
            foreach (Transform t in _taskbar2Items[taskbar].transform)
                t.gameObject.SetActive(false);

            _taskbar2States[taskbar] = false;
        }
        else
        {
            int activeTaskBar = anyActive2TaskBars();

            if (activeTaskBar != Consts.MinusOne)
                toggleTaskBarRight(activeTaskBar);

            _taskbar2Items[taskbar].SetActive(true);
            foreach (Transform t in _taskbar2Items[taskbar].transform)
                t.gameObject.SetActive(true);

            _taskbar2States[taskbar] = true;
        }
    }

    public void toggleTaskBar8()
    {
        toggleTaskBarLeft(7);
    }

    public void toggleMenu1()
    {
        toggleMenu(0);
    }
    public void toggleMenu2()
    {
        toggleMenu(1);
    }
    public void toggleMenu3()
    {
        toggleMenu(2);
    }
    public void toggleMenu4()
    {
        toggleMenu(3);
    }
    public void toggleMenu5()
    {
        toggleMenu(4);
    }
    public void toggleMenu6()
    {
        toggleMenu(5);
    }
    public void toggleMenu7()
    {
        toggleMenu(6);
    }
    public void toggleMenu8()
    {
        toggleMenu(7);
    }

    public void AdvancedClassDropDown(int value)
    {
        Params.Current.ClassType = (Def.Class)value;

        bool isSingle = value == 0;

        foreach (GameObject g in advancedValuesClass2Storage)
            g.SetActive(!isSingle);

    }
    public void AdvancedInteractionDropDown(int value)
    {
        Params.Current.InterationType = (Def.Interaction)value;
        AdvancedInputFieldsRefresh();
    }
    public void AdvancedFunctionDropDown(int value)
    {
        Params.Current.FunctionType = (Def.Function)value;
        AdvancedInputFieldsRefresh();
    }
    private void AdvancedInputFieldsRefresh()
    {
        for (int i = 0; i < advancedValuesStorage.Count; i++)
        {
            switch ((Def.GcValues)i)
            {
                case Def.GcValues.Tovis:
                case Def.GcValues.Toinvis:
                    //advancedValuesStorage[i].SetActive(Params.Current.InterationType != 0);
                    break;
                case Def.GcValues.Flow:
                    //advancedValuesStorage[i].SetActive(Params.Current.InterationType == 0);
                    break;
            }

            if (i >= (int)Def.GcValues.SSxVC)
                advancedValuesStorage[i].SetActive(Params.Current.SpaceSyntaxEnabled);
        }
    }

    public void AdvanvedGC_VIS1(string value)
    {
        float newValue = Params.CurrentDefaults.UtilitiesClass1._visibilityUtility;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.UtilitiesClass1._visibilityUtility = newValue;
        string memberName = MemberInfoGetting.GetMemberName(() => Params.Current.UtilitiesClass1._visibilityUtility);

        if (Params.Current.UtilitiesClass1._visibilityUtility > Params.Maximum.UtilitiesClass1._visibilityUtility ||
            Params.Current.UtilitiesClass1._visibilityUtility < Params.Minimum.UtilitiesClass1._visibilityUtility)
        {
            ShowGeneralDialog(
                OOR1 + memberName + OOR15 + Params.GetModeTxt() + OOR2 + Params.Minimum.UtilitiesClass1._visibilityUtility + OOR3 + Params.Maximum.UtilitiesClass1._visibilityUtility + OOR4, OOR5);
        }

        _consoleText.text = memberName + "1" + SetTo + newValue;
    }
    public void AdvanvedGC_VIS2(string value)
    {
        float newValue = Params.CurrentDefaults.UtilitiesClass2._visibilityUtility;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.UtilitiesClass2._visibilityUtility = newValue;
        string memberName = MemberInfoGetting.GetMemberName(() => Params.Current.UtilitiesClass2._visibilityUtility);

        if (Params.Current.UtilitiesClass2._visibilityUtility > Params.Maximum.UtilitiesClass2._visibilityUtility ||
            Params.Current.UtilitiesClass2._visibilityUtility < Params.Minimum.UtilitiesClass2._visibilityUtility)
        {
            ShowGeneralDialog(
                OOR1 + memberName + OOR15 + Params.GetModeTxt() + OOR2 + Params.Minimum.UtilitiesClass2._visibilityUtility + OOR3 + Params.Maximum.UtilitiesClass2._visibilityUtility + OOR4, OOR5);
        }

        _consoleText.text = memberName + "2" + SetTo + newValue;
    }
    public void AdvanvedGC_DIST1(string value)
    {
        float newValue = Params.CurrentDefaults.UtilitiesClass1._distanceUtility;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.UtilitiesClass1._distanceUtility = newValue;
        string memberName = MemberInfoGetting.GetMemberName(() => Params.Current.UtilitiesClass1._distanceUtility);

        if (Params.Current.UtilitiesClass1._distanceUtility > Params.Maximum.UtilitiesClass1._distanceUtility ||
            Params.Current.UtilitiesClass1._distanceUtility < Params.Minimum.UtilitiesClass1._distanceUtility)
        {
            ShowGeneralDialog(
                OOR1 + memberName + OOR15 + Params.GetModeTxt() + OOR2 + Params.Minimum.UtilitiesClass1._distanceUtility + OOR3 + Params.Maximum.UtilitiesClass1._distanceUtility + OOR4, OOR5);
        }

        _consoleText.text = memberName + "1" + SetTo + newValue;
    }
    public void AdvanvedGC_DIST2(string value)
    {
        float newValue = Params.CurrentDefaults.UtilitiesClass2._distanceUtility;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.UtilitiesClass2._distanceUtility = newValue;
        string memberName = MemberInfoGetting.GetMemberName(() => Params.Current.UtilitiesClass2._distanceUtility);

        if (Params.Current.UtilitiesClass2._distanceUtility > Params.Maximum.UtilitiesClass2._distanceUtility ||
            Params.Current.UtilitiesClass2._distanceUtility < Params.Minimum.UtilitiesClass2._distanceUtility)
        {
            ShowGeneralDialog(
                OOR1 + memberName + OOR15 + Params.GetModeTxt() + OOR2 + Params.Minimum.UtilitiesClass2._distanceUtility + OOR3 + Params.Maximum.UtilitiesClass2._distanceUtility + OOR4, OOR5);
        }

        _consoleText.text = memberName + "2" + SetTo + newValue;
    }
    public void AdvanvedGC_CONG1(string value)
    {
        float newValue = Params.CurrentDefaults.UtilitiesClass1._congestionUtility;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.UtilitiesClass1._congestionUtility = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.UtilitiesClass1._congestionUtility) + "1" + SetTo + newValue;
    }
    public void AdvanvedGC_CONG2(string value)
    {
        float newValue = Params.CurrentDefaults.UtilitiesClass2._congestionUtility;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.UtilitiesClass2._congestionUtility = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.UtilitiesClass2._congestionUtility) + "2" + SetTo + newValue;
    }
    public void AdvanvedGC_FLOW1(string value)
    {
        float newValue = Params.CurrentDefaults.UtilitiesClass1._flowExitUtility;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.UtilitiesClass1._flowExitUtility = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.UtilitiesClass1._flowExitUtility) + "1" + SetTo + newValue;
    }
    public void AdvanvedGC_FLOW2(string value)
    {
        float newValue = Params.CurrentDefaults.UtilitiesClass2._flowExitUtility;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.UtilitiesClass2._flowExitUtility = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.UtilitiesClass2._flowExitUtility) + "2" + SetTo + newValue;
    }
    public void AdvanvedGC_FLOWTOVIS1(string value)
    {
        float newValue = Params.CurrentDefaults.UtilitiesClass1._fltovis;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.UtilitiesClass1._fltovis = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.UtilitiesClass1._fltovis) + "1" + SetTo + newValue;
    }
    public void AdvanvedGC_FLOWTOVIS2(string value)
    {
        float newValue = Params.CurrentDefaults.UtilitiesClass2._fltovis;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.UtilitiesClass2._fltovis = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.UtilitiesClass2._fltovis) + "2" + SetTo + newValue;
    }
    public void AdvanvedGC_FLOWTOINVIS1(string value)
    {
        float newValue = Params.CurrentDefaults.UtilitiesClass1._fltoinvis;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.UtilitiesClass1._fltoinvis = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.UtilitiesClass1._fltoinvis) + "1" + SetTo + newValue;
    }
    public void AdvanvedGC_FLOWTOINVIS2(string value)
    {
        float newValue = Params.CurrentDefaults.UtilitiesClass2._fltoinvis;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.UtilitiesClass2._fltoinvis = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.UtilitiesClass2._fltoinvis) + "2" + SetTo + newValue;
    }
    public void AdvanvedGC_SPLIT1(string value)
    {
        float newValue = Params.CurrentDefaults.UtilitiesClass1._split;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.UtilitiesClass1._split = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.UtilitiesClass1._split) + "1" + SetTo + newValue;
    }
    public void AdvanvedGC_SPLIT2(string value)
    {
        float newValue = Params.CurrentDefaults.UtilitiesClass2._split;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.UtilitiesClass2._split = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.UtilitiesClass2._split) + "2" + SetTo + newValue;
    }
    public void AdvancedUIUpdate(string value)
    {
        int newValue = Params.CurrentDefaults.UiUpdateCycle;

        if (!string.IsNullOrEmpty(value))
            int.TryParse(value, out newValue);

        //int.TryParse(value, out newValue);

        Params.Current.UiUpdateCycle = newValue;
        Consts.SpeedMultiplier = Consts.SpeedMultiplier * 30f / Params.Current.UiUpdateCycle;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.UiUpdateCycle) + SetTo +
                            newValue;
    }
    public void AdvancedTheta(string value)
    {
        float newValue = Params.CurrentDefaults.Theta;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        //float.TryParse(value, out newValue);

        Params.Current.Theta = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.Theta) + SetTo + newValue;
    }
    public void AdvancedTimeStep(string value)
    {
        float newValue = Params.CurrentDefaults.TimeStep;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.TimeStep = newValue;
        string memberName = MemberInfoGetting.GetMemberName(() => Params.Current.TimeStep);

        if (Params.Current.TimeStep > Params.Maximum.TimeStep)
        {
            if (_showAdvancedSettings)
            {
                ShowGeneralDialog(
                OOR1 + memberName + OOR15 + Params.GetModeTxt() + OOR2 + Params.Minimum.TimeStep + OOR3 + Params.Maximum.TimeStep + OOR4, OOR5);
            }
            else
            {
                Params.Current.TimeStep = Params.Maximum.TimeStep;
                VariablePresetHandler.Instance.UpdateAllFields();
            }
        }
        else if (Params.Current.TimeStep < Params.Minimum.TimeStep)
        {
            if (_showAdvancedSettings)
            {
                ShowGeneralDialog(
                    OOR1 + memberName + OOR15 + Params.GetModeTxt() + OOR2 + Params.Minimum.TimeStep + OOR3 + Params.Maximum.TimeStep + OOR4, OOR5);
            }
            else
            {
                Params.Current.TimeStep = Params.Minimum.TimeStep;
                VariablePresetHandler.Instance.UpdateAllFields();
            }
        }

        _consoleText.text = memberName + SetTo + Params.Current.TimeStep;
    }
    public void AdvancedPenaltyWeight(string value)
    {
        uint newValue = Params.Current.PenaltyWeight;

        if (!string.IsNullOrEmpty(value))
            uint.TryParse(value, out newValue);

        Params.Current.PenaltyWeight = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.PenaltyWeight) + SetTo +
                            newValue;
    }
    public void AdvancedWallPadding(string value)
    {
        float newValue = Params.CurrentDefaults.AgentWallPaddingDistance;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.AgentWallPaddingDistance = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.AgentWallPaddingDistance) +
                            SetTo + newValue;
    }
    public void AdvancedDensityFactor(string value)
    {
        double newValue = Params.Current.DensityFactor;

        if (!string.IsNullOrEmpty(value))
            double.TryParse(value, out newValue);

        Params.Current.DensityFactor = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.DensityFactor) + SetTo +
                            newValue;
    }
    public void AdvancedFrictionForce(string value)
    {
        int newValue = Params.CurrentDefaults.DefaultFrictionalForce;

        if (!string.IsNullOrEmpty(value))
            int.TryParse(value, out newValue);

        Params.Current.DefaultFrictionalForce = newValue;
        string memberName = MemberInfoGetting.GetMemberName(() => Params.Current.DefaultFrictionalForce);

        if (Params.Current.DefaultFrictionalForce > Params.Maximum.DefaultFrictionalForce || Params.Current.DefaultFrictionalForce < Params.Minimum.DefaultFrictionalForce)
        {
            ShowGeneralDialog(
                OOR1 + memberName + OOR15 + Params.GetModeTxt() + OOR2 + Params.Minimum.DefaultFrictionalForce + OOR3 + Params.Maximum.DefaultFrictionalForce + OOR4, OOR5);
        }

        _consoleText.text = memberName + SetTo + newValue;
    }
    public void AdvancedRepulsionDistance(string value)
    {
        float newValue = Params.CurrentDefaults.DefaultRepultionDistance;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.DefaultRepultionDistance = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.DefaultRepultionDistance) +
                            SetTo + newValue;
    }
    public void AdvancedAttractiveForce(string value)
    {
        int newValue = Params.CurrentDefaults.DefaultAttractiveForce;

        if (!string.IsNullOrEmpty(value))
            int.TryParse(value, out newValue);

        Params.Current.DefaultAttractiveForce = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.DefaultAttractiveForce) +
                            SetTo + newValue;
    }
    public void AdvancedRepulsiveForce(string value)
    {
        int newValue = Params.CurrentDefaults.DefaultRepulsiveForce;

        if (!string.IsNullOrEmpty(value))
            int.TryParse(value, out newValue);

        Params.Current.DefaultRepulsiveForce = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.DefaultRepulsiveForce) +
                            SetTo + newValue;
    }
    public void AdvancedReactionTime(string value)
    {
        float newValue = Params.CurrentDefaults.DefaultReactionTime;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.DefaultReactionTime = newValue;
        string memberName = MemberInfoGetting.GetMemberName(() => Params.Current.DefaultReactionTime);

        if (Params.Current.DefaultReactionTime > Params.Maximum.DefaultReactionTime || Params.Current.DefaultReactionTime < Params.Minimum.DefaultReactionTime)
        {
            ShowGeneralDialog(
                OOR1 + memberName + OOR15 + Params.GetModeTxt() + OOR2 + Params.Minimum.DefaultReactionTime + OOR3 + Params.Maximum.DefaultReactionTime + OOR4, OOR5);
        }

        _consoleText.text = memberName + SetTo + newValue;
    }
    public void AdvancedRandomSeed(string value)
    {
        float newValue = DateTime.Now.Millisecond + DateTime.Now.Second / 1000f;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.RandomSeed = newValue;
        string memberName = MemberInfoGetting.GetMemberName(() => Params.Current.RandomSeed);
        _consoleText.text = memberName + SetTo + newValue;
    }
    public void AdvancedNeighborRadius(string value)
    {
        float newValue = Params.CurrentDefaults.NeighborRadius;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.NeighborRadius = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.NeighborRadius) + SetTo +
                            newValue;
    }
    public void AdvancedWallForce(string value)
    {
        int newValue = Params.CurrentDefaults.DefaultWallForce;

        if (!string.IsNullOrEmpty(value))
            int.TryParse(value, out newValue);

        Params.Current.DefaultWallForce = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.DefaultWallForce) + SetTo +
                            newValue;
    }
    public void AdvancedSafeWallDistance(string value)
    {
        float newValue = Params.CurrentDefaults.DefaultSafeWallDistance;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.DefaultSafeWallDistance = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.DefaultSafeWallDistance) +
                            SetTo + newValue;
    }

    public void AdvancedAgentMaxSpeed(string value)
    {
        var newValue = Params.CurrentDefaults.AgentMaxspeed;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.AgentMaxspeed = newValue;
        string memberName = MemberInfoGetting.GetMemberName(() => Params.Current.AgentMaxspeed);

        if (Params.Current.AgentMaxspeed > Params.Maximum.AgentMaxspeed)
        {
            if (_showAdvancedSettings)
            {
                ShowGeneralDialog(
                    OOR1 + memberName + OOR15 + Params.GetModeTxt() + OOR2 + Params.Minimum.AgentMaxspeed +
                    OOR3 + Params.Maximum.AgentMaxspeed + OOR4, OOR5);
            }
            else
            {
                Params.Current.AgentMaxspeed = Params.Maximum.AgentMaxspeed;
                VariablePresetHandler.Instance.UpdateAllFields();
            }
        }
        else if (Params.Current.AgentMaxspeed < Params.Minimum.AgentMaxspeed)
        {
            if (_showAdvancedSettings)
            {
                ShowGeneralDialog(
                    OOR1 + memberName + OOR15 + Params.GetModeTxt() + OOR2 + Params.Minimum.AgentMaxspeed +
                    OOR3 + Params.Maximum.AgentMaxspeed + OOR4, OOR5);
            }
            else
            {
                Params.Current.AgentMaxspeed = Params.Minimum.AgentMaxspeed;
                VariablePresetHandler.Instance.UpdateAllFields();
            }
        }

        _consoleText.text = memberName + SetTo + newValue;
    }
    public void AdvancedAgentMaxSpeedDeviation(string value)
    {
        var newValue = Params.CurrentDefaults.AgentMaxspeedDeviation;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.AgentMaxspeedDeviation = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.AgentMaxspeedDeviation) +
                            SetTo + newValue;
    }
    public void AdvancedAgentFollowerLeadingSpeedFactor(string value)
    {
        var newValue = Params.CurrentDefaults.AgentFollowerInFrontSpeed;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.AgentFollowerInFrontSpeed = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.AgentFollowerInFrontSpeed) + SetTo + newValue;
    }
    public void AdvancedAgentRadius(string value)
    {
        var newValue = Params.CurrentDefaults.AgentRadius;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.AgentRadius = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.AgentRadius) + SetTo +
                            newValue;
    }
    public void AdvancedAgentRadiusDeviation(string value)
    {
        var newValue = Params.CurrentDefaults.AgentRadiusDeviation;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.AgentRadiusDeviation = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.AgentRadiusDeviation) +
                            SetTo + newValue;
    }
    public void AdvancedAgentWeight(string value)
    {
        var newValue = Params.CurrentDefaults.AgentWeight;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.AgentWeight = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.AgentWeight) + SetTo +
                            newValue;
    }
    public void AdvancedAgentWeightDeviation(string value)
    {
        var newValue = Params.CurrentDefaults.AgentWeightDeviation;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.AgentWeightDeviation = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.AgentWeightDeviation) +
                            SetTo + newValue;
    }
    public void AdvancedSpeedThreshold(string value)
    {
        var newValue = Params.CurrentDefaults.SpeedFlowThreshold;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.SpeedFlowThreshold = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.SpeedFlowThreshold) +
                            SetTo + newValue;
    }
    public void AdvancedDensityThreshold(string value)
    {
        var newValue = Params.CurrentDefaults.DensityFlowThreshold;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.DensityFlowThreshold = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.DensityFlowThreshold) +
                            SetTo + newValue;
    }
    public void AdvancedCongestionRadiusThreshold(string value)
    {
        var newValue = Params.CurrentDefaults.CongestionRadius;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.CongestionRadius = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.CongestionRadius) +
                            SetTo + newValue;
    }

    public void AdvancedCA_Disabled(bool value)
    {
        Consts.RVODisabled = value;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Consts.RVODisabled) +
                            SetTo + value;

        foreach (var field in CAFields)
            field.interactable = !Consts.RVODisabled;
    }
    public void AdvancedCA_MaxNeighbours(string value)
    {
        var newValue = Params.CurrentDefaults.RvoMaxNeighbours;

        if (!string.IsNullOrEmpty(value))
            int.TryParse(value, out newValue);

        Params.Current.RvoMaxNeighbours = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.RvoMaxNeighbours) +
                            SetTo + newValue;
    }
    public void AdvancedCA_TimeH_Agent(string value)
    {
        var newValue = Params.CurrentDefaults.RvoTimeHorizonAgent;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.RvoTimeHorizonAgent = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.RvoTimeHorizonAgent) +
                            SetTo + newValue;
    }
    public void AdvancedCA_TimeH_Obstacles(string value)
    {
        var newValue = Params.CurrentDefaults.RvoTimeHorizonObstacle;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.RvoTimeHorizonObstacle = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.RvoTimeHorizonObstacle) +
                            SetTo + newValue;
    }
    public void AdvancedHF_Enabled(bool value)
    {
        Consts.HFEnabled = value;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Consts.HFEnabled) +
                            SetTo + value;

        foreach (var field in HFFields)
            field.interactable = Consts.HFEnabled;
    }
    public void AdvancedHF_Fatigue(string value)
    {
        var newValue = Params.CurrentDefaults.HFFatigue;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.HFFatigue = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.HFFatigue) +
                            SetTo + newValue;
    }
    public void AdvancedHF_HeightLimit(string value)
    {
        var newValue = Params.CurrentDefaults.HFHeightLimit;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.HFHeightLimit = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.HFHeightLimit) +
                            SetTo + newValue;
    }
    public void AdvancedHF_InitialVelocity(string value)
    {
        var newValue = Params.CurrentDefaults.HFIntialVelocity;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.HFIntialVelocity = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.HFIntialVelocity) +
                            SetTo + newValue;
    }
    public void AdvancedHF_SteadySpeed(string value)
    {
        var newValue = Params.CurrentDefaults.HFSteadySpeed;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.HFSteadySpeed = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.HFSteadySpeed) +
                            SetTo + newValue;
    }

    public void AdvancedRTRadius(string value)
    {
        var newValue = Params.CurrentDefaults.Radius_RT;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.Radius_RT = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.Radius_RT) +
                            SetTo + newValue;
    }
    public void AdvancedRTBeta1(string value)
    {
        var newValue = Params.CurrentDefaults.Beta1;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.Beta1 = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.Beta1) +
                            SetTo + newValue;
    }
    public void AdvancedRTBeta2(string value)
    {
        var newValue = Params.CurrentDefaults.Beta2;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.Beta2 = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.Beta2) +
                            SetTo + newValue;
    }

    public void AdvancedDestinationTheta1(string value)
    {
        var newValue = Params.CurrentDefaults.DestinationLevelTheta1;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.DestinationLevelTheta1 = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.DestinationLevelTheta1) +
                            SetTo + newValue;
    }
    public void AdvancedDestinationTheta2(string value)
    {
        var newValue = Params.CurrentDefaults.DestinationLevelTheta2;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.DestinationLevelTheta2 = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.DestinationLevelTheta2) +
                            SetTo + newValue;
    }

    public void ReactionTimeMethod(int value)
    {
        Params.Current.ReactionMethod = (Def.ReactionMethod)value;

        for (int i = 0; i < ReactionTimePages.Count; i++)
            ReactionTimePages[i].SetActive(i == value);

        switch ((Def.ReactionMethod)value)
        {
            case Def.ReactionMethod.WeibullHazard:
            case Def.ReactionMethod.ExpHazard:
                ReactionTimePages[3].SetActive(true);
                break;
            case Def.ReactionMethod.ExpDist:
            case Def.ReactionMethod.None:
                ReactionTimePages[3].SetActive(false);
                break;
            default:
                throw new ArgumentOutOfRangeException("value", value, null);
        }
    }
    public void ReactionTimeExpHazardMu(string value)
    {
        var newValue = Params.CurrentDefaults.Exp_Hazard_Mu;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.Exp_Hazard_Mu = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.Exp_Hazard_Mu) +
                            SetTo + newValue;
    }
    public void ReactionTimeExpHazardLambda(string value)
    {
        var newValue = Params.CurrentDefaults.Exp_Hazard_Lambda;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.Exp_Hazard_Lambda = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.Exp_Hazard_Lambda) +
                            SetTo + newValue;
    }
    public void ReactionTimeWeibullHazardMu(string value)
    {
        var newValue = Params.CurrentDefaults.Weibull_Hazard_Mu;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.Weibull_Hazard_Mu = newValue;
        string memberName = MemberInfoGetting.GetMemberName(() => Params.Current.Weibull_Hazard_Mu);

        if (Params.Current.Weibull_Hazard_Mu > Params.Maximum.Weibull_Hazard_Mu || Params.Current.Weibull_Hazard_Mu < Params.Minimum.Weibull_Hazard_Mu)
        {
            ShowGeneralDialog(
                OOR1 + memberName + OOR15 + Params.GetModeTxt() + OOR2 + Params.Minimum.Weibull_Hazard_Mu + OOR3 + Params.Maximum.Weibull_Hazard_Mu + OOR4, OOR5);
        }

        _consoleText.text = memberName + SetTo + newValue;
    }
    public void ReactionTimeWeibullHazardNu(string value)
    {
        var newValue = Params.CurrentDefaults.Weibull_Hazard_Nu;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.Weibull_Hazard_Nu = newValue;
        string memberName = MemberInfoGetting.GetMemberName(() => Params.Current.Weibull_Hazard_Nu);

        if (Params.Current.Weibull_Hazard_Nu > Params.Maximum.Weibull_Hazard_Nu || Params.Current.Weibull_Hazard_Nu < Params.Minimum.Weibull_Hazard_Nu)
        {
            ShowGeneralDialog(
                OOR1 + memberName + OOR15 + Params.GetModeTxt() + OOR2 + Params.Minimum.Weibull_Hazard_Nu + OOR3 + Params.Maximum.Weibull_Hazard_Nu + OOR4, OOR5);
        }

        _consoleText.text = memberName + SetTo + newValue;
    }
    public void ReactionTimeWeibullHazardLambda(string value)
    {
        var newValue = Params.CurrentDefaults.Weibull_Hazard_Lambda;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.Weibull_Hazard_Lambda = newValue;
        string memberName = MemberInfoGetting.GetMemberName(() => Params.Current.Weibull_Hazard_Lambda);

        if (Params.Current.Weibull_Hazard_Lambda > Params.Maximum.Weibull_Hazard_Lambda || Params.Current.Weibull_Hazard_Lambda < Params.Minimum.Weibull_Hazard_Lambda)
        {
            ShowGeneralDialog(
                OOR1 + memberName + OOR15 + Params.GetModeTxt() + OOR2 + Params.Minimum.Weibull_Hazard_Lambda + OOR3 + Params.Maximum.Weibull_Hazard_Lambda + OOR4, OOR5);
        }

        _consoleText.text = memberName + SetTo + newValue;
    }
    public void ReactionTimeCutoffMultiplier(string value)
    {
        var newValue = Params.CurrentDefaults.VarianceMultiplierCutoff;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Params.Current.VarianceMultiplierCutoff = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.VarianceMultiplierCutoff) +
                            SetTo + newValue;
    }
    public void ReactionTimeMaxRetries(string value)
    {
        var newValue = Params.CurrentDefaults.CutoffMaxRetries;

        if (!string.IsNullOrEmpty(value))
            int.TryParse(value, out newValue);

        Params.Current.CutoffMaxRetries = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.CutoffMaxRetries) +
                            SetTo + newValue;
    }

    public void SimulationNumberOfRuns(string value)
    {
        var newValue = Consts.NumberOfRunsDefault;

        if (!string.IsNullOrEmpty(value))
            int.TryParse(value, out newValue);

        SimulationController.Instance.NumberOfRuns = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => SimulationController.Instance.NumberOfRuns) +
                            SetTo + newValue;
    }
    public void SimulationCancelAfter(string value)
    {
        var newValue = Consts.CancelAfterSecDefault;

        if (!string.IsNullOrEmpty(value))
            int.TryParse(value, out newValue);

        SimulationController.Instance.CancelAfter = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => SimulationController.Instance.CancelAfter) +
                            SetTo + newValue;
    }
    public void SimulationRecordingEnabled(bool value)
    {
        Params.Current.RecordingEnabled = !value;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.RecordingEnabled) +
                            SetTo + !value;
    }
    public void ExportDataPositions(bool value)
    {
        Consts.ExportDataPositions = value;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Consts.ExportDataPositions) +
                            SetTo + value;
    }
    public void ExportDataEvacTimes(bool value)
    {
        Consts.ExportDataEvacTimes = value;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Consts.ExportDataEvacTimes) +
                            SetTo + value;
    }
    public void ExportDataGateShares(bool value)
    {
        Consts.ExportDataGateShares = value;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Consts.ExportDataGateShares) +
                            SetTo + value;
    }
    public void ExportReactionTimes(bool value)
    {
        Consts.ExportReactionTimes = value;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Consts.ExportReactionTimes) +
                            SetTo + value;
    }

    public void AgentsUpdateType()
    {
        bool isDynamic = _agentTypeDropdown.value == (int)Def.DistributionType.Dynamic;
        bool isGroup = _agentGroupDropdown.value == (int)Def.GroupStatus.Yes;

        agentSecFields[0].interactable = isDynamic;
        agentLimitField.interactable = isDynamic;
        agentGroupSize.interactable = isGroup;


        for (int i = 1; i < 6; i++)
            agentInitialFields[i].interactable = isGroup;

        for (int i = 1; i < 6; i++)
            agentSecFields[i].interactable = isGroup && isDynamic;
    }
    public void AgentsLoadTimetable()
    {
        StartCoroutine(OpenTimetable(_resultPathTimetable));
    }

    public void UpdateAgentGenerationType(int value)
    {
        string fn = "";

        AgentGenerationType = (Def.AgentPlacement)value;

        switch (AgentGenerationType)
        {
            case Def.AgentPlacement.Circle:
                fn = "Agents > Generate in Circle";
                break;
            case Def.AgentPlacement.Rectangle:
                fn = "Agents > Generate in a Square";
                break;
            case Def.AgentPlacement.Room:
                fn = "Agents > Generate Uniformly in Room";
                break;
            default:
                throw new ArgumentOutOfRangeException("Agent generation type out of range.");
        }
        _consoleText.text = fn;
    }

    public void TicketGateQueueAroundCorners(bool value)
    {
        Params.Current.QueueAroundCorners = value;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Params.Current.QueueAroundCorners) + SetTo + value;
    }
    public void TrainTicketWidth()
    {
        var newValue = 1.0f;

        if (!string.IsNullOrEmpty(TicketGateWidthField.text))
            float.TryParse(TicketGateWidthField.text, out newValue);

        newValue = Mathf.RoundToInt(newValue * 10f) / 10f;

        if (newValue < 0.7f) newValue = 0.7f;

        TicketGateWidthField.text = newValue.ToString();

        Consts.TicketGateWidth = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Consts.TicketGateWidth) + SetTo + newValue;
    }
    public void TrainLoadTimetable()
    {
        if (FindObjectsOfType<TrainInfo>().Length < 1)
            ShowGeneralDialog("You haven't got any trains in the environment yet.", "No trains detected.");
        else
            StartCoroutine(OpenTrainTimetable(_resultPathTimetable));
    }
    public void TrainClearTimetable()
    {
        //Debug.Log("Clear Train Timetable");
        TimetableStatusTextTrains.GetComponent<Text>().text = LocalizationManager.GetTermTranslation("Transit Timetable") + " " + LocalizationManager.GetTermTranslation("Disabled");
        TimetableStatusTextTrains.GetComponent<Text>().font = FindObjectOfType<ReplaceFonts>().reg;
    }
    public void DistributionLoadTable()
    {
        if (FindObjectsOfType<AgentDistInfo>().Length < 1)
            ShowGeneralDialog("You haven't got any agent distributions in the environment yet.", "No agent distributions detected.");
        else
            StartCoroutine(OpenDistributionTable(_resultPathTimetable));
    }
    public void DistributionClearTable()
    {
        Debug.Log("Clear Distribution Table");
        TableStatusDistribution.GetComponent<Text>().text = LocalizationManager.GetTermTranslation("Distribution Table") + " " + LocalizationManager.GetTermTranslation("Disabled");
        TableStatusDistribution.GetComponent<Text>().font = FindObjectOfType<ReplaceFonts>().reg;
    }

    public void LegendMaxUtil(string value)
    {
        var newValue = -1f;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Consts.HeatmapMaximums[(int)Def.HeatmapType.Utilization] = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Consts.HeatmapMaximums) +
                            SetTo + newValue;

        if (SimulationController.Instance.ActiveHeatmapType == Def.HeatmapType.Utilization)
            SimulationController.Instance.UpdateHeatmap(_smoothHeatmap);
    }
    public void LegendMaxMaxDensity(string value)
    {
        var newValue = -1f;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Consts.HeatmapMaximums[(int)Def.HeatmapType.MaxDensity] = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Consts.HeatmapMaximums) +
                            SetTo + newValue;

        if (SimulationController.Instance.ActiveHeatmapType == Def.HeatmapType.MaxDensity)
            SimulationController.Instance.UpdateHeatmap(_smoothHeatmap);
    }
    public void LegendMaxAvgDensity(string value)
    {
        var newValue = -1f;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Consts.HeatmapMaximums[(int)Def.HeatmapType.AverageDensity] = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Consts.HeatmapMaximums) +
                            SetTo + newValue;

        if (SimulationController.Instance.ActiveHeatmapType == Def.HeatmapType.AverageDensity)
            SimulationController.Instance.UpdateHeatmap(_smoothHeatmap);
    }
    public void LegendMaxAvgSpeed(string value)
    {
        var newValue = -1f;

        if (!string.IsNullOrEmpty(value))
            float.TryParse(value, out newValue);

        Consts.HeatmapMaximums[(int)Def.HeatmapType.AverageSpeed] = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Consts.HeatmapMaximums) +
                            SetTo + newValue;

        if (SimulationController.Instance.ActiveHeatmapType == Def.HeatmapType.AverageSpeed)
            SimulationController.Instance.UpdateHeatmap(_smoothHeatmap);
    }

    internal SimulationParams GetSimulationParameters()
    {
        FunctionChoice fchoice = Params.Current.FunctionType == Def.Function.Utility
            ? FunctionChoice.Utility
            : FunctionChoice.Regret;

        FunctionFactory factory = null;

        if (Params.Current.InterationType == Def.Interaction.Enabled)
            factory = new WithInteractionFactory();
        else
            factory = new WithoutInteractionFactory();

        GateChoiceMethod gateChoiceFunction = factory.GetFunction(fchoice);

        ClassChoice class_Choice;

        if (Params.Current.ClassType == Def.Class.Single)
        {
            class_Choice = new SingleClass();
            class_Choice.AddUtilities(1, Params.Current.UtilitiesClass1);
        }
        else
        {
            class_Choice = new DoubleClass();
            class_Choice.AddUtilities(1, Params.Current.UtilitiesClass1);
            class_Choice.AddUtilities(2, Params.Current.UtilitiesClass2);
        }

        if (gateChoiceFunction != null && class_Choice != null)
        {
            SimulationParams simParams = new SimulationParams(gateChoiceFunction, class_Choice);
            return simParams;
        }

        return null;
    }

    public void LevelSetLevel(int level)
    {
        Create.Instance.displayAllLevels = false;
        string fn = "Level > Select Level " + level;
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedLevel(level);
    }
    public void LevelRemoveLevel()
    {
        string fn;
        if (NumLevels > 0)
        {
            fn = "Level > Remove Level";
            foreach (GameObject g in _levelButtons)
                g.GetComponent<RectTransform>().anchoredPosition3D -= new Vector3(0f, -Consts.ButtonDiff, 0f);

            GameObject button = _levelButtons[_levelButtons.Count - 1];
            _levelButtons.Remove(button);
            button.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(-1000f, 0, 0);
            NumLevels--;
            Create.Instance.RemoveLevel();
        }
        else
            fn = "There are no more levels to remove.";

        _consoleText.text = fn;
    }
    public void LevelAddLevel()
    {
        string fn;
        if (NumLevels < levelButtonStorage.Count)
        {
            fn = "Level > Add Level";
            Vector3 defaultButtonPos = new Vector3(-64f, -5.7f, 0f);
            Vector3 defaultButtonScale = new Vector3(1f, 0.05f, 1f);
            foreach (GameObject g in _levelButtons)
                g.GetComponent<RectTransform>().anchoredPosition3D -= new Vector3(0f, Consts.ButtonDiff, 0f);

            GameObject button = levelButtonStorage[NumLevels];
            button.GetComponent<RectTransform>().anchoredPosition3D = defaultButtonPos;
            button.GetComponent<RectTransform>().localScale = defaultButtonScale;
            _levelButtons.Add(button);

            NumLevels++;
            Create.Instance.AddLevel();
        }
        else
        {
            fn = "You have reached the maximum number of levels.";
        }
        //Debug.Log(fn);
        _consoleText.text = fn;
    }

    public void CreateNone()
    {
        const string fn = "Create > None";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.None);
    }
    public void CreateWall()
    {
        const string fn = "Create > Wall";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.Wall);
    }
    public void CreateGate()
    {
        const string fn = "Create > Gate";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.Gate);
    }
    public void CreateBarrier()
    {
        string fn = "Create > Barrier";
        MenuItemClicked(fn);
        Create.Instance.lowBarricade = false;
        Create.Instance.ChangeSelectedObject((int)Def.Object.Barricade);
    }
    public void CreateBarrierLow()
    {
        string fn = "Create > Barrier";
        MenuItemClicked(fn);
        Create.Instance.lowBarricade = true;
        Create.Instance.ChangeSelectedObject((int)Def.Object.Barricade);
    }
    public void CreatePoleObstacle()
    {
        string fn = "Create > Obstacle";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.PoleObstacle);
    }
    public void CreateCounter()
    {
        const string fn = "Create > Counter";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.Counter);
    }
    public void CreateAgents()
    {
        string fn = "Create > Agents";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.Agents);
    }
    public void CreateAvoidSquare()
    {
        string fn = "Create > Avoid Square";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.AvoidSquare);
    }
    public void CreateAvoidCircle()
    {
        string fn = "Create > Avoid Circle";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.AvoidCircle);
    }
    public void CreateGateObstruction()
    {
        const string fn = "Create > Gate Obstruction";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.GateObstruction);
    }
    public void CreateStairObstruction()
    {
        const string fn = "Create > Stair Obstruction";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.StairObstruction);
    }
    public void CreateDangerInRoom()
    {
        const string fn = "Create > Danger In Room";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.DangerInRoom);
    }
    public void CreateFireSource()
    {
        const string fn = "Create > Fire Source";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.FireSource);
    }
    public void CreateTrain()
    {
        const string fn = "Create > Train";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.Train);
    }
    public void CreateTram()
    {
        const string fn = "Create > Tram";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.Tram);
    }
    public void CreateTicketGate()
    {
        const string fn = "Create > TicketGate1";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.TicketGate);
    }
    public void CreatePrefabrication(int type)
    {
        string fn = "Create > Prefabrication: " + type.ToString();
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.Prefabrication);
        DialogPrefabrications.Instance.Active(true, type);
    }

    public void StairsHalfLanding()
    {
        string fn = "Stairs > Half Landing";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.StairsHalfLanding);
        FindObjectOfType<StairTaskbar>().UpdateStairType(Def.StairType.HalfLanding);
    }
    public void StairsStraight()
    {
        string fn = "Stairs > Straight";
        MenuItemClicked(fn);
        Create.Instance.ChangeSelectedObject((int)Def.Object.StairsStraight);
        FindObjectOfType<StairTaskbar>().UpdateStairType(Def.StairType.Straight);
    }

    public void FileNew()
    {
        string fn = "File > New";
        MenuItemClicked(fn);
        SwitchBackToMainCamera();
        SetDialogStatus(true, DialogNewFile);
    }
    public void FileExit()
    {
        string fn = "File > Exit";
        MenuItemClicked(fn);
        SetDialogStatus(true, DialogQuit);
    }
    public void FileImport()
    {
        string fn = "File > Import";
        MenuItemClicked(fn);
        StartCoroutine(ImportFile(_resultPathImport));
    }
    public void FileExportData()
    {
        string fn = "File > Export Data";
        MenuItemClicked(fn);
        StartCoroutine(ExportDataDialog(_resultPathExportData));
        SetDialogStatus(true);
    }
    public void FileExportHeatmap()
    {
        string fn = "File > Export Heatmap Exact Data";
        MenuItemClicked(fn);
        SimulationController.Instance.OutputHeatmaps();
    }
    public void FileExportHeatmapScreenshots()
    {
        string fn = "File > Export Heatmap Screenshots";
        MenuItemClicked(fn);
        SimulationController.Instance.OutputHeatmapScreenshots();
    }
    public void FileOpen()
    {
        string fn = "File > Open";
        MenuItemClicked(fn);
        SwitchBackToMainCamera();
        StartCoroutine(OpenFile(_resultPath));
        SetDialogStatus(true);
    }
    public void FileSave()
    {
        string fn = "File > Save";
        MenuItemClicked(fn);
        SwitchBackToMainCamera();
        TopDownView.Instance.SetTopDown(false);
        SaveFile();
    }
    public void FileSaveAs()
    {
        string fn = "File > Save As";
        MenuItemClicked(fn);
        SwitchBackToMainCamera();
        TopDownView.Instance.SetTopDown(false);
        StartCoroutine(SaveFile(_resultPathSave));
        SetDialogStatus(true);
    }

    public void EditUndo()
    {
        string fn = "Edit > Undo";
        MenuItemClicked(fn);
        Create.Instance.Undo();
    }
    public void EditDuplicateLevel()
    {
        string fn = "Edit > Duplicate Level";
        MenuItemClicked(fn);
        Create.Instance.DuplicateLevel();
    }
    public void EditToggleGridSnapping()
    {
        string fn = "Edit > Toggle Grid Snapping";
        MenuItemClicked(fn);
        _smallerGridSnap = !_smallerGridSnap;
        GridSnapTick.SetActive(_smallerGridSnap);
        Create.Instance.GridSnapping(_smallerGridSnap);
    }
    public void EditToggleWallSnapping()
    {
        string fn = "Edit > Toggle Wall Snapping";
        MenuItemClicked(fn);

        _wallSnapEnabled = !_wallSnapEnabled;

        WallSnapTick.SetActive(_wallSnapEnabled);
        Create.Instance.WallSnapping(_wallSnapEnabled);
    }
    public void EditChangeGranularity()
    {
        const string fn = "Edit > Change Granularity";
        MenuItemClicked(fn);
    }

    public void ViewToggleGridDisplay()
    {
        const string fn = "View > Toggle Grid Display";
        MenuItemClicked(fn);

        GridDisplayEnabled = !GridDisplayEnabled;
        NetworkServer.Instance.SendDisplayUpdate(Create.Instance.displayAllLevels, GridDisplayEnabled, Create.Instance.SelectedLevel);

        foreach (GridDisplay g in FindObjectsOfType<GridDisplay>())
            g.ToggleGridDisplay(GridDisplayEnabled);
    }
    public void ViewDisplayAllLevels()
    {
        string fn = "View > Display All Levels";
        MenuItemClicked(fn);

        Create.Instance.DisplayAllLevels();
    }
    public void ViewToggleTopDownView()
    {
        string fn = "View > Toggle Top Down View";
        MenuItemClicked(fn);
        TopDownView.Instance.ToggleTopDown();
    }
    public void ViewToggleImageDisplay()
    {
        string fn = "View > Toggle Image Display";
        MenuItemClicked(fn);
        ImagePlane.Instance.ToggleDisplay();
    }
    public void ViewToggleGateShares()
    {
        string fn = "View > Toggle Gate Shares";
        MenuItemClicked(fn);
        SimulationController.Instance.ToggleGateShares();
    }

    public void ViewToggleAdvancedSettings()
    {
        _showAdvancedSettings = !_showAdvancedSettings;
        string fn = "View > Academic Settings " + (_showAdvancedSettings ? "Enabled" : "Disabled");
        MenuItemClicked(fn);
        AdvancedSettingsTick.SetActive(_showAdvancedSettings);
        if (_showAdvancedSettings)
            ShowGeneralDialog(
                LocalizationManager.GetTermTranslation("Academic Dialog"),
                LocalizationManager.GetTermTranslation("Warning"));
        else
            CloseActiveTaskbar();

        SetActiveAdvancedItems(_showAdvancedSettings);
    }

    private void SetActiveAdvancedItems(bool showAdvancedSettings)
    {
        foreach (var item in advancedSettingsObjects)
            item.SetActive(showAdvancedSettings);
    }

    public void SaveParametersToggle(bool value)
    {
        saveParemeters = value;
    }

    public void Heatmap(int type)
    {
        string fn = "Heatmap Type: " + type;
        MenuItemClicked(fn);
        SimulationController.Instance.ToggleHeatmap((Def.HeatmapType)type, _smoothHeatmap);
    }
    public void HeatmapSmooth(bool smooth)
    {
        string fn = "Heatmap > Smooth: " + smooth;
        MenuItemClicked(fn);
        _smoothHeatmap = smooth;
        SimulationController.Instance.UpdateHeatmap(_smoothHeatmap);
    }

    public void ToolsConnectToHoloLens()
    {
        string fn = "Tools > Connect To HoloLens";
        MenuItemClicked(fn);
        if (Consts.holoLensEnabled)
            FindObjectOfType<NetworkServer>()?.ToggleStatus();
        else
            ShowGeneralDialog("Unfortunately your product's licence (Standard) does not include support for HoloLens 2.", "Insufficient Permissions");
    }
    public void AboutAboutThisProgram()
    {
        string fn = "About > About This Program";
        MenuItemClicked(fn);
        ShowGeneralDialog(Strings.AboutText, "About Pedestride Multimodal");
    }

    public void HelpCPMManual()
    {
        string fn = "Help > User Manual";
        Debug.Log(fn);
        _consoleText.text = fn;
        CloseAllMenus();
        Application.OpenURL("http://help.pedestride.com/");
    }
    public void HelpKeyboardControls()
    {
        string fn = "Help > Keyboard Controls";
        Debug.Log(fn);
        _consoleText.text = fn;
        CloseAllMenus();
        Application.OpenURL("https://pedestride.s3.ap-southeast-2.amazonaws.com/KeyboardShortcuts.html");
    }
    public void HelpChangeLanguage()
    {
        string fn = "About > About This Program";
        MenuItemClicked(fn);
        DialogLanguage.SetActive(true);
    }

    public void SimulationRunSimulation()
    {
        string fn = "Simulation > Run Once";
        MenuItemClicked(fn);
        SimulationController.Instance.RunSimulation();
    }
    public void SimulationCancelSimulation()
    {
        string fn = "Simulation > Cancel";
        MenuItemClicked(fn);
        SimulationController.Instance.CancelSimulation();
    }
    public void SimulationCompleteSimulation()
    {
        string fn = "Simulation > Force Complete";
        MenuItemClicked(fn);
        SimulationController.Instance.CompleteSimulation();
    }

    public void CompleteSimButton(bool enabled)
    {
        if (CompleteButton != null)
            CompleteButton.interactable = enabled;
    }

    public void PlaybackRestart()
    {
        string fn = "Playback > Restart";
        MenuItemClicked(fn);
        SimulationController.Instance.ReplaySimulation();
    }
    public void PlaybackPause()
    {
        string fn = "Playback > Pause";
        MenuItemClicked(fn);
        SimulationController.Instance.TogglePause();
    }
    public void PlaybackDelete()
    {
        string fn = "Playback > Delete";
        MenuItemClicked(fn);
        SimulationController.Instance.DeletePlayback();
    }

    public void SpeedResetSpeed()
    {
        string fn = "Speed > Reset Speed";
        MenuItemClicked(fn);
        FindObjectOfType<Slider>().value = 1f;
    }
    public void SpeedSetSpeed(float speed)
    {
        speed = speed * speed; // Gives a wider range for the UI.
        string fn = "Speed > Set Speed: " + speed.ToString(Str.DecimalFormat);
        MenuItemClicked(fn);
        Consts.SpeedMultiplier = speed;
        Consts.SpeedMultiplier = Consts.SpeedMultiplier * 30f / Params.Current.UiUpdateCycle;
    }

    public void CameraReset()
    {
        string fn = "Camera > Reset Camera";
        MenuItemClicked(fn);
        Create.Instance.ResetCamera();
    }

    public void NoAnimations(bool value)
    {
        string fn = "Camera > No Animations";
        MenuItemClicked(fn);
        SimulationController.Instance.ToggleAnimations(value);
    }

    public void DangerWeightModifier(string value)
    {
        int newValue = Consts.DangerWeightModifierDefault;

        if (!string.IsNullOrEmpty(value))
            int.TryParse(value, out newValue);

        Consts.DangerWeightModifier = newValue;
        _consoleText.text = MemberInfoGetting.GetMemberName(() => Consts.DangerWeightModifier) + SetTo + newValue;
    }

    //Quit Dialog
    public void QuitDialogCancel()
    {
        SetDialogStatus(false, DialogQuit);
    }
    public void QuitApplication()
    {
        Debug.Log("Application quit successfully.");
        Application.Quit();
    }
    public void QuitGeneralDialog()
    {
        SetDialogStatus(false, DialogGeneral);
    }

    //New Model Dialog
    public void NewModelOK()
    {
        InputField modelWidthField = GameObject.Find("ModelWidthInput").GetComponent<InputField>();
        InputField modelLengthField = GameObject.Find("ModelLengthInput").GetComponent<InputField>();
        InputField modelHeightField = GameObject.Find("ModelHeightInput").GetComponent<InputField>();

        int newWidth = 49;
        int newLength = 49;
        float newHeight = 2.5f;

        if (modelWidthField.text != "" && !int.TryParse(modelWidthField.text, out newWidth))
            Debug.Log("Could not convert " + modelWidthField.text + " into a width.");

        if (modelLengthField.text != "" && !int.TryParse(modelLengthField.text, out newLength))
            Debug.Log("Could not convert " + modelLengthField.text + " into a length.");

        if (modelHeightField.text != "" && !float.TryParse(modelHeightField.text, out newHeight))
            Debug.Log("Could not convert " + modelHeightField.text + " into a height.");

        SimulationController.Instance.CancelSimulation();

        Create.Instance.ChangeLevelHeight(newHeight);
        Create.Instance.SetGridSize(newWidth, newLength);
        Create.Instance.ClearAll();
        GroundHelper.Instance.MakeGround();
        LevelAddLevel();

        SetDialogStatus(false, DialogNewFile);

        //Get the window handle.
        IntPtr windowPtr = FindWindow(null, _windowNameCurrent);
        SetWindowText(windowPtr, _windowNameDefault);
    }

    //Agents Dialog
    public void DialogWithFieldsOpen<T>(T info, GameObject editingPrefab = null)
    {
        if (editingPrefab != null)
            _dialogFieldsEditingObject = editingPrefab;

        if (_dialogFieldsEditingObject == null)
            return;

        SetDialogStatus(true, DialogWithFields);

        // Set buttons to blank.
        timetableButton.GetComponentInChildren<Text>().text = "-";
        timetableButton.interactable = false;
        groupsButton.GetComponentInChildren<Text>().text = "-";
        groupsButton.interactable = false;
        designatedButton.GetComponentInChildren<Text>().text = "-";
        designatedButton.interactable = false;
        colorButton_fieldsDialog.GetComponentInChildren<Text>().text = "-";
        colorButton_fieldsDialog.interactable = false;

        if (typeof(T) == typeof(AgentDistInfo))
        {
            _dialogFieldsEditingType = Def.DialogType.AgentPrefab;
            DialogWithFieldsOpenAgentDist();
        }
        else if (typeof(T) == typeof(ThreatInfo))
        {
            _dialogFieldsEditingType = Def.DialogType.ThreatInfo;
            #region ThreatInfo

            ThreatInfo ti = info as ThreatInfo;

            if (ti.LevelId < 0)
            {
                Debug.Log("Try clicking again.");
                return;
            }

            System.Diagnostics.Debug.Assert(ti != null, "ti != null");

            DialogTitle.text = "Threat Parameters";

            DialogField1.text = string.Empty + ti.ThreatType;
            DialogField1.interactable = false;
            DialogText1.text = "Type";

            DialogField2.text = ti.StartTime.ToString();
            DialogField2.interactable = true;
            DialogText2.text = "Start Time";

            DialogField3.text = ti.DurationString;
            DialogField3.interactable = true;
            DialogText3.text = "Duration";

            ColorBlock block = colorButton_fieldsDialog.colors;
            block.normalColor = Consts.defaultButtonColor;
            colorButton_fieldsDialog.colors = block;
            #endregion
        }
        else if (typeof(T) == typeof(WaitPointInfo))
        {
            _dialogFieldsEditingType = Def.DialogType.WaitPoint;
            #region WaitPointInfo

            WaitPointInfo wp = info as WaitPointInfo;

            System.Diagnostics.Debug.Assert(wp != null, "wp != null");

            DialogTitle.text = "Wait Point Parameters";

            DialogField1.text = wp.interest.ToString();
            DialogField1.interactable = true;
            DialogText1.text = "Interest";

            DialogField2.text = wp.waitTime.ToString();
            DialogField2.interactable = true;
            DialogText2.text = "Wait Time";

            DialogField3.text = wp.wonderTime.ToString();
            DialogField3.interactable = true;
            DialogText3.text = "Wonder Time";

            ColorBlock block = colorButton_fieldsDialog.colors;
            block.normalColor = Consts.defaultButtonColor;
            colorButton_fieldsDialog.colors = block;
            #endregion
        }
        else if (typeof(T) == typeof(FireSource))
        {
            _dialogFieldsEditingType = Def.DialogType.FireSource;
            #region FireSource

            FireSource fireSource = info as FireSource;

            System.Diagnostics.Debug.Assert(fireSource != null, "fireSource != null");

            DialogTitle.text = "Fire Source Parameters";

            DialogField1.text = fireSource.flowRate.ToString();
            DialogField1.interactable = true;
            DialogText1.text = "Flow Rate";

            DialogField2.text = fireSource.velocity.ToString();
            DialogField2.interactable = true;
            DialogText2.text = "Velocity";

            DialogField3.text = fireSource.fireHeight.ToString();
            DialogField3.interactable = true;
            DialogText3.text = "Height";

            ColorBlock block = colorButton_fieldsDialog.colors;
            block.normalColor = Consts.defaultButtonColor;
            colorButton_fieldsDialog.colors = block;
            #endregion
        }
        else if (typeof(T) == typeof(TrainInfo))
        {
            _dialogFieldsEditingType = Def.DialogType.TrainInfo;
            #region Train/Tram

            TrainInfo trainInfo = info as TrainInfo;

            System.Diagnostics.Debug.Assert(trainInfo != null, "trainInfo != null");

            DialogTitle.text = "Train/Tram Parameters, ID: " + trainInfo.ObjectId;

            DialogField1.text = trainInfo.arrivalSpeed.ToString();
            DialogField1.interactable = true;
            DialogText1.text = "Arrival Speed (s)";

            DialogField2.text = trainInfo.trainGen.numCarriages.ToString();
            DialogField2.interactable = true;
            DialogText2.text = "Number Carriages";

            DialogField3.text = trainInfo.trainGen.doorWidth.ToString();
            DialogField3.interactable = true;
            DialogText3.text = "Door Width";

            groupsButton.GetComponentInChildren<Text>().text = "Swap";
            groupsButton.interactable = false;
            designatedButton.GetComponentInChildren<Text>().text = "Carriages";
            designatedButton.interactable = true;
            colorButton_fieldsDialog.interactable = true;
            colorButton_fieldsDialog.GetComponentInChildren<Text>().text = "Color";

            ColorBlock block = colorButton_fieldsDialog.colors;
            block.normalColor = trainInfo.color;
            colorButton_fieldsDialog.colors = block;
            #endregion
        }
        else if (typeof(T) == typeof(TicketGateInfo))
        {
            _dialogFieldsEditingType = Def.DialogType.TicketGate;
            #region TicketGate

            TicketGateInfo ticketGateInfo = info as TicketGateInfo;

            System.Diagnostics.Debug.Assert(ticketGateInfo != null, "ticketGateInfo != null");

            DialogTitle.text = "Ticket Gate Details";

            DialogField1.text = ticketGateInfo.waitTime.ToString();
            DialogField1.interactable = true;
            DialogText1.text = "Wait Time";

            DialogField2.text = ticketGateInfo.ObjectId.ToString();
            DialogField2.interactable = false;
            DialogText2.text = "ID";

            DialogField3.text = ticketGateInfo.isBidirectional.ToString();
            DialogField3.interactable = false;
            DialogText3.text = "Bidirectional";

            ColorBlock block = colorButton_fieldsDialog.colors;
            block.normalColor = Consts.defaultButtonColor;
            colorButton_fieldsDialog.colors = block;

            designatedButton.GetComponentInChildren<Text>().text = "Bidir";
            designatedButton.interactable = true;
            #endregion
        }
        else if (typeof(T) == typeof(StairInfo))
        {
            _dialogFieldsEditingType = Def.DialogType.StairInfo;
            #region StairInfo

            StairInfo stairInfo = info as StairInfo;

            System.Diagnostics.Debug.Assert(stairInfo != null, "stairInfo != null");

            DialogField1.text = stairInfo.stairDirection.ToString();
            DialogField1.interactable = false;
            DialogText1.text = "Direction";

            DialogField2.text = stairInfo.StairId.ToString();
            DialogField2.interactable = false;
            DialogText2.text = "ID";

            ColorBlock block = colorButton_fieldsDialog.colors;
            block.normalColor = Consts.defaultButtonColor;
            colorButton_fieldsDialog.colors = block;

            designatedButton.GetComponentInChildren<Text>().text = "FlipDir";
            designatedButton.interactable = true;

            if (stairInfo.stairType == Def.StairType.Escalator)
            {
                DialogTitle.text = "Escalator Details";
                DialogField3.text = stairInfo.stairType.ToString();
                DialogField3.interactable = false;
                DialogText3.text = "Speed";
            }
            else
            {
                DialogTitle.text = "Staircase Details";
                DialogField3.text = stairInfo.speed == -1 ? "N/A" : stairInfo.speed.ToString();
                DialogField3.interactable = true;
                DialogText3.text = "Speed";
            }
            #endregion
        }
    }

    public void DialogWithFieldsOpenAgentDist()
    {
        ag = _dialogFieldsEditingObject.GetComponent<AgentDistInfo>();

        System.Diagnostics.Debug.Assert(ag != null, "ag != null");

        if (ag.NumberOfAgents < 0)
        {
            Debug.Log("Try clicking again.");
            return;
        }

        timetableButton.GetComponentInChildren<Text>().text = "Timetable";
        timetableButton.interactable = true;

        if (ag.PopulationTimetable == null || ag.PopulationTimetable.Count == 0)
        {
            DialogField1.text = ag.NumberOfAgents.ToString();
            DialogField1.interactable = true;
        }
        else
        {
            DialogField1.text = timetableEnabledString;
            DialogField1.interactable = false;
        }

        DialogText1.text = "Initial Agents";

        if (ag.AgentType == Def.DistributionType.Dynamic)
        {
            if (ag.PopulationTimetable == null)
            {
                DialogField2.text = ag.GetFirstIncremental().ToString();
                DialogField3.text = ag.MaxAgents.ToString();
                DialogField2.interactable = true;
                DialogField3.interactable = true;
            }
            else
            {
                DialogField2.text = timetableEnabledString;
                DialogField3.text = timetableEnabledString;
                DialogField2.interactable = false;
                DialogField3.interactable = false;
            }

            DialogText2.text = "Agents / Second";
            DialogText3.text = "Total Limit";
            DialogTitle.text = "AgentDistribution (Dynamic) ID: ";
        }
        else
        {
            DialogField2.text = "-";
            DialogField3.text = "-";
            DialogText2.text = "-";
            DialogText3.text = "-";
            DialogField2.interactable = false;
            DialogField3.interactable = false;
            DialogTitle.text = "AgentDistribution (Static) ID: ";
        }

        DialogTitle.text += ag.ID;

        ColorBlock block = colorButton_fieldsDialog.colors;
        block.normalColor = ag.color;
        colorButton_fieldsDialog.colors = block;

        groupsButton.GetComponentInChildren<Text>().text = "Groups";
        groupsButton.interactable = true;
        designatedButton.GetComponentInChildren<Text>().text = "Designate";
        designatedButton.interactable = true;
        colorButton_fieldsDialog.interactable = true;
        colorButton_fieldsDialog.GetComponentInChildren<Text>().text = "Color";
    }

    public void DialogWithFieldsDesignatedOn()
    {
        if (_dialogFieldsEditingType == Def.DialogType.AgentPrefab)
        {
            var designatedGatesDatas = _dialogFieldsEditingObject.GetComponent<AgentDistInfo>().DGatesData;
            if (designatedGatesDatas == null || designatedGatesDatas.Count < 1)
                DialogDesignatedGate.Instance.Active(true);
            else
                DialogDesignatedGate.Instance.Active(true, designatedGatesDatas[0]);
        }
        else if (_dialogFieldsEditingType == Def.DialogType.TrainInfo)
        {
            var trainGen = _dialogFieldsEditingObject.GetComponent<TrainGenerator>();

            if (DialogField2.text != "" && DialogField2.text != "N/A" &&
                !int.TryParse(DialogField2.text, out trainGen.numCarriages))
                if (!string.IsNullOrEmpty(DialogField2.text))
                    Debug.Log("Could not convert " + DialogField2.text + " into a number.");

            DialogCarriageEditor.Instance.Active(true, trainGen);
        }
        else if (_dialogFieldsEditingType == Def.DialogType.StairInfo)
        {
            _dialogFieldsEditingObject.GetComponent<StairInfo>().FlipDirection();
            DialogWithFieldsClose();
        }
        else if (_dialogFieldsEditingType == Def.DialogType.TicketGate)
        {
            _dialogFieldsEditingObject.GetComponent<TicketGateInfo>().toggleBidir();
            DialogWithFieldsClose();
        }
    }
    public void DialogWithFieldsGroups()
    {
        if (_dialogFieldsEditingType == Def.DialogType.AgentPrefab)
            GroupDialog.Instance.Active(true, _dialogFieldsEditingObject.GetComponent<AgentDistInfo>());
        if (_dialogFieldsEditingType == Def.DialogType.TrainInfo)
        {
            _dialogFieldsEditingObject.GetComponent<TrainInfo>().ReverseDirection();
            ShowGeneralDialog("The direction of the train/tram has been reversed.", "Reversed.");
        }
    }
    public void DialogWithFieldsTimetable()
    {
        if (_dialogFieldsEditingType == Def.DialogType.AgentPrefab)
            DialogTimetable.Instance.Active(true, _dialogFieldsEditingObject.GetComponent<AgentDistInfo>());
    }
    public void DialogWithFieldsColorButton()
    {
        if (_dialogFieldsEditingType == Def.DialogType.AgentPrefab)
            ColorPickerDialog.Instance.PickColor(_dialogFieldsEditingObject.GetComponent<AgentDistInfo>(), colorButton_fieldsDialog);
        if (_dialogFieldsEditingType == Def.DialogType.TrainInfo)
            ColorPickerDialog.Instance.PickColor(_dialogFieldsEditingObject.GetComponent<TrainInfo>(), colorButton_fieldsDialog);
    }
    public void DialogWithFieldsUpdateButton()
    {
        switch (_dialogFieldsEditingType)
        {
            case Def.DialogType.ThreatInfo:

                ThreatInfo ti = _dialogFieldsEditingObject.GetComponent<ThreatInfo>();

                if (DialogField2.text != "" && DialogField2.text != "N/A" &&
                    !int.TryParse(DialogField2.text, out ti.StartTime))
                    if (!string.IsNullOrEmpty(DialogField2.text))
                        Debug.Log("Could not convert " + DialogField2.text + " into a number.");

                if (DialogField3.text == "∞")
                    ti.Duration = -1;
                else if (DialogField3.text != "" && DialogField3.text != "N/A" &&
                  !int.TryParse(DialogField3.text, out ti.Duration))
                    if (!string.IsNullOrEmpty(DialogField3.text))
                        Debug.Log("Could not convert " + DialogField3.text + " into a number.");
                break;

            case Def.DialogType.StairInfo:

                StairInfo si = _dialogFieldsEditingObject.GetComponent<StairInfo>();

                if (si.stairType != Def.StairType.Escalator)
                {
                    if (DialogField3.text != "" && DialogField3.text != "N/A" &&
                        !float.TryParse(DialogField3.text, out si.speed))
                        if (!string.IsNullOrEmpty(DialogField3.text))
                            Debug.Log("Could not convert " + DialogField3.text + " into a number.");
                }
                break;

            case Def.DialogType.AgentPrefab:

                AgentDistInfo ag = _dialogFieldsEditingObject.GetComponent<AgentDistInfo>();

                // Only apply fields when timetable isn't applied.
                if (ag.PopulationTimetable == null || ag.PopulationTimetable.Count < 1)
                {
                    float incrm = 0;

                    if (DialogField1.text != "" && DialogField1.text != "N/A" &&
                        !int.TryParse(DialogField1.text, out ag.NumberOfAgents))
                        if (!string.IsNullOrEmpty(DialogField1.text))
                            Debug.Log("Could not convert " + DialogField1.text + " into a number.");

                    if (DialogField2.text != "" && DialogField2.text != "N/A" &&
                        !float.TryParse(DialogField2.text, out incrm))
                        if (!string.IsNullOrEmpty(DialogField2.text))
                            Debug.Log("Could not convert " + DialogField2.text + " into a number.");

                    if (DialogField3.text != "" && DialogField3.text != "N/A" &&
                        !int.TryParse(DialogField3.text, out ag.MaxAgents))
                        if (!string.IsNullOrEmpty(DialogField3.text))
                            Debug.Log("Could not convert " + DialogField3.text + " into a number.");

                    if (ag.NumberOfAgentsIncremental.Count < 1)
                        ag.NumberOfAgentsIncremental.Add(incrm);
                    else
                        ag.NumberOfAgentsIncremental[0] = incrm;
                }

                ag.CheckDensity();
                break;

            case Def.DialogType.WaitPoint:
                WaitPointInfo wp = _dialogFieldsEditingObject.GetComponent<WaitPointInfo>();

                if (DialogField1.text != "" && DialogField1.text != "N/A" &&
                    !float.TryParse(DialogField1.text, out wp.interest))
                    if (!string.IsNullOrEmpty(DialogField1.text))
                        Debug.Log("Could not convert " + DialogField1.text + " into a number.");

                if (DialogField2.text != "" && DialogField2.text != "N/A" &&
                    !float.TryParse(DialogField2.text, out wp.waitTime))
                    if (!string.IsNullOrEmpty(DialogField2.text))
                        Debug.Log("Could not convert " + DialogField2.text + " into a number.");

                if (DialogField3.text != "" && DialogField3.text != "N/A" &&
                    !float.TryParse(DialogField3.text, out wp.wonderTime))
                    if (!string.IsNullOrEmpty(DialogField3.text))
                        Debug.Log("Could not convert " + DialogField3.text + " into a number.");
                break;

            case Def.DialogType.FireSource:

                FireSource fs = _dialogFieldsEditingObject.GetComponent<FireSource>();

                if (DialogField1.text != "" && DialogField1.text != "N/A" &&
                    !float.TryParse(DialogField1.text, out fs.flowRate))
                    if (!string.IsNullOrEmpty(DialogField1.text))
                        Debug.Log("Could not convert " + DialogField1.text + " into a number.");

                if (DialogField2.text != "" && DialogField2.text != "N/A" &&
                    !float.TryParse(DialogField2.text, out fs.velocity))
                    if (!string.IsNullOrEmpty(DialogField2.text))
                        Debug.Log("Could not convert " + DialogField2.text + " into a number.");

                if (DialogField3.text != "" && DialogField3.text != "N/A" &&
                    !float.TryParse(DialogField3.text, out fs.fireHeight))
                    if (!string.IsNullOrEmpty(DialogField3.text))
                        Debug.Log("Could not convert " + DialogField3.text + " into a number.");
                break;

            case Def.DialogType.TrainInfo:

                TrainInfo tri = _dialogFieldsEditingObject.GetComponent<TrainInfo>();

                if (DialogField1.text != "" && DialogField1.text != "N/A" &&
                    !float.TryParse(DialogField1.text, out tri.arrivalSpeed))
                    if (!string.IsNullOrEmpty(DialogField1.text))
                        Debug.Log("Could not convert " + DialogField1.text + " into a number.");

                if (DialogField2.text != "" && DialogField2.text != "N/A" &&
                    !int.TryParse(DialogField2.text, out tri.trainGen.numCarriages))
                    if (!string.IsNullOrEmpty(DialogField2.text))
                        Debug.Log("Could not convert " + DialogField2.text + " into a number.");

                if (DialogField3.text != "" && DialogField3.text != "N/A" &&
                    !float.TryParse(DialogField3.text, out tri.trainGen.doorWidth))
                    if (!string.IsNullOrEmpty(DialogField3.text))
                        Debug.Log("Could not convert " + DialogField3.text + " into a number.");

                tri.trainGen.BuildTrain();
                tri.InitialiseTrain();
                tri.trainGen.HideItems();
                tri.trainGen.ReconnectAgentDistributions();
                break;

            case Def.DialogType.TicketGate:

                TicketGateInfo tgi = _dialogFieldsEditingObject.GetComponent<TicketGateInfo>();

                if (DialogField1.text != "" && DialogField1.text != "N/A" &&
                    !float.TryParse(DialogField1.text, out tgi.waitTime))
                    if (!string.IsNullOrEmpty(DialogField1.text))
                        Debug.Log("Could not convert " + DialogField1.text + " into a number.");
                break;
        }

        DialogWithFieldsClose();
    }
    public void DialogWithFieldsDeleteButton()
    {
        Create.Instance.DestroyCreatedObject(_dialogFieldsEditingObject);
        DialogWithFieldsClose();
    }
    public void DialogWithFieldsClose()
    {
        SetDialogStatus(false, DialogWithFields);
        GroupDialog.Instance.Active(false);
        DialogDesignatedGate.Instance.Active(false);
    }
    public void DialogWithFieldsApplyAgentDestinationGates(List<DesignatedGatesData> dGatesData)
    {
        if (_dialogFieldsEditingType == Def.DialogType.AgentPrefab)
        {
            AgentDistInfo ag = _dialogFieldsEditingObject.GetComponent<AgentDistInfo>();

            float sum = 0;
            foreach (var itm in dGatesData)
            {
                foreach (var itm2 in itm.Distribution)
                    sum += itm2.Percentage;
            }

            if (sum >= 0.0001f)
                ag.DGatesData = dGatesData;
            else
                ag.DGatesData = null;
        }
        else if (_dialogFieldsEditingType == Def.DialogType.TrainInfo)
        {
            AgentDistInfo ag = _dialogFieldsEditingObject.GetComponent<TrainInfo>().trainGen.trainAgentDists[0].GetComponent<AgentDistInfo>();

            float sum = 0;
            foreach (var itm in dGatesData)
            {
                foreach (var itm2 in itm.Distribution)
                    sum += itm2.Percentage;
            }

            if (sum >= 0.0001f)
                ag.DGatesData = dGatesData;
            else
                ag.DGatesData = null;
        }
    }
    public void DialogWithFieldsApplyGroupNumbers(AgentDistInfo agentDistInfo)
    {
        AgentDistInfo ag = _dialogFieldsEditingObject.GetComponent<AgentDistInfo>();
        ag.GroupNumbers = agentDistInfo.GroupNumbers;
        ag.NumberOfAgents = agentDistInfo.NumberOfAgents;
        DialogField1.text = ag.NumberOfAgents.ToString();
    }
    public void DialogWithFieldsApplyTrainGeneration(List<int> numDoors, List<float> lengthCarriages, List<float> boardDists)
    {
        var trainGen = _dialogFieldsEditingObject.GetComponent<TrainGenerator>();
        trainGen.numDoorsList = numDoors;
        trainGen.carriageLengthsList = lengthCarriages;
        trainGen.boardDistributionList = boardDists;
    }

    //Created Prefab Dialog
    public void DialogPrefabs(string message, string windowName, GameObject editing = null, bool canMakeTransparent = false)
    {
        foreach (Button b in DialogPrefab.GetComponentsInChildren<Button>())
        {
            if (b.name != "DeleteButton") continue;
            b.enabled = editing != null;
            b.GetComponent<Image>().enabled = editing != null;
            b.GetComponentInChildren<Text>().enabled = editing != null;
        }

        if (editing != null)
            _currentlyEditingPrefab = editing;

        SetDialogStatus(true, DialogPrefab);

        foreach (Text t in DialogPrefab.GetComponentsInChildren<Text>())
        {
            switch (t.name)
            {
                case "MessageText":
                    t.text = message;
                    break;
                case "WindowNameText":
                    t.text = windowName;
                    break;
            }
        }

        WallInfo wallinfo = _currentlyEditingPrefab.GetComponent<WallInfo>();
        GateInfo gateinfo = _currentlyEditingPrefab.GetComponent<GateInfo>();

        foreach (Button b in DialogPrefab.GetComponentsInChildren<Button>())
        {
            if (b.gameObject.name == "HideButton")
            {
                b.interactable = gateinfo != null || wallinfo != null;
                b.GetComponentInChildren<Text>().text = LocalizationManager.GetTermTranslation("HIDE");

                if (wallinfo != null && wallinfo.IsTransparent)
                    b.GetComponentInChildren<Text>().text = LocalizationManager.GetTermTranslation("UNHIDE");
                if (gateinfo != null && gateinfo.IsTransparent)
                    b.GetComponentInChildren<Text>().text = LocalizationManager.GetTermTranslation("UNHIDE");
            }
        }
    }


    public void DialogLanguageClose()
    {
        DialogLanguage.SetActive(false);
    }

    public void DialogLanguageChange(int num)
    {
        switch ((Languages)num)
        {
            case Languages.English:
                LocalizationManager.CurrentLanguageCode = "en";
                break;
            case Languages.Chinese:
                LocalizationManager.CurrentLanguageCode = "zh";
                break;
            default:
                Debug.Log("Couldn't find language.");
                break;
        }


        DialogLanguageClose();
    }

    public void PrefabDeleteButton()
    {
        Create.Instance.DestroyCreatedObject(_currentlyEditingPrefab);
        PrefabDialogClose();
    }
    public void PrefabHideButton()
    {
        Debug.Log("Hide");
        WallInfo wallinfo = _currentlyEditingPrefab.GetComponent<WallInfo>();
        if (wallinfo != null)
            wallinfo.ToggleTransparency();
        GateInfo gateinfo = _currentlyEditingPrefab.GetComponent<GateInfo>();
        if (gateinfo != null)
            gateinfo.ToggleTransparency();
        PrefabDialogClose();
    }
    public void PrefabDialogClose()
    {
        SetDialogStatus(false, DialogPrefab);
        FileOperations.Instance.ObjectsToModel();
    }

    // Agents Select Dialog
    public void AgentsClickDialogOpen(string message, string windowName, GameObject editing = null)
    {
        foreach (Button b in DialogIndividualAgent.GetComponentsInChildren<Button>())
        {
            if (b.name != "DeleteButton") continue;
            b.enabled = editing != null;
            b.GetComponent<Image>().enabled = editing != null;
            b.GetComponentInChildren<Text>().enabled = editing != null;
        }

        if (editing != null)
            _currentlyEditingPrefab = editing;

        SetDialogStatus(true, DialogIndividualAgent);

        foreach (Text t in DialogIndividualAgent.GetComponentsInChildren<Text>())
        {
            switch (t.name)
            {
                case "MessageText":
                    t.text = message;
                    break;
                case "WindowNameText":
                    t.text = windowName;
                    break;
            }
        }
    }
    public void AgentsClickDeleteButton()
    {
        Consts.deleteAnAgent = _currentlyEditingPrefab.GetComponent<IndividualAgent>().agentID;
        Create.Instance.DestroyCreatedObject(_currentlyEditingPrefab);
        AgentsClickDialogClose();
    }
    public void AgentsClickColorButton()
    {
        ColorPickerDialog.Instance.PickColor(_currentlyEditingPrefab.GetComponent<IndividualAgent>(), colorButton_iaDialog);
    }
    public void AgentsClickFollow()
    {
        Create.Instance.UpdateCameraLocation(_mainCamera.transform.position, _mainCamera.transform.eulerAngles);
        ViewDisplayAllLevels();

        _mainCamera.enabled = false;
        _followCamera = Instantiate(followCameraPrefab).GetComponent<Camera>();
        Vector3 localRot = _followCamera.transform.localEulerAngles;
        Vector3 localPos = _followCamera.transform.position;
        _followCamera.transform.SetParent(_currentlyEditingPrefab.transform);
        _followCamera.transform.localPosition = localPos;
        _followCamera.transform.localEulerAngles = localRot;
        AgentsClickDialogClose();
    }
    public void AgentsClickDialogClose()
    {
        SetDialogStatus(false, DialogIndividualAgent);
        FileOperations.Instance.ObjectsToModel();
    }

    private void MenuItemClicked(string fn)
    {
        Analytics.CustomEvent(fn);
        _consoleText.text = fn;
        CloseAllMenus();
    }

    internal AgentDistInfo GetAgentDetails()
    {
        AgentDistInfo agentDistInfo = new AgentDistInfo();

        if (string.IsNullOrEmpty(agentInitialFields[0].text) || !int.TryParse(agentInitialFields[0].text, out agentDistInfo.NumberOfAgents))
            Debug.Log("Could not convert " + agentInitialFields[0].text + " into a number of agents.");

        agentDistInfo.AgentPlacement = AgentGenerationType;
        agentDistInfo.AgentType = GetAgentDistType();
        agentDistInfo.GroupNumbers.Clear();
        agentDistInfo.GroupDynamicNumbers.Clear();

        // If yes to groups
        if (_agentGroupDropdown.value == 1)
        {
            if (!string.IsNullOrEmpty(agentInitialFields[1].text))
            {
                int number = 0;
                if (int.TryParse(agentInitialFields[1].text, out number))
                    agentDistInfo.GroupNumbers.Add(new Group(2, number));
            }

            if (!string.IsNullOrEmpty(agentInitialFields[2].text))
            {
                int number = 0;
                if (int.TryParse(agentInitialFields[2].text, out number))
                    agentDistInfo.GroupNumbers.Add(new Group(3, number));
            }

            if (!string.IsNullOrEmpty(agentInitialFields[3].text))
            {
                int number = 0;
                if (int.TryParse(agentInitialFields[3].text, out number))
                    agentDistInfo.GroupNumbers.Add(new Group(4, number));
            }

            if (!string.IsNullOrEmpty(agentInitialFields[4].text))
            {
                int number = 0;
                if (int.TryParse(agentInitialFields[4].text, out number))
                    agentDistInfo.GroupNumbers.Add(new Group(5, number));
            }

            if (!string.IsNullOrEmpty(agentGroupSize.text) && !string.IsNullOrEmpty(agentInitialFields[5].text))
            {
                int number = 0;
                int size = 0;
                if (int.TryParse(agentGroupSize.text, out size) && int.TryParse(agentInitialFields[5].text, out number))
                    agentDistInfo.GroupNumbers.Add(new Group(size, number));
            }

            // If yes to groups AND yes to dynamic
            if ((int)agentDistInfo.AgentType == 1)
            {
                if (!string.IsNullOrEmpty(agentSecFields[1].text))
                {
                    float number = 0;
                    if (float.TryParse(agentSecFields[1].text, out number))
                        agentDistInfo.GroupDynamicNumbers.Add(new Group(2, number));
                }

                if (!string.IsNullOrEmpty(agentSecFields[2].text))
                {
                    float number = 0;
                    if (float.TryParse(agentSecFields[2].text, out number))
                        agentDistInfo.GroupDynamicNumbers.Add(new Group(3, number));
                }

                if (!string.IsNullOrEmpty(agentSecFields[3].text))
                {
                    float number = 0;
                    if (float.TryParse(agentSecFields[3].text, out number))
                        agentDistInfo.GroupDynamicNumbers.Add(new Group(4, number));
                }

                if (!string.IsNullOrEmpty(agentSecFields[4].text))
                {
                    float number = 0;
                    if (float.TryParse(agentSecFields[4].text, out number))
                        agentDistInfo.GroupDynamicNumbers.Add(new Group(5, number));
                }

                if (!string.IsNullOrEmpty(agentGroupSize.text) && !string.IsNullOrEmpty(agentSecFields[5].text))
                {
                    float number = 0;
                    int size = 0;
                    if (int.TryParse(agentGroupSize.text, out size) && float.TryParse(agentSecFields[5].text, out number))
                        agentDistInfo.GroupDynamicNumbers.Add(new Group(size, number));
                }
            }
        }

        // If yes to dynamic
        if ((int)agentDistInfo.AgentType == 1)
        {
            if (!string.IsNullOrEmpty(agentLimitField.text))
                int.TryParse(agentLimitField.text, out agentDistInfo.MaxAgents);

            float incrm = 0;

            if (!string.IsNullOrEmpty(agentSecFields[0].text))
                float.TryParse(agentSecFields[0].text, out incrm);

            if (agentDistInfo.NumberOfAgentsIncremental.Count < 1)
                agentDistInfo.NumberOfAgentsIncremental.Add(incrm);
            else
                agentDistInfo.NumberOfAgentsIncremental[0] = incrm;
        }

        if (agentDistInfo.NumberOfAgents > Consts.MaxNumAgents)
        {
            string message = agentDistInfo.NumberOfAgents + " is too many agents." + Environment.NewLine
                             + "Number of agents set to " + Consts.MaxNumAgents;
            Debug.Log(message);
            ShowGeneralDialog(message, "Exceeded Max No. Agents");
            agentDistInfo.NumberOfAgents = Consts.MaxNumAgents;
            agentInitialFields[0].text = Consts.MaxNumAgents.ToString();
        }

        return agentDistInfo;
    }
    public Def.DistributionType GetAgentDistType()
    {
        return (Def.DistributionType)_agentTypeDropdown.value;
    }
    public void GroupFieldsUIUpdate()
    {
        int total = 0;
        int number = 0;
        int size = 0;

        if (!string.IsNullOrEmpty(agentInitialFields[0].text))
            int.TryParse(agentInitialFields[0].text, out number);
        total += number;
        number = 0;

        if (!string.IsNullOrEmpty(agentInitialFields[1].text))
            int.TryParse(agentInitialFields[1].text, out number);
        total += number * 2;
        number = 0;

        if (!string.IsNullOrEmpty(agentInitialFields[2].text))
            int.TryParse(agentInitialFields[2].text, out number);
        total += number * 3;
        number = 0;

        if (!string.IsNullOrEmpty(agentInitialFields[3].text))
            int.TryParse(agentInitialFields[3].text, out number);
        total += number * 4;
        number = 0;

        if (!string.IsNullOrEmpty(agentInitialFields[4].text))
            int.TryParse(agentInitialFields[4].text, out number);
        total += number * 5;
        number = 0;

        if (!string.IsNullOrEmpty(agentGroupSize.text) && !string.IsNullOrEmpty(agentInitialFields[5].text))
        {
            int.TryParse(agentInitialFields[5].text, out number);
            int.TryParse(agentGroupSize.text, out size);
            total += number * size;
        }

        agentInitialFields[6].text = total.ToString();

        float totalSec = 0;
        float numberSec = 0;
        size = 0;

        if (!string.IsNullOrEmpty(agentSecFields[0].text))
            float.TryParse(agentSecFields[0].text, out numberSec);
        totalSec += numberSec;
        numberSec = 0;

        if (!string.IsNullOrEmpty(agentSecFields[1].text))
            float.TryParse(agentSecFields[1].text, out numberSec);
        totalSec += numberSec * 2;
        numberSec = 0;

        if (!string.IsNullOrEmpty(agentSecFields[2].text))
            float.TryParse(agentSecFields[2].text, out numberSec);
        totalSec += numberSec * 3;
        numberSec = 0;

        if (!string.IsNullOrEmpty(agentSecFields[3].text))
            float.TryParse(agentSecFields[3].text, out numberSec);
        totalSec += numberSec * 4;
        numberSec = 0;

        if (!string.IsNullOrEmpty(agentSecFields[4].text))
            float.TryParse(agentSecFields[4].text, out numberSec);
        totalSec += numberSec * 5;
        numberSec = 0;

        if (!string.IsNullOrEmpty(agentGroupSize.text) && !string.IsNullOrEmpty(agentSecFields[5].text))
        {
            float.TryParse(agentSecFields[5].text, out numberSec);
            int.TryParse(agentGroupSize.text, out size);
            totalSec += numberSec * size;
        }

        agentSecFields[6].text = totalSec.ToString();
    }

    internal ThreatInfo GetThreatDetails()
    {
        ThreatInfo threatInfo = new ThreatInfo();
        threatInfo.LevelId = Create.Instance.SelectedLevel;
        if (string.IsNullOrEmpty(_dangerStartTime.text) || !int.TryParse(_dangerStartTime.text, out threatInfo.StartTime))
            Debug.Log("Could not convert " + _dangerStartTime.text + " into a start time.");
        if (string.IsNullOrEmpty(_dangerDuration.text) || !int.TryParse(_dangerDuration.text, out threatInfo.Duration))
            Debug.Log("Could not convert " + _dangerDuration.text + " into a duration.");
        return threatInfo;
    }

    private void SetFields(bool isEnabled)
    {
        foreach (var fireSimField in fireSimFields)
        {
            InputField inf = fireSimField.GetComponent<InputField>();
            if (inf != null)
                inf.interactable = isEnabled;
            Button but = fireSimField.GetComponent<Button>();
            if (but != null)
                but.interactable = isEnabled;
        }
    }

    public void DxfType(int type)
    {
        DxfReader.Read(_resultPathImport, type);
        CloseDxfDialog();
    }

    public void CloseDxfDialog()
    {
        SetDialogStatus(false, DialogDxf);
    }


    public void FireSetEnabledState(int isEnabled)
    {
        SetFields(isEnabled == 1);
        FireDomain.Instance.SetEnabledState(isEnabled);
    }
    public void FireSimulation()
    {
        DialogFire.SetActive(true);
    }
    public void FireSimulationClose()
    {
        DialogFire.SetActive(false);
    }
    public void FireSimulationRead()
    {
        FireDomain.Instance.ReadFireSimulation();
    }
    public void FireSimulationPlay()
    {
        FireDomain.Instance.PlayFireSimulation();
    }

    public void ShowDxfDialog()
    {
        SetDialogStatus(true, DialogDxf);
    }
    internal void ShowGeneralDialog(string message, string windowName = "message")
    {
        //CloseAllMenus();
        SetDialogStatus(true, DialogGeneral);

        foreach (Text t in DialogGeneral.GetComponentsInChildren<Text>())
        {
            switch (t.name)
            {
                case "MessageText":
                    t.text = message;
                    break;
                case "WindowNameText":
                    t.text = windowName;
                    break;
            }
        }
    }

    private void SetDialogStatus(bool open, GameObject dialog = null)
    {
        if (dialog != null)
            dialog.SetActive(open);
        DialogOpen = open;

        GenericMoveCamera gmc = null;

        if (Camera.main != null)
            gmc = Camera.main.GetComponent<GenericMoveCamera>();

        if (gmc != null)
            gmc.Operational = !open;
    }

    //Asynchronous file dialogs:
    public IEnumerator OpenFile(string path)
    {
        yield return StartCoroutine(DialogFile.Open(path, Strings.ExtensionCpm, "Open Model", null,
            Consts.MaxSizeDialogPath, _saveLastPath));

        if (DialogFile.result != null)
        {
            // Opening a new model.
            _resultPath = DialogFile.result;
            OpenModel(_resultPath);
        }
        SetDialogStatus(false);
    }

    public void OpenModel(string path)
    {
        if (!File.Exists(path))
        {
            Debug.Log("Could not find model " + path);
            return;
        }

        TopDownView.Instance.SetTopDown(false);
        Model model = FileOperations.Instance.OpenModel(path);

        if (string.IsNullOrEmpty(model.version))
            ShowGeneralDialog("File might have been create prior to version 2020.5.2.", "File Version");
        else if (!model.version.Equals(Application.version))
            ShowGeneralDialog("File Version " + model.version, "File Version");

        FileOperations.Instance.ModelToObjects(model);
        UpdateWindowName(path);
        SimulationController.Instance.CancelSimulation();
    }

    public IEnumerator SaveFile(string path)
    {
        yield return StartCoroutine(DialogFile.Save(path, Strings.ExtensionCpm, "Save Model", null, _saveLastPath));

        if (DialogFile.result != null)
        {
            _resultPathSave = DialogFile.result;
            SaveFile();
            UpdateWindowName(DialogFile.result);
        }
        SetDialogStatus(false);
    }
    public IEnumerator OpenTimetable(string path)
    {
        yield return StartCoroutine(DialogFile.Open(path, ".csv", "Open Timetable (.csv)", null, -1, _saveLastPath));

        if (DialogFile.result != null)
            DialogTimetable.Instance.OpenTimetable(DialogFile.result);

        SetDialogStatus(false);
    }
    public IEnumerator OpenTrainTimetable(string path)
    {
        yield return StartCoroutine(DialogFile.Open(path, ".csv", "Open Train Timetable (.csv)", null, -1, _saveLastPath));

        int totalTrains = 0;
        int matches = 0;

        if (DialogFile.result != null)
        {
            _resultPathTimetable = DialogFile.result;
            Debug.Log("Open " + _resultPathTimetable);

            Dictionary<int, List<TrainData>> trainData = FileOperations.Instance.ReadTrainTimetable(_resultPathTimetable);

            totalTrains = trainData.Count;

            TimetableStatusTextTrains.GetComponent<Text>().text = LocalizationManager.GetTermTranslation("Transit Timetable") + " " + LocalizationManager.GetTermTranslation("Enabled");
            TimetableStatusTextTrains.GetComponent<Text>().font = FindObjectOfType<ReplaceFonts>().bold;

            foreach (var trainInfo in FindObjectsOfType<TrainInfo>())
            {
                if (trainData.ContainsKey(trainInfo.ObjectId))
                {
                    matches++;
                    trainInfo.ApplyTrainData(trainData[trainInfo.ObjectId]);
                }
            }
        }

        if (totalTrains == 0)
        {
            Debug.Log("Loaded no trains...");
        }
        else if (totalTrains == matches)
        {
            ShowGeneralDialog("Successfully loaded and matched " + totalTrains + " trains.", "Loaded Train Timetable");
        }
        else
        {
            ShowGeneralDialog(totalTrains + " unique trains loaded, but could only match " + matches + " trains. Please ensure the correct train IDs are specified and match the trains within the environment.", "Loaded Train Timtable");
        }

        AgentsUpdateType();
        SetDialogStatus(false);
    }
    public IEnumerator OpenDistributionTable(string path)
    {
        yield return StartCoroutine(DialogFile.Open(path, ".csv", "Open Distribution Table (.csv)", null, -1, _saveLastPath));

        int numDistributions = 0;
        int agentMatches = 0;

        if (DialogFile.result != null)
        {
            _resultPathTimetable = DialogFile.result;
            Debug.Log("Open " + _resultPathTimetable);

            var distributionTableDatas = FileOperations.Instance.ReadDistributionTable(_resultPathTimetable);

            numDistributions = distributionTableDatas.Count;

            if (numDistributions != 0)
            {
                TableStatusDistribution.GetComponent<Text>().text = LocalizationManager.GetTermTranslation("Distribution Table") + " " + LocalizationManager.GetTermTranslation("Enabled");
                TableStatusDistribution.GetComponent<Text>().font = FindObjectOfType<ReplaceFonts>().bold;

                foreach (var agentDistInfo in FindObjectsOfType<AgentDistInfo>())
                {
                    if (distributionTableDatas.ContainsKey(agentDistInfo.ID))
                    {
                        agentMatches++;
                        agentDistInfo.ApplyDesignatedData(distributionTableDatas[agentDistInfo.ID]);
                    }
                }

                foreach (var trainInfo in FindObjectsOfType<TrainInfo>())
                {
                    if (distributionTableDatas.ContainsKey(trainInfo.ObjectId))
                    {
                        agentMatches++;
                        trainInfo.ApplyDesignatedData(distributionTableDatas[trainInfo.ObjectId]);
                    }
                }
            }
        }

        if (numDistributions == 0)
        {
            Debug.Log("Loaded no distributions...");
        }
        else if (numDistributions == agentMatches)
        {
            ShowGeneralDialog("Successfully loaded and matched " + numDistributions + " agent distributions.", "Loaded Distribution Table");
        }
        else
        {
            ShowGeneralDialog(numDistributions + " unique agent distributions loaded, but could only match " + agentMatches + ". Please ensure the correct IDs are specified and match the agent distributions within the environment.", "Loaded Distribution Table");
        }

        AgentsUpdateType();
        SetDialogStatus(false);
    }

    private void SaveFile()
    {
        if (_resultPathSave == null)
        {
            FileSaveAs();
            return;
        }

        if (!_resultPathSave.EndsWith(".cpm"))
            _resultPathSave = _resultPathSave + ".cpm";

        if (FileOperations.Instance.SaveModel(FileOperations.Instance.ObjectsToModel(), _resultPathSave))
            _consoleText.text = " Saved: " + _resultPathSave;
        else
            _consoleText.text = " Did not save: " + _resultPathSave;
    }

    public void ExportData(string path = "")
    {
        if (string.IsNullOrEmpty(path))
        {
            if (!string.IsNullOrEmpty(ConfigurationFile.outputFile))
            {
                path = Environment.CurrentDirectory + "/" + ConfigurationFile.outputFile;
            }
            else
            {
                path = Application.dataPath + "Export_" + ModelName + "/";
                Directory.CreateDirectory(path);
                path += "SimulationData.csv";
            }
        }

        _consoleText.text = RunExports(path) ? "Exported data." : "Export unsuccessfull.";
    }

    private static bool RunExports(string path)
    {
        bool success = true;

        if (Consts.ExportDataPositions)
            if (!FileOperations.Instance.ExportDataPositions(path)) success = false;
        if (Consts.ExportReactionTimes)
            if (!FileOperations.Instance.ExportReactionTimes(path)) success = false;
        if (Consts.ExportDataEvacTimes)
            if (!FileOperations.Instance.ExportDataEvacTimes(path)) success = false;
        if (Consts.ExportDataGateShares)
            if (!FileOperations.Instance.ExportDataGateShares(path)) success = false;
        if (Consts.ExportSimulationRealTimes)
            if (!FileOperations.Instance.ExportSimulationTimesReal(path)) success = false;

        return success;
    }

    public IEnumerator ExportDataDialog(string path)
    {
        yield return StartCoroutine(DialogFile.Save(path, Strings.ExtensionCsv, "Export Data", null, _saveLastPath));

        Analytics.CustomEvent("ExportData");

        if (DialogFile.result != null)
        {
            if (!SimulationController.Instance.IsSimulationComplete())
            {
                ShowGeneralDialog(
                    "Data exported while simulation was still running.",
                    "Exporting Data");
            }

            _resultPathExportData = DialogFile.result;
            ExportData(_resultPathExportData);
        }
        SetDialogStatus(false);
    }

    public IEnumerator ImportFile(string path)
    {
        yield return StartCoroutine(DialogFile.Open(path, Strings.ExtensionImport, "Import File", null,
            Consts.MaxSizeDialogPath, _saveLastPath));

        //Analytics.CustomEvent("ImportImage");

        if (DialogFile.result != null)
        {
            //Debug.Log(Strings.fileDialogEnded + dialog.result);
            _resultPathImport = DialogFile.result;
            TopDownView.Instance.SetTopDown(false);
            FileOperations.Instance.ProcessImport(DialogFile.result);
            _consoleText.text = "Import: " + _resultPathImport;
        }
        SetDialogStatus(false);
    }

    private void UpdateWindowName(string resultPath)
    {
#if UNITY_STANDALONE_WIN
        string[] splitName = resultPath.Split('\\');
        modelName = splitName[splitName.Length - 1];
        modelName = modelName.Remove(modelName.Length - 4);

        //Get the window handle.
        IntPtr windowPtr = FindWindow(null, _windowNameCurrent);
        _windowNameCurrent = _windowNameDefault + "   " + ModelName;
        SetWindowText(windowPtr, _windowNameCurrent);
#endif
    }

    internal void SwitchBackToMainCamera()
    {
        if (_followCamera != null)
            Destroy(_followCamera);
        _mainCamera.enabled = true;
        _mainCamera.GetComponent<GenericMoveCamera>().Operational = true;
    }

    public void CloseAnyDialog()
    {
        AgentsClickDialogClose();
        PrefabDialogClose();
        DialogWithFieldsClose();
        QuitDialogCancel();
        DialogFile.gameObject.SetActive(false);
    }

    public void ViewTogglePathUpdates()
    {
        _viewPathUpdates = !ViewPathUpdates;
        SetConsoleText("Path Updates set to " + _viewPathUpdates);

        if (!_viewPathUpdates)
        {
            SimulationController.Instance.SetAllAgentsWhite();
        }
        else
        {
            SimulationController.Instance.ToggleColorDisplayOff();
        }
    }

    public void PathUpdatesOff()
    {
        _viewPathUpdates = false;
    }

    public void ViewTogglePathfindingView()
    {
        _viewPathfinding = !_viewPathfinding;

        if (!_viewPathfinding)
            SimulationController.Instance.ViewPathsNow(false);
    }

    public void ResetPreset()
    {
        VariablePresetHandler.Instance.ResetPresetToDefault();
        SetConsoleText("Updated all parameters to default values.");
    }

    public void CentreTrains()
    {
        foreach (var train in FindObjectsOfType<TrainInfo>())
            train.CentreTrain();
    }

    public void ChangeMode(int mode)
    {
        ChangeMode(mode == 1);
    }

    public void ChangeMode(bool panicMode)
    {
        if (SimulationController.Instance.SimState == (int)Def.SimulationState.Started)
        {
            ShowGeneralDialog("You can't change modes while the simulation is running.", "Please Note");
        }
        else
        {
            string message = "Switching panic mode to " + panicMode;
            SetConsoleText(message);
            Params.ApplyMode(!panicMode);
        }

        ApplyPanicModeUI(panicMode);
    }

    private void ApplyPanicModeUI(bool panicMode)
    {
        behaviouralMode.value = panicMode ? 1 : 0;

        foreach (var item in panicButtonsFields)
        {
            var button = item.GetComponent<Button>();
            var field = item.GetComponent<InputField>();
            var dropdown = item.GetComponent<Dropdown>();

            if (button != null)
                button.interactable = !panicMode;

            if (field != null)
                field.interactable = !panicMode;

            if (dropdown != null)
                dropdown.interactable = !panicMode;
        }

        foreach (var item in normalButtonsFields)
        {
            var button = item.GetComponent<Button>();
            var field = item.GetComponent<InputField>();
            var dropdown = item.GetComponent<Dropdown>();

            if (button != null)
                button.interactable = panicMode;

            if (field != null)
                field.interactable = panicMode;

            if (dropdown != null)
                dropdown.interactable = panicMode;
        }
    }

    public void DeleteKey()
    {
        if (DialogWithFields.activeInHierarchy &&
            !DialogDesignatedGate.Instance.IsActive() &&
            !GroupDialog.Instance.IsActive() &&
            !DialogCarriageEditor.Instance.IsActive())
            DialogWithFieldsDeleteButton();

        if (DialogPrefab.activeInHierarchy)
            PrefabDeleteButton();
    }
}