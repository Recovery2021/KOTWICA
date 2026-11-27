using System.Collections.Generic;
using Kotwica.Items;
using Kotwica.Quests;
using Kotwica.World;
using UnityEditor;
using UnityEngine;

namespace Kotwica.EditorTools
{
    /// <summary>
    /// Creates the starter data of the open world (only what does not exist yet, so manual edits survive):
    /// three rings with districts, starter items, the first main chapter, sample side quests and an activity.
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
            created += CreateItems();
            created += CreateQuests();
            created += DialogueDataBuilder.Build(); // conversations of chapter 2 and side quests (rebuilt every run)
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Kotwica",
                created > 0 ? $"Utworzono {created} nowych plików danych w {Root}." : "Wszystkie dane startowe już istnieją.",
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

        // --- Items -------------------------------------------------------------------------

        private static int CreateItems()
        {
            int n = 0;
            n += Item("modul_naprawiony", "Moduł po naprawie", ItemCategory.Ladunek, 120, true, "Podzespół podnośnika z serwisu Varo.");
            n += Item("lek_zgloszony", "Lek z kliniki (zgłoszony)", ItemCategory.Medykament, 90, true, "Lek z pieczęcią kliniki Varo.");
            n += Item("lek_niezgloszony", "Lek bez pieczęci", ItemCategory.Medykament, 90, false, "Lek wyniesiony bez zgody. Celnicy go nie przepuszczą.");
            n += Item("przepustka_varo", "Przepustka na Varo", ItemCategory.Dokument, 0, true, "Jednorazowe prawo dokowania w porcie Varo.");
            n += Item("czesci_pomp", "Części do pomp", ItemCategory.Ladunek, 25, true, "Uszczelki i wirniki pomp chłodzenia.");
            n += Item("filtr_powietrza", "Filtr powietrza", ItemCategory.Ladunek, 15, true, "Na Kesh zawsze ich brakuje.");
            n += Item("probki_lab", "Próbki z laboratorium", ItemCategory.Ladunek, 200, false, "Zaplombowane próbki z Varo. Ktoś za nie dobrze zapłaci.");
            // Chapter 1 (Kesh) and ring permits
            n += Item("modul_uszkodzony", "Moduł sterujący podnośnika", ItemCategory.Ladunek, 0, true, "Zatarty moduł z podnośnika dokerki Mary. Do warsztatu Brena.");
            n += Item("lek_przydzial", "Lek z przydziału", ItemCategory.Ladunek, 0, true, "Lek dla córki dokerki, ze stemplem ambulatorium.");
            n += Item("lek_bez_zgody", "Lek bez przydziału", ItemCategory.Ladunek, 0, false, "Wyniesiony ze skrzynki dla Varo. Ochrona go zabierze.");
            n += Item("rejestracja_statku", "Rejestracja holownika Wróbel", ItemCategory.Dokument, 0, true, "Papiery statku. Bez nich nie wejdziesz do kokpitu.");
            n += Item("podmieniona_plomba", "Podrobiona plomba", ItemCategory.Dokument, 0, true, "Plomba Zarządu z kontenera filtrów. Numer się nie zgadza.");
            n += Item("przepustka_orrin", "Przepustka na Orrin", ItemCategory.Dokument, 0, true, "Podpisana przez kapitana portu Kesh. Port Orrin przyjmie twój statek.");
            n += Item("kod_archiwum", "Kod do archiwum B-12", ItemCategory.Dokument, 0, true, "Cztery znaki zapisane na odwrocie formularza.");
            n += Item("wyciag_rejestru", "Wyciąg z rejestru przydziałów", ItemCategory.Dokument, 0, true, "31 przydziałów na Varo podpisanych kluczem kwatermistrza Dorna.");
            n += Item("odpowiedzi_egzamin", "Odpowiedzi na egzamin", ItemCategory.Dokument, 0, false, "Skopiowane z terminalu szkoły technicznej. Ochrona je zabierze.");
            return n;
        }

        private static int Item(string id, string name, ItemCategory category, int value, bool legal, string description)
        {
            string path = $"{Root}/Items/Item_{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemDefinition>(path) != null) return 0;

            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.id = id;
            item.displayName = name;
            item.category = category;
            item.value = value;
            item.legal = legal;
            item.description = description;
            Save(item, path);
            return 1;
        }

        // --- Quests ------------------------------------------------------------------------

        private static int CreateQuests()
        {
            int n = 0;

            // Chapter 1 = the existing "Kurs powrotny" mission; its code reports these target ids
            // Chapter 1 stays on Kesh (rebuilt every run): ends with the permit for Orrin. Conversations: DialogueDataBuilder
            Refresh("main_01_kurs_powrotny");
            n += Quest("main_01_kurs_powrotny", "Rozdział 1: Lek dla córki", QuestKind.Glowny, "kesh",
                "Na Kesh nikt nic nie daje za darmo. Dokerka Mara ma zepsuty podnośnik i chorą córkę, a ty dopiero zaczynasz. " +
                "Zanim polecisz wyżej, musisz zapracować na statek i na zaufanie portu.",
                autoStart: true, repeatable: false, minLevel: 1, required: null,
                reward: Reward(0, 250, (SocialClass.Robotnicy, 5)),
                (ObjectiveType.Porozmawiaj, "kesh_dokerka_start", 1, "Porozmawiaj z dokerką Marą na pokładzie portu"),
                (ObjectiveType.Interakcja, "kesh_podnosnik", 1, "Wymontuj zatarty moduł z podnośnika"),
                (ObjectiveType.Dostarcz, "kesh_serwis", 1, "Zanieś moduł do warsztatu Brena na ulicy"),
                (ObjectiveType.Porozmawiaj, "kesh_medyczka", 1, "Zapytaj w ambulatorium o lek dla córki Mary"),
                (ObjectiveType.Porozmawiaj, "kesh_dokerka", 1, "Wróć do dokerki Mary"),
                (ObjectiveType.Zdobadz, "rejestracja_statku", 1, "Kup holownik u handlarza Brodda na pokładzie portu"),
                (ObjectiveType.Lec, "port_kesh_b", 1, "Przeleć statkiem na dok B Kesh"),
                (ObjectiveType.Porozmawiaj, "kesh_kapitan", 1, "Zgłoś się do kapitana portu Hale"),
                (ObjectiveType.Interakcja, "kesh_b_kontener", 1, "Znajdź kontener z podmienioną plombą"),
                (ObjectiveType.Porozmawiaj, "kesh_kapitan", 1, "Oddaj plombę kapitanowi Hale"));

            // Chapter 2: rebuilt every run (it replaced the old skeleton); its conversations are in DialogueDataBuilder
            Refresh("main_02_urzad_przepustek");
            n += Quest("main_02_urzad_przepustek", "Rozdział 2: Urząd Przepustek", QuestKind.Glowny, "orrin",
                "Przepustka od kapitana Hale otworzyła ci drogę na Orrin. Tu przepustki decydują, kto dostanie się bliżej Zarządu " +
                "i kto przeżyje, gdy stacja zacznie się sypać. Ktoś handluje nimi na lewo.",
                autoStart: true, repeatable: false, minLevel: 1, required: new[] { "main_01_kurs_powrotny" },
                reward: Reward(150, 300, (SocialClass.KlasaSrednia, 10)),
                (ObjectiveType.Lec, "port_orrin", 1, "Zawieź filtry kapitana Hale: zadokuj w porcie Orrin"),
                (ObjectiveType.Idz, "orrin_urzad", 1, "Dotrzyj do Urzędu Przepustek na Orrin"),
                (ObjectiveType.Porozmawiaj, "orrin_urzednik", 1, "Porozmawiaj z urzędniczką Sel"),
                (ObjectiveType.Interakcja, "orrin_archiwum", 1, "Sprawdź rejestr w terminalu archiwum B-12"),
                (ObjectiveType.Porozmawiaj, "orrin_dorn", 1, "Rozmów się z kwatermistrzem Dornem na targu"),
                (ObjectiveType.Zdobadz, "przepustka_varo", 1, "Zdobądź przepustkę na Varo"),
                (ObjectiveType.Lec, "port_varo", 1, "Zadokuj w porcie Varo"));

            // Chapter 3 skeleton: the hook from chapter 2, content comes later
            n += Quest("main_03_zarzad", "Rozdział 3: Sekretarz Zarządu (w budowie)", QuestKind.Glowny, "varo",
                "Przepustki podpisuje ktoś z Zarządu. Miejsca w kapsułach ratunkowych dostają ci, którzy zapłacą. " +
                "Trzeba się dowiedzieć, ilu ich naprawdę jest.",
                autoStart: true, repeatable: false, minLevel: 1, required: new[] { "main_02_urzad_przepustek" },
                reward: Reward(200, 400, (SocialClass.Robotnicy, 10)),
                (ObjectiveType.Idz, "varo_zarzad", 1, "Dotrzyj pod gmach Zarządu na Varo"),
                (ObjectiveType.Porozmawiaj, "varo_halvard", 1, "Znajdź sekretarza Halvarda (w budowie)"));

            n += Quest("side_orrin_egzamin", "Egzamin Teo", QuestKind.Poboczny, "orrin",
                "Uczeń szkoły technicznej ma jutro egzamin, od którego zależy przydział na Varo. Terminal w szkole zna odpowiedzi.",
                autoStart: false, repeatable: false, minLevel: 1, required: null,
                reward: Reward(60, 100, (SocialClass.KlasaSrednia, 5)),
                (ObjectiveType.Porozmawiaj, "orrin_teo", 1, "Porozmawiaj z Teo"),
                (ObjectiveType.Interakcja, "orrin_szkola_terminal", 1, "Zdobądź odpowiedzi z terminalu szkoły"),
                (ObjectiveType.Porozmawiaj, "orrin_teo", 1, "Wróć do Teo"));

            n += Quest("side_kesh_pompy", "Zimne pompy", QuestKind.Poboczny, "kesh",
                "Brygadzista pompowni twierdzi, że części do pomp znikają z magazynu. Bez nich baraki zamarzną.",
                autoStart: false, repeatable: false, minLevel: 1, required: null,
                reward: Reward(80, 120, (SocialClass.Robotnicy, 10)),
                (ObjectiveType.Porozmawiaj, "kesh_brygadzista", 1, "Porozmawiaj z brygadzistą pompowni"),
                (ObjectiveType.Zdobadz, "czesci_pomp", 3, "Zdobądź części do pomp"),
                (ObjectiveType.Dostarcz, "kesh_pompownia", 1, "Zanieś części do pompowni"));

            n += Quest("side_varo_probki", "Próbki z chłodni", QuestKind.Poboczny, "varo",
                "Laborantka z Varo chce wynieść próbki dla paserów z Kesh. Dobrze płaci, ale elita nie zapomina.",
                autoStart: false, repeatable: false, minLevel: 2, required: new[] { "main_01_kurs_powrotny" },
                reward: Reward(150, 100, (SocialClass.Elita, -10), (SocialClass.Robotnicy, 5)),
                (ObjectiveType.Porozmawiaj, "varo_laborantka", 1, "Porozmawiaj z laborantką"),
                (ObjectiveType.Interakcja, "varo_chlodnia", 1, "Wynieś próbki z chłodni"),
                (ObjectiveType.Dostarcz, "kesh_paser", 1, "Sprzedaj próbki paserowi na Kesh"));

            n += Quest("act_kurier", "Kurs kurierski", QuestKind.Aktywnosc, null,
                "Szybkie zlecenie z tablicy: paczka do innego portu.",
                autoStart: false, repeatable: true, minLevel: 1, required: null,
                reward: Reward(40, 30),
                (ObjectiveType.Dostarcz, "terminal_dostaw", 1, "Dostarcz paczkę do terminalu w porcie docelowym"));

            return n;
        }

        /// <summary>Deletes a quest asset so the code version replaces it (for quests whose design changed).</summary>
        private static void Refresh(string questId)
        {
            string path = $"{Root}/Quests/Quest_{questId}.asset";
            if (AssetDatabase.LoadAssetAtPath<QuestDefinition>(path) != null) AssetDatabase.DeleteAsset(path);
        }

        private static QuestReward Reward(int credits, int xp, params (SocialClass socialClass, int amount)[] reputation)
        {
            var reward = new QuestReward { credits = credits, xp = xp };
            foreach ((SocialClass socialClass, int amount) in reputation)
                reward.reputation.Add(new ReputationReward { socialClass = socialClass, amount = amount });
            return reward;
        }

        private static int Quest(string id, string title, QuestKind kind, string ringId, string description,
            bool autoStart, bool repeatable, int minLevel, string[] required, QuestReward reward,
            params (ObjectiveType type, string target, int count, string text)[] objectives)
        {
            string path = $"{Root}/Quests/Quest_{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<QuestDefinition>(path) != null) return 0;

            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.id = id;
            quest.title = title;
            quest.kind = kind;
            quest.ringId = ringId;
            quest.description = description;
            quest.autoStart = autoStart;
            quest.repeatable = repeatable;
            quest.minLevel = minLevel;
            quest.requiredQuests = required != null ? new List<string>(required) : new List<string>();
            quest.reward = reward;
            foreach ((ObjectiveType type, string target, int count, string text) in objectives)
                quest.objectives.Add(new QuestObjective { type = type, targetId = target, count = count, description = text });
            Save(quest, path);
            return 1;
        }

        private static void Save(Object asset, string path)
        {
            ArtPalette.EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
