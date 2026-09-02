using UnityEngine;
using UnityEngine.UI;

namespace ChalkMaze
{
    /// 손가락을 댄 기준점과 현재 방향을 화면에 그린다.
    /// 보이지 않는 조이스틱은 어디를 기준으로 미는지 감이 안 잡혀서
    /// "왜 이쪽으로 안 가지" 가 된다.
    public sealed class SteerIndicator : MonoBehaviour
    {
        public TouchSteer Steer;
        Image _ring, _arrow;
        Canvas _canvas;

        public void Build(Transform canvas, Sprite ring, Sprite arrow)
        {
            _canvas = canvas.GetComponent<Canvas>();

            _ring = New(canvas, ring, new Color(0.91f, 0.89f, 0.84f, 0.16f), 190f);
            _arrow = New(canvas, arrow, new Color(1f, 0.48f, 0.24f, 0.85f), 92f);
            Hide();
        }

        Image New(Transform parent, Sprite s, Color c, float size)
        {
            var go = new GameObject("steer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();      // 버튼 뒤에 깔린다
            var img = go.GetComponent<Image>();
            img.sprite = s; img.color = c; img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            return img;
        }

        void Hide()
        {
            if (_ring != null) _ring.enabled = false;
            if (_arrow != null) _arrow.enabled = false;
        }

        void Update()
        {
            if (Steer == null || _ring == null) return;

            if (!Steer.Active) { Hide(); return; }

            _ring.enabled = true;
            _arrow.enabled = true;

            // 화면 좌표 → 캔버스 좌표
            var scaler = _canvas.GetComponent<CanvasScaler>();
            float k = _canvas.scaleFactor > 0 ? 1f / _canvas.scaleFactor : 1f;
            Vector2 p = (Steer.Anchor - new Vector2(Screen.width, Screen.height) * 0.5f) * k;

            _ring.rectTransform.anchoredPosition = p;

            // 방향 화살표는 기준점에서 조금 떨어뜨려 배치
            int d = Steer.Direction;
            Vector2 off = d == 0 ? Vector2.up : d == 1 ? Vector2.right
                        : d == 2 ? Vector2.down : Vector2.left;
            _arrow.rectTransform.anchoredPosition = p + off * 92f;
            _arrow.rectTransform.localRotation =
                Quaternion.Euler(0, 0, d == 0 ? 0f : d == 1 ? -90f : d == 2 ? 180f : 90f);
        }
    }
}
