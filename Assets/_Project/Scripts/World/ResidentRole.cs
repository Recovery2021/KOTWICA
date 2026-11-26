using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kotwica.World
{
    public enum Activity { Dom, Praca, Nauka, Wypoczynek }

    [Serializable]
    public class ScheduleEntry
    {
        [Range(0, 23)] public int fromHour;
        public Activity activity;
    }

    /// <summary>
    /// A kind of resident (dock worker on the day shift, clerk, student...): what they do at each hour
    /// and what they mutter when you walk past. Data asset, edited without code.
    /// </summary>
    [CreateAssetMenu(menuName = "Kotwica/Mieszkaniec - rola", fileName = "Role_")]
    public class ResidentRole : ScriptableObject
    {
        public string roleName = "Mieszkaniec";
        public SocialClass socialClass = SocialClass.Robotnicy;
        [Tooltip("Sorted by hour; each entry lasts until the next one (the last wraps over midnight).")]
        public List<ScheduleEntry> schedule = new List<ScheduleEntry>();

        [Header("Lines when the player walks by")]
        public string homeLine = "Idę do domu.";
        public string workLine = "Robota sama się nie zrobi.";
        public string studyLine = "Spóźnię się na zajęcia.";
        public string leisureLine = "W końcu chwila spokoju.";

        public Activity ActivityAt(float hour)
        {
            if (schedule.Count == 0) return Activity.Dom;
            hour = Mathf.Repeat(hour, 24f);
            ScheduleEntry current = schedule[schedule.Count - 1]; // before the first entry = still last night's
            foreach (ScheduleEntry entry in schedule)
                if (entry.fromHour <= hour) current = entry;
            return current.activity;
        }

        public string LineFor(Activity activity) => activity switch
        {
            Activity.Dom => homeLine,
            Activity.Praca => workLine,
            Activity.Nauka => studyLine,
            _ => leisureLine
        };
    }
}
