using System.Collections.Generic;
using UnityEngine;

namespace Kotwica.Characters
{
    /// <summary>
    /// All character presets of the game in one place: player looks for the creator,
    /// the enemy look, the shared animator and the weapon prop held in the right hand.
    /// </summary>
    [CreateAssetMenu(menuName = "Kotwica/Postacie - lista", fileName = "CharacterRoster")]
    public class CharacterRoster : ScriptableObject
    {
        public const string ResourcePath = "CharacterRoster"; // Assets/_Project/Resources/CharacterRoster.asset

        [SerializeField] private List<CharacterPreset> playerPresets = new List<CharacterPreset>();
        [SerializeField] private CharacterPreset enemyPreset;
        [SerializeField] private RuntimeAnimatorController animatorController;
        [Tooltip("Optional rifle model; a simple placeholder is built when empty.")]
        [SerializeField] private GameObject weaponPrefab;

        public IReadOnlyList<CharacterPreset> PlayerPresets => playerPresets;
        public CharacterPreset EnemyPreset => enemyPreset;
        public RuntimeAnimatorController AnimatorController => animatorController;
        public GameObject WeaponPrefab => weaponPrefab;

        public CharacterPreset GetPlayerPreset(int index)
        {
            if (playerPresets.Count == 0) return null;
            return playerPresets[Mathf.Clamp(index, 0, playerPresets.Count - 1)];
        }

        /// <summary>Loaded from Resources so any scene can find it without wiring.</summary>
        public static CharacterRoster Load() => Resources.Load<CharacterRoster>(ResourcePath);

#if UNITY_EDITOR
        public void EditorSetup(List<CharacterPreset> players, CharacterPreset enemy, RuntimeAnimatorController controller)
        {
            playerPresets = players;
            enemyPreset = enemy;
            animatorController = controller;
        }
#endif
    }

    /// <summary>The player's choice from the creator, kept between scenes and sessions.</summary>
    public static class CharacterSelection
    {
        private const string IndexKey = "Kotwica.PresetIndex";
        private const string NameKey = "Kotwica.PlayerName";

        public static int PresetIndex
        {
            get => PlayerPrefs.GetInt(IndexKey, 0);
            set { PlayerPrefs.SetInt(IndexKey, value); PlayerPrefs.Save(); }
        }

        public static string PlayerName
        {
            get => PlayerPrefs.GetString(NameKey, "Kurier");
            set { PlayerPrefs.SetString(NameKey, value); PlayerPrefs.Save(); }
        }
    }
}
