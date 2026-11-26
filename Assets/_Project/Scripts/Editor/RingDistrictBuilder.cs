using System.Collections.Generic;
using Kotwica.Quests;
using Kotwica.Security;
using Kotwica.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kotwica.EditorTools
{
    /// <summary>
    /// Builds the first district of every ring as its own scene (graybox), streamed in by RingStreamer:
    ///   Ring_Kesh  - workers: container barracks, pipes, pump station, foreman, fence
    ///   Ring_Orrin - middle class: apartment blocks, market stalls, Permit Office
    ///   Ring_Varo  - elite: villas with gardens, laboratory with a cold store
    /// Each district is a 100 m street east of the port deck, in the ring's own coordinates (deck top y = 13).
    /// Residents and objects are wired to the starter quests (side quests and chapter 2).
    /// Menu: Kotwica > Świat: zbuduj dzielnice pierścieni.
    /// </summary>
    public static class RingDistrictBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes/Rings";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Districts";
        private const float Floor = 13f;            // deck surface height in ring coordinates
        private const float StreetStart = 16f;      // east edge of the port deck
        private const float StreetEnd = 118f;
        private const float HalfWidth = 22f;

        private static Dictionary<string, Material> palette;
        private static Dictionary<string, Material> extra;
        private static Transform root;

        [MenuItem("Kotwica/Świat: zbuduj dzielnice pierścieni")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            palette = ArtPalette.LoadOrCreate();
            extra = new Dictionary<string, Material>
            {
                ["Rdza"] = Mat("D_Rdza", new Color(0.36f, 0.2f, 0.12f), 0.3f, 0.25f),
                ["KontenerA"] = Mat("D_KontenerA", new Color(0.18f, 0.3f, 0.35f), 0.5f, 0.3f),
                ["KontenerB"] = Mat("D_KontenerB", new Color(0.42f, 0.16f, 0.12f), 0.5f, 0.3f),
                ["KontenerC"] = Mat("D_KontenerC", new Color(0.32f, 0.3f, 0.18f), 0.5f, 0.3f),
                ["Beton"] = Mat("D_Beton", new Color(0.42f, 0.42f, 0.44f), 0f, 0.15f),
                ["Elita"] = Mat("D_Elita", new Color(0.82f, 0.83f, 0.84f), 0.1f, 0.6f),
                ["Ogrod"] = Mat("D_Ogrod", new Color(0.14f, 0.32f, 0.16f), 0f, 0.2f),
                ["NeonCyjan"] = Mat("D_NeonCyjan", new Color(0.1f, 0.3f, 0.35f), 0f, 0.5f, new Color(0.2f, 0.9f, 1f) * 2f),
                ["NeonRoz"] = Mat("D_NeonRoz", new Color(0.35f, 0.1f, 0.2f), 0f, 0.5f, new Color(1f, 0.3f, 0.6f) * 2f),
            };

            var scenes = new List<string>
            {
                BuildRing("kesh", "Kesh — doki i baraki", BuildKesh),
                BuildRing("orrin", "Orrin — urzędy i targ", BuildOrrin),
                BuildRing("varo", "Varo — rezydencje i laboratoria", BuildVaro),
            };

            // Streaming loads scenes by name, so they must be in Build Settings
            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (string path in scenes)
                if (!buildScenes.Exists(s => s.path == path)) buildScenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            EditorUtility.DisplayDialog("Kotwica",
                "Zbudowano dzielnice: Ring_Kesh, Ring_Orrin, Ring_Varo.\n\nTeraz: Kotwica → Zbuduj scenę Prototyp 01 (dodaje Orrin i doczytywanie), potem Play.",
                "OK");
        }

        private static string BuildRing(string ringId, string title, System.Action build)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject($"Dzielnice: {title}");
            var content = go.AddComponent<RingContent>();
            var so = new SerializedObject(content);
            so.FindProperty("ringId").stringValue = ringId;
            so.ApplyModifiedPropertiesWithoutUndo();
            root = go.transform;

            BuildStreet();
            build();

            ArtPalette.EnsureFolder(SceneFolder);
            string name = "Ring_" + char.ToUpperInvariant(ringId[0]) + ringId.Substring(1);
            string path = $"{SceneFolder}/{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // --- Shared street -----------------------------------------------------------------

        private static void BuildStreet()
        {
            float length = StreetEnd - StreetStart;
            float midX = (StreetStart + StreetEnd) * 0.5f;
            Box("Ulica", new Vector3(midX, Floor - 0.5f, 0f), new Vector3(length, 1f, HalfWidth * 2f), palette["M_DarkMetal"]);
            Box("Pas ruchu", new Vector3(midX, Floor + 0.01f, 0f), new Vector3(length, 0.02f, 6f), palette["M_Rubber"], false);
            Box("Barierka_Pn", new Vector3(midX, Floor + 0.55f, HalfWidth), new Vector3(length, 1.1f, 0.3f), palette["M_BareMetal"]);
            Box("Barierka_Pd", new Vector3(midX, Floor + 0.55f, -HalfWidth), new Vector3(length, 1.1f, 0.3f), palette["M_BareMetal"]);
            Box("Barierka_Koniec", new Vector3(StreetEnd, Floor + 0.55f, 0f), new Vector3(0.3f, 1.1f, HalfWidth * 2f), palette["M_BareMetal"]);
        }

        // --- Kesh: workers ------------------------------------------------------------------

        private static void BuildKesh()
        {
            Material[] containers = { extra["KontenerA"], extra["KontenerB"], extra["KontenerC"] };

            // Container barracks: two rows, stacked 1-3 high, some with a lit window
            for (int i = 0; i < 12; i++)
            {
                float x = 24f + i * 7f;
                foreach (float z in new[] { 17f, -17f })
                {
                    int height = 1 + (i * 7 + (z > 0 ? 3 : 0)) % 3;
                    for (int level = 0; level < height; level++)
                    {
                        Material m = containers[(i + level + (z > 0 ? 1 : 0)) % 3];
                        Box($"Kontener_{i}_{level}", new Vector3(x, Floor + 1.3f + level * 2.6f, z), new Vector3(6f, 2.6f, 2.5f), m);
                        if ((i + level) % 3 == 0)
                            Box("Okno", new Vector3(x, Floor + 1.5f + level * 2.6f, z - Mathf.Sign(z) * 1.26f), new Vector3(1.2f, 0.7f, 0.02f), palette["M_LightAmber"], false);
                    }
                }
            }

            // Steam pipes over the street
            foreach (float z in new[] { 8f, -8f })
                Cylinder("Rura parowa", new Vector3(StreetStart, Floor + 6f, z), new Vector3(StreetEnd, Floor + 6f, z), 0.3f, palette["M_Pipe"]);

            // Pump station at the end of the street
            Box("Pompownia", new Vector3(110f, Floor + 4f, 0f), new Vector3(12f, 8f, 16f), extra["Rdza"]);
            Sign("POMPOWNIA", new Vector3(103.5f, Floor + 6f, 0f));
            Target("Zawór pompowni", new Vector3(103f, Floor + 0.8f, 3f), new Vector3(0.8f, 1.6f, 0.8f), palette["M_Warning"],
                ObjectiveType.Dostarcz, "kesh_pompownia", "Oddaj części do pomp", take: "czesci_pomp", takeCount: 3);

            // Foreman gives "Zimne pompy"; three crates of parts lie around the barracks
            Npc("Brygadzista Oren", new Vector3(40f, Floor, 6f), palette["M_Warning"], offers: "side_kesh_pompy", talk: "kesh_brygadzista",
                idle: "Pompy dalej stoją. Zimno w barakach.");
            Target("Skrzynka z częściami", new Vector3(55f, Floor + 0.4f, -12f), new Vector3(0.9f, 0.8f, 0.7f), palette["M_PaintedMetal"],
                ObjectiveType.Zdobadz, "czesci_pomp", "Zabierz części", give: "czesci_pomp", singleUse: true);
            Target("Skrzynka z częściami", new Vector3(72f, Floor + 0.4f, 12f), new Vector3(0.9f, 0.8f, 0.7f), palette["M_PaintedMetal"],
                ObjectiveType.Zdobadz, "czesci_pomp", "Zabierz części", give: "czesci_pomp", singleUse: true);
            Target("Skrzynka z częściami", new Vector3(88f, Floor + 0.4f, -12f), new Vector3(0.9f, 0.8f, 0.7f), palette["M_PaintedMetal"],
                ObjectiveType.Zdobadz, "czesci_pomp", "Zabierz części", give: "czesci_pomp", singleUse: true);

            // The fence who buys stolen lab samples from Varo
            Target("Paser", new Vector3(84f, Floor + 1f, -5f), new Vector3(0.7f, 2f, 0.7f), palette["M_DarkMetal"],
                ObjectiveType.Dostarcz, "kesh_paser", "Sprzedaj próbki paserowi", take: "probki_lab");
            Sign("PASER", new Vector3(84f, Floor + 2.4f, -5f));

            StreetLights(new Color(1f, 0.6f, 0.25f), 2.5f, 14f);
            PopulateKesh();
        }

        // --- Orrin: middle class ----------------------------------------------------------------

        private static void BuildOrrin()
        {
            // Apartment blocks with rows of cold windows facing the street
            for (int i = 0; i < 6; i++)
            {
                float x = 26f + i * 12f;
                foreach (float z in new[] { 17f, -17f })
                {
                    Box($"Blok_{i}", new Vector3(x, Floor + 9f, z), new Vector3(10f, 18f, 8f), extra["Beton"]);
                    for (int floor = 0; floor < 5; floor++)
                        Box("Okna", new Vector3(x, Floor + 2.5f + floor * 3.3f, z - Mathf.Sign(z) * 4.01f), new Vector3(8f, 0.8f, 0.02f),
                            (floor + i) % 4 == 0 ? palette["M_LightAmber"] : palette["M_LightCold"], false);
                }
            }

            // Market stalls with neon signs
            for (int i = 0; i < 4; i++)
            {
                foreach (float z in new[] { 5f, -5f })
                {
                    float x = 32f + i * 9f;
                    Box("Stragan", new Vector3(x, Floor + 0.5f, z), new Vector3(2.5f, 1f, 1.5f), palette["M_PaintedMetal"]);
                    Box("Daszek", new Vector3(x, Floor + 2.4f, z), new Vector3(3f, 0.1f, 2f), (i % 2 == 0) ? extra["NeonCyjan"] : extra["NeonRoz"], false);
                }
            }

            // Permit Office: chapter 2 starts here
            Box("Urząd Przepustek", new Vector3(110f, Floor + 6f, 0f), new Vector3(14f, 12f, 20f), extra["Beton"]);
            Sign("URZĄD PRZEPUSTEK", new Vector3(102.5f, Floor + 8f, 0f));
            Area("Przed urzędem", new Vector3(96f, Floor + 1.5f, 0f), new Vector3(6f, 3f, 12f), "orrin_urzad");
            Npc("Urzędniczka Sel", new Vector3(100f, Floor, 2f), palette["M_PaintedMetal"], offers: null, talk: "orrin_urzednik",
                give: "przepustka_varo", idle: "Numerek i czekać. Jak wszyscy.");

            StreetLights(new Color(0.7f, 0.85f, 1f), 3f, 12f);
            PopulateOrrin();
        }

        // --- Varo: elite -----------------------------------------------------------------------------

        private static void BuildVaro()
        {
            // Villas behind gardens, small trees
            foreach (float x in new[] { 28f, 50f, 72f })
            {
                foreach (float z in new[] { 15f, -15f })
                {
                    Box("Rezydencja", new Vector3(x, Floor + 2.5f, z), new Vector3(14f, 5f, 10f), extra["Elita"]);
                    Box("Przeszklenie", new Vector3(x, Floor + 2.5f, z - Mathf.Sign(z) * 5.01f), new Vector3(10f, 2.5f, 0.02f), palette["M_Glass"], false);
                    Box("Ogród", new Vector3(x, Floor + 0.15f, z - Mathf.Sign(z) * 7.5f), new Vector3(14f, 0.3f, 4f), extra["Ogrod"]);
                    Tree(new Vector3(x - 4f, Floor, z - Mathf.Sign(z) * 7.5f));
                    Tree(new Vector3(x + 4f, Floor, z - Mathf.Sign(z) * 7.5f));
                }
            }

            // Laboratory with a cold store: side quest "Próbki z chłodni"
            Box("Laboratorium", new Vector3(106f, Floor + 5f, 0f), new Vector3(20f, 10f, 24f), extra["Elita"]);
            Box("Pas okien", new Vector3(95.99f, Floor + 6f, 0f), new Vector3(0.02f, 1.5f, 20f), palette["M_LightCold"], false);
            Sign("LABORATORIA ZARZĄDU", new Vector3(95.5f, Floor + 8.5f, 0f));
            Npc("Laborantka Ida", new Vector3(84f, Floor, -6f), extra["Elita"], offers: "side_varo_probki", talk: "varo_laborantka",
                idle: "Proszę nie dotykać niczego w laboratorium.");
            Target("Chłodnia", new Vector3(95f, Floor + 1f, 7f), new Vector3(1.2f, 2f, 1.2f), palette["M_LightCold"],
                ObjectiveType.Interakcja, "varo_chlodnia", "Wynieś próbki z chłodni", give: "probki_lab", crime: true);

            // Security cameras on poles: the elite is watching
            foreach (float x in new[] { 40f, 64f, 88f })
            {
                Cylinder("Słup kamery", new Vector3(x, Floor, 21f), new Vector3(x, Floor + 5f, 21f), 0.08f, palette["M_BareMetal"]);
                Box("Kamera", new Vector3(x, Floor + 5f, 20.7f), new Vector3(0.3f, 0.25f, 0.5f), palette["M_DarkMetal"]);
                Box("Dioda", new Vector3(x, Floor + 5.05f, 20.44f), new Vector3(0.05f, 0.05f, 0.02f), palette["M_LightRed"], false);
            }

            StreetLights(new Color(0.9f, 0.95f, 1f), 3.5f, 10f);
            PopulateVaro();
        }

        // --- Building blocks --------------------------------------------------------------------------

        private static GameObject Box(string name, Vector3 position, Vector3 size, Material material, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Cylinder(string name, Vector3 from, Vector3 to, float radius, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.localPosition = (from + to) * 0.5f;
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (to - from).normalized);
            go.transform.localScale = new Vector3(radius * 2f, (to - from).magnitude * 0.5f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void Tree(Vector3 position)
        {
            Cylinder("Pień", position, position + Vector3.up * 1.6f, 0.12f, extra["Rdza"]);
            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Korona";
            Object.DestroyImmediate(crown.GetComponent<Collider>());
            crown.transform.SetParent(root, false);
            crown.transform.localPosition = position + Vector3.up * 2.3f;
            crown.transform.localScale = Vector3.one * 1.8f;
            crown.GetComponent<Renderer>().sharedMaterial = extra["Ogrod"];
        }

        private static void StreetLights(Color color, float intensity, float spacing)
        {
            for (float x = StreetStart + 6f; x < StreetEnd; x += spacing)
            {
                var go = new GameObject("Latarnia");
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(x, Floor + 6f, 0f);
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = color;
                light.intensity = intensity;
                light.range = spacing * 1.4f;
            }
        }

        private static void Sign(string text, Vector3 position)
        {
            var go = new GameObject("Napis: " + text);
            go.transform.SetParent(root, false);
            go.transform.localPosition = position;
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = 0.12f;
            mesh.fontSize = 48;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.color = new Color(1f, 0.85f, 0.55f);
            go.AddComponent<FaceCamera>();
        }

        private static void Npc(string name, Vector3 feet, Material material, string offers, string talk, string idle, string give = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule); // keeps its collider: F finds it
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.localPosition = feet + Vector3.up;
            go.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // facing the port (players come from there)
            go.GetComponent<Renderer>().sharedMaterial = material;

            var npc = go.AddComponent<QuestNpc>();
            var so = new SerializedObject(npc);
            so.FindProperty("npcName").stringValue = name;
            so.FindProperty("offersQuestId").stringValue = offers ?? "";
            so.FindProperty("talkTargetId").stringValue = talk ?? "";
            so.FindProperty("giveItemId").stringValue = give ?? "";
            so.FindProperty("idleLine").stringValue = idle;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Target(string name, Vector3 position, Vector3 size, Material material, ObjectiveType type, string targetId,
            string prompt, string give = null, string take = null, int takeCount = 1, bool singleUse = false, bool crime = false)
        {
            GameObject go = Box(name, position, size, material);
            var target = go.AddComponent<QuestTarget>();
            var so = new SerializedObject(target);
            so.FindProperty("targetId").stringValue = targetId;
            so.FindProperty("type").enumValueIndex = (int)type;
            so.FindProperty("prompt").stringValue = prompt;
            so.FindProperty("giveItemId").stringValue = give ?? "";
            so.FindProperty("takeItemId").stringValue = take ?? "";
            so.FindProperty("takeCount").intValue = takeCount;
            so.FindProperty("singleUse").boolValue = singleUse;
            so.FindProperty("isCrime").boolValue = crime;
            so.FindProperty("crime").enumValueIndex = (int)CrimeType.Kradziez;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Area(string name, Vector3 position, Vector3 size, string targetId)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = position;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var area = go.AddComponent<QuestArea>();
            var so = new SerializedObject(area);
            so.FindProperty("targetId").stringValue = targetId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- Residents ---------------------------------------------------------------------------------
        // Roles (schedules) are data assets in Assets/_Project/Data/Roles: edit hours and lines there.

        private const string RoleFolder = "Assets/_Project/Data/Roles";

        private static void PopulateKesh()
        {
            ResidentRole day = Role("kesh_zmiana_dzienna", "doker, zmiana dzienna", SocialClass.Robotnicy,
                new[] { (0, Activity.Dom), (6, Activity.Praca), (14, Activity.Wypoczynek), (19, Activity.Dom) },
                "Padam z nóg.", "Kontenery same się nie przeładują.", "-", "Na bazarze rzucili filtry. Drogo.");
            ResidentRole late = Role("kesh_zmiana_popoludniowa", "hutnik, zmiana popołudniowa", SocialClass.Robotnicy,
                new[] { (0, Activity.Dom), (10, Activity.Wypoczynek), (13, Activity.Praca), (21, Activity.Dom) },
                "Jeszcze godzina snu...", "Piec znowu przegrzany.", "-", "Ostatnia chwila przed zmianą.");
            ResidentRole student = Role("kesh_uczen", "uczeń warsztatów", SocialClass.Robotnicy,
                new[] { (0, Activity.Dom), (8, Activity.Nauka), (14, Activity.Wypoczynek), (20, Activity.Dom) },
                "Matka czeka z kolacją.", "-", "Dziś spawanie w próżni. Na sucho, na szczęście.", "Może kiedyś polecę na Varo.");

            var homes = new List<Transform>();
            for (int i = 0; i < 12; i += 2)
            {
                float x = 24f + i * 7f;
                homes.Add(Spot($"Dom_{i}a", new Vector3(x, Floor, 14.6f)));
                homes.Add(Spot($"Dom_{i}b", new Vector3(x, Floor, -14.6f)));
            }
            Transform[] work = { Spot("Praca: pompownia", new Vector3(102f, Floor, -4f)), Spot("Praca: doki", new Vector3(10f, Floor, -12f)), Spot("Praca: rampa", new Vector3(10f, Floor, 13f)) };
            Sign("WARSZTATY", new Vector3(60f, Floor + 3f, 10f));
            Transform school = Spot("Nauka: warsztaty", new Vector3(60f, Floor, 9f));
            Transform[] leisure = { Spot("Bazar", new Vector3(24f, Floor, 6f)), Spot("Pod rurami", new Vector3(46f, Floor, -6f)) };

            string[] names = { "Tamar", "Bodo", "Ilka", "Rusz", "Weno", "Jaga", "Kord", "Mila", "Sten", "Orla" };
            ResidentRole[] roles = { day, day, day, day, late, late, late, student, student, student };
            for (int i = 0; i < names.Length; i++)
                Resident(names[i], roles[i], "kesh", homes[i % homes.Count], work[i % work.Length], school, leisure[i % leisure.Length],
                    7f, extra["KontenerB"]);
        }

        private static void PopulateOrrin()
        {
            ResidentRole clerk = Role("orrin_urzednik", "urzędnik", SocialClass.KlasaSrednia,
                new[] { (0, Activity.Dom), (8, Activity.Praca), (16, Activity.Wypoczynek), (20, Activity.Dom) },
                "Jutro znowu kolejka.", "Formularz B-12. Następny!", "-", "Herbata z targu, jedyna przyjemność.");
            ResidentRole trader = Role("orrin_handlarz", "handlarz", SocialClass.KlasaSrednia,
                new[] { (0, Activity.Dom), (7, Activity.Praca), (19, Activity.Wypoczynek), (22, Activity.Dom) },
                "Utarg słaby.", "Filtry, uszczelki, kawa syntetyczna!", "-", "Kto by pomyślał, że ceny jeszcze wzrosną.");
            ResidentRole student = Role("orrin_uczen", "uczeń szkoły technicznej", SocialClass.KlasaSrednia,
                new[] { (0, Activity.Dom), (8, Activity.Nauka), (15, Activity.Wypoczynek), (21, Activity.Dom) },
                "Egzamin jutro.", "-", "Jak zdam, dostanę przydział na Varo. Może.", "Targ to jedyne ciekawe miejsce.");

            var homes = new List<Transform>();
            for (int i = 0; i < 6; i++)
            {
                float x = 26f + i * 12f;
                if (i != 5) homes.Add(Spot($"Dom_{i}a", new Vector3(x, Floor, 12.5f)));
                homes.Add(Spot($"Dom_{i}b", new Vector3(x, Floor, -12.5f)));
            }
            Sign("SZKOŁA TECHNICZNA", new Vector3(86f, Floor + 4f, 12.2f));
            Transform school = Spot("Nauka: szkoła", new Vector3(86f, Floor, 11.8f));
            var work = new List<Transform> { Spot("Praca: urząd", new Vector3(100f, Floor, -5f)) };
            for (int i = 0; i < 4; i++) work.Add(Spot($"Praca: stragan {i}", new Vector3(32f + i * 9f, Floor, i % 2 == 0 ? 6.6f : -6.6f)));
            Transform[] leisure = { Spot("Targ", new Vector3(45f, Floor, 0f)), Spot("Ławki", new Vector3(55f, Floor, 2f)) };

            string[] names = { "Pela", "Adri", "Konst", "Liwa", "Marek", "Nesa", "Teo", "Wanda", "Ivo", "Rena" };
            ResidentRole[] roles = { clerk, clerk, clerk, clerk, trader, trader, trader, student, student, student };
            for (int i = 0; i < names.Length; i++)
            {
                // Clerks go to the office, traders to their stalls
                Transform job = roles[i] == trader ? work[1 + i % 4] : work[0];
                Resident(names[i], roles[i], "orrin", homes[i % homes.Count], job, school, leisure[i % leisure.Length], 8f, extra["Beton"]);
            }
        }

        private static void PopulateVaro()
        {
            ResidentRole scientist = Role("varo_naukowiec", "naukowiec zarządu", SocialClass.Elita,
                new[] { (0, Activity.Dom), (9, Activity.Praca), (17, Activity.Wypoczynek), (22, Activity.Dom) },
                "Proszę zejść mi z drogi.", "Wyniki są gorsze, niż raportujemy.", "-", "Ogrody to jedyne miejsce, gdzie nie słychać stacji.");
            ResidentRole student = Role("varo_student", "student akademii", SocialClass.Elita,
                new[] { (0, Activity.Dom), (9, Activity.Nauka), (15, Activity.Wypoczynek), (23, Activity.Dom) },
                "Ojciec mówi, że Kesh i tak upadnie pierwszy.", "-", "Wykład o stabilizacji orbity. Nudy.", "Kolacja u Celinów, wypada być.");
            ResidentRole resident = Role("varo_rezydent", "rezydent", SocialClass.Elita,
                new[] { (0, Activity.Dom), (10, Activity.Wypoczynek), (18, Activity.Dom) },
                "Ochrona! A, to tylko kurier.", "-", "-", "Te róże przywieziono jeszcze z pierwszym transportem.");

            var homes = new List<Transform>();
            foreach (float x in new[] { 28f, 50f, 72f })
            {
                homes.Add(Spot($"Dom_{x}a", new Vector3(x, Floor, 10.2f)));
                homes.Add(Spot($"Dom_{x}b", new Vector3(x, Floor, -10.2f)));
            }
            Transform[] work = { Spot("Praca: laboratorium A", new Vector3(94f, Floor, -3f)), Spot("Praca: laboratorium B", new Vector3(94f, Floor, 3f)) };
            Sign("AKADEMIA", new Vector3(90f, Floor + 3f, -11f));
            Transform school = Spot("Nauka: akademia", new Vector3(90f, Floor, -10f));
            Transform[] leisure = { Spot("Ogród wschodni", new Vector3(39f, Floor, 6.5f)), Spot("Ogród zachodni", new Vector3(61f, Floor, -6.5f)) };

            string[] names = { "Aurel", "Celin", "Damira", "Evander", "Ilse", "Viktor" };
            ResidentRole[] roles = { scientist, scientist, scientist, student, student, resident };
            for (int i = 0; i < names.Length; i++)
                Resident(names[i], roles[i], "varo", homes[i % homes.Count], work[i % work.Length], school, leisure[i % leisure.Length],
                    4f, extra["Elita"]);
        }

        private static Transform Spot(string name, Vector3 position)
        {
            var go = new GameObject("Miejsce: " + name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = position;
            return go.transform;
        }

        private static ResidentRole Role(string fileName, string roleName, SocialClass socialClass, (int hour, Activity activity)[] schedule,
            string home, string work, string study, string leisure)
        {
            ArtPalette.EnsureFolder(RoleFolder);
            string path = $"{RoleFolder}/Role_{fileName}.asset";
            var role = AssetDatabase.LoadAssetAtPath<ResidentRole>(path);
            if (role != null) return role; // keep the user's edits

            role = ScriptableObject.CreateInstance<ResidentRole>();
            role.roleName = roleName;
            role.socialClass = socialClass;
            foreach ((int hour, Activity activity) in schedule)
                role.schedule.Add(new ScheduleEntry { fromHour = hour, activity = activity });
            role.homeLine = home;
            role.workLine = work;
            role.studyLine = study;
            role.leisureLine = leisure;
            AssetDatabase.CreateAsset(role, path);
            return role;
        }

        private static void Resident(string name, ResidentRole role, string ringId, Transform home, Transform work, Transform school,
            Transform leisure, float lane, Material clothes)
        {
            var go = new GameObject("Mieszkaniec: " + name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = home.localPosition;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); // collider kept: the player bumps into people
            body.name = "Ciało";
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            body.GetComponent<Renderer>().sharedMaterial = clothes;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Głowa";
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            head.transform.localScale = Vector3.one * 0.28f;
            head.GetComponent<Renderer>().sharedMaterial = palette["M_Pipe"];

            var ai = go.AddComponent<ResidentAI>();
            var so = new SerializedObject(ai);
            so.FindProperty("residentName").stringValue = name;
            so.FindProperty("role").objectReferenceValue = role;
            so.FindProperty("ringId").stringValue = ringId;
            so.FindProperty("home").objectReferenceValue = home;
            so.FindProperty("work").objectReferenceValue = work;
            so.FindProperty("school").objectReferenceValue = school;
            so.FindProperty("leisure").objectReferenceValue = leisure;
            so.FindProperty("laneOffset").floatValue = lane;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material Mat(string fileName, Color color, float metallic, float smoothness, Color emission = default)
        {
            ArtPalette.EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/{fileName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
