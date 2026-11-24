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

        private void Start()
        {
            if (missionBoard && home != null && destination != null)
            {
                VaroClinic.Install(destination.Port.Ring.transform);
                var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                npc.name = "Dokerka";
                npc.transform.SetParent(home.Port.Ring.transform, false);
                npc.transform.localPosition = new Vector3(-5, 14, -12);
                npc.AddComponent<Dockworker>();
                Label(npc.transform, "DOKERKA", 1.6f);
                Label(home.transform, "ODPRAWA", 1.5f);
            }
        }

        private static void Label(Transform parent, string text, float height)
        {
            var sign = new GameObject(text);
            sign.transform.SetParent(parent, false);
            sign.transform.localPosition = Vector3.up * height;
            sign.transform.localRotation = Quaternion.Euler(0,180,0);
            var label = sign.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.characterSize = 0.06f;
            label.fontSize = 48;
        }

        public string Prompt
        {
            get
            {
                if (lift) return GameState.Mission == GameState.Stage.Repair ? "Pomóż dokerce naprawić podnośnik" : "Podnośnik działa";
                if (home != null && GameState.Mission == GameState.Stage.Repair) return "Najpierw napraw podnośnik obok tablicy";
                if (home != null && GameState.Mission == GameState.Stage.Ready) return "Kurs powrotny: przewieź moduł do Varo";
                if (home != null && GameState.NeedsMedicineRun && GameState.ActiveJob == null) return "Kolejny kurs po lek: zawieź moduł serwisowy na Varo";
                if (destination == null) return "Tablica zleceń jest pusta";
                if (GameState.ActiveJob != null) return "Masz już aktywne zlecenie";
                return (GameState.InspectionDue ? "Licencja: kontrola przed startem. " : "") + $"Weź zlecenie: dostawa do {destination.LocationName} ({reward + GameState.NextRewardAdjustment} kr)";
            }
        }

        public bool CanInteract => lift ? GameState.Mission == GameState.Stage.Repair :
            destination != null && GameState.ActiveJob == null &&
            (GameState.Mission == GameState.Stage.Complete || home != null && GameState.Mission == GameState.Stage.Ready || !missionBoard);

        public void Interact()
        {
            if (!CanInteract) return;
            if (lift) { GameState.RepairLift(); return; }
            if (home != null && (GameState.Mission == GameState.Stage.Ready || GameState.NeedsMedicineRun))
            {
                GameState.StartReturnCourse(new CourierJob { Cargo = GameState.NeedsMedicineRun ? "Moduł serwisowy" : "Uszkodzony moduł", Home = home, Destination = destination, Reward = reward + GameState.NextRewardAdjustment });
                return;
            }
            var job = new CourierJob
            {
                Cargo = cargoNames.Length > 0 ? cargoNames[Random.Range(0, cargoNames.Length)] : "Paczka",
                Home = home,
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
