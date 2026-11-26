using System;
using Kotwica.Core;
using Kotwica.World;
using UnityEngine;

namespace Kotwica.Progression
{
    /// <summary>
    /// Standing with each social class, -100..+100. Opens and closes jobs, prices and passages.
    /// Helping one class can cost standing with another.
    /// </summary>
    [Serializable]
    public class Reputation
    {
        [SerializeField] private int[] values = new int[Enum.GetValues(typeof(SocialClass)).Length];

        public int Get(SocialClass socialClass) => values[(int)socialClass];

        public void Change(SocialClass socialClass, int delta)
        {
            if (delta == 0) return;
            int i = (int)socialClass;
            values[i] = Mathf.Clamp(values[i] + delta, -100, 100);
            GameEvents.RaiseReputationChanged(DisplayName(socialClass), values[i]);
        }

        public static string DisplayName(SocialClass socialClass) => socialClass switch
        {
            SocialClass.Robotnicy => "Robotnicy (Kesh)",
            SocialClass.KlasaSrednia => "Klasa średnia (Orrin)",
            SocialClass.Elita => "Elita (Varo)",
            _ => socialClass.ToString()
        };

        public static string Label(int value) => value switch
        {
            <= -50 => "wrogość",
            <= -10 => "nieufność",
            < 10 => "obojętność",
            < 50 => "życzliwość",
            _ => "zaufanie"
        };
    }
}
