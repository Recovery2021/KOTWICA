using UnityEngine;

namespace Kotwica.Orbits
{
    /// <summary>
    /// Moves one habitat ring along a circular orbit around the gas giant
    /// and spins it around its own axis (spin = artificial gravity).
    /// Rings with different radius/period drift relative to each other,
    /// which is the core "city in motion" mechanic of the game.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class OrbitalRing : MonoBehaviour
    {
        [Header("Orbit around the planet")]
        [Tooltip("Planet (orbit centre). If empty, world origin is used.")]
        [SerializeField] private Transform orbitCenter;

        [Tooltip("Orbit radius in metres.")]
        [SerializeField] private float orbitRadius = 2000f;

        [Tooltip("Time of one full orbit in orbital seconds.")]
        [SerializeField] private float orbitalPeriod = 600f;

        [Tooltip("Starting angle on the orbit in degrees.")]
        [SerializeField, Range(0f, 360f)] private float phaseDegrees = 0f;

        [Tooltip("Orbit tilt relative to the planet's equator, in degrees.")]
        [SerializeField, Range(-30f, 30f)] private float inclinationDegrees = 0f;

        [Header("Spin (artificial gravity)")]
        [Tooltip("Time of one full spin around the ring's own axis, in orbital seconds. 0 = no spin.")]
        [SerializeField] private float spinPeriod = 60f;

        private Rigidbody body;
        private Quaternion baseRotation;

        private void Awake()
        {
            // Kinematic body: we drive it ourselves, but physics objects
            // standing inside (player, crates) still get correct contacts.
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            baseRotation = transform.rotation;
        }

        private void FixedUpdate()
        {
            double t = OrbitalClock.Instance != null ? OrbitalClock.Instance.Time : Time.timeSinceLevelLoad;

            body.MovePosition(GetPositionAt(t));
            body.MoveRotation(GetRotationAt(t));
        }

        /// <summary>
        /// Ring position at any orbital time.
        /// Public so the navigation UI can predict "orbital windows" (when two rings come close).
        /// </summary>
        public Vector3 GetPositionAt(double orbitalTime)
        {
            Vector3 center = orbitCenter != null ? orbitCenter.position : Vector3.zero;

            // Angle along the orbit: start phase + full turns elapsed
            double angle = phaseDegrees * Mathf.Deg2Rad + 2d * Mathf.PI * (orbitalTime / orbitalPeriod);
            Vector3 flat = new Vector3((float)System.Math.Cos(angle), 0f, (float)System.Math.Sin(angle)) * orbitRadius;

            // Tilt the orbit plane
            return center + Quaternion.Euler(inclinationDegrees, 0f, 0f) * flat;
        }

        /// <summary>Ring rotation at any orbital time (spin around local Y axis).</summary>
        public Quaternion GetRotationAt(double orbitalTime)
        {
            if (spinPeriod <= 0f) return baseRotation;

            float spinAngle = (float)(360d * (orbitalTime / spinPeriod) % 360d);
            return baseRotation * Quaternion.AngleAxis(spinAngle, Vector3.up);
        }

#if UNITY_EDITOR
        // Draw the orbit in the Scene view so designers can see where rings will pass
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            const int segments = 128;
            Vector3 prev = GetPositionAt(0d);
            for (int i = 1; i <= segments; i++)
            {
                Vector3 next = GetPositionAt(orbitalPeriod * i / (double)segments);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
#endif
    }
}
