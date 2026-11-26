using System;
using System.Collections.Generic;
using Kotwica.Core;
using Kotwica.World;
using UnityEngine;

namespace Kotwica.Security
{
    public enum CrimeType { Wtargniecie, Kradziez, Przemyt, Napasc, Zabojstwo }

    /// <summary>
    /// "Poziom alarmu": 0..5 per ring. Crimes raise it (more on strict, upper-class rings),
    /// it decays while nobody is watching. Saved with the game; police response comes later.
    /// </summary>
    [Serializable]
    public class AlertSystem
    {
        public const int MaxLevel = 5;

        [Serializable]
        public class RingAlert
        {
            public string ringId;
            public float heat;
        }

        [SerializeField] private List<RingAlert> rings = new List<RingAlert>();

        public int Level(string ringId) => Mathf.Clamp(Mathf.CeilToInt(Heat(ringId) - 0.01f), 0, MaxLevel);

        public float Heat(string ringId)
        {
            RingAlert alert = rings.Find(r => r.ringId == ringId);
            return alert != null ? alert.heat : 0f;
        }

        private static float Severity(CrimeType crime) => crime switch
        {
            CrimeType.Wtargniecie => 0.5f,
            CrimeType.Kradziez => 0.7f,
            CrimeType.Przemyt => 1f,
            CrimeType.Napasc => 1.2f,
            CrimeType.Zabojstwo => 2.5f,
            _ => 0.5f
        };

        /// <summary>Unwitnessed crimes count only partly.</summary>
        public void ReportCrime(string ringId, CrimeType crime, bool witnessed)
        {
            if (string.IsNullOrEmpty(ringId)) return;
            RingDefinition ring = RingDefinition.Find(ringId);
            float law = ring != null ? ring.lawMultiplier : 1f;
            float amount = Severity(crime) * law * (witnessed ? 1f : 0.3f);
            SetHeat(ringId, Heat(ringId) + amount);
        }

        /// <summary>Called every frame: heat on every ring cools down while unobserved.</summary>
        public void Tick(float deltaTime, bool playerSeenBySecurity)
        {
            if (playerSeenBySecurity) return;
            foreach (RingAlert alert in rings.ToArray())
            {
                if (alert.heat <= 0f) continue;
                RingDefinition ring = RingDefinition.Find(alert.ringId);
                float decay = ring != null ? ring.alertDecayPerSecond : 0.05f;
                SetHeat(alert.ringId, alert.heat - decay * deltaTime);
            }
        }

        public void Clear(string ringId) => SetHeat(ringId, 0f);

        private void SetHeat(string ringId, float heat)
        {
            int before = Level(ringId);
            RingAlert alert = rings.Find(r => r.ringId == ringId);
            if (alert == null)
            {
                alert = new RingAlert { ringId = ringId };
                rings.Add(alert);
            }
            alert.heat = Mathf.Clamp(heat, 0f, MaxLevel);
            int after = Level(ringId);
            if (after != before) GameEvents.RaiseAlertChanged(ringId, after);
        }
    }
}
