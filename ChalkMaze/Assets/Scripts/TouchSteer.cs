using System;
using UnityEngine;

namespace ChalkMaze
{
    /// 손가락을 댄 자리가 기준점이 되고, 거기서 어느 쪽으로 밀었는지로 방향을 정한다.
    /// 화면 어디에 대든 상관없다 — 엄지가 닿는 곳이 곧 조이스틱이다.
    ///
    /// 기준점을 걸음마다 옮기면 안 된다. 엄지는 직선이 아니라 호를 그리며 움직여서,
    /// 오른쪽으로 밀어도 아래 성분이 계속 섞인다. 기준점이 따라오면 그 아래 성분이
    /// 매번 새로 평가되어 방향이 아래로 새어버린다.
    public sealed class TouchSteer : MonoBehaviour
    {
        public Action<int> OnMove;
        public Camera Cam;
        public Transform Player;

        /// 기준점에서 이만큼 밀어야 방향이 잡힌다 (화면 짧은 변 비율)
        const float DeadZone = 0.045f;

        /// 지금 가던 방향을 버리고 직각으로 꺾으려면 이 배수만큼 확실해야 한다.
        /// 1 이면 45도에서 흔들린다. 엄지 호 때문에 방향이 새는 것을 막는다.
        const float SwitchBias = 1.6f;

        const float FirstRepeat = 0.26f;
        const float RepeatRate  = 0.13f;

        bool _holding;
        int _dir = -1;
        float _nextMove;
        Vector2 _anchor;

        /// 조이스틱 기준점 (화면 좌표). 표시용으로 밖에서 읽는다.
        public bool Active => _holding && _dir >= 0;
        public Vector2 Anchor => _anchor;
        public int Direction => _dir;

        void Update()
        {
            bool overUi = UnityEngine.EventSystems.EventSystem.current != null &&
                          UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (InputProbe.PressStarted(out var down))
            {
                if (overUi) { _holding = false; return; }
                _holding = true;
                _anchor = down;          // 이번 터치 내내 고정
                _dir = -1;
                _nextMove = 0f;
                return;
            }

            if (!_holding) return;

            if (InputProbe.PressHeld(out var now))
            {
                int want = Resolve(now - _anchor);
                if (want < 0) return;               // 아직 데드존 안

                if (want != _dir) { _dir = want; _nextMove = 0f; }   // 꺾으면 즉시 한 걸음
                if (Time.time < _nextMove) return;

                _nextMove = Time.time + (_nextMove == 0f ? FirstRepeat : RepeatRate);
                OnMove?.Invoke(_dir);
                return;
            }

            _holding = false;
            _dir = -1;
        }

        /// 기준점에서 민 벡터를 방향으로. 지금 가던 방향에 관성을 준다.
        int Resolve(Vector2 d)
        {
            float dead = Mathf.Min(Screen.width, Screen.height) * DeadZone;
            if (d.magnitude < dead) return -1;

            float ax = Mathf.Abs(d.x), ay = Mathf.Abs(d.y);
            int horiz = d.x > 0 ? 1 : 3;
            int vert  = d.y > 0 ? 0 : 2;   // 화면 y는 위가 +, 격자는 북이 0

            if (_dir < 0) return ax > ay ? horiz : vert;   // 첫 판정은 단순 비교

            bool goingHoriz = _dir == 1 || _dir == 3;
            if (goingHoriz)
            {
                // 가로로 가는 중 — 세로로 꺾으려면 세로가 확실히 우세해야 한다
                if (ay > ax * SwitchBias) return vert;
                return horiz;
            }
            else
            {
                if (ax > ay * SwitchBias) return horiz;
                return vert;
            }
        }
    }
}
