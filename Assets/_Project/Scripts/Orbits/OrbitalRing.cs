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
        [Tooltip("Id linking this ring to its data (Resources/Rings) and its district scene Ring_<Id>, e.g. 'kesh'.")]
        [SerializeField] private string ringId;

        [Header("Orbit around the planet")]
        [Tooltip("Orbit radius in metres, measured from the planet centre.")]
        [SerializeField] private float orbitRadius = 600f;

        [Tooltip("Time of one full orbit in orbital seconds.")]
        [SerializeField] private float orbitalPeriod = 300f;

        [Tooltip("Starting angle on the orbit in degrees.")]
        [SerializeField, Range(0f, 360f)] private float phaseDegrees = 0f;

        [Tooltip("Orbit tilt relative to the planet's equator, in degrees.")]
        [SerializeField, Range(-30f, 30f)] private float inclinationDegrees = 0f;

        [Tooltip("Time of one full spin in orbital seconds. 0 = no spin.")]
        [SerializeField] private float spinPeriod = 60f;

        private Rigidbody body;
        public string DisplayName => displayName;
        public string RingId => ringId;

        private void Awake()
        {
            // Kinematic body: moved by code, but ships and players still collide with it properly
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void OnEnable() => StationFrame.Register(this);
        private void OnDisable() => StationFrame.Unregister(this);

        public Quaternion Orientation(double time)
        {
            float yaw = phaseDegrees + Angle(time, orbitalPeriod);
            float tilt = inclinationDegrees + Angle(time, spinPeriod);
            return Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(tilt, Vector3.right);
        }

        private static float Angle(double time, float period) =>
            period > 0f ? (float)(360d * (time % period) / period) : 0f;

        // The root is the port district on the rim. The common stellar centre is the origin.
        public Vector3 GetOrbitalPosition(double time) => Orientation(time) * (Vector3.forward * orbitRadius);

        public void Place(Vector3 position, Quaternion rotation, bool teleport)
        {
            if (teleport)
            {
                body.position = position;
                body.rotation = rotation;
                transform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                body.MovePosition(position);
                body.MoveRotation(rotation);
            }
        }
    }
}
