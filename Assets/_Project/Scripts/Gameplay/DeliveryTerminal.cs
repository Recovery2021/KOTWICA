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

        public string Prompt => HasDeliveryHere && GameState.AtClinic ? "Zrezygnuj z leku i wróć na Kesh" :
            HasDeliveryHere ? $"Oddaj ładunek ({GameState.ActiveJob.Reward} kr)" :
            "Terminal dostaw: brak przesyłki dla tego pierścienia";

        public bool CanInteract => HasDeliveryHere;

        public void Interact()
        {
            if (!CanInteract) return;
            switch (GameState.Mission)
            {
                case GameState.Stage.Outbound: GameState.DeliverModule(); return;
                case GameState.Stage.Decide:
                case GameState.Stage.Waiting: GameState.ReturnWithoutMedicine(); return;
                case GameState.Stage.Return: GameState.FinishReturn(); return;
            }
            int reward = GameState.ActiveJob.Reward;
            GameState.CompleteActiveJob();
            GameState.Notify($"Dostarczono. +{reward} kr");
        }
    }
}
