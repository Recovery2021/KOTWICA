using System;
using Kotwica.Core;
using Kotwica.Items;
using Kotwica.Progression;
using Kotwica.Quests;
using Kotwica.World;
using UnityEngine;

namespace Kotwica.UI
{
    /// <summary>
    /// Prototype RPG screens (IMGUI), present in every scene:
    /// Tab opens Character / Journal / Inventory / Reputation; when closed a small tracker
    /// shows the current objective, level and the alert level of the current ring.
    /// </summary>
    public class RpgPanel : MonoBehaviour
    {
        private enum Tab { Postac, Dziennik, Ekwipunek, Reputacja }

        private bool open;
        private Tab tab;
        private string selectedQuest;
        private Vector2 scroll;
        private CursorLockMode cursorBefore;
        private GUIStyle title, text, small, header, button, tracker;

        private void Update()
        {
            if (!GameInput.JournalTogglePressed()) return;
            open = !open;
            if (open)
            {
                cursorBefore = Cursor.lockState;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = cursorBefore;
                Cursor.visible = cursorBefore != CursorLockMode.Locked;
            }
        }

        private void OnGUI()
        {
            GameServices services = GameServices.Instance;
            if (services == null) return;
            EnsureStyles();

            if (open) DrawPanel(services);
            else DrawTracker(services);
        }

        // --- Always-on tracker ------------------------------------------------------------

        private void DrawTracker(GameServices services)
        {
            float w = 330f;
            float x = Screen.width - w - 16f;
            float y = 150f;

            GUI.Label(new Rect(x, y, w, 22), services.Clock.Label, small);
            y += 20f;

            PlayerProfile p = services.Profile;
            GUI.Label(new Rect(x, y, w, 22), $"Poziom {p.Level} · XP {p.Xp}/{p.XpToNextLevel}" + (p.UnspentPoints > 0 ? "  [Tab: punkt!]" : ""), small);
            y += 24f;

            QuestState tracked = services.Quests.Tracked;
            if (tracked != null && tracked.Definition != null)
            {
                QuestObjective objective = tracked.CurrentObjective;
                GUI.Label(new Rect(x, y, w, 24), tracked.Definition.title, tracker);
                y += 24f;
                if (objective != null)
                {
                    string count = objective.count > 1 ? $" ({tracked.progress}/{objective.count})" : "";
                    GUI.Label(new Rect(x, y, w, 40), "> " + objective.description + count, text);
                    y += 42f;
                }
            }

            string ring = services.CurrentRingId;
            if (ring != null)
            {
                int level = services.Alert.Level(ring);
                if (level > 0)
                {
                    GUI.Label(new Rect(x, y, w, 26), $"ALARM {new string('*', level)}{new string('·', 5 - level)}",
                        new GUIStyle(tracker) { normal = { textColor = new Color(1f, 0.35f, 0.25f) } });
                }
            }
        }

        // --- Full panel -------------------------------------------------------------------

        private void DrawPanel(GameServices services)
        {
            var area = new Rect(Screen.width * 0.5f - 400, Screen.height * 0.5f - 260, 800, 520);
            GUI.Box(area, GUIContent.none);
            GUI.Box(area, GUIContent.none); // second pass = darker background
            GUILayout.BeginArea(new Rect(area.x + 20, area.y + 16, area.width - 40, area.height - 32));

            GUILayout.BeginHorizontal();
            foreach (Tab t in Enum.GetValues(typeof(Tab)))
            {
                string label = t switch { Tab.Postac => "Postać", Tab.Dziennik => "Dziennik", Tab.Ekwipunek => "Ekwipunek", _ => "Reputacja" };
                if (GUILayout.Toggle(tab == t, label, button, GUILayout.Height(32))) tab = t;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(12);

            scroll = GUILayout.BeginScrollView(scroll);
            switch (tab)
            {
                case Tab.Postac: DrawCharacter(services.Profile); break;
                case Tab.Dziennik: DrawJournal(services.Quests); break;
                case Tab.Ekwipunek: DrawInventory(services.Inventory); break;
                case Tab.Reputacja: DrawReputation(services.Reputation); break;
            }
            GUILayout.EndScrollView();

            GUILayout.Label("Tab: zamknij · F5: szybki zapis · F9: wczytaj", small);
            GUILayout.EndArea();
        }

        private void DrawCharacter(PlayerProfile p)
        {
            GUILayout.Label($"{Kotwica.Characters.CharacterSelection.PlayerName} · poziom {p.Level}", title);
            GUILayout.Label($"Doświadczenie: {p.Xp} / {p.XpToNextLevel}", text);
            GUILayout.Label(p.UnspentPoints > 0 ? $"Punkty do rozdania: {p.UnspentPoints}" : "Brak wolnych punktów", text);
            GUILayout.Space(10);

            foreach (PlayerAttribute attribute in Enum.GetValues(typeof(PlayerAttribute)))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(PlayerProfile.DisplayName(attribute), text, GUILayout.Width(140));
                GUILayout.Label($"{p.Get(attribute)} / {PlayerProfile.MaxAttribute}", text, GUILayout.Width(70));
                GUILayout.Label($"praktyka {Mathf.RoundToInt(p.PracticeProgress(attribute) * 100f)}%", small, GUILayout.Width(120));
                GUI.enabled = p.UnspentPoints > 0 && p.Get(attribute) < PlayerProfile.MaxAttribute;
                if (GUILayout.Button("+", button, GUILayout.Width(36))) p.SpendPoint(attribute);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        private void DrawJournal(QuestLog log)
        {
            if (log.Quests.Count == 0)
            {
                GUILayout.Label("Brak zadań.", text);
                return;
            }

            GUILayout.BeginHorizontal();

            // List: active first, then finished
            GUILayout.BeginVertical(GUILayout.Width(300));
            foreach (QuestStatus status in new[] { QuestStatus.Aktywne, QuestStatus.Ukonczone, QuestStatus.Nieudane })
            {
                foreach (QuestState state in log.Quests)
                {
                    if (state.status != status || state.Definition == null) continue;
                    string kind = state.Definition.kind switch { QuestKind.Glowny => "[G]", QuestKind.Poboczny => "[P]", _ => "[A]" };
                    string mark = status == QuestStatus.Aktywne ? "" : status == QuestStatus.Ukonczone ? "  (ukończone)" : "  (nieudane)";
                    if (GUILayout.Button($"{kind} {state.Definition.title}{mark}", button)) selectedQuest = state.questId;
                }
            }
            GUILayout.EndVertical();

            // Details
            GUILayout.BeginVertical();
            QuestState selected = log.Get(selectedQuest) ?? log.Tracked;
            if (selected != null && selected.Definition != null)
            {
                QuestDefinition def = selected.Definition;
                GUILayout.Label(def.title, header);
                GUILayout.Label(def.description, text);
                GUILayout.Space(8);
                for (int i = 0; i < def.objectives.Count; i++)
                {
                    bool done = selected.status == QuestStatus.Ukonczone || i < selected.step;
                    bool current = selected.status == QuestStatus.Aktywne && i == selected.step;
                    if (!done && !current) continue; // future steps stay hidden
                    GUILayout.Label((done ? "[x] " : "> ") + def.objectives[i].description, done ? small : text);
                }
                GUILayout.Space(8);
                GUILayout.Label($"Nagroda: {def.reward.xp} XP" + (def.reward.credits > 0 ? $", {def.reward.credits} kr" : ""), small);
                if (selected.status == QuestStatus.Aktywne && GUILayout.Button("Śledź na ekranie", button, GUILayout.Width(180)))
                    log.Track(selected.questId);
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawInventory(Inventory inventory)
        {
            if (inventory.Stacks.Count == 0)
            {
                GUILayout.Label("Pusto.", text);
                return;
            }
            foreach (ItemStack stack in inventory.Stacks)
            {
                ItemDefinition item = ItemDefinition.Find(stack.itemId);
                string name = item != null ? item.displayName : stack.itemId;
                string flag = item != null && !item.legal ? "  [KONTRABANDA]" : "";
                GUILayout.Label($"{name} × {stack.count}{flag}", text);
                if (item != null && !string.IsNullOrEmpty(item.description)) GUILayout.Label(item.description, small);
            }
        }

        private void DrawReputation(Reputation reputation)
        {
            foreach (SocialClass socialClass in Enum.GetValues(typeof(SocialClass)))
            {
                int value = reputation.Get(socialClass);
                GUILayout.BeginHorizontal();
                GUILayout.Label(Reputation.DisplayName(socialClass), text, GUILayout.Width(240));
                GUILayout.Label($"{value:+0;-0;0}  ({Reputation.Label(value)})", text);
                GUILayout.EndHorizontal();
            }
        }

        private void EnsureStyles()
        {
            if (title != null) return;
            var light = new Color(0.85f, 0.9f, 0.95f);
            title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = light } };
            header = new GUIStyle(title) { fontSize = 19 };
            text = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, normal = { textColor = light } };
            small = new GUIStyle(text) { fontSize = 13, normal = { textColor = new Color(0.65f, 0.7f, 0.75f) } };
            tracker = new GUIStyle(text) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.78f, 0.4f) } };
            button = new GUIStyle(GUI.skin.button) { fontSize = 14 };
        }
    }
}
