using Kotwica.Core;
using Kotwica.Gameplay;
using Kotwica.Orbits;
using Kotwica.Player;
using UnityEngine;

namespace Kotwica.Ship
{
    /// <summary>
    /// Small Newtonian ship: thrusters push it, nothing slows it down except the brake (X).
    /// Handles boarding, leaving, docking (only below a safe relative speed) and undocking.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ShipController : MonoBehaviour
    {
        [Header("Flight (accelerations in m/s²)")]
        [SerializeField] private float forwardAcceleration = 14f;
        [SerializeField] private float strafeAcceleration = 8f;
        [SerializeField] private float verticalAcceleration = 8f;
        [Tooltip("Mouse turning strength.")]
        [SerializeField] private float lookSensitivity = 0.15f;
        [SerializeField] private float rollAcceleration = 2f;
        [Tooltip("How fast X matches speed with the target ring.")]
        [SerializeField] private float brakeAcceleration = 10f;

        [Header("Docking")]
        [Tooltip("Max speed relative to the target ring that still allows docking.")]
        [SerializeField] private float maxDockingSpeed = 4f;
        [SerializeField] private float undockPushSpeed = 3f;
        [SerializeField] private DockingPort startDockedAt;

        [Header("References")]
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private PlayerController player;
        [Tooltip("Where the player appears after leaving the ship.")]
        [SerializeField] private Transform exitPoint;

        private Rigidbody body;
        private DockingPort portInRange;
        private DockingPort dockedAt;
        private Vector2 lookAccumulated;
        private int boardedFrame = -1;

        public bool IsPiloted { get; private set; }
        public bool IsDocked => dockedAt != null;
        public DockingPort DockedAt => dockedAt;
        public DockingPort PortInRange => portInRange;
        public float MaxDockingSpeed => maxDockingSpeed;
        public bool CanDock => !IsDocked && portInRange != null && VelocityRelativeTo(portInRange).magnitude <= maxDockingSpeed;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;          // space: no gravity outside the decks
            body.linearDamping = 0f;          // no "air drag", true inertia
            body.angularDamping = 2.5f;       // gyros calm rotation, keeps it flyable
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void Start()
        {
            if (startDockedAt != null) SnapToPort(startDockedAt);
        }

        /// <summary>Ship velocity as seen from a port's ring (0 = matched speed).</summary>
        public Vector3 VelocityRelativeTo(DockingPort port)
        {
            Vector3 shipVelocity = body.isKinematic ? Vector3.zero : body.linearVelocity;
            Vector3 ringVelocity = port != null && port.Ring != null ? port.Ring.FrameVelocity : Vector3.zero;
            return shipVelocity - ringVelocity;
        }

        private void Update()
        {
            if (!IsPiloted || Time.frameCount == boardedFrame) return;

            // Collect mouse movement every frame, apply it in FixedUpdate
            lookAccumulated += GameInput.Look();

            if (GameInput.InteractPressed())
            {
                if (IsDocked) ExitShip();
                else if (CanDock) Dock(portInRange);
                else if (portInRange != null) GameState.Notify($"Za szybko! Zwolnij poniżej {maxDockingSpeed:0} m/s.");
            }

            if (IsDocked && GameInput.Vertical() > 0.5f) Undock();
        }

        private void FixedUpdate()
        {
            if (!IsPiloted || IsDocked)
            {
                lookAccumulated = Vector2.zero;
                return;
            }

            // Translation thrusters (local axes)
            Vector2 move = GameInput.Move();
            Vector3 thrust = new Vector3(
                move.x * strafeAcceleration,
                GameInput.Vertical() * verticalAcceleration,
                move.y * forwardAcceleration);
            body.AddRelativeForce(thrust, ForceMode.Acceleration);

            // Rotation: mouse = pitch/yaw, Q/E = roll
            Vector3 turn = new Vector3(-lookAccumulated.y, lookAccumulated.x, 0f) * lookSensitivity;
            body.AddRelativeTorque(turn, ForceMode.VelocityChange);
            body.AddRelativeTorque(new Vector3(0f, 0f, -GameInput.Roll() * rollAcceleration), ForceMode.Acceleration);
            lookAccumulated = Vector2.zero;

            // Brake: flight computer matches speed with the reference ring (makes docking possible)
            if (GameInput.BrakeHeld())
            {
                float dt = Time.fixedDeltaTime;
                DockingPort reference = BrakeReference();
                Vector3 targetVelocity = reference != null && reference.Ring != null ? reference.Ring.FrameVelocity : Vector3.zero;
                body.linearVelocity = Vector3.MoveTowards(body.linearVelocity, targetVelocity, brakeAcceleration * dt);
                body.angularVelocity = Vector3.MoveTowards(body.angularVelocity, Vector3.zero, 3f * dt);
            }
        }

        /// <summary>Ring the brake matches speed with: the port we are at, else the job destination.</summary>
        public DockingPort BrakeReference()
        {
            if (portInRange != null) return portInRange;
            CourierJob job = GameState.ActiveJob;
            return job != null && job.Destination != null ? job.Destination.Port : null;
        }

        // --- Boarding --------------------------------------------------------

        public void Board()
        {
            if (!IsDocked || IsPiloted) return;

            IsPiloted = true;
            boardedFrame = Time.frameCount; // the same F press must not throw us out again
            player.gameObject.SetActive(false);
            cameraRig.FollowShip(transform);
            GameState.Notify("W kokpicie. Spacja = odcumowanie.");
        }

        private void ExitShip()
        {
            IsPiloted = false;
            player.TeleportTo(exitPoint.position, transform.rotation);
            player.gameObject.SetActive(true);
            cameraRig.FollowPlayer(player.transform);
        }

        // --- Docking ---------------------------------------------------------

        public void EnterPortRange(DockingPort port) => portInRange = port;

        public void ExitPortRange(DockingPort port)
        {
            if (portInRange == port) portInRange = null;
        }

        private void Dock(DockingPort port)
        {
            // The ring we dock at becomes the still point of the world
            if (StationFrame.Instance != null) StationFrame.Instance.SetAnchor(port.Ring);
            SnapToPort(port);
            GameState.Notify($"Zadokowano: {port.Ring.DisplayName}");
        }

        private void SnapToPort(DockingPort port)
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;

            Transform point = port.DockPoint;
            transform.SetPositionAndRotation(point.position, point.rotation);
            body.position = point.position;
            body.rotation = point.rotation;

            dockedAt = port;
            portInRange = port;
        }

        private void Undock()
        {
            dockedAt = null;
            body.isKinematic = false;
            body.linearVelocity = transform.up * undockPushSpeed; // gentle lift off the pad
            GameState.Notify("Odcumowano.");
        }
    }
}
