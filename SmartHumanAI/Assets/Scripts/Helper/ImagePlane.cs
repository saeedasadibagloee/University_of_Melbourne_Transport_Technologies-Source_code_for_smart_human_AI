using System;
using System.IO;
using UnityEngine;

namespace Assets.Scripts
{
    public class ImagePlane : MonoBehaviour
    {
        private static ImagePlane _instance;
        private readonly Vector3 _activatePos = new Vector3(0f, -0.9f, 0f);
        private Vector3 _deactivatedPos;
        private float _imageScale = 0.02f;
        private const float ImageScalingAmount = 1.01f;
        private Texture _tex;
        private bool _enableDisplay = true;
        private int currentLevel = 0;

        internal static ImagePlane Instance
        {
            get { return _instance ?? (_instance = FindObjectOfType<ImagePlane>()); }
        }

        // Use this for initialization
        private void Start()
        {
            _deactivatedPos = transform.position;
        }

        // Update is called once per frame
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    ChangeLevelBy(1);
                else
                {
                    _imageScale *= ImageScalingAmount;
                    UpdateScale();
                }
            }
            else if (Input.GetKeyDown(KeyCode.PageDown))
            {
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    ChangeLevelBy(-1);
                else
                {
                    _imageScale *= 1f / ImageScalingAmount;
                    UpdateScale();
                }
            }

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                    ChangePosition(0, -1);
                if (Input.GetKeyDown(KeyCode.DownArrow))
                    ChangePosition(0, 1);
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                    ChangePosition(1, 0);
                if (Input.GetKeyDown(KeyCode.RightArrow))
                    ChangePosition(-1, 0);
            }
        }

        private void ChangePosition(int x, int y)
        {
            transform.position += new Vector3(x, 0, y);
        }

        private void ChangeLevelBy(int v)
        {
            currentLevel += v;
            if (currentLevel < 0)
                currentLevel = 0;

            transform.position = Create.Instance.ToFh(transform.position, currentLevel) + new Vector3(0, 0.1f, 0);
        }

        internal void Activate(string path)
        {
            float xMiddle = Create.Instance.Width / 2f;
            float yMiddle = Create.Instance.Height / 2f;

            transform.position = new Vector3(xMiddle, 0f, yMiddle) + _activatePos;
            _tex = LoadImage(path);
            GetComponent<MeshRenderer>().material.mainTexture = _tex;
            UpdateScale();
        }

        private void UpdateScale()
        {
            if (_tex != null)
                transform.localScale = new Vector3(_tex.width * _imageScale, _tex.height * _imageScale, 1f);
        }

        internal void Deactivate()
        {
            transform.position = _deactivatedPos;
        }

        public static Texture2D LoadImage(string filePath)
        {

            Texture2D tex = null;

            if (!File.Exists(filePath)) return null;
            byte[] fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            return tex;
        }

        internal void ToggleDisplay()
        {
            _enableDisplay = !_enableDisplay;
            GetComponent<MeshRenderer>().enabled = _enableDisplay;
        }
    }
}
