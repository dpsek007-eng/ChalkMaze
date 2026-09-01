using UnityEngine;

namespace ChalkMaze
{
    /// 바닥+벽을 메시 하나로 굽는다. 안개는 정점 색 알파로만 갱신하므로
    /// 이동할 때마다 메시를 다시 만들지 않는다.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class MazeMesh : MonoBehaviour
    {
        const int VertsPerCell = 20;   // 바닥 4 + 벽 4 x 4
        const float WallT = 0.09f;
        const float Inset = 0.02f;

        Mesh _mesh;
        Color[] _colors;
        byte[] _wallMask;
        int _n;

        void Awake()
        {
            _mesh = new Mesh { name = "MazeMesh" };
            _mesh.MarkDynamic();
            GetComponent<MeshFilter>().mesh = _mesh;

            var mr = GetComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(FindSpriteShader());
            mr.sortingOrder = 0;
        }

        /// 렌더 파이프라인에 따라 쓸 수 있는 셰이더가 다르다.
        /// Built-in / URP 어느 쪽에서도 정점 색 + 알파 블렌딩이 되는 것을 고른다.
        static Shader FindSpriteShader()
        {
            string[] candidates =
            {
                "Universal Render Pipeline/2D/Sprite-Unlit-Default",
                "Sprites/Default",
                "Unlit/Transparent"
            };
            foreach (var name in candidates)
            {
                var sh = Shader.Find(name);
                if (sh != null) { Debug.Log($"[MazeMesh] 셰이더: {name}"); return sh; }
            }
            Debug.LogError("[MazeMesh] 쓸 수 있는 스프라이트 셰이더를 찾지 못했다");
            return Shader.Find("Hidden/InternalErrorShader");
        }

        public void Build(Maze maze)
        {
            _n = maze.N;
            int cells = _n * _n;
            var verts = new Vector3[cells * VertsPerCell];
            var tris = new int[cells * 5 * 6];
            _colors = new Color[verts.Length];
            _wallMask = new byte[cells];

            int t = 0;
            for (int y = 0; y < _n; y++)
            for (int x = 0; x < _n; x++)
            {
                int ci = y * _n + x;
                int v = ci * VertsPerCell;

                byte mask = 0;
                for (int d = 0; d < 4; d++) if (maze.HasWall(x, y, d)) mask |= (byte)(1 << d);
                _wallMask[ci] = mask;

                float l = x, r = x + 1f, top = -y, bot = -y - 1f;

                Quad(verts, v, l + Inset, bot + Inset, r - Inset, top - Inset);       // 바닥
                Quad(verts, v + 4,  l - WallT, top - WallT, r + WallT, top + WallT);  // 북
                Quad(verts, v + 8,  r - WallT, bot - WallT, r + WallT, top + WallT);  // 동
                Quad(verts, v + 12, l - WallT, bot - WallT, r + WallT, bot + WallT);  // 남
                Quad(verts, v + 16, l - WallT, bot - WallT, l + WallT, top + WallT);  // 서

                for (int q = 0; q < 5; q++)
                {
                    int b = v + q * 4;
                    tris[t++] = b; tris[t++] = b + 1; tris[t++] = b + 2;
                    tris[t++] = b; tris[t++] = b + 2; tris[t++] = b + 3;
                }
            }

            _mesh.Clear();
            _mesh.indexFormat = verts.Length > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            _mesh.vertices = verts;
            _mesh.triangles = tris;
            _mesh.colors = _colors;
            _mesh.RecalculateBounds();
        }

        static void Quad(Vector3[] v, int i, float x0, float y0, float x1, float y1)
        {
            v[i]     = new Vector3(x0, y0, 0);
            v[i + 1] = new Vector3(x0, y1, 0);
            v[i + 2] = new Vector3(x1, y1, 0);
            v[i + 3] = new Vector3(x1, y0, 0);
        }

        /// 보이는 칸만 색을 넣는다. 나머지는 알파 0 — 어둠 그 자체.
        public void ApplyVisibility(System.Collections.Generic.HashSet<int> visible)
        {
            var clear = new Color(0, 0, 0, 0);
            for (int i = 0; i < _colors.Length; i++) _colors[i] = clear;

            foreach (int ci in visible)
            {
                if (ci < 0 || ci >= _wallMask.Length) continue;
                int v = ci * VertsPerCell;
                for (int k = 0; k < 4; k++) _colors[v + k] = Palette.Floor;

                byte mask = _wallMask[ci];
                for (int d = 0; d < 4; d++)
                {
                    var c = (mask & (1 << d)) != 0 ? Palette.StoneLit : clear;
                    int b = v + 4 + d * 4;
                    _colors[b] = c; _colors[b + 1] = c; _colors[b + 2] = c; _colors[b + 3] = c;
                }
            }
            _mesh.colors = _colors;
        }
    }
}
