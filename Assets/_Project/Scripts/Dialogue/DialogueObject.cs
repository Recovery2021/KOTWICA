using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Dialogue
{
    /// <summary>
    /// A terminal, door panel or any object that "talks" through a dialogue asset (F):
    /// the dialogue's entries decide what it shows for the current quest state.
    /// </summary>
    public class DialogueObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private string dialogueId;
        [SerializeField] private string prompt = "Użyj terminalu";

        public string Prompt => prompt;
        public bool CanInteract => !DialogueRunner.IsOpen && DialogueDefinition.Find(dialogueId) != null;

        public void Interact()
        {
            Transform player = FindPlayer();
            DialogueRunner.Open(dialogueId, transform, player);
        }

        /// <summary>The active player character (the one using the object).</summary>
        public static Transform FindPlayer()
        {
            var interactor = FindFirstObjectByType<Kotwica.Player.PlayerInteractor>();
            return interactor != null ? interactor.transform : null;
        }
    }
}
