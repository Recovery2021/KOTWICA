# KOTWICA (tytuł roboczy)

Fabularne RPG akcji 3D sci-fi (TPP) w Unity / C#.
Jesteś kurierem i „załatwiaczem” na umierającej stacji z krążących pierścieni habitatów nad gazowym olbrzymem Morwen.

Dokument wizji: [docs/vision.md](docs/vision.md)

## Technologia
- Unity (URP), ta sama wersja co w projekcie „gra kosmiczna Unity C#”
- C#, krótkie komentarze w kodzie wyjaśniające cel
- Modele 3D: Blender (pliki źródłowe w `Blender/`, eksport FBX do `Assets/_Project/Art/Models/`)
- Darmowe assety tam, gdzie się da (np. tekstury CC0), zawsze dopasowane do klimatu gry
- Duże pliki binarne (modele, tekstury, dźwięk) przez Git LFS

## Struktura
```
Assets/_Project/        wszystko, co nasze (oddzielone od paczek z Asset Store)
  Scripts/Orbits/       ruch orbitalny pierścieni (OrbitalClock, OrbitalRing)
  Scenes/  Prefabs/  Art/  Audio/  Settings/
Blender/                pliki .blend
docs/                   dokumentacja projektu
```

## Pierwsze uruchomienie na komputerze z Unity (Windows)
1. Zainstaluj [Git](https://git-scm.com/download/win) (zawiera Git LFS), potem w terminalu: `git lfs install`
2. W Unity Hub utwórz nowy projekt **Universal 3D (URP)** w folderze `E:\Unity\Kotwica`. Zamknij Unity.
3. W terminalu:
   ```
   cd E:\Unity\Kotwica
   git init -b main
   git remote add origin https://github.com/<TWOJ_LOGIN>/Kotwica.git
   git fetch origin
   git checkout -t origin/main
   git add .
   git commit -m "Unity URP project files"
   git push
   ```
4. Otwórz projekt w Unity Hub. Skrypty pojawią się w `Assets/_Project/Scripts`.

## Szybki test orbit
1. Pusty obiekt `OrbitalClock` + komponent `OrbitalClock` (Time Scale np. 20).
2. Kula = planeta w (0,0,0).
3. Dwa torusy/cylindry z komponentem `OrbitalRing`, różny `Orbit Radius` (np. 2000 i 2300) i `Orbital Period` (600 i 750).
4. Play: pierścienie zmieniają położenie względem siebie.
