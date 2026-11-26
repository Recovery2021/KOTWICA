using UnityEngine;

namespace Kotwica.Quests
{
    /// <summary>Keeps a world-space label turned towards the camera (name tags, signs).</summary>
    public class FaceCamera : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
        }
    }
}
