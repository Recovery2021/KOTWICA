using System.Collections.Generic;
using Kotwica.Items;
using Kotwica.Quests;
using Kotwica.World;
using UnityEditor;
using UnityEngine;

namespace Kotwica.EditorTools
{
    /// <summary>
    /// Creates the ring data of the open world (only what does not exist yet; security tuning is refreshed).
    /// Quests, conversations and items are NOT made here any more: they live in the editable JSON files
    /// in Assets/StreamingAssets/Kotwica (see ContentLibrary and the "Kotwica/Zadania" menu).
    /// Menu: Kotwica > Otwarty świat: utwórz dane startowe.
    /// </summary>
    public static class OpenWorldDataBuilder
    {
        private const string Root = "Assets/_Project/Resources";

        [MenuItem("Kotwica/Otwarty świat: utwórz dane startowe")]
        public static void Build()
        {
            int created = 0;
            created += CreateRings();
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Kotwica",
                (created > 0 ? $"Utworzono {created} nowych plików pierścieni w {Root}." : "Dane pierścieni już istnieją (ochrona odświeżona).") +
                "\n\nZadania, rozmowy i przedmioty edytujesz w plikach Assets/StreamingAssets/Kotwica (menu Kotwica > Zadania).",
                "OK");
        }

        // --- Rings -------------------------------------------------------------------------

        private static int CreateRings()
        {
            int n = 0;
            n += Ring("kesh", "Kesh", SocialClass.Robotnicy, 0.6f, 0.08f,
                "Pierścień robotników. Doki, odlewnie i recykling próżniowy. Tłok, para i bursztynowe światła; ochrona bywa przekupna.",
                ("Doki przeładunkowe", DistrictType.Doki), ("Baraki kontenerowe", DistrictType.Mieszkalna),
                ("Odlewnia i recykling", DistrictType.Praca), ("Warsztaty przyzakładowe", DistrictType.Edukacja),
                ("Bazar przy śluzie", DistrictType.Handel));
            n += Ring("orrin", "Orrin", SocialClass.KlasaSrednia, 1.0f, 0.05f,
                "Pierścień klasy średniej (nazwa robocza). Urzędy, handel i szkoły techniczne. Kolejki, pieczątki i neony sklepów.",
                ("Bloki mieszkalne", DistrictType.Mieszkalna), ("Urząd Przepustek", DistrictType.Bezpieczenstwo),
                ("Targowisko", DistrictType.Handel), ("Szkoła techniczna", DistrictType.Edukacja),
                ("Dzielnica serwisów", DistrictType.Praca));
            n += Ring("varo", "Varo", SocialClass.Elita, 1.6f, 0.03f,
                "Pierścień elity, najbliżej gwiazdy. Zarząd stacji, klinika, laboratoria i akademia. Czystość, ogrody i kamery.",
                ("Rezydencje z ogrodami", DistrictType.Mieszkalna), ("Zarząd stacji", DistrictType.Praca),
                ("Klinika", DistrictType.Medycyna), ("Laboratoria", DistrictType.Praca),
                ("Akademia", DistrictType.Edukacja));
            // Security tuning is refreshed every run, also on rings created by older versions
            Security("kesh", 2, 4, true, 30);
            Security("orrin", 3, 3, false, 40);
            Security("varo", 5, 3, false, 60);
            return n;
        }

        /// <summary>How hard each ring's guards come down on the player (see SecurityResponse).</summary>
        private static void Security(string id, int maxGuards, int armedFrom, bool bribes, int finePerLevel)
        {
            var ring = AssetDatabase.LoadAssetAtPath<RingDefinition>($"{Root}/Rings/Ring_{id}.asset");
            if (ring == null) return;
            ring.maxGuards = maxGuards;
            ring.armedFromLevel = armedFrom;
            ring.bribesAccepted = bribes;
            ring.finePerLevel = finePerLevel;
            EditorUtility.SetDirty(ring);
        }

        private static int Ring(string id, string name, SocialClass socialClass, float law, float decay, string description,
            params (string name, DistrictType type)[] districts)
        {
            string path = $"{Root}/Rings/Ring_{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<RingDefinition>(path) != null) return 0;

            var ring = ScriptableObject.CreateInstance<RingDefinition>();
            ring.id = id;
            ring.displayName = name;
            ring.socialClass = socialClass;
            ring.lawMultiplier = law;
            ring.alertDecayPerSecond = decay;
            ring.description = description;
            foreach ((string districtName, DistrictType type) in districts)
                ring.districts.Add(new DistrictInfo { name = districtName, type = type });
            Save(ring, path);
            return 1;
        }

        private static void Save(Object asset, string path)
        {
            ArtPalette.EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
