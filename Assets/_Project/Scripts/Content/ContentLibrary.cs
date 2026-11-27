using System;
using System.Collections.Generic;
using System.IO;
using Kotwica.Dialogue;
using Kotwica.Items;
using Kotwica.Progression;
using Kotwica.Quests;
using Kotwica.Security;
using Kotwica.World;
using UnityEngine;

namespace Kotwica.Content
{
    /// <summary>
    /// Reads the editable game content from StreamingAssets/Kotwica/**/*.json (quests, conversations, items)
    /// and turns it into the same definitions the game uses everywhere. When the folder holds content,
    /// it replaces the old assets in Resources. Problems (typos in ids, missing nodes, unknown enum words)
    /// are logged as warnings with the file name, and listed by "Kotwica/Zadania: sprawdź pliki".
    /// In a built game the folder sits next to the .exe (Kotwica_Data/StreamingAssets/Kotwica) and can be edited there.
    /// </summary>
    public static class ContentLibrary
    {
        public const string FolderName = "Kotwica";

        private static bool loaded;
        private static readonly List<QuestDefinition> quests = new List<QuestDefinition>();
        private static readonly List<DialogueDefinition> dialogues = new List<DialogueDefinition>();
        private static readonly List<ItemDefinition> items = new List<ItemDefinition>();
        private static readonly List<string> problems = new List<string>();

        public static string RootPath => Path.Combine(Application.streamingAssetsPath, FolderName);

        public static IReadOnlyList<QuestDefinition> Quests { get { EnsureLoaded(); return quests; } }
        public static IReadOnlyList<DialogueDefinition> Dialogues { get { EnsureLoaded(); return dialogues; } }
        public static IReadOnlyList<ItemDefinition> Items { get { EnsureLoaded(); return items; } }
        public static IReadOnlyList<string> Problems { get { EnsureLoaded(); return problems; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => loaded = false;

        public static void EnsureLoaded()
        {
            if (loaded) return;
            Reload();
        }

        /// <summary>Reads every file again (play mode start, or the editor check menu).</summary>
        public static void Reload()
        {
            loaded = true;
            quests.Clear();
            dialogues.Clear();
            items.Clear();
            problems.Clear();

            if (!Directory.Exists(RootPath)) return;
            string[] files = Directory.GetFiles(RootPath, "*.json", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal);
            foreach (string file in files) ReadFile(file);

            Validate();
            foreach (string problem in problems) Debug.LogWarning("[Kotwica] Zadania/rozmowy: " + problem);
            if (files.Length > 0)
                Debug.Log($"[Kotwica] Wczytano z plików: {quests.Count} zadań, {dialogues.Count} rozmów, {items.Count} przedmiotów" +
                          (problems.Count > 0 ? $" ({problems.Count} problemów, szczegóły wyżej)." : "."));
        }

        private static void ReadFile(string path)
        {
            string name = Path.GetFileName(path);
            ContentFile file;
            try
            {
                file = JsonUtility.FromJson<ContentFile>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                problems.Add($"{name}: błąd składni JSON ({e.Message}). Plik pominięty.");
                return;
            }
            if (file == null) return;

            if (file.przedmioty != null) foreach (ItemData data in file.przedmioty) AddItem(data, name);
            if (file.zadania != null) foreach (QuestData data in file.zadania) AddQuest(data, name);
            if (file.rozmowy != null) foreach (DialogueData data in file.rozmowy) AddDialogue(data, name);
        }

        // --- Conversion -------------------------------------------------------------------------------

        private static void AddItem(ItemData data, string file)
        {
            if (string.IsNullOrEmpty(data.id)) { problems.Add($"{file}: przedmiot bez id."); return; }
            if (items.Exists(i => i.id == data.id)) { problems.Add($"{file}: przedmiot '{data.id}' jest zdefiniowany drugi raz."); return; }

            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.name = data.id;
            item.id = data.id;
            item.displayName = string.IsNullOrEmpty(data.nazwa) ? data.id : data.nazwa;
            item.description = data.opis;
            item.category = Parse(data.kategoria, ItemCategory.Inne, file, $"przedmiot {data.id}");
            item.value = data.wartosc;
            item.legal = !data.nielegalny;
            items.Add(item);
        }

        private static void AddQuest(QuestData data, string file)
        {
            if (string.IsNullOrEmpty(data.id)) { problems.Add($"{file}: zadanie bez id."); return; }
            if (quests.Exists(q => q.id == data.id)) { problems.Add($"{file}: zadanie '{data.id}' jest zdefiniowane drugi raz."); return; }
            string where = $"zadanie {data.id}";

            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.name = data.id;
            quest.id = data.id;
            quest.title = string.IsNullOrEmpty(data.tytul) ? data.id : data.tytul;
            quest.description = data.opis;
            quest.kind = Parse(data.rodzaj, QuestKind.Poboczny, file, where);
            quest.ringId = data.pierscien;
            quest.minLevel = Mathf.Max(1, data.minPoziom);
            quest.requiredQuests = data.wymagane != null ? new List<string>(data.wymagane) : new List<string>();
            quest.autoStart = data.autostart;
            quest.repeatable = data.powtarzalne;

            quest.objectives = new List<QuestObjective>();
            if (data.cele != null)
                foreach (ObjectiveData o in data.cele)
                    quest.objectives.Add(new QuestObjective
                    {
                        description = o.opis,
                        type = Parse(o.typ, ObjectiveType.Interakcja, file, where),
                        targetId = o.cel,
                        count = Mathf.Max(1, o.ilosc)
                    });
            if (quest.objectives.Count == 0) problems.Add($"{file}: {where} nie ma żadnego celu.");

            RewardData r = data.nagroda ?? new RewardData();
            quest.reward = new QuestReward { credits = r.kredyty, xp = r.xp };
            if (r.reputacja != null)
                foreach (ReputationData rep in r.reputacja)
                    quest.reward.reputation.Add(new ReputationReward { socialClass = Parse(rep.klasa, SocialClass.Robotnicy, file, where), amount = rep.wartosc });
            if (r.przedmioty != null)
                foreach (ItemCountData stack in r.przedmioty)
                    quest.reward.items.Add(new ItemStack { itemId = stack.id, count = Mathf.Max(1, stack.ilosc) });
            quests.Add(quest);
        }

        private static void AddDialogue(DialogueData data, string file)
        {
            if (string.IsNullOrEmpty(data.id)) { problems.Add($"{file}: rozmowa bez id."); return; }
            if (dialogues.Exists(d => d.id == data.id)) { problems.Add($"{file}: rozmowa '{data.id}' jest zdefiniowana drugi raz."); return; }
            string where = $"rozmowa {data.id}";

            var dialogue = ScriptableObject.CreateInstance<DialogueDefinition>();
            dialogue.name = data.id;
            dialogue.id = data.id;
            dialogue.speaker = data.rozmowca;
            dialogue.entries = new List<DialogueEntry>();
            dialogue.nodes = new List<DialogueNode>();

            if (data.wejscia != null)
                foreach (EntryData e in data.wejscia)
                    dialogue.entries.Add(new DialogueEntry { node = e.wezel, conditions = Conditions(e.warunki, file, where) });

            if (data.wezly != null)
                foreach (NodeData n in data.wezly)
                {
                    var node = new DialogueNode { id = n.id, speaker = n.mowca, text = n.tekst, choices = new List<DialogueChoice>() };
                    if (n.wybory != null)
                        foreach (ChoiceData c in n.wybory)
                        {
                            var choice = new DialogueChoice
                            {
                                text = c.tekst,
                                next = c.dalej ?? "",
                                conditions = Conditions(c.warunki, file, where),
                                effects = Effects(c.efekty, file, where)
                            };
                            if (c.test != null && !string.IsNullOrEmpty(c.test.atrybut))
                            {
                                choice.check = true;
                                choice.attribute = Parse(c.test.atrybut, PlayerAttribute.Perswazja, file, where);
                                choice.difficulty = Mathf.Max(1, c.test.trudnosc);
                            }
                            node.choices.Add(choice);
                        }
                    dialogue.nodes.Add(node);
                }
            dialogues.Add(dialogue);
        }

        private static List<DialogueCondition> Conditions(List<ConditionData> list, string file, string where)
        {
            var result = new List<DialogueCondition>();
            if (list == null) return result;
            foreach (ConditionData c in list)
                result.Add(new DialogueCondition
                {
                    type = Parse(c.typ, ConditionType.FlagaUstawiona, file, where),
                    objective = Parse(c.cel, ObjectiveType.Interakcja, file, where, optional: true),
                    id = c.id,
                    value = c.wartosc
                });
            return result;
        }

        private static List<DialogueEffect> Effects(List<EffectData> list, string file, string where)
        {
            var result = new List<DialogueEffect>();
            if (list == null) return result;
            foreach (EffectData e in list)
                result.Add(new DialogueEffect
                {
                    type = Parse(e.typ, EffectType.UstawFlage, file, where),
                    objective = Parse(e.cel, ObjectiveType.Interakcja, file, where, optional: true),
                    id = e.id,
                    value = e.wartosc,
                    socialClass = Parse(e.klasa, SocialClass.Robotnicy, file, where, optional: true),
                    attribute = Parse(e.atrybut, PlayerAttribute.Perswazja, file, where, optional: true),
                    crime = Parse(e.przestepstwo, CrimeType.Kradziez, file, where, optional: true)
                });
            return result;
        }

        /// <summary>Enum from text ("Porozmawiaj"); unknown words are reported, empty ones use the fallback.</summary>
        private static T Parse<T>(string text, T fallback, string file, string where, bool optional = false) where T : struct
        {
            if (string.IsNullOrEmpty(text))
            {
                if (!optional) problems.Add($"{file}: {where}: brak wartości typu {typeof(T).Name}, przyjęto '{fallback}'.");
                return fallback;
            }
            if (Enum.TryParse(text.Trim(), true, out T value)) return value;
            problems.Add($"{file}: {where}: nieznane słowo '{text}' (dozwolone: {string.Join(", ", Enum.GetNames(typeof(T)))}).");
            return fallback;
        }

        // --- Checks -----------------------------------------------------------------------------------

        private static void Validate()
        {
            bool QuestKnown(string id) => quests.Exists(q => q.id == id);
            bool ItemKnown(string id) => items.Count == 0 || items.Exists(i => i.id == id);

            foreach (QuestDefinition q in quests)
            {
                foreach (string required in q.requiredQuests)
                    if (!QuestKnown(required)) problems.Add($"zadanie {q.id}: wymaga nieznanego zadania '{required}'.");
                foreach (QuestObjective o in q.objectives)
                {
                    if (string.IsNullOrEmpty(o.targetId)) problems.Add($"zadanie {q.id}: cel '{o.description}' nie ma pola \"cel\".");
                    else if (o.type == ObjectiveType.Zdobadz && !ItemKnown(o.targetId)) problems.Add($"zadanie {q.id}: Zdobadz nieznanego przedmiotu '{o.targetId}'.");
                    else if (o.type == ObjectiveType.Lec && !o.targetId.StartsWith("port_")) problems.Add($"zadanie {q.id}: cel Lec powinien mieć postać port_<id>, jest '{o.targetId}'.");
                }
                foreach (ItemStack stack in q.reward.items)
                    if (!ItemKnown(stack.itemId)) problems.Add($"zadanie {q.id}: nagroda to nieznany przedmiot '{stack.itemId}'.");
            }

            foreach (DialogueDefinition d in dialogues)
            {
                string where = $"rozmowa {d.id}";
                if (d.entries.Count == 0) problems.Add($"{where}: brak wejść (rozmowa nigdy się nie otworzy).");
                else if (d.entries[d.entries.Count - 1].conditions.Count > 0)
                    problems.Add($"{where}: ostatnie wejście ma warunki; dodaj na końcu wejście bez warunków (kwestia domyślna).");

                foreach (DialogueEntry e in d.entries)
                {
                    if (d.Node(e.node) == null) problems.Add($"{where}: wejście prowadzi do nieznanego węzła '{e.node}'.");
                    CheckConditions(e.conditions, where, QuestKnown, ItemKnown);
                }
                foreach (DialogueNode n in d.nodes)
                    foreach (DialogueChoice c in n.choices)
                    {
                        string at = $"{where}, węzeł {n.id}";
                        if (!string.IsNullOrEmpty(c.next) && d.Node(c.next) == null) problems.Add($"{at}: wybór prowadzi do nieznanego węzła '{c.next}'.");
                        CheckConditions(c.conditions, at, QuestKnown, ItemKnown);
                        foreach (DialogueEffect ef in c.effects)
                        {
                            bool questEffect = ef.type == EffectType.RozpocznijZadanie || ef.type == EffectType.OblejZadanie;
                            bool itemEffect = ef.type == EffectType.DajPrzedmiot || ef.type == EffectType.ZabierzPrzedmiot;
                            if (questEffect && !QuestKnown(ef.id)) problems.Add($"{at}: efekt {ef.type} dla nieznanego zadania '{ef.id}'.");
                            if (itemEffect && !ItemKnown(ef.id)) problems.Add($"{at}: efekt {ef.type} dla nieznanego przedmiotu '{ef.id}'.");
                        }
                    }
            }
        }

        private static void CheckConditions(List<DialogueCondition> list, string where, Func<string, bool> questKnown, Func<string, bool> itemKnown)
        {
            foreach (DialogueCondition c in list)
            {
                bool questCondition = c.type == ConditionType.ZadanieAktywne || c.type == ConditionType.ZadanieUkonczone || c.type == ConditionType.ZadanieMoznaRozpoczac;
                bool itemCondition = c.type == ConditionType.MaPrzedmiot || c.type == ConditionType.NieMaPrzedmiotu;
                if (questCondition && !questKnown(c.id)) problems.Add($"{where}: warunek {c.type} dla nieznanego zadania '{c.id}'.");
                if (itemCondition && !itemKnown(c.id)) problems.Add($"{where}: warunek {c.type} dla nieznanego przedmiotu '{c.id}'.");
            }
        }
    }
}
