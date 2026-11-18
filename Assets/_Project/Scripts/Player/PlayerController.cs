using Kotwica.Combat;
using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Player
{
    /// <summary>
    /// Third-person on-foot movement: walk/sprint relative to the camera, crouch (C), jump, gravity.
    /// While aiming the body faces where the camera looks and moves slower.
    /// External pulls (decompression) are added on top of the player's own movement.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, ICharacterPose
    {
        [Header("Speeds (m/s)")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float crouchSpeed = 1.8f;
        [SerializeField] private float aimSpeed = 2.2f;

        [Header("Body")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.1f;
        [Tooltip("Visual body that gets squashed when crouching (optional).")]
        [SerializeField] private Transform visual;
        [Tooltip("How fast the body turns towards the movement direction.")]
        [SerializeField] private float rotationSharpness = 12f;
        [SerializeField] private float jumpHeight = 1.0f;
        [SerializeField] private float gravity = -9.81f;

        [Tooltip("Camera used to decide what 'forward' means. Defaults to Camera.main.")]
        [SerializeField] private Transform cameraTransform;

        private CharacterController controller;
        private Health health;
        private float verticalVelocity;
        private Vector3 pullVelocity;
        private float pullUntil;
        private Vector3 visualBaseScale = Vector3.one;
        private Vector3 visualBasePosition;
        private Transform anchor;

        public bool IsCrouched { get; private set; }
        /// <summary>Holding a safety handle: cannot move, cannot be pulled, can still aim and shoot.</summary>
        public bool IsAnchored => anchor != null;
        /// <summary>Set by the weapon each frame while the aim button is held.</summary>
        public bool IsAiming { get; set; }
        public bool IsMoving { get; private set; }
        /// <summary>Eye height above the feet (camera and enemy aim use it).</summary>
        public float EyeHeight => IsCrouched ? 1.0f : 1.6f;
        /// <summary>Chest height: what enemies shoot at.</summary>
        public float ChestHeight => IsCrouched ? 0.75f : 1.35f;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            if (visual != null)
            {
                visualBaseScale = visual.localScale;
                visualBasePosition = visual.localPosition;
            }
        }

        private void Update()
        {
            bool alive = health == null || !health.IsDead;

            if (alive && GameInput.CrouchTogglePressed()) SetCrouched(!IsCrouched);

            // Movement direction on the floor plane, relative to where the camera looks
            Vector2 input = alive && !IsAnchored ? GameInput.Move() : Vector2.zero;
            Vector3 forward = cameraTransform != null
                ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized
                : transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 move = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
            IsMoving = move.sqrMagnitude > 0.01f;

            float speed = IsCrouched ? crouchSpeed : IsAiming ? aimSpeed : GameInput.SprintHeld() ? sprintSpeed : walkSpeed;

            // Aiming: face the camera direction; otherwise turn towards where we walk
            float t = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            if (alive && IsAiming && forward.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(forward, Vector3.up), t * 2f);
            else if (IsMoving)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move, Vector3.up), t);

            // Gravity and jump (no jumping from a crouch)
            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f; // keeps us glued to the floor
                if (alive && !IsCrouched && !IsAnchored && GameInput.JumpPressed()) verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;

            // Pulls fade out quickly when nobody keeps applying them; a held handle cancels them
            if (Time.time > pullUntil || IsAnchored) pullVelocity = Vector3.MoveTowards(pullVelocity, Vector3.zero, 20f * Time.deltaTime);
            if (IsAnchored) pullVelocity = Vector3.zero;

            controller.Move((move * speed + Vector3.up * verticalVelocity + pullVelocity) * Time.deltaTime);
        }

        /// <summary>Crouch or stand up (standing needs free space above the head).</summary>
        public void SetCrouched(bool crouch)
        {
            if (!crouch && IsCrouched)
            {
                Vector3 head = transform.position + Vector3.up * (crouchHeight - 0.1f);
                if (Physics.SphereCast(head, controller.radius * 0.9f, Vector3.up, out _, standingHeight - crouchHeight + 0.1f, ~0, QueryTriggerInteraction.Ignore))
                    return; // something above us, stay down
            }

            IsCrouched = crouch;
            float height = crouch ? crouchHeight : standingHeight;
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);

            if (visual != null)
            {
                float k = height / standingHeight;
                visual.localScale = new Vector3(visualBaseScale.x, visualBaseScale.y * k, visualBaseScale.z);
                visual.localPosition = new Vector3(visualBasePosition.x, visualBasePosition.y * k, visualBasePosition.z);
            }
        }

        /// <summary>External velocity applied this frame (e.g. decompression), m/s.</summary>
        public void ApplyPull(Vector3 velocity)
        {
            pullVelocity = velocity;
            pullUntil = Time.time + 0.1f;
        }

        /// <summary>Grab a handle: snap to its hold point and stay there until released.</summary>
        public void Grab(Transform holdPoint)
        {
            anchor = holdPoint;
            TeleportTo(holdPoint.position, holdPoint.rotation);
        }

        public void Release() => anchor = null;

        public bool IsHolding(Transform holdPoint) => anchor == holdPoint;

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
