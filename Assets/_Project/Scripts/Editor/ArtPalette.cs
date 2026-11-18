using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kotwica.EditorTools
{
    /// <summary>
    /// The game's single material palette (same names and values as in the Blender kit script).
    /// Creates/updates URP Lit materials in Assets/_Project/Art/Materials/Palette
    /// and swaps them onto imported models by material name.
    /// </summary>
    public static class ArtPalette
    {
        public const string Folder = "Assets/_Project/Art/Materials/Palette";

        private struct Entry
        {
            public string Hex; public float Metallic; public float Smoothness; public float Emission;
            public Entry(string hex, float metallic, float smoothness, float emission)
            { Hex = hex; Metallic = metallic; Smoothness = smoothness; Emission = emission; }
        }

        // Keep in sync with PALETTE in Blender/Scripts/kotwica_kit_corridor.py
        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>
        {
            { "M_PaintedMetal", new Entry("4A4F55", 0.6f, 0.35f, 0f) },
            { "M_DarkMetal",    new Entry("2B2E33", 0.8f, 0.30f, 0f) },
            { "M_BareMetal",    new Entry("8A8D90", 1.0f, 0.55f, 0f) },
            { "M_Rubber",       new Entry("1E1F21", 0.0f, 0.20f, 0f) },
            { "M_Pipe",         new Entry("6B6252", 0.0f, 0.25f, 0f) },
            { "M_Warning",      new Entry("C9A227", 0.3f, 0.40f, 0f) },
            { "M_Glass",        new Entry("0C1418", 0.0f, 0.90f, 0f) },
            { "M_LightCold",    new Entry("DCE8FF", 0.0f, 0.50f, 3.0f) },
            { "M_LightAmber",   new Entry("FF9A3D", 0.0f, 0.50f, 2.5f) },
            { "M_LightRed",     new Entry("FF2A1A", 0.0f, 0.50f, 2.0f) },
            { "M_Screen",       new Entry("4DFF88", 0.0f, 0.80f, 1.2f) },
        };

        /// <summary>Creates or refreshes every palette material and returns them by name.</summary>
        public static Dictionary<string, Material> LoadOrCreate()
        {
            EnsureFolder(Folder);
            var result = new Dictionary<string, Material>();

            foreach (KeyValuePair<string, Entry> pair in Entries)
            {
                string path = $"{Folder}/{pair.Key}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null) shader = Shader.Find("Standard");
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }

                ColorUtility.TryParseHtmlString("#" + pair.Value.Hex, out Color color);
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Metallic", pair.Value.Metallic);
                material.SetFloat("_Smoothness", pair.Value.Smoothness);

                if (pair.Value.Emission > 0f)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * pair.Value.Emission);
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", Color.black);
                }

                EditorUtility.SetDirty(material);
                result[pair.Key] = material;
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        /// <summary>Replaces imported FBX materials with palette materials of the same name.</summary>
        public static void Apply(GameObject root, Dictionary<string, Material> palette)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    string name = materials[i].name;
                    int dot = name.IndexOf('.');                 // "M_Rubber.001" -> "M_Rubber"
                    if (dot > 0) name = name.Substring(0, dot);
                    if (palette.TryGetValue(name, out Material replacement)) materials[i] = replacement;
                    else Debug.LogWarning($"[Kotwica] Materiał '{materials[i].name}' w {root.name} nie należy do palety.");
                }
                renderer.sharedMaterials = materials;
            }
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
