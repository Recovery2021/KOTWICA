using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kotwica.World
{
    /// <summary>Three social classes, one per habitat ring.</summary>
    public enum SocialClass { Robotnicy, KlasaSrednia, Elita }

    public enum DistrictType { Mieszkalna, Praca, Edukacja, Handel, Medycyna, Bezpieczenstwo, Doki, Rozrywka }

    /// <summary>A district inside a ring: where people live, work or study.</summary>
    [Serializable]
    public class DistrictInfo
    {
        public string name;
        public DistrictType type;
        [TextArea] public string description;
    }

    /// <summary>
    /// Data for one habitat ring (city of one social class): its districts and how strict its law is.
    /// Lives in Resources/Rings so any system can look rings up by id.
    /// </summary>
    [CreateAssetMenu(menuName = "Kotwica/Świat - pierścień", fileName = "Ring_")]
    public class RingDefinition : ScriptableObject
    {
        [Tooltip("Short id used by quests, saves and the alert system, e.g. 'kesh'.")]
        public string id = "kesh";
        public string displayName = "Kesh";
        public SocialClass socialClass = SocialClass.Robotnicy;
        [TextArea] public string description;

        [Header("Law")]
        [Tooltip("How much a crime raises the alert here (Kesh 0.6, middle ring 1.0, Varo 1.6).")]
        public float lawMultiplier = 1f;
        [Tooltip("Alert stars lost per second while nobody is watching.")]
        public float alertDecayPerSecond = 0.05f;

        [Header("Security response")]
        [Tooltip("Guards on duty at alert level 5 (fewer at lower levels).")]
        public int maxGuards = 3;
        [Tooltip("From this alert level guards shoot instead of only arresting.")]
        public int armedFromLevel = 3;
        [Tooltip("Guards here take bribes (needs Perswazja 3+).")]
        public bool bribesAccepted;
        [Tooltip("Fine per alert level, before the law multiplier.")]
        public int finePerLevel = 40;

        public List<DistrictInfo> districts = new List<DistrictInfo>();

        private static Dictionary<string, RingDefinition> cache;

        /// <summary>Ring by id (null when unknown).</summary>
        public static RingDefinition Find(string ringId)
        {
            if (string.IsNullOrEmpty(ringId)) return null;
            if (cache == null)
            {
                cache = new Dictionary<string, RingDefinition>();
                foreach (RingDefinition ring in Resources.LoadAll<RingDefinition>("Rings")) cache[ring.id] = ring;
            }
            return cache.TryGetValue(ringId, out RingDefinition found) ? found : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => cache = null;
    }
}
