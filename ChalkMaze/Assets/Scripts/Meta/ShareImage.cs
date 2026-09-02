using UnityEngine;
using UnityEngine.UI;

namespace ChalkMaze
{
    /// 결과를 그림으로 만든다. 인스타·틱톡은 이미지 우선이라 글자만 보내면 퍼지지 않는다.
    ///
    /// 픽셀을 직접 찍으면 한글을 그릴 수 없다. 그래서 화면 밖 먼 곳에 카드를 uGUI 로
    /// 세우고 전용 카메라로 렌더 텍스처에 찍는다. 번들한 나눔고딕이 그대로 쓰인다.
    public static class ShareImage
    {
        const int W = 1080, H = 1920;

        /// 미로에서 멀리 떨어진 곳. 게임 카메라에 걸리지 않는다.
        static readonly Vector3 Far = new Vector3(100000f, 100000f, 0f);

        public static Texture2D Build(RunState st, int dayIndex, bool daily, Texture2D shot = null)
        {
            var root = new GameObject("ShareCard");
            root.transform.position = Far;

            var canvasGo = new GameObject("cv", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var crt = canvas.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(W, H);
            crt.position = Far;
            canvasGo.transform.localScale = Vector3.one * 0.01f;

            BuildCard(canvasGo.transform, st, dayIndex, daily, shot);

            // 카드만 담는 카메라
            var camGo = new GameObject("cam", typeof(Camera));
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = H * 0.01f * 0.5f;
            cam.transform.position = Far + new Vector3(0, 0, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Void;
            cam.cullingMask = ~0;
            canvas.worldCamera = cam;

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(root);
            return tex;
        }

        static void BuildCard(Transform cv, RunState st, int dayIndex, bool daily, Texture2D shot)
        {
            // 어둠 + 횃불
            var bg = UIKit.Panel(cv, "bg", Palette.Void);
            UIKit.Stretch(bg);

            var halo = new GameObject("halo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            halo.transform.SetParent(cv, false);
            var hi = halo.GetComponent<Image>();
            hi.sprite = ProcTex.Glow();
            hi.color = new Color(Palette.Ember.r, Palette.Ember.g, Palette.Ember.b, 0.30f);
            var hrt = hi.rectTransform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f);
            hrt.anchoredPosition = new Vector2(0, 260);
            hrt.sizeDelta = new Vector2(1500, 1500);

            // 상징
            var crest = new GameObject("crest", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            crest.transform.SetParent(cv, false);
            var ci = crest.GetComponent<Image>();
            ci.sprite = ProcTex.Crest();
            ci.color = Palette.Chalk;
            ci.preserveAspect = true;
            var crt2 = ci.rectTransform;
            crt2.anchorMin = crt2.anchorMax = new Vector2(0.5f, 0.5f);
            crt2.anchoredPosition = new Vector2(0, 740);
            crt2.sizeDelta = new Vector2(190, 190);

            Label(cv, daily ? $"오늘의 미로 #{dayIndex}" : $"{st.Cfg.Chapter} · {st.Level}층 돌파", 44, Palette.Ember, 0.5f, 610, 64);
            Label(cv, "분필 미로", 84, Palette.Chalk, 0.5f, 520, 116);

            // 클리어한 순간의 미로. 어느 게임인지 한눈에 보여야 공유가 힘을 얻는다.
            if (shot != null)
            {
                var frame = UIKit.Panel(cv, "frame", new Color(Palette.Chalk.r, Palette.Chalk.g, Palette.Chalk.b, 0.18f));
                var frt = frame.GetComponent<RectTransform>();
                frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
                frt.anchoredPosition = new Vector2(0, 90);
                frt.sizeDelta = new Vector2(640, 640);

                var shotGo = new GameObject("shot", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                shotGo.transform.SetParent(frame, false);
                var ri = shotGo.GetComponent<RawImage>();
                ri.texture = shot;
                var srt = ri.rectTransform;
                srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
                srt.offsetMin = new Vector2(6, 6); srt.offsetMax = new Vector2(-6, -6);
            }

            // 폭죽 — 해냈다는 신호. 숫자만 있으면 성적표처럼 보인다.
            for (int i = 0; i < 4; i++)
            {
                var sp = new GameObject("spark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                sp.transform.SetParent(cv, false);
                sp.transform.SetSiblingIndex(2);   // 배경·후광 위, 글자 아래
                var si = sp.GetComponent<Image>();
                si.sprite = ProcTex.Spark(dayIndex * 31 + i);
                var col = i % 2 == 0 ? Palette.Fire : Palette.Ember;
                si.color = new Color(col.r, col.g, col.b, 0.75f - i * 0.09f);
                si.preserveAspect = true;
                var rt = si.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                float[] xs = { -360f, 350f, -300f, 330f };
                float[] ys = { 520f, 430f, -180f, -110f };
                float[] sz = { 260f, 200f, 170f, 220f };
                rt.anchoredPosition = new Vector2(xs[i], ys[i]);
                rt.sizeDelta = new Vector2(sz[i], sz[i]);
            }

            // 성적
            string big = daily ? $"{st.Steps}걸음" : $"{st.Level}층";
            Label(cv, big, 132, Palette.Chalk, 0.5f, -400, 170);

            string sub = daily
                ? $"{st.Runs}회차 · 화톳불 {st.FiresLit}/{st.Bonfires.Count} · 분필 {st.Marks.Count}"
                : $"{st.Runs}회차 · 총 {st.Steps}걸음";
            Label(cv, sub, 40, Palette.Ash, 0.5f, -510, 60);

            // 최고 도달 층수 — 오늘의 미로에서도 실력의 지표가 된다
            int best = Mathf.Max(PlayerProfile.BestLevel, daily ? 0 : st.Level);
            if (best > 0)
                Label(cv, $"최고 도달  {best}층", 44, Palette.Fire, 0.5f, -586, 60);

            string mods = "";
            foreach (var m in ModInfo.Pool)
                if (st.Cfg.Has(m)) mods += (mods.Length > 0 ? " · " : "") + ModInfo.Name(m);
            if (mods.Length > 0)
                Label(cv, mods, 38, Palette.Ash, 0.5f, -652, 54);

            Label(cv, "지나온 길은 다시 어두워진다", 38, Palette.Ash, 0.5f, -772, 56);
            Label(cv, daily ? "당신은 몇 걸음?" : "당신은 몇 층까지?", 46, Palette.Chalk, 0.5f, -862, 66);
        }

        static void Label(Transform cv, string text, int size, Color c,
                          float anchorY, float y, float h)
        {
            var t = UIKit.Label(cv, text, size, c, TextAnchor.MiddleCenter);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0, anchorY);
            rt.anchorMax = new Vector2(1, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(-120, h);
        }
    }
}
