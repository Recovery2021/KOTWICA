using System.Collections.Generic;
using UnityEngine;

namespace Kotwica.Combat
{
    /// <summary>
    /// A spot next to a crate, pillar or wall where an enemy can crouch.
    /// Protection is checked live: a crouched enemy here must not be visible from the threat,
    /// so the same point is good cover from one side and useless from the other.
    /// </summary>
    public class CoverPoint : MonoBehaviour
    {
        public static readonly List<CoverPoint> All = new List<CoverPoint>();

        private const float CrouchedChest = 0.8f;

        /// <summary>Enemy currently using this point (one per point).</summary>
        public EnemyAI Occupant { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() => All.Remove(this);

        /// <summary>True when something solid (not a character) stands between this spot and the threat.</summary>
        public bool ProtectsFrom(Vector3 threatPoint)
        {
            Vector3 low = transform.position + Vector3.up * CrouchedChest;
            if (!Physics.Linecast(low, threatPoint, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)) return false;
            return hit.collider.GetComponentInParent<Health>() == null; // bodies are not cover
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Occupant != null ? Color.red : new Color(0.2f, 0.9f, 1f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.4f, 0.3f);
        }
    }
}
