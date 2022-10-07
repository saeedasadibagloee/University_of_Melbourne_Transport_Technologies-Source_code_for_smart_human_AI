using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace OxOD
{
    public class FileListElement : MonoBehaviour
    {
        public Image icon;
        public Text elementName;
        public Text size;
        public Text date;

        private float _previousClickTime = 0;
        public float DoubleClickTime = 0.5f;
        public FileDialog instance;
        public bool isFile;
        public string data;

        void Start()
        {

        }
        
        void Update()
        {

        }

        public void OnClick()
        {
            if (!isFile)
            {
                instance.OpenDir(data);
            }
            else
            {
                if (Time.time > _previousClickTime + DoubleClickTime)
                {
                    instance.SelectFile(data);
                    _previousClickTime = Time.time;
                }
                else
                {
                    instance.OnCommitClick();
                }
            }
                
        }
    }
}