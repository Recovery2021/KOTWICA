using System;
using System.Collections.Generic;

namespace Kotwica.Content
{
    // Shapes of the editable JSON files in StreamingAssets/Kotwica (Polish keys, read with JsonUtility).
    // Enum values are written as text (e.g. "Porozmawiaj", "Perswazja") and parsed by ContentLibrary.
    // A file may hold any of the three lists; all *.json files under the folder are read.

    /// <summary>One JSON file: quests, conversations and/or items.</summary>
    [Serializable]
    public class ContentFile
    {
        public List<QuestData> zadania = new List<QuestData>();
        public List<DialogueData> rozmowy = new List<DialogueData>();
        public List<ItemData> przedmioty = new List<ItemData>();
    }

    [Serializable]
    public class QuestData
    {
        public string id;
        public string tytul;
        public string opis;
        public string rodzaj = "Poboczny";          // Glowny / Poboczny / Aktywnosc
        public string pierscien;                    // kesh / orrin / varo
        public int minPoziom = 1;
        public List<string> wymagane = new List<string>();
        public bool autostart;
        public bool powtarzalne;
        public List<ObjectiveData> cele = new List<ObjectiveData>();
        public RewardData nagroda = new RewardData();
    }

    [Serializable]
    public class ObjectiveData
    {
        public string typ;                          // Idz / Interakcja / Porozmawiaj / Dostarcz / Zdobadz / Lec ...
        public string cel;                          // id from the scene (or item id for Zdobadz, port_<id> for Lec)
        public int ilosc = 1;
        public string opis;
    }

    [Serializable]
    public class RewardData
    {
        public int kredyty;
        public int xp;
        public List<ReputationData> reputacja = new List<ReputationData>();
        public List<ItemCountData> przedmioty = new List<ItemCountData>();
    }

    [Serializable]
    public class ReputationData
    {
        public string klasa;                        // Robotnicy / KlasaSrednia / Elita
        public int wartosc;
    }

    [Serializable]
    public class ItemCountData
    {
        public string id;
        public int ilosc = 1;
    }

    [Serializable]
    public class ItemData
    {
        public string id;
        public string nazwa;
        public string opis;
        public string kategoria = "Inne";           // Ladunek / Medykament / Narzedzie / Bron / Dokument / Inne
        public int wartosc;
        public bool nielegalny;                     // contraband: confiscated when arrested
    }

    [Serializable]
    public class DialogueData
    {
        public string id;
        public string rozmowca;
        public List<EntryData> wejscia = new List<EntryData>();
        public List<NodeData> wezly = new List<NodeData>();
    }

    [Serializable]
    public class EntryData
    {
        public string wezel;
        public List<ConditionData> warunki = new List<ConditionData>();
    }

    [Serializable]
    public class NodeData
    {
        public string id;
        public string mowca;
        public string tekst;
        public List<ChoiceData> wybory = new List<ChoiceData>();
    }

    [Serializable]
    public class ChoiceData
    {
        public string tekst;
        public string dalej;
        public List<ConditionData> warunki = new List<ConditionData>();
        public CheckData test = new CheckData();
        public List<EffectData> efekty = new List<EffectData>();
    }

    [Serializable]
    public class CheckData
    {
        public string atrybut;                      // empty = no check; Sprawnosc / Walka / Technika / Pilotaz / Perswazja
        public int trudnosc;
    }

    [Serializable]
    public class ConditionData
    {
        public string typ;
        public string cel;                          // objective type for CelAktywny
        public string id;
        public int wartosc;
    }

    [Serializable]
    public class EffectData
    {
        public string typ;
        public string cel;                          // objective type for ZglosCel
        public string id;
        public int wartosc;
        public string klasa;                        // for Reputacja
        public string atrybut;                      // for Praktyka
        public string przestepstwo;                 // for Przestepstwo
    }
}
