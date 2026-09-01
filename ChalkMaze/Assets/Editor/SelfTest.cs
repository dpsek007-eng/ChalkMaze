using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ChalkMaze.EditorTools
{
    /// 실제 게임 코드를 그대로 돌려 규칙이 성립하는지 확인한다.
    /// 파이썬으로 다시 짠 모형이 아니라 출시될 C# 자체를 검사한다.
    public static class SelfTest
    {
        static int _fail;

        static void Check(bool ok, string what)
        {
            if (ok) return;
            _fail++;
            Debug.LogError("[TEST FAIL] " + what);
        }

        [MenuItem("분필 미로/3. 1000층 규칙 검사")]
        public static void Run()
        {
            _fail = 0;
            var rng = new System.Random(12345);
            // 1~1000층 전수 + 심연 표본. 표본 검사로는 잡히지 않는 구멍이 있었다.
            var levels = new List<int>();
            for (int L = 1; L <= 1000; L++) levels.Add(L);
            foreach (var L in new[] { 1200, 1500, 2000, 5000 }) levels.Add(L);

            int walked = 0;
            var modSeen = new HashSet<Mods>();

            foreach (int L in levels)
            {
                var st = new RunState();
                try { st.LoadLevel(L, rng); }
                catch (Exception e) { Check(false, $"{L}층 생성 예외: {e.Message}"); continue; }

                var cfg = st.Cfg;
                foreach (var m in ModInfo.Pool) if (cfg.Has(m)) modSeen.Add(m);

                // 층은 반드시 깰 수 있어야 한다 — 구덩이를 통행 불가로 두고 출구 도달 검사
                Check(st.Maze.Reachable(st.StartX, st.StartY, st.ExitX, st.ExitY, st.Pits),
                      $"{L}층 출구 도달 불가 (구덩이 {st.Pits.Count}개)");

                // 열쇠도 닿을 수 있어야 한다
                if (cfg.Has(Mods.KeyLock))
                {
                    Check(st.KeyPlaced, $"{L}층 잠긴문인데 열쇠 미배치");
                    if (st.KeyPlaced)
                        Check(st.Maze.Reachable(st.StartX, st.StartY, st.KeyX, st.KeyY, st.Pits),
                              $"{L}층 열쇠 도달 불가");
                }

                // 화톳불도 마찬가지
                for (int b = 0; b < st.Bonfires.Count; b++)
                    Check(st.Maze.Reachable(st.StartX, st.StartY, st.Bonfires[b].X, st.Bonfires[b].Y, st.Pits),
                          $"{L}층 화톳불#{b} 도달 불가");

                Check(cfg.Fuel > 10, $"{L}층 연료 {cfg.Fuel} — 너무 적다");
                Check(cfg.Chalk >= 0, $"{L}층 분필 음수");
                Check(st.Bonfires.Count >= 1, $"{L}층 화톳불 0개");
                Check(cfg.Pickups <= 5 && cfg.Pits <= 6, $"{L}층 상한 초과 (아이템 {cfg.Pickups}, 구덩이 {cfg.Pits})");
                Check(st.Pickups.Count <= cfg.Pickups, $"{L}층 아이템 배치 초과");
                Check(st.StartX != st.ExitX || st.StartY != st.ExitY, $"{L}층 시작=출구");

                // 정보 박탈 규칙 중복 금지
                int infoLoss = 0;
                foreach (var m in ModInfo.Pool)
                    if (cfg.Has(m) && ModInfo.IsInfoLoss(m)) infoLoss++;
                Check(infoLoss <= 1, $"{L}층 정보박탈 규칙 {infoLoss}개 중복");

                // 규칙 해금 시점 준수
                foreach (var m in ModInfo.Pool)
                    if (cfg.Has(m)) Check(L >= ModInfo.UnlockAt(m), $"{L}층에 {ModInfo.Name(m)} 조기 등장");

                // 마구 걸어봐도 예외가 안 나야 한다 (전수는 느리므로 표본만)
                try
                {
                    int walkSteps = (L <= 40 || L % 37 == 0) ? 400 : 0;
                    for (int i = 0; i < walkSteps; i++)
                    {
                        var r = st.TryMove(rng.Next(4));
                        walked++;
                        if (r == MoveResult.Died) st.Respawn();
                        if (r == MoveResult.Won) break;
                        if (i % 37 == 0) st.ToggleMark(MarkKind.Arrow);
                        if (i % 53 == 0) st.ToggleMark(MarkKind.DeadEnd);
                        if (i % 61 == 0) { st.UseShovel(st.LastDir); st.UsePlank(st.LastDir); st.UseThread(); }
                    }
                }
                catch (Exception e) { Check(false, $"{L}층 이동 중 예외: {e.Message}\n{e.StackTrace}"); }

                Check(st.Marks.Count <= st.Cfg.Chalk, $"{L}층 분필 한도 초과 ({st.Marks.Count}/{st.Cfg.Chalk})");
            }

            // 결정성 : 같은 층은 언제 불러도 같은 조건
            for (int L = 1; L <= 300; L += 7)
            {
                var a = LevelConfig.For(L);
                var b = LevelConfig.For(L);
                Check(a.Size == b.Size && a.Chalk == b.Chalk && a.Mods == b.Mods,
                      $"{L}층 설정이 호출마다 다르다");
            }

            Debug.Log($"[TEST] 검사 층 {levels.Count}개 · 이동 {walked}회 · 등장 규칙 {modSeen.Count}/7종");
            if (_fail == 0) Debug.Log("[TEST] ✅ 전부 통과");
            else Debug.LogError($"[TEST] ❌ 실패 {_fail}건");

            if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
        }
    }
}
