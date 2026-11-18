using System.IO;
using UnityEditor;

namespace Kotwica.EditorTools
{
    /// <summary>
    /// Automatic import settings for Mixamo characters and animations
    /// (Assets/_Project/Art/Characters/...), so dropping FBX files in the right folder is all it takes:
    /// - every FBX becomes a Humanoid (animations are shared between all characters);
    /// - animation clips are named after the file, loop where it makes sense and stay "in place"
    ///   (movement comes from gameplay code, not from the animation).
    /// </summary>
    public class CharacterImportPostprocessor : AssetPostprocessor
    {
        public const string CharactersRoot = "Assets/_Project/Art/Characters/";
        public const string AnimationsFolder = CharactersRoot + "Animations/";

        private bool IsCharacterAsset => assetPath.StartsWith(CharactersRoot);
        private bool IsAnimationAsset => assetPath.StartsWith(AnimationsFolder);

        private void OnPreprocessModel()
        {
            if (!IsCharacterAsset) return;

            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = IsAnimationAsset;

            // Animation-only files do not need their (hidden) mesh or materials
            if (IsAnimationAsset) importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }

        private void OnPreprocessAnimation()
        {
            if (!IsAnimationAsset) return;

            var importer = (ModelImporter)assetImporter;
            string clipName = Path.GetFileNameWithoutExtension(assetPath);
            bool loop = !clipName.StartsWith("Death") && !clipName.StartsWith("Hit");

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.name = clipName;
                clip.loopTime = loop;
                // Bake root motion into the pose: the character stays in place, code moves it
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
        }
    }
}
