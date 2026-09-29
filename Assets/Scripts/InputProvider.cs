using UnityEngine;

namespace Fishy
{
    /// 输入适配层：自动兼容旧 Input Manager 与新 Input System
    /// （Unity 6 模板默认启用新 Input System，旧项目则是旧输入，两者都支持）
    public static class InputProvider
    {
#if ENABLE_INPUT_SYSTEM
        static UnityEngine.InputSystem.Controls.KeyControl Key(
            UnityEngine.InputSystem.Key k)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb == null ? null : kb[k];
        }
        static bool Down(UnityEngine.InputSystem.Controls.KeyControl c) => c != null && c.isPressed;
        static bool Hit(UnityEngine.InputSystem.Controls.KeyControl c) => c != null && c.wasPressedThisFrame;
#endif

        public static float Horizontal()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return 0f;
            float r = (kb.rightArrowKey.isPressed || kb.dKey.isPressed) ? 1f : 0f;
            float l = (kb.leftArrowKey.isPressed || kb.aKey.isPressed) ? 1f : 0f;
            return r - l;
#else
            float r = (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) ? 1f : 0f;
            float l = (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) ? 1f : 0f;
            return r - l;
#endif
        }

        public static bool WalkModifierHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Down(Key(UnityEngine.InputSystem.Key.LeftShift)) || Down(Key(UnityEngine.InputSystem.Key.RightShift)) || Down(Key(UnityEngine.InputSystem.Key.J));
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.J);
#endif
        }

        public static bool JumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Hit(Key(UnityEngine.InputSystem.Key.Space)) || Hit(Key(UnityEngine.InputSystem.Key.UpArrow)) || Hit(Key(UnityEngine.InputSystem.Key.W));
#else
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
#endif
        }

        public static bool JumpHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Down(Key(UnityEngine.InputSystem.Key.Space)) || Down(Key(UnityEngine.InputSystem.Key.UpArrow)) || Down(Key(UnityEngine.InputSystem.Key.W));
#else
            return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
#endif
        }

        public static bool AnyKeyDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.anyKey.wasPressedThisFrame;
#else
            return Input.anyKeyDown;
#endif
        }

        public static bool PausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Hit(Key(UnityEngine.InputSystem.Key.P));
#else
            return Input.GetKeyDown(KeyCode.P);
#endif
        }

        public static bool MutePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Hit(Key(UnityEngine.InputSystem.Key.M));
#else
            return Input.GetKeyDown(KeyCode.M);
#endif
        }

        public static bool RestartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Hit(Key(UnityEngine.InputSystem.Key.R));
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }
    }
}
