using Kotwica.Orbits;
using UnityEngine;

namespace Kotwica.Ship
{
    /// <summary>
    /// Landing pad on a ring deck. Its trigger volume tells a ship it may dock here;
    /// DockPoint is where the ship is parked after docking.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DockingPort : MonoBehaviour
    {
        [SerializeField] private OrbitalRing ring;
        [SerializeField] private Transform dockPoint;
        [Tooltip("Size of the approach zone above the pad where auto-docking can start (m).")]
        [SerializeField] private Vector3 approachZoneSize = new Vector3(40f, 30f, 40f);

        [Tooltip("Ports of other rings: docking without this permit = port guards seize the ship.")]
        [SerializeField] private bool requiresPermit;
        [Tooltip("Item that opens this port (e.g. przepustka_orrin, przepustka_varo).")]
        [SerializeField] private string permitItemId = "przepustka_varo";

        [Tooltip("Optional id of a second dock on the same ring (e.g. kesh_b); quests use port_<id>. Empty = the ring's main port.")]
        [SerializeField] private string portId;
        [Tooltip("Optional name shown instead of the ring name (e.g. 'Kesh · dok B').")]
        [SerializeField] private string displayName;

        public OrbitalRing Ring => ring;
        public string PortId => portId;
        public string DisplayName => !string.IsNullOrEmpty(displayName) ? displayName : ring != null ? ring.DisplayName : name;
        public bool RequiresPermit => requiresPermit;
        public string PermitItemId => permitItemId;
        public Transform DockPoint => dockPoint != null ? dockPoint : transform;

        private Vector3 localDockPoint;
        public Vector3 PositionAt(double time) =>
            ring.GetOrbitalPosition(time) + ring.Orientation(time) * localDockPoint;

        public Vector3 Velocity
        {
            get
            {
                var frame = StationFrame.Instance;
                if (frame == null || ring == null) return Vector3.zero;
                double t = OrbitalClock.Now;
                float dt = Time.fixedDeltaTime;
                return (frame.PointPosition(ring, localDockPoint, t) -
                        frame.PointPosition(ring, localDockPoint, t - dt * OrbitalClock.Rate)) / dt;
            }
        }

        private void Awake()
        {
            var zone = GetComponent<Collider>();
            zone.isTrigger = true;

            // Approach zone sits on the deck and reaches upwards
            if (zone is BoxCollider box)
            {
                box.size = approachZoneSize;
                box.center = new Vector3(0f, approachZoneSize.y * 0.5f, 0f);
            }
            if (ring == null) ring = GetComponentInParent<OrbitalRing>();
            if (ring != null) localDockPoint = ring.transform.InverseTransformPoint(DockPoint.position);
        }

        private void OnTriggerEnter(Collider other)
        {
            ShipController ship = FindShip(other);
            if (ship != null) ship.EnterPortRange(this);
        }

        private void OnTriggerExit(Collider other)
        {
            ShipController ship = FindShip(other);
            if (ship != null) ship.ExitPortRange(this);
        }

        private static ShipController FindShip(Collider other) =>
            other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<ShipController>() : null;
    }
}
