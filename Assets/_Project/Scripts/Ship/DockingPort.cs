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

        public OrbitalRing Ring => ring;
        public Transform DockPoint => dockPoint != null ? dockPoint : transform;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (ring == null) ring = GetComponentInParent<OrbitalRing>();
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
