using UnityEngine;

namespace ChalkMaze
{
    /// 플레이어를 따라다니는 거대한 구멍 뚫린 어둠 스프라이트.
    /// 라이팅 세팅이 전혀 필요 없다.
    public sealed class Torch : MonoBehaviour
    {
        SpriteRenderer _dark;
        SpriteRenderer _panic;
        float _reach = 1f;

        void Awake()
        {
            _dark = Make(Palette.Void, 20);
            _panic = Make(new Color(0.47f, 0.08f, 0.08f, 0f), 21);
        }

        SpriteRenderer Make(Color c, int order)
        {
            var go = new GameObject("darkness");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProcTex.Vignette();
            sr.color = c;
            sr.sortingOrder = order;
            sr.transform.localScale = Vector3.one * 40f;
            return sr;
        }

        /// sightCells : 실제 시야 칸 수. 어둠의 경계를 여기에 맞춰야
        /// "보이는 데까지가 밝다"는 인상이 생긴다.
        public void SetFuel(float ratio01, int sightCells)
        {
            // 비네트 텍스처는 d=0.55 부근부터 어두워진다. 그 지점이 시야 끝에 오게 맞춘다.
            float want = (sightCells + 1f) / 1.1f;
            _reach = want * (0.55f + 0.45f * Mathf.Clamp01(ratio01));
            float panic = ratio01 < 0.25f ? (0.25f - ratio01) / 0.25f : 0f;
            var c = _panic.color;
            _panic.color = new Color(c.r, c.g, c.b, 0.40f * panic);
        }

        void Update()
        {
            // 불꽃이 흔들린다
            float flick = 1f + Mathf.Sin(Time.time * 5.2f) * 0.028f + Mathf.Sin(Time.time * 13.9f) * 0.016f;
            float s = _reach * flick;
            _dark.transform.localScale = Vector3.one * s;
            _panic.transform.localScale = Vector3.one * s * 1.4f;
        }
    }
}
