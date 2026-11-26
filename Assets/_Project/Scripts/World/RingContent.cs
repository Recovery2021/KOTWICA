using System.Collections.Generic;
using Kotwica.Orbits;
using UnityEngine;

namespace Kotwica.World
{
    /// <summary>
    /// Root of a ring's district scene (Ring_Kesh, Ring_Orrin, Ring_Varo).
    /// Districts are built in the ring's own coordinates (port deck at the origin, deck surface at y = 13);
    /// when the scene loads, this root attaches itself to the matching orbiting ring and moves with it.
    /// </summary>
    public class RingContent : MonoBehaviour
    {
        private static readonly Dictionary<string, RingContent> Loaded = new Dictionary<string, RingContent>();

        [SerializeField] private string ringId = "kesh";
        [Tooltip("Objects on the ring's port deck to hide while the district is attached (e.g. a rail blocking the walkway).")]
        [SerializeField] private string[] hideOnRing = { "Barierka_E" };

        private readonly List<GameObject> hidden = new List<GameObject>();

        public string RingId => ringId;
        public bool Attached { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Loaded.Clear();

        public static RingContent Find(string id) => id != null && Loaded.TryGetValue(id, out RingContent c) ? c : null;

        private void Awake()
        {
            Loaded[ringId] = this;
            AttachToRing();
        }

        private void OnDestroy()
        {
            if (Loaded.TryGetValue(ringId, out RingContent c) && c == this) Loaded.Remove(ringId);
            foreach (GameObject go in hidden)
                if (go != null) go.SetActive(true);
        }

        /// <summary>Finds the orbiting ring with our id and becomes its child (keeps local layout).</summary>
        private void AttachToRing()
        {
            foreach (OrbitalRing ring in FindObjectsByType<OrbitalRing>(FindObjectsSortMode.None))
            {
                if (Kotwica.Core.GameServices.RingIdFor(ring) != ringId) continue;

                Vector3 localPosition = transform.localPosition;
                Quaternion localRotation = transform.localRotation;
                transform.SetParent(ring.transform, false);
                transform.localPosition = localPosition;
                transform.localRotation = localRotation;

                foreach (string childName in hideOnRing)
                {
                    Transform child = ring.transform.Find(childName);
                    if (child == null || !child.gameObject.activeSelf) continue;
                    child.gameObject.SetActive(false);
                    hidden.Add(child.gameObject);
                }

                Attached = true;
                return;
            }

            // Opened on its own in the editor: stay where it is so it can be edited
            Debug.Log($"[Kotwica] Dzielnice '{ringId}': brak pierścienia w scenie, zostaję w miejscu (tryb edycji).");
        }
    }
}
