using System;
using System.Collections.Generic;
using Kotwica.Core;
using Kotwica.Gameplay;
using UnityEngine;

namespace Kotwica.Quests
{
    public enum QuestStatus { Aktywne, Ukonczone, Nieudane }

    /// <summary>Progress of one quest (saved as JSON).</summary>
    [Serializable]
    public class QuestState
    {
        public string questId;
        public QuestStatus status;
        public int step;
        public int progress;
        public int timesCompleted;

        public QuestDefinition Definition => QuestDefinition.Find(questId);

        public QuestObjective CurrentObjective
        {
            get
            {
                QuestDefinition def = Definition;
                return def != null && status == QuestStatus.Aktywne && step < def.objectives.Count ? def.objectives[step] : null;
            }
        }
    }

    /// <summary>
    /// The player's journal: starts quests, receives progress reports from the world
    /// ("player used object X", "enemy with tag Y died") and hands out rewards.
    /// Reward delivery is delegated (rewardHandler) so this class stays free of scene code.
    /// </summary>
    [Serializable]
    public class QuestLog
    {
        [SerializeField] private List<QuestState> quests = new List<QuestState>();
        [SerializeField] private string trackedQuestId;

        [NonSerialized] public Action<QuestReward> rewardHandler;
        [NonSerialized] public Func<int> playerLevel;

        public IReadOnlyList<QuestState> Quests => quests;

        /// <summary>Quest shown in the on-screen tracker (main quest by default).</summary>
        public QuestState Tracked
        {
            get
            {
                QuestState tracked = Get(trackedQuestId);
                if (tracked != null && tracked.status == QuestStatus.Aktywne) return tracked;
                QuestState main = quests.Find(q => q.status == QuestStatus.Aktywne && q.Definition != null && q.Definition.kind == QuestKind.Glowny);
                return main ?? quests.Find(q => q.status == QuestStatus.Aktywne);
            }
        }

        public void Track(string questId) => trackedQuestId = questId;

        public QuestState Get(string questId) => string.IsNullOrEmpty(questId) ? null : quests.Find(q => q.questId == questId);

        public bool IsCompleted(string questId)
        {
            QuestState state = Get(questId);
            return state != null && state.timesCompleted > 0;
        }

        public bool IsActive(string questId)
        {
            QuestState state = Get(questId);
            return state != null && state.status == QuestStatus.Aktywne;
        }

        public bool CanStart(string questId)
        {
            QuestDefinition def = QuestDefinition.Find(questId);
            if (def == null || IsActive(questId)) return false;
            if (IsCompleted(questId) && !def.repeatable) return false;
            if (playerLevel != null && playerLevel() < def.minLevel) return false;
            foreach (string required in def.requiredQuests)
                if (!IsCompleted(required)) return false;
            return true;
        }

        public bool Start(string questId)
        {
            if (!CanStart(questId)) return false;
            QuestState state = Get(questId);
            if (state == null)
            {
                state = new QuestState { questId = questId };
                quests.Add(state);
            }
            state.status = QuestStatus.Aktywne;
            state.step = 0;
            state.progress = 0;

            GameEvents.RaiseQuestStarted(questId);
            GameState.Notify($"Nowe zadanie: {state.Definition.title}");
            CheckInventoryObjective(state, null);
            return true;
        }

        /// <summary>Starts every auto-start quest that is not running yet (new game).</summary>
        public void StartAutoQuests()
        {
            foreach (QuestDefinition def in QuestDefinition.All)
                if (def.autoStart && Get(def.id) == null) Start(def.id);
        }

        /// <summary>The world reports something happened; matching current objectives advance.</summary>
        public void Report(ObjectiveType type, string targetId, int amount = 1)
        {
            foreach (QuestState state in quests.ToArray())
            {
                QuestObjective objective = state.CurrentObjective;
                if (objective == null || objective.type != type || objective.targetId != targetId) continue;
                Advance(state, amount);
            }
        }

        /// <summary>Re-checks "own N items" objectives after the inventory changed.</summary>
        public void CheckInventoryObjective(QuestState state, Func<string, int> itemCount)
        {
            if (itemCount == null) return;
            QuestObjective objective = state.CurrentObjective;
            if (objective == null || objective.type != ObjectiveType.Zdobadz) return;
            int owned = itemCount(objective.targetId);
            if (owned >= objective.count) Advance(state, objective.count - state.progress);
        }

        public void CheckAllInventoryObjectives(Func<string, int> itemCount)
        {
            foreach (QuestState state in quests.ToArray()) CheckInventoryObjective(state, itemCount);
        }

        public void Fail(string questId)
        {
            QuestState state = Get(questId);
            if (state == null || state.status != QuestStatus.Aktywne) return;
            state.status = QuestStatus.Nieudane;
            GameState.Notify($"Zadanie nieudane: {state.Definition?.title}");
            GameEvents.RaiseQuestUpdated(questId);
        }

        private void Advance(QuestState state, int amount)
        {
            QuestDefinition def = state.Definition;
            QuestObjective objective = state.CurrentObjective;
            if (def == null || objective == null) return;

            state.progress += Mathf.Max(1, amount);
            if (state.progress < objective.count)
            {
                GameEvents.RaiseQuestUpdated(state.questId);
                return;
            }

            state.step++;
            state.progress = 0;
            if (state.step >= def.objectives.Count)
            {
                Complete(state, def);
                return;
            }

            GameEvents.RaiseQuestUpdated(state.questId);
            GameState.Notify($"{def.title}: {def.objectives[state.step].description}");
        }

        private void Complete(QuestState state, QuestDefinition def)
        {
            state.status = QuestStatus.Ukonczone;
            state.timesCompleted++;
            rewardHandler?.Invoke(def.reward);
            GameEvents.RaiseQuestCompleted(state.questId);

            string xp = def.reward.xp > 0 ? $" +{def.reward.xp} XP" : "";
            string credits = def.reward.credits > 0 ? $" +{def.reward.credits} kr" : "";
            GameState.Notify($"Ukończono: {def.title}.{xp}{credits}");

            // Unlock follow-ups that auto start (next main chapter)
            StartAutoQuests();
        }
    }
}
