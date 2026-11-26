using System;
using UnityEngine;

namespace Kotwica.World
{
    /// <summary>
    /// Station time of day (artificial day/night cycle of the habitats). Drives residents' schedules.
    /// One station day lasts 24 real minutes by default (1 hour = 1 minute). Saved with the game.
    /// </summary>
    [Serializable]
    public class StationClock
    {
        public const float MinutesPerDay = 1440f;

        [SerializeField] private int day = 1;
        [SerializeField] private float minuteOfDay = 7f * 60f; // a new game starts at 07:00
        [Tooltip("Real seconds per station day.")]
        [SerializeField] private float realSecondsPerDay = 1440f;

        public int Day => day;
        /// <summary>0..24, fractional.</summary>
        public float Hour => minuteOfDay / 60f;
        public string Label => $"Dzień {day} · {(int)Hour:00}:{(int)(minuteOfDay % 60f):00}";
        public bool IsNight => Hour >= 22f || Hour < 6f;

        public void Tick(float realDeltaSeconds)
        {
            minuteOfDay += realDeltaSeconds * (MinutesPerDay / Mathf.Max(1f, realSecondsPerDay));
            while (minuteOfDay >= MinutesPerDay)
            {
                minuteOfDay -= MinutesPerDay;
                day++;
            }
        }

        /// <summary>Skip time (sleeping, waiting).</summary>
        public void Advance(float hours) => Tick(hours * 60f * realSecondsPerDay / MinutesPerDay);
    }
}
