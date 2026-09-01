using System.Collections.Generic;

namespace ChalkMaze
{
    /// 방향 : 0=북 1=동 2=남 3=서. 격자 y는 아래로 증가한다(렌더에서 뒤집음).
    public static class Dir
    {
        public static readonly int[] DX = { 0, 1, 0, -1 };
        public static readonly int[] DY = { -1, 0, 1, 0 };
        public static int Opposite(int d) => (d + 2) & 3;
    }

    /// 한 칸의 벽 상태를 비트로 담는다. bit0=북 bit1=동 bit2=남 bit3=서, 1이면 벽.
    public sealed class Maze
    {
        public readonly int N;
        readonly byte[] _walls;

        public Maze(int n, int braid, System.Random rng)
        {
            N = n;
            _walls = new byte[n * n];
            for (int i = 0; i < _walls.Length; i++) _walls[i] = 0b1111;
            Carve(rng);
            Braid(braid, rng);
        }

        public int Index(int x, int y) => y * N + x;
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < N && y < N;

        public bool HasWall(int x, int y, int dir) => (_walls[Index(x, y)] & (1 << dir)) != 0;

        /// 삽으로 벽을 뚫을 때 쓴다. 바깥 경계는 뚫리지 않는다.
        public bool Dig(int x, int y, int dir)
        {
            int nx = x + Dir.DX[dir], ny = y + Dir.DY[dir];
            if (!InBounds(nx, ny)) return false;
            if (!HasWall(x, y, dir)) return false;
            OpenBetween(x, y, dir);
            return true;
        }

        void OpenBetween(int x, int y, int dir)
        {
            int nx = x + Dir.DX[dir], ny = y + Dir.DY[dir];
            _walls[Index(x, y)] &= (byte)~(1 << dir);
            _walls[Index(nx, ny)] &= (byte)~(1 << Dir.Opposite(dir));
        }

        /// 재귀 백트래커
        void Carve(System.Random rng)
        {
            var seen = new bool[N * N];
            var stack = new Stack<int>();
            var order = new int[4];

            seen[0] = true;
            stack.Push(0);

            while (stack.Count > 0)
            {
                int cur = stack.Peek();
                int cx = cur % N, cy = cur / N;

                int count = 0;
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + Dir.DX[d], ny = cy + Dir.DY[d];
                    if (!InBounds(nx, ny) || seen[Index(nx, ny)]) continue;
                    order[count++] = d;
                }

                if (count == 0) { stack.Pop(); continue; }

                int dir = order[rng.Next(count)];
                OpenBetween(cx, cy, dir);
                int tx = cx + Dir.DX[dir], ty = cy + Dir.DY[dir];
                seen[Index(tx, ty)] = true;
                stack.Push(Index(tx, ty));
            }
        }

        /// 벽 몇 개를 헐어 순환로를 만든다.
        /// 완전한 트리로 두면 "한쪽 벽만 짚고 걷기"로 풀려버려 기억이 필요 없어진다.
        void Braid(int count, System.Random rng)
        {
            for (int i = 0; i < count; i++)
            {
                int x = 1 + rng.Next(N - 2);
                int y = 1 + rng.Next(N - 2);
                int d = rng.Next(4);
                int nx = x + Dir.DX[d], ny = y + Dir.DY[d];
                if (!InBounds(nx, ny)) continue;
                OpenBetween(x, y, d);
            }
        }

        /// 시작점에서의 최단 걸음 수. 도달 불가 칸은 -1.
        public int[] DistanceFrom(int sx, int sy)
        {
            var dist = new int[N * N];
            for (int i = 0; i < dist.Length; i++) dist[i] = -1;

            var q = new Queue<int>();
            int s = Index(sx, sy);
            dist[s] = 0;
            q.Enqueue(s);

            while (q.Count > 0)
            {
                int cur = q.Dequeue();
                int cx = cur % N, cy = cur / N;
                for (int d = 0; d < 4; d++)
                {
                    if (HasWall(cx, cy, d)) continue;
                    int nx = cx + Dir.DX[d], ny = cy + Dir.DY[d];
                    if (!InBounds(nx, ny)) continue;
                    int ni = Index(nx, ny);
                    if (dist[ni] >= 0) continue;
                    dist[ni] = dist[cur] + 1;
                    q.Enqueue(ni);
                }
            }
            return dist;
        }

        /// 특정 칸들을 통행 불가로 두고 도달 가능한지 확인한다.
        /// 구덩이가 유일한 통로를 막으면 그 층은 깰 수 없게 되므로 반드시 검사한다.
        public bool Reachable(int sx, int sy, int tx, int ty, HashSet<int> blocked)
        {
            int start = Index(sx, sy), goal = Index(tx, ty);
            if (blocked.Contains(goal)) return false;
            var seen = new bool[N * N];
            var q = new Queue<int>();
            seen[start] = true; q.Enqueue(start);
            while (q.Count > 0)
            {
                int cur = q.Dequeue();
                if (cur == goal) return true;
                int cx = cur % N, cy = cur / N;
                for (int d = 0; d < 4; d++)
                {
                    if (HasWall(cx, cy, d)) continue;
                    int nx = cx + Dir.DX[d], ny = cy + Dir.DY[d];
                    if (!InBounds(nx, ny)) continue;
                    int ni = Index(nx, ny);
                    if (seen[ni] || blocked.Contains(ni)) continue;
                    seen[ni] = true; q.Enqueue(ni);
                }
            }
            return false;
        }

        /// blocked 를 피해 도달 가능한 칸 전부. 매번 Reachable 을 부르는 것보다 싸다.
        public void ReachableFrom(int sx, int sy, HashSet<int> blocked, HashSet<int> into)
        {
            into.Clear();
            int start = Index(sx, sy);
            if (blocked.Contains(start)) return;
            var q = new Queue<int>();
            into.Add(start); q.Enqueue(start);
            while (q.Count > 0)
            {
                int cur = q.Dequeue();
                int cx = cur % N, cy = cur / N;
                for (int d = 0; d < 4; d++)
                {
                    if (HasWall(cx, cy, d)) continue;
                    int nx = cx + Dir.DX[d], ny = cy + Dir.DY[d];
                    if (!InBounds(nx, ny)) continue;
                    int ni = Index(nx, ny);
                    if (into.Contains(ni) || blocked.Contains(ni)) continue;
                    into.Add(ni); q.Enqueue(ni);
                }
            }
        }

        /// 시야 : 현재 칸 + 네 방향 복도를 벽에 막힐 때까지.
        /// 지나온 칸은 기억되지 않는다 — 그게 이 게임의 전부다.
        /// maxRange 를 주면 복도를 그만큼만 본다 (암흑 규칙).
        public void ComputeVisible(int px, int py, HashSet<int> into, int maxRange = 64)
        {
            into.Clear();
            into.Add(Index(px, py));
            for (int d = 0; d < 4; d++)
            {
                int x = px, y = py;
                for (int step = 0; step < maxRange; step++)
                {
                    if (HasWall(x, y, d)) break;
                    x += Dir.DX[d]; y += Dir.DY[d];
                    if (!InBounds(x, y)) break;
                    into.Add(Index(x, y));
                }
            }
        }
    }
}
