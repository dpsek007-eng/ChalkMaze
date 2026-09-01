using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ChalkMaze
{
    /// 입력을 한 겹 감싼다. 구 Input Manager 와 신 Input System 어느 쪽이 켜져 있어도
    /// 게임 코드는 그대로 돈다. Unity 가 활성 핸들러에 따라 심볼을 정의해 준다.
    ///
    /// 게임 로직이 특정 입력 API 에 직접 묶이면, 파이프라인이나 입력 시스템이 바뀔 때마다
    /// 게임 코드를 헤집게 된다. 갈아끼우는 지점은 여기 하나면 된다.
    public static class InputProbe
    {
        /// dir : 0=북 1=동 2=남 3=서. 이번 프레임에 눌렸으면 true.
        public static bool DirPressed(int dir)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            switch (dir)
            {
                case 0: return k.upArrowKey.wasPressedThisFrame    || k.wKey.wasPressedThisFrame;
                case 1: return k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame;
                case 2: return k.downArrowKey.wasPressedThisFrame  || k.sKey.wasPressedThisFrame;
                case 3: return k.leftArrowKey.wasPressedThisFrame  || k.aKey.wasPressedThisFrame;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            switch (dir)
            {
                case 0: return Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.W);
                case 1: return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
                case 2: return Input.GetKeyDown(KeyCode.DownArrow)  || Input.GetKeyDown(KeyCode.S);
                case 3: return Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A);
            }
            return false;
#else
            return false;
#endif
        }

        /// 손가락이나 마우스가 이번 프레임에 눌렸는가
        public static bool PressStarted(out Vector2 pos)
        {
            pos = default;
#if ENABLE_INPUT_SYSTEM
            var t = Touchscreen.current;
            if (t != null && t.primaryTouch.press.wasPressedThisFrame)
            {
                pos = t.primaryTouch.position.ReadValue();
                return true;
            }
            var m = Mouse.current;
            if (m != null && m.leftButton.wasPressedThisFrame)
            {
                pos = m.position.ReadValue();
                return true;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0)) { pos = Input.mousePosition; return true; }
            return false;
#else
            return false;
#endif
        }

        /// 손가락이나 마우스가 이번 프레임에 떨어졌는가
        public static bool PressEnded(out Vector2 pos)
        {
            pos = default;
#if ENABLE_INPUT_SYSTEM
            var t = Touchscreen.current;
            if (t != null && t.primaryTouch.press.wasReleasedThisFrame)
            {
                pos = t.primaryTouch.position.ReadValue();
                return true;
            }
            var m = Mouse.current;
            if (m != null && m.leftButton.wasReleasedThisFrame)
            {
                pos = m.position.ReadValue();
                return true;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonUp(0)) { pos = Input.mousePosition; return true; }
            return false;
#else
            return false;
#endif
        }

        /// 누르고 있는 동안의 위치. 손가락을 떼지 않고 계속 끌 때 쓴다.
        public static bool PressHeld(out Vector2 pos)
        {
            pos = default;
#if ENABLE_INPUT_SYSTEM
            var t = Touchscreen.current;
            if (t != null && t.primaryTouch.press.isPressed)
            {
                pos = t.primaryTouch.position.ReadValue();
                return true;
            }
            var m = Mouse.current;
            if (m != null && m.leftButton.isPressed)
            {
                pos = m.position.ReadValue();
                return true;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButton(0)) { pos = Input.mousePosition; return true; }
            return false;
#else
            return false;
#endif
        }

        /// 오버레이 버튼을 키보드로 누르기 (스페이스 / 엔터)
        public static bool SubmitPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            return k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame
                || k.numpadEnterKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter);
#else
            return false;
#endif
        }
    }
}
