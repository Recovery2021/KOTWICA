using System.IO;
using Kotwica.Content;
using UnityEditor;
using UnityEngine;

namespace Kotwica.EditorTools
{
    /// <summary>
    /// Menu helpers for the editable content files (Assets/StreamingAssets/Kotwica):
    /// check them for mistakes and open the folder.
    /// </summary>
    public static class ContentTools
    {
        [MenuItem("Kotwica/Zadania: sprawdź pliki")]
        public static void Check()
        {
            ContentLibrary.Reload(); // also logs every problem to the Console
            int problems = ContentLibrary.Problems.Count;
            string summary = $"Zadania: {ContentLibrary.Quests.Count}\nRozmowy: {ContentLibrary.Dialogues.Count}\nPrzedmioty: {ContentLibrary.Items.Count}\n\n";
            if (problems == 0) summary += "Nie znaleziono problemów.";
            else
            {
                summary += $"Problemy: {problems} (pełna lista w Console):\n";
                for (int i = 0; i < Mathf.Min(problems, 12); i++) summary += "• " + ContentLibrary.Problems[i] + "\n";
            }
            EditorUtility.DisplayDialog("Kotwica: pliki zadań", summary, "OK");
        }

        [MenuItem("Kotwica/Zadania: otwórz folder")]
        public static void OpenFolder()
        {
            string path = ContentLibrary.RootPath;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(Path.Combine(path, "przedmioty.json"));
        }
    }
}
