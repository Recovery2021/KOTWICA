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

        public string Prompt => HasDeliveryHere && GameState.Mission == GameState.Stage.Decide ? "Lek dla córki: F wróć bez leku · 2 złóż wniosek · 3 zabierz bez zgody" :
            HasDeliveryHere && GameState.Mission == GameState.Stage.Waiting ? $"Odbierz lek ze zgodą ({GameState.ApprovalRemaining:0} s)" :
            HasDeliveryHere
            ? $"Oddaj ładunek ({GameState.ActiveJob.Reward} kr)"
            : "Terminal dostaw: brak przesyłki dla tego pierścienia";

        public bool CanInteract => HasDeliveryHere && (GameState.Mission != GameState.Stage.Waiting || GameState.ApprovalRemaining <= 0);

        public void ChooseMedicine(int option)
        {
            if (!HasDeliveryHere || GameState.Mission != GameState.Stage.Decide) return;
            GameState.ChooseMedicine(option == 2 ? GameState.Medicine.Approved : GameState.Medicine.Undeclared);
        }

        public void Interact()
        {
            if (!CanInteract) return;
            switch (GameState.Mission)
            {
                case GameState.Stage.Outbound: GameState.DeliverModule(); return;
                case GameState.Stage.Decide: GameState.ChooseMedicine(GameState.Medicine.None); return;
                case GameState.Stage.Waiting: GameState.BeginReturn(); return;
                case GameState.Stage.Return: GameState.FinishReturn(); return;
            }
            int reward = GameState.ActiveJob.Reward;
            GameState.CompleteActiveJob();
            GameState.Notify($"Dostarczono. +{reward} kr");
        }
    }
}
