using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Gameplay
{
    /// <summary>Notice board that hands out courier jobs to another ring.</summary>
    public class JobBoard : MonoBehaviour, IInteractable
    {
        [SerializeField] private DeliveryTerminal destination;
        [SerializeField] private int reward = 120;
        [SerializeField] private string[] cargoNames =
        {
            "Zaplombowana skrzynka z lekami",
            "Filtry powietrza",
            "Pakiet danych frakcji",
            "Części do pomp chłodzenia"
        };

        public string Prompt
        {
            get
            {
                if (destination == null) return "Tablica zleceń jest pusta";
                if (GameState.ActiveJob != null) return "Masz już aktywne zlecenie";
                return $"Weź zlecenie: dostawa do {destination.LocationName} ({reward} kr)";
            }
        }

        public bool CanInteract => destination != null && GameState.ActiveJob == null;

        public void Interact()
        {
            var job = new CourierJob
            {
                Cargo = cargoNames.Length > 0 ? cargoNames[Random.Range(0, cargoNames.Length)] : "Paczka",
                Destination = destination,
                Reward = reward
            };

            if (GameState.TryAcceptJob(job))
            {
                GameState.Notify($"Przyjęto: {job.Cargo} → {destination.LocationName}");
            }
        }
    }
}
