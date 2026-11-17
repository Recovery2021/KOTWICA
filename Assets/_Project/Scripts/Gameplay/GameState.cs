using UnityEngine;

namespace Kotwica.Gameplay
{
    /// <summary>A single courier contract: what to carry, where, and for how much.</summary>
    public class CourierJob
    {
        public string Cargo;
        public DeliveryTerminal Destination;
        public int Reward;
    }

    /// <summary>
    /// Prototype-level game state: money, the active job, the last on-screen message.
    /// Later replaced by a proper save system and quest log.
    /// </summary>
    public static class GameState
    {
        public static int Credits { get; private set; }
        public static CourierJob ActiveJob { get; private set; }
        public static string LastMessage { get; private set; }
        public static float LastMessageTime { get; private set; }

        // Reset when entering play mode (domain reload may be off)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Credits = 0;
            ActiveJob = null;
            LastMessage = null;
            LastMessageTime = -100f;
        }

        /// <summary>Shows a short message on the HUD (and in the Console).</summary>
        public static void Notify(string message)
        {
            LastMessage = message;
            LastMessageTime = Time.time;
            Debug.Log($"[Kotwica] {message}");
        }

        public static bool TryAcceptJob(CourierJob job)
        {
            if (ActiveJob != null || job == null) return false;
            ActiveJob = job;
            return true;
        }

        public static void CompleteActiveJob()
        {
            if (ActiveJob == null) return;
            Credits += ActiveJob.Reward;
            ActiveJob = null;
        }
    }
}
