using System;
using Kotwica.Core;
using Kotwica.Gameplay;
using UnityEngine;

namespace Kotwica.Progression
{
    public enum PlayerAttribute { Sprawnosc, Walka, Technika, Pilotaz, Perswazja }

    /// <summary>
    /// RPG progression of the player, saved as JSON:
    /// - level from XP (quests, activities, kills), each level gives one attribute point;
    /// - attributes 1..10 that also grow by use (running trains Sprawność, shooting trains Walka...).
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public const int MaxAttribute = 10;
        private static readonly int AttributeCount = Enum.GetValues(typeof(PlayerAttribute)).Length;

        [SerializeField] private int level = 1;
        [SerializeField] private int xp;
        [SerializeField] private int unspentPoints;
        [SerializeField] private int[] attributes;
        [SerializeField] private float[] practice;

        public int Level => level;
        public int Xp => xp;
        public int UnspentPoints => unspentPoints;
        public int XpToNextLevel => XpRequired(level);

        public PlayerProfile() => EnsureArrays();

        /// <summary>XP needed to go from this level to the next (100, 150, 200...).</summary>
        public static int XpRequired(int fromLevel) => 100 + 50 * (fromLevel - 1);

        public static string DisplayName(PlayerAttribute attribute) => attribute switch
        {
            PlayerAttribute.Sprawnosc => "Sprawność",
            PlayerAttribute.Walka => "Walka",
            PlayerAttribute.Technika => "Technika",
            PlayerAttribute.Pilotaz => "Pilotaż",
            PlayerAttribute.Perswazja => "Perswazja",
            _ => attribute.ToString()
        };

        /// <summary>Old saves may have fewer attributes: pad the arrays.</summary>
        public void EnsureArrays()
        {
            if (attributes == null || attributes.Length != AttributeCount)
            {
                var fresh = new int[AttributeCount];
                for (int i = 0; i < AttributeCount; i++) fresh[i] = attributes != null && i < attributes.Length ? attributes[i] : 1;
                attributes = fresh;
            }
            if (practice == null || practice.Length != AttributeCount) practice = new float[AttributeCount];
        }

        public int Get(PlayerAttribute attribute) => attributes[(int)attribute];

        /// <summary>Gameplay bonus: 1.0 at attribute 1, +perPoint for every point above it.</summary>
        public float Multiplier(PlayerAttribute attribute, float perPoint) => 1f + perPoint * (Get(attribute) - 1);

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            xp += amount;
            GameEvents.RaiseXpGained(amount);
            while (xp >= XpRequired(level))
            {
                xp -= XpRequired(level);
                level++;
                unspentPoints++;
                GameEvents.RaiseLevelUp(level);
                GameState.Notify($"Poziom {level}! Masz punkt atrybutu (Tab).");
            }
        }

        /// <summary>Spend a level-up point on an attribute.</summary>
        public bool SpendPoint(PlayerAttribute attribute)
        {
            if (unspentPoints <= 0 || Get(attribute) >= MaxAttribute) return false;
            unspentPoints--;
            Raise(attribute);
            return true;
        }

        /// <summary>Learning by doing: enough practice raises the attribute by one.</summary>
        public void Practice(PlayerAttribute attribute, float amount)
        {
            int i = (int)attribute;
            if (attributes[i] >= MaxAttribute || amount <= 0f) return;
            practice[i] += amount;
            float needed = PracticeRequired(attribute);
            if (practice[i] < needed) return;
            practice[i] -= needed;
            Raise(attribute);
            GameState.Notify($"{DisplayName(attribute)} rośnie dzięki praktyce: {Get(attribute)}");
        }

        public float PracticeRequired(PlayerAttribute attribute) => 100f * Get(attribute);
        public float PracticeProgress(PlayerAttribute attribute) => practice[(int)attribute] / PracticeRequired(attribute);

        private void Raise(PlayerAttribute attribute)
        {
            attributes[(int)attribute]++;
            GameEvents.RaiseAttributeRaised(DisplayName(attribute), Get(attribute));
        }
    }
}
