using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Ship
{
    /// <summary>Lets the player get into the ship with F (only while it is docked and owned).</summary>
    [RequireComponent(typeof(ShipController))]
    public class ShipBoarding : MonoBehaviour, IInteractable
    {
        private ShipController ship;

        private void Awake() => ship = GetComponent<ShipController>();

        public string Prompt => !ship.Owned ? "Holownik Wróbel · na sprzedaż u handlarza Brodda" :
            ship.IsDocked ? "Wsiądź do statku" : "Statek nie jest zadokowany";
        public bool CanInteract => ship.Owned && ship.IsDocked && !ship.IsPiloted;
        public void Interact() => ship.Board();
    }
}
