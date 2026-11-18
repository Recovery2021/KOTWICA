using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kotwica.Combat
{
    public enum Team { Player, Enemy }

    /// <summary>
    /// Hit points for the player and enemies. Others listen to Damaged / Died
    /// (AI reacts to being shot, HUD shows damage, airlock kills).
    /// </summary>
    public class Health : MonoBehaviour
    {
        public static readonly List<Health> All = new List<Health>();

        [SerializeField] private Team team = Team.Enemy;
        [SerializeField] private float maxHealth = 100f;

        public Team Team => team;
        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;
        public string DeathCause { get; private set; }
        public float LastDamageTime { get; private set; } = -100f;

        /// <summary>(who, direction the damage came from)</summary>
        public event Action<Health, Vector3> Damaged;
        public event Action<Health> Died;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        private void Awake() => Current = maxHealth;
        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() => All.Remove(this);

        public void TakeDamage(float amount, Vector3 direction, string cause = "Postrzał")
        {
            if (IsDead || amount <= 0f) return;

            Current = Mathf.Max(0f, Current - amount);
            LastDamageTime = Time.time;
            Damaged?.Invoke(this, direction);

            if (IsDead)
            {
                DeathCause = cause;
                Died?.Invoke(this);
            }
        }

        public void Kill(string cause) => TakeDamage(Current + 1f, Vector3.zero, cause);

        /// <summary>First living character of a team (e.g. AI looking for the player).</summary>
        public static Health FindFirst(Team team)
        {
            foreach (Health h in All)
                if (h.Team == team && !h.IsDead) return h;
            return null;
        }

        public static int CountAlive(Team team)
        {
            int count = 0;
            foreach (Health h in All)
                if (h.Team == team && !h.IsDead) count++;
            return count;
        }
    }
}
