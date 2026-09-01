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

        /// 분필 화살표 (위를 가리킴 — 회전은 트랜스폼으로)
        public static Sprite ArrowMark()
        {
            var t = Blank(S);
            float cx = S / 2f;
            // 삼각 촉
            for (int y = 30; y <= 52; y++)
            {
                float u = (y - 30) / 22f;
                float half = Mathf.Lerp(0f, 15f, u);
                for (int x = 0; x < S; x++)
                    if (Mathf.Abs(x - cx) <= half) Plot(t, x, y, 1f);
            }
            // 자루
            Line(t, cx, 30f, cx, 12f, 3.4f);
            return Make(t);
        }

    }
}
