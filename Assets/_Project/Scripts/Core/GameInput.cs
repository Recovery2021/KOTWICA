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

        /// <summary>Set while a conversation (or another modal screen) is open: gameplay keys read as released.</summary>
        public static bool InDialogue { get; set; }
        private static int unblockFrame = -1;

        /// <summary>Gameplay input is ignored now (also for one frame after a conversation closes).</summary>
        public static bool Blocked => InDialogue || Time.frameCount <= unblockFrame;

        /// <summary>Keeps gameplay keys blocked until the end of the next frame (the closing key press must not reopen anything).</summary>
        public static void BlockUntilNextFrame() => unblockFrame = Time.frameCount + 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            InDialogue = false;
            unblockFrame = -1;
        }

#if ENABLE_INPUT_SYSTEM
        private static Keyboard Kb => Keyboard.current;
        private static Mouse Ms => Mouse.current;

        private static bool Held(Key key) => !Blocked && Kb != null && Kb[key].isPressed;
        private static bool Down(Key key) => !Blocked && Kb != null && Kb[key].wasPressedThisFrame;

        /// <summary>Number key 1..9 pressed this frame (0 = none); works while gameplay input is blocked.</summary>
        public static int NumberPressed()
        {
            if (Kb == null) return 0;
            Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
            for (int i = 0; i < keys.Length; i++)
                if (Kb[keys[i]].wasPressedThisFrame) return i + 1;
            return 0;
        }

        /// <summary>F or Enter, readable while blocked (to continue a conversation).</summary>
        public static bool ConfirmPressed() => Kb != null && (Kb[Key.F].wasPressedThisFrame || Kb[Key.Enter].wasPressedThisFrame);

        /// <summary>Esc, readable while blocked (to leave a conversation).</summary>
        public static bool CancelPressed() => Kb != null && Kb[Key.Escape].wasPressedThisFrame;

        private static Vector2 RawLook() => Ms != null ? Ms.delta.ReadValue() * MouseDeltaScale : Vector2.zero;
#else
        private static bool Held(KeyCode key) => !Blocked && Input.GetKey(key);
        private static bool Down(KeyCode key) => !Blocked && Input.GetKeyDown(key);

        /// <summary>Number key 1..9 pressed this frame (0 = none); works while gameplay input is blocked.</summary>
        public static int NumberPressed()
        {
            for (int i = 0; i < 9; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) return i + 1;
            return 0;
        }

        /// <summary>F or Enter, readable while blocked (to continue a conversation).</summary>
        public static bool ConfirmPressed() => Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return);

        /// <summary>Esc, readable while blocked (to leave a conversation).</summary>
        public static bool CancelPressed() => Input.GetKeyDown(KeyCode.Escape);

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
        public static bool JournalTogglePressed() => Down(Key.Tab);
        public static bool QuickSavePressed() => Down(Key.F5);
        public static bool QuickLoadPressed() => Down(Key.F9);
        public static bool ReloadPressed() => Down(Key.R);
        /// <summary>Right mouse button: aim over the shoulder.</summary>
        public static bool AimHeld() => !Blocked && Locked && Ms != null && Ms.rightButton.isPressed;
        /// <summary>Left mouse button: fire (ignored while the cursor is free).</summary>
        public static bool FireHeld() => !Blocked && Locked && Ms != null && Ms.leftButton.isPressed;
        public static bool AnyClickPressed() => Ms != null && Ms.leftButton.wasPressedThisFrame;
        public static bool LeftMouseHeld() => Ms != null && Ms.leftButton.isPressed;
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
        public static bool JournalTogglePressed() => Down(KeyCode.Tab);
        public static bool QuickSavePressed() => Down(KeyCode.F5);
        public static bool QuickLoadPressed() => Down(KeyCode.F9);
        public static bool ReloadPressed() => Down(KeyCode.R);
        public static bool AimHeld() => !Blocked && Locked && Input.GetMouseButton(1);
        public static bool FireHeld() => !Blocked && Locked && Input.GetMouseButton(0);
        public static bool AnyClickPressed() => Input.GetMouseButtonDown(0);
        public static bool LeftMouseHeld() => Input.GetMouseButton(0);
        public static float Vertical() => (Held(KeyCode.Space) ? 1f : 0f) - (Held(KeyCode.LeftControl) ? 1f : 0f);
        public static float Roll() => (Held(KeyCode.E) ? 1f : 0f) - (Held(KeyCode.Q) ? 1f : 0f);
#endif
    }
}
