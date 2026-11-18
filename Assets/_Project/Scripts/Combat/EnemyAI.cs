using System.Collections;
using System.Collections.Generic;
using Kotwica.Gameplay;
using Kotwica.Player;
using UnityEngine;

namespace Kotwica.Combat
{
    /// <summary>
    /// Prototype enemy soldier, a small state machine:
    /// Patrol  - walks between points, slowly "notices" the player (crouching = noticed slower);
    /// Combat  - runs to cover that hides it from the player, crouches, pops up for short bursts,
    ///           changes cover when the player flanks; hears gunshots; alerts nearby allies.
    /// Shots are real raycasts, so crates and pillars genuinely protect the player too.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class EnemyAI : MonoBehaviour
    {
        private enum State { Patrol, Combat, Dead }

        public static readonly List<EnemyAI> All = new List<EnemyAI>();

        [Header("Movement")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float walkSpeed = 1.6f;
        [SerializeField] private float runSpeed = 3.6f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Senses")]
        [SerializeField] private float viewDistance = 22f;
        [SerializeField] private float viewAngle = 60f;
        [Tooltip("Seconds of clear sight needed to spot a standing player.")]
        [SerializeField] private float detectTime = 1.4f;
        [SerializeField] private float hearingRange = 30f;
        [SerializeField] private float alertAlliesRange = 15f;

        [Header("Combat")]
        [SerializeField] private float damage = 12f;
        [SerializeField] private float burstInterval = 0.18f;
        [SerializeField] private int burstMin = 3;
        [SerializeField] private int burstMax = 5;
        [SerializeField] private float coverTimeMin = 1.2f;
        [SerializeField] private float coverTimeMax = 2.6f;
        [Tooltip("Hit chance at point blank against a standing player.")]
        [SerializeField] private float baseAccuracy = 0.6f;
        [SerializeField] private float preferredMinDistance = 5f;
        [SerializeField] private float preferredMaxDistance = 18f;

        [Header("References")]
        [SerializeField] private Transform visual;
        [SerializeField] private Transform muzzle;

        private CharacterController controller;
        private Health health;
        private Health player;
        private PlayerController playerController;

        private State state = State.Patrol;
        private int patrolIndex;
        private float detection;
        private Vector3 lastKnownPlayerPosition;
        private CoverPoint cover;
        private float nextCoverSearch;
        private bool crouched;
        private bool shootingPhase;
        private float phaseEndTime;
        private int burstLeft;
        private float nextShotTime;

        private Vector3 desiredVelocity;
        private Vector3 lookTarget;
        private float verticalVelocity;
        private Vector3 pullVelocity;
        private float pullUntil;
        private float stuckTimer;
        private Vector3 visualBaseScale = Vector3.one;
        private Vector3 visualBasePosition;

        public bool IsInCombat => state == State.Combat;
        public bool IsCrouched => crouched;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            if (visual != null)
            {
                visualBaseScale = visual.localScale;
                visualBasePosition = visual.localPosition;
            }
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            PlayerShooter.ShotFired += OnGunshotHeard;
        }

        private void OnDisable()
        {
            All.Remove(this);
            PlayerShooter.ShotFired -= OnGunshotHeard;
        }

        private void Start()
        {
            player = Health.FindFirst(Team.Player);
            if (player != null) playerController = player.GetComponent<PlayerController>();
            lookTarget = transform.position + transform.forward;
        }

        private void Update()
        {
            if (state == State.Dead) return;

            desiredVelocity = Vector3.zero;
            if (player == null || player.IsDead)
            {
                crouched = false;
                state = State.Patrol;
            }

            if (state == State.Patrol) UpdatePatrol();
            else UpdateCombat();

            ApplyBody();
            Move();
        }

        // --- Senses ---------------------------------------------------------------

        private Vector3 EyePosition => transform.position + Vector3.up * (crouched ? 1.0f : 1.6f);

        private Vector3 PlayerChest => player.transform.position + Vector3.up *
                                       (playerController != null ? playerController.ChestHeight : 1.3f);

        private bool CanSeePlayer(bool useViewCone)
        {
            if (player == null || player.IsDead) return false;

            Vector3 eye = EyePosition;
            Vector3 toPlayer = PlayerChest - eye;
            if (toPlayer.magnitude > viewDistance) return false;
            if (useViewCone && Vector3.Angle(transform.forward, toPlayer) > viewAngle) return false;

            if (!Physics.Raycast(eye, toPlayer.normalized, out RaycastHit hit, toPlayer.magnitude + 0.5f, ~0, QueryTriggerInteraction.Ignore))
                return false;
            return hit.collider.GetComponentInParent<Health>() == player;
        }

        private void OnGunshotHeard(Vector3 position)
        {
            if (state == State.Dead) return;
            if (Vector3.Distance(position, transform.position) <= hearingRange) EnterCombat(position);
        }

        private void OnDamaged(Health self, Vector3 direction)
        {
            if (player != null) EnterCombat(player.transform.position);
            // Getting shot in cover means the cover is bad: find another
            ReleaseCover();
            nextCoverSearch = 0f;
        }

        // --- Patrol ---------------------------------------------------------------

        private void UpdatePatrol()
        {
            crouched = false;

            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                Transform point = patrolPoints[patrolIndex % patrolPoints.Length];
                if (FlatDistance(transform.position, point.position) < 0.4f) patrolIndex++;
                else WalkTowards(point.position, walkSpeed);
            }

            // Spotting takes time: faster when close, slower when the player crouches
            if (CanSeePlayer(true))
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                float rate = 1f / detectTime;
                if (playerController != null && playerController.IsCrouched) rate *= 0.45f;
                if (distance < 8f) rate *= 2f;
                else if (distance > 12f) rate *= 0.6f;
                detection += rate * Time.deltaTime;
                lookTarget = player.transform.position; // turns head towards the suspicious shape
                if (detection >= 1f) EnterCombat(player.transform.position);
            }
            else
            {
                detection = Mathf.Max(0f, detection - 0.5f * Time.deltaTime);
            }
        }

        /// <summary>0..1 how close this enemy is to spotting the player (for the HUD).</summary>
        public float Suspicion => state == State.Patrol ? Mathf.Clamp01(detection) : 0f;

        // --- Combat ---------------------------------------------------------------

        public void EnterCombat(Vector3 knownPlayerPosition)
        {
            if (state != State.Patrol) return;

            bool firstToNotice = true;
            foreach (EnemyAI other in All)
                if (other != this && other.IsInCombat) firstToNotice = false;
            if (firstToNotice) GameState.Notify("Wykryto cię!");

            state = State.Combat;
            lastKnownPlayerPosition = knownPlayerPosition;
            shootingPhase = false;
            phaseEndTime = Time.time + Random.Range(0.3f, 0.8f);
            nextCoverSearch = 0f;

            // Shout to friends nearby
            foreach (EnemyAI other in All)
                if (other != this && Vector3.Distance(other.transform.position, transform.position) <= alertAlliesRange)
                    other.EnterCombat(knownPlayerPosition);
        }

        private void UpdateCombat()
        {
            bool sees = CanSeePlayer(false);
            if (sees) lastKnownPlayerPosition = player.transform.position;
            lookTarget = lastKnownPlayerPosition;

            Vector3 threat = lastKnownPlayerPosition + Vector3.up * 1.2f;

            // Player flanked us: this cover no longer hides us
            if (cover != null && !cover.ProtectsFrom(threat)) ReleaseCover();
            if (cover == null && Time.time >= nextCoverSearch)
            {
                FindCover(threat);
                nextCoverSearch = Time.time + 1.5f;
            }

            // Run to cover first
            if (cover != null && FlatDistance(transform.position, cover.transform.position) > 0.35f)
            {
                crouched = false;
                shootingPhase = false;
                RunTowards(cover.transform.position);
                return;
            }

            // No cover and no sight: push towards where the player was last seen
            if (cover == null && !sees && FlatDistance(transform.position, lastKnownPlayerPosition) > preferredMinDistance)
            {
                crouched = false;
                RunTowards(lastKnownPlayerPosition);
                return;
            }

            // In position: hide, pop up, fire a burst, hide again
            if (!shootingPhase)
            {
                crouched = cover != null;
                if (Time.time >= phaseEndTime)
                {
                    shootingPhase = true;
                    burstLeft = Random.Range(burstMin, burstMax + 1);
                    nextShotTime = Time.time + 0.3f; // time to stand up and aim
                }
            }
            else
            {
                crouched = false;
                if (Time.time >= nextShotTime)
                {
                    if (burstLeft > 0)
                    {
                        if (CanSeePlayer(false)) Shoot();
                        burstLeft--;
                        nextShotTime = Time.time + burstInterval;
                    }
                    else
                    {
                        shootingPhase = false;
                        phaseEndTime = Time.time + Random.Range(coverTimeMin, coverTimeMax);
                    }
                }
            }
        }

        private void FindCover(Vector3 threat)
        {
            CoverPoint best = null;
            float bestScore = float.MaxValue;

            foreach (CoverPoint point in CoverPoint.All)
            {
                if (point.Occupant != null && point.Occupant != this) continue;

                float toThreat = FlatDistance(point.transform.position, threat);
                if (toThreat < preferredMinDistance || toThreat > preferredMaxDistance) continue;
                if (!point.ProtectsFrom(threat)) continue;

                // Prefer close cover at a comfortable fighting distance
                float score = FlatDistance(transform.position, point.transform.position) + Mathf.Abs(toThreat - 10f) * 0.3f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = point;
                }
            }

            if (best != null)
            {
                cover = best;
                cover.Occupant = this;
            }
        }

        private void ReleaseCover()
        {
            if (cover != null && cover.Occupant == this) cover.Occupant = null;
            cover = null;
        }

        private void Shoot()
        {
            Vector3 origin = muzzle != null ? muzzle.position : EyePosition;
            Vector3 chest = PlayerChest;
            float distance = Vector3.Distance(origin, chest);

            // Harder to hit: far away, crouched or moving targets
            float chance = baseAccuracy * Mathf.Clamp01(1f - distance / 40f);
            if (playerController != null && playerController.IsCrouched) chance *= 0.7f;
            if (playerController != null && playerController.IsMoving) chance *= 0.75f;

            bool aimedHit = Random.value < chance;
            Vector3 aimPoint = aimedHit ? chest : chest + Random.insideUnitSphere * 1.2f;
            Vector3 direction = (aimPoint - origin).normalized;
            Vector3 end = origin + direction * 60f;

            // The bullet is a real ray: crates and walls stop it
            if (Physics.Raycast(origin, direction, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                Health target = hit.collider.GetComponentInParent<Health>();
                if (target == player) player.TakeDamage(damage, direction, "Zastrzelony");
                ShotEffects.Flash(hit.point - direction * 0.05f, new Color(1f, 0.5f, 0.3f), 1f, 1.2f);
            }

            ShotEffects.Tracer(origin, end, new Color(1f, 0.35f, 0.25f));
            ShotEffects.Flash(origin, new Color(1f, 0.5f, 0.3f), 3f, 4f);
        }

        // --- Movement ----------------------------------------------------------------

        private void WalkTowards(Vector3 target, float speed)
        {
            Vector3 flat = target - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) return;
            desiredVelocity = flat.normalized * speed;
            lookTarget = target;
        }

        private void RunTowards(Vector3 target)
        {
            WalkTowards(target, runSpeed);
            lookTarget = target;
        }

        private void Move()
        {
            if (!controller.enabled) return;

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            if (Time.time > pullUntil) pullVelocity = Vector3.MoveTowards(pullVelocity, Vector3.zero, 20f * Time.deltaTime);

            Vector3 before = transform.position;
            controller.Move((desiredVelocity + pullVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            // Stuck on a crate corner? Give up this cover and pick another
            float moved = FlatDistance(before, transform.position);
            float wanted = desiredVelocity.magnitude * Time.deltaTime;
            stuckTimer = wanted > 0.001f && moved < wanted * 0.2f ? stuckTimer + Time.deltaTime : 0f;
            if (stuckTimer > 1f)
            {
                ReleaseCover();
                nextCoverSearch = Time.time + 0.5f;
                stuckTimer = 0f;
            }

            // Turn towards the target smoothly
            Vector3 look = lookTarget - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 1f - Mathf.Exp(-8f * Time.deltaTime));
        }

        private void ApplyBody()
        {
            float height = crouched ? 1.1f : 1.8f;
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);

            if (visual != null)
            {
                float k = height / 1.8f;
                visual.localScale = Vector3.Lerp(visual.localScale, new Vector3(visualBaseScale.x, visualBaseScale.y * k, visualBaseScale.z), 0.3f);
                visual.localPosition = Vector3.Lerp(visual.localPosition, new Vector3(visualBasePosition.x, visualBasePosition.y * k, visualBasePosition.z), 0.3f);
            }
        }

        /// <summary>External velocity applied this frame (decompression), m/s.</summary>
        public void ApplyPull(Vector3 velocity)
        {
            pullVelocity = velocity;
            pullUntil = Time.time + 0.1f;
        }

        // --- Death ----------------------------------------------------------------------

        private void OnDied(Health self)
        {
            state = State.Dead;
            ReleaseCover();
            controller.enabled = false;
            StartCoroutine(FallOver());
        }

        private IEnumerator FallOver()
        {
            if (visual == null) yield break;

            // Placeholder "ragdoll": tip over backwards and lie on the floor
            Quaternion start = visual.localRotation;
            Quaternion end = Quaternion.Euler(-85f, 0f, 0f) * start;
            Vector3 startPos = visual.localPosition;
            Vector3 endPos = new Vector3(startPos.x, 0.35f, startPos.z - 0.6f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f)
            {
                float s = t * t;
                visual.localRotation = Quaternion.Slerp(start, end, s);
                visual.localPosition = Vector3.Lerp(startPos, endPos, s);
                yield return null;
            }
            visual.localRotation = end;
            visual.localPosition = endPos;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
