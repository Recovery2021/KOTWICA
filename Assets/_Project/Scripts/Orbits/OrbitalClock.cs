using UnityEngine;

namespace Kotwica.Orbits
{
    /// <summary>
    /// Single source of "orbital time" for the whole station.
    /// All rings read time from here, so their positions are deterministic
    /// (easy to save/load and, in the future, to sync over the network).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class OrbitalClock : MonoBehaviour
    {
        public static OrbitalClock Instance { get; private set; }

        [Tooltip("How many orbital seconds pass per real second (speeds up testing).")]
        [SerializeField] private float timeScale = 1f;

        [Tooltip("Orbital time at scene start; restored from a save file later.")]
        [SerializeField] private double startTime = 0d;

        /// <summary>Current orbital time in seconds (double = no precision loss in long sessions).</summary>
        public double Time { get; private set; }

        /// <summary>Orbital time, or scene time when no clock exists (e.g. edit-mode tests).</summary>
        public static double Now => Instance != null ? Instance.Time : UnityEngine.Time.timeSinceLevelLoad;

        private void Awake()
        {
            // Keep exactly one clock in the scene
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Time = startTime;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void FixedUpdate()
        {
            // Advance in the physics step so moving rings and rigidbodies stay in sync
            Time += UnityEngine.Time.fixedDeltaTime * timeScale;
        }

        /// <summary>Jump to a given orbital time (used by save/load).</summary>
        public void SetTime(double orbitalTime) => Time = orbitalTime;
    }
}
