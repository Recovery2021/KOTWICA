using System;
using Kotwica.Characters;
using Kotwica.Core;
using Kotwica.Player;
using UnityEngine;

namespace Kotwica.Combat
{
    /// <summary>
    /// Player's rifle: hitscan from the screen centre, magazine + reload, spread (smaller when aiming,
    /// larger when moving), recoil, and a "noise" event that alerts nearby enemies.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerShooter : MonoBehaviour
    {
        /// <summary>Raised on every shot with the shooter's position (enemies listen = hearing).</summary>
        public static event Action<Vector3> ShotFired;

        [SerializeField] private CameraRig cameraRig;
        [Tooltip("Where tracers start (gun barrel).")]
        [SerializeField] private Transform muzzle;

        [Header("Weapon")]
        [SerializeField] private float damage = 34f;
        [SerializeField] private float fireInterval = 0.22f;
        [SerializeField] private int magazineSize = 12;
        [SerializeField] private int reserveAmmo = 60;
        [SerializeField] private float reloadTime = 1.8f;
        [SerializeField] private float range = 80f;

        [Header("Accuracy (degrees)")]
        [SerializeField] private float hipSpread = 2.5f;
        [SerializeField] private float aimSpread = 0.4f;
        [SerializeField] private float movingSpreadBonus = 1.5f;
        [SerializeField] private float recoil = 1.2f;

        private PlayerController controller;
        private Health health;
        private Camera viewCamera;
        private float nextFireTime;
        private float reloadEndTime;
        private readonly RaycastHit[] hits = new RaycastHit[16];

        public int Ammo { get; private set; }
        public int Reserve { get; private set; }
        public bool IsReloading { get; private set; }
        public float LastHitTime { get; private set; } = -100f;
        public bool LastHitKilled { get; private set; }

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<Health>();
            Ammo = magazineSize;
            Reserve = reserveAmmo;
        }

        private void Start()
        {
            if (cameraRig == null && Camera.main != null) cameraRig = Camera.main.GetComponent<CameraRig>();
            viewCamera = cameraRig != null ? cameraRig.GetComponent<Camera>() : Camera.main;

            // Real model present: tracers start at its rifle
            var avatar = GetComponent<CharacterAvatar>();
            if (avatar != null && avatar.WeaponMuzzle != null) muzzle = avatar.WeaponMuzzle;
        }

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                SetAiming(false);
                return;
            }

            SetAiming(GameInput.AimHeld());

            if (IsReloading && Time.time >= reloadEndTime) FinishReload();
            if (!IsReloading && GameInput.ReloadPressed() && Ammo < magazineSize && Reserve > 0) StartReload();

            if (!IsReloading && GameInput.FireHeld() && Time.time >= nextFireTime)
            {
                if (Ammo > 0) Fire();
                else if (Reserve > 0) StartReload();
            }
        }

        private void SetAiming(bool aiming)
        {
            controller.IsAiming = aiming;
            if (cameraRig != null) cameraRig.SetAiming(aiming);
        }

        private void Fire()
        {
            nextFireTime = Time.time + fireInterval;
            Ammo--;

            // Spread: random cone around the screen centre
            float spread = controller.IsAiming ? aimSpread : hipSpread;
            if (controller.IsMoving) spread += movingSpreadBonus;
            Transform view = viewCamera != null ? viewCamera.transform : transform;
            Vector3 direction = view.rotation
                * Quaternion.Euler(UnityEngine.Random.Range(-spread, spread), UnityEngine.Random.Range(-spread, spread), 0f)
                * Vector3.forward;

            Vector3 end = view.position + direction * range;
            if (TryHit(new Ray(view.position, direction), out RaycastHit hit))
            {
                end = hit.point;
                Health target = hit.collider.GetComponentInParent<Health>();
                if (target != null && target != health && !target.IsDead)
                {
                    target.TakeDamage(damage, direction);
                    LastHitTime = Time.time;
                    LastHitKilled = target.IsDead;
                }
                ShotEffects.Flash(hit.point - direction * 0.05f, new Color(1f, 0.7f, 0.4f), 1.5f, 1.5f);
            }

            Vector3 barrel = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.35f + transform.right * 0.3f;
            ShotEffects.Tracer(barrel, end, new Color(1f, 0.8f, 0.5f));
            ShotEffects.Flash(barrel, new Color(1f, 0.75f, 0.45f), 4f, 5f);

            if (cameraRig != null) cameraRig.AddRecoil(recoil);
            ShotFired?.Invoke(transform.position);
        }

        /// <summary>Nearest hit that is not the player's own body or a trigger.</summary>
        private bool TryHit(Ray ray, out RaycastHit best)
        {
            best = default;
            int count = Physics.RaycastNonAlloc(ray, hits, range, ~0, QueryTriggerInteraction.Ignore);
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform)) continue;
                if (hits[i].distance < bestDistance)
                {
                    bestDistance = hits[i].distance;
                    best = hits[i];
                }
            }
            return bestDistance < float.MaxValue;
        }

        private void StartReload()
        {
            IsReloading = true;
            reloadEndTime = Time.time + reloadTime;
        }

        private void FinishReload()
        {
            IsReloading = false;
            int needed = magazineSize - Ammo;
            int taken = Mathf.Min(needed, Reserve);
            Ammo += taken;
            Reserve -= taken;
        }

        /// <summary>Called when picking up ammo (later).</summary>
        public void AddReserve(int amount) => Reserve += Mathf.Max(0, amount);
    }
}
