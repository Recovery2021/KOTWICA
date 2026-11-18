using System.Collections;
using Kotwica.Gameplay;
using Kotwica.Player;
using UnityEngine;

namespace Kotwica.Combat
{
    /// <summary>
    /// Emergency airlock: after a short countdown it opens the outer door for a few seconds.
    /// Air rushes out and drags everyone towards the opening; whoever gets too close is blown into space.
    /// The player survives by holding a safety handle (SafetyHandle). One use per scene.
    /// </summary>
    public class AirlockController : MonoBehaviour
    {
        [SerializeField] private Transform door;
        [Tooltip("Point in the doorway everything is sucked towards.")]
        [SerializeField] private Transform suctionPoint;
        [SerializeField] private Light alarmLight;

        [Tooltip("Seconds between pressing the console and the door opening: time to grab a handle.")]
        [SerializeField] private float armDelay = 3f;
        [SerializeField] private float openHeight = 2.4f;
        [SerializeField] private float openTime = 0.5f;
        [SerializeField] private float ventDuration = 6f;
        [Tooltip("Pull speed right at the door, m/s; fades to zero at Reach.")]
        [SerializeField] private float maxPull = 14f;
        [SerializeField] private float reach = 18f;
        [Tooltip("Closer than this to the doorway = blown out into space.")]
        [SerializeField] private float killRadius = 2.2f;

        private Vector3 doorClosedPosition;

        public bool Used { get; private set; }
        public bool Venting { get; private set; }
        /// <summary>Seconds until the door opens; 0 when not counting down.</summary>
        public float CountdownRemaining { get; private set; }

        private void Awake()
        {
            if (door != null) doorClosedPosition = door.localPosition;
            if (alarmLight != null) alarmLight.enabled = false;
        }

        public void Trigger()
        {
            if (Used) return;
            Used = true;
            StartCoroutine(Vent());
        }

        private IEnumerator Vent()
        {
            // Countdown: alarm on, time to grab a handle
            if (alarmLight != null) alarmLight.enabled = true;
            GameState.Notify("Alarm śluzy! Złap uchwyt bezpieczeństwa (F)");
            float open = Time.time + armDelay;
            while (Time.time < open)
            {
                CountdownRemaining = open - Time.time;
                if (alarmLight != null) alarmLight.intensity = Mathf.Repeat(Time.time * 2f, 1f) < 0.5f ? 5f : 0.5f;
                yield return null;
            }
            CountdownRemaining = 0f;

            Venting = true;
            GameState.Notify("DEKOMPRESJA!");

            yield return MoveDoor(doorClosedPosition + Vector3.up * openHeight);

            float end = Time.time + ventDuration;
            while (Time.time < end)
            {
                PullEveryone(1f - (end - Time.time) / ventDuration);
                if (alarmLight != null) alarmLight.intensity = 3f + 2f * Mathf.Sin(Time.time * 12f);
                yield return null;
            }

            yield return MoveDoor(doorClosedPosition);
            Venting = false;
            if (alarmLight != null) alarmLight.enabled = false;
            GameState.Notify("Grodzie awaryjne zamknięte. Ciśnienie wraca.");
        }

        private IEnumerator MoveDoor(Vector3 target)
        {
            if (door == null) yield break;
            Vector3 start = door.localPosition;
            for (float t = 0f; t < 1f; t += Time.deltaTime / openTime)
            {
                door.localPosition = Vector3.Lerp(start, target, t);
                yield return null;
            }
            door.localPosition = target;
        }

        /// <param name="progress">0..1 through the vent; the pull weakens as the air runs out.</param>
        private void PullEveryone(float progress)
        {
            Vector3 hole = suctionPoint != null ? suctionPoint.position : transform.position;
            float strength = maxPull * Mathf.Lerp(1f, 0.4f, progress);

            // Copy: killing someone changes Health.All while we iterate
            foreach (Health h in Health.All.ToArray())
            {
                if (h.IsDead) continue;

                var holder = h.GetComponent<PlayerController>();
                if (holder != null && holder.IsAnchored) continue; // safe on the handle

                Vector3 toHole = hole - h.transform.position;
                toHole.y = 0f;
                float distance = toHole.magnitude;
                if (distance > reach) continue;

                if (distance < killRadius)
                {
                    BlowOut(h);
                    continue;
                }

                Vector3 pull = toHole.normalized * strength * (1f - distance / reach);

                var playerController = h.GetComponent<PlayerController>();
                // Only a held handle saves the player; crouching barely helps
                if (playerController != null && !playerController.IsAnchored)
                    playerController.ApplyPull(playerController.IsCrouched ? pull * 0.75f : pull);

                var enemy = h.GetComponent<EnemyAI>();
                if (enemy != null) enemy.ApplyPull(enemy.IsCrouched ? pull * 0.6f : pull);
            }
        }

        private static void BlowOut(Health victim)
        {
            victim.Kill("Wyssany w próżnię");
            foreach (Renderer r in victim.GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (Collider c in victim.GetComponentsInChildren<Collider>()) c.enabled = false;
        }
    }
}
