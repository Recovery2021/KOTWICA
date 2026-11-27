using System.Collections.Generic;
using Kotwica.Core;
using Kotwica.Gameplay;
using Kotwica.Items;
using Kotwica.Progression;
using Kotwica.World;
using UnityEngine;

namespace Kotwica.Dialogue
{
    /// <summary>
    /// Evaluates dialogue conditions against the game state and applies choice effects.
    /// Kept apart from the UI so quests, items and crimes are changed in one place.
    /// </summary>
    public static class DialogueRules
    {
        public static bool Pass(List<DialogueCondition> conditions)
        {
            if (conditions == null) return true;
            foreach (DialogueCondition c in conditions)
                if (!Pass(c)) return false;
            return true;
        }

        public static bool Pass(DialogueCondition c)
        {
            GameServices s = GameServices.Instance;
            if (s == null) return false;
            int amount = Mathf.Max(1, c.value);
            switch (c.type)
            {
                case ConditionType.CelAktywny: return s.IsObjectiveTarget(c.objective, c.id);
                case ConditionType.ZadanieMoznaRozpoczac: return s.Quests.CanStart(c.id);
                case ConditionType.ZadanieAktywne: return s.Quests.IsActive(c.id);
                case ConditionType.ZadanieUkonczone: return s.Quests.IsCompleted(c.id);
                case ConditionType.MaPrzedmiot: return s.Inventory.Has(c.id, amount);
                case ConditionType.NieMaPrzedmiotu: return !s.Inventory.Has(c.id, amount);
                case ConditionType.FlagaUstawiona: return s.Quests.HasFlag(c.id);
                case ConditionType.FlagaNieustawiona: return !s.Quests.HasFlag(c.id);
                case ConditionType.KredytyCoNajmniej: return GameState.Credits >= c.value;
                default: return true;
            }
        }

        /// <summary>True when the player is strong enough for the choice's attribute check (or there is none).</summary>
        public static bool CheckPassed(DialogueChoice choice)
        {
            if (!choice.check) return true;
            GameServices s = GameServices.Instance;
            return s != null && s.Profile.Get(choice.attribute) >= choice.difficulty;
        }

        /// <summary>Picked a choice: practise the tested attribute, then run its effects in order.</summary>
        public static void Apply(DialogueChoice choice, Vector3 playerPosition)
        {
            if (choice.check) GameServices.Practice(choice.attribute, 10f + 5f * choice.difficulty);
            foreach (DialogueEffect effect in choice.effects) Apply(effect, playerPosition);
        }

        private static void Apply(DialogueEffect e, Vector3 playerPosition)
        {
            GameServices s = GameServices.Instance;
            if (s == null) return;
            int amount = Mathf.Max(1, e.value);
            switch (e.type)
            {
                case EffectType.RozpocznijZadanie: s.Quests.Start(e.id); break;
                case EffectType.ZglosCel: GameServices.Report(e.objective, e.id); break;
                case EffectType.DajPrzedmiot:
                    s.Inventory.Add(e.id, amount);
                    GameState.Notify($"Otrzymano: {ItemName(e.id)}");
                    break;
                case EffectType.ZabierzPrzedmiot:
                    s.Inventory.Remove(e.id, amount);
                    GameState.Notify($"Oddano: {ItemName(e.id)}");
                    break;
                case EffectType.Kredyty:
                    GameState.AddCredits(e.value);
                    GameState.Notify(e.value >= 0 ? $"+{e.value} kr" : $"{e.value} kr");
                    break;
                case EffectType.Reputacja: GameServices.ChangeReputation(e.socialClass, e.value); break;
                case EffectType.UstawFlage: s.Quests.SetFlag(e.id); break;
                case EffectType.Doswiadczenie: GameServices.AwardXp(e.value); break;
                case EffectType.Praktyka: GameServices.Practice(e.attribute, e.value); break;
                case EffectType.Przestepstwo:
                    bool seen = Witnesses.AnyoneSees(playerPosition);
                    GameServices.ReportCrime(null, e.crime, seen);
                    if (seen) GameState.Notify("Ktoś to widział!");
                    break;
                case EffectType.OblejZadanie: s.Quests.Fail(e.id); break;
            }
        }

        private static string ItemName(string itemId)
        {
            ItemDefinition item = ItemDefinition.Find(itemId);
            return item != null ? item.displayName : itemId;
        }
    }
}
