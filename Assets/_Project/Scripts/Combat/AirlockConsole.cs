using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Combat
{
    /// <summary>Wall console that triggers the airlock (F).</summary>
    public class AirlockConsole : MonoBehaviour, IInteractable
    {
        [SerializeField] private AirlockController airlock;

        public string Prompt => airlock == null ? "Konsola nie działa"
            : airlock.Used ? "Śluza awaryjna: już użyta"
            : "Awaryjne otwarcie zewnętrznej śluzy";

        public bool CanInteract => airlock != null && !airlock.Used;

        public void Interact() => airlock.Trigger();
    }
}
