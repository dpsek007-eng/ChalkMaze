using System;
using System.Collections.Generic;

namespace ChalkMaze
{
    public enum MarkKind { Arrow, DeadEnd }

    public struct ChalkMark { public MarkKind Kind; public int Dir; public int PlacedStep; }
    public struct Bonfire   { public int X, Y; public bool Lit; }

    public enum MoveResult
    {
        Blocked,        // 벽
        BlockedByPit,   // 구덩이 — 판자가 필요하다
        LockedExit,     // 잠긴 문 — 열쇠가 없다
        GotKey,
        Stepped,
        PickedUp,
        LitBonfire,
        Died,
        Won
    }

    /// 한 층의 진행 상태 전부. 화면도 입력도 전혀 모른다.
    public sealed class RunState
    {
        public Maze Maze;
        public LevelConfig Cfg;
        public int Level;

        public int PlayerX, PlayerY;
        public int SpawnX, SpawnY;
        public int StartX, StartY;
        public int ExitX, ExitY;

        public int Fuel;
        public int Runs = 1;
        public int Steps;
        public int LastDir = 2;

        public readonly List<Bonfire> Bonfires = new List<Bonfire>();
        public readonly Dictionary<int, ChalkMark> Marks = new Dictionary<int, ChalkMark>();
        public readonly HashSet<int> Visible = new HashSet<int>();

        // ── 아이템 ──
        public readonly Dictionary<ItemKind, int> Inventory = new Dictionary<ItemKind, int>();
        public readonly Dictionary<int, ItemKind> Pickups = new Dictionary<int, ItemKind>();
        public readonly HashSet<int> Pits = new HashSet<int>();
        public readonly HashSet<int> Planks = new HashSet<int>();   // 판자 놓은 구덩이 — 영구
        public readonly HashSet<int> Dug = new HashSet<int>();      // 삽으로 뚫은 자리 — 표시용

        public bool ThreadSet;
        public int ThreadX, ThreadY;
        public int CompassSteps;      // 남은 걸음 수 동안 출구 방향이 보인다

        public ItemKind LastPickup;

        // ── 변형 규칙 ──
        public bool HasKey;
        public int  KeyX, KeyY;
        public bool KeyPlaced;
        public const int ChalkLifetime = 40;

        /// 영구 업그레이드(랜턴 강화)로 더해지는 값. 바깥에서 넣어 준다.
        public int BonusSight;
        public int LanternSteps;
        public const int LanternBoost = 3;
        public const int LanternDuration = 25;

        public int SightRange
        {
            get
            {
                int b = Cfg.Has(Mods.Blind) ? 1 : Cfg.Sight;
                return b + BonusSight + (LanternSteps > 0 ? LanternBoost : 0);
            }
        }
        public bool ExitLocked => Cfg.Has(Mods.KeyLock) && KeyPlaced && !HasKey;

        public int ChalkLeft => Cfg.Chalk - Marks.Count;

        public int FiresLit
        {
            get { int c = 0; for (int i = 0; i < Bonfires.Count; i++) if (Bonfires[i].Lit) c++; return c; }
        }

        public int Count(ItemKind k) => Inventory.TryGetValue(k, out var v) ? v : 0;

        public void Add(ItemKind k, int n = 1)
        {
            Inventory[k] = Count(k) + n;
        }

        bool Spend(ItemKind k)
        {
            int c = Count(k);
            if (c <= 0) return false;
            Inventory[k] = c - 1;
            return true;
        }

        // ══════════════════════════════════════════════
        public void LoadLevel(int level, System.Random rng)
        {
            Level = level;
            Cfg = LevelConfig.For(level);
            Maze = new Maze(Cfg.Size, Cfg.Braid, rng);

            int n = Cfg.Size;
            var dist = Maze.DistanceFrom(0, 0);

            int maxD = 0, exitIdx = 0;
            for (int i = 0; i < dist.Length; i++)
                if (dist[i] > maxD) { maxD = dist[i]; exitIdx = i; }
            int startIdx = 0;
            if (Cfg.Has(Mods.Reversed)) { int t = startIdx; startIdx = exitIdx; exitIdx = t; }
            ExitX = exitIdx % n; ExitY = exitIdx / n;
            StartX = startIdx % n; StartY = startIdx / n;

            var taken = new HashSet<int> { startIdx, exitIdx };

            // 화톳불
            Bonfires.Clear();
            float[] fracs = Cfg.Bonfires == 2
                ? new[] { 0.36f, 0.68f }
                : new[] { 0.28f, 0.52f, 0.76f };
            foreach (var f in fracs)
            {
                double want = maxD * f;
                int best = -1; double bestErr = double.MaxValue;
                for (int i = 0; i < dist.Length; i++)
                {
                    if (dist[i] < 0 || taken.Contains(i)) continue;
                    double err = Math.Abs(dist[i] - want);
                    if (err < bestErr) { bestErr = err; best = i; }
                }
                if (best >= 0)
                {
                    taken.Add(best);
                    Bonfires.Add(new Bonfire { X = best % n, Y = best / n, Lit = false });
                }
            }

            // 구덩이 — 입구 근처엔 두지 않는다
            Pits.Clear(); Planks.Clear(); Dug.Clear();
            int guard = 0;
            while (Pits.Count < Cfg.Pits && guard++ < n * n * 8)
            {
                int i = rng.Next(n * n);
                if (taken.Contains(i) || Pits.Contains(i)) continue;
                if (dist[i] < 4) continue;

                // 이 구덩이를 놓아도 출구와 모든 화톳불에 갈 수 있어야 한다.
                // 아니면 판자 없이는 절대 못 깨는 층이 만들어진다.
                Pits.Add(i);
                bool ok = Maze.Reachable(StartX, StartY, ExitX, ExitY, Pits);
                if (ok)
                    for (int b = 0; b < Bonfires.Count && ok; b++)
                        ok = Maze.Reachable(StartX, StartY, Bonfires[b].X, Bonfires[b].Y, Pits);
                if (!ok) { Pits.Remove(i); continue; }

                taken.Add(i);
            }

            // 바닥에 떨어진 아이템
            Pickups.Clear();
            guard = 0;
            int totalW = 0;
            foreach (var k in ItemInfo.All) totalW += ItemInfo.Weight(k);
            while (Pickups.Count < Cfg.Pickups && guard++ < n * n * 8)
            {
                int i = rng.Next(n * n);
                if (taken.Contains(i) || Pickups.ContainsKey(i)) continue;
                if (dist[i] < 3) continue;

                int roll = rng.Next(totalW), acc = 0;
                ItemKind pick = ItemKind.Oil;
                foreach (var k in ItemInfo.All)
                {
                    acc += ItemInfo.Weight(k);
                    if (roll < acc) { pick = k; break; }
                }
                Pickups[i] = pick; taken.Add(i);
            }

            // 열쇠 : 출구에서 먼 곳에 둔다. 찾은 뒤 되돌아와야 한다.
            HasKey = false; KeyPlaced = false;
            if (Cfg.Has(Mods.KeyLock))
            {
                // 구덩이를 피해 실제로 갈 수 있는 칸들 중에서만 고른다.
                // 단순히 '출구에서 가장 먼 칸'을 고르면 구덩이가 그 길을 끊어
                // 판자 없이는 영영 못 깨는 층이 만들어진다.
                var dFromExit = Maze.DistanceFrom(ExitX, ExitY);
                var reach = new HashSet<int>();
                Maze.ReachableFrom(StartX, StartY, Pits, reach);

                int best = -1, bestD = -1;
                foreach (int i in reach)
                {
                    if (taken.Contains(i)) continue;
                    if (dFromExit[i] <= bestD) continue;
                    bestD = dFromExit[i]; best = i;
                }
                if (best >= 0)
                {
                    KeyX = best % n; KeyY = best / n;
                    KeyPlaced = true; taken.Add(best);
                }
            }

            Marks.Clear();
            Runs = 1; Steps = 0;
            SpawnX = StartX; SpawnY = StartY;
            ThreadSet = false; CompassSteps = 0;
            Respawn();
        }

        public void Respawn()
        {
            PlayerX = SpawnX; PlayerY = SpawnY;
            Fuel = Cfg.Fuel;
            LastDir = 2;
            CompassSteps = 0;
            LanternSteps = 0;
            Maze.ComputeVisible(PlayerX, PlayerY, Visible, SightRange);
        }

        public bool IsBlockedPit(int x, int y)
        {
            int i = Maze.Index(x, y);
            return Pits.Contains(i) && !Planks.Contains(i);
        }

        public MoveResult TryMove(int dir)
        {
            if (Maze.HasWall(PlayerX, PlayerY, dir)) return MoveResult.Blocked;

            int nx = PlayerX + Dir.DX[dir], ny = PlayerY + Dir.DY[dir];
            if (!Maze.InBounds(nx, ny)) return MoveResult.Blocked;
            if (IsBlockedPit(nx, ny)) { LastDir = dir; return MoveResult.BlockedByPit; }

            PlayerX = nx; PlayerY = ny;
            LastDir = dir;
            Fuel--;
            Steps++;
            if (CompassSteps > 0) CompassSteps--;
            if (LanternSteps > 0) LanternSteps--;
            Maze.ComputeVisible(PlayerX, PlayerY, Visible, SightRange);

            ExpireChalk();

            if (nx == ExitX && ny == ExitY)
            {
                if (ExitLocked) return MoveResult.LockedExit;
                return MoveResult.Won;
            }

            if (KeyPlaced && !HasKey && nx == KeyX && ny == KeyY)
            {
                HasKey = true;
                return MoveResult.GotKey;
            }

            int idx = Maze.Index(nx, ny);

            for (int i = 0; i < Bonfires.Count; i++)
            {
                var b = Bonfires[i];
                if (b.Lit || b.X != nx || b.Y != ny) continue;
                b.Lit = true; Bonfires[i] = b;
                SpawnX = nx; SpawnY = ny;
                Fuel = Cfg.Fuel;
                GrantChalk(1);          // 불을 밝힌 값
                return MoveResult.LitBonfire;
            }

            if (Pickups.TryGetValue(idx, out var got))
            {
                Pickups.Remove(idx);
                Add(got);
                LastPickup = got;
                if (Fuel <= 0) return MoveResult.Died;
                return MoveResult.PickedUp;
            }

            if (Fuel <= 0) return MoveResult.Died;
            return MoveResult.Stepped;
        }

        // ── 아이템 사용 ────────────────────────────────
        public bool UseOil()
        {
            if (!Spend(ItemKind.Oil)) return false;
            Fuel = Math.Min(Cfg.Fuel, Fuel + Cfg.Fuel / 2);
            return true;
        }

        /// 바라보는 방향의 벽을 뚫는다. 어디를 뚫었는지는 스스로 기억해야 한다.
        public bool UseShovel(int dir)
        {
            if (Count(ItemKind.Shovel) <= 0) return false;
            if (!Maze.Dig(PlayerX, PlayerY, dir)) return false;
            Spend(ItemKind.Shovel);
            Dug.Add(Maze.Index(PlayerX, PlayerY));
            Maze.ComputeVisible(PlayerX, PlayerY, Visible, SightRange);
            return true;
        }

        /// 바라보는 방향의 구덩이에 판자를 놓는다. 다음 회차에도 남는다.
        public bool UsePlank(int dir)
        {
            if (Count(ItemKind.Plank) <= 0) return false;
            int nx = PlayerX + Dir.DX[dir], ny = PlayerY + Dir.DY[dir];
            if (!Maze.InBounds(nx, ny)) return false;
            if (Maze.HasWall(PlayerX, PlayerY, dir)) return false;
            int i = Maze.Index(nx, ny);
            if (!Pits.Contains(i) || Planks.Contains(i)) return false;
            Spend(ItemKind.Plank);
            Planks.Add(i);
            return true;
        }

        /// 실 : 매듭이 없으면 묶고, 있으면 그 자리로 돌아간다. 되돌아가는 걸음이 공짜가 된다.
        public bool UseThread()
        {
            if (!ThreadSet)
            {
                if (!Spend(ItemKind.Thread)) return false;
                ThreadSet = true; ThreadX = PlayerX; ThreadY = PlayerY;
                return true;
            }
            PlayerX = ThreadX; PlayerY = ThreadY;
            ThreadSet = false;
            Maze.ComputeVisible(PlayerX, PlayerY, Visible, SightRange);
            return true;
        }

        public bool UseLantern()
        {
            if (!Spend(ItemKind.Lantern)) return false;
            LanternSteps = LanternDuration;
            Maze.ComputeVisible(PlayerX, PlayerY, Visible, SightRange);
            return true;
        }

        public bool UseCompass()
        {
            if (!Spend(ItemKind.Compass)) return false;
            CompassSteps = 12;
            return true;
        }

        /// 출구가 어느 쪽인지 (나침반용). 벽을 무시한 단순 방향.
        public int ExitBearing()
        {
            int dx = ExitX - PlayerX, dy = ExitY - PlayerY;
            if (Math.Abs(dx) > Math.Abs(dy)) return dx > 0 ? 1 : 3;
            return dy > 0 ? 2 : 0;
        }

        // ── 분필 ──────────────────────────────────────
        public bool ToggleMark(MarkKind kind)
        {
            int k = Maze.Index(PlayerX, PlayerY);
            if (Marks.TryGetValue(k, out var existing) && existing.Kind == kind)
            {
                Marks.Remove(k); return true;
            }
            bool replacing = Marks.ContainsKey(k);
            if (!replacing && Marks.Count >= Cfg.Chalk) return false;
            Marks[k] = new ChalkMark { Kind = kind, Dir = LastDir, PlacedStep = Steps };
            return true;
        }

        /// 지워지는 분필 규칙 — 찍어둔 자국이 시간이 지나면 사라진다.
        void ExpireChalk()
        {
            if (!Cfg.Has(Mods.FadingChalk) || Marks.Count == 0) return;
            List<int> gone = null;
            foreach (var kv in Marks)
                if (Steps - kv.Value.PlacedStep > ChalkLifetime)
                    (gone ??= new List<int>()).Add(kv.Key);
            if (gone != null) foreach (var k in gone) Marks.Remove(k);
        }

        /// 남은 수명 비율 (1=방금, 0=곧 사라짐). 지워지는 규칙이 아니면 항상 1.
        public float ChalkFreshness(int cellIndex)
        {
            if (!Cfg.Has(Mods.FadingChalk)) return 1f;
            if (!Marks.TryGetValue(cellIndex, out var m)) return 1f;
            float age = Steps - m.PlacedStep;
            return Math.Max(0f, 1f - age / ChalkLifetime);
        }

        public bool CanMark()
        {
            int k = Maze.Index(PlayerX, PlayerY);
            return Marks.ContainsKey(k) || Marks.Count < Cfg.Chalk;
        }

        // ── 광고 보상 ─────────────────────────────────
        public void RefillFuel() => Fuel = Cfg.Fuel;
        public void GrantChalk(int extra) { var c = Cfg; c.Chalk += extra; Cfg = c; }

        /// 분필을 다 썼고, 지금 선 칸에도 표식이 없다 = 더 찍고 싶어도 못 찍는 상태
        public bool ChalkExhausted =>
            Marks.Count >= Cfg.Chalk && !Marks.ContainsKey(Maze.Index(PlayerX, PlayerY));
    }
}
