using Kotwica.Combat;
using Kotwica.Player;
using Kotwica.UI;
using UnityEditor;
using UnityEngine;

namespace Kotwica.EditorTools
{
    /// <summary>
    /// Combat prototype: the art-test corridor plus extra crates, cover points, three patrolling
    /// enemies, a working emergency airlock at the far end and the combat HUD.
    /// Menu: Kotwica > Zbuduj scenę Prototyp 02 (walka).
    /// </summary>
    public static class Prototype02SceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype02_Combat.unity";

        // Every crate in the scene (base corridor + extra ones), x/z on the floor
        private static readonly Vector2[] Crates =
        {
            new Vector2(-0.8f, 3.8f),    // extra: first cover for the player
            new Vector2(0.85f, 6.8f),    // base corridor
            new Vector2(0.85f, 10.2f),   // extra: midfield
            new Vector2(-0.85f, 13.2f),  // base corridor (stack of two)
        };

        [MenuItem("Kotwica/Zbuduj scenę Prototyp 02 (walka)")]
        public static void Build()
        {
            if (!ArtTestSceneBuilder.BuildBase(out var scene, out GameObject player, out Camera camera)) return;

            Transform root = ArtTestSceneBuilder.Root;

            // Extra crates so both sides have somewhere to hide
            ArtTestSceneBuilder.Place("KIT_Crate_1m", new Vector3(-0.8f, 0f, 3.8f), 4f);
            ArtTestSceneBuilder.Place("KIT_Crate_1m", new Vector3(0.85f, 0f, 10.2f), -12f);

            BuildCoverPoints(root);
            SetupPlayer(player, camera);
            AirlockController airlock = BuildAirlock(root);

            BuildEnemy("Strażnik_1", new Vector3(0.4f, 0f, 14.5f), new Vector3(0.3f, 0f, 14.5f), new Vector3(0.3f, 0f, 11.0f));
            BuildEnemy("Strażnik_2", new Vector3(-0.2f, 0f, 12.0f), new Vector3(-0.2f, 0f, 11.6f), new Vector3(-0.2f, 0f, 15.0f));
            BuildEnemy("Strażnik_3", new Vector3(0.0f, 0f, 9.2f), new Vector3(0.0f, 0f, 9.2f), new Vector3(0.1f, 0f, 12.0f));

            var hud = new GameObject("_HUD").AddComponent<CombatHud>();
            Set(hud, "playerHealth", player.GetComponent<Health>());
            Set(hud, "shooter", player.GetComponent<PlayerShooter>());
            Set(hud, "playerController", player.GetComponent<PlayerController>());
            Set(hud, "interactor", player.GetComponent<PlayerInteractor>());
            Set(hud, "airlock", airlock);

            ArtTestSceneBuilder.SaveScene(scene, ScenePath);
            Debug.Log($"[Kotwica] Zbudowano {ScenePath}. Naciśnij Play. Walka: PPM celowanie, LPM strzał, C kucanie.");
        }

        private static void SetupPlayer(GameObject player, Camera camera)
        {
            player.transform.position = new Vector3(0f, 0.05f, 1.2f);

            var health = player.AddComponent<Health>();
            SetEnum(health, "team", (int)Team.Player);
            SetFloat(health, "maxHealth", 100f);

            var muzzle = new GameObject("Lufa").transform;
            muzzle.SetParent(player.transform, false);
            muzzle.localPosition = new Vector3(0.3f, 1.35f, 0.45f);

            var shooter = player.AddComponent<PlayerShooter>();
            Set(shooter, "cameraRig", camera.GetComponent<CameraRig>());
            Set(shooter, "muzzle", muzzle);

            player.AddComponent<PlayerInteractor>();
        }

        private static void BuildCoverPoints(Transform root)
        {
            var holder = new GameObject("Osłony").transform;
            holder.SetParent(root, false);

            // Both sides of every crate: which side is "safe" is decided live against the player
            int n = 0;
            foreach (Vector2 crate in Crates)
            {
                AddCover(holder, new Vector3(crate.x, 0f, crate.y - 0.9f), n++);
                AddCover(holder, new Vector3(crate.x, 0f, crate.y + 0.9f), n++);
            }

            // Hugging the bulkhead pillars
            foreach (float z in new[] { 4f, 8f, 12f })
            {
                foreach (float x in new[] { -0.85f, 0.85f })
                {
                    AddCover(holder, new Vector3(x, 0f, z - 0.5f), n++);
                    AddCover(holder, new Vector3(x, 0f, z + 0.5f), n++);
                }
            }
        }

        private static void AddCover(Transform parent, Vector3 position, int index)
        {
            var go = new GameObject($"Osłona_{index:00}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<CoverPoint>();
        }

        private static AirlockController BuildAirlock(Transform root)
        {
            // The far door panel becomes the moving outer airlock door (must not be static-batched)
            Transform door = null;
            Transform console = null;
            foreach (Transform child in root)
            {
                if (child.name == "KIT_Door_Panel" && child.localPosition.z > 8f) door = child;
                if (child.name == "KIT_Console") console = child;
            }
            if (door != null)
                foreach (Transform t in door.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);

            var airlockObject = new GameObject("Śluza awaryjna");
            airlockObject.transform.SetParent(root, false);
            airlockObject.transform.localPosition = new Vector3(0f, 0f, 16f);

            var suction = new GameObject("Punkt ssania").transform;
            suction.SetParent(airlockObject.transform, false);
            suction.localPosition = new Vector3(0f, 1.2f, 0.8f);

            var alarm = new GameObject("Alarm śluzy").AddComponent<Light>();
            alarm.transform.SetParent(airlockObject.transform, false);
            alarm.transform.localPosition = new Vector3(0f, 2.4f, -1.5f);
            alarm.type = LightType.Point;
            alarm.color = new Color(1f, 0.1f, 0.05f);
            alarm.intensity = 4f;
            alarm.range = 10f;

            var airlock = airlockObject.AddComponent<AirlockController>();
            Set(airlock, "door", door);
            Set(airlock, "suctionPoint", suction);
            Set(airlock, "alarmLight", alarm);

            // Grab bars: one next to the console (move there within the countdown), one near the start
            BuildHandle(root, "Uchwyt_Konsola", new Vector3(1.38f, 0f, 6.6f), new Vector3(0.9f, 0.05f, 6.6f));
            BuildHandle(root, "Uchwyt_Start", new Vector3(-1.38f, 0f, 2.6f), new Vector3(-0.9f, 0.05f, 2.6f));

            if (console != null)
            {
                var airlockConsole = console.gameObject.AddComponent<AirlockConsole>();
                Set(airlockConsole, "airlock", airlock);
            }
            else Debug.LogWarning("[Kotwica] Nie znaleziono konsoli dla śluzy.");

            return airlock;
        }

        /// <summary>Yellow vertical grab bar on the wall plus the spot where the player stands holding it.</summary>
        private static void BuildHandle(Transform root, string handleName, Vector3 wallPosition, Vector3 standPosition)
        {
            var palette = ArtTestSceneBuilder.Palette;
            float side = Mathf.Sign(wallPosition.x); // +1 right wall, -1 left wall

            var handle = new GameObject(handleName);
            handle.transform.SetParent(root, false);
            handle.transform.localPosition = wallPosition;

            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Drążek";
            bar.transform.SetParent(handle.transform, false);
            bar.transform.localPosition = new Vector3(-side * 0.08f, 1.3f, 0f);
            bar.transform.localScale = new Vector3(0.05f, 0.9f, 0.05f);
            bar.GetComponent<Renderer>().sharedMaterial = palette["M_Warning"];

            foreach (float y in new[] { 0.88f, 1.72f })
            {
                var bracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bracket.name = "Mocowanie";
                Object.DestroyImmediate(bracket.GetComponent<Collider>());
                bracket.transform.SetParent(handle.transform, false);
                bracket.transform.localPosition = new Vector3(-side * 0.04f, y, 0f);
                bracket.transform.localScale = new Vector3(0.1f, 0.04f, 0.04f);
                bracket.GetComponent<Renderer>().sharedMaterial = palette["M_BareMetal"];
            }

            var hold = new GameObject("PunktTrzymania").transform;
            hold.SetParent(root, false);
            hold.localPosition = standPosition;
            hold.localRotation = Quaternion.identity; // facing down the corridor, towards the enemies

            var safety = handle.AddComponent<SafetyHandle>();
            Set(safety, "holdPoint", hold);
        }

        private static void BuildEnemy(string enemyName, Vector3 position, Vector3 patrolA, Vector3 patrolB)
        {
            var palette = ArtTestSceneBuilder.Palette;

            var root = new GameObject(enemyName);
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(patrolB - patrolA);

            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.35f;

            // Placeholder body: dark suit, red visor, gun
            var visual = new GameObject("Wygląd").transform;
            visual.SetParent(root.transform, false);
            visual.localPosition = new Vector3(0f, 0.9f, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Ciało";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(visual, false);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            body.GetComponent<Renderer>().sharedMaterial = palette["M_DarkMetal"];

            AddPart(visual, "Wizjer", new Vector3(0f, 0.62f, 0.3f), new Vector3(0.42f, 0.12f, 0.12f), palette["M_LightRed"]);
            AddPart(visual, "Broń", new Vector3(0.28f, 0.42f, 0.4f), new Vector3(0.1f, 0.12f, 0.7f), palette["M_BareMetal"]);

            var muzzle = new GameObject("Lufa").transform;
            muzzle.SetParent(root.transform, false);
            muzzle.localPosition = new Vector3(0.28f, 1.35f, 0.8f);

            var patrol = new GameObject($"{enemyName}_Patrol").transform;
            var a = new GameObject("A").transform;
            a.SetParent(patrol, false);
            a.position = patrolA;
            var b = new GameObject("B").transform;
            b.SetParent(patrol, false);
            b.position = patrolB;

            var health = root.AddComponent<Health>();
            SetEnum(health, "team", (int)Team.Enemy);
            SetFloat(health, "maxHealth", 100f);

            var ai = root.AddComponent<EnemyAI>();
            Set(ai, "visual", visual);
            Set(ai, "muzzle", muzzle);
            var so = new SerializedObject(ai);
            SerializedProperty points = so.FindProperty("patrolPoints");
            points.arraySize = 2;
            points.GetArrayElementAtIndex(0).objectReferenceValue = a;
            points.GetArrayElementAtIndex(1).objectReferenceValue = b;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddPart(Transform parent, string partName, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        // --- Serialized field helpers ---------------------------------------------------

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Kotwica] Brak pola '{field}' w {target.GetType().Name}"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Kotwica] Brak pola '{field}' w {target.GetType().Name}"); return; }
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Kotwica] Brak pola '{field}' w {target.GetType().Name}"); return; }
            p.enumValueIndex = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
