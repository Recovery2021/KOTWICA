# Jak edytować zadania, rozmowy i przedmioty

Cała treść fabularna gry jest w plikach JSON w tym folderze. Gra czyta je przy każdym starcie
(w Unity: przy każdym Play). Nie trzeba niczego przebudowywać.

- `przedmioty.json`: wszystkie przedmioty
- `zadania/*.json`: zadania i ich rozmowy, jeden plik na rozdział, zadanie poboczne albo zestaw zleceń

Gra czyta każdy plik `.json` w tym folderze i jego podfolderach. Nowy plik możesz więc po prostu dodać.
Każdy plik może zawierać listy `"zadania"`, `"rozmowy"` i `"przedmioty"`, w dowolnym połączeniu.

## Jak pracować

1. Otwórz plik w edytorze tekstu (np. VS Code: od razu podświetli błędy składni JSON).
2. Zmień tekst albo dopisz nowe zadanie.
3. W Unity: **Kotwica → Zadania: sprawdź pliki**. Pokaże literówki w identyfikatorach, nieznane węzły
   rozmów, nieznane słowa (np. `Porozmawjaj`) i brakujące przedmioty. Te same ostrzeżenia trafiają do Console przy Play.
4. Play. Zacznij nową grę, jeśli zmieniłeś kolejność albo liczbę celów w zadaniu, które jest w zapisie.

W gotowej grze ten folder leży w `NazwaGry_Data/StreamingAssets/Kotwica` i też można go edytować.

Pułapki JSON: przecinek między elementami (ale nie po ostatnim), cudzysłowy proste `"`, żadnych komentarzy.
Polskie znaki są w porządku (plik w UTF-8).

## Zadanie

```json
{
  "id": "job_kesh_paczka",               // unikalne, bez spacji i polskich znaków
  "tytul": "Zlecenie: paczka do baraków",
  "opis": "Tekst w dzienniku.",
  "rodzaj": "Aktywnosc",                  // Glowny / Poboczny / Aktywnosc
  "pierscien": "kesh",                    // kesh / orrin / varo
  "minPoziom": 1,
  "wymagane": ["main_01_kurs_powrotny"],  // zadania, które muszą być ukończone (można pominąć)
  "autostart": false,                     // true = startuje samo, gdy spełnione wymagania (rozdziały)
  "powtarzalne": true,                    // można brać wiele razy (zlecenia)
  "cele": [
    { "typ": "Zdobadz", "cel": "paczka_kesh", "ilosc": 1, "opis": "Odbierz paczkę z magazynu" },
    { "typ": "Dostarcz", "cel": "kesh_barak", "ilosc": 1, "opis": "Zostaw paczkę w skrzynce baraku 7" }
  ],
  "nagroda": { "kredyty": 30, "xp": 20,
               "reputacja": [ { "klasa": "Robotnicy", "wartosc": 5 } ],
               "przedmioty": [ { "id": "filtr_powietrza", "ilosc": 1 } ] }
}
```
(Komentarze `//` są tu tylko dla wyjaśnienia. W prawdziwym pliku ich nie wpisuj.)

Typy celów:
| typ | co trzeba zrobić | `cel` to |
|---|---|---|
| `Idz` | wejść w strefę | id strefy w scenie |
| `Interakcja` | użyć obiektu (F) | id obiektu w scenie |
| `Porozmawiaj` | rozmowa | id zgłaszane przez rozmowę (efekt `ZglosCel`) |
| `Dostarcz` | oddać ładunek przy obiekcie | id obiektu w scenie |
| `Zdobadz` | mieć przedmioty w ekwipunku | id przedmiotu |
| `Lec` | zadokować | `port_kesh`, `port_kesh_b`, `port_orrin`, `port_orrin_b`, `port_varo`, `port_varo_b` |

Obiekty i strefy w scenach (id do celów): na Kesh `kesh_podnosnik`, `kesh_serwis`, `kesh_pompownia`, `kesh_paser`,
`kesh_barak`, `kesh_pompownia_filtry`, `kesh_magazyn`, `kesh_b_rampa`, `kesh_b_kontener`; punkty odbioru zleceń
`paczka_kesh`, `wklady_filtrow`, `skrzynia_dok_b`, `skrzynia_z_doku_b` (cel `Zdobadz`). Na Orrin `orrin_urzad`, `orrin_szkola_terminal`,
`orrin_targ`, punkt odbioru `towar_orrin`. Na Varo `varo_chlodnia`, `varo_zarzad`.
Nowe miejsce w scenie (obiekt, strefę, postać) dodaje się w Unity albo przez kreator dzielnic (poproś Claude).

## Przedmiot

```json
{ "id": "paczka_kesh", "nazwa": "Paczka do baraków", "opis": "...", "kategoria": "Ladunek", "wartosc": 0, "nielegalny": false }
```
Kategorie: `Ladunek`, `Medykament`, `Narzedzie`, `Bron`, `Dokument`, `Inne`. Przedmiot `"nielegalny": true` zabiera ochrona przy zatrzymaniu.
Przepustki na pierścienie to zwykłe przedmioty: `przepustka_orrin`, `przepustka_varo`. Statek gracza: `rejestracja_statku`.

## Rozmowa

```json
{
  "id": "dlg_mara",
  "rozmowca": "Dokerka Mara",
  "wejscia": [
    { "wezel": "mara_start", "warunki": [ { "typ": "CelAktywny", "cel": "Porozmawiaj", "id": "kesh_dokerka_start" } ] },
    { "wezel": "mara_idle" }
  ],
  "wezly": [
    { "id": "mara_start", "tekst": "Ty jesteś ten nowy?",
      "wybory": [
        { "tekst": "Pokaż ten podnośnik.", "dalej": "mara_ok",
          "efekty": [ { "typ": "ZglosCel", "cel": "Porozmawiaj", "id": "kesh_dokerka_start" } ] },
        { "tekst": "Przekonaj ją.", "dalej": "mara_ok",
          "test": { "atrybut": "Perswazja", "trudnosc": 3 } },
        { "tekst": "Do widzenia.", "dalej": "" }
      ] },
    { "id": "mara_idle", "tekst": "Nie mam czasu.", "wybory": [] }
  ]
}
```
- **wejscia**: gra bierze pierwsze wejście, którego wszystkie warunki są spełnione. Ostatnie wejście zostaw bez warunków (kwestia domyślna).
- **dalej**: id następnego węzła; `""` kończy rozmowę. Węzeł bez wyborów pokazuje przycisk „Dalej” i kończy rozmowę.
- **test**: atrybut i próg. Wybór widać zawsze, ale jest zablokowany, gdy gracz ma za mało. Udany test trenuje atrybut.
  Atrybuty: `Sprawnosc`, `Walka`, `Technika`, `Pilotaz`, `Perswazja`.
- **warunki** wyboru: wybór jest ukryty, dopóki wszystkie nie są spełnione.
- **efekty** wykonują się po kolei. Ważne: `ZglosCel` daj **przed** `DajPrzedmiot`, jeśli następny cel zadania to `Zdobadz` tego przedmiotu.
- `mowca` w węźle (opcjonalny) zmienia imię mówiącego tylko w tym węźle.

Warunki (`typ`, potrzebne pola):
| typ | pola | znaczenie |
|---|---|---|
| `CelAktywny` | `cel` (typ celu), `id` | któreś aktywne zadanie ma teraz taki cel |
| `ZadanieMoznaRozpoczac` | `id` | zadanie można teraz wziąć |
| `ZadanieAktywne` / `ZadanieUkonczone` | `id` | stan zadania |
| `MaPrzedmiot` / `NieMaPrzedmiotu` | `id`, `wartosc` (ile, domyślnie 1) | ekwipunek |
| `FlagaUstawiona` / `FlagaNieustawiona` | `id` | pamięć wyborów gracza (dowolna nazwa) |
| `KredytyCoNajmniej` | `wartosc` | stan konta |

Efekty (`typ`, potrzebne pola):
| typ | pola |
|---|---|
| `RozpocznijZadanie` / `OblejZadanie` | `id` |
| `ZglosCel` | `cel` (typ celu), `id` |
| `DajPrzedmiot` / `ZabierzPrzedmiot` | `id`, `wartosc` (ile) |
| `Kredyty` | `wartosc` (ujemna = płaci gracz) |
| `Reputacja` | `klasa` (`Robotnicy` / `KlasaSrednia` / `Elita`), `wartosc` |
| `UstawFlage` | `id` |
| `Doswiadczenie` | `wartosc` |
| `Praktyka` | `atrybut`, `wartosc` |
| `Przestepstwo` | `przestepstwo` (`Wtargniecie`, `Kradziez`, `Przemyt`, `Napasc`, `Zabojstwo`, `Strzelanina`); widziane, jeśli ktoś patrzy |

Postać albo obiekt w scenie wskazuje swoją rozmowę przez id (pole `dialogueId` w komponencie QuestNpc / DialogueObject).
Obecne rozmowy: `dlg_mara`, `dlg_medyczka`, `dlg_brodd`, `dlg_kapitan`, `dlg_kontener`, `dlg_tablica_kesh`, `dlg_oren`,
`dlg_sel`, `dlg_archiwum`, `dlg_dorn`, `dlg_teo`, `dlg_tablica_orrin`, `dlg_ida`.
