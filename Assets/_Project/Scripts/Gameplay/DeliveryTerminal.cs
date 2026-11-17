using Kotwica.Core;
using Kotwica.Orbits;
using Kotwica.Ship;
using UnityEngine;

namespace Kotwica.Gameplay
{
    /// <summary>Drop-off point for courier jobs. Its port is used as the navigation target.</summary>
    public class DeliveryTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private OrbitalRing ring;
        [Tooltip("Docking port of this ring; the HUD points the pilot here.")]
        [SerializeField] private DockingPort port;

        public string LocationName => ring != null ? ring.DisplayName : name;
        public DockingPort Port => port;

        private bool HasDeliveryHere => GameState.ActiveJob != null && GameState.ActiveJob.Destination == this;

        public string Prompt => HasDeliveryHere
            ? $"Oddaj ładunek ({GameState.ActiveJob.Reward} kr)"
            : "Terminal dostaw: brak przesyłki dla tego pierścienia";

        public bool CanInteract => HasDeliveryHere;

        public void Interact()
        {
            int reward = GameState.ActiveJob.Reward;
            GameState.CompleteActiveJob();
            GameState.Notify($"Dostarczono. +{reward} kr");
        }
    }
}
