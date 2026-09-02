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

        public static Texture2D Build(RunState st, int dayIndex, bool daily)
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

            BuildCard(canvasGo.transform, st, dayIndex, daily);

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

        static void BuildCard(Transform cv, RunState st, int dayIndex, bool daily)
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
            crt2.anchorMin = crt2.anchorMax = new Vector2(0.5f, 1f);
            crt2.anchoredPosition = new Vector2(0, -330);
            crt2.sizeDelta = new Vector2(230, 230);

            Label(cv, daily ? $"오늘의 미로 #{dayIndex}" : "분필 미로", 46, Palette.Ember, 1f, -500, 70);
            Label(cv, "분필 미로", 92, Palette.Chalk, 1f, -600, 130);

            // 성적
            string big = daily ? $"{st.Steps}걸음" : $"{st.Level}층";
            Label(cv, big, 150, Palette.Chalk, 0.5f, 130, 200);

            string sub = daily
                ? $"{st.Runs}회차 · 화톳불 {st.FiresLit}/{st.Bonfires.Count} · 분필 {st.Marks.Count}"
                : $"{st.Runs}회차 · 총 {st.Steps}걸음";
            Label(cv, sub, 44, Palette.Ash, 0.5f, -30, 70);

            // 규칙이 있으면 알려 준다 — 같은 조건이라는 게 비교의 전제다
            string mods = "";
            foreach (var m in ModInfo.Pool)
                if (st.Cfg.Has(m)) mods += (mods.Length > 0 ? " · " : "") + ModInfo.Name(m);
            if (mods.Length > 0)
                Label(cv, mods, 42, Palette.Fire, 0.5f, -130, 70);

            Label(cv, "지나온 길은 다시 어두워진다", 40, Palette.Ash, 0f, 300, 60);
            Label(cv, "당신은 몇 걸음?", 46, Palette.Chalk, 0f, 200, 70);
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
