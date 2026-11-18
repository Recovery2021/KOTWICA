using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Player
{
    /// <summary>
    /// One camera for both modes:
    /// on foot = over-the-shoulder orbit camera (closer and narrower while aiming, follows crouch height,
    /// never clips through walls); ship = smooth chase camera behind the ship.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public enum Mode { OnFoot, Ship }

        [SerializeField] private Transform target;
        [SerializeField] private Mode mode = Mode.OnFoot;

        [Header("On foot")]
        [SerializeField] private float footDistance = 4.5f;
        [Tooltip("Eye height used when the target has no PlayerController.")]
        [SerializeField] private float footHeight = 1.6f;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 70f;
        [Tooltip("Keeps the camera from going through walls.")]
        [SerializeField] private float collisionRadius = 0.2f;

        [Header("Shoulder / aiming")]
        [SerializeField] private float shoulderOffset = 0.3f;
        [SerializeField] private float aimShoulderOffset = 0.55f;
        [SerializeField] private float aimDistance = 1.5f;
        [SerializeField] private float aimFieldOfView = 48f;
        [SerializeField] private float aimSharpness = 12f;

        [Header("Ship")]
        [SerializeField] private float shipDistance = 16f;
        [SerializeField] private float shipHeight = 4f;
        [Tooltip("Higher = camera follows the ship more tightly.")]
        [SerializeField] private float shipFollowSharpness = 8f;

        private Camera cam;
        private PlayerController targetPlayer;
        private float baseFieldOfView;
        private float yaw;
        private float pitch = 15f;
        private float aimBlend;
        private bool aiming;
        private float smoothedHeight;

        public Mode CurrentMode => mode;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            baseFieldOfView = cam != null ? cam.fieldOfView : 60f;
            CacheTarget();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (target != null) yaw = target.eulerAngles.y;
        }

        private void Update()
        {
            // Esc frees the mouse (for the editor); a click in the game window locks it again
            if (GameInput.CursorTogglePressed())
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }
            else if (Cursor.lockState != CursorLockMode.Locked && GameInput.AnyClickPressed())
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
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
            Vector2 look = GameInput.Look() * mouseSensitivity * (aiming ? 0.6f : 1f);
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);

            float k = 1f - Mathf.Exp(-aimSharpness * Time.deltaTime);
            aimBlend = Mathf.Lerp(aimBlend, aiming ? 1f : 0f, k);

            // Eye height follows crouching smoothly
            float wantedHeight = targetPlayer != null ? targetPlayer.EyeHeight : footHeight;
            smoothedHeight = smoothedHeight <= 0f ? wantedHeight : Mathf.Lerp(smoothedHeight, wantedHeight, k);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 head = target.position + Vector3.up * smoothedHeight;

            // Step 1: slide sideways to the shoulder (stop at walls)
            Vector3 side = rotation * Vector3.right;
            float shoulder = Mathf.Lerp(shoulderOffset, aimShoulderOffset, aimBlend);
            if (Physics.SphereCast(head, collisionRadius, side, out RaycastHit sideHit, shoulder, ~0, QueryTriggerInteraction.Ignore))
                shoulder = Mathf.Max(0f, sideHit.distance - 0.05f);
            Vector3 pivot = head + side * shoulder;

            // Step 2: pull back behind the shoulder (stop in front of walls)
            Vector3 back = rotation * Vector3.back;
            float distance = Mathf.Lerp(footDistance, aimDistance, aimBlend);
            if (Physics.SphereCast(pivot, collisionRadius, back, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
                distance = Mathf.Max(0.2f, hit.distance - 0.05f);

            transform.SetPositionAndRotation(pivot + back * distance, rotation);
            if (cam != null) cam.fieldOfView = Mathf.Lerp(baseFieldOfView, aimFieldOfView, aimBlend);
        }

        private void UpdateShipCamera()
        {
            Vector3 desired = target.position - target.forward * shipDistance + target.up * shipHeight;
            float t = 1f - Mathf.Exp(-shipFollowSharpness * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, desired, t);
            Quaternion look = Quaternion.LookRotation(target.position + target.forward * 30f - transform.position, target.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, t);
            if (cam != null) cam.fieldOfView = baseFieldOfView;
        }

        /// <summary>Weapon tells the camera whether the aim button is held.</summary>
        public void SetAiming(bool value) => aiming = value;

        /// <summary>Kicks the view up a little after each shot.</summary>
        public void AddRecoil(float degrees) => pitch = Mathf.Clamp(pitch - degrees, minPitch, maxPitch);

        public void FollowPlayer(Transform player)
        {
            target = player;
            mode = Mode.OnFoot;
            yaw = player.eulerAngles.y;
            pitch = 15f;
            CacheTarget();
        }

        public void FollowShip(Transform ship)
        {
            target = ship;
            mode = Mode.Ship;
            aiming = false;
            CacheTarget();
        }

        private void CacheTarget() => targetPlayer = target != null ? target.GetComponent<PlayerController>() : null;
    }
}
