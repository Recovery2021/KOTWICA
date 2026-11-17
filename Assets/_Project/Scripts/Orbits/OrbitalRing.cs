using UnityEngine;

namespace Kotwica.Orbits
{
    /// <summary>
    /// One habitat ring of the station. Knows its circular orbit around the gas giant
    /// and spins its habitat part (spin = artificial gravity, visual in the prototype).
    /// Rings with different radius/period drift relative to each other:
    /// the core "city in motion" mechanic. Scene position is set by StationFrame.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class OrbitalRing : MonoBehaviour
    {
        [SerializeField] private string displayName = "Pierścień";

        [Header("Orbit around the planet")]
        [Tooltip("Orbit radius in metres, measured from the planet centre.")]
        [SerializeField] private float orbitRadius = 600f;

        [Tooltip("Time of one full orbit in orbital seconds.")]
        [SerializeField] private float orbitalPeriod = 300f;

        [Tooltip("Starting angle on the orbit in degrees.")]
        [SerializeField, Range(0f, 360f)] private float phaseDegrees = 0f;

        [Tooltip("Orbit tilt relative to the planet's equator, in degrees.")]
        [SerializeField, Range(-30f, 30f)] private float inclinationDegrees = 0f;

        [Header("Spin (artificial gravity)")]
        [Tooltip("Child that rotates (habitat torus, spokes). Docks and decks stay outside it.")]
        [SerializeField] private Transform spinPart;

        [Tooltip("Time of one full spin in orbital seconds. 0 = no spin.")]
        [SerializeField] private float spinPeriod = 60f;

        private Rigidbody body;
        private Vector3 lastScenePosition;

        public string DisplayName => displayName;

        /// <summary>Velocity of the ring in the current scene frame (needed for docking speed).</summary>
        public Vector3 FrameVelocity { get; private set; }

        /// <summary>Where the physics body is right now in the scene.</summary>
        public Vector3 ScenePosition => lastScenePosition;

        private void Awake()
        {
            // Kinematic body: moved by code, but ships and players still collide with it properly
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            lastScenePosition = body.position;
        }

        private void OnEnable() => StationFrame.Register(this);
        private void OnDisable() => StationFrame.Unregister(this);

        /// <summary>
        /// Ring position relative to the planet centre at any orbital time.
        /// Public so navigation can predict "orbital windows" (when two rings pass close).
        /// </summary>
        public Vector3 GetOrbitalPosition(double orbitalTime)
        {
            // Angle along the orbit: start phase + full turns elapsed
            double angle = phaseDegrees * Mathf.Deg2Rad + 2d * System.Math.PI * (orbitalTime / orbitalPeriod);
            Vector3 flat = new Vector3((float)System.Math.Cos(angle), 0f, (float)System.Math.Sin(angle)) * orbitRadius;

            // Tilt the orbit plane
            return Quaternion.Euler(inclinationDegrees, 0f, 0f) * flat;
        }

        /// <summary>
        /// Called by StationFrame every physics step.
        /// teleport = true when the reference frame changes (no velocity, no interpolation).
        /// </summary>
        public void ApplyScenePosition(Vector3 scenePosition, double orbitalTime, bool teleport)
        {
            if (body == null) body = GetComponent<Rigidbody>();

            if (teleport)
            {
                body.position = scenePosition;
                transform.position = scenePosition;
            }
            else
            {
                FrameVelocity = (scenePosition - lastScenePosition) / Time.fixedDeltaTime;
                body.MovePosition(scenePosition);
            }
            lastScenePosition = scenePosition;

            // Spin the habitat around the ring axis
            if (spinPart != null && spinPeriod > 0f)
            {
                float spinAngle = (float)(360d * (orbitalTime / spinPeriod) % 360d);
                spinPart.localRotation = Quaternion.AngleAxis(spinAngle, Vector3.up);
            }
        }

        /// <summary>Clears velocity after a reference-frame change (prevents a fake speed spike).</summary>
        public void ResetFrameVelocity(Vector3 velocity) => FrameVelocity = velocity;
    }
}
