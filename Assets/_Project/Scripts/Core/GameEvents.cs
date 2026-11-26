using System;
using UnityEngine;

namespace Kotwica.Core
{
    /// <summary>
    /// Global game events, so systems talk without knowing each other
    /// (HUD listens to level-ups, quests listen to kills, audio later listens to alarms...).
    /// </summary>
    public static class GameEvents
    {
        public static event Action<int> XpGained;            // amount
        public static event Action<int> LevelUp;             // new level
        public static event Action<string, int> AttributeRaised;  // attribute name, new value
        public static event Action<string> QuestStarted;     // quest id
        public static event Action<string> QuestUpdated;
        public static event Action<string> QuestCompleted;
        public static event Action<string, int> ReputationChanged; // class name, new value
        public static event Action<string, int> AlertChanged;      // ring id, new level 0..5
        public static event Action<string, int> ItemAdded;         // item id, amount

        public static void RaiseXpGained(int amount) => XpGained?.Invoke(amount);
        public static void RaiseLevelUp(int level) => LevelUp?.Invoke(level);
        public static void RaiseAttributeRaised(string attribute, int value) => AttributeRaised?.Invoke(attribute, value);
        public static void RaiseQuestStarted(string id) => QuestStarted?.Invoke(id);
        public static void RaiseQuestUpdated(string id) => QuestUpdated?.Invoke(id);
        public static void RaiseQuestCompleted(string id) => QuestCompleted?.Invoke(id);
        public static void RaiseReputationChanged(string socialClass, int value) => ReputationChanged?.Invoke(socialClass, value);
        public static void RaiseAlertChanged(string ringId, int level) => AlertChanged?.Invoke(ringId, level);
        public static void RaiseItemAdded(string itemId, int amount) => ItemAdded?.Invoke(itemId, amount);

        // Listeners from a previous play session must not survive (domain reload may be off)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            XpGained = null; LevelUp = null; AttributeRaised = null;
            QuestStarted = null; QuestUpdated = null; QuestCompleted = null;
            ReputationChanged = null; AlertChanged = null; ItemAdded = null;
        }
    }
}
