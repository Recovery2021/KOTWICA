using UnityEngine;

namespace Kotwica.Characters
{
    /// <summary>
    /// One ready-made look the player can pick in the character creator
    /// (a humanoid model imported from Mixamo or made in Blender).
    /// </summary>
    [CreateAssetMenu(menuName = "Kotwica/Postać - wzór", fileName = "Preset_")]
    public class CharacterPreset : ScriptableObject
    {
        [SerializeField] private string displayName = "Nowa postać";
        [TextArea, SerializeField] private string description;
        [Tooltip("Humanoid model (FBX with skin).")]
        [SerializeField] private GameObject model;
        [Tooltip("Uniform scale applied to the model (Mixamo models are already ~1.8 m).")]
        [SerializeField] private float scale = 1f;

        public string DisplayName => displayName;
        public string Description => description;
        public GameObject Model => model;
        public float Scale => scale;

#if UNITY_EDITOR
        /// <summary>Used by the editor setup tool when creating presets automatically.</summary>
        public void EditorSetup(string newName, GameObject newModel)
        {
            displayName = newName;
            model = newModel;
        }
#endif
    }
}
