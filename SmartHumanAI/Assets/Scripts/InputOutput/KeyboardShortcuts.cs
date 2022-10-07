using Assets.Scripts;
using DataFormats;
using UnityEngine;
using UnityEngine.UI;

namespace InputOutput
{
    [RequireComponent(typeof(UIController))]
    public class KeyboardShortcuts : MonoBehaviour
    {
        private UIController _uic;
        private SimulationController _sim;

        // Use this for initialization
        public void Start()
        {
            _uic = UIController.Instance;
            _sim = SimulationController.Instance;
        }

        // Update is called once per frame
        public void Update()
        {
            if (!_uic.KeyboardEnabled) return;

            if (_uic.DialogOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                    _uic.CloseAnyDialog();
                else if (Input.GetKeyDown(KeyCode.Delete))
                    _uic.DeleteKey();
                return;
            }

           
            else if (Input.GetKeyDown(KeyCode.P))
                _uic.PlaybackPause();
            else if (Input.GetKeyDown(KeyCode.R))
                _uic.PlaybackRestart();
            else if (Input.GetKeyDown(KeyCode.J))
                _sim.ToggleLineLinear();
            else if (Input.GetKeyDown(KeyCode.K))
                _sim.TogglePaths();
            else if (Input.GetKeyDown(KeyCode.Z))
                _sim.ChangeColorDisplay(Def.ColorDisplay.Density);
            else if (Input.GetKeyDown(KeyCode.X))
                _sim.ChangeColorDisplay(Def.ColorDisplay.Speed);
            else if (Input.GetKeyDown(KeyCode.C))
                _sim.ChangeColorDisplay(Def.ColorDisplay.DensityThreshold);
            else if (Input.GetKeyDown(KeyCode.V))
                _sim.ChangeColorDisplay(Def.ColorDisplay.SpeedThreshold);
            else if (Input.GetKeyDown(KeyCode.B))
                _sim.ChangeColorDisplay(Def.ColorDisplay.JointThreshold);
            else if (Input.GetKeyDown(KeyCode.M))
                _uic.ViewTogglePathUpdates();
            else if (Input.GetKeyDown(KeyCode.Slash))
                PathfindingGridViewerSquare.Instance.ToggleDisplayGrid();
            else if (Input.GetKeyDown(KeyCode.Escape))
                _uic.FileExit();
            

            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetKeyDown(KeyCode.BackQuote))
                    _uic.LevelSetLevel(0);
                else if (Input.GetKeyDown(KeyCode.Alpha1))
                    _uic.LevelSetLevel(1);
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                    _uic.LevelSetLevel(2);
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                    _uic.LevelSetLevel(3);
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                    _uic.LevelSetLevel(4);
                else if (Input.GetKeyDown(KeyCode.Alpha5))
                    _uic.LevelSetLevel(5);
                else if (Input.GetKeyDown(KeyCode.Alpha6))
                    _uic.LevelSetLevel(6);
                else if (Input.GetKeyDown(KeyCode.Alpha7))
                    _uic.LevelSetLevel(7);
                else if (Input.GetKeyDown(KeyCode.Alpha8))
                    _uic.LevelSetLevel(8);
                else if (Input.GetKeyDown(KeyCode.Alpha9))
                    _uic.LevelSetLevel(9);
                else if (Input.GetKeyDown(KeyCode.Alpha0))
                    _uic.LevelSetLevel(10);
                else if (Input.GetKeyDown(KeyCode.H))
                    _sim.HideAllAgents();
            }
        }
    }
}
