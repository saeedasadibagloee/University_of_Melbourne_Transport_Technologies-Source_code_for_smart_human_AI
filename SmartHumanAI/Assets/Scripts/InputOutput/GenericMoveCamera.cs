using System;
using DataFormats;
using UnityEngine;

namespace InputOutput
{

    public class GenericMoveCamera : MonoBehaviour
    {

        private Movement _forward;
        private Movement _panX;
        private Movement _rotateX;
        private Movement _panY;
        private Movement _rotateY;
        private float _resolution = 1f;

        [Header("Operational")]
        public bool Operational = true;

        [Header("Input Method")]
        public GenericMoveCameraInputs GetInputs = null;

        [Header("Camera")]
        public bool LevelCamera = true;
        public bool ForwardMovementLockEnabled = true;

        [Header("Look At")]
        public GameObject LookAtTarget = null;
        public float MinimumZoom = 20f;
        public float MaximumZoom = 80f;

        [Header("Movement Limits - X")]
        public bool LockX = false;
        public bool UseXRange;
        public float XRangeMin;
        public float XRangeMax;

        [Header("Movement Limits - Y")]
        public bool LockY = false;
        public bool UseYRange;
        public float YRangeMin;
        public float YRangeMax;

        [Header("Movement Limits - Z")]
        public bool LockZ = false;
        public bool UseZRange;
        public float ZRangeMin;
        public float ZRangeMax;

        private class Movement
        {
            private readonly Action<float> _action;
            private readonly Func<float> _dampenRate;
            private float _velocity;
            private float _dampen;

            public Movement(Action<float> aAction, Func<float> aDampenRate)
            {
                _action = aAction;
                _dampenRate = aDampenRate;
                _velocity = 0f;
                _dampen = 0;
            }

            public void ChangeVelocity(float aAmount)
            {
                _velocity += aAmount;
                _dampen = _dampenRate();
            }

            public void SetVelocity(float aAmount)
            {
                _velocity = aAmount;
                _dampen = _dampenRate();
            }

            public void Update(bool aDampen = true)
            {
                if (_dampen > 0)
                    if (_velocity >= -0.003f && _velocity <= 0.003f)
                    {
                        _dampen = 0;
                        _velocity = 0;
                    }
                    else
                    {
                        if (aDampen)
                            _velocity *= _dampen;

                        _action(_velocity);
                    }
            }
        }

        public void SetResolution(float aResolution)
        {
            _resolution = aResolution;
        }

        public void Awake()
        {
            if (GetInputs == null)
                GetInputs = gameObject.AddComponent<GenericMoveCameraInputs>();

            GetInputs.Initialize();
        }

        public void Start()
        {
            InitialiseForward();

            _panX = new Movement(aAmount => gameObject.transform.Translate(Vector3.left * aAmount), () => PanningDampenRate);
            _panY = new Movement(aAmount => gameObject.transform.Translate(Vector3.up * aAmount), () => PanningDampenRate);

            _rotateX = new Movement(aAmount => gameObject.transform.Rotate(Vector3.up * aAmount), () => RotateDampenRate);
            _rotateY = new Movement(aAmount => gameObject.transform.Rotate(Vector3.left * aAmount), () => RotateDampenRate);

        }

        private void InitialiseForward()
        {
            _forward = LookAtTarget == null ? new Movement(aAmount => gameObject.transform.Translate(Vector3.forward * aAmount), () => ForwardDampenRate) : new Movement(aAmount => gameObject.GetComponent<Camera>().fieldOfView += aAmount, () => ForwardDampenRate);
        }

        public void Update()
        {

            if (!Operational)
                return;

            Vector3 startPosition = Vector3.zero;

            try
            {
                GetInputs.QueryInputSystem();

                startPosition = gameObject.transform.position;

                if (GetInputs.ResetMovement)
                {
                    ResetMovement();
                }
                else
                {

                    float mag = (GetInputs.IsSlowModifier ? ControlKeyMagnification : 1f) * (GetInputs.IsFastModifier ? ShiftKeyMagnification : 1f);

                    if (GetInputs.IsPanLeft)
                    {
                        _panX.ChangeVelocity(0.01f * mag * _resolution * PanLeftRightSensitivity);
                    }
                    else if (GetInputs.IsPanRight)
                    {
                        _panX.ChangeVelocity(-0.01f * mag * _resolution * PanLeftRightSensitivity);
                    }

                    if (_panX != null)
                        _panX.Update();

                    if (GetInputs.IsMoveForward)
                    {
                        _forward.ChangeVelocity(0.005f * mag * _resolution * MovementSpeedMagnification);
                    }
                    else if (GetInputs.IsMoveBackward)
                    {
                        _forward.ChangeVelocity(-0.005f * mag * _resolution * MovementSpeedMagnification);
                    }

                    if (GetInputs.IsMoveForwardAlt)
                    {
                        _forward.ChangeVelocity(0.005f * mag * _resolution * MovementSpeedMagnification * WheelMouseMagnification);
                    }
                    else if (GetInputs.IsMoveBackwardAlt)
                    {
                        _forward.ChangeVelocity(-0.005f * mag * _resolution * MovementSpeedMagnification * WheelMouseMagnification);
                    }

                    if (GetInputs.IsPanUp)
                    {
                        _panY.ChangeVelocity(0.005f * mag * _resolution * PanUpDownSensitivity);
                    }
                    else if (GetInputs.IsPanDown)
                    {
                        _panY.ChangeVelocity(-0.005f * mag * _resolution * PanUpDownSensitivity);
                    }

                    bool forwardLock = GetInputs.IsLockForwardMovement && ForwardMovementLockEnabled;
                    _forward.Update(!forwardLock);

                    _panY.Update();

                    // Pan
                    if (GetInputs.IsRotateAction)
                    {

                        float x = (Input.mousePosition.x - GetInputs.RotateActionStart.x) / Screen.width * MouseRotationSensitivity;
                        float y = (Input.mousePosition.y - GetInputs.RotateActionStart.y) / Screen.height * MouseRotationSensitivity;

                        _rotateX.SetVelocity(x * mag * RotationMagnification * _resolution);
                        _rotateY.SetVelocity(y * mag * RotationMagnification * _resolution);

                    }

                    _rotateX.Update();
                    _rotateY.Update();

                    // Make sure camera doesn't rotate too far down that it spasms.
                    if (transform.rotation.eulerAngles.x > Consts.MaxCameraAngle && transform.rotation.eulerAngles.x < Consts.Angle90)
                        transform.rotation = Quaternion.Euler(Consts.MaxCameraAngle, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
                }
            }
            catch (NullReferenceException e)
            {
                if (_forward == null)
                {
#if UNITY_EDITOR
                    //Debug.LogError("_forward is null, quitting.");
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                }
                else
                {
                    Debug.LogError("Could not find mouse input: " + e);
                }
            }

            // Lock at object
            if (LookAtTarget != null)
            {
                transform.LookAt(LookAtTarget.transform);
                if (gameObject.GetComponent<Camera>().fieldOfView < MinimumZoom)
                {
                    ResetMovement();
                    gameObject.GetComponent<Camera>().fieldOfView = MinimumZoom;
                }
                else if (gameObject.GetComponent<Camera>().fieldOfView > MaximumZoom)
                {
                    ResetMovement();
                    gameObject.GetComponent<Camera>().fieldOfView = MaximumZoom;
                }
            }

            // Set ranges
            Vector3 endPosition = transform.position;

            if (LockX)
                endPosition.x = startPosition.x;
            if (LockY)
                endPosition.y = startPosition.y;
            if (LockZ)
                endPosition.z = startPosition.z;

            if (UseXRange && gameObject.transform.position.x < XRangeMin) endPosition.x = XRangeMin;
            if (UseXRange && gameObject.transform.position.x > XRangeMax) endPosition.x = XRangeMax;

            if (UseYRange && gameObject.transform.position.y < YRangeMin) endPosition.y = YRangeMin;
            if (UseYRange && gameObject.transform.position.y > YRangeMax) endPosition.y = YRangeMax;

            if (UseZRange && gameObject.transform.position.z < ZRangeMin) endPosition.z = ZRangeMin;
            if (UseZRange && gameObject.transform.position.z > ZRangeMax) endPosition.z = ZRangeMax;

            transform.position = endPosition;

            // Level Camera
            if (LevelCamera)
                LevelTheCamera();

        }

        public void ResetMovement()
        {
            _panX.SetVelocity(0);
            _panY.SetVelocity(0);
            _forward.SetVelocity(0);
            _rotateX.SetVelocity(0);
            _rotateY.SetVelocity(0);

            _panX.Update();
            _panY.Update();
            _forward.Update();
            _rotateX.Update();
            _rotateY.Update();
        }

        public void OnCollisionEnter(Collision collision)
        {
            ResetMovement();
        }

        public void PanY(float aMagnitude)
        {
            _panY.ChangeVelocity(0.005f * aMagnitude * _resolution * PanUpDownSensitivity);
        }

        public void PanX(float aMagnitude)
        {
            _panX.ChangeVelocity(-0.01f * aMagnitude * _resolution * PanLeftRightSensitivity);
        }

        public void ForwardBack(float aMagnitude)
        {
            _forward.ChangeVelocity(-0.005f * aMagnitude * _resolution * MovementSpeedMagnification);
        }

        public void LevelTheCamera()
        {
            transform.rotation = Quaternion.LookRotation(transform.forward.normalized, Vector3.up);
        }

    }

}