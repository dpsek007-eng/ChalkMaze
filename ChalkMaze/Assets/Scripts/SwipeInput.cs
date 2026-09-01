using System;
using UnityEngine;

namespace ChalkMaze
{
    /// 화면 아무 데나 스와이프. UI 버튼 위에서는 동작하지 않는다.
    public sealed class SwipeInput : MonoBehaviour
    {
        public Action<int> OnSwipe;
        public float MinPixels = 40f;

        Vector2 _start;
        bool _tracking;

        void Update()
        {
            if (InputProbe.PressStarted(out var down))
            {
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
                _start = down; _tracking = true;
            }
            else if (InputProbe.PressEnded(out var up) && _tracking)
            {
                _tracking = false;
                Vector2 d = up - _start;
                if (d.magnitude < MinPixels) return;
                // 화면 y는 위가 +, 격자 y는 아래가 + 이므로 상하를 뒤집는다
                int dir = Mathf.Abs(d.x) > Mathf.Abs(d.y)
                    ? (d.x > 0 ? 1 : 3)
                    : (d.y > 0 ? 0 : 2);
                OnSwipe?.Invoke(dir);
            }
        }
    }
}
