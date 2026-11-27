using Kotwica.Core;
using Kotwica.Gameplay;
using Kotwica.Orbits;
using Kotwica.Player;
using Kotwica.Quests;
using UnityEngine;

namespace Kotwica.Ship
{
    /// <summary>
    /// Player ship with two flight modes:
    /// - Flight assist ON (default): keys set the wanted speed RELATIVE TO THE NEAREST RING,
    ///   releasing them makes the ship hold position next to that ring even though it moves.
    /// - Flight assist OFF (Z): pure Newtonian thrust, for experienced pilots.
    /// Also handles boarding, auto-docking inside a port's approach zone and undocking.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ShipController : MonoBehaviour
    {
        [Header("Flight assist (Z toggles)")]
        [SerializeField] private bool flightAssist = true;
        [Tooltip("Top forward speed relative to the reference ring, m/s.")]
        [SerializeField] private float assistMaxForwardSpeed = 60f;
        [Tooltip("Top sideways / vertical speed relative to the reference ring, m/s.")]
        [SerializeField] private float assistMaxStrafeSpeed = 15f;
        [Tooltip("How hard thrusters chase the wanted velocity, m/s².")]
        [SerializeField] private float assistAcceleration = 14f;

        [Header("Newtonian mode (assist off), m/s²")]
        [SerializeField] private float forwardAcceleration = 14f;
        [SerializeField] private float strafeAcceleration = 8f;
        [SerializeField] private float verticalAcceleration = 8f;
        [Tooltip("How fast X matches speed with the reference ring.")]
        [SerializeField] private float brakeAcceleration = 12f;

        [Header("Rotation")]
        [Tooltip("Turn rate per unit of mouse movement (rad/s). Lower = calmer.")]
        [SerializeField] private float mouseTurnScale = 0.45f;
        [SerializeField] private float maxTurnRate = 1.2f;
        [SerializeField] private float rollRate = 1.0f;
        [Tooltip("How quickly rotation reaches the wanted rate (rad/s²). Lower = heavier ship.")]
        [SerializeField] private float turnResponse = 6f;

        [Header("Docking")]
        [Tooltip("Max speed relative to the ring that still allows auto-docking.")]
        [SerializeField] private float autoDockMaxSpeed = 12f;
        [Tooltip("Seconds the docking computer needs to park the ship.")]
        [SerializeField] private float autoDockDuration = 3f;
        [SerializeField] private float undockPushSpeed = 3f;
        [SerializeField] private DockingPort startDockedAt;
        [Tooltip("The player's home ring port: impounded ships end up here (Brodd's shipyard on Kesh).")]
        [SerializeField] private DockingPort homePort;

        [Header("References")]
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private PlayerController player;
        [Tooltip("Where the player appears after leaving the ship.")]
        [SerializeField] private Transform exitPoint;

        private Rigidbody body;
        private DockingPort[] allPorts;
        private DockingPort portInRange;
        private DockingPort dockedAt;
        private Vector2 lookAccumulated;
        private int boardedFrame = -1;

        // Auto-docking state (offsets are relative to the ring centre; ring roots never rotate)
        private DockingPort autoDockTarget;
        private float autoDockProgress;
        private Vector3 autoDockStartOffset;
        private Quaternion autoDockStartRotation;

        public bool IsPiloted { get; private set; }
        public bool IsDocked => dockedAt != null;
        public bool IsAutoDocking => autoDockTarget != null;
        public bool FlightAssist => flightAssist;
        public DockingPort DockedAt => dockedAt;
        public DockingPort PortInRange => portInRange;
        public float AutoDockMaxSpeed => autoDockMaxSpeed;

        /// <summary>Nearest ring's port: the frame the flight assist and the brake work against.</summary>
        public DockingPort ReferencePort { get; private set; }

        /// <summary>
        /// Docking is refused only during an alert lockdown (3+). A missing permit is not refused:
        /// the port guards seize the ship after it docks (see Seize).
        /// </summary>
        public bool PortAllowed => portInRange == null || !PortLockedDown(portInRange, 3);

        /// <summary>Ship papers bought from Brodd on Kesh: without them the ship cannot be boarded.</summary>
        public const string RegistrationItemId = "rejestracja_statku";
        public bool Owned => GameServices.Instance != null && GameServices.Instance.Inventory.Has(RegistrationItemId);

        /// <summary>Home-ring ports are always open; ports of other rings need that ring's permit.</summary>
        public static bool HasPermitFor(DockingPort port) =>
            port == null || !port.RequiresPermit ||
            (GameServices.Instance != null && GameServices.Instance.Inventory.Has(port.PermitItemId));

        /// <summary>Quest id of a port: port_kesh, port_kesh_b, port_orrin...</summary>
        public static string PortQuestId(DockingPort port) =>
            "port_" + (!string.IsNullOrEmpty(port.PortId) ? port.PortId : GameServices.RingIdFor(port.Ring));

        /// <summary>The port a "fly to" objective points at (for the HUD marker), or null.</summary>
        public DockingPort PortForObjective(string targetId)
        {
            if (allPorts == null || string.IsNullOrEmpty(targetId)) return null;
            foreach (DockingPort port in allPorts)
                if (port != null && PortQuestId(port) == targetId) return port;
            return null;
        }

        /// <summary>True when the alert on the port's ring is at least the given level.</summary>
        public static bool PortLockedDown(DockingPort port, int fromLevel)
        {
            if (GameServices.Instance == null || port == null || port.Ring == null) return false;
            string ring = GameServices.RingIdFor(port.Ring);
            return ring != null && GameServices.Instance.Alert.Level(ring) >= fromLevel;
        }
        public float CruiseSpeed => assistMaxForwardSpeed;

        public bool CanAutoDock => PortAllowed && !IsDocked && !IsAutoDocking && portInRange != null
                                   && VelocityRelativeTo(portInRange).magnitude <= autoDockMaxSpeed;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;          // space: no gravity outside the decks
            body.linearDamping = 0f;          // no "air drag", true inertia
            body.angularDamping = 0f;         // rotation is fully controlled in code
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void Start()
        {
            allPorts = FindObjectsByType<DockingPort>(FindObjectsSortMode.None);
            if (startDockedAt != null) SnapToPort(startDockedAt);
            ReferencePort = FindNearestPort();
        }

        /// <summary>Ship velocity as seen from a port's ring (0 = matched speed).</summary>
        public Vector3 VelocityRelativeTo(DockingPort port)
        {
            Vector3 shipVelocity = body.isKinematic ? Vector3.zero : body.linearVelocity;
            return shipVelocity - RingVelocity(port);
        }

        private static Vector3 RingVelocity(DockingPort port) =>
            port != null && port.Ring != null ? port.Velocity : Vector3.zero;

        private void Update()
        {
            if (!IsPiloted || Time.frameCount == boardedFrame) return;

            // Collect mouse movement every frame, apply it in FixedUpdate
            lookAccumulated += GameInput.Look();

            if (GameInput.FlightAssistTogglePressed() && !IsDocked)
            {
                flightAssist = !flightAssist;
                GameState.Notify(flightAssist ? "Asystent lotu: WŁĄCZONY" : "Asystent lotu: WYŁĄCZONY (pełna bezwładność)");
            }

            if (GameInput.InteractPressed() && !IsAutoDocking)
            {
                if (IsDocked) ExitShip();
                else if (portInRange != null && !PortAllowed) GameState.Notify("Port zablokowany: na tym pierścieniu trwa alarm.");
                else if (CanAutoDock) BeginAutoDock(portInRange);
                else if (portInRange != null) GameState.Notify($"Za szybko do dokowania! Zwolnij poniżej {autoDockMaxSpeed:0} m/s.");
            }

            if (IsDocked && GameInput.Vertical() > 0.5f) Undock();
        }

        private void FixedUpdate()
        {
            ReferencePort = FindNearestPort();

            if (IsAutoDocking)
            {
                StepAutoDock();
                lookAccumulated = Vector2.zero;
                return;
            }

            if (!IsPiloted || IsDocked)
            {
                lookAccumulated = Vector2.zero;
                return;
            }

            ApplyRotation();
            if (flightAssist) ApplyAssistedThrust();
            else ApplyNewtonianThrust();
        }

        // --- Flight ----------------------------------------------------------

        private void ApplyRotation()
        {
            // Mouse sets a wanted turn rate; the ship eases into it (feels heavy, never twitchy)
            Vector3 wanted = new Vector3(
                Mathf.Clamp(-lookAccumulated.y * mouseTurnScale, -maxTurnRate, maxTurnRate),
                Mathf.Clamp(lookAccumulated.x * mouseTurnScale, -maxTurnRate, maxTurnRate),
                -GameInput.Roll() * rollRate);
            lookAccumulated = Vector2.zero;

            Vector3 current = transform.InverseTransformDirection(body.angularVelocity);
            Vector3 next = Vector3.MoveTowards(current, wanted, turnResponse * Time.fixedDeltaTime);
            body.angularVelocity = transform.TransformDirection(next);
        }

        private void ApplyAssistedThrust()
        {
            // Wanted velocity = reference ring's velocity + what the pilot asks for in local axes
            Vector2 move = GameInput.Move();
            Vector3 localWanted = new Vector3(
                move.x * assistMaxStrafeSpeed,
                GameInput.Vertical() * assistMaxStrafeSpeed,
                move.y * assistMaxForwardSpeed);
            Vector3 wanted = RingVelocity(ReferencePort) + transform.TransformDirection(localWanted);

            float accel = GameInput.BrakeHeld() ? assistAcceleration * 1.5f : assistAcceleration;
            Vector3 next = Vector3.MoveTowards(body.linearVelocity, wanted, accel * Time.fixedDeltaTime);
            body.AddForce(next - body.linearVelocity, ForceMode.VelocityChange);
        }

        private void ApplyNewtonianThrust()
        {
            Vector2 move = GameInput.Move();
            Vector3 thrust = new Vector3(
                move.x * strafeAcceleration,
                GameInput.Vertical() * verticalAcceleration,
                move.y * forwardAcceleration);
            body.AddRelativeForce(thrust, ForceMode.Acceleration);

            // Brake: flight computer matches speed with the reference ring
            if (GameInput.BrakeHeld())
            {
                Vector3 next = Vector3.MoveTowards(body.linearVelocity, RingVelocity(ReferencePort), brakeAcceleration * Time.fixedDeltaTime);
                body.AddForce(next - body.linearVelocity, ForceMode.VelocityChange);
            }
        }

        private DockingPort FindNearestPort()
        {
            if (allPorts == null) return null;

            DockingPort nearest = null;
            float best = float.MaxValue;
            foreach (DockingPort port in allPorts)
            {
                if (port == null) continue;
                float distance = (port.transform.position - body.position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = port;
                }
            }
            return nearest;
        }

        // --- Boarding --------------------------------------------------------

        public void Board()
        {
            if (!IsDocked || IsPiloted || !Owned) return;

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

        private void BeginAutoDock(DockingPort port)
        {
            // Docking computer takes over: ship becomes kinematic and glides onto the pad
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;

            autoDockTarget = port;
            autoDockProgress = 0f;
            autoDockStartOffset = port.DockPoint.InverseTransformPoint(body.position);
            autoDockStartRotation = Quaternion.Inverse(port.DockPoint.rotation) * body.rotation;
            GameState.Notify($"Komputer dokujący przejmuje stery: {port.DisplayName}");
        }

        private void StepAutoDock()
        {
            DockingPort port = autoDockTarget;
            autoDockProgress += Time.fixedDeltaTime / Mathf.Max(0.1f, autoDockDuration);
            float s = Mathf.SmoothStep(0f, 1f, autoDockProgress);

            // Interpolate in the ring's frame, so the ring's own motion is followed exactly
            body.MovePosition(port.DockPoint.TransformPoint(Vector3.Lerp(autoDockStartOffset, Vector3.zero, s)));
            body.MoveRotation(port.DockPoint.rotation * Quaternion.Slerp(autoDockStartRotation, Quaternion.identity, s));

            if (autoDockProgress >= 1f)
            {
                autoDockTarget = null;
                Dock(port);
            }
        }

        private void Dock(DockingPort port)
        {
            if (!HasPermitFor(port))
            {
                Seize(port);
                return;
            }

            // The ring we dock at becomes the still point of the world
            if (StationFrame.Instance != null) StationFrame.Instance.SetAnchor(port.Ring);
            SnapToPort(port);
            GameServices.Practice(Kotwica.Progression.PlayerAttribute.Pilotaz, 15f);
            GameState.Notify($"Zadokowano: {port.DisplayName}");
            GameServices.Report(ObjectiveType.Lec, PortQuestId(port)); // "fly to dock B / Orrin / Varo" objectives
        }

        /// <summary>
        /// Port guards of a ring the player has no permit for: the ship is impounded (papers taken,
        /// ship towed to the home shipyard) and the pilot is sent back to the home ring on foot.
        /// </summary>
        private void Seize(DockingPort port)
        {
            DockingPort home = homePort != null ? homePort : startDockedAt;
            GameServices services = GameServices.Instance;
            if (services != null)
            {
                services.Inventory.Remove(RegistrationItemId);
                services.Quests.SetFlag("statek_skonfiskowany");
            }

            if (home != null)
            {
                if (StationFrame.Instance != null) StationFrame.Instance.SetAnchor(home.Ring);
                SnapToPort(home);
            }
            if (IsPiloted) ExitShip();
            GameState.Notify($"Straż portowa ({port.DisplayName}): brak przepustki. Statek skonfiskowany, odesłano cię na Kesh. Depozyt u Brodda.");
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
            if (PortLockedDown(dockedAt, 4))
            {
                GameState.Notify("Blokada portu: przy takim alarmie ochrona nie wypuści statku.");
                return;
            }
            dockedAt = null;
            body.isKinematic = false;
            body.linearVelocity = transform.up * undockPushSpeed; // gentle lift off the pad
            GameState.Notify("Odcumowano. Asystent lotu utrzyma statek przy pierścieniu.");
        }
    }
}
