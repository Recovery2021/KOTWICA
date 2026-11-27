using System.Collections.Generic;
using Kotwica.Dialogue;
using Kotwica.Progression;
using Kotwica.Quests;
using Kotwica.Security;
using Kotwica.World;
using UnityEditor;
using UnityEngine;

namespace Kotwica.EditorTools
{
    /// <summary>
    /// Writes the conversations of chapter 2 and the side quests into Resources/Dialogues.
    /// Called from "Kotwica/Otwarty świat: utwórz dane startowe". These assets are rebuilt on every run,
    /// so change the lines here (or copy an asset under a new id before editing it by hand).
    /// </summary>
    public static class DialogueDataBuilder
    {
        private const string Folder = "Assets/_Project/Resources/Dialogues";

        public static int Build()
        {
            int n = 0;
            n += Mara();
            n += Medic();
            n += Brodd();
            n += Captain();
            n += Container();
            n += Sel();
            n += Archive();
            n += Dorn();
            n += Teo();
            n += Oren();
            n += Ida();
            return n;
        }

        // --- Chapter 1: Kesh -------------------------------------------------------------------------

        private static int Mara()
        {
            var d = Begin("dlg_mara", "Dokerka Mara", out int created);
            Entry(d, "mara_start", Objective(ObjectiveType.Porozmawiaj, "kesh_dokerka_start"));
            Entry(d, "mara_podnosnik", Objective(ObjectiveType.Interakcja, "kesh_podnosnik"));
            Entry(d, "mara_serwis", Objective(ObjectiveType.Dostarcz, "kesh_serwis"));
            Entry(d, "mara_ambulatorium", Objective(ObjectiveType.Porozmawiaj, "kesh_medyczka"));
            Entry(d, "mara_lek_legalny", Objective(ObjectiveType.Porozmawiaj, "kesh_dokerka"), Has("lek_przydzial"));
            Entry(d, "mara_lek_nielegalny", Objective(ObjectiveType.Porozmawiaj, "kesh_dokerka"), Has("lek_bez_zgody"));
            Entry(d, "mara_bez_leku", Objective(ObjectiveType.Porozmawiaj, "kesh_dokerka"));
            Entry(d, "mara_po", Flag("ch1_lek"));
            Entry(d, "mara_po_bez", Flag("ch1_bez_leku"));
            Entry(d, "mara_idle");

            DialogueEffect started = Report(ObjectiveType.Porozmawiaj, "kesh_dokerka_start");
            DialogueEffect back = Report(ObjectiveType.Porozmawiaj, "kesh_dokerka");

            Node(d, "mara_start",
                "Ty jesteś ten nowy? Podnośnik znowu stanął, a zmiana zaraz się zaczyna. Jak nie ruszy, potrącą mi z dniówki. " +
                "A córka... córka kaszle od tygodnia.",
                C("Pokaż ten podnośnik.", "mara_ok", started),
                C("Co z córką?", "mara_corka"));
            Node(d, "mara_corka",
                "Powietrze w barakach. Medyczka mówi, że lek jest, ale na przydział. A przydziały idą najpierw na górę, na Varo.",
                C("Najpierw podnośnik. Potem zobaczymy.", "mara_ok", started));
            Node(d, "mara_ok", "Stoi za mną. Moduł sterujący się zatarł. Wyjmij go, Bren z warsztatu na ulicy go postawi.");
            Node(d, "mara_podnosnik", "Moduł siedzi pod osłoną z boku. Odkręcisz cztery śruby i wyjdzie.");
            Node(d, "mara_serwis", "Warsztat Brena jest w połowie ulicy, między barakami. Nie zgub tego modułu, nie mam na nowy.");
            Node(d, "mara_ambulatorium", "Ambulatorium jest przed pompownią. Powiedz medyczce, że to dla małej Mary.");

            Node(d, "mara_lek_legalny", "Ze stemplem? Z przydziału? (ściska opakowanie) Nikt tu nie dostaje przydziału...",
                C("Dla córki.", "mara_koniec", back, Take("lek_przydzial"), Credits(100), Reputation(SocialClass.Robotnicy, 5),
                    SetFlag("ch1_lek"), SetFlag("ch1_lek_legalny")));
            Node(d, "mara_lek_nielegalny", "Bez stempla? Jak ochrona to u mnie znajdzie... (chowa pod kurtkę) Nieważne. Dziękuję.",
                C("Nikt się nie dowie.", "mara_koniec", back, Take("lek_bez_zgody"), Credits(100), Reputation(SocialClass.Robotnicy, 10),
                    Reputation(SocialClass.Elita, -5), SetFlag("ch1_lek"), SetFlag("ch1_lek_nielegalny")));
            Node(d, "mara_bez_leku", "Nie dali? ...Jasne. Na Kesh nikt nic nie daje.",
                C("Wrócę z lekiem.", null),
                C("Przepraszam. Nie dało się.", "mara_koniec_bez", back, Credits(100), Reputation(SocialClass.Robotnicy, -5), SetFlag("ch1_bez_leku")));

            const string next = "Słuchaj: Brodd na pokładzie sprzedaje stary holownik. Ze statkiem zarobisz więcej niż na nogach. " +
                                "A kapitan portu na doku B szuka ludzi, którym można ufać.";
            Node(d, "mara_koniec", "Za podnośnik i za to... sto kredytów. Więcej nie mam. " + next);
            Node(d, "mara_koniec_bez", "Sto kredytów za podnośnik. Tyle mogę. " + next);
            Node(d, "mara_po", "Córka śpi spokojnie, pierwszy raz od tygodnia.");
            Node(d, "mara_po_bez", "Córka dalej kaszle. Jakoś to będzie. Zawsze jakoś było.");
            Node(d, "mara_idle", "Zmiana się zaczyna. Nie mam czasu.");
            return Finish(d, created);
        }

        private static int Medic()
        {
            var d = Begin("dlg_medyczka", "Medyczka Nadia", out int created);
            Entry(d, "med_start", Objective(ObjectiveType.Porozmawiaj, "kesh_medyczka"));
            // Came back without medicine: the offer stands while Mara still waits
            Entry(d, "med_start", Objective(ObjectiveType.Porozmawiaj, "kesh_dokerka"), HasNot("lek_przydzial"), HasNot("lek_bez_zgody"));
            Entry(d, "med_idle");

            DialogueEffect asked = Report(ObjectiveType.Porozmawiaj, "kesh_medyczka");

            Node(d, "med_start",
                "Lek dla córki dokerki? Jest. Na półce za mną, w skrzynce z plombą Zarządu. Przydział dla Kesh na ten cykl " +
                "już wydany. Reszta leci na Varo.",
                C("Złożę wniosek o przydział. (opłata 60 kr)", "med_przydzial", asked, Credits(-60), Give("lek_przydzial")).If(MinCredits(60)),
                C("Dziecko kaszle od tygodnia. Pani wie, że to nie może czekać do następnego cyklu.", "med_perswazja",
                    asked, Give("lek_przydzial")).Check(PlayerAttribute.Perswazja, 3),
                C("A gdyby jedna skrzynka zginęła w drodze na Varo?", "med_bez_zgody", asked, Give("lek_bez_zgody")),
                C("Nie stać mnie. Powiem Marze.", null, asked),
                C("Wrócę później.", null));

            Node(d, "med_przydzial", "(stempluje formularz) Przydział z rezerwy. Oficjalnie. Niech pan to trzyma przy sobie, ochrona sprawdza.");
            Node(d, "med_perswazja", "(długo milczy) ...Wypiszę z rezerwy awaryjnej. Za darmo. I nie było tej rozmowy.");
            Node(d, "med_bez_zgody",
                "Nic nie słyszałam. Skrzynka stoi przy drzwiach, a ja idę na zaplecze.\n\n(Lek bez przydziału: jeśli ochrona go znajdzie, przepadnie.)");
            Node(d, "med_idle", "Ambulatorium Kesh. Jeśli nie krwawisz, czekaj w kolejce.");
            return Finish(d, created);
        }

        private static int Brodd()
        {
            const string papers = "rejestracja_statku";
            var d = Begin("dlg_brodd", "Handlarz Brodd", out int created);
            Entry(d, "brodd_dlug", Has(papers), Flag("dlug_stocznia"), NoFlag("dlug_splacony"));
            Entry(d, "brodd_wlasciciel", Has(papers));
            Entry(d, "brodd_depozyt", Flag("statek_skonfiskowany"));
            Entry(d, "brodd_start");

            Node(d, "brodd_start",
                "Holownik Wróbel. Stary, ale silniki ma uczciwe. Trzysta kredytów i jest twój. Doki Kesh, ładownia na dwie skrzynie. " +
                "Na inne pierścienie i tak cię nie wpuszczą bez przepustki: straż portowa zabiera statki od ręki.",
                C("Biorę. (300 kr)", "brodd_kupiony", Credits(-300), Give(papers)).If(MinCredits(300)),
                C("Za te silniki? Dwieście dwadzieścia i nie płaczesz.", "brodd_kupiony", Credits(-220), Give(papers))
                    .Check(PlayerAttribute.Perswazja, 3).If(MinCredits(220)),
                C("Osiemdziesiąt teraz, reszta z kursów.", "brodd_raty", Credits(-80), Give(papers), SetFlag("dlug_stocznia")).If(MinCredits(80)),
                C("Jeszcze nie.", null));

            Node(d, "brodd_kupiony",
                "Rejestracja, kody startowe. Stoi na lądowisku obok. W kokpicie Spacja odcumowuje, a dok B jest kawałek dalej po obręczy, " +
                "za końcem ulicy. Wypatruj masztu z pomarańczowym światłem.");
            Node(d, "brodd_raty",
                "Dobra. Ale jak nie spłacisz, znajdę cię nawet na Varo. Reszta: dwieście dwadzieścia. Kapitan portu płaci za uczciwe kursy, zacznij od niego.");
            Node(d, "brodd_dlug", "Reszta za Wróbla: dwieście dwadzieścia. Pamiętam.",
                C("Spłacam. (220 kr)", "brodd_splacone", Credits(-220), SetFlag("dlug_splacony")).If(MinCredits(220)),
                C("Jeszcze nie mam.", null));
            Node(d, "brodd_splacone", "Uczciwy kurier na Kesh. Kto by pomyślał.");
            Node(d, "brodd_wlasciciel", "Lata? To dobrze. Wróbel ma więcej lat niż ty, szanuj go.");
            Node(d, "brodd_depozyt", "Straż portowa przyholowała twojego Wróbla. Depozyt: sto pięćdziesiąt.",
                C("Płacę. (150 kr)", "brodd_odebrany", Credits(-150), Give(papers)).If(MinCredits(150)),
                C("Później.", null));
            Node(d, "brodd_odebrany", "Tym razem miej przepustkę, zanim gdzieś zadokujesz.");
            return Finish(d, created);
        }

        private static int Captain()
        {
            var d = Begin("dlg_kapitan", "Kapitan portu Hale", out int created);
            Entry(d, "kap_raport", Objective(ObjectiveType.Porozmawiaj, "kesh_kapitan"), Has("podmieniona_plomba"));
            Entry(d, "kap_start", Objective(ObjectiveType.Porozmawiaj, "kesh_kapitan"));
            Entry(d, "kap_szukaj", Objective(ObjectiveType.Interakcja, "kesh_b_kontener"));
            Entry(d, "kap_po", Has("przepustka_orrin"));
            Entry(d, "kap_idle");

            DialogueEffect report = Report(ObjectiveType.Porozmawiaj, "kesh_kapitan");

            Node(d, "kap_start",
                "Mara mówi, że można ci ufać. Sprawdzimy. Ktoś podmienia plomby na kontenerach z filtrami powietrza: dobre filtry " +
                "lecą na Orrin, a do baraków trafia złom. Znajdź podmieniony kontener i przynieś mi plombę.",
                C("Załatwione.", "kap_ok", report),
                C("Co z tego będę miał?", "kap_nagroda"));
            Node(d, "kap_nagroda",
                "Przepustkę na Orrin. Prawdziwą, z moim podpisem. Bez niej straż portowa na Orrin zabierze ci statek, zanim wyłączysz silniki.",
                C("Biorę to.", "kap_ok", report));
            Node(d, "kap_ok", "Trzy kontenery przy rampie. Sprawdź plomby. Numery Zarządu mają kreskę pod ostatnią cyfrą.");
            Node(d, "kap_szukaj", "Trzy kontenery przy rampie. Któraś plomba jest podrobiona.");

            Node(d, "kap_raport",
                "(ogląda plombę pod lampą) Podróbka. Dobra robota. Trzymaj: przepustka na Orrin i transport filtrów. " +
                "Zawieź je do portu Orrin i patrz, komu tam zależy na przydziałach.",
                C("Lecę.", "kap_koniec", Take("podmieniona_plomba"), Give("przepustka_orrin"), report),
                C("Za taką robotę należy się coś więcej.", "kap_premia", Take("podmieniona_plomba"), Give("przepustka_orrin"), Credits(50), report)
                    .Check(PlayerAttribute.Perswazja, 2));
            Node(d, "kap_koniec", "Nie zawiedź mnie. Na Orrin wszyscy są uprzejmi. To najgorsze.");
            Node(d, "kap_premia", "Masz tupet. Pięćdziesiąt ekstra. I nie zawiedź mnie.");
            Node(d, "kap_po", "Orrin czeka. Na Orrin wszyscy są uprzejmi. To najgorsze.");
            Node(d, "kap_idle", "Kapitanat Kesh, dok B. Bez sprawy nie zawracaj głowy.");
            return Finish(d, created);
        }

        /// <summary>Decoy containers on dock B: honest seals.</summary>
        private static int Container()
        {
            var d = Begin("dlg_kontener", "Kontener z filtrami", out int created);
            Entry(d, "kont_ok", Objective(ObjectiveType.Interakcja, "kesh_b_kontener"));
            Entry(d, "kont_idle");
            Node(d, "kont_ok", "Plomba Zarządu, kreska pod ostatnią cyfrą, numer zgadza się z listem przewozowym. Ten jest czysty.");
            Node(d, "kont_idle", "Filtry powietrza dla Orrin. Plomba Zarządu.");
            return Finish(d, created);
        }

        // --- Chapter 2: Permit Office ---------------------------------------------------------------

        private static int Sel()
        {
            var d = Begin("dlg_sel", "Urzędniczka Sel", out int created);
            Entry(d, "sel_start", Objective(ObjectiveType.Porozmawiaj, "orrin_urzednik"));
            Entry(d, "sel_raport", Objective(ObjectiveType.Zdobadz, "przepustka_varo"), Flag("ch2_doniesienie"), Has("wyciag_rejestru"));
            Entry(d, "sel_archiwum", Objective(ObjectiveType.Interakcja, "orrin_archiwum"));
            Entry(d, "sel_po", Flag("ch2_sel_oficjalna"));
            Entry(d, "sel_idle");

            DialogueEffect[] talked = { Report(ObjectiveType.Porozmawiaj, "orrin_urzednik") };

            Node(d, "sel_start", "Numerek? Przydziały na Varo wyczerpane do końca cyklu. Następny!",
                C("Wyczerpane? Na targu mówią, że da się je kupić.", "sel_rynek"),
                C("Ręce pani drżą. Ktoś grzebie w rejestrze, prawda?", "sel_prawda",
                    Report(ObjectiveType.Porozmawiaj, "orrin_urzednik"), Give("kod_archiwum"), SetFlag("sel_ufa")).Check(PlayerAttribute.Perswazja, 3),
                C("A gdyby przydział się znalazł? Za fatygę. (100 kr)", "sel_lapowka",
                    Report(ObjectiveType.Porozmawiaj, "orrin_urzednik"), Credits(-100), Give("kod_archiwum"), SetFlag("sel_przekupiona"))
                    .If(MinCredits(100)),
                C("Nieważne.", null));

            Node(d, "sel_rynek",
                "(ścisza głos) Proszę tego nie mówić przy kolejce. Ja tylko stempluję. Rejestr nie zgadza się od miesięcy, " +
                "ale archiwum to nie moja sprawa. Terminal B-12, zaraz za mną. Bez kodu nic pan nie zobaczy.",
                C("To niech mi pani da kod. Nikt się nie dowie, że to od pani.", "sel_prawda",
                    Report(ObjectiveType.Porozmawiaj, "orrin_urzednik"), Give("kod_archiwum"), SetFlag("sel_ufa")).Check(PlayerAttribute.Perswazja, 3),
                C("Poradzę sobie bez kodu.", null, talked));

            Node(d, "sel_prawda",
                "...Dobrze. Cztery, cztery, zero, B. Jeśli ktoś zapyta, stał pan w kolejce jak wszyscy. " +
                "I niech pan uważa na Dorna z targu. Zawsze ma towar, którego nikt inny nie ma.",
                C("Dziękuję.", null));

            Node(d, "sel_lapowka",
                "(chowa kredyty pod formularze) Kod do archiwum leży na biurku. Gdyby pan go przypadkiem zobaczył... Następny!",
                C("Rozumiem.", null));

            Node(d, "sel_archiwum", "Terminal B-12 jest za mną. Ja niczego nie widzę. Następny!");

            Node(d, "sel_raport",
                "Wyciąg z rejestru? Z kluczem Dorna... (długo milczy) Złożę raport. A pan dostanie przydział. Prawdziwy, z moim podpisem.",
                C("Oddaj wyciąg.", "sel_raport_koniec",
                    Take("wyciag_rejestru"), Give("przepustka_varo"), SetFlag("ch2_sel_oficjalna"), Reputation(SocialClass.KlasaSrednia, 5)));

            Node(d, "sel_raport_koniec", "Port Varo przyjmie pana statek. I proszę... niech pan tu nie wraca przez jakiś czas.");
            Node(d, "sel_po", "Raport poszedł do Zarządu. Mam nadzieję, że trafi do właściwych rąk.");
            Node(d, "sel_idle", "Numerek i czekać. Jak wszyscy.");
            return Finish(d, created);
        }

        private static int Archive()
        {
            var d = Begin("dlg_archiwum", "Terminal archiwum B-12", out int created);
            Entry(d, "arch_kod", Objective(ObjectiveType.Interakcja, "orrin_archiwum"), Has("kod_archiwum"));
            Entry(d, "arch_brak", Objective(ObjectiveType.Interakcja, "orrin_archiwum"));
            Entry(d, "arch_idle");

            const string register = "REJESTR PRZYDZIAŁÓW NA VARO: 40 pozycji w cyklu. 31 podpisanych tym samym kluczem: " +
                                    "K. DORN, kwatermistrz targu Orrin.\nKlucz wydany przez: SEKRETARIAT ZARZĄDU STACJI.";

            Node(d, "arch_kod", "Wpisujesz kod: 440B.\n" + register,
                C("Skopiuj wyciąg i wyloguj się.", null,
                    Report(ObjectiveType.Interakcja, "orrin_archiwum"), Take("kod_archiwum"), Give("wyciag_rejestru"), Xp(40)));

            Node(d, "arch_brak", "DOSTĘP ZABLOKOWANY. PODAJ KOD.",
                C("Obejdź zabezpieczenie (włamanie, ktoś może zauważyć).", "arch_wlam", Crime(CrimeType.Wtargniecie))
                    .Check(PlayerAttribute.Technika, 3),
                C("Przeszukaj biurko urzędniczki (kradzież, ktoś może zauważyć).", "arch_biurko", Crime(CrimeType.Kradziez), Give("kod_archiwum")),
                C("Odejdź.", null));

            Node(d, "arch_biurko", "Pod stertą formularzy B-12 leży kartka z czterema znakami: 440B.",
                C("Wpisz kod.", "arch_kod"));

            Node(d, "arch_wlam", "Panel puszcza po minucie. Gdzieś na zapleczu cicho piszczy czujnik.\n" + register,
                C("Skopiuj wyciąg i znikaj.", null,
                    Report(ObjectiveType.Interakcja, "orrin_archiwum"), Give("wyciag_rejestru"), Xp(40)));

            Node(d, "arch_idle", "TERMINAL B-12 · Rejestr przydziałów. Dostęp tylko dla personelu.");
            return Finish(d, created);
        }

        private static int Dorn()
        {
            var d = Begin("dlg_dorn", "Kwatermistrz Dorn", out int created);
            Entry(d, "dorn_start", Objective(ObjectiveType.Porozmawiaj, "orrin_dorn"));
            Entry(d, "dorn_po_wydal", Flag("ch2_dorn_wydal"));
            Entry(d, "dorn_po_sila", Flag("ch2_sila"));
            Entry(d, "dorn_po_donos", Flag("ch2_doniesienie"));
            Entry(d, "dorn_idle");

            DialogueEffect talked = Report(ObjectiveType.Porozmawiaj, "orrin_dorn"); // always first: the permit step must see the item

            Node(d, "dorn_start", "Kurier, co? Filtry, uszczelki, kawa. A może coś... na wyższe piętra?",
                C("Przepustka na Varo. Ile?", "dorn_cena"),
                C("Mam wyciąg z rejestru. Trzydzieści jeden przydziałów z twoim kluczem.", "dorn_wyciag").If(Has("wyciag_rejestru")),
                C("Tylko patrzę.", null));

            Node(d, "dorn_cena", "Dwieście pięćdziesiąt. Prawdziwy stempel, prawdziwy klucz. Nie pytaj czyj.",
                C("Biorę. (250 kr)", "dorn_kupiona", talked, Credits(-250), Give("przepustka_varo"), SetFlag("ch2_kupiona")).If(MinCredits(250)),
                C("A może porozmawiamy o twoim kluczu?", "dorn_wyciag").If(Has("wyciag_rejestru")),
                C("Za drogo.", null));

            Node(d, "dorn_kupiona", "Miłego lotu. I zapomnij, skąd ją masz. Na Varo nie lubią pytań.");

            Node(d, "dorn_wyciag",
                "(blednie) Skąd to masz? Słuchaj... ja tylko sprzedaję. Klucz dostaję z góry, w skrzynkach z próbkami. Nic więcej nie wiem.",
                C("Wiesz. Dasz mi przepustkę i nazwisko, a wyciąg zniknie.", "dorn_nazwisko",
                    talked, Give("przepustka_varo"), SetFlag("ch2_dorn_wydal"), Xp(60)).Check(PlayerAttribute.Perswazja, 4),
                C("(chwytasz go za kołnierz) Przepustka. Teraz.", "dorn_sila",
                    talked, Give("przepustka_varo"), Crime(CrimeType.Napasc), Reputation(SocialClass.KlasaSrednia, -5), SetFlag("ch2_sila"))
                    .Check(PlayerAttribute.Walka, 3),
                C("Zaniosę to Sel. Niech ochrona się tobą zajmie.", "dorn_donos",
                    talked, SetFlag("ch2_doniesienie"), Reputation(SocialClass.KlasaSrednia, 5)),
                C("Jeszcze wrócę.", null));

            Node(d, "dorn_nazwisko",
                "Halvard. Sekretarz Zarządu. On podpisuje, ja sprzedaję, a ludzie z Kesh płacą ostatnimi oszczędnościami " +
                "za miejsce w kapsułach ratunkowych. Teraz zejdź mi z oczu.");
            Node(d, "dorn_sila", "Dobra, dobra! Bierz! (krzyczy) Ochrona! Napad!");
            Node(d, "dorn_donos", "Ty... nie wiesz, z kim zadzierasz. Ten klucz nie jest mój.\n\n(Wróć z wyciągiem do urzędniczki Sel.)");
            Node(d, "dorn_po_wydal", "Znowu ty? Nic więcej nie wiem. Idź już.");
            Node(d, "dorn_po_sila", "Trzymaj się ode mnie z daleka.");
            Node(d, "dorn_po_donos", "Ochrona już tu węszy. Twoja robota, co?");
            Node(d, "dorn_idle", "Filtry, uszczelki, kawa syntetyczna. Kupujesz albo nie zasłaniasz.");
            return Finish(d, created);
        }

        // --- Side quest Orrin: Teo's exam ---------------------------------------------------------

        private static int Teo()
        {
            const string quest = "side_orrin_egzamin";
            var d = Begin("dlg_teo", "Teo", out int created);
            Entry(d, "teo_start", CanStart(quest));
            Entry(d, "teo_czeka", Objective(ObjectiveType.Interakcja, "orrin_szkola_terminal"));
            Entry(d, "teo_daj", Objective(ObjectiveType.Porozmawiaj, "orrin_teo"), Has("odpowiedzi_egzamin"));
            Entry(d, "teo_wynik", Objective(ObjectiveType.Porozmawiaj, "orrin_teo"), Flag("teo_uczciwie"));
            Entry(d, "teo_bez", Objective(ObjectiveType.Porozmawiaj, "orrin_teo"));
            Entry(d, "teo_po_uczciwie", Done(quest), Flag("teo_uczciwie"));
            Entry(d, "teo_po", Done(quest));
            Entry(d, "teo_idle");

            DialogueEffect talk = Report(ObjectiveType.Porozmawiaj, "orrin_teo");

            Node(d, "teo_start",
                "Jutro egzamin z napędów. Jak zdam, dostanę przydział na Varo. Jak nie... zostanę na Orrin na zawsze. Pomożesz mi?",
                C("Co trzeba zrobić?", "teo_plan"),
                C("Nie mam czasu.", null));

            Node(d, "teo_plan", "W szkole stoi terminal z odpowiedziami. Wieczorem nikt go nie pilnuje. Prawie nikt.",
                C("Zdobędę je.", "teo_ok", StartQuest(quest), talk),
                C("Lepiej, żebyś to umiał. Siadaj, przerobimy napędy razem.", "teo_nauka",
                    StartQuest(quest), talk, Report(ObjectiveType.Interakcja, "orrin_szkola_terminal"), SetFlag("teo_uczciwie"))
                    .Check(PlayerAttribute.Technika, 3),
                C("To nie dla mnie.", null));

            Node(d, "teo_ok", "Terminal stoi przy wejściu do szkoły technicznej. Uważaj na ludzi: jak ktoś zobaczy, wezwie ochronę.");
            Node(d, "teo_nauka",
                "(godzinę później) ...czyli sprzężenie idzie przed stabilizatorem. Dobra. Chyba rozumiem. Przyjdź po egzaminie, powiem ci, jak poszło.");
            Node(d, "teo_czeka", "Terminal stoi przy wejściu do szkoły. Pośpiesz się, egzamin jutro.");

            Node(d, "teo_daj", "Masz je? Pokaż!",
                C("Trzymaj.", "teo_koniec_oszust", Take("odpowiedzi_egzamin"), talk),
                C("Zatrzymam je. Zdasz, bo umiesz więcej, niż myślisz.", "teo_koniec_uczciwy",
                    Take("odpowiedzi_egzamin"), SetFlag("teo_uczciwie"), Reputation(SocialClass.KlasaSrednia, 5), talk)
                    .Check(PlayerAttribute.Perswazja, 3));

            Node(d, "teo_koniec_oszust", "Zdam. Na pewno zdam. ...Nie mów nikomu, dobra? Na Varo nie lubią takich jak my.");
            Node(d, "teo_koniec_uczciwy", "Myślisz? ...Dobra. Spróbuję sam. Jak się uda, będę ci winien więcej niż kredyty.");

            Node(d, "teo_wynik", "Zdałem! Sam! Przydział na Varo... Nie wiem, jak ci dziękować.",
                C("Powodzenia na górze.", null, talk));

            Node(d, "teo_bez", "Zabrali ci odpowiedzi? To koniec...",
                C("Nie koniec. Siadaj, nauczę cię.", "teo_nauka", SetFlag("teo_uczciwie")).Check(PlayerAttribute.Technika, 3),
                C("Coś wymyślę.", null));

            Node(d, "teo_po_uczciwie", "Za tydzień lecę na Varo. Pierwszy z naszego bloku.");
            Node(d, "teo_po", "Zdałem. Tylko czasem śni mi się ten terminal.");
            Node(d, "teo_idle", "Jutro egzamin. Nie mam teraz głowy do rozmów.");
            return Finish(d, created);
        }

        // --- Side quest Kesh: Cold pumps -----------------------------------------------------------

        private static int Oren()
        {
            const string quest = "side_kesh_pompy";
            var d = Begin("dlg_oren", "Brygadzista Oren", out int created);
            Entry(d, "oren_start", CanStart(quest));
            Entry(d, "oren_szukaj", Objective(ObjectiveType.Zdobadz, "czesci_pomp"));
            Entry(d, "oren_zanies", Objective(ObjectiveType.Dostarcz, "kesh_pompownia"));
            Entry(d, "oren_po", Done(quest));
            Entry(d, "oren_idle");

            DialogueEffect talk = Report(ObjectiveType.Porozmawiaj, "kesh_brygadzista");

            Node(d, "oren_start",
                "Kurier? Dobrze, że jesteś. Ktoś wynosi części do pomp z magazynu. Bez nich baraki zamarzną za dwie zmiany. " +
                "Trzy skrzynki leżą jeszcze gdzieś między kontenerami, zanim złodziej je zabierze.",
                C("Znajdę je.", "oren_ok", StartQuest(quest), talk),
                C("Znajdę, ale zaliczka się przyda. Paliwo nie jest za darmo.", "oren_zaliczka", StartQuest(quest), talk, Credits(30))
                    .Check(PlayerAttribute.Perswazja, 2),
                C("Kto kradnie?", "oren_kto"),
                C("Nie teraz.", null));

            Node(d, "oren_kto",
                "Gdybym wiedział, już by nie miał rąk. Paser spod rur skupuje wszystko, co da się przetopić. Ale za rękę nikt go nie złapał.",
                C("Znajdę te części.", "oren_ok", StartQuest(quest), talk),
                C("Nie teraz.", null));

            Node(d, "oren_ok", "Trzy skrzynki. Potem zanieś je do zaworu przy pompowni.");
            Node(d, "oren_zaliczka", "(liczy kredyty) Trzydzieści. Więcej nie mam, reszta po robocie. Trzy skrzynki, do zaworu przy pompowni.");
            Node(d, "oren_szukaj", "Szukaj między kontenerami. Trzy skrzynki.");
            Node(d, "oren_zanies", "Masz je? To do zaworu przy pompowni, szybko.");
            Node(d, "oren_po", "Pompy chodzą. Ludzie w barakach mówią o tobie. Dobrze mówią.");
            Node(d, "oren_idle", "Pompy dalej stoją. Zimno w barakach.");
            return Finish(d, created);
        }

        // --- Side quest Varo: Samples from the cold store ------------------------------------------

        private static int Ida()
        {
            const string quest = "side_varo_probki";
            var d = Begin("dlg_ida", "Laborantka Ida", out int created);
            Entry(d, "ida_po_donos", Flag("ida_doniesiona"));
            Entry(d, "ida_start", CanStart(quest));
            Entry(d, "ida_czeka", Objective(ObjectiveType.Interakcja, "varo_chlodnia"));
            Entry(d, "ida_paser", Objective(ObjectiveType.Dostarcz, "kesh_paser"));
            Entry(d, "ida_po", Done(quest));
            Entry(d, "ida_idle");

            DialogueEffect[] accept = { StartQuest(quest), Report(ObjectiveType.Porozmawiaj, "varo_laborantka") };

            Node(d, "ida_start",
                "(rozgląda się) Pan z dołu, prawda? Kurier. W chłodni leżą próbki, których Zarząd nie wpisuje do raportów. " +
                "Na Kesh są ludzie, którzy dużo za nie zapłacą. Ja nie mogę ich wynieść. Pan może.",
                C("Ile?", "ida_ile"),
                C("Co jest w tych próbkach?", "ida_co"),
                C("Zgłoszę to ochronie.", "ida_donos", SetFlag("ida_doniesiona"), Reputation(SocialClass.Elita, 10), Credits(50)),
                C("Nie interesuje mnie to.", null));

            Node(d, "ida_ile", "Sto pięćdziesiąt na rękę od pasera. Elita nie zapomina, ale elita nie schodzi na Kesh.",
                C("Wchodzę w to.", "ida_ok", accept),
                C("Nie.", null));

            Node(d, "ida_co",
                "Wyniki badań powietrza z dolnych pierścieni. Prawdziwe wyniki. Ludzie na Kesh oddychają czymś, czego nie ma w raportach.",
                C("Wchodzę w to.", "ida_ok", accept),
                C("Nie.", null));

            Node(d, "ida_ok", "Chłodnia stoi przy laboratorium. Kamery patrzą na ulicę, nie na chłodnię. Zwykle.");
            Node(d, "ida_donos", "(blednie) Nie... Proszę... (za tobą słychać kroki ochrony)");
            Node(d, "ida_czeka", "Proszę się pośpieszyć. Zmiana ochrony za pół godziny.");
            Node(d, "ida_paser", "Ma je pan? To na Kesh, do pasera. I proszę tu nie wracać przez jakiś czas.");
            Node(d, "ida_po", "Na Kesh podobno mówią o próbkach. Dobrze. Niech mówią.");
            Node(d, "ida_po_donos", "(Ida patrzy przez ciebie, jakby cię tu nie było.)");
            Node(d, "ida_idle", "Proszę nie dotykać niczego w laboratorium.");
            return Finish(d, created);
        }

        // --- Small builders ---------------------------------------------------------------------------

        private static DialogueDefinition Begin(string id, string speaker, out int created)
        {
            string path = $"{Folder}/Dialogue_{id}.asset";
            var d = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(path);
            created = 0;
            if (d == null)
            {
                d = ScriptableObject.CreateInstance<DialogueDefinition>();
                ArtPalette.EnsureFolder(Folder);
                AssetDatabase.CreateAsset(d, path);
                created = 1;
            }
            d.id = id;
            d.speaker = speaker;
            d.entries = new List<DialogueEntry>();
            d.nodes = new List<DialogueNode>();
            return d;
        }

        private static int Finish(DialogueDefinition d, int created)
        {
            EditorUtility.SetDirty(d);
            return created;
        }

        private static void Entry(DialogueDefinition d, string node, params DialogueCondition[] conditions) =>
            d.entries.Add(new DialogueEntry { node = node, conditions = new List<DialogueCondition>(conditions) });

        private static void Node(DialogueDefinition d, string id, string text, params DialogueChoice[] choices) =>
            d.nodes.Add(new DialogueNode { id = id, text = text, choices = new List<DialogueChoice>(choices) });

        private static DialogueChoice C(string text, string next, params DialogueEffect[] effects) =>
            new DialogueChoice { text = text, next = next ?? "", effects = new List<DialogueEffect>(effects) };

        private static DialogueChoice Check(this DialogueChoice choice, PlayerAttribute attribute, int difficulty)
        {
            choice.check = true;
            choice.attribute = attribute;
            choice.difficulty = difficulty;
            return choice;
        }

        private static DialogueChoice If(this DialogueChoice choice, params DialogueCondition[] conditions)
        {
            choice.conditions.AddRange(conditions);
            return choice;
        }

        // Conditions
        private static DialogueCondition Objective(ObjectiveType type, string id) => new DialogueCondition { type = ConditionType.CelAktywny, objective = type, id = id };
        private static DialogueCondition CanStart(string quest) => new DialogueCondition { type = ConditionType.ZadanieMoznaRozpoczac, id = quest };
        private static DialogueCondition Done(string quest) => new DialogueCondition { type = ConditionType.ZadanieUkonczone, id = quest };
        private static DialogueCondition Has(string item) => new DialogueCondition { type = ConditionType.MaPrzedmiot, id = item, value = 1 };
        private static DialogueCondition Flag(string flag) => new DialogueCondition { type = ConditionType.FlagaUstawiona, id = flag };
        private static DialogueCondition NoFlag(string flag) => new DialogueCondition { type = ConditionType.FlagaNieustawiona, id = flag };
        private static DialogueCondition HasNot(string item) => new DialogueCondition { type = ConditionType.NieMaPrzedmiotu, id = item, value = 1 };
        private static DialogueCondition MinCredits(int amount) => new DialogueCondition { type = ConditionType.KredytyCoNajmniej, value = amount };

        // Effects
        private static DialogueEffect StartQuest(string quest) => new DialogueEffect { type = EffectType.RozpocznijZadanie, id = quest };
        private static DialogueEffect Report(ObjectiveType type, string id) => new DialogueEffect { type = EffectType.ZglosCel, objective = type, id = id };
        private static DialogueEffect Give(string item, int count = 1) => new DialogueEffect { type = EffectType.DajPrzedmiot, id = item, value = count };
        private static DialogueEffect Take(string item, int count = 1) => new DialogueEffect { type = EffectType.ZabierzPrzedmiot, id = item, value = count };
        private static DialogueEffect Credits(int amount) => new DialogueEffect { type = EffectType.Kredyty, value = amount };
        private static DialogueEffect Reputation(SocialClass socialClass, int amount) => new DialogueEffect { type = EffectType.Reputacja, socialClass = socialClass, value = amount };
        private static DialogueEffect SetFlag(string flag) => new DialogueEffect { type = EffectType.UstawFlage, id = flag };
        private static DialogueEffect Xp(int amount) => new DialogueEffect { type = EffectType.Doswiadczenie, value = amount };
        private static DialogueEffect Crime(CrimeType crime) => new DialogueEffect { type = EffectType.Przestepstwo, crime = crime };
    }
}
