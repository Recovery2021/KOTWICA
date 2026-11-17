using System.Collections.Generic;
using Kotwica.Gameplay;
using Kotwica.Orbits;
using Kotwica.Player;
using Kotwica.Ship;
using Kotwica.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kotwica.EditorTools
{
    /// <summary>
    /// Builds the graybox scene for prototype 01 from code (menu: Kotwica > Zbuduj scenę Prototyp 01).
    /// Why code instead of a hand-made scene: easy to rebuild after changes and easy to review in Git.
    /// Contents: gas giant, two orbiting habitat rings with decks, docking ports, job board,
    /// delivery terminals, the player's ship, the player and the camera.
    /// </summary>
    public static class Prototype01SceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype01.unity";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Graybox";

        // Ring geometry (metres)
        private const float RingRadius = 60f;
        private const float DeckTop = 13f;      // walkable surface height above the ring centre
        private const float DeckSize = 32f;

        /// <summary>Shared graybox materials (muted greys + a few warm/cold accents for mood).</summary>
        private class Palette
        {
            public Material Hull, Deck, Rail, WindowStrip, Pad, Board, Terminal, Planet, ShipHull, Glass, Engine, Player;
        }

        [MenuItem("Kotwica/Zbuduj scenę Prototyp 01")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Palette palette = CreatePalette();

            SetupLighting();

            // Systems: orbital time, moving reference frame, HUD
            var systems = new GameObject("_Systemy");
            systems.AddComponent<OrbitalClock>();
            var frame = systems.AddComponent<StationFrame>();
            var hud = systems.AddComponent<PrototypeHud>();

            // Gas giant (visual only, far below the rings)
            var planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = "Morwen (gazowy olbrzym)";
            Object.DestroyImmediate(planet.GetComponent<Collider>());
            planet.transform.localScale = Vector3.one * 600f;
            planet.GetComponent<Renderer>().sharedMaterial = palette.Planet;

            // Two rings on different orbits -> their distance changes over time (160 m to 1.4 km)
            OrbitalRing ringA = BuildRing("Pierścień Kesh", 600f, 300f, 0f, 50f, palette, out DockingPort portA);
            OrbitalRing ringB = BuildRing("Pierścień Varo", 760f, 430f, 25f, 70f, palette, out DockingPort portB);

            // Edit-mode preview of positions (at runtime StationFrame recalculates them every physics step)
            Vector3 anchorOrbital = ringA.GetOrbitalPosition(0d);
            ringA.transform.position = Vector3.zero;
            ringB.transform.position = ringB.GetOrbitalPosition(0d) - anchorOrbital;
            planet.transform.position = -anchorOrbital;

            // Gameplay objects on the decks: each board sends you to the other ring
            DeliveryTerminal terminalA = CreateTerminal(ringA, portA, palette, new Vector3(9f, DeckTop, -9f));
            DeliveryTerminal terminalB = CreateTerminal(ringB, portB, palette, new Vector3(9f, DeckTop, -9f));
            CreateJobBoard(ringA, terminalB, 120, palette, new Vector3(-9f, DeckTop, -9f));
            CreateJobBoard(ringB, terminalA, 150, palette, new Vector3(-9f, DeckTop, -9f));

            // Ship, player and camera
            ShipController ship = BuildShip(palette, portA, out Transform exitPoint);
            PlayerController player = BuildPlayer(palette, new Vector3(-5f, DeckTop + 0.05f, -4f));
            Camera camera = BuildCamera(player.transform);
            var rig = camera.GetComponent<CameraRig>();

            // Wire references (private [SerializeField] fields)
            Set(frame, "anchor", ringA);
            Set(frame, "planet", planet.transform);

            Set(ship, "startDockedAt", portA);
            Set(ship, "cameraRig", rig);
            Set(ship, "player", player);
            Set(ship, "exitPoint", exitPoint);

            Set(player, "cameraTransform", camera.transform);

            Set(hud, "interactor", player.GetComponent<PlayerInteractor>());
            Set(hud, "ship", ship);
            Set(hud, "viewCamera", camera);

            EnsureFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = player.gameObject;
            Debug.Log($"[Kotwica] Zbudowano scenę {ScenePath}. Naciśnij Play.");
        }

        // --- World ---------------------------------------------------------------

        private static void SetupLighting()
        {
            // Distant star: warm, low angle, hard shadows = moody contrast
            var sun = new GameObject("Słońce");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.9f, 0.78f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(25f, -60f, 0f);

            // Black space, very dim cold fill light
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.07f, 0.09f);
        }

        private static OrbitalRing BuildRing(string ringName, float orbitRadius, float period, float phase, float spinPeriod,
            Palette palette, out DockingPort port)
        {
            var root = new GameObject(ringName);
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var ring = root.AddComponent<OrbitalRing>();

            // Spinning habitat: torus made of segments, spokes and the hub
            var spinPart = new GameObject("Habitat (obrotowy)").transform;
            spinPart.SetParent(root.transform, false);

            const int segments = 24;
            float segmentLength = 2f * Mathf.PI * RingRadius / segments * 1.04f;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Quaternion rotation = Quaternion.LookRotation(tangent, Vector3.up);

                Cube(spinPart, $"Segment_{i:00}", outward * RingRadius, rotation, new Vector3(8f, 8f, segmentLength), palette.Hull);
                // Thin lit window strip on the outside: life inside the ring
                Cube(spinPart, $"Okna_{i:00}", outward * (RingRadius + 4.05f), rotation, new Vector3(0.1f, 1.2f, segmentLength * 0.7f), palette.WindowStrip, false);
            }

            for (int k = 0; k < 4; k++)
            {
                float angle = k * Mathf.PI * 0.5f;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                const float hubRadius = 6f;
                float spokeLength = RingRadius - 4f - hubRadius;
                Cube(spinPart, $"Szprycha_{k}", outward * (hubRadius + spokeLength * 0.5f), Quaternion.LookRotation(outward),
                    new Vector3(2.5f, 2.5f, spokeLength), palette.Hull);
            }

            var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder); // radius 6, height 24
            hub.name = "Piasta";
            hub.transform.SetParent(spinPart, false);
            hub.transform.localScale = new Vector3(12f, 12f, 12f);
            hub.GetComponent<Renderer>().sharedMaterial = palette.Hull;

            // Non-spinning dock deck on top of the hub (normal gravity, walkable)
            Cube(root.transform, "Pokład", new Vector3(0f, DeckTop - 0.5f, 0f), Quaternion.identity, new Vector3(DeckSize, 1f, DeckSize), palette.Deck);
            float half = DeckSize * 0.5f;
            Cube(root.transform, "Barierka_N", new Vector3(0f, DeckTop + 0.55f, half), Quaternion.identity, new Vector3(DeckSize, 1.1f, 0.3f), palette.Rail);
            Cube(root.transform, "Barierka_S", new Vector3(0f, DeckTop + 0.55f, -half), Quaternion.identity, new Vector3(DeckSize, 1.1f, 0.3f), palette.Rail);
            Cube(root.transform, "Barierka_E", new Vector3(half, DeckTop + 0.55f, 0f), Quaternion.identity, new Vector3(0.3f, 1.1f, DeckSize), palette.Rail);
            Cube(root.transform, "Barierka_W", new Vector3(-half, DeckTop + 0.55f, 0f), Quaternion.identity, new Vector3(0.3f, 1.1f, DeckSize), palette.Rail);
            Cube(root.transform, "Lądowisko", new Vector3(0f, DeckTop + 0.02f, 5f), Quaternion.identity, new Vector3(11f, 0.04f, 13f), palette.Pad, false);

            // Docking trigger above the pad + exact parking point for the ship
            var portObject = new GameObject("PortDokujący");
            portObject.transform.SetParent(root.transform, false);
            portObject.transform.localPosition = new Vector3(0f, DeckTop, 5f);
            var trigger = portObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 5f, 0f);
            trigger.size = new Vector3(16f, 10f, 18f);

            var dockPoint = new GameObject("PunktDokowania").transform;
            dockPoint.SetParent(portObject.transform, false);
            dockPoint.localPosition = new Vector3(0f, 1.1f, 0f); // ship hull is 2 m tall

            port = portObject.AddComponent<DockingPort>();
            Set(port, "ring", ring);
            Set(port, "dockPoint", dockPoint);

            // Cold deck lamp: small pool of light in the dark
            var lamp = new GameObject("Lampa pokładu");
            lamp.transform.SetParent(root.transform, false);
            lamp.transform.localPosition = new Vector3(0f, DeckTop + 8f, -4f);
            var lampLight = lamp.AddComponent<Light>();
            lampLight.type = LightType.Point;
            lampLight.color = new Color(0.75f, 0.85f, 1f);
            lampLight.range = 30f;
            lampLight.intensity = 3f;

            SetString(ring, "displayName", ringName);
            SetFloat(ring, "orbitRadius", orbitRadius);
            SetFloat(ring, "orbitalPeriod", period);
            SetFloat(ring, "phaseDegrees", phase);
            SetFloat(ring, "spinPeriod", spinPeriod);
            Set(ring, "spinPart", spinPart);

            return ring;
        }

        private static DeliveryTerminal CreateTerminal(OrbitalRing ring, DockingPort port, Palette palette, Vector3 deckPosition)
        {
            GameObject go = Cube(ring.transform, "TerminalDostaw", deckPosition + Vector3.up * 0.75f, Quaternion.identity,
                new Vector3(1.2f, 1.5f, 1.2f), palette.Terminal);
            var terminal = go.AddComponent<DeliveryTerminal>();
            Set(terminal, "ring", ring);
            Set(terminal, "port", port);
            return terminal;
        }

        private static void CreateJobBoard(OrbitalRing ring, DeliveryTerminal destination, int reward, Palette palette, Vector3 deckPosition)
        {
            GameObject go = Cube(ring.transform, "TablicaZleceń", deckPosition + Vector3.up * 1.4f, Quaternion.identity,
                new Vector3(2.4f, 2.2f, 0.2f), palette.Board);
            var board = go.AddComponent<JobBoard>();
            Set(board, "destination", destination);
            SetInt(board, "reward", reward);
        }

        private static ShipController BuildShip(Palette palette, DockingPort startPort, out Transform exitPoint)
        {
            var root = new GameObject("Statek gracza – Wróbel");
            var body = root.AddComponent<Rigidbody>();
            body.mass = 2000f;
            body.useGravity = false;
            body.isKinematic = true; // starts docked

            Cube(root.transform, "Kadłub", Vector3.zero, Quaternion.identity, new Vector3(4f, 2f, 8f), palette.ShipHull);
            Cube(root.transform, "Kokpit", new Vector3(0f, 1.1f, 2.2f), Quaternion.identity, new Vector3(2.4f, 1f, 2.4f), palette.Glass);
            Cube(root.transform, "Silnik_L", new Vector3(-1.4f, 0f, -4.5f), Quaternion.identity, new Vector3(0.9f, 0.9f, 1.2f), palette.Engine, false);
            Cube(root.transform, "Silnik_P", new Vector3(1.4f, 0f, -4.5f), Quaternion.identity, new Vector3(0.9f, 0.9f, 1.2f), palette.Engine, false);

            exitPoint = new GameObject("PunktWyjścia").transform;
            exitPoint.SetParent(root.transform, false);
            exitPoint.localPosition = new Vector3(-4.5f, -1.05f, 0f); // next to the hull, feet on the deck

            root.AddComponent<FrameObject>();
            var ship = root.AddComponent<ShipController>();
            root.AddComponent<ShipBoarding>();

            root.transform.SetPositionAndRotation(startPort.DockPoint.position, startPort.DockPoint.rotation);
            return ship;
        }

        private static PlayerController BuildPlayer(Palette palette, Vector3 position)
        {
            var root = new GameObject("Gracz");
            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f); // pivot at the feet
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.stepOffset = 0.4f;

            // Placeholder body until the character creator exists
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Ciało (placeholder)";
            Object.DestroyImmediate(capsule.GetComponent<Collider>());
            capsule.transform.SetParent(root.transform, false);
            capsule.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            capsule.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            capsule.GetComponent<Renderer>().sharedMaterial = palette.Player;

            // Visor shows which way the character faces
            Cube(root.transform, "Wizjer", new Vector3(0f, 1.55f, 0.3f), Quaternion.identity, new Vector3(0.4f, 0.15f, 0.15f), palette.Glass, false);

            var player = root.AddComponent<PlayerController>();
            root.AddComponent<PlayerInteractor>();
            root.AddComponent<FrameObject>();
            root.transform.position = position;
            return player;
        }

        private static Camera BuildCamera(Transform followTarget)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.005f, 0.006f, 0.01f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 20000f;
            go.AddComponent<AudioListener>();
            var rig = go.AddComponent<CameraRig>();
            go.AddComponent<FrameObject>();
            Set(rig, "target", followTarget);
            go.transform.position = followTarget.position + new Vector3(0f, 3f, -5f);
            return camera;
        }

        // --- Materials -----------------------------------------------------------

        private static Palette CreatePalette()
        {
            EnsureFolder(MaterialFolder);
            return new Palette
            {
                Hull = Mat("GB_Kadlub", new Color(0.32f, 0.33f, 0.35f), Color.black, 0.35f, 0.6f),
                Deck = Mat("GB_Poklad", new Color(0.22f, 0.22f, 0.23f), Color.black, 0.25f, 0.4f),
                Rail = Mat("GB_Barierka", new Color(0.45f, 0.4f, 0.3f), Color.black, 0.3f, 0.5f),
                WindowStrip = Mat("GB_Okna", new Color(0.2f, 0.15f, 0.05f), new Color(1f, 0.6f, 0.2f) * 2.5f, 0.5f, 0f),
                Pad = Mat("GB_Ladowisko", new Color(0.1f, 0.2f, 0.25f), new Color(0.2f, 0.7f, 0.9f) * 0.6f, 0.4f, 0f),
                Board = Mat("GB_Tablica", new Color(0.05f, 0.12f, 0.08f), new Color(0.3f, 1f, 0.5f) * 0.8f, 0.6f, 0f),
                Terminal = Mat("GB_Terminal", new Color(0.3f, 0.18f, 0.08f), new Color(1f, 0.45f, 0.1f) * 0.8f, 0.4f, 0.3f),
                Planet = Mat("GB_Morwen", new Color(0.45f, 0.3f, 0.22f), Color.black, 0.1f, 0f),
                ShipHull = Mat("GB_StatekKadlub", new Color(0.55f, 0.53f, 0.5f), Color.black, 0.45f, 0.7f),
                Glass = Mat("GB_Szklo", new Color(0.05f, 0.08f, 0.1f), new Color(0.3f, 0.6f, 0.8f) * 0.5f, 0.9f, 0f),
                Engine = Mat("GB_Silnik", new Color(0.1f, 0.1f, 0.1f), new Color(0.4f, 0.7f, 1f) * 1.5f, 0.3f, 0f),
                Player = Mat("GB_Gracz", new Color(0.6f, 0.35f, 0.15f), Color.black, 0.3f, 0f),
            };
        }

        /// <summary>Creates (or updates) a URP Lit material asset.</summary>
        private static Material Mat(string fileName, Color baseColor, Color emission, float smoothness, float metallic)
        {
            string path = $"{MaterialFolder}/{fileName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);

            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        // --- Helpers -------------------------------------------------------------

        private static GameObject Cube(Transform parent, string objectName, Vector3 localPosition, Quaternion localRotation,
            Vector3 scale, Material material, bool keepCollider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) { Debug.LogError($"[Kotwica] Brak pola '{field}' w {target.GetType().Name}"); return; }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) { Debug.LogError($"[Kotwica] Brak pola '{field}' w {target.GetType().Name}"); return; }
            property.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) { Debug.LogError($"[Kotwica] Brak pola '{field}' w {target.GetType().Name}"); return; }
            property.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null) { Debug.LogError($"[Kotwica] Brak pola '{field}' w {target.GetType().Name}"); return; }
            property.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
