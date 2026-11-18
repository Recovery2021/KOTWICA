using UnityEditor;

namespace Kotwica.EditorTools
{
    /// <summary>
    /// Import settings for our own Blender models (Assets/_Project/Art/Models):
    /// metres 1:1, no cameras/lights/animation, lightmap UVs for later baked lighting.
    /// Runs automatically every time an FBX in that folder is (re)imported.
    /// </summary>
    public class KitModelPostprocessor : AssetPostprocessor
    {
        private const string ModelsFolder = "Assets/_Project/Art/Models/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelsFolder)) return;

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importNormals = ModelImporterNormals.Import;   // keep Blender's hardened normals
            importer.generateSecondaryUV = true;
            importer.isReadable = false;
        }
    }
}
