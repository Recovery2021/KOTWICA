namespace Kotwica.Core
{
    /// <summary>
    /// What the animation system needs to know about a character's stance,
    /// implemented by the player controller and by enemy AI.
    /// </summary>
    public interface ICharacterPose
    {
        bool IsCrouched { get; }
        bool IsAiming { get; }
    }
}
