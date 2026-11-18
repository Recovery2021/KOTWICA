using Kotwica.Characters;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kotwica.UI
{
    /// <summary>
    /// Character creator (prototype, IMGUI): browse ready-made presets on a rotating platform,
    /// type a name, start the game. The choice is saved and used by every scene.
    /// </summary>
    public class CharacterCreatorScreen : MonoBehaviour
    {
        [SerializeField] private Transform previewPoint;
        [SerializeField] private string nextScene = "Prototype02_Combat";
        [SerializeField] private float turnSpeed = 18f;

        private CharacterRoster roster;
        private int index;
        private string playerName;
        private GameObject preview;
        private GUIStyle titleStyle, nameStyle, textStyle, buttonStyle, fieldStyle;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            roster = CharacterRoster.Load();
            playerName = CharacterSelection.PlayerName;
            int count = roster != null ? roster.PlayerPresets.Count : 0;
            index = count > 0 ? Mathf.Clamp(CharacterSelection.PresetIndex, 0, count - 1) : 0;
            ShowPreset();
        }

        private void Update()
        {
            if (previewPoint != null) previewPoint.Rotate(0f, turnSpeed * Time.deltaTime, 0f);
        }

        private void ShowPreset()
        {
            if (preview != null) Destroy(preview);
            CharacterPreset preset = roster != null ? roster.GetPlayerPreset(index) : null;
            if (preset == null || preset.Model == null || previewPoint == null) return;

            preview = Instantiate(preset.Model, previewPoint);
            preview.transform.localPosition = Vector3.zero;
            preview.transform.localRotation = Quaternion.identity;
            preview.transform.localScale = Vector3.one * preset.Scale;

            var animator = preview.GetComponent<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = roster.AnimatorController; // idle pose
                animator.applyRootMotion = false;
            }
        }

        private void Step(int direction)
        {
            int count = roster.PlayerPresets.Count;
            index = (index + direction + count) % count;
            ShowPreset();
        }

        private void OnGUI()
        {
            EnsureStyles();
            var panel = new Rect(40, 40, 420, Screen.height - 80);
            GUI.Box(panel, GUIContent.none);
            GUILayout.BeginArea(new Rect(panel.x + 24, panel.y + 24, panel.width - 48, panel.height - 48));

            GUILayout.Label("NOWA POSTAĆ", titleStyle);
            GUILayout.Label("Stacja Halden. Kim jesteś?", textStyle);
            GUILayout.Space(24);

            if (roster == null || roster.PlayerPresets.Count == 0)
            {
                GUILayout.Label("Brak wzorów postaci.\n\nW Unity: Kotwica → Postacie: przygotuj wzory i animacje.", textStyle);
                GUILayout.EndArea();
                return;
            }

            CharacterPreset preset = roster.GetPlayerPreset(index);
            GUILayout.Label($"Wzór {index + 1} z {roster.PlayerPresets.Count}", textStyle);
            GUILayout.Label(preset.DisplayName, nameStyle);
            if (!string.IsNullOrEmpty(preset.Description)) GUILayout.Label(preset.Description, textStyle);

            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<  Poprzedni", buttonStyle, GUILayout.Height(40))) Step(-1);
            if (GUILayout.Button("Następny  >", buttonStyle, GUILayout.Height(40))) Step(1);
            GUILayout.EndHorizontal();

            GUILayout.Space(24);
            GUILayout.Label("Imię", textStyle);
            playerName = GUILayout.TextField(playerName, 24, fieldStyle, GUILayout.Height(36));

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("ROZPOCZNIJ", buttonStyle, GUILayout.Height(52)))
            {
                CharacterSelection.PresetIndex = index;
                CharacterSelection.PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Kurier" : playerName.Trim();
                SceneManager.LoadScene(nextScene);
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            var light = new Color(0.85f, 0.9f, 0.95f);
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, normal = { textColor = light } };
            nameStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.75f, 0.35f) } };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, normal = { textColor = new Color(0.7f, 0.75f, 0.8f) } };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
            fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 18, padding = new RectOffset(10, 10, 8, 8) };
        }
    }
}
