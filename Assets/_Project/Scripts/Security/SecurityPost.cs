using System.Collections.Generic;
using Kotwica.World;
using UnityEngine;

namespace Kotwica.Security
{
    /// <summary>
    /// Place in a district where security officers appear (guard booth, alley, lift).
    /// The ring is taken from the district it belongs to.
    /// </summary>
    public class SecurityPost : MonoBehaviour
    {
        public static readonly List<SecurityPost> All = new List<SecurityPost>();

        public string RingId
        {
            get
            {
                var content = GetComponentInParent<RingContent>();
                return content != null ? content.RingId : null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() => All.Remove(this);

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.6f, 1f);
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(0.8f, 2f, 0.8f));
        }
    }
}
