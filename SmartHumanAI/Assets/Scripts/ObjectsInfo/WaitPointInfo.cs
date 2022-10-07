using System;
using System.Runtime.Remoting.Channels;
using DataFormats;
using Helper;
using UnityEngine;

namespace Info
{
    class WaitPointInfo : MonoBehaviour
    {
        public int id = -1;
        public float radius = 3f;
        public float waitTime = 3f;
        public float wonderTime = 0f;
        public float interest = 0.5f;

        private PolygonHelper polyH = null;
        public ParticleSystem ps1;

        protected internal WaitPoint Get
        {
            get
            {
                WaitPoint wp = new WaitPoint
                {
                    id = id,
                    x = transform.position.x,
                    y = transform.position.z,
                    interest = interest,
                    waitTime = waitTime,
                    wonderTime = wonderTime,
                    radius = radius
                };

                return wp;
            }
        }

        public void Update()
        {
   
            }

            ps1.SetParticles(particles);
        }

        public void UpdateRadius(float distance)
        {
            radius = distance;

            if (Math.Abs(radius) < 0.001f)
            {
                ps1.gameObject.SetActive(false);
                ps1.gameObject.SetActive(false);
            }
            else
            {
                ps1.gameObject.SetActive(true);
                ps1.gameObject.SetActive(true);

                var velocityOverLifetime1 = ps1.velocityOverLifetime;
                velocityOverLifetime1.orbitalYMultiplier = -1 / radius;
                velocityOverLifetime1.yMultiplier = 0.04f / radius;

                var main1 = ps1.main;
                var startLife1 = main1.startLifetime;
                startLife1.constant = 35 * radius;
                main1.startLifetime = startLife1;

                ps1.transform.localPosition = new Vector3(5 * radius, ps1.transform.localPosition.y, ps1.transform.localPosition.z);

                ps1.Stop();
                ps1.Play();
            }
        }

        public void CopyFrom(WaitPoint wp)
        {
            id = wp.id;
            waitTime = wp.waitTime;
            wonderTime = wp.wonderTime;
            interest = wp.interest;

            UpdateRadius(wp.radius);
            TryGetPolyH();
        }

        internal void TryGetPolyH()
        {
            polyH = Create.Instance.GetRoomPoly(transform.position.x, transform.position.z);
            if (polyH.Points == null)
                polyH = null;

            if (ps1 != null)
            {
                ps1.Stop();
                ps1.Play();
            }
        }

        public void Apply()
        {
            id = ObjectInfo.Instance.ArtifactId++;
            TryGetPolyH();
        }
    }
}
