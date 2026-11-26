using System.Collections;
using System.Collections.Generic;
using Kotwica.Combat;
using Kotwica.Core;
using Kotwica.Player;
using UnityEngine;

namespace Kotwica.Security
{
    /// <summary>
    /// Station security officer spawned by SecurityResponse on the ring where the player is.
    /// Patrols near its post, chases the player on sight, searches the last known position for a while,
    /// arrests when close; when Armed (high alert) it also shoots. Killing one is murder (always witnessed).
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class SecurityGuard : MonoBehaviour, ICharacterPose
    {
        public static readonly List<SecurityGuard> All = new List<SecurityGuard>();

        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float runSpeed = 4.6f;
        [SerializeField] private float sightRange = 22f;
        [SerializeField] private float fieldOfView = 120f;
        [SerializeField] private float catchDistance = 1.6f;
        [SerializeField] private float searchTime = 15f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float fireInterval = 0.8f;
        [SerializeField] private Transform visual;

        private CharacterController controller;
        private Health health;
        private Health player;
        private PlayerController playerController;
        private Vector3 post;
        private Vector3 lastKnown;
        private float searchUntil;
        private Vector3 wanderTarget;
        private float nextWander;
        private float nextShot;
        private float verticalVelocity;

        public string RingId { get; private set; }
        public bool Armed { get; set; }
        public bool SeesPlayer { get; private set; }
        public bool IsCrouched => false;
        public bool IsAiming => Armed && SeesPlayer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() => All.Remove(this);

        public void SetVisual(Transform body) => visual = body;

        /// <summary>Called right after spawning.</summary>
        public void Init(string ringId, bool armed, Vector3? crimeScene)
        {
            RingId = ringId;
            Armed = armed;
            post = transform.position;
            wanderTarget = post;
            if (crimeScene.HasValue)
            {
                lastKnown = crimeScene.Value;
                searchUntil = Time.time + searchTime; // head straight to where it happened
            }
        }

        private void Update()
        {
            if (health.IsDead) return;

            player = Health.FindFirst(Team.Player);
            if (player != null && playerController == null) playerController = player.GetComponent<PlayerController>();
            SeesPlayer = CanSeePlayer();

            Vector3 velocity = Vector3.zero;
            if (SeesPlayer)
            {
                lastKnown = player.transform.position;
                searchUntil = Time.time + searchTime;
            }

            float distance = player != null ? Flat(player.transform.position - transform.position).magnitude : float.MaxValue;

            if (SeesPlayer && distance <= catchDistance && SecurityResponse.Instance != null)
            {
                SecurityResponse.Instance.Arrest(this);
                return;
            }

            if (SeesPlayer && Armed && distance > 4f && distance < 25f)
            {
                // Stand and shoot
                Face(player.transform.position);
                if (Time.time >= nextShot)
                {
                    nextShot = Time.time + fireInterval;
                    Shoot();
                }
            }
            else if (Time.time < searchUntil)
            {
                velocity = Toward(lastKnown, runSpeed, 0.5f);
            }
            else
            {
                // Bored patrol around the post
                if (Time.time >= nextWander || Flat(wanderTarget - transform.position).magnitude < 0.5f)
                {
                    Vector2 r = Random.insideUnitCircle * 6f;
                    wanderTarget = post + new Vector3(r.x, 0f, r.y);
                    nextWander = Time.time + Random.Range(4f, 8f);
                }
                velocity = Toward(wanderTarget, walkSpeed, 0.5f);
            }

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += -9.81f * Time.deltaTime;
            controller.Move((velocity + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        private Vector3 Toward(Vector3 target, float speed, float stopDistance)
        {
            Vector3 to = Flat(target - transform.position);
            if (to.magnitude <= stopDistance) return Vector3.zero;
            Face(target);
            return to.normalized * speed;
        }

        private void Face(Vector3 target)
        {
            Vector3 to = Flat(target - transform.position);
            if (to.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 1f - Mathf.Exp(-8f * Time.deltaTime));
        }

        private Vector3 Eye => transform.position + Vector3.up * 1.6f;
        private Vector3 PlayerChest => player.transform.position + Vector3.up * (playerController != null ? playerController.ChestHeight : 1.3f);

        private bool CanSeePlayer()
        {
            if (player == null || player.IsDead || !player.gameObject.activeInHierarchy) return false;
            Vector3 to = PlayerChest - Eye;
            if (to.magnitude > sightRange) return false;
            if (Vector3.Angle(transform.forward, to) > fieldOfView * 0.5f && to.magnitude > 3f) return false;
            if (!Physics.Raycast(Eye, to.normalized, out RaycastHit hit, to.magnitude + 0.5f, ~0, QueryTriggerInteraction.Ignore)) return false;
            return hit.collider.GetComponentInParent<Health>() == player;
        }

        /// <summary>Does this guard have a clear view of a point (crime witnesses)?</summary>
        public bool CanSee(Vector3 point)
        {
            if (health.IsDead) return false;
            Vector3 to = point - Eye;
            if (to.magnitude > sightRange) return false;
            if (!Physics.Linecast(Eye, point, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            return (hit.point - point).sqrMagnitude < 2.25f;
        }

        private void Shoot()
        {
            Vector3 origin = Eye + transform.forward * 0.5f - Vector3.up * 0.25f;
            Vector3 chest = PlayerChest;
            float chance = 0.5f * Mathf.Clamp01(1f - Vector3.Distance(origin, chest) / 35f);
            if (playerController != null && playerController.IsCrouched) chance *= 0.7f;
            Vector3 aim = Random.value < chance ? chest : chest + Random.insideUnitSphere * 1.2f;
            Vector3 direction = (aim - origin).normalized;
            Vector3 end = origin + direction * 40f;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                if (hit.collider.GetComponentInParent<Health>() == player) player.TakeDamage(damage, direction, "Zastrzelony przez ochronę");
            }
            ShotEffects.Tracer(origin, end, new Color(0.4f, 0.7f, 1f));
            ShotEffects.Flash(origin, new Color(0.5f, 0.7f, 1f), 3f, 4f);
        }

        private void OnDamaged(Health self, Vector3 direction)
        {
            // Attacking an officer: witnessed by definition, and now they know where you are
            GameServices.ReportCrime(RingId, CrimeType.Napasc, true);
            if (player != null)
            {
                lastKnown = player.transform.position;
                searchUntil = Time.time + searchTime;
            }
        }

        private void OnDied(Health self)
        {
            GameServices.ReportCrime(RingId, CrimeType.Zabojstwo, true);
            controller.enabled = false;
            StartCoroutine(FallAndVanish());
        }

        private IEnumerator FallAndVanish()
        {
            if (visual != null)
            {
                Quaternion start = visual.localRotation;
                Quaternion end = Quaternion.Euler(-85f, 0f, 0f) * start;
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f)
                {
                    visual.localRotation = Quaternion.Slerp(start, end, t * t);
                    yield return null;
                }
            }
            yield return new WaitForSeconds(30f);
            Destroy(gameObject);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
