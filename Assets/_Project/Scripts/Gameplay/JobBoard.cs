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

        [SerializeField] private DeliveryTerminal home;
        [SerializeField] private bool lift;
        [SerializeField] private bool missionBoard;

        public string Prompt
        {
            get
            {
                if (lift) return GameState.Mission == GameState.Stage.Repair ? "Pomóż dokerce naprawić podnośnik" : "Podnośnik działa";
                if (home != null && GameState.Mission == GameState.Stage.Repair) return "Najpierw napraw podnośnik obok tablicy";
                if (home != null && GameState.Mission == GameState.Stage.Ready) return "Kurs powrotny: przewieź moduł do Varo";
                if (destination == null) return "Tablica zleceń jest pusta";
                if (GameState.ActiveJob != null) return "Masz już aktywne zlecenie";
                return $"Weź zlecenie: dostawa do {destination.LocationName} ({reward + GameState.NextRewardAdjustment} kr)";
            }
        }

        public bool CanInteract => lift ? GameState.Mission == GameState.Stage.Repair :
            destination != null && GameState.ActiveJob == null &&
            (GameState.Mission == GameState.Stage.Complete || home != null && GameState.Mission == GameState.Stage.Ready || !missionBoard);

        public void Interact()
        {
            if (!CanInteract) return;
            if (lift) { GameState.RepairLift(); return; }
            if (home != null && GameState.Mission == GameState.Stage.Ready)
            {
                GameState.StartReturnCourse(new CourierJob { Cargo = "Uszkodzony moduł", Home = home, Destination = destination, Reward = reward });
                return;
            }
            var job = new CourierJob
            {
                Cargo = cargoNames.Length > 0 ? cargoNames[Random.Range(0, cargoNames.Length)] : "Paczka",
                Destination = destination,
                Reward = reward + GameState.NextRewardAdjustment
            };

            if (GameState.TryAcceptJob(job))
            {
                GameState.Notify($"Przyjęto: {job.Cargo} → {destination.LocationName}");
            }
        }
    }
}
