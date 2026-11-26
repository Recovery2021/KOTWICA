using System.Collections;
using System.Collections.Generic;
using Kotwica.Core;
using Kotwica.Orbits;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kotwica.World
{
    /// <summary>
    /// Streams the district scenes of the rings: loads Ring_&lt;Id&gt; in the background when the camera
    /// gets close to a ring (or when the player docks there) and removes it again far away.
    /// Keeps memory low and lets every ring be built and edited as its own scene.
    /// </summary>
    public class RingStreamer : MonoBehaviour
    {
        [Tooltip("Load a ring's districts when the camera is closer than this to its port (m).")]
        [SerializeField] private float loadDistance = 450f;
        [Tooltip("Unload when farther than this (bigger than load distance, so it does not flicker).")]
        [SerializeField] private float unloadDistance = 700f;
        [SerializeField] private float checkInterval = 0.5f;

        private class Entry
        {
            public OrbitalRing ring;
            public string ringId;
            public string sceneName;
            public bool busy;
        }

        private readonly List<Entry> entries = new List<Entry>();

        private IEnumerator Start()
        {
            foreach (OrbitalRing ring in FindObjectsByType<OrbitalRing>(FindObjectsSortMode.None))
            {
                string id = GameServices.RingIdFor(ring);
                if (string.IsNullOrEmpty(id)) continue;
                string scene = "Ring_" + char.ToUpperInvariant(id[0]) + id.Substring(1);
                if (!Application.CanStreamedLevelBeLoaded(scene))
                {
                    Debug.Log($"[Kotwica] Brak sceny {scene} w Build Settings: pierścień {id} bez dzielnic.");
                    continue;
                }
                entries.Add(new Entry { ring = ring, ringId = id, sceneName = scene });
            }

            var wait = new WaitForSeconds(checkInterval);
            while (true)
            {
                Check();
                yield return wait;
            }
        }

        private void Check()
        {
            Camera view = Camera.main;
            if (view == null) return;
            OrbitalRing anchor = StationFrame.Instance != null ? StationFrame.Instance.Anchor : null;

            foreach (Entry entry in entries)
            {
                if (entry.busy) continue;
                bool loaded = RingContent.Find(entry.ringId) != null;
                float distance = Vector3.Distance(view.transform.position, entry.ring.transform.position);
                bool isAnchor = entry.ring == anchor;

                if (!loaded && (isAnchor || distance < loadDistance)) StartCoroutine(Load(entry));
                else if (loaded && !isAnchor && distance > unloadDistance) Unload(entry);
            }
        }

        private IEnumerator Load(Entry entry)
        {
            entry.busy = true;
            AsyncOperation op = SceneManager.LoadSceneAsync(entry.sceneName, LoadSceneMode.Additive);
            while (op != null && !op.isDone) yield return null;

            // The district root has moved itself onto the ring (and into this scene); the empty scene can go
            Scene scene = SceneManager.GetSceneByName(entry.sceneName);
            if (scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
            entry.busy = false;
        }

        private static void Unload(Entry entry)
        {
            RingContent content = RingContent.Find(entry.ringId);
            if (content != null) Destroy(content.gameObject);
        }
    }
}
