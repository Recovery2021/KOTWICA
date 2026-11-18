using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kotwica.Characters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kotwica.EditorTools
{
    /// <summary>
    /// Turns downloaded Mixamo files into game-ready characters:
    ///   Art/Characters/Models/*.fbx      -> one player preset per model (character creator)
    ///   Art/Characters/Enemies/*.fbx     -> enemy preset (first file)
    ///   Art/Characters/Animations/*.fbx  -> shared Animator Controller (locomotion, crouch, aim, death)
    /// Also builds the character creator scene.
    /// </summary>
    public static class CharacterSetupBuilder
    {
        private const string ModelsFolder = "Assets/_Project/Art/Characters/Models";
        private const string EnemiesFolder = "Assets/_Project/Art/Characters/Enemies";
        private const string AnimationsFolder = "Assets/_Project/Art/Characters/Animations";
        private const string ControllerPath = "Assets/_Project/Art/Characters/Character.controller";
        private const string PresetFolder = "Assets/_Project/Characters/Presets";
        private const string RosterPath = "Assets/_Project/Resources/CharacterRoster.asset";
        private const string CreatorScenePath = "Assets/_Project/Scenes/CharacterCreator.unity";

        // Animation files the controller looks for (file name = clip name)
        private static readonly string[] ExpectedClips =
        {
            "Idle", "Walk", "Run", "CrouchIdle", "CrouchWalk",
            "AimIdle", "AimWalkForward", "AimWalkBack", "AimStrafeLeft", "AimStrafeRight", "Death"
        };

        [MenuItem("Kotwica/Postacie: przygotuj wzory i animacje")]
        public static void Setup()
        {
            foreach (string folder in new[] { ModelsFolder, EnemiesFolder, AnimationsFolder, PresetFolder, "Assets/_Project/Resources" })
                ArtPalette.EnsureFolder(folder);

            Dictionary<string, AnimationClip> clips = LoadClips();
            var report = new StringBuilder();

            if (!clips.ContainsKey("Idle"))
            {
                EditorUtility.DisplayDialog("Kotwica",
                    $"Brak animacji Idle.fbx w {AnimationsFolder}.\n\nPobierz animacje z Mixamo i zapisz je pod nazwami: {string.Join(", ", ExpectedClips)}.",
                    "OK");
                return;
            }

            AnimatorController controller = BuildController(clips, report);

            List<CharacterPreset> players = new List<CharacterPreset>();
            foreach (GameObject model in LoadModels(ModelsFolder)) players.Add(GetOrCreatePreset(model, model.name));
            CharacterPreset enemy = LoadModels(EnemiesFolder).Select(m => GetOrCreatePreset(m, "Wróg")).FirstOrDefault();

            var roster = AssetDatabase.LoadAssetAtPath<CharacterRoster>(RosterPath);
            if (roster == null)
            {
                roster = ScriptableObject.CreateInstance<CharacterRoster>();
                AssetDatabase.CreateAsset(roster, RosterPath);
            }
            roster.EditorSetup(players, enemy, controller);
            EditorUtility.SetDirty(roster);
            AssetDatabase.SaveAssets();

            report.Insert(0, $"Wzory gracza: {players.Count}\nWzór wroga: {(enemy != null ? "jest" : "BRAK (wrogowie zostaną kapsułami)")}\n\n");
            EditorUtility.DisplayDialog("Kotwica: postacie gotowe", report.ToString(), "OK");
            Selection.activeObject = roster;
        }

        // --- Animations ------------------------------------------------------------------

        private static Dictionary<string, AnimationClip> LoadClips()
        {
            var result = new Dictionary<string, AnimationClip>();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { AnimationsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (AnimationClip clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                {
                    if (clip.name.StartsWith("__preview__")) continue;
                    result[Path.GetFileNameWithoutExtension(path)] = clip;
                }
            }
            return result;
        }

        private static AnimatorController BuildController(Dictionary<string, AnimationClip> clips, StringBuilder report)
        {
            // Missing clips fall back to a similar one, so the game always runs
            AnimationClip Get(string name, string fallback)
            {
                if (clips.TryGetValue(name, out AnimationClip clip)) return clip;
                report.AppendLine($"Brak {name}.fbx, używam {fallback}");
                return clips.TryGetValue(fallback, out clip) ? clip : clips["Idle"];
            }

            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Crouch", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Aim", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            // Walking around: idle -> walk -> run by speed (m/s)
            AnimatorState locomotion = controller.CreateBlendTreeInController("Lokomocja", out BlendTree locoTree, 0);
            locoTree.blendType = BlendTreeType.Simple1D;
            locoTree.blendParameter = "Speed";
            locoTree.useAutomaticThresholds = false;
            locoTree.AddChild(Get("Idle", "Idle"), 0f);
            locoTree.AddChild(Get("Walk", "Idle"), 1.6f);
            locoTree.AddChild(Get("Run", "Walk"), 5f);

            // Crouching: idle -> sneak walk
            AnimatorState crouch = controller.CreateBlendTreeInController("Kucanie", out BlendTree crouchTree, 0);
            crouchTree.blendType = BlendTreeType.Simple1D;
            crouchTree.blendParameter = "Speed";
            crouchTree.useAutomaticThresholds = false;
            crouchTree.AddChild(Get("CrouchIdle", "Idle"), 0f);
            crouchTree.AddChild(Get("CrouchWalk", "Walk"), 1.8f);

            // Aiming: rifle up, strafing in all directions (body faces the camera)
            AnimatorState aim = controller.CreateBlendTreeInController("Celowanie", out BlendTree aimTree, 0);
            aimTree.blendType = BlendTreeType.SimpleDirectional2D;
            aimTree.blendParameter = "MoveX";
            aimTree.blendParameterY = "MoveY";
            aimTree.AddChild(Get("AimIdle", "Idle"), Vector2.zero);
            aimTree.AddChild(Get("AimWalkForward", "Walk"), new Vector2(0f, 1f));
            aimTree.AddChild(Get("AimWalkBack", "AimWalkForward"), new Vector2(0f, -1f));
            aimTree.AddChild(Get("AimStrafeLeft", "AimWalkForward"), new Vector2(-1f, 0f));
            aimTree.AddChild(Get("AimStrafeRight", "AimWalkForward"), new Vector2(1f, 0f));

            machine.defaultState = locomotion;

            Connect(locomotion, crouch, ("Crouch", true));
            Connect(crouch, locomotion, ("Crouch", false));
            Connect(locomotion, aim, ("Aim", true), ("Crouch", false));
            Connect(aim, locomotion, ("Aim", false));
            Connect(aim, crouch, ("Crouch", true));

            // Death from any state (once)
            AnimatorState death = machine.AddState("Śmierć");
            death.motion = Get("Death", "CrouchIdle");
            AnimatorStateTransition toDeath = machine.AddAnyStateTransition(death);
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            toDeath.hasExitTime = false;
            toDeath.duration = 0.1f;
            toDeath.canTransitionToSelf = false;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            if (report.Length == 0) report.AppendLine("Wszystkie animacje znalezione.");
            return controller;
        }

        private static void Connect(AnimatorState from, AnimatorState to, params (string parameter, bool value)[] conditions)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.15f;
            foreach ((string parameter, bool value) in conditions)
                transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        // --- Presets ------------------------------------------------------------------------

        private static IEnumerable<GameObject> LoadModels(string folder) =>
            AssetDatabase.FindAssets("t:Model", new[] { folder })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .OrderBy(p => p)
                .Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path))
                .Where(m => m != null);

        private static CharacterPreset GetOrCreatePreset(GameObject model, string displayName)
        {
            string path = $"{PresetFolder}/Preset_{model.name}.asset";
            var preset = AssetDatabase.LoadAssetAtPath<CharacterPreset>(path);
            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<CharacterPreset>();
                preset.EditorSetup(displayName.Replace('_', ' '), model);
                AssetDatabase.CreateAsset(preset, path);
            }
            else
            {
                // Keep a name the user may have edited; only refresh the model
                preset.EditorSetup(preset.DisplayName, model);
                EditorUtility.SetDirty(preset);
            }
            return preset;
        }

        // --- Creator scene ---------------------------------------------------------------

        [MenuItem("Kotwica/Zbuduj scenę kreatora postaci")]
        public static void BuildCreatorScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Dictionary<string, Material> palette = ArtPalette.LoadOrCreate();

            // Dark service bay: a round platform lit like a medical scanner
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "Platforma";
            platform.transform.localScale = new Vector3(2.2f, 0.05f, 2.2f);
            platform.GetComponent<Renderer>().sharedMaterial = palette["M_DarkMetal"];

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Pierścień świetlny";
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.localScale = new Vector3(2.3f, 0.02f, 2.3f);
            ring.transform.position = new Vector3(0f, -0.01f, 0f);
            ring.GetComponent<Renderer>().sharedMaterial = palette["M_LightCold"];

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Podłoga";
            floor.transform.position = new Vector3(0f, -0.06f, 0f);
            floor.transform.localScale = Vector3.one * 3f;
            floor.GetComponent<Renderer>().sharedMaterial = palette["M_Rubber"];

            var preview = new GameObject("Podgląd postaci").transform;
            preview.position = new Vector3(0f, 0.05f, 0f);
            preview.rotation = Quaternion.Euler(0f, 180f, 0f); // facing the camera

            AddLight("Światło główne", new Vector3(1.5f, 3.2f, 2.2f), new Vector3(0f, 1.1f, 0f), new Color(0.82f, 0.9f, 1f), 18f, LightType.Spot);
            AddLight("Kontra", new Vector3(-1.6f, 2.4f, -1.8f), new Vector3(0f, 1.3f, 0f), new Color(1f, 0.6f, 0.25f), 10f, LightType.Spot);
            AddLight("Wypełnienie", new Vector3(-1.5f, 1f, 2f), new Vector3(0f, 1f, 0f), new Color(0.4f, 0.45f, 0.55f), 0.6f, LightType.Point);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.03f, 0.035f, 0.04f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.06f;
            RenderSettings.fogColor = new Color(0.02f, 0.025f, 0.03f);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.fieldOfView = 35f;
            cameraObject.transform.position = new Vector3(0.6f, 1.35f, 3.6f);
            cameraObject.transform.LookAt(new Vector3(0.6f, 1.0f, 0f));
            cameraObject.AddComponent<AudioListener>();

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/PP_StationInterior.asset");
            if (profile != null)
            {
                var data = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(camera);
                data.renderPostProcessing = true;
                var volume = new GameObject("Post-processing").AddComponent<Volume>();
                volume.isGlobal = true;
                volume.sharedProfile = profile;
            }

            var screen = new GameObject("Kreator").AddComponent<Kotwica.UI.CharacterCreatorScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("previewPoint").objectReferenceValue = preview;
            so.ApplyModifiedPropertiesWithoutUndo();

            ArtPalette.EnsureFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, CreatorScenePath);

            // The creator is the first scene of the game
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == CreatorScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(CreatorScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log("[Kotwica] Zbudowano kreator postaci. Play: wybierz wzór, wpisz imię, Rozpocznij.");
        }

        private static void AddLight(string lightName, Vector3 position, Vector3 lookAt, Color color, float intensity, LightType type)
        {
            var go = new GameObject(lightName);
            go.transform.position = position;
            go.transform.LookAt(lookAt);
            var light = go.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = 8f;
            light.spotAngle = 45f;
            light.shadows = type == LightType.Spot ? LightShadows.Soft : LightShadows.None;
        }
    }
}
