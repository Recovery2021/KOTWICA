using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Quests
{
    /// <summary>NPC or board that offers a quest (F).</summary>
    public class QuestGiver : MonoBehaviour, IInteractable
    {
        [SerializeField] private string questId;

        private QuestDefinition Definition => QuestDefinition.Find(questId);

        public string Prompt
        {
            get
            {
                QuestDefinition def = Definition;
                if (def == null) return "Brak zleceń";
                if (GameServices.Instance != null && GameServices.Instance.Quests.IsActive(questId)) return $"W toku: {def.title}";
                return $"Zlecenie: {def.title}";
            }
        }

        public bool CanInteract => GameServices.Instance != null && GameServices.Instance.Quests.CanStart(questId);

        public void Interact() => GameServices.Instance.Quests.Start(questId);
    }
}
