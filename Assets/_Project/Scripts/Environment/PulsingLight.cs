using UnityEngine;

namespace Kotwica.Environment
{
    /// <summary>
    /// Slow pulse or flicker for emergency and faulty lights: adds unease without any gameplay cost.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class PulsingLight : MonoBehaviour
    {
        public enum Pattern { Pulse, Flicker }

        [SerializeField] private Pattern pattern = Pattern.Pulse;
        [SerializeField] private float minIntensity = 0.2f;
        [SerializeField] private float maxIntensity = 2f;
        [Tooltip("Pulses per second (Pulse) or flicker speed (Flicker).")]
        [SerializeField] private float speed = 0.6f;

        private Light lightSource;
        private float seed;

        private void Awake()
        {
            lightSource = GetComponent<Light>();
            seed = Random.value * 100f; // lamps do not blink in sync
        }

        private void Update()
        {
            float t = Time.time * speed + seed;
            float k = pattern == Pattern.Pulse
                ? 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f)
                : Mathf.PerlinNoise(t * 8f, seed) > 0.35f ? 1f : Mathf.PerlinNoise(t * 20f, seed) * 0.4f;
            lightSource.intensity = Mathf.Lerp(minIntensity, maxIntensity, k);
        }
    }
}
