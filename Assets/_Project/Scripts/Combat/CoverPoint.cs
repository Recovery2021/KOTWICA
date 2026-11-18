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
        private const float StandingEye = 1.6f;
        private const float SideStep = 0.7f;

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

        /// <summary>
        /// Where to stand to shoot back: right here if the cover is low (over the top),
        /// otherwise a short step to the side of tall cover (lean out). False = no way to see the threat.
        /// </summary>
        public bool TryGetPeekPosition(Vector3 threatPoint, out Vector3 peek)
        {
            Vector3 here = transform.position;
            peek = here;
            if (HasClearShot(here, threatPoint)) return true;

            Vector3 toThreat = threatPoint - here;
            toThreat.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, toThreat.normalized);
            foreach (Vector3 candidate in new[] { here + side * SideStep, here - side * SideStep })
            {
                if (!HasRoom(candidate) || !HasClearShot(candidate, threatPoint)) continue;
                peek = candidate;
                return true;
            }
            return false;
        }

        private static bool HasClearShot(Vector3 feet, Vector3 threatPoint)
        {
            Vector3 eye = feet + Vector3.up * StandingEye;
            if (!Physics.Linecast(eye, threatPoint, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.collider.GetComponentInParent<Health>() != null; // the first thing hit is a character
        }

        private static bool HasRoom(Vector3 feet)
        {
            // A body-sized capsule must fit (not inside a wall or crate); characters do not count
            Collider[] blockers = Physics.OverlapCapsule(feet + Vector3.up * 0.5f, feet + Vector3.up * 1.5f, 0.3f, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider c in blockers)
                if (c.GetComponentInParent<Health>() == null) return false;
            return true;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Occupant != null ? Color.red : new Color(0.2f, 0.9f, 1f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.4f, 0.3f);
        }
    }
}
