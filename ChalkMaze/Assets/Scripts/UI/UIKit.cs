using UnityEngine;
using UnityEngine.UI;

namespace ChalkMaze
{
    /// TextMeshPro는 에센셜 임포트라는 수동 단계가 필요해서
    /// 부트스트랩 단계에서는 레거시 Text를 쓴다. 출시 전 TMP로 교체 권장.
    public static class UIKit
    {
        static Font _font;

        /// 유니티 내장 폰트에는 한글 글리프가 없다. UI 문구가 전부 한글이라
        /// 내장 폰트를 쓰면 글자가 하나도 그려지지 않는다 — 화면이 텅 빈 것처럼 보인다.
        /// 나눔고딕(SIL OFL)을 번들해서 쓴다. 안드로이드에서도 그대로 동작한다.
        public static Font Font
        {
            get
            {
                if (_font != null) return _font;

                _font = Resources.Load<Font>("NanumGothic");
                if (_font == null)
                {
                    Debug.LogWarning("[UIKit] NanumGothic 을 못 찾았다. 한글이 안 보일 수 있다.");
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (_font == null) Debug.LogError("[UIKit] 쓸 수 있는 폰트가 없다 — 텍스트가 전혀 안 나온다");
                return _font;
            }
        }

        public static RectTransform Panel(Transform parent, string name, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = bg;
            img.raycastTarget = bg.a > 0.01f;
            return go.GetComponent<RectTransform>();
        }

        public static RectTransform Empty(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static Text Label(Transform parent, string txt, int size, Color c,
                                TextAnchor anchor = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("txt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font; t.text = txt; t.fontSize = size; t.color = c;
            t.alignment = anchor; t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Btn(Transform parent, string txt, int size, Color fg, Color bg,
                                 System.Action onClick)
        {
            var rt = Panel(parent, "btn", bg);
            var b = rt.gameObject.AddComponent<Button>();
            var g = rt.GetComponent<Image>();
            // 버튼은 배경이 투명하더라도 반드시 터치를 받아야 한다.
            // Panel 은 알파가 낮으면 레이캐스트를 끄므로 여기서 되살린다.
            g.raycastTarget = true;
            b.targetGraphic = g;
            var l = Label(rt, txt, size, fg, TextAnchor.MiddleCenter);
            Stretch(l.rectTransform);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        public static void Stretch(RectTransform rt, float pad = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad);
        }

        public static RectTransform At(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
                                       Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        public static Image Box(Transform parent, Color c)
        {
            var rt = Panel(parent, "box", c);
            return rt.GetComponent<Image>();
        }
    }
}
