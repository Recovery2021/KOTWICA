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
            Quaternion inverse = Quaternion.Inverse(anchor.Orientation(t));
            Vector3 origin = anchor.GetOrbitalPosition(t);
            foreach (var ring in Rings)
                ring.Place(inverse * (ring.GetOrbitalPosition(t) - origin), inverse * ring.Orientation(t), teleport);
            if (planet != null) planet.position = inverse * -origin;
        }

        public Vector3 PointPosition(OrbitalRing ring, Vector3 local, double t) =>
            Quaternion.Inverse(anchor.Orientation(t)) *
            (ring.GetOrbitalPosition(t) + ring.Orientation(t) * local - anchor.GetOrbitalPosition(t));

        public void SetAnchor(OrbitalRing newAnchor)
        {
            if (newAnchor == null || newAnchor == anchor) return;
            double t = OrbitalClock.Now;
            Vector3 origin = PointPosition(newAnchor, Vector3.zero, t);
            Quaternion rotation = Quaternion.Inverse(newAnchor.Orientation(t)) * anchor.Orientation(t);
            anchor = newAnchor;
            Reposition(true);
            foreach (var obj in FreeObjects)
                obj.ChangeFrame(origin, rotation);
            Physics.SyncTransforms();
        }
    }
}
