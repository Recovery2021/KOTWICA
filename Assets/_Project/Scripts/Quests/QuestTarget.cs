using Kotwica.Core;
using Kotwica.Security;
using UnityEngine;

namespace Kotwica.Quests
{
    /// <summary>
    /// Object in the scene that quests can point at (F): reports "type + targetId" to the quest log,
    /// can hand over or take items, can be a one-off pickup and can count as a crime.
    /// Only usable while an active quest needs it.
    /// </summary>
    public class QuestTarget : MonoBehaviour, IInteractable
    {
        [SerializeField] private string targetId = "cel";
        [SerializeField] private ObjectiveType type = ObjectiveType.Interakcja;
        [SerializeField] private string prompt = "Użyj";

        [Header("Items")]
        [Tooltip("Item handed to the player when used (e.g. a package, a permit).")]
        [SerializeField] private string giveItemId;
        [Tooltip("Item taken from the player (delivery). Use is blocked without it.")]
        [SerializeField] private string takeItemId;
        [SerializeField] private int takeCount = 1;
        [Tooltip("Pickup that disappears after use.")]
        [SerializeField] private bool singleUse;

        [Header("Crime")]
        [SerializeField] private bool isCrime;
        [SerializeField] private CrimeType crime = CrimeType.Kradziez;
        [SerializeField] private bool witnessed;

        public string Prompt
        {
            get
            {
                if (!string.IsNullOrEmpty(takeItemId) && GameServices.Instance != null && !GameServices.Instance.Inventory.Has(takeItemId, takeCount))
                    return $"{prompt} (brakuje przedmiotu)";
                return prompt;
            }
        }

        public bool CanInteract
        {
            get
            {
                GameServices services = GameServices.Instance;
                if (services == null) return false;
                // "Get N items" objectives are fed by the inventory, so pickups only need the quest to want the item
                bool wanted = type == ObjectiveType.Zdobadz
                    ? services.IsObjectiveTarget(ObjectiveType.Zdobadz, giveItemId)
                    : services.IsObjectiveTarget(type, targetId);
                return wanted && (string.IsNullOrEmpty(takeItemId) || services.Inventory.Has(takeItemId, takeCount));
            }
        }

        public void Interact()
        {
            GameServices services = GameServices.Instance;
            if (!string.IsNullOrEmpty(takeItemId)) services.Inventory.Remove(takeItemId, takeCount);

            // Report first, then give: a following "get the item" step sees the new item at once
            if (type != ObjectiveType.Zdobadz) GameServices.Report(type, targetId);
            if (!string.IsNullOrEmpty(giveItemId)) services.Inventory.Add(giveItemId);

            if (isCrime) GameServices.ReportCrime(null, crime, witnessed);
            if (singleUse) gameObject.SetActive(false);
        }
    }
}
