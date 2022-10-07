using UnityEngine;

namespace Helper
{
    public class ToggleChildren : MonoBehaviour
    {
        private bool _childrenVisible = false;

        void Start()
        {
            foreach (Transform t in transform)
            {
                t.gameObject.SetActive(false);
            }
            _childrenVisible = false;
        }

        public void ToggleChildrenVisibility()
        {
            if (_childrenVisible)
            {
                foreach (Transform t in transform)
                    t.gameObject.SetActive(false);

                _childrenVisible = false;
            }
            else
            {
                foreach (Transform t in transform)
                    t.gameObject.SetActive(true);

                _childrenVisible = true;
            }
        }

        public void ToggleChildrenVisibility(bool visibility)
        {
            if (visibility != _childrenVisible)
                ToggleChildrenVisibility();
        }
    }
}
