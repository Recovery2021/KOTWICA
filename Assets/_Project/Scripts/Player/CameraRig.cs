using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Player
{
    /// <summary>
    /// One camera for both modes:
    /// on foot = mouse-orbit camera behind the character (does not clip through walls),
    /// ship = smooth chase camera behind the ship.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public enum Mode { OnFoot, Ship }

        [SerializeField] private Transform target;
        [SerializeField] private Mode mode = Mode.OnFoot;

        [Header("On foot")]
        [SerializeField] private float footDistance = 4.5f;
        [SerializeField] private float footHeight = 1.6f;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 70f;
        [Tooltip("Keeps the camera from going through walls.")]
        [SerializeField] private float collisionRadius = 0.25f;

        [Header("Ship")]
        [SerializeField] private float shipDistance = 16f;
        [SerializeField] private float shipHeight = 4f;
        [Tooltip("Higher = camera follows the ship more tightly.")]
        [SerializeField] private float shipFollowSharpness = 8f;

        private float yaw;
        private float pitch = 15f;

        public Mode CurrentMode => mode;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (target != null) yaw = target.eulerAngles.y;
        }

        private void Update()
        {
            // Esc frees the mouse (for the editor), click/Esc again to lock
            if (GameInput.CursorTogglePressed())
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if (mode == Mode.OnFoot) UpdateFootCamera();
            else UpdateShipCamera();
        }

        private void UpdateFootCamera()
        {
            Vector2 look = GameInput.Look() * mouseSensitivity;
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * footHeight;
            Vector3 direction = rotation * Vector3.back;

            // Pull the camera in front of walls instead of seeing through them
            float distance = footDistance;
            if (Physics.SphereCast(pivot, collisionRadius, direction, out RaycastHit hit, footDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                distance = hit.distance;
            }

            transform.SetPositionAndRotation(pivot + direction * distance, rotation);
        }

        private void UpdateShipCamera()
        {
            Vector3 desired = target.position - target.forward * shipDistance + target.up * shipHeight;
            float t = 1f - Mathf.Exp(-shipFollowSharpness * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, desired, t);
            Quaternion look = Quaternion.LookRotation(target.position + target.forward * 30f - transform.position, target.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, t);
        }

        public void FollowPlayer(Transform player)
        {
            target = player;
            mode = Mode.OnFoot;
            yaw = player.eulerAngles.y;
            pitch = 15f;
        }

        public void FollowShip(Transform ship)
        {
            target = ship;
            mode = Mode.Ship;
        }
    }
}
