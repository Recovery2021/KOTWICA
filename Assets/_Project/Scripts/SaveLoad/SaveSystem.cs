using System;
using System.IO;
using Kotwica.Core;
using Kotwica.Gameplay;
using Kotwica.Items;
using Kotwica.Orbits;
using Kotwica.Progression;
using Kotwica.Quests;
using Kotwica.Security;
using Kotwica.World;
using UnityEngine;

namespace Kotwica.SaveLoad
{
    /// <summary>Everything that makes a playthrough unique, in one JSON file.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string savedAt;
        public string playerName;
        public int presetIndex;
        public int credits;
        public double orbitalTime;
        public string ringId;
        public PlayerProfile profile;
        public Reputation reputation;
        public Inventory inventory;
        public QuestLog quests;
        public AlertSystem alert;
        public StationClock clock;
    }

    /// <summary>
    /// Save / load to JSON files in the player's data folder (F5 quick save, F9 quick load).
    /// Positions in the world and the current scene come later with world streaming.
    /// </summary>
    public static class SaveSystem
    {
        public static string Folder => Path.Combine(Application.persistentDataPath, "saves");
        private static string PathFor(string slot) => Path.Combine(Folder, slot + ".json");

        public static bool Exists(string slot) => File.Exists(PathFor(slot));

        public static bool Save(string slot)
        {
            GameServices services = GameServices.Instance;
            if (services == null) return false;

            var data = new SaveData
            {
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                playerName = Kotwica.Characters.CharacterSelection.PlayerName,
                presetIndex = Kotwica.Characters.CharacterSelection.PresetIndex,
                credits = GameState.Credits,
                orbitalTime = OrbitalClock.Now,
                ringId = services.CurrentRingId,
                profile = services.Profile,
                reputation = services.Reputation,
                inventory = services.Inventory,
                quests = services.Quests,
                alert = services.Alert,
                clock = services.Clock
            };

            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(PathFor(slot), JsonUtility.ToJson(data, true));
                GameState.Notify("Zapisano grę.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Kotwica] Zapis nieudany: {e.Message}");
                GameState.Notify("Nie udało się zapisać gry.");
                return false;
            }
        }

        public static bool Load(string slot)
        {
            GameServices services = GameServices.Instance;
            if (services == null || !Exists(slot))
            {
                GameState.Notify("Brak zapisu do wczytania.");
                return false;
            }
            Kotwica.Vehicles.RingVehicle.ForceExit(); // a loaded game starts on foot

            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFor(slot)));
                services.Apply(data);
                GameState.SetCredits(data.credits);
                if (OrbitalClock.Instance != null) OrbitalClock.Instance.SetTime(data.orbitalTime);
                GameState.Notify($"Wczytano zapis z {data.savedAt}.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Kotwica] Wczytanie nieudane: {e.Message}");
                GameState.Notify("Zapis jest uszkodzony.");
                return false;
            }
        }
    }
}
