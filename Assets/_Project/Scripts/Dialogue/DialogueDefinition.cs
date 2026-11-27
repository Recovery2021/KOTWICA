using System;
using System.Collections.Generic;
using Kotwica.Progression;
using Kotwica.Quests;
using Kotwica.Security;
using Kotwica.World;
using UnityEngine;

namespace Kotwica.Dialogue
{
    /// <summary>What a condition checks (all conditions of a choice or entry must pass).</summary>
    public enum ConditionType
    {
        CelAktywny,             // some active quest's current objective is objective + id
        ZadanieMoznaRozpoczac,  // quest id can be started now
        ZadanieAktywne,
        ZadanieUkonczone,
        MaPrzedmiot,            // owns at least value (min 1) of item id
        NieMaPrzedmiotu,
        FlagaUstawiona,         // story flag id is set
        FlagaNieustawiona,
        KredytyCoNajmniej       // credits >= value
    }

    /// <summary>What a choice does when picked (effects run in list order).</summary>
    public enum EffectType
    {
        RozpocznijZadanie,  // start quest id
        ZglosCel,           // report objective + id (advances matching quests)
        DajPrzedmiot,       // give value (min 1) of item id
        ZabierzPrzedmiot,
        Kredyty,            // add value credits (negative = pay)
        Reputacja,          // change socialClass reputation by value
        UstawFlage,         // set story flag id
        Doswiadczenie,      // value XP
        Praktyka,           // value practice points for attribute
        Przestepstwo,       // crime on the current ring, witnessed if somebody sees the player
        OblejZadanie        // mark quest id as failed
    }

    [Serializable]
    public class DialogueCondition
    {
        public ConditionType type;
        public ObjectiveType objective;
        public string id;
        public int value;
    }

    [Serializable]
    public class DialogueEffect
    {
        public EffectType type;
        public ObjectiveType objective;
        public string id;
        public int value;
        public SocialClass socialClass;
        public PlayerAttribute attribute;
        public CrimeType crime;
    }

    [Serializable]
    public class DialogueChoice
    {
        [TextArea(1, 3)] public string text = "...";
        [Tooltip("Node shown next; empty = the conversation ends.")]
        public string next;
        [Tooltip("Hidden unless all pass.")]
        public List<DialogueCondition> conditions = new List<DialogueCondition>();

        [Header("Attribute check (shown, locked when too low)")]
        public bool check;
        public PlayerAttribute attribute = PlayerAttribute.Perswazja;
        [Min(1)] public int difficulty = 3;

        public List<DialogueEffect> effects = new List<DialogueEffect>();
    }

    [Serializable]
    public class DialogueNode
    {
        public string id = "start";
        [Tooltip("Empty = the dialogue's speaker.")]
        public string speaker;
        [TextArea(2, 6)] public string text;
        [Tooltip("No choices = one 'Dalej' button that ends the talk.")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();
    }

    /// <summary>Picks the opening node: the first entry whose conditions all pass.</summary>
    [Serializable]
    public class DialogueEntry
    {
        public string node;
        public List<DialogueCondition> conditions = new List<DialogueCondition>();
    }

    /// <summary>
    /// A conversation as data (normally from the JSON files in StreamingAssets/Kotwica): opening entries chosen by quest state,
    /// nodes with lines, and choices that can need an attribute (e.g. Perswazja 3) and change the world.
    /// </summary>
    [CreateAssetMenu(menuName = "Kotwica/Rozmowa", fileName = "Dialogue_")]
    public class DialogueDefinition : ScriptableObject
    {
        public string id = "dialogue";
        public string speaker = "Rozmówca";
        public List<DialogueEntry> entries = new List<DialogueEntry>();
        public List<DialogueNode> nodes = new List<DialogueNode>();

        public DialogueNode Node(string nodeId) => nodes.Find(n => n.id == nodeId);

        private static Dictionary<string, DialogueDefinition> cache;

        public static DialogueDefinition Find(string dialogueId)
        {
            if (cache == null)
            {
                cache = new Dictionary<string, DialogueDefinition>();
                var fromFiles = Kotwica.Content.ContentLibrary.Dialogues; // editable JSON first, assets as fallback
                if (fromFiles.Count > 0)
                    foreach (DialogueDefinition d in fromFiles) cache[d.id] = d;
                else
                    foreach (DialogueDefinition d in Resources.LoadAll<DialogueDefinition>("Dialogues")) cache[d.id] = d;
            }
            return dialogueId != null && cache.TryGetValue(dialogueId, out DialogueDefinition found) ? found : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => cache = null;
    }
}
