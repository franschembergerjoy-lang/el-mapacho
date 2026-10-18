using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RobaYCorre
{
    /// <summary>
    /// Lectura de teclado centralizada. Funciona con el Input System nuevo y,
    /// si está activo, con el Input Manager clásico.
    /// </summary>
    public static class GameInput
    {
        public static Vector2 Move
        {
            get
            {
                Vector2 v = Vector2.zero;
                if (Held(KeyId.Up)) v.y += 1f;
                if (Held(KeyId.Down)) v.y -= 1f;
                if (Held(KeyId.Right)) v.x += 1f;
                if (Held(KeyId.Left)) v.x -= 1f;
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        public static bool Run => Held(KeyId.Run);
        public static bool Crouch => Held(KeyId.Crouch);
        public static bool InteractDown => Down(KeyId.Interact);
        public static bool DropDown => Down(KeyId.Drop);
        public static bool PauseDown => Down(KeyId.Pause);
        public static bool ConfirmDown => Down(KeyId.Confirm);

        enum KeyId { Up, Down, Left, Right, Run, Crouch, Interact, Drop, Pause, Confirm }

        static bool Held(KeyId id)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                switch (id)
                {
                    case KeyId.Up: return k.wKey.isPressed || k.upArrowKey.isPressed;
                    case KeyId.Down: return k.sKey.isPressed || k.downArrowKey.isPressed;
                    case KeyId.Left: return k.aKey.isPressed || k.leftArrowKey.isPressed;
                    case KeyId.Right: return k.dKey.isPressed || k.rightArrowKey.isPressed;
                    case KeyId.Run: return k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
                    case KeyId.Crouch: return k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed || k.cKey.isPressed;
                    case KeyId.Interact: return k.eKey.isPressed;
                    case KeyId.Drop: return k.qKey.isPressed;
                    case KeyId.Pause: return k.escapeKey.isPressed || k.pKey.isPressed;
                    case KeyId.Confirm: return k.enterKey.isPressed || k.spaceKey.isPressed || k.numpadEnterKey.isPressed;
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            switch (id)
            {
                case KeyId.Up: return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
                case KeyId.Down: return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
                case KeyId.Left: return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
                case KeyId.Right: return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
                case KeyId.Run: return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                case KeyId.Crouch: return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.C);
                case KeyId.Interact: return Input.GetKey(KeyCode.E);
                case KeyId.Drop: return Input.GetKey(KeyCode.Q);
                case KeyId.Pause: return Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.P);
                case KeyId.Confirm: return Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.KeypadEnter);
            }
#endif
            return false;
        }

        static bool Down(KeyId id)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                switch (id)
                {
                    case KeyId.Interact: return k.eKey.wasPressedThisFrame;
                    case KeyId.Drop: return k.qKey.wasPressedThisFrame;
                    case KeyId.Pause: return k.escapeKey.wasPressedThisFrame || k.pKey.wasPressedThisFrame;
                    case KeyId.Confirm: return k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame;
                }
                return false;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            switch (id)
            {
                case KeyId.Interact: return Input.GetKeyDown(KeyCode.E);
                case KeyId.Drop: return Input.GetKeyDown(KeyCode.Q);
                case KeyId.Pause: return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
                case KeyId.Confirm: return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter);
            }
#endif
            return false;
        }
    }
}
