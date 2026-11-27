using System.Collections.Generic;
using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Dialogue
{
    /// <summary>
    /// Shows one conversation at a time (IMGUI panel at the bottom of the screen).
    /// Choices are picked with the number keys or a click; attribute checks the player cannot pass
    /// stay visible but locked, so you know what a better build would open. Gameplay input is blocked meanwhile.
    /// Created automatically next to GameServices.
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        public static DialogueRunner Instance { get; private set; }

        [Tooltip("The talk ends when the player is pushed further than this from the speaker.")]
        [SerializeField] private float maxDistance = 6f;

        private DialogueDefinition dialogue;
        private DialogueNode node;
        private Transform speakerTransform;
        private Transform playerTransform;
        private readonly List<DialogueChoice> visible = new List<DialogueChoice>();
        private int openedFrame;
        private bool relockPending;
        private GUIStyle box, nameStyle, textStyle, choiceStyle, hintStyle;

        public static bool IsOpen => Instance != null && Instance.dialogue != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake() => Instance = this;

        /// <summary>Starts a conversation; false when the dialogue id is unknown or no entry fits the current state.</summary>
        public static bool Open(string dialogueId, Transform speaker, Transform player)
        {
            if (Instance == null || IsOpen) return false;
            DialogueDefinition def = DialogueDefinition.Find(dialogueId);
            if (def == null)
            {
                Debug.LogWarning($"[Kotwica] Brak rozmowy '{dialogueId}' w plikach StreamingAssets/Kotwica (menu Kotwica > Zadania: sprawdź pliki).");
                return false;
            }

            DialogueNode first = null;
            foreach (DialogueEntry entry in def.entries)
            {
                if (!DialogueRules.Pass(entry.conditions)) continue;
                first = def.Node(entry.node);
                break;
            }
            if (first == null) return false;

            Instance.Begin(def, first, speaker, player);
            return true;
        }

        private void Begin(DialogueDefinition def, DialogueNode first, Transform speaker, Transform player)
        {
            dialogue = def;
            speakerTransform = speaker;
            playerTransform = player;
            openedFrame = Time.frameCount;
            relockPending = false;
            GameInput.InDialogue = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Show(first);
        }

        private void Show(DialogueNode next)
        {
            node = next;
            visible.Clear();
            foreach (DialogueChoice choice in next.choices)
                if (DialogueRules.Pass(choice.conditions)) visible.Add(choice);
        }

        private void Choose(int index)
        {
            if (index < 0 || index >= visible.Count) return;
            DialogueChoice choice = visible[index];
            if (!DialogueRules.CheckPassed(choice)) return;

            Vector3 position = playerTransform != null ? playerTransform.position : Vector3.zero;
            DialogueRules.Apply(choice, position);

            DialogueNode next = string.IsNullOrEmpty(choice.next) ? null : dialogue.Node(choice.next);
            if (next == null) Close();
            else Show(next);
        }

        public void Close()
        {
            dialogue = null;
            node = null;
            GameInput.InDialogue = false;
            GameInput.BlockUntilNextFrame(); // the key that ended the talk must not start it again
            relockPending = true;
        }

        private void Update()
        {
            // Lock the mouse again only after the click that closed the talk is released (no stray shot)
            if (relockPending && !IsOpen && !GameInput.Blocked && !GameInput.LeftMouseHeld())
            {
                relockPending = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (!IsOpen || Time.frameCount == openedFrame) return;

            bool tooFar = speakerTransform != null && playerTransform != null
                          && Vector3.Distance(speakerTransform.position, playerTransform.position) > maxDistance;
            if (GameInput.CancelPressed() || tooFar || (playerTransform != null && !playerTransform.gameObject.activeInHierarchy))
            {
                Close();
                return;
            }

            int number = GameInput.NumberPressed();
            if (number > 0) Choose(number - 1);
            else if (GameInput.ConfirmPressed())
            {
                // F/Enter: continue when there is nothing to decide
                if (visible.Count == 0) Close();
                else if (visible.Count == 1) Choose(0);
            }
        }

        private void OnGUI()
        {
            if (!IsOpen || node == null) return;
            EnsureStyles();

            float width = Mathf.Min(900f, Screen.width - 40f);
            float x = (Screen.width - width) * 0.5f;
            float textHeight = textStyle.CalcHeight(new GUIContent(node.text), width - 32f);
            float choicesHeight = Mathf.Max(1, visible.Count) * 34f;
            float height = 24f + 30f + textHeight + 12f + choicesHeight + 26f;
            float y = Screen.height - height - 20f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none, box);
            float cx = x + 16f;
            float cy = y + 12f;

            string speaker = string.IsNullOrEmpty(node.speaker) ? dialogue.speaker : node.speaker;
            GUI.Label(new Rect(cx, cy, width - 32f, 28f), speaker, nameStyle);
            cy += 30f;
            GUI.Label(new Rect(cx, cy, width - 32f, textHeight), node.text, textStyle);
            cy += textHeight + 12f;

            if (visible.Count == 0)
            {
                if (GUI.Button(new Rect(cx, cy, width - 32f, 30f), "[F] Dalej", choiceStyle)) Close();
            }
            for (int i = 0; i < visible.Count; i++)
            {
                DialogueChoice choice = visible[i];
                bool can = DialogueRules.CheckPassed(choice);
                string label = $"{i + 1}. ";
                if (choice.check)
                {
                    string attr = Kotwica.Progression.PlayerProfile.DisplayName(choice.attribute);
                    int have = GameServices.Instance != null ? GameServices.Instance.Profile.Get(choice.attribute) : 0;
                    label += can ? $"[{attr} {choice.difficulty}] " : $"[{attr} {choice.difficulty}, masz {have}] ";
                }
                label += choice.text;

                GUI.enabled = can;
                if (GUI.Button(new Rect(cx, cy + i * 34f, width - 32f, 30f), label, choiceStyle)) Choose(i);
                GUI.enabled = true;
            }

            GUI.Label(new Rect(cx, y + height - 24f, width - 32f, 20f), "1-9 wybór · Esc koniec rozmowy", hintStyle);
        }

        private void EnsureStyles()
        {
            if (box != null) return;
            var background = new Texture2D(1, 1);
            background.SetPixel(0, 0, new Color(0.03f, 0.035f, 0.05f, 0.92f));
            background.Apply();
            box = new GUIStyle(GUI.skin.box) { normal = { background = background } };
            nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.78f, 0.45f) } };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, normal = { textColor = new Color(0.9f, 0.92f, 0.95f) } };
            choiceStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 10, 4, 4) };
            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(0.6f, 0.62f, 0.66f) } };
        }
    }
}
