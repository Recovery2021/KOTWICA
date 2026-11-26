using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Quests
{
    /// <summary>
    /// Object in the scene that quests can point at: press F to use it / talk to it,
    /// and the quest log hears "Interakcja/Porozmawiaj + targetId". Only usable while a quest needs it.
    /// </summary>
    public class QuestTarget : MonoBehaviour, IInteractable
    {
        [SerializeField] private string targetId = "cel";
        [SerializeField] private ObjectiveType type = ObjectiveType.Interakcja;
        [SerializeField] private string prompt = "Użyj";
        [Tooltip("Optional item handed to the player when used (e.g. a package).")]
        [SerializeField] private string giveItemId;

        public string Prompt => prompt;
        public bool CanInteract => GameServices.Instance != null && GameServices.Instance.IsObjectiveTarget(type, targetId);

        public void Interact()
        {
            if (!string.IsNullOrEmpty(giveItemId)) GameServices.Instance.Inventory.Add(giveItemId);
            GameServices.Report(type, targetId);
        }
    }
}
