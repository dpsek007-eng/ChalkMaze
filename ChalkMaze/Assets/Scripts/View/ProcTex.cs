using UnityEngine;

namespace ChalkMaze
{
    /// 텍스처를 전부 런타임에 그린다. 에디터에서 임포트할 이미지가 하나도 없다.
    public static class ProcTex
    {
        const int S = 64;

        static Sprite Make(Texture2D t)
        {
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height),
                                 new Vector2(0.5f, 0.5f), S);
        }

        static Texture2D Blank(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(1, 1, 1, 0);
            t.SetPixels(px);
            return t;
        }

        static void Plot(Texture2D t, int x, int y, float a)
        {
            if (x < 0 || y < 0 || x >= t.width || y >= t.height) return;
            var c = t.GetPixel(x, y);
            t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Max(c.a, Mathf.Clamp01(a))));
        }

        static void Line(Texture2D t, float x0, float y0, float x1, float y1, float w)
        {
            int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) * 2f) + 1;
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps;
                float cx = Mathf.Lerp(x0, x1, u), cy = Mathf.Lerp(y0, y1, u);
                int r = Mathf.CeilToInt(w);
                for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d <= w) Plot(t, (int)cx + dx, (int)cy + dy, 1f - Mathf.Max(0, d - w + 1f));
                }
            }
        }

        static void Disc(Texture2D t, float cx, float cy, float rad, bool hollow, float w)
        {
            for (int y = 0; y < t.height; y++)
            for (int x = 0; x < t.width; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float a = hollow
                    ? 1f - Mathf.Abs(d - rad) / Mathf.Max(0.5f, w)
                    : 1f - (d - rad + 1f);
                if (a > 0) Plot(t, x, y, a);
            }
        }

        /// 부드러운 원형 발광 — 횃불·플레이어·화톳불
        public static Sprite Glow()
        {
            int size = 128;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            float c = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - d);
                px[y * size + x] = new Color(1, 1, 1, a * a);
            }
            t.SetPixels(px);
            return Make(t);
        }

        /// 어둠 : 가운데가 뚫린 거대한 사각형. 플레이어를 따라다닌다.
        public static Sprite Vignette()
        {
            int size = 256;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            float c = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a;
                if (d < 0.10f) a = 0f;
                else if (d < 0.55f) a = Mathf.SmoothStep(0f, 0.42f, (d - 0.10f) / 0.45f);
                else a = Mathf.Lerp(0.42f, 0.985f, Mathf.Clamp01((d - 0.55f) / 0.45f));
                px[y * size + x] = new Color(1, 1, 1, a);
            }
            t.SetPixels(px);
            return Make(t);
        }

        // ── 아이콘 ───────────────────────────────────
        // 버튼에 글자를 쓰면 서식 문서처럼 보인다. 게임은 그림으로 말해야 한다.

        static void Rect(Texture2D t, float x0, float y0, float x1, float y1)
        {
            for (int y = Mathf.RoundToInt(y0); y <= Mathf.RoundToInt(y1); y++)
            for (int x = Mathf.RoundToInt(x0); x <= Mathf.RoundToInt(x1); x++)
                Plot(t, x, y, 1f);
        }

        static void Tri(Texture2D t, Vector2 a, Vector2 b, Vector2 c)
        {
            float minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x)), maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            float minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y)), maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            float Side(Vector2 p, Vector2 q, Vector2 r) => (q.x-p.x)*(r.y-p.y) - (q.y-p.y)*(r.x-p.x);
            for (int y = (int)minY; y <= (int)maxY; y++)
            for (int x = (int)minX; x <= (int)maxX; x++)
            {
                var p = new Vector2(x, y);
                bool s1 = Side(a, b, p) >= 0, s2 = Side(b, c, p) >= 0, s3 = Side(c, a, p) >= 0;
                if (s1 == s2 && s2 == s3) Plot(t, x, y, 1f);
            }
        }

        static Sprite Finish(Texture2D t)
        {
            t.filterMode = FilterMode.Bilinear; t.Apply();
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100);
        }

        /// 아이템 아이콘. 작게 줄여도 실루엣으로 구분되게 단순하게 그린다.
        public static Sprite ItemIcon(ItemKind k)
        {
            int N = 96;
            var t = Blank(N);
            float c = N / 2f;

            switch (k)
            {
                case ItemKind.Oil:      // 기름병 — 목이 좁은 병
                    Rect(t, c - 8, 62, c + 8, 76);              // 목
                    Rect(t, c - 13, 74, c + 13, 80);            // 마개
                    Disc(t, c, 40, 22f, false, 0);              // 몸통
                    Rect(t, c - 16, 40, c + 16, 64);
                    break;

                case ItemKind.Shovel:   // 삽 — 뾰족하면 방향 화살표로 읽힌다. 날을 각지게.
                    Rect(t, c - 5, 44, c + 5, 80);              // 자루
                    Rect(t, c - 16, 80, c + 16, 88);            // T자 손잡이
                    Rect(t, c - 5, 72, c + 5, 88);
                    Rect(t, c - 22, 20, c + 22, 44);            // 날 몸통
                    Tri(t, new Vector2(c - 22, 20), new Vector2(c + 22, 20), new Vector2(c - 22, 8));
                    Tri(t, new Vector2(c + 22, 20), new Vector2(c + 22, 8), new Vector2(c - 22, 8));
                    break;

                case ItemKind.Plank:    // 판자 — 결과 못을 파내야 널빤지로 읽힌다
                    Rect(t, 10, 34, N - 10, 62);
                    // 나뭇결 — 가로로 파낸다
                    for (int gx = 16; gx < N - 16; gx += 3)
                    {
                        int gy = 44 + (int)(Mathf.Sin(gx * 0.22f) * 2f);
                        t.SetPixel(gx, gy, new Color(1, 1, 1, 0));
                        t.SetPixel(gx, gy + 8, new Color(1, 1, 1, 0));
                    }
                    // 못 구멍
                    for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float d1 = Mathf.Sqrt((x-24)*(x-24) + (y-48)*(y-48));
                        float d2 = Mathf.Sqrt((x-(N-24))*(x-(N-24)) + (y-48)*(y-48));
                        if (d1 < 4.5f || d2 < 4.5f) t.SetPixel(x, y, new Color(1, 1, 1, 0));
                    }
                    break;

                case ItemKind.Thread:   // 아리아드네의 실 — 실패
                    Rect(t, 26, 72, N - 26, 80);                // 위 테
                    Rect(t, 26, 16, N - 26, 24);                // 아래 테
                    Rect(t, c - 12, 24, c + 12, 72);            // 감긴 실
                    for (int y = 28; y < 70; y += 8) Rect(t, c - 20, y, c + 20, y + 3);
                    break;

                case ItemKind.Compass:  // 나침반 — 원과 바늘
                    Disc(t, c, c, 34f, true, 5f);
                    Line(t, c - 14, c - 14, c + 14, c + 14, 4f);
                    Disc(t, c, c, 6f, false, 0);
                    break;

                default:                // 랜턴 — 사다리꼴 등불
                    Rect(t, c - 4, 78, c + 4, 86);              // 고리
                    Disc(t, c, 86, 9f, true, 3.5f);
                    Rect(t, c - 22, 68, c + 22, 76);            // 지붕
                    Tri(t, new Vector2(c - 22, 68), new Vector2(c + 22, 68), new Vector2(c, 84));
                    Rect(t, c - 17, 20, c + 17, 68);            // 몸통
                    Rect(t, c - 24, 14, c + 24, 22);            // 받침
                    break;
            }
            return Finish(t);
        }

        /// 톱니 — 설정
        public static Sprite GearIcon()
        {
            int N = 96; var t = Blank(N); float c = N / 2f;
            Disc(t, c, c, 30f, true, 9f);
            // 사각 이빨이라야 톱니로 읽힌다. 둥근 점은 구슬 목걸이가 된다.
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                float dx = Mathf.Cos(a), dy = Mathf.Sin(a);
                for (float r = 26f; r <= 42f; r += 0.5f)
                for (float w = -7f; w <= 7f; w += 0.5f)
                {
                    float x = c + dx * r - dy * w;
                    float y = c + dy * r + dx * w;
                    Plot(t, Mathf.RoundToInt(x), Mathf.RoundToInt(y), 1f);
                }
            }
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float d = Mathf.Sqrt((x-c)*(x-c) + (y-c)*(y-c));
                if (d < 11f) t.SetPixel(x, y, new Color(1,1,1,0));   // 가운데 구멍
            }
            return Finish(t);
        }

        /// 물음표 — 규칙
        public static Sprite QuestionIcon()
        {
            int N = 96; var t = Blank(N); float c = N / 2f;
            Disc(t, c, 66, 20f, true, 7f);
            for (int y = 0; y < 50; y++)
            for (int x = 0; x < N; x++)
                if (y > 46) t.SetPixel(x, y, t.GetPixel(x, y));
            Rect(t, c - 4, 34, c + 4, 52);
            Disc(t, c, 22, 6f, false, 0);
            // 원의 아래 절반을 지워 갈고리 모양으로
            for (int y = 0; y < 62; y++)
            for (int x = 0; x < (int)c; x++)
            {
                float d = Mathf.Sqrt((x-c)*(x-c) + (y-66)*(y-66));
                if (d > 12f && d < 28f) t.SetPixel(x, y, new Color(1,1,1,0));
            }
            return Finish(t);
        }

        /// 분필로 그은 듯 흔들리는 가로선. 반듯한 사각형은 문서처럼 보인다.
        public static Sprite ChalkRule()
        {
            int W = 256, H = 24;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(1, 1, 1, 0);
            t.SetPixels(px);

            float mid = H / 2f;
            for (int x = 0; x < W; x++)
            {
                // 손이 흔들린 만큼 위아래로 떨고, 끝으로 갈수록 옅어진다
                float wobble = Mathf.Sin(x * 0.09f) * 1.6f + Mathf.Sin(x * 0.31f) * 0.9f;
                float edge = Mathf.Clamp01(Mathf.Min(x, W - 1 - x) / 46f);
                float grain = 0.72f + 0.28f * Mathf.Sin(x * 0.77f);
                for (int dy = -2; dy <= 2; dy++)
                {
                    int y = Mathf.RoundToInt(mid + wobble) + dy;
                    if (y < 0 || y >= H) continue;
                    float a = (1f - Mathf.Abs(dy) / 2.6f) * edge * grain;
                    if (a > 0) t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            }
            t.filterMode = FilterMode.Bilinear; t.Apply();
            return Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100);
        }

        /// 속이 찬 둥근 사각형. 주 버튼용. 각진 사각형은 혼자 튄다.
        public static Sprite RectFill()
        {
            int S2 = 48, r = 12;
            var t = new Texture2D(S2, S2, TextureFormat.RGBA32, false);
            for (int y = 0; y < S2; y++)
            for (int x = 0; x < S2; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - (S2 - 1) / 2f) - ((S2 - 1) / 2f - r), 0);
                float dy = Mathf.Max(Mathf.Abs(y - (S2 - 1) / 2f) - ((S2 - 1) / 2f - r), 0);
                float d = Mathf.Sqrt(dx * dx + dy * dy) - r;
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - d)));
            }
            t.filterMode = FilterMode.Bilinear; t.Apply();
            return Sprite.Create(t, new Rect(0, 0, S2, S2), new Vector2(0.5f, 0.5f), 100,
                                 0, SpriteMeshType.FullRect, new Vector4(r + 2, r + 2, r + 2, r + 2));
        }

        /// 테두리만 있는 둥근 사각형. 9-슬라이스로 늘려 쓴다.
        public static Sprite RectOutline()
        {
            int S2 = 48, r = 12, w = 3;
            var t = new Texture2D(S2, S2, TextureFormat.RGBA32, false);
            var px = new Color[S2 * S2];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(1, 1, 1, 0);
            t.SetPixels(px);

            for (int y = 0; y < S2; y++)
            for (int x = 0; x < S2; x++)
            {
                // 둥근 사각형까지의 거리
                float dx = Mathf.Max(Mathf.Abs(x - (S2 - 1) / 2f) - ((S2 - 1) / 2f - r), 0);
                float dy = Mathf.Max(Mathf.Abs(y - (S2 - 1) / 2f) - ((S2 - 1) / 2f - r), 0);
                float d = Mathf.Sqrt(dx * dx + dy * dy) - r;
                float a = Mathf.Clamp01(1f - Mathf.Abs(d + w * 0.5f) / (w * 0.5f));
                if (a > 0) t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            t.filterMode = FilterMode.Bilinear; t.Apply();
            var sp = Sprite.Create(t, new Rect(0, 0, S2, S2), new Vector2(0.5f, 0.5f), 100,
                                   0, SpriteMeshType.FullRect, new Vector4(r + w, r + w, r + w, r + w));
            return sp;
        }

        /// 앱 아이콘과 같은 상징 — 두 벽 사이의 화살표
        public static Sprite Crest()
        {
            int W = 180, H = 180;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(1, 1, 1, 0);
            t.SetPixels(px);

            void Bar(int x0, int x1, int y0, int y1)
            {
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (x >= 0 && y >= 0 && x < W && y < H) t.SetPixel(x, y, Color.white);
            }
            Bar(30, 44, 34, 146);
            Bar(136, 150, 34, 146);

            // 위를 가리키는 갈매기꼴
            for (int y = 60; y <= 150; y++)
            {
                float u = (150 - y) / 90f;                 // y=150 끝(뾰족), 아래로 넓어짐
                float half = Mathf.Lerp(0f, 44f, u);
                float inner = Mathf.Lerp(0f, 26f, u);
                for (int x = 0; x < W; x++)
                {
                    float dx = Mathf.Abs(x - W / 2f);
                    if (dx <= half && dx >= inner - 1f) t.SetPixel(x, y, Color.white);
                }
            }
            t.filterMode = FilterMode.Bilinear; t.Apply();
            return Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100);
        }

        /// 폭죽 — 한 점에서 뻗는 불꽃 갈래. 끝에 불티가 맺힌다.
        public static Sprite Spark(int seed)
        {
            int N = 192;
            var t = Blank(N);
            float c = N / 2f;
            var rng = new System.Random(seed);
            int rays = 11 + rng.Next(5);

            for (int i = 0; i < rays; i++)
            {
                float a = (float)(i * (2.0 * Mathf.PI / rays) + rng.NextDouble() * 0.25);
                float len = c * (0.45f + (float)rng.NextDouble() * 0.5f);
                float ex = c + Mathf.Cos(a) * len, ey = c + Mathf.Sin(a) * len;

                // 안쪽은 진하고 바깥으로 갈수록 옅어지는 갈래
                int steps = Mathf.CeilToInt(len);
                for (int k = 0; k <= steps; k++)
                {
                    float u = k / (float)steps;
                    float x = Mathf.Lerp(c, ex, u), y = Mathf.Lerp(c, ey, u);
                    float w = Mathf.Lerp(2.2f, 0.6f, u);
                    float al = Mathf.Lerp(1f, 0.15f, u * u);
                    int r = Mathf.CeilToInt(w);
                    for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d <= w) Plot(t, (int)x + dx, (int)y + dy, al * (1f - d / (w + 0.5f)));
                    }
                }
                // 끝의 불티
                Disc(t, ex, ey, 2.6f, false, 0);
            }
            Disc(t, c, c, 4.5f, false, 0);
            return Finish(t);
        }

        /// 조이스틱 기준점을 나타내는 얇은 원
        public static Sprite Ring()
        {
            int size = 128;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            float c = size / 2f, r = c - 6f, w = 3.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - r) / w);
                px[y * size + x] = new Color(1, 1, 1, a);
            }
            t.SetPixels(px); t.filterMode = FilterMode.Bilinear; t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        public static Sprite Square()
        {
            var t = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var px = new Color[64];
            for (int i = 0; i < 64; i++) px[i] = Color.white;
            t.SetPixels(px);
            t.filterMode = FilterMode.Point;
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8);
        }

        /// 분필 ✕
        public static Sprite CrossMark()
        {
            var t = Blank(S);
            Line(t, 14, 14, S - 14, S - 14, 3.2f);
            Line(t, S - 14, 14, 14, S - 14, 3.2f);
            return Make(t);
        }

        /// 분필 화살표 — 위를 가리킨다. 회전은 트랜스폼이 맡는다.
        /// 텍스처 좌표는 y 가 위로 증가한다. 촉이 위(큰 y), 자루가 아래(작은 y).
        public static Sprite ArrowMark()
        {
            var t = Blank(S);
            float cx = S / 2f;

            // 촉 : y=54 에서 한 점, 아래로 내려오며 넓어진다
            for (int y = 34; y <= 54; y++)
            {
                float u = (54 - y) / 20f;
                float half = Mathf.Lerp(0f, 16f, u);
                for (int x = 0; x < S; x++)
                    if (Mathf.Abs(x - cx) <= half) Plot(t, x, y, 1f);
            }

            // 자루 : 촉 아래로
            Line(t, cx, 40f, cx, 12f, 4f);
            return Make(t);
        }

    }
}
