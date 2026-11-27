using Kotwica.Core;
using Kotwica.Dialogue;
using UnityEngine;

namespace Kotwica.Quests
{
    /// <summary>
    /// A resident you can talk to (F): offers a quest, and counts as the "talk to X" target
    /// of whatever quest currently needs them. Can hand over an item when talked to.
    /// Shows a floating name above the head. With a dialogue id set, F opens that conversation instead
    /// (the dialogue's choices then start quests, report objectives and hand over items).
    /// </summary>
    public class QuestNpc : MonoBehaviour, IInteractable
    {
        [SerializeField] private string npcName = "Mieszkaniec";
        [Tooltip("Quest this person offers (optional).")]
        [SerializeField] private string offersQuestId;
        [Tooltip("Target id for 'Porozmawiaj' objectives, e.g. kesh_brygadzista.")]
        [SerializeField] private string talkTargetId;
        [Tooltip("Item given when the talk objective is fulfilled (optional).")]
        [SerializeField] private string giveItemId;
        [TextArea, SerializeField] private string idleLine = "Nie mam teraz nic dla ciebie.";
        [Tooltip("Conversation in Resources/Dialogues (optional). Replaces the simple offer/talk behaviour.")]
        [SerializeField] private string dialogueId;

        private bool HasDialogue => !string.IsNullOrEmpty(dialogueId) && DialogueDefinition.Find(dialogueId) != null;

        private GameServices Services => GameServices.Instance;
        private bool CanOffer => Services != null && !string.IsNullOrEmpty(offersQuestId) && Services.Quests.CanStart(offersQuestId);
        private bool IsTalkTarget => Services != null && !string.IsNullOrEmpty(talkTargetId)
                                     && Services.IsObjectiveTarget(ObjectiveType.Porozmawiaj, talkTargetId);

        public string Prompt
        {
            get
            {
                if (IsTalkTarget) return $"Porozmawiaj: {npcName}";
                if (CanOffer) return $"{npcName} ma zlecenie: {QuestDefinition.Find(offersQuestId)?.title}";
                return HasDialogue ? $"Porozmawiaj: {npcName}" : $"{npcName}: {idleLine}";
            }
        }

        public bool CanInteract => HasDialogue ? !DialogueRunner.IsOpen : IsTalkTarget || CanOffer;

        public void Interact()
        {
            if (HasDialogue && DialogueRunner.Open(dialogueId, transform, DialogueObject.FindPlayer())) return;

            if (CanOffer && !IsTalkTarget)
            {
                Services.Quests.Start(offersQuestId);
                // Taking the job from this person already counts as "talking to them"
                if (!IsTalkTarget) return;
            }

            GameServices.Report(ObjectiveType.Porozmawiaj, talkTargetId);
            if (!string.IsNullOrEmpty(giveItemId)) Services.Inventory.Add(giveItemId);
        }

        private void Start()
        {
            // Floating name tag, readable from a few metres
            var tag = new GameObject("Imię");
            tag.transform.SetParent(transform, false);
            tag.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            var text = tag.AddComponent<TextMesh>();
            text.text = npcName;
            text.characterSize = 0.06f;
            text.fontSize = 40;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.85f, 0.55f);
            tag.AddComponent<FaceCamera>();
        }
    }

}
