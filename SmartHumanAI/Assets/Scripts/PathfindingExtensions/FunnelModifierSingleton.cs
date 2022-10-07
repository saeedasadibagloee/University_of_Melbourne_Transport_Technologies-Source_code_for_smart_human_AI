using Pathfinding;
using UnityEngine;

namespace Assets.Scripts.PathfindingExtensions
{
    class FunnelModifierSingleton : MonoBehaviour
	{
		private static FunnelModifier instance;
		public static FunnelModifier Instance
		{
			get { return instance ?? (instance = FindObjectOfType<FunnelModifier>()); }
		}

		void Start()
		{
			instance = FindObjectOfType<FunnelModifier>();
		}
	}
}
