using Kotwica.Combat;
using Kotwica.Core;
using Kotwica.Gameplay;
using Kotwica.Player;
using Kotwica.Progression;
using Kotwica.Security;
using Kotwica.World;
using UnityEngine;

namespace Kotwica.Vehicles
{
    /// <summary>
    /// A cart / scooter / hover car parked in a ring district. F gets in (stealing a resident's vehicle
    /// is a crime when someone sees it), WASD drives on the deck in the district's own coordinates
    /// (kinematic, no physics: rings rotate), Space brakes, F gets out when slow.
    /// Running people over hurts them (and is a crime), walls and railings stop the vehicle.
    /// </summary>
    public class RingVehicle : MonoBehaviour, IInteractable
    {
        public static RingVehicle Driven { get; private set; }

        [SerializeField] private string vehicleName = "Wózek";
        [Tooltip("Empty = free to use; otherwise it belongs to this resident and taking it is theft.")]
        [SerializeField] private string ownerName;

        [Header("Driving")]
        [SerializeField] private float maxSpeed = 10f;
        [SerializeField] private float reverseSpeed = 3.5f;
        [SerializeField] private float acceleration = 5f;
        [SerializeField] private float braking = 14f;
        [Tooltip("Degrees per second at full steering once rolling.")]
        [SerializeField] private float turnRate = 75f;

        [Header("Shape (local)")]
        [Tooltip("Where the driver stands, local to the vehicle.")]
        [SerializeField] private Vector3 seat = new Vector3(0f, 0.45f, -0.3f);
        [SerializeField] private Vector3 exitOffset = new Vector3(-1.6f, 0f, 0f);
        [Tooltip("Half width (x) and half length (z) used for collision checks.")]
        [SerializeField] private Vector2 halfSize = new Vector2(0.8f, 1.5f);

        [Header("Drivable area (district coordinates)")]
        [SerializeField] private Vector2 boundsX = new Vector2(-15f, 117f);
        [SerializeField] private Vector2 boundsZ = new Vector2(-20.5f, 20.5f);

        private Collider[] solids;
        private PlayerController driver;
        private Behaviour[] driverParts;
        private float speed;
        private float baseY;
        private int enteredFrame;
        private Health lastHit;
        private float lastHitTime;
        private readonly RaycastHit[] hits = new RaycastHit[12];
        private GUIStyle hintStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Driven = null;

        private void Awake()
        {
            solids = GetComponentsInChildren<Collider>();
            baseY = transform.localPosition.y;
        }

        // --- IInteractable -------------------------------------------------------------------------

        public string Prompt => string.IsNullOrEmpty(ownerName) ? $"Wsiądź: {vehicleName}" : $"Ukradnij: {vehicleName} (własność: {ownerName})";
        public bool CanInteract => Driven == null && driver == null;

        public void Interact()
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) Enter(player);
        }

        // --- Getting in and out --------------------------------------------------------------------

        private void Enter(PlayerController player)
        {
            if (!string.IsNullOrEmpty(ownerName))
            {
                // Somebody's property: the theft is a crime, witnessed if anyone looks
                bool seen = Witnesses.AnyoneSees(transform.position);
                GameServices.ReportCrime(null, CrimeType.Kradziez, seen);
                GameState.Notify(seen ? $"Kradzież pojazdu! Ktoś to widział." : $"Nikt nie widział. {vehicleName} jest twój... na razie.");
                ownerName = null;
            }

            driver = player;
            Driven = this;
            enteredFrame = Time.frameCount;
            speed = 0f;

            // The character rides along: its own movement, gun and F-key handling pause meanwhile
            player.SetCrouched(false);
            driverParts = new Behaviour[] { player, player.GetComponent<PlayerShooter>(), player.GetComponent<PlayerInteractor>() };
            foreach (Behaviour part in driverParts)
                if (part != null) part.enabled = false;
            foreach (Collider c in solids) c.enabled = false; // camera and the vehicle's own sweep must not hit it
            SnapDriver();
        }

        private void Exit()
        {
            if (driver == null) return;
            PlayerController player = driver;
            driver = null;
            if (Driven == this) Driven = null;
            speed = 0f;

            foreach (Collider c in solids) c.enabled = true;
            Vector3 outside = transform.TransformPoint(exitOffset) + transform.up * 0.05f;
            player.TeleportTo(outside, Quaternion.LookRotation(transform.forward, transform.up));
            foreach (Behaviour part in driverParts)
                if (part != null) part.enabled = true;
        }

        /// <summary>Throws the player out of any vehicle (arrest, death, scene change).</summary>
        public static void ForceExit()
        {
            if (Driven != null) Driven.Exit();
        }

        private void OnDisable() => Exit(); // district unloaded while driving: never leave the player hanging

        // --- Driving -------------------------------------------------------------------------------

        private void Update()
        {
            if (driver == null) return;
            if (!driver.gameObject.activeInHierarchy)
            {
                Exit();
                return;
            }

            if (Time.frameCount != enteredFrame && GameInput.InteractPressed())
            {
                if (Mathf.Abs(speed) < 2f)
                {
                    Exit();
                    return;
                }
                GameState.Notify("Zwolnij, żeby wysiąść.");
            }

            Vector2 input = GameInput.Move();
            bool brake = GameInput.Vertical() > 0.5f || GameInput.BrakeHeld();
            float top = maxSpeed * GameServices.Bonus(PlayerAttribute.Pilotaz, 0.03f);

            float target = brake ? 0f : input.y > 0.1f ? top : input.y < -0.1f ? (speed > 0.5f ? 0f : -reverseSpeed) : 0f;
            bool slowing = brake || Mathf.Abs(target) < Mathf.Abs(speed) || Mathf.Sign(target) != Mathf.Sign(speed);
            float rate = slowing ? (brake || Mathf.Abs(input.y) > 0.1f ? braking : acceleration * 0.6f) : acceleration;
            speed = Mathf.MoveTowards(speed, target, rate * Time.deltaTime);

            // Steering only bites while rolling; reversing turns the other way like a real cart
            float grip = Mathf.Clamp01(Mathf.Abs(speed) / 3f) * Mathf.Sign(speed);
            float turn = input.x * turnRate * grip * Time.deltaTime;
            transform.localRotation = Quaternion.AngleAxis(turn, Vector3.up) * transform.localRotation;

            Move(speed * Time.deltaTime);
            SnapDriver();

            if (Mathf.Abs(speed) > 3f) GameServices.Practice(PlayerAttribute.Pilotaz, Time.deltaTime * 0.5f);
        }

        /// <summary>Sweeps ahead: people get hit, everything else stops the vehicle.</summary>
        private void Move(float step)
        {
            if (Mathf.Abs(step) < 0.0001f) return;
            Vector3 direction = transform.forward * Mathf.Sign(step);
            float distance = Mathf.Abs(step);
            Vector3 center = transform.position + transform.up * 0.75f;
            var extents = new Vector3(halfSize.x, 0.45f, 0.1f);

            int count = Physics.BoxCastNonAlloc(center, extents, direction, hits, transform.rotation, distance + halfSize.y,
                ~0, QueryTriggerInteraction.Ignore);
            float allowed = distance;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (driver != null && hit.collider.transform.IsChildOf(driver.transform)) continue;
                if (hit.distance <= 0f) continue; // started inside (e.g. a body lying there): drive on

                Health victim = hit.collider.GetComponentInParent<Health>();
                if (victim != null && victim.IsDead) continue; // bodies do not stop the cart

                float free = Mathf.Max(0f, hit.distance - halfSize.y - 0.05f);
                if (victim != null) HitPerson(victim, direction);
                else if (Mathf.Abs(speed) > 6f) GameState.Notify("Uderzenie!");
                allowed = Mathf.Min(allowed, free);
                speed = victim != null ? speed * 0.3f : 0f;
            }

            transform.position += direction * allowed;

            // Stay on the deck: fixed height, inside the street and port area
            Vector3 local = transform.localPosition;
            local.x = Mathf.Clamp(local.x, boundsX.x, boundsX.y);
            local.z = Mathf.Clamp(local.z, boundsZ.x, boundsZ.y);
            local.y = baseY;
            transform.localPosition = local;
        }

        private void HitPerson(Health victim, Vector3 direction)
        {
            if (victim == lastHit && Time.time - lastHitTime < 0.8f) return;
            lastHit = victim;
            lastHitTime = Time.time;
            if (Mathf.Abs(speed) < 2.5f) return; // a nudge, not an accident

            // Damage grows with speed; residents and guards report the assault themselves (witnesses, panic)
            victim.TakeDamage(Mathf.Abs(speed) * 6f, direction, "Potrącony");
            GameState.Notify("Potrącenie!");
        }

        private void SnapDriver()
        {
            if (driver == null) return;
            driver.transform.SetPositionAndRotation(transform.TransformPoint(seat), Quaternion.LookRotation(transform.forward, transform.up));
        }

        private void OnGUI()
        {
            if (driver == null) return;
            hintStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.85f, 0.9f, 1f) } };
            string text = $"{vehicleName} · {Mathf.Abs(speed) * 3.6f:0} km/h   W/S gaz · A/D skręt · Spacja hamulec · F wysiądź";
            GUI.Label(new Rect(0f, Screen.height - 70f, Screen.width, 24f), text, hintStyle);
        }
    }
}
