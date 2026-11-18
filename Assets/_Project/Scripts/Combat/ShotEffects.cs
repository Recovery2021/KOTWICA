using System.Collections.Generic;
using UnityEngine;

namespace Kotwica.Combat
{
    /// <summary>
    /// Cheap prototype shot effects: a bright tracer line and short light flashes.
    /// Bloom in post-processing turns them into a convincing muzzle flash and impact.
    /// </summary>
    public static class ShotEffects
    {
        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        public static void Tracer(Vector3 from, Vector3 to, Color color, float lifetime = 0.05f)
        {
            var go = new GameObject("Smuga");
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = 0.025f;
            line.endWidth = 0.012f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = GetMaterial(color);
            Object.Destroy(go, lifetime);
        }

        public static void Flash(Vector3 position, Color color, float intensity, float range, float lifetime = 0.05f)
        {
            var go = new GameObject("Błysk");
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            Object.Destroy(go, lifetime);
        }

        // HDR unlit material per colour, so tracers glow through bloom
        private static Material GetMaterial(Color color)
        {
            if (Materials.TryGetValue(color, out Material material) && material != null) return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            material = new Material(shader);
            material.SetColor("_BaseColor", color * 6f);
            material.color = color * 6f;
            Materials[color] = material;
            return material;
        }
    }
}
