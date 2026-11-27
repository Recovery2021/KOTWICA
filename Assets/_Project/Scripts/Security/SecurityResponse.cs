using System.Collections;
using System.Collections.Generic;
using Kotwica.Combat;
using Kotwica.Core;
using Kotwica.Gameplay;
using Kotwica.Items;
using Kotwica.Orbits;
using Kotwica.Player;
using Kotwica.Progression;
using Kotwica.World;
using UnityEngine;

namespace Kotwica.Security
{
    /// <summary>
    /// The station's answer to the alert level on the ring where the player is:
    /// more officers at higher levels (armed from the ring's threshold), alert does not cool while they see you,
    /// getting caught = fine (or a bribe on Kesh) and confiscation, getting killed = infirmary bill.
    /// Also turns gunfire in the districts into crimes.
    /// </summary>
    public class SecurityResponse : MonoBehaviour
    {
        public static SecurityResponse Instance { get; private set; }

        [SerializeField] private float spawnInterval = 1.5f;
        [Tooltip("Seconds at alert 0 before officers go back to their posts (despawn).")]
        [SerializeField] private float standDownDelay = 10f;
        [Tooltip("Where the player is brought after arrest or the infirmary, in ring coordinates (port deck).")]
        [SerializeField] private Vector3 releasePoint = new Vector3(-5f, 13.05f, -4f);

        private readonly List<SecurityGuard> guards = new List<SecurityGuard>();
        private string ringOfGuards;
        private float nextSpawn;
        private float calmSince = -1f;
        private float lastShotCrime = -10f;
        private Health playerHealth;
        private PlayerController player;
        private bool respawning;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable() => PlayerShooter.ShotFired += OnGunshot;
        private void OnDisable() => PlayerShooter.ShotFired -= OnGunshot;

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            playerHealth = player != null ? player.GetComponent<Health>() : null;
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
        }

        private void Update()
        {
            GameServices services = GameServices.Instance;
            if (services == null) return;

            guards.RemoveAll(g => g == null);
            string ring = services.CurrentRingId;

            // Flew to another ring: these officers stay behind
            if (ring != ringOfGuards) DespawnAll();
            ringOfGuards = ring;

            bool onFoot = player != null && player.gameObject.activeInHierarchy;
            int level = ring != null ? services.Alert.Level(ring) : 0;
            RingDefinition data = RingDefinition.Find(ring);
            int max = data != null ? data.maxGuards : 3;
            int armedFrom = data != null ? data.armedFromLevel : 3;

            bool seen = false;
            foreach (SecurityGuard guard in guards)
            {
                guard.Armed = level >= armedFrom;
                seen |= guard.SeesPlayer;
            }
            services.PlayerSeenBySecurity = seen;

            // Officers come only while the player walks on this ring
            int wanted = onFoot && level > 0 ? Mathf.CeilToInt(level / (float)AlertSystem.MaxLevel * max) : 0;
            if (CountAlive() < wanted && Time.time >= nextSpawn)
            {
                nextSpawn = Time.time + spawnInterval;
                Spawn(ring, level >= armedFrom);
            }

            // Stand down after a quiet while
            if (level == 0 && !seen)
            {
                if (calmSince < 0f) calmSince = Time.time;
                if (Time.time - calmSince > standDownDelay) DespawnAll();
            }
            else calmSince = -1f;
        }

        private int CountAlive()
        {
            int n = 0;
            foreach (SecurityGuard g in guards)
                if (!g.GetComponent<Health>().IsDead) n++;
            return n;
        }

        // --- Spawning --------------------------------------------------------------------------------------

        private void Spawn(string ringId, bool armed)
        {
            Vector3 position = PickSpawnPoint(ringId);
            var go = new GameObject("Ochrona");
            go.transform.position = position;

            var controller = go.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.35f;

            var health = go.AddComponent<Health>();
            health.Configure(Team.Security, 100f);

            // Placeholder look: dark uniform, blue light bar on the shoulders
            var visual = new GameObject("Wygląd").transform;
            visual.SetParent(go.transform, false);
            visual.localPosition = new Vector3(0f, 0.9f, 0f);
            Part(PrimitiveType.Capsule, visual, Vector3.zero, new Vector3(0.7f, 0.9f, 0.7f), new Color(0.08f, 0.1f, 0.16f), Color.black);
            Part(PrimitiveType.Cube, visual, new Vector3(0f, 0.55f, 0f), new Vector3(0.75f, 0.06f, 0.2f), Color.black, new Color(0.2f, 0.5f, 1f) * 3f);

            var guard = go.AddComponent<SecurityGuard>();
            guard.SetVisual(visual);
            Vector3? crimeScene = player != null ? player.transform.position : (Vector3?)null;
            guard.Init(ringId, armed, crimeScene);
            guards.Add(guard);

            if (guards.Count == 1) GameState.Notify(armed ? "Ochrona otworzy ogień!" : "Ochrona cię szuka.");
        }

        private Vector3 PickSpawnPoint(string ringId)
        {
            // Prefer a post the player is not standing next to
            var options = new List<Vector3>();
            foreach (SecurityPost post in SecurityPost.All)
                if (post.RingId == ringId) options.Add(post.transform.position);

            OrbitalRing anchor = StationFrame.Instance != null ? StationFrame.Instance.Anchor : null;
            if (options.Count == 0 && anchor != null)
            {
                options.Add(anchor.transform.TransformPoint(new Vector3(-14f, 13.1f, -14f)));
                options.Add(anchor.transform.TransformPoint(new Vector3(14f, 13.1f, -14f)));
            }
            if (options.Count == 0) return player != null ? player.transform.position + Vector3.forward * 20f : Vector3.zero;

            Vector3 best = options[0];
            float bestScore = float.MinValue;
            foreach (Vector3 option in options)
            {
                float d = player != null ? Vector3.Distance(option, player.transform.position) : 0f;
                float score = d > 12f ? 100f - d : d; // not on top of the player, not across the station either
                score += Random.value * 5f;
                if (score > bestScore) { bestScore = score; best = option; }
            }
            return best;
        }

        private static void Part(PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color, Color emission)
        {
            var part = GameObject.CreatePrimitive(type);
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<Renderer>();
            renderer.material.color = color;
            if (emission.maxColorComponent > 0f)
            {
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", emission);
            }
        }

        private void DespawnAll()
        {
            foreach (SecurityGuard guard in guards)
                if (guard != null) Destroy(guard.gameObject);
            guards.Clear();
            if (GameServices.Instance != null) GameServices.Instance.PlayerSeenBySecurity = false;
        }

        // --- Crimes from gunfire ---------------------------------------------------------------------------

        private void OnGunshot(Vector3 position)
        {
            GameServices services = GameServices.Instance;
            if (services == null || services.CurrentRingId == null) return;

            ResidentAI.Panic(position, 30f);
            if (Time.time - lastShotCrime < 3f) return; // one crime per burst, not per bullet
            lastShotCrime = Time.time;
            GameServices.ReportCrime(null, CrimeType.Strzelanina, Witnesses.AnyoneSees(position, 30f));
        }

        // --- Consequences --------------------------------------------------------------------------------

        /// <summary>A guard caught the player: fine (or bribe), contraband confiscated, alert cleared.</summary>
        public void Arrest(SecurityGuard by)
        {
            Kotwica.Vehicles.RingVehicle.ForceExit(); // pulled out of the vehicle first
            GameServices services = GameServices.Instance;
            string ring = by.RingId;
            int level = Mathf.Max(1, services.Alert.Level(ring));
            RingDefinition data = RingDefinition.Find(ring);
            float law = data != null ? data.lawMultiplier : 1f;
            int fine = Mathf.RoundToInt((data != null ? data.finePerLevel : 40) * level * law);

            bool bribe = data != null && data.bribesAccepted && services.Profile.Get(PlayerAttribute.Perswazja) >= 3;
            string message;
            if (bribe)
            {
                fine /= 2;
                services.Profile.Practice(PlayerAttribute.Perswazja, 30f);
                message = $"Dogadałeś się z ochroną. Łapówka: {fine} kr.";
            }
            else
            {
                int confiscated = Confiscate(services.Inventory);
                message = $"Zatrzymany. Mandat: {fine} kr" + (confiscated > 0 ? $", skonfiskowano kontrabandę ({confiscated})." : ".");
            }

            GameState.AddCredits(-Mathf.Min(fine, Mathf.Max(0, GameState.Credits)));
            services.Alert.Clear(ring);
            DespawnAll();
            ReleaseToPort();
            GameState.Notify(message);
        }

        private void OnPlayerDied(Health self)
        {
            Kotwica.Vehicles.RingVehicle.ForceExit();
            if (!respawning) StartCoroutine(Infirmary());
        }

        private IEnumerator Infirmary()
        {
            respawning = true;
            yield return new WaitForSeconds(3f);

            GameServices services = GameServices.Instance;
            string ring = services != null ? services.CurrentRingId : null;
            int bill = Mathf.RoundToInt(Mathf.Max(0, GameState.Credits) * 0.25f);
            GameState.AddCredits(-bill);
            if (services != null && ring != null)
            {
                Confiscate(services.Inventory);
                services.Alert.Clear(ring);
            }

            DespawnAll();
            playerHealth.Revive();
            ReleaseToPort();
            GameState.Notify($"Obudziłeś się w ambulatorium portu. Rachunek: {bill} kr.");
            respawning = false;
        }

        private void ReleaseToPort()
        {
            OrbitalRing anchor = StationFrame.Instance != null ? StationFrame.Instance.Anchor : null;
            if (player == null || anchor == null || !player.gameObject.activeInHierarchy) return;
            player.Release();
            player.TeleportTo(anchor.transform.TransformPoint(releasePoint), anchor.transform.rotation);
        }

        private static int Confiscate(Inventory inventory)
        {
            int taken = 0;
            foreach (ItemStack stack in new List<ItemStack>(inventory.Stacks))
            {
                ItemDefinition item = ItemDefinition.Find(stack.itemId);
                if (item == null || item.legal) continue;
                taken += stack.count;
                inventory.Remove(stack.itemId, stack.count);
            }
            return taken;
        }
    }
}
