using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Gameplay
{
    public class ClinicContact : MonoBehaviour, IInteractable
    {
        public enum Role { Intercom, Medic }
        public Role role;
        private bool Waiting => GameState.Mission == GameState.Stage.Waiting;
        public bool CanInteract => GameState.Mission == GameState.Stage.Decide ||
            (role == Role.Medic && Waiting && GameState.ApprovalRemaining <= 0);

        public string Prompt
        {
            get
            {
                if (!GameState.AtClinic)
                    return role == Role.Intercom ? "Bramka kliniki · dostęp dla personelu" : "Medyczka kliniki Varo";
                if (role == Role.Intercom)
                    return Waiting ? "Medyczka niesie zatwierdzony lek do bramki" :
                        "Wstęp tylko do magazynu. Poproś medyczkę przez interkom o dostawę (25 s)";
                if (Waiting) return GameState.ApprovalRemaining > 0 ? "Medyczka przygotowuje wydanie" : "Odbierz lek i zmianę deklaracji";
                return "Medyczka: mogę dać ci lek, ale nie masz zgody na przewóz. Zabierz lek";
            }
        }

        public void Interact()
        {
            if (!CanInteract) return;
            if (Waiting) GameState.BeginReturn();
            else GameState.ChooseMedicine(role == Role.Intercom ? GameState.Medicine.Approved : GameState.Medicine.Undeclared);
        }
    }
}
