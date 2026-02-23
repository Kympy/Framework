using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DragonGate
{
    public partial class InputManager
    {
        // UI 위를 포인팅 중인지 체크
        public static bool IsPointerOverUI()
        {
            if (EventSystem.current == null)
                return false;

            // 터치 우선
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue());
            }

            // 마우스
            if (Mouse.current != null)
            {
                return EventSystem.current.IsPointerOverGameObject();
            }

            return false;
        }
        
        public static Vector2 GetClickScreenPosition()
        {
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();

            if (Touchscreen.current != null)
                return Touchscreen.current.primaryTouch.position.ReadValue();

            return Vector2.zero;
        }
        
        public static bool IsLeftClickTriggered()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;

            return false;
        }

        public static bool IsRightClickTriggered()
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                return true;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;

            return false;
        }

        public static bool EscPressed()
        {
            if (Keyboard.current == null)
            {
                return false;
            }
            return Keyboard.current.escapeKey.isPressed;
        }
    }
}
