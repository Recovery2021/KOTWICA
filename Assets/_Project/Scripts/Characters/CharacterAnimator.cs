using Kotwica.Combat;
using Kotwica.Core;
using UnityEngine;

namespace Kotwica.Characters
{
    /// <summary>
    /// Feeds the shared Animator Controller from gameplay state:
    /// speed and local move direction (from the CharacterController), crouch, aim and death.
    /// Works the same for the player and for enemies.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int MoveXId = Animator.StringToHash("MoveX");
        private static readonly int MoveYId = Animator.StringToHash("MoveY");
        private static readonly int CrouchId = Animator.StringToHash("Crouch");
        private static readonly int AimId = Animator.StringToHash("Aim");
        private static readonly int DeadId = Animator.StringToHash("Dead");

        [Tooltip("Speed that counts as 'full' strafing in the aim blend tree, m/s.")]
        [SerializeField] private float aimMoveSpeed = 2.2f;

        private Animator animator;
        private CharacterController controller;
        private ICharacterPose pose;
        private Health health;
        private Vector3 lastPosition;

        public Animator Animator => animator;

        public void Bind(Animator target)
        {
            animator = target;
            controller = GetComponent<CharacterController>();
            pose = GetComponent<ICharacterPose>();
            health = GetComponent<Health>();
            lastPosition = transform.position;
        }

        private void Update()
        {
            if (animator == null) return;

            // Measured movement (works even when the controller gets disabled on death)
            Vector3 velocity = (transform.position - lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPosition = transform.position;
            velocity.y = 0f;
            if (controller != null && !controller.enabled) velocity = Vector3.zero;

            Vector3 local = transform.InverseTransformDirection(velocity) / aimMoveSpeed;
            float dt = Time.deltaTime;

            animator.SetFloat(SpeedId, velocity.magnitude, 0.1f, dt);
            animator.SetFloat(MoveXId, Mathf.Clamp(local.x, -1f, 1f), 0.1f, dt);
            animator.SetFloat(MoveYId, Mathf.Clamp(local.z, -1f, 1f), 0.1f, dt);
            animator.SetBool(CrouchId, pose != null && pose.IsCrouched);
            animator.SetBool(AimId, pose != null && pose.IsAiming);
            animator.SetBool(DeadId, health != null && health.IsDead);
        }
    }
}
