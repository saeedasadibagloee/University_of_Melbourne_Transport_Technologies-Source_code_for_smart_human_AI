using Pathfinding;
using UnityEngine;

namespace Helper
{
    public class GroundHelper : MonoBehaviour
    {
        private static GroundHelper _instance = null;
        private Create _cr;
        private const float Border = 0.4f;
        private const float TilingFactor = 4f;

        public static GroundHelper Instance
        {
            get { return _instance ?? (_instance = FindObjectOfType<GroundHelper>()); }
        }

        void Start()
        {
            MakeGround();
        }

        // Use this for initialization
        public void MakeGround()
        {
            _cr = Create.Instance;

            float xScale = _cr.Width / 10f + Border;
            float yScale = _cr.Height / 10f + Border;

            transform.position = new Vector3(_cr.Width / 2f, transform.position.y, _cr.Height / 2f);
            transform.localScale = new Vector3(xScale, 1f, yScale);

            GetComponent<MeshRenderer>().material.mainTextureScale = new Vector2(xScale * TilingFactor, yScale * TilingFactor);

            // This holds all graph data
            AstarData data = AstarPath.active.data;

            foreach (GridGraph gg in data.graphs)
            {
                if (gg == null)
                    break;

                gg.width = _cr.Width * 2;
                gg.depth = _cr.Height * 2;
                Vector3 center = new Vector3(_cr.Width / 2f - 0.25f, gg.center.y, _cr.Height / 2f - 0.25f);
                gg.RelocateNodes(center, Quaternion.identity, 0.5f);

                // Updates internal size from the above values
                gg.UpdateSizeFromWidthDepth();
            }
        }
    }
}
