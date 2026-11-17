using System.Collections.Generic;
using UnityEngine;

namespace Kotwica.Orbits
{
    /// <summary>
    /// Moving reference frame of the station.
    /// The "anchor" ring (where the player last docked) always sits still at the scene origin;
    /// every other ring and the planet move relative to it.
    /// Why: the player can walk on a deck with normal physics (no moving platform problems)
    /// and coordinates stay small, so there is no floating-point jitter.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class StationFrame : MonoBehaviour
    {
        public static StationFrame Instance { get; private set; }

        private static readonly List<OrbitalRing> Rings = new List<OrbitalRing>();
        private static readonly List<FrameObject> FreeObjects = new List<FrameObject>();

        [Tooltip("Ring that starts as the still point of the scene.")]
        [SerializeField] private OrbitalRing anchor;

        [Tooltip("Gas giant visual; moved so it stays at the true orbit centre.")]
        [SerializeField] private Transform planet;

        public OrbitalRing Anchor => anchor;

        // Static lists survive play-mode restarts when domain reload is off; clear them
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Rings.Clear();
            FreeObjects.Clear();
            Instance = null;
        }

        public static void Register(OrbitalRing ring) { if (!Rings.Contains(ring)) Rings.Add(ring); }
        public static void Unregister(OrbitalRing ring) => Rings.Remove(ring);
        public static void Register(FrameObject obj) { if (!FreeObjects.Contains(obj)) FreeObjects.Add(obj); }
        public static void Unregister(FrameObject obj) => FreeObjects.Remove(obj);

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start() => Reposition(true);

        private void FixedUpdate() => Reposition(false);

        /// <summary>Places every ring and the planet relative to the anchor ring.</summary>
        private void Reposition(bool teleport)
        {
            double t = OrbitalClock.Now;
            Vector3 anchorOrbital = anchor != null ? anchor.GetOrbitalPosition(t) : Vector3.zero;

            foreach (OrbitalRing ring in Rings)
            {
                ring.ApplyScenePosition(ring.GetOrbitalPosition(t) - anchorOrbital, t, teleport);
            }

            if (planet != null) planet.position = -anchorOrbital;
        }

        /// <summary>
        /// Makes another ring the still point (called when a ship docks there).
        /// Shifts all free objects (player, ship, camera) so nothing visibly jumps.
        /// </summary>
        public void SetAnchor(OrbitalRing newAnchor)
        {
            if (newAnchor == null || newAnchor == anchor) return;

            Vector3 shift = -newAnchor.ScenePosition;
            Vector3 newAnchorVelocity = newAnchor.FrameVelocity;

            anchor = newAnchor;
            Reposition(true);

            // Velocities are now measured against the new anchor
            foreach (OrbitalRing ring in Rings)
            {
                ring.ResetFrameVelocity(ring == anchor ? Vector3.zero : ring.FrameVelocity - newAnchorVelocity);
            }

            foreach (FrameObject obj in FreeObjects)
            {
                obj.ShiftBy(shift, newAnchorVelocity);
            }

            Physics.SyncTransforms();
        }
    }
}
