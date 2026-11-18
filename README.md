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

## Pierwsze uruchomienie na nowym komputerze
1. Zainstaluj [Git](https://git-scm.com/download/win) (z Git LFS) lub GitHub Desktop.
2. Sklonuj `Recovery2021/KOTWICA`.
3. W Unity Hub: **Add → Add project from disk** i wskaż sklonowany folder (Unity 6000.3.17f1).
