using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Gameplay
{
    public class Dockworker : MonoBehaviour, IInteractable
    {
        public bool CanInteract => GameState.Mission == GameState.Stage.Return &&
            (GameState.Cleared || GameState.Package == GameState.Medicine.Undeclared);
        public string Prompt
        {
            get
            {
                if (GameState.Mission == GameState.Stage.Complete)
                    return GameState.HasMedicine ? "Dziękuję. Córka dostała lek." : "Na tablicy Kesh czeka kolejny kurs. Przywieź proszę lek dla córki.";
                if (CanInteract && !GameState.Cleared) return "Przekaż lek przed odprawą (kontrola następnego kursu)";
                if (CanInteract) return GameState.HasMedicine ? "Przekaż dokerce lek dla córki" : "Powiedz dokerce, że wracasz bez leku";
                return GameState.Mission == GameState.Stage.Return ? "Najpierw odpraw moduł przy terminalu" : "Dokerka przy podnośniku";
            }
        }
        public void Interact() { if (CanInteract) GameState.FinishReturn(); }
    }
}
