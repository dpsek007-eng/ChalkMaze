using System.Collections.Generic;
using UnityEngine;

namespace ChalkMaze
{
    /// 지형지물·분필·화톳불·출구·플레이어를 스프라이트로 그린다.
    /// 이동할 때만 갱신하므로 매 프레임 비용이 없다.
    public sealed class GlyphLayer : MonoBehaviour
    {
        Sprite _cross, _arrow, _glow, _square;

        readonly List<SpriteRenderer> _pool = new List<SpriteRenderer>();
        int _used;

        public Transform PlayerT { get; private set; }

        void Awake()
        {
            _cross  = ProcTex.CrossMark();
            _arrow  = ProcTex.ArrowMark();
            _glow   = ProcTex.Glow();
            _square = ProcTex.Square();

            var p = new GameObject("Player");
            p.transform.SetParent(transform, false);
            PlayerT = p.transform;

            var pg = New(p.transform, _glow, Palette.Ember, 30);
            pg.color = new Color(Palette.Ember.r, Palette.Ember.g, Palette.Ember.b, 0.5f);
            pg.transform.localScale = Vector3.one * 1.9f;

            var pd = New(p.transform, _square, Palette.Ember, 31);
            pd.transform.localScale = Vector3.one * 0.22f;
        }

        SpriteRenderer New(Transform parent, Sprite s, Color c, int order)
        {
            var go = new GameObject("g");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.color = c; sr.sortingOrder = order;
            return sr;
        }

        SpriteRenderer Take(Sprite s, Vector3 pos, Color c, int order, float scale, float rotDeg = 0)
        {
            SpriteRenderer sr;
            if (_used < _pool.Count) { sr = _pool[_used]; sr.gameObject.SetActive(true); }
            else { sr = New(transform, s, c, order); _pool.Add(sr); }
            _used++;

            sr.sprite = s; sr.color = c; sr.sortingOrder = order;
            sr.transform.localPosition = pos;
            sr.transform.localScale = Vector3.one * scale;
            sr.transform.localRotation = Quaternion.Euler(0, 0, rotDeg);
            return sr;
        }

        static Vector3 CellCenter(int x, int y) => new Vector3(x + 0.5f, -y - 0.5f, 0);

        public void Refresh(RunState st)
        {
            _used = 0;
            var maze = st.Maze;

            // 출구 — 잠겨 있으면 붉게 죽어 있다
            int exitIdx = maze.Index(st.ExitX, st.ExitY);
            if (st.Visible.Contains(exitIdx))
            {
                var p = CellCenter(st.ExitX, st.ExitY);
                var c = st.ExitLocked ? Palette.Danger : Palette.Moss;
                Take(_glow, p, new Color(c.r, c.g, c.b, st.ExitLocked ? 0.35f : 0.55f), 8, 2.2f);
                Take(_square, p, c, 9, 0.36f);
            }

            // 열쇠
            if (st.KeyPlaced && !st.HasKey && st.Visible.Contains(maze.Index(st.KeyX, st.KeyY)))
            {
                var p = CellCenter(st.KeyX, st.KeyY);
                Take(_glow, p, new Color(Palette.Fire.r, Palette.Fire.g, Palette.Fire.b, 0.5f), 26, 2.0f);
                Take(_square, p, Palette.Fire, 27, 0.22f);
            }

            // 화톳불 — 불을 붙이면 어둠 너머에서도 보인다 (등대이자 나침반)
            foreach (var b in st.Bonfires)
            {
                var p = CellCenter(b.X, b.Y);
                if (b.Lit)
                {
                    float pulse = 1f + Mathf.Sin(Time.time * 3.1f + b.X) * 0.09f;
                    Take(_glow, p, new Color(Palette.Fire.r, Palette.Fire.g, Palette.Fire.b, 0.62f), 40, 3.4f * pulse);
                    Take(_square, p, Palette.Fire, 41, 0.28f * pulse);
                }
                else if (st.Visible.Contains(maze.Index(b.X, b.Y)))
                {
                    Take(_glow, p, new Color(0.35f, 0.29f, 0.23f, 0.55f), 7, 1.1f);
                }
            }

            // 분필 자국
            foreach (var kv in st.Marks)
            {
                if (!st.Visible.Contains(kv.Key)) continue;
                int x = kv.Key % maze.N, y = kv.Key / maze.N;
                var p = CellCenter(x, y);
                // 지워지는 분필 규칙에서는 수명이 다해갈수록 흐려진다
                float fresh = st.ChalkFreshness(kv.Key);
                var cc = new Color(Palette.Chalk.r, Palette.Chalk.g, Palette.Chalk.b,
                                   0.25f + 0.75f * fresh);
                if (kv.Value.Kind == MarkKind.DeadEnd)
                    Take(_cross, p, cc, 35, 0.60f);
                else
                    Take(_arrow, p, cc, 35, 0.58f, -kv.Value.Dir * 90f);
            }

            for (int i = _used; i < _pool.Count; i++) _pool[i].gameObject.SetActive(false);

            PlayerT.localPosition = CellCenter(st.PlayerX, st.PlayerY);
        }

        /// 화톳불 맥동만 매 프레임 갱신 (전체 Refresh는 이동할 때만)
        public void TickPulse(RunState st)
        {
            if (st == null) return;
            Refresh(st);
        }
    }
}
