using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Player
{
    /// <summary>
    /// Third-person on-foot movement: walk/sprint relative to the camera, jump, gravity.
    /// Uses CharacterController (stable on decks, stairs, no physics jitter).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [Tooltip("How fast the body turns towards the movement direction.")]
        [SerializeField] private float rotationSharpness = 12f;
        [SerializeField] private float jumpHeight = 1.0f;
        [SerializeField] private float gravity = -9.81f;
        [Tooltip("Camera used to decide what 'forward' means. Defaults to Camera.main.")]
        [SerializeField] private Transform cameraTransform;

        private CharacterController controller;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            // Movement direction on the floor plane, relative to where the camera looks
            Vector2 input = GameInput.Move();
            Vector3 forward = cameraTransform != null
                ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized
                : transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 move = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);

            float speed = GameInput.SprintHeld() ? sprintSpeed : walkSpeed;

            // Turn the body smoothly towards where we walk
            if (move.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
                float t = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
            }

            // Gravity and jump
            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f; // keeps us glued to the floor
                if (GameInput.JumpPressed()) verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;

            controller.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        /// <summary>Moves the player instantly (leaving the ship, loading a save).</summary>
        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
            controller.enabled = true;
            verticalVelocity = 0f;
        }
    }
}
