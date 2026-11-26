using System.Collections.Generic;
using Kotwica.Core;
using UnityEngine;

namespace Kotwica.World
{
    /// <summary>
    /// A resident living by the clock: walks between home, work, school and leisure spots of its district.
    /// Moves in the district's own coordinates (no physics), so it rides along with the orbiting ring.
    /// At home it goes inside (disappears). When the ring's alert is 2+ it hurries home.
    /// Walking past shows a short line (no interaction).
    /// </summary>
    public class ResidentAI : MonoBehaviour, IInteractable
    {
        public static readonly List<ResidentAI> All = new List<ResidentAI>();

        [SerializeField] private string residentName = "Mieszkaniec";
        [SerializeField] private ResidentRole role;
        [SerializeField] private string ringId = "kesh";

        [Header("Places (children of the same district)")]
        [SerializeField] private Transform home;
        [SerializeField] private Transform work;
        [SerializeField] private Transform school;
        [SerializeField] private Transform leisure;

        [Header("Walking")]
        [Tooltip("Distance of the walking lane from the street centre line (sign picked per trip).")]
        [SerializeField] private float laneOffset = 7f;
        [SerializeField] private float walkSpeed = 1.4f;

        private readonly List<Vector3> path = new List<Vector3>();
        private int pathIndex;
        private Activity current = (Activity)(-1);
        private Renderer[] renderers;
        private Collider[] colliders;
        private float personalOffset;   // hours: not everyone leaves at the same minute
        private Vector3 spotJitter;

        public bool IsVisible { get; private set; } = true;
        public Activity CurrentActivity => current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() => All.Remove(this);

        private void Start()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);

            // Stable per-person randomness from the name
            int seed = residentName.GetHashCode();
            personalOffset = ((seed & 0xFF) / 255f - 0.5f) * 1f;
            spotJitter = new Vector3((((seed >> 8) & 0xFF) / 255f - 0.5f) * 1.6f, 0f, (((seed >> 16) & 0xFF) / 255f - 0.5f) * 1.2f);

            // Appear where the schedule says right now (no crowd walking in from the port after loading)
            current = WantedActivity(out _);
            transform.localPosition = SpotFor(current);
            path.Clear();
            SetInside(current == Activity.Dom);
        }

        private void Update()
        {
            Activity wanted = WantedActivity(out bool panic);
            if (wanted != current)
            {
                current = wanted;
                BuildPath(SpotFor(current));
                SetInside(false);
            }

            if (pathIndex < path.Count) Walk(panic ? walkSpeed * 2.2f : walkSpeed);
        }

        private Activity WantedActivity(out bool panic)
        {
            panic = GameServices.Instance != null && GameServices.Instance.Alert.Level(ringId) >= 2;
            if (panic) return Activity.Dom;
            float hour = GameServices.Instance != null ? GameServices.Instance.Clock.Hour : 12f;
            return role != null ? role.ActivityAt(hour + personalOffset) : Activity.Dom;
        }

        private Vector3 SpotFor(Activity activity)
        {
            Transform spot = activity switch
            {
                Activity.Praca => work,
                Activity.Nauka => school,
                Activity.Wypoczynek => leisure,
                _ => home
            };
            if (spot == null) spot = home;
            return spot != null ? spot.localPosition + spotJitter : transform.localPosition;
        }

        /// <summary>Simple street route: out to the walking lane, along the street, in to the spot.</summary>
        private void BuildPath(Vector3 target)
        {
            path.Clear();
            pathIndex = 0;
            Vector3 from = transform.localPosition;

            if (Mathf.Abs(target.x - from.x) > 2f)
            {
                float side = Mathf.Sign(Mathf.Abs(target.z) > 0.5f ? target.z : (from.z == 0f ? 1f : from.z));
                float lane = side * laneOffset;
                path.Add(new Vector3(from.x, from.y, lane));
                path.Add(new Vector3(target.x, target.y, lane));
            }
            path.Add(target);
        }

        private void Walk(float speed)
        {
            Vector3 position = transform.localPosition;
            Vector3 next = path[pathIndex];
            Vector3 step = Vector3.MoveTowards(position, next, speed * Time.deltaTime);
            Vector3 direction = step - position;
            transform.localPosition = step;

            if (direction.sqrMagnitude > 0.000001f)
            {
                Quaternion face = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z), Vector3.up);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, face, 1f - Mathf.Exp(-8f * Time.deltaTime));
            }

            if ((step - next).sqrMagnitude < 0.0025f)
            {
                pathIndex++;
                if (pathIndex >= path.Count && current == Activity.Dom) SetInside(true); // went indoors
            }
        }

        private void SetInside(bool inside)
        {
            IsVisible = !inside;
            if (renderers != null) foreach (Renderer r in renderers) r.enabled = !inside;
            if (colliders != null) foreach (Collider c in colliders) c.enabled = !inside;
        }

        // --- Passing remark (shown, not usable) ---------------------------------------------------

        public string Prompt
        {
            get
            {
                string roleName = role != null ? role.roleName : "mieszkaniec";
                string line = role != null ? role.LineFor(current) : "";
                return $"{residentName} ({roleName}): „{line}”";
            }
        }

        public bool CanInteract => false;
        public void Interact() { }
    }

    /// <summary>Residents as witnesses: a crime seen by anyone nearby counts in full.</summary>
    public static class Witnesses
    {
        public static bool AnyoneSees(Vector3 worldPosition, float range = 15f)
        {
            foreach (ResidentAI resident in ResidentAI.All)
            {
                if (!resident.IsVisible) continue;
                Vector3 eye = resident.transform.position + resident.transform.up * 1.6f;
                if ((eye - worldPosition).sqrMagnitude > range * range) continue;

                // Clear line, or the first thing hit is (next to) the crime spot itself
                if (!Physics.Linecast(eye, worldPosition, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)) return true;
                if (hit.collider.transform.IsChildOf(resident.transform)) return true;
                if ((hit.point - worldPosition).sqrMagnitude < 2.25f) return true;
            }
            return false;
        }
    }
}
