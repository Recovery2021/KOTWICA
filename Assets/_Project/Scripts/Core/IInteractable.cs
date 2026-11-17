namespace Kotwica.Core
{
    /// <summary>
    /// Anything the player can use with the interact key (F):
    /// job boards, terminals, ship hatch, later doors, NPCs, loot.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Text shown on the HUD when the player stands nearby.</summary>
        string Prompt { get; }

        /// <summary>False = prompt is shown greyed out, F does nothing.</summary>
        bool CanInteract { get; }

        void Interact();
    }
}
