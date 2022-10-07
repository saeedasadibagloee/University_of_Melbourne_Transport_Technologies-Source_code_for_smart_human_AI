using Assets.Scripts;
using DataFormats;
using InputOutput;
using UnityEngine;

namespace Helper
{
    public class TopDownView : MonoBehaviour
    {
        private Vector3 _defaultRot;
        private Vector3 _defaultPos;
        private Vector3 _topDownViewRot = new Vector3(90f, 0, 0);
        private float _mouseRotSensitivity = 2f;
        private const float OrthographicStartingSize = 15f;

        Transform _mainCamera;
        GenericMoveCamera _gmc;
        Camera _mainCam;
        private bool _modelHasAtLeastOneVertex = false;

        private static TopDownView _instance;

        public TopDownView()
        {
            IsTopDown = false;
        }

        public static TopDownView Instance
        {
            get { return _instance ?? (_instance = FindObjectOfType<TopDownView>()); }
        }

        public bool IsTopDown { get; private set; }

        void Start()
        {
            Transform mainCamera = Camera.main.transform;
            _mainCam = Camera.main.transform.GetComponent<Camera>();
            _mainCam.orthographicSize = OrthographicStartingSize;
            _gmc = mainCamera.GetComponent<GenericMoveCamera>();
        }

        // Update is called once per frame
        void Update()
        {
            if (!IsTopDown) return;

            if (UnityEngine.Input.GetKey(KeyCode.W))
            {
                _gmc.PanY(2);
                _gmc.ForwardBack(1);
            }
            else if (UnityEngine.Input.GetKey(KeyCode.S))
            {
                _gmc.PanY(-2);
                _gmc.ForwardBack(-1);
            }
            else if (UnityEngine.Input.GetKey(KeyCode.A))
            {
                _gmc.PanX(-2);
            }
            else if (UnityEngine.Input.GetKey(KeyCode.D))
            {
                _gmc.PanX(2);
            }
            SetOrthographicSize();

            if (_mainCamera.eulerAngles != _topDownViewRot)
            {
                _mainCamera.eulerAngles = _topDownViewRot;
            }
        }

        private void SetOrthographicSize()
        {
            _mainCam.orthographicSize = Mathf.Abs(1 + _mainCamera.transform.position.y) / 1.81f;
        }

        public void ToggleTopDown()
        {
            _mainCamera = Camera.main.transform;
            if (IsTopDown)
            {
                //Set normal camera parameters
                IsTopDown = false;

                _mainCamera.position = _defaultPos;
                _mainCamera.eulerAngles = _defaultRot;
                _gmc.MouseRotationSensitivity = _mouseRotSensitivity;
            }
            else
            {
                //Set top down camera parameters
                IsTopDown = true;

                _defaultPos = _mainCamera.position;
                _defaultRot = _mainCamera.eulerAngles;
                _mainCamera.eulerAngles = _topDownViewRot;

                Model currentModel = FileOperations.Instance.ObjectsToModel();
                _modelHasAtLeastOneVertex = false;

                float minX = float.MaxValue;
                float minY = float.MaxValue;
                float maxX = float.MinValue;
                float maxY = float.MinValue;

                foreach (Level level in currentModel.levels)
                {
                    foreach (Wall e in level.wall_pkg.walls)
                    {
                        foreach (Vertex v in e.vertices)
                        {
                            _modelHasAtLeastOneVertex = true;
                            if (v.X < minX)
                                minX = v.X;
                            if (v.X > maxX)
                                maxX = v.X;
                            if (v.Y < minY)
                                minY = v.Y;
                            if (v.Y > maxY)
                                maxY = v.Y;
                        }
                    }
                    foreach (Gate e in level.gate_pkg.gates)
                    {
                        foreach (Vertex v in e.vertices)
                        {
                            _modelHasAtLeastOneVertex = true;
                            if (v.X < minX)
                                minX = v.X;
                            if (v.X > maxX)
                                maxX = v.X;
                            if (v.Y < minY)
                                minY = v.Y;
                            if (v.Y > maxY)
                                maxY = v.Y;
                        }
                    }
                    foreach (Wall e in level.barricade_pkg.barricade_walls)
                    {
                        foreach (Vertex v in e.vertices)
                        {
                            _modelHasAtLeastOneVertex = true;
                            if (v.X < minX)
                                minX = v.X;
                            if (v.X > maxX)
                                maxX = v.X;
                            if (v.Y < minY)
                                minY = v.Y;
                            if (v.Y > maxY)
                                maxY = v.Y;
                        }
                    }
                }

                if (_modelHasAtLeastOneVertex)
                {
                    float width = maxX - minX;
                    float height = maxY - minY;

                    float size = width > height ? width : height;

                    float midX = (maxX + minX) / 2f;
                    float midY = (maxY + minY) / 2f;

                    _mainCamera.position = new Vector3(midX, size, midY);
                }
                else
                {
                    Create create = Create.Instance;

                    int size = create.GetGridSize();

                    _mainCamera.position = new Vector3(create.Width / 2, size, create.Height / 2);
                }

                SetOrthographicSize();
                _gmc.ResetMovement();

                _mouseRotSensitivity = _gmc.MouseRotationSensitivity;
                _gmc.MouseRotationSensitivity = 0f;

                //QualitySettings.SetQualityLevel((int)Shadows.Off, false);
            }

            _mainCam.orthographic = IsTopDown;
            SimulationController.Instance.ChangeModeAllAgents(IsTopDown);
        }

        public void SetTopDown(bool value)
        {
            if (value != IsTopDown)
                ToggleTopDown();
        }

        public bool GetTopDown()
        {
            return IsTopDown;
        }
    }
}
