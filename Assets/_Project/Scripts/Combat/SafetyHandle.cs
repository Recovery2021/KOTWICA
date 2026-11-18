using Kotwica.Core;
using Kotwica.Player;
using UnityEngine;

namespace Kotwica.Combat
{
    /// <summary>
    /// Wall grab bar (F): while holding it the player cannot walk but is safe from decompression
    /// and can still aim and shoot. F again lets go.
    /// </summary>
    public class SafetyHandle : MonoBehaviour, IInteractable
    {
        [Tooltip("Where the player's feet go while holding the bar.")]
        [SerializeField] private Transform holdPoint;

        private PlayerController Player
        {
            get
            {
                Health h = Health.FindFirst(Team.Player);
                return h != null ? h.GetComponent<PlayerController>() : null;
            }
        }

        private Transform HoldPoint => holdPoint != null ? holdPoint : transform;

        public string Prompt
        {
            get
            {
                PlayerController p = Player;
                return p != null && p.IsHolding(HoldPoint) ? "Puść uchwyt" : "Złap uchwyt bezpieczeństwa";
            }
        }

        public bool CanInteract => Player != null;

        public void Interact()
        {
            PlayerController p = Player;
            if (p == null) return;
            if (p.IsHolding(HoldPoint)) p.Release();
            else p.Grab(HoldPoint);
        }
    }
}
