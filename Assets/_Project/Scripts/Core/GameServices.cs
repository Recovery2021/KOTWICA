using Kotwica.Gameplay;
using Kotwica.Items;
using Kotwica.Orbits;
using Kotwica.Progression;
using Kotwica.Quests;
using Kotwica.SaveLoad;
using Kotwica.Security;
using Kotwica.World;
using UnityEngine;

namespace Kotwica.Core
{
    /// <summary>
    /// The one persistent object of the game (survives scene changes, created automatically):
    /// holds the player's RPG profile, reputation, inventory, quest log and alert levels,
    /// and offers short static helpers so gameplay code can report events with one line.
    /// </summary>
    public class GameServices : MonoBehaviour
    {
        public static GameServices Instance { get; private set; }

        public PlayerProfile Profile { get; private set; }
        public Reputation Reputation { get; private set; }
        public Inventory Inventory { get; private set; }
        public QuestLog Quests { get; private set; }
        public AlertSystem Alert { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        // Every scene gets the services without any setup
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("_GameServices");
            go.AddComponent<GameServices>();
            go.AddComponent<Kotwica.UI.RpgPanel>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            GameEvents.ItemAdded += (_, __) => Quests.CheckAllInventoryObjectives(Inventory.Count);
            GameEvents.QuestStarted += _ => Quests.CheckAllInventoryObjectives(Inventory.Count);
            NewGame();
        }

        public void NewGame()
        {
            Profile = new PlayerProfile();
            Reputation = new Reputation();
            Inventory = new Inventory();
            Quests = new QuestLog();
            Alert = new AlertSystem();
            Wire();
            Quests.StartAutoQuests();
        }

        /// <summary>Replaces the whole state with a loaded save.</summary>
        public void Apply(SaveData data)
        {
            Profile = data.profile ?? new PlayerProfile();
            Reputation = data.reputation ?? new Reputation();
            Inventory = data.inventory ?? new Inventory();
            Quests = data.quests ?? new QuestLog();
            Alert = data.alert ?? new AlertSystem();
            Wire();
        }

        private void Wire()
        {
            Profile.EnsureArrays();
            Quests.rewardHandler = GiveReward;
            Quests.playerLevel = () => Profile.Level;
        }

        private void GiveReward(QuestReward reward)
        {
            if (reward == null) return;
            if (reward.credits != 0) GameState.AddCredits(reward.credits);
            Profile.AddXp(reward.xp);
            foreach (ReputationReward rep in reward.reputation) Reputation.Change(rep.socialClass, rep.amount);
            foreach (ItemStack item in reward.items) Inventory.Add(item.itemId, item.count);
        }

        private void Update()
        {
            Alert.Tick(Time.deltaTime, playerSeenBySecurity: false);

            if (GameInput.QuickSavePressed()) SaveSystem.Save("szybki");
            if (GameInput.QuickLoadPressed()) SaveSystem.Load("szybki");
        }

        /// <summary>Ring the player is on now (anchor of the station frame), e.g. "kesh".</summary>
        public string CurrentRingId
        {
            get
            {
                OrbitalRing anchor = StationFrame.Instance != null ? StationFrame.Instance.Anchor : null;
                return anchor != null ? RingIdFor(anchor) : null;
            }
        }

        /// <summary>Matches a scene ring to its data by name ("Kesh · doki robotnicze" -> "kesh").</summary>
        public static string RingIdFor(OrbitalRing ring)
        {
            string name = ring.DisplayName.ToLowerInvariant();
            foreach (string id in new[] { "kesh", "orrin", "varo" })
                if (name.Contains(id)) return id;
            return null;
        }

        public bool IsObjectiveTarget(ObjectiveType type, string targetId)
        {
            foreach (QuestState state in Quests.Quests)
            {
                QuestObjective objective = state.CurrentObjective;
                if (objective != null && objective.type == type && objective.targetId == targetId) return true;
            }
            return false;
        }

        // --- One-line helpers for gameplay code (safe when services are missing) ---------

        public static void Report(ObjectiveType type, string targetId, int amount = 1)
        {
            if (Instance != null) Instance.Quests.Report(type, targetId, amount);
        }

        public static void AwardXp(int amount)
        {
            if (Instance != null) Instance.Profile.AddXp(amount);
        }

        public static void Practice(PlayerAttribute attribute, float amount)
        {
            if (Instance != null) Instance.Profile.Practice(attribute, amount);
        }

        /// <summary>Attribute multiplier for gameplay (1 when services are missing).</summary>
        public static float Bonus(PlayerAttribute attribute, float perPoint) =>
            Instance != null ? Instance.Profile.Multiplier(attribute, perPoint) : 1f;

        public static void ChangeReputation(SocialClass socialClass, int delta)
        {
            if (Instance != null) Instance.Reputation.Change(socialClass, delta);
        }

        public static void ReportCrime(string ringId, CrimeType crime, bool witnessed)
        {
            if (Instance != null) Instance.Alert.ReportCrime(ringId ?? Instance.CurrentRingId, crime, witnessed);
        }
    }
}
