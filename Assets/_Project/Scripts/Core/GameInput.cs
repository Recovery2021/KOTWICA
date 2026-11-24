using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Kotwica.Core
{
    /// <summary>
    /// Thin wrapper over keyboard/mouse input, so gameplay code does not care
    /// whether the new Input System or the legacy Input Manager is active.
    /// Prototype only: later replaced by an Input Actions asset (gamepad, rebinding).
    /// </summary>
    public static class GameInput
    {
        // Scales raw mouse delta (pixels); lowered after playtest 1 (mouse felt too fast)
        private const float MouseDeltaScale = 0.06f;

#if ENABLE_INPUT_SYSTEM
        private static Keyboard Kb => Keyboard.current;
        private static Mouse Ms => Mouse.current;

        private static bool Held(Key key) => Kb != null && Kb[key].isPressed;
        private static bool Down(Key key) => Kb != null && Kb[key].wasPressedThisFrame;

        private static Vector2 RawLook() => Ms != null ? Ms.delta.ReadValue() * MouseDeltaScale : Vector2.zero;
#else
        private static bool Held(KeyCode key) => Input.GetKey(key);
        private static bool Down(KeyCode key) => Input.GetKeyDown(key);

        private static Vector2 RawLook() => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 0.6f;
#endif

        /// <summary>WASD as a vector (x = strafe, y = forward), length max 1.</summary>
        public static Vector2 Move()
        {
#if ENABLE_INPUT_SYSTEM
            float x = (Held(Key.D) ? 1f : 0f) - (Held(Key.A) ? 1f : 0f);
            float y = (Held(Key.W) ? 1f : 0f) - (Held(Key.S) ? 1f : 0f);
#else
            float x = (Held(KeyCode.D) ? 1f : 0f) - (Held(KeyCode.A) ? 1f : 0f);
            float y = (Held(KeyCode.W) ? 1f : 0f) - (Held(KeyCode.S) ? 1f : 0f);
#endif
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        private static bool Locked => Cursor.lockState == CursorLockMode.Locked;

        /// <summary>Mouse movement this frame; zero while the cursor is free (menus, Esc).</summary>
        public static Vector2 Look() => Locked ? RawLook() : Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        public static bool InteractPressed() => Down(Key.F);
        public static bool JumpPressed() => Down(Key.Space);
        public static bool SprintHeld() => Held(Key.LeftShift);
        public static bool BrakeHeld() => Held(Key.X);
        public static bool CursorTogglePressed() => Down(Key.Escape);
        /// <summary>Ship: flight assist on/off.</summary>
        public static bool FlightAssistTogglePressed() => Down(Key.Z);
        public static bool CrouchTogglePressed() => Down(Key.C);
        public static bool ReloadPressed() => Down(Key.R);
        /// <summary>Right mouse button: aim over the shoulder.</summary>
        public static bool AimHeld() => Locked && Ms != null && Ms.rightButton.isPressed;
        /// <summary>Left mouse button: fire (ignored while the cursor is free).</summary>
        public static bool FireHeld() => Locked && Ms != null && Ms.leftButton.isPressed;
        public static bool AnyClickPressed() => Ms != null && Ms.leftButton.wasPressedThisFrame;
        /// <summary>Ship vertical thrust: Space = up, Ctrl = down.</summary>
        public static float Vertical() => (Held(Key.Space) ? 1f : 0f) - (Held(Key.LeftCtrl) ? 1f : 0f);
        /// <summary>Ship roll: E = right, Q = left.</summary>
        public static float Roll() => (Held(Key.E) ? 1f : 0f) - (Held(Key.Q) ? 1f : 0f);
#else
        public static bool InteractPressed() => Down(KeyCode.F);
        public static bool JumpPressed() => Down(KeyCode.Space);
        public static bool SprintHeld() => Held(KeyCode.LeftShift);
        public static bool BrakeHeld() => Held(KeyCode.X);
        public static bool CursorTogglePressed() => Down(KeyCode.Escape);
        public static bool FlightAssistTogglePressed() => Down(KeyCode.Z);
        public static bool CrouchTogglePressed() => Down(KeyCode.C);
        public static bool ReloadPressed() => Down(KeyCode.R);
        public static bool AimHeld() => Locked && Input.GetMouseButton(1);
        public static bool FireHeld() => Locked && Input.GetMouseButton(0);
        public static bool AnyClickPressed() => Input.GetMouseButtonDown(0);
        public static float Vertical() => (Held(KeyCode.Space) ? 1f : 0f) - (Held(KeyCode.LeftControl) ? 1f : 0f);
        public static float Roll() => (Held(KeyCode.E) ? 1f : 0f) - (Held(KeyCode.Q) ? 1f : 0f);
#endif
    }
}
