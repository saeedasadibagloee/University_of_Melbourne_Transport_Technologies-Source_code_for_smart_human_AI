using UnityEngine;

namespace Helper
{
    public class DensityDisplay : MonoBehaviour
    {
        private SkinnedMeshRenderer _smr = null;
        private MeshRenderer _mr = null;

        private bool isHighlighted = false;

        public void UpdateColor(Color c, bool lowPoleMode)
        {
            if (isHighlighted)
                return;

            if (lowPoleMode)
            {
                if (_mr == null)
                    _mr = GetComponentInChildren<MeshRenderer>();
                if (_mr != null)
                    _mr.material.color = c;
            } else
            {
                if (_smr == null)
                    _smr = GetComponentInChildren<SkinnedMeshRenderer>();
                if (_smr != null)
                    _smr.material.color = c;
            }
        }

        public void Highlighted(bool b)
        {
            isHighlighted = b;
        }
    }
}
