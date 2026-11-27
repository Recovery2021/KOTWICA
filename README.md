# KOTWICA (tytuł roboczy)

Fabularne RPG akcji 3D sci-fi (TPP) w Unity / C#.
Jesteś kurierem i „załatwiaczem” na umierającej stacji z krążących pierścieni habitatów nad gazowym olbrzymem Morwen.

Dokument wizji: [docs/vision.md](docs/vision.md)

## Technologia
- Unity **6000.3.17f1** (Unity 6), URP
- C#, krótkie komentarze w kodzie wyjaśniające cel
- Modele 3D: Blender (pliki źródłowe w `Blender/`, eksport FBX do `Assets/_Project/Art/Models/`)
- Darmowe assety tam, gdzie się da (np. tekstury CC0), zawsze dopasowane do klimatu gry
- Duże pliki binarne (modele, tekstury, dźwięk) przez Git LFS

## Struktura
```
Assets/_Project/
  Scripts/
    Core/       wejście (klawiatura/mysz), interfejs interakcji
    Orbits/     czas orbitalny, pierścienie, ruchomy układ odniesienia stacji
    Player/     postać TPP, interakcje, kamera
    Ship/       lot statkiem, dokowanie, wsiadanie
    Gameplay/   zlecenia kurierskie, stan gry
    Combat/     zdrowie, broń gracza, AI wrogów, osłony, śluza
    Characters/ wzory postaci, model + animator w miejsce kapsuł
    World/      dane pierścieni, klasy społeczne, doczytywanie dzielnic
    Progression/ poziomy, atrybuty, reputacja
    Items/      przedmioty, ekwipunek, kontrabanda
    Quests/     zadania jako dane, dziennik, cele w scenie
    Security/   poziom alarmu
    SaveLoad/   zapis gry do JSON
    Environment/ efekty otoczenia (pulsujące światła)
    UI/         prototypowy HUD
    Editor/     generator sceny prototypu
  Scenes/  Prefabs/  Art/  Audio/  Settings/
Blender/        pliki .blend
docs/           dokumentacja projektu
```

## Prototyp 01: „Jedno zlecenie” (graybox)
1. Otwórz projekt w Unity 6000.3.17f1.
2. Menu **Kotwica → Zbuduj scenę Prototyp 01**. Scena zapisze się w `Assets/_Project/Scenes/Prototype01.unity`.
3. Play.

Pętla do sprawdzenia: tablica zleceń (F) → wejście do statku (F) → Spacja odcumowuje → lot do drugiego pierścienia za znacznikiem → X dopasowuje prędkość do celu → F dokuje (poniżej 4 m/s) → F wyjście → terminal dostaw (F) → wypłata.

Sterowanie pieszo: WASD, Shift bieg, Spacja skok, F interakcja, Esc zwalnia kursor.
Sterowanie statkiem: W/S ciąg, A/D na boki, Spacja/Ctrl góra/dół, mysz obrót, Q/E przechył, X dopasowanie prędkości.

Jak to działa: pierścień, na którym ostatnio zadokowałeś, stoi w miejscu w środku sceny, a reszta stacji i planeta poruszają się względem niego (`StationFrame`). Dzięki temu chodzenie po pokładzie to zwykła fizyka, a współrzędne są małe.

## Test klimatu: korytarz z Blendera
1. Blender: Scripting → Open → `Blender/Scripts/kotwica_kit_corridor.py` → Run Script (eksportuje 13 modułów do `Assets/_Project/Art/Models/Kit_Corridor`).
2. Unity: **Kotwica → Zbuduj scenę testu klimatu**.

## Prototyp 02: walka w korytarzu
1. Unity: **Kotwica → Zbuduj scenę Prototyp 02 (walka)** (wymaga modeli z testu klimatu).
2. Play. Trzech strażników patroluje korytarz; kucnij (C), żeby podchodzić niezauważony i chować się za skrzyniami.
3. Konsola na prawej ścianie (F) uruchamia 3-sekundowe odliczanie śluzy: złap żółty uchwyt na ścianie (F), bo dekompresja wyssie każdego, kto się nie trzyma. Na uchwycie możesz celować i strzelać.

Sterowanie: WASD, Shift bieg, C kucanie, PPM celowanie zza ramienia, LPM strzał, R przeładowanie / restart po walce, F interakcja.

## Postacie i kreator (krok 3)
Instrukcja pobrania postaci i animacji z Mixamo: [docs/characters.md](docs/characters.md).
Potem: **Kotwica → Postacie: przygotuj wzory i animacje**, **Kotwica → Zbuduj scenę kreatora postaci**, **Kotwica → Zbuduj scenę Prototyp 02 (walka)**, Play w `CharacterCreator`.

## Fundamenty otwartego świata (krok 4)
Systemy działające w każdej scenie (tworzą się same): rozwój postaci, reputacja u trzech klas, ekwipunek, dziennik zadań, poziom alarmu, zapis gry.
1. **Kotwica → Otwarty świat: utwórz dane startowe** (3 pierścienie, przedmioty, zadania w `Assets/_Project/Resources`).
2. Play w dowolnej scenie. **Tab**: postać / dziennik / ekwipunek / reputacja. **F5** szybki zapis, **F9** wczytanie.
3. Rozdział 1 wątku głównego to misja „Kurs powrotny” (Prototyp 01) i postępuje sam, gdy ją grasz.

Nowe zadanie: prawy klik w `Resources/Quests` → Create → Kotwica → Zadanie. W scenie cele zgłaszają: `QuestTarget` (F), `QuestArea` (wejście w strefę), `QuestGiver` (przyjęcie zlecenia).

## Struktura świata (krok 5)
Scena kosmosu (Prototyp 01) ma trzy pierścienie: Kesh (robotnicy), Orrin (klasa średnia), Varo (elita). Dzielnice każdego pierścienia to osobna scena (`Scenes/Rings/Ring_*.unity`), doczytywana w tle, gdy zbliżasz się do pierścienia albo na nim dokujesz (`RingStreamer`).
1. **Kotwica → Otwarty świat: utwórz dane startowe** (jeśli jeszcze nie).
2. **Kotwica → Świat: zbuduj dzielnice pierścieni**.
3. **Kotwica → Zbuduj scenę Prototyp 01**, Play. Z pokładu portu idź na wschód (ulica dzielnicy).
Porty Kesh i Orrin są otwarte; Varo wymaga zlecenia albo przepustki (rozdział 2 na Orrin).

## Mieszkańcy i zegar stacji (krok 6)
Doba stacji trwa 24 minuty (1 godzina = 1 minuta, start 07:00, godzina w prawym panelu). Mieszkańcy dzielnic (Kesh 10, Orrin 10, Varo 6) żyją według ról: dom → praca / nauka → odpoczynek → dom; w domu znikają w budynku. Przy alarmie 2+ uciekają do domów. Przestępstwo widziane przez mieszkańca liczy się w całości.
Role (godziny, kwestie) to pliki w `Assets/_Project/Data/Roles`. Po zmianach: **Kotwica → Świat: zbuduj dzielnice pierścieni**.

## Ochrona i alarm (krok 7)
Przestępstwo przy świadkach podnosi alarm pierścienia (0–5). Z posterunków wychodzą strażnicy (Kesh 2, Orrin 3, Varo 5), od poziomu 3 (Kesh 4) strzelają. Złapanie = mandat (w Kesh łapówka przy Perswazji 3+) i konfiskata kontrabandy. Śmierć = ambulatorium za 25% kredytów. Alarm 3+ zamyka port dla dokowania, 4+ blokuje start. Strzały wywołują panikę mieszkańców. Kod: `Scripts/Security`.

## Rozmowy, rozdział 2 i pojazdy (krok 8)
**Rozmowy z wyborami:** rozmowa to dane (`Resources/Dialogues`, typ *Kotwica/Rozmowa*): wejścia zależne od stanu zadań, węzły z kwestiami i wybory, które mogą wymagać atrybutu (np. *Perswazja 3*: widoczny, ale zablokowany, gdy masz za mało), uruchamiać zadania, zgłaszać cele, dawać/zabierać przedmioty i kredyty, zmieniać reputację, ustawiać flagi fabularne (zapisywane) i zgłaszać przestępstwa. Klawisze 1–9 lub klik, Esc kończy. Treść rozmów jest w `Editor/DialogueDataBuilder.cs` i odświeża się przy każdym uruchomieniu **Otwarty świat: utwórz dane startowe**.
**Rozdział 2 „Urząd Przepustek” (Orrin):** urzędniczka Sel → terminal archiwum B-12 (kod od Sel za Perswazję/łapówkę, włamanie za Technikę albo kradzież kodu z biurka) → kwatermistrz Dorn na targu (kupno 250 kr, Perswazja 4, zastraszenie Walka 3 albo donos do Sel) → przepustka → dokowanie na Varo. Potem startuje szkielet rozdziału 3.
**Zadania poboczne:** *Egzamin Teo* (Orrin, nowe), *Zimne pompy* (Kesh) i *Próbki z chłodni* (Varo) z rozmowami i wyborami.
**Pojazdy:** wózek towarowy (Kesh, wolny), skuter kurierski (Orrin, cudzy) i kabriolet lewitujący (Varo, cudzy). F wsiada (cudzy pojazd = kradzież przy świadkach), W/S gaz, A/D skręt, Spacja hamulec, F wysiada. Potrącenie to napaść. Aresztowanie lub śmierć wyrzuca z pojazdu.

## Pierwsze uruchomienie na nowym komputerze
1. Zainstaluj [Git](https://git-scm.com/download/win) (z Git LFS) lub GitHub Desktop.
2. Sklonuj `Recovery2021/KOTWICA`.
3. W Unity Hub: **Add → Add project from disk** i wskaż sklonowany folder (Unity 6000.3.17f1).
