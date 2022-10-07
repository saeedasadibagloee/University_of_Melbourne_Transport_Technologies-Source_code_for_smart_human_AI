using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.ObjectsInfo
{
    public abstract class BaseObject : MonoBehaviour
    {
        public Vector3 endPolePos = Vector3.zero;
        public Vector3 startPolePos = Vector3.zero;

        public abstract void StartClick();
        public virtual void Adjust()
        {
            endPolePos = Vector3.zero;
            startPolePos = Create.Instance.Prefabs[(int)Create.Prefab.SPole].transform.position;
        }
        public abstract void EndClick();
        public abstract Vector3 PositionOffset();
        public abstract void UpdateInfo();

        public void ChangeMaterial(bool transparent, int materialId)
        {
            foreach (MeshRenderer mR in gameObject.GetComponentsInChildren<MeshRenderer>())
                mR.material = transparent ? Create.Instance.MaterialsTransparent[materialId] : Create.Instance.MaterialsOpaque[materialId];
        }

        internal Vector3 ToLh(Vector3 vector3)
        {
            return Create.Instance.ToLh(vector3);
        }

        public List<GameObject> Prefabs
        {
            get
            {
                return Create.Instance.Prefabs;
            }
        }

        public Vector3 GetWorldPoint(bool v)
        {
            return Create.Instance.GetWorldPoint(v);
        }

        public Vector3 ClosestPole(Vector3 vector3)
        {
            return Create.Instance.ClosestPole(vector3);
        }
    }
}
