using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Player
{
    /// <summary>
    /// Finds the nearest usable object around the player and uses it on F.
    /// The HUD reads Current to show the prompt.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float radius = 2.5f;
        [Tooltip("Height of the search centre above the feet.")]
        [SerializeField] private float centerHeight = 1f;

        private readonly Collider[] hits = new Collider[16];
        private int enabledFrame;

        public IInteractable Current { get; private set; }

        private void OnEnable()
        {
            // Ignore the key press that re-activated us (e.g. F used to leave the ship)
            enabledFrame = Time.frameCount;
        }

        private void OnDisable() => Current = null;

        private void Update()
        {
            Current = FindNearest();

            if (Current != null && Current.CanInteract && Time.frameCount != enabledFrame && GameInput.InteractPressed())
            {
                Current.Interact();
            }
        }

        private IInteractable FindNearest()
        {
            Vector3 center = transform.position + Vector3.up * centerHeight;
            int count = Physics.OverlapSphereNonAlloc(center, radius, hits, ~0, QueryTriggerInteraction.Collide);

            IInteractable best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var candidate = hits[i].GetComponentInParent<IInteractable>();
                if (candidate == null) continue;

                float distance = (hits[i].ClosestPoint(center) - center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            return best;
        }
    }
}
