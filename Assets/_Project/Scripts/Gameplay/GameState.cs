using UnityEngine;

namespace Kotwica.Gameplay
{
    /// <summary>A single courier contract: what to carry, where, and for how much.</summary>
    public class CourierJob
    {
        public string Cargo;
        public DeliveryTerminal Destination;
        public int Reward;
        public double ReturnBy;
        public string DeclaredCargo;
        public DeliveryTerminal Home;
    }

    /// <summary>
    /// Prototype-level game state: money, the active job, the last on-screen message.
    /// Later replaced by a proper save system and quest log.
    /// </summary>
    public static class GameState
    {
        public enum Stage { Repair, Ready, Outbound, Decide, Waiting, Return, Complete }
        public enum Medicine { None, Approved, Undeclared }
        public static Stage Mission { get; private set; }
        public static Medicine Package { get; private set; }
        public static string Outcome { get; private set; }
        public static int NextRewardAdjustment { get; private set; }
        private static double approvalAt;
        public static double ApprovalRemaining => System.Math.Max(0, approvalAt - Kotwica.Orbits.OrbitalClock.Now);

        public static void RepairLift()
        {
            if (Mission != Stage.Repair) return;
            Mission = Stage.Ready;
            Notify("Podnośnik działa. Dokerka prosi o przewóz modułu i lek dla córki.");
        }

        public static void StartReturnCourse(CourierJob job)
        {
            if (Mission != Stage.Ready || !TryAcceptJob(job)) return;
            Mission = Stage.Outbound;
            job.ReturnBy = Kotwica.Orbits.OrbitalClock.Now + 240;
            job.DeclaredCargo = job.Cargo;
        }

        public static void DeliverModule()
        {
            if (Mission != Stage.Outbound) return;
            Mission = Stage.Decide;
            Notify("Serwis przyjął moduł. Medyczka jest za oknem magazynu. Podejdź do bramki kliniki.");
        }

        public static void ChooseMedicine(Medicine choice)
        {
            if (Mission != Stage.Decide) return;
            Package = choice;
            if (choice == Medicine.Approved)
            {
                approvalAt = Kotwica.Orbits.OrbitalClock.Now + 25;
                Mission = Stage.Waiting;
            }
            else BeginReturn();
        }

        public static bool AtClinic => Mission == Stage.Decide || Mission == Stage.Waiting;

        public static void ReturnWithoutMedicine()
        {
            if (!AtClinic) return;
            Package = Medicine.None;
            Mission = Stage.Decide;
            BeginReturn();
        }

        public static void BeginReturn()
        {
            if (Mission == Stage.Waiting && ApprovalRemaining > 0) return;
            var job = ActiveJob;
            job.Destination = job.Home;
            job.DeclaredCargo = Package == Medicine.Approved ? "Moduł i lek" : "Moduł";
            job.Cargo = Package == Medicine.None ? "Moduł po naprawie" : "Moduł po naprawie i lek";
            Mission = Stage.Return;
        }

        public static void ConfiscateMedicine()
        {
            if (Mission != Stage.Return || Package != Medicine.Undeclared) return;
            Package = Medicine.None;
            ActiveJob.Cargo = "Moduł po naprawie";
        }

        public static bool Cleared { get; private set; }
        public static bool InspectionDue { get; private set; }
        public static bool HasMedicine => Package != Medicine.None;

        public static void Inspect()
        {
            if (Mission == Stage.Complete && InspectionDue && ActiveJob != null)
            {
                InspectionDue = false;
                Notify("Ładunek sprawdzony. Port zezwala na start.");
                return;
            }
            if (Mission != Stage.Return || Cleared) return;
            Cleared = true;
            if (Package == Medicine.Undeclared)
            {
                ActiveJob.Reward -= 30;
                ActiveJob.DeclaredCargo = "Moduł i lek";
            }
            Notify("Odprawa zakończona. Podejdź do dokerki przy podnośniku.");
        }

        public static void FinishReturn()
        {
            if (Mission != Stage.Return || (!Cleared && Package != Medicine.Undeclared)) return;
            InspectionDue = !Cleared;
            bool late = Kotwica.Orbits.OrbitalClock.Now > ActiveJob.ReturnBy;
            NextRewardAdjustment = Package == Medicine.Approved ? 30 : 0;
            if (late) NextRewardAdjustment -= 20;
            string family = Package == Medicine.None ? "Córka dokerki nadal czeka na lek." : "Dokerka podała córce lek.";
            Outcome = $"{family} " + (InspectionDue ? "Następny kurs wymaga kontroli przed startem. " : "")
                + (late ? "Spóźniony powrót. " : "") + $"Korekta kolejnej stawki: {NextRewardAdjustment:+0;-0;0} kr.";
            CompleteActiveJob();
            Mission = Stage.Complete;
            Notify(Outcome);
        }

        public static int Credits { get; private set; }
        public static CourierJob ActiveJob { get; private set; }
        public static string LastMessage { get; private set; }
        public static float LastMessageTime { get; private set; }

        // Reset when entering play mode (domain reload may be off)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Mission = Stage.Repair;
            Package = Medicine.None;
            Outcome = null;
            NextRewardAdjustment = 0;
            Cleared = InspectionDue = false;
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
            NextRewardAdjustment = 0;
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
