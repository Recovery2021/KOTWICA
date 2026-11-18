using System.Collections.Generic;
using Kotwica.Environment;
using Kotwica.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Kotwica.EditorTools
{
    /// <summary>
    /// Art direction test: builds a 16 m station corridor from the Blender kit
    /// (Assets/_Project/Art/Models/Kit_Corridor), with moody lights, fog, reflections and post-processing,
    /// plus a walkable player so the space can be judged at human scale.
    /// Menu: Kotwica > Zbuduj scenę testu klimatu.
    /// </summary>
    public static class ArtTestSceneBuilder
    {
        private const string KitFolder = "Assets/_Project/Art/Models/Kit_Corridor";
        private const string ScenePath = "Assets/_Project/Scenes/ArtTest_Corridor.unity";
        private const string ProfilePath = "Assets/_Project/Settings/PP_StationInterior.asset";
        private const int Modules = 8;               // 8 x 2 m = 16 m corridor
        private const float ModuleLength = 2f;

        private static Dictionary<string, GameObject> kit;
        private static Dictionary<string, Material> palette;
        private static Quaternion axisFix = Quaternion.identity;
        private static float scaleFix = 1f;
        private static Transform root;

        [MenuItem("Kotwica/Zbuduj scenę testu klimatu")]
        public static void Build()
        {
            if (!BuildBase(out Scene scene, out _, out _)) return;
            SaveScene(scene, ScenePath);
            Debug.Log($"[Kotwica] Zbudowano {ScenePath}. Naciśnij Play i przejdź się korytarzem.");
        }

        /// <summary>
        /// Builds the lit corridor with a walkable player and camera into a new scene.
        /// Used by this test and by the combat prototype, which adds its own layer on top.
        /// </summary>
        internal static bool BuildBase(out Scene scene, out GameObject player, out Camera camera)
        {
            scene = default;
            player = null;
            camera = null;
            if (!LoadKit()) return false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            palette = ArtPalette.LoadOrCreate();
            DetectAxisAndScale();

            root = new GameObject("Korytarz").transform;
            BuildCorridor();
            BuildLights();
            BuildAtmosphere();
            camera = BuildPlayerAndCamera(out player);
            BuildPostProcessing(camera);
            return true;
        }

        internal static void SaveScene(Scene scene, string path)
        {
            ArtPalette.EnsureFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, path);

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        /// <summary>Corridor root and palette, for builders that extend the base scene.</summary>
        internal static Transform Root => root;
        internal static Dictionary<string, Material> Palette => palette;

        // --- Kit loading ---------------------------------------------------------

        private static bool LoadKit()
        {
            kit = new Dictionary<string, GameObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { KitFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model != null) kit[model.name] = model;
            }

            if (!kit.ContainsKey("KIT_Floor_2m"))
            {
                EditorUtility.DisplayDialog("Kotwica",
                    "Brak modeli w " + KitFolder + ".\n\nNajpierw uruchom w Blenderze skrypt Blender/Scripts/kotwica_kit_corridor.py.",
                    "OK");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Blender to Unity axis conventions are easy to get wrong, so we measure the floor piece:
        /// it must lie flat, be 3.4 m wide and extend towards +Z. Everything else gets the same fix.
        /// </summary>
        private static void DetectAxisAndScale()
        {
            var probe = (GameObject)PrefabUtility.InstantiatePrefab(kit["KIT_Floor_2m"]);
            Bounds bounds = WorldBounds(probe);
            Object.DestroyImmediate(probe);

            scaleFix = bounds.size.x > 0.01f ? 3.4f / bounds.size.x : 1f;
            axisFix = bounds.center.z < 0f ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;

            if (bounds.size.y > bounds.size.z)
                Debug.LogError($"[Kotwica] Podłoga stoi pionowo ({bounds.size}). Sprawdź ustawienia osi eksportu FBX.");
            if (Mathf.Abs(scaleFix - 1f) > 0.01f)
                Debug.LogWarning($"[Kotwica] Skala modeli różna od 1 (poprawka x{scaleFix:0.###}). Sprawdź eksport FBX.");
            if (axisFix != Quaternion.identity)
                Debug.LogWarning("[Kotwica] Modele są obrócone o 180°. Poprawiam automatycznie.");
        }

        private static Bounds WorldBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            Bounds b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(go.transform.position, Vector3.zero);
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Places one kit piece. Position and yaw are in corridor space (Unity metres).</summary>
        internal static GameObject Place(string piece, Vector3 position, float yaw = 0f)
        {
            if (!kit.TryGetValue(piece, out GameObject model))
            {
                Debug.LogWarning($"[Kotwica] Brak elementu {piece} w zestawie.");
                return null;
            }

            var holder = new GameObject(piece).transform;
            holder.SetParent(root, false);
            holder.localPosition = position;
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, holder);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = axisFix;
            instance.transform.localScale = Vector3.one * scaleFix;

            ArtPalette.Apply(instance, palette);
            foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.GetComponent<Collider>() == null) filter.gameObject.AddComponent<MeshCollider>();
                GameObjectUtility.SetStaticEditorFlags(filter.gameObject,
                    StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
            }
            return holder.gameObject;
        }

        // --- Layout ----------------------------------------------------------------

        private static readonly HashSet<int> RightPipes = new HashSet<int> { 2, 3, 6 };
        private static readonly HashSet<int> LeftPipes = new HashSet<int> { 4, 5 };
        private static readonly HashSet<int> CeilingLights = new HashSet<int> { 1, 4, 6 };

        private static void BuildCorridor()
        {
            for (int i = 0; i < Modules; i++)
            {
                float z = i * ModuleLength;
                Place("KIT_Floor_2m", new Vector3(0f, 0f, z));
                Place(RightPipes.Contains(i) ? "KIT_Wall_2m_Pipes" : "KIT_Wall_2m", new Vector3(0f, 0f, z));
                // Left wall = right wall turned around; turning also flips its Z range, hence z + 2
                Place(LeftPipes.Contains(i) ? "KIT_Wall_2m_Pipes" : "KIT_Wall_2m", new Vector3(0f, 0f, z + ModuleLength), 180f);
                Place(CeilingLights.Contains(i) ? "KIT_Ceiling_2m_Light" : "KIT_Ceiling_2m_Plain", new Vector3(0f, 0f, z));
            }

            // Structure rhythm: bulkhead every 4 m, closed doors at both ends
            foreach (float z in new[] { 4f, 8f, 12f }) Place("KIT_Bulkhead", new Vector3(0f, 0f, z));
            Place("KIT_Door_Frame", new Vector3(0f, 0f, 0f));
            Place("KIT_Door_Panel", new Vector3(0f, 0f, 0f));
            float end = Modules * ModuleLength;
            Place("KIT_Door_Frame", new Vector3(0f, 0f, end), 180f);
            Place("KIT_Door_Panel", new Vector3(0f, 0f, end), 180f);

            // Props: details that say "people live and work here"
            Place("KIT_Console", new Vector3(1.42f, 0f, 5f), -90f);
            Place("KIT_WallLamp", new Vector3(1.42f, 2.1f, 1.5f), -90f);
            Place("KIT_WallLamp", new Vector3(-1.42f, 2.1f, 14.5f), 90f);
            Place("KIT_Pipe_Straight_2m", new Vector3(-1.05f, 0.2f, 9f));
            Place("KIT_Pipe_Elbow", new Vector3(-1.05f, 0.2f, 11f));
            Place("KIT_Crate_1m", new Vector3(-0.85f, 0f, 13.2f), 8f);
            Place("KIT_Crate_1m", new Vector3(-0.8f, 1f, 13.25f), -6f);
            Place("KIT_Crate_1m", new Vector3(0.85f, 0f, 6.8f), 25f);
        }

        // --- Light and air ----------------------------------------------------------

        private static void BuildLights()
        {
            var lights = new GameObject("Światła").transform;
            lights.SetParent(root, false);

            // Cold ceiling panels shining down; one of them is faulty and flickers
            foreach (int i in CeilingLights)
            {
                Light spot = AddLight(lights, $"Sufit_{i}", new Vector3(0f, 2.85f, i * ModuleLength + 1f), LightType.Spot,
                    new Color(0.82f, 0.9f, 1f), 6f, 7f, true);
                spot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                spot.spotAngle = 130f;
                spot.innerSpotAngle = 60f;
                if (i == 6) Pulse(spot, PulsingLight.Pattern.Flicker, 0f, 6f, 0.4f);
            }

            // Warm accents next to doors and lamps
            AddLight(lights, "Lampa_P", new Vector3(1.2f, 2.1f, 1.5f), LightType.Point, new Color(1f, 0.6f, 0.25f), 1.5f, 3.5f, false);
            AddLight(lights, "Lampa_L", new Vector3(-1.2f, 2.1f, 14.5f), LightType.Point, new Color(1f, 0.6f, 0.25f), 1.5f, 3.5f, false);
            AddLight(lights, "Drzwi_Start", new Vector3(0f, 2.65f, 0.4f), LightType.Point, new Color(1f, 0.6f, 0.25f), 0.8f, 2.5f, false);
            AddLight(lights, "Drzwi_Koniec", new Vector3(0f, 2.65f, Modules * ModuleLength - 0.4f), LightType.Point, new Color(1f, 0.6f, 0.25f), 0.8f, 2.5f, false);

            // Red emergency pulse in the middle: the "something is wrong" note
            Light red = AddLight(lights, "Awaryjne", new Vector3(0f, 2.35f, 8f), LightType.Point, new Color(1f, 0.15f, 0.08f), 2f, 6f, false);
            Pulse(red, PulsingLight.Pattern.Pulse, 0.2f, 2.5f, 0.5f);

            // Faint screen glow from the console
            AddLight(lights, "Konsola", new Vector3(1.0f, 1.35f, 5f), LightType.Point, new Color(0.3f, 1f, 0.55f), 0.5f, 1.5f, false);

            // Real-time reflection probes: without them metal looks flat black
            for (int k = 0; k < 2; k++)
            {
                var probeObject = new GameObject($"Odbicia_{k}");
                probeObject.transform.SetParent(root, false);
                probeObject.transform.localPosition = new Vector3(0f, 1.6f, 4f + k * 8f);
                var probe = probeObject.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                probe.boxProjection = true;
                probe.size = new Vector3(3.4f, 3.4f, 8.2f);
                probe.resolution = 128;
            }
        }

        private static Light AddLight(Transform parent, string lightName, Vector3 position, LightType type, Color color,
            float intensity, float range, bool shadows)
        {
            var go = new GameObject(lightName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var light = go.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return light;
        }

        private static void Pulse(Light light, PulsingLight.Pattern pattern, float min, float max, float speed)
        {
            var pulse = light.gameObject.AddComponent<PulsingLight>();
            var so = new SerializedObject(pulse);
            so.FindProperty("pattern").enumValueIndex = (int)pattern;
            so.FindProperty("minIntensity").floatValue = min;
            so.FindProperty("maxIntensity").floatValue = max;
            so.FindProperty("speed").floatValue = speed;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildAtmosphere()
        {
            // Dusty air: dark blue-grey fog swallows the far end of the corridor
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.035f;
            RenderSettings.fogColor = new Color(0.02f, 0.025f, 0.03f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.025f, 0.03f, 0.035f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        }

        // --- Player, camera, post-processing -------------------------------------------

        private static Camera BuildPlayerAndCamera(out GameObject player)
        {
            player = new GameObject("Gracz");
            var controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.35f;
            player.transform.position = new Vector3(0f, 0.05f, 2f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Ciało (placeholder)";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            body.GetComponent<Renderer>().sharedMaterial = palette["M_Pipe"];

            var playerController = player.AddComponent<PlayerController>();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 200f;
            camera.fieldOfView = 65f;
            cameraObject.AddComponent<AudioListener>();
            var rig = cameraObject.AddComponent<CameraRig>();
            cameraObject.transform.position = new Vector3(0f, 1.8f, -1f);

            var rigSo = new SerializedObject(rig);
            rigSo.FindProperty("target").objectReferenceValue = player.transform;
            rigSo.FindProperty("footDistance").floatValue = 2.6f;   // closer camera for tight corridors
            rigSo.ApplyModifiedPropertiesWithoutUndo();

            var playerSo = new SerializedObject(playerController);
            playerSo.FindProperty("cameraTransform").objectReferenceValue = camera.transform;
            playerSo.FindProperty("visual").objectReferenceValue = body.transform;
            playerSo.ApplyModifiedPropertiesWithoutUndo();

            return camera;
        }

        private static void BuildPostProcessing(Camera camera)
        {
            // Camera must opt in to post-processing in URP
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                ArtPalette.EnsureFolder("Assets/_Project/Settings");
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            // Glow around lamps, filmic contrast, muted colours, dark edges, a bit of grain
            Bloom bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.65f);

            Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.Override(0.4f);
            color.contrast.Override(18f);
            color.saturation.Override(-20f);

            Vignette vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.4f);

            FilmGrain grain = GetOrAdd<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.18f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var volumeObject = new GameObject("Post-processing (globalny)");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>(true);
                component.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            return component;
        }
    }
}
