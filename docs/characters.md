# Postacie: pobieranie z Mixamo i konfiguracja

Mixamo (mixamo.com, darmowe konto Adobe) daje postacie i animacje do użytku w grach bez opłat licencyjnych.
Licencja zabrania udostępniania surowych plików, więc repozytorium musi pozostać PRYWATNE.

## 1. Postacie gracza (3–4 wzory) → `Assets/_Project/Art/Characters/Models/`
- Zakładka **Characters**, wybierz postacie pasujące do klimatu: technicy, robotnicy, ludzie w kombinezonach. Bez fantasy i kreskówek.
- **Download**: Format **FBX for Unity (.fbx)**, Pose **T-pose**.
- Nazwa pliku = nazwa wzoru w kreatorze (np. `Technik_Ana.fbx`, `Doker_Rel.fbx`).

## 2. Wróg (1) → `Assets/_Project/Art/Characters/Enemies/`
- Postać w pancerzu lub mundurze, te same ustawienia.

## 3. Animacje → `Assets/_Project/Art/Characters/Animations/`
Ustawienia pobierania: **FBX for Unity**, **Skin: Without Skin**, **30 fps**, zaznacz **In Place**, jeśli jest dostępne.
Wybieraj wersje z karabinem w rękach (spójny wygląd). Nazwy plików MUSZĄ być dokładnie takie:

| Plik | Czego szukać w Mixamo |
| --- | --- |
| Idle.fbx | rifle idle |
| Walk.fbx | rifle walk (do przodu) |
| Run.fbx | rifle run |
| CrouchIdle.fbx | crouch idle (rifle) |
| CrouchWalk.fbx | crouch walk / crouched walking (rifle) |
| AimIdle.fbx | rifle aiming idle |
| AimWalkForward.fbx | walking forward while aiming (rifle) |
| AimWalkBack.fbx | walking backwards (rifle) |
| AimStrafeLeft.fbx | strafe left (rifle) |
| AimStrafeRight.fbx | strafe right (rifle) |
| Death.fbx | rifle death / dying |

Brakujące animacje są zastępowane podobnymi, więc gra działa nawet z niepełnym zestawem. Minimum to Idle.fbx.

## 4. W Unity
1. **Kotwica → Postacie: przygotuj wzory i animacje** (animator, wzory, lista postaci).
2. **Kotwica → Zbuduj scenę kreatora postaci**.
3. **Kotwica → Zbuduj scenę Prototyp 02 (walka)**.
4. Otwórz `Scenes/CharacterCreator`, Play: wybierz wzór, wpisz imię, ROZPOCZNIJ.

Ponowne pobranie lub dodanie postaci: wrzuć plik i powtórz krok 1.
