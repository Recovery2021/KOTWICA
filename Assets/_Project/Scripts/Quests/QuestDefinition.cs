using System;
using System.Collections.Generic;
using Kotwica.Items;
using Kotwica.World;
using UnityEngine;

namespace Kotwica.Quests
{
    public enum QuestKind { Glowny, Poboczny, Aktywnosc }

    /// <summary>What the player has to do in one quest step.</summary>
    public enum ObjectiveType
    {
        Idz,          // reach a place (QuestArea)
        Interakcja,   // use an object (QuestTarget)
        Porozmawiaj,  // talk to someone
        Dostarcz,     // hand over cargo at a place
        Zdobadz,      // own N items (checked against the inventory)
        Wyeliminuj,   // neutralise N enemies with a tag
        Przetrwaj,    // survive / wait N seconds
        Lec           // dock at a port
    }

    [Serializable]
    public class QuestObjective
    {
        [Tooltip("Text shown in the journal and the on-screen tracker.")]
        public string description = "Cel";
        public ObjectiveType type = ObjectiveType.Interakcja;
        [Tooltip("Id reported by scene objects (QuestTarget/QuestArea) or an item id for Zdobadź.")]
        public string targetId;
        [Min(1)] public int count = 1;
    }

    [Serializable]
    public class ReputationReward
    {
        public SocialClass socialClass;
        public int amount;
    }

    [Serializable]
    public class QuestReward
    {
        public int credits;
        public int xp = 50;
        public List<ReputationReward> reputation = new List<ReputationReward>();
        public List<ItemStack> items = new List<ItemStack>();
    }

    /// <summary>
    /// A quest as data: objectives in order, requirements and reward. Normally read from the editable
    /// JSON files in StreamingAssets/Kotwica (see ContentLibrary); assets in Resources/Quests are a fallback.
    /// </summary>
    [CreateAssetMenu(menuName = "Kotwica/Zadanie", fileName = "Quest_")]
    public class QuestDefinition : ScriptableObject
    {
        public string id = "quest";
        public string title = "Nowe zadanie";
        [TextArea(3, 8)] public string description;
        public QuestKind kind = QuestKind.Poboczny;
        [Tooltip("Ring where the quest is given (for the journal and map later).")]
        public string ringId;

        [Header("Requirements")]
        public int minLevel = 1;
        public List<string> requiredQuests = new List<string>();
        [Tooltip("Start automatically when a new game begins (first main chapter).")]
        public bool autoStart;
        [Tooltip("Activities can be taken again after completion.")]
        public bool repeatable;

        public List<QuestObjective> objectives = new List<QuestObjective>();
        public QuestReward reward = new QuestReward();

        private static Dictionary<string, QuestDefinition> cache;

        public static QuestDefinition Find(string questId)
        {
            LoadAll();
            return questId != null && cache.TryGetValue(questId, out QuestDefinition found) ? found : null;
        }

        public static IEnumerable<QuestDefinition> All
        {
            get
            {
                LoadAll();
                return cache.Values;
            }
        }

        private static void LoadAll()
        {
            if (cache != null) return;
            cache = new Dictionary<string, QuestDefinition>();
            // Editable JSON files (StreamingAssets/Kotwica) win; the old assets are only a fallback
            var fromFiles = Kotwica.Content.ContentLibrary.Quests;
            if (fromFiles.Count > 0)
                foreach (QuestDefinition quest in fromFiles) cache[quest.id] = quest;
            else
                foreach (QuestDefinition quest in Resources.LoadAll<QuestDefinition>("Quests")) cache[quest.id] = quest;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => cache = null;
    }
}
