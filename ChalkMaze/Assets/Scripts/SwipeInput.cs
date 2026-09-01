using System;
using UnityEngine;

namespace ChalkMaze
{
    /// 화면 아무 데나 스와이프. UI 버튼 위에서는 동작하지 않는다.
    public sealed class SwipeInput : MonoBehaviour
    {
        public Action<int> OnSwipe;

        /// 화면 크기에 비례한 임계값. 고해상도 폰에서 고정 픽셀은 너무 짧다.
        float Threshold => Mathf.Max(28f, Screen.width * 0.055f);

        Vector2 _origin;
        bool _tracking;

        void Update()
        {
            if (InputProbe.PressStarted(out var down))
            {
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
                _origin = down; _tracking = true;
                return;
            }

            if (!_tracking) return;

            // 손가락을 떼기 전에도, 임계값을 넘을 때마다 한 칸씩 움직인다.
            // 뗄 때만 반응하면 미로를 지나는 데 한 걸음마다 손을 들었다 놔야 한다.
            if (InputProbe.PressHeld(out var now))
            {
                Vector2 d = now - _origin;
                float th = Threshold;
                if (d.magnitude < th) return;

                // 화면 y는 위가 +, 격자 y는 아래가 + 이므로 상하를 뒤집는다
                int dir = Mathf.Abs(d.x) > Mathf.Abs(d.y)
                    ? (d.x > 0 ? 1 : 3)
                    : (d.y > 0 ? 0 : 2);
                OnSwipe?.Invoke(dir);

                // 기준점을 옮겨 같은 제스처로 계속 이동할 수 있게 한다
                _origin = now;
                return;
            }

            _tracking = false;
        }
    }
}
