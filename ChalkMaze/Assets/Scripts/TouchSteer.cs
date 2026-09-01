using System;
using UnityEngine;

namespace ChalkMaze
{
    /// 화면을 누른 지점이 플레이어의 어느 쪽인지 보고 그 방향으로 움직인다.
    /// 누르고 있으면 계속 간다.
    ///
    /// 스와이프는 손가락을 그어야 해서, 한 칸씩 조심스럽게 움직여야 하는
    /// 이 게임과 맞지 않았다. 누르는 방식은 "가고 싶은 쪽을 가리킨다"에 가깝다.
    public sealed class TouchSteer : MonoBehaviour
    {
        public Action<int> OnMove;
        public Camera Cam;
        public Transform Player;

        /// 플레이어 주변 이 반경 안을 누르면 무시한다 (화면 짧은 변 기준 비율).
        /// 없으면 손가락이 조금만 흔들려도 방향이 튄다.
        const float DeadZone = 0.07f;
        const float FirstRepeat = 0.28f;   // 첫 반복까지
        const float RepeatRate  = 0.14f;   // 이후 간격

        bool _holding;
        int _lastDir = -1;
        float _nextMove;

        void Update()
        {
            // UI 위를 누른 것은 버튼이 처리한다
            bool overUi = UnityEngine.EventSystems.EventSystem.current != null &&
                          UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (InputProbe.PressStarted(out var down))
            {
                if (overUi) { _holding = false; return; }
                _holding = true;
                _lastDir = -1;
                Step(down, first: true);
                return;
            }

            if (!_holding) return;

            if (InputProbe.PressHeld(out var now))
            {
                if (Time.time < _nextMove) return;
                Step(now, first: false);
                return;
            }

            _holding = false;
            _lastDir = -1;
        }

        void Step(Vector2 screenPos, bool first)
        {
            int dir = DirectionFrom(screenPos);
            if (dir < 0) return;

            // 방향이 바뀌면 반복 대기를 처음부터 — 꺾을 때 미끄러지지 않게
            _nextMove = Time.time + (first || dir != _lastDir ? FirstRepeat : RepeatRate);
            _lastDir = dir;
            OnMove?.Invoke(dir);
        }

        /// 누른 지점이 플레이어의 어느 쪽인가. 우세한 축 하나만 고른다.
        int DirectionFrom(Vector2 screenPos)
        {
            Vector2 origin = Cam != null && Player != null
                ? (Vector2)Cam.WorldToScreenPoint(Player.position)
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            Vector2 d = screenPos - origin;
            float dead = Mathf.Min(Screen.width, Screen.height) * DeadZone;
            if (d.magnitude < dead) return -1;

            // 화면 y는 위가 +, 격자 y는 아래가 + 이므로 상하를 뒤집는다
            return Mathf.Abs(d.x) > Mathf.Abs(d.y)
                ? (d.x > 0 ? 1 : 3)
                : (d.y > 0 ? 0 : 2);
        }
    }
}
