using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChalkMaze
{
    /// HUD를 코드로 세운다. 에디터에서 조립할 것이 없다.
    public sealed class Hud : MonoBehaviour
    {
        public Action<int> OnDir;
        public Action<MarkKind> OnMark;
        public Action<ItemKind> OnUseItem;
        public Action OnWatchForChalk;
        public Action OnWatchForItem;

        Text _lv, _fuelNum, _toastBig, _toastSmall;
        Image _fuelFill;
        RectTransform _fireRow, _pipRow, _row, _toast;
        readonly List<Image> _fireDots = new List<Image>();
        readonly List<Image> _pips = new List<Image>();
        readonly Dictionary<ItemKind, (RectTransform rt, Text label)> _itemBtns
            = new Dictionary<ItemKind, (RectTransform, Text)>();
        readonly List<GameObject> _panels = new List<GameObject>();

        Button _mkArrow, _mkCross, _chalkAd, _itemAd;
        float _toastUntil;

        // 행동 줄에서 한 칸의 크기
        const float SlotW = 132f, SlotGap = 8f;

        public void Build(Transform canvas)
        {
            // ── 상단 계기 ──
            var top = UIKit.Empty(canvas, "Top");
            UIKit.At(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -190), new Vector2(-36, -40));

            _lv = UIKit.Label(top, "층 1", 40, Palette.Chalk, TextAnchor.UpperLeft);
            UIKit.At(_lv.rectTransform, new Vector2(0, 0), new Vector2(0.34f, 1), Vector2.zero, Vector2.zero);

            _fireRow = UIKit.Empty(top, "Fires");
            UIKit.At(_fireRow, new Vector2(0, 0), new Vector2(0.34f, 0), new Vector2(0, 26), new Vector2(0, 46));

            var fuelLbl = UIKit.Label(top, "횃불", 22, Palette.Ash, TextAnchor.UpperLeft);
            UIKit.At(fuelLbl.rectTransform, new Vector2(0.36f, 1), new Vector2(0.7f, 1), new Vector2(0, -34), new Vector2(0, 0));

            _fuelNum = UIKit.Label(top, "0", 28, Palette.Ember, TextAnchor.UpperRight);
            UIKit.At(_fuelNum.rectTransform, new Vector2(0.62f, 1), new Vector2(1f, 1), new Vector2(0, -34), new Vector2(0, 0));

            var barBg = UIKit.Box(top, Palette.StoneLit);
            UIKit.At(barBg.rectTransform, new Vector2(0.36f, 1), new Vector2(1, 1), new Vector2(0, -48), new Vector2(0, -42));

            _fuelFill = UIKit.Box(barBg.transform, Palette.Ember);
            UIKit.Stretch(_fuelFill.rectTransform);
            _fuelFill.type = Image.Type.Filled;
            _fuelFill.fillMethod = Image.FillMethod.Horizontal;
            _fuelFill.fillAmount = 1f;

            var pipLbl = UIKit.Label(top, "분필", 22, Palette.Ash, TextAnchor.UpperRight);
            UIKit.At(pipLbl.rectTransform, new Vector2(0.36f, 0), new Vector2(1, 0), new Vector2(0, 52), new Vector2(0, 76));

            _pipRow = UIKit.Empty(top, "Pips");
            UIKit.At(_pipRow, new Vector2(0.36f, 0), new Vector2(1, 0), new Vector2(0, 22), new Vector2(0, 46));

            // ── 토스트 ──
            _toast = UIKit.Empty(canvas, "Toast");
            UIKit.At(_toast, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(-400, -60), new Vector2(400, 60));
            _toastBig = UIKit.Label(_toast, "", 46, Palette.Fire, TextAnchor.MiddleCenter);
            UIKit.At(_toastBig.rectTransform, new Vector2(0, 0.45f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            _toastSmall = UIKit.Label(_toast, "", 24, Palette.Ash, TextAnchor.UpperCenter);
            UIKit.At(_toastSmall.rectTransform, new Vector2(0, 0), new Vector2(1, 0.45f), Vector2.zero, Vector2.zero);
            _toast.gameObject.SetActive(false);

            // ── 행동 줄 ──
            // 표식(지나간 방향·막다른 길)도 아이템과 똑같이 한 줄에 늘어놓는다.
            // 위쪽에 두면 아래가 통째로 비어 조향 공간이 넓어지고,
            // 엄지가 닿는 자리에 버튼이 없어 오조작도 줄어든다.
            _row = UIKit.Empty(canvas, "Actions");
            UIKit.At(_row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -382), new Vector2(-20, -212));

            _panels.Add(top.gameObject);
            _panels.Add(_row.gameObject);

            _mkArrow = MakeMarkBtn("지나간 방향", MarkKind.Arrow);
            _mkCross = MakeMarkBtn("막다른 길", MarkKind.DeadEnd);

            _chalkAd = UIKit.Btn(_row, "▶ 분필 +2", 24, Palette.Void, Palette.Fire,
                                 () => OnWatchForChalk?.Invoke());
            _chalkAd.gameObject.SetActive(false);

            _itemAd = UIKit.Btn(_row, "", 22, Palette.Void, Palette.Fire,
                                () => OnWatchForItem?.Invoke());
            _itemAd.gameObject.SetActive(false);
        }

        /// 타이틀·규칙·설정 화면에서는 게임 UI 가 비치면 안 된다.
        public void SetVisible(bool on)
        {
            foreach (var g in _panels) if (g != null) g.SetActive(on);
        }

        /// 줄 안에서 index 번째 자리. 표식도 아이템도 같은 규칙을 쓴다.
        static void Slot(RectTransform rt, int index, int span = 1)
        {
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(index * (SlotW + SlotGap), 0);
            rt.sizeDelta = new Vector2(SlotW * span + SlotGap * (span - 1), 0);
        }

        /// 글자 대신 그림. 상자를 두르면 서식 폼처럼 보인다.
        Button MakeMarkBtn(string caption, MarkKind kind)
        {
            var b = UIKit.Btn(_row, "", 24, Palette.Chalk, new Color(0, 0, 0, 0.001f),
                              () => OnMark?.Invoke(kind));
            var rt = b.GetComponent<RectTransform>();
            Icon(rt, kind == MarkKind.Arrow ? ProcTex.ArrowMark() : ProcTex.CrossMark(),
                 Palette.Chalk, 56f, new Vector2(0, 16));
            var cap = UIKit.Label(rt, caption, 17, Palette.Ash, TextAnchor.LowerCenter);
            UIKit.At(cap.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 4), new Vector2(0, 30));
            return b;
        }

        static Image Icon(Transform parent, Sprite sp, Color c, float size, Vector2 offset)
        {
            var go = new GameObject("icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sp; img.color = c; img.raycastTarget = false; img.preserveAspect = true;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = offset;
            return img;
        }

        public void Toast(string big, string small)
        {
            _toastBig.text = big; _toastSmall.text = small;
            _toast.gameObject.SetActive(true);
            _toastUntil = Time.time + 2.1f;
        }

        void Update()
        {
            if (_toast.gameObject.activeSelf && Time.time > _toastUntil)
                _toast.gameObject.SetActive(false);
        }

        public void Refresh(RunState st)
        {
            _lv.text = $"{st.Level}층   <color=#6E6875>{st.Runs}회차</color>\n"
                     + $"<size=20><color=#6E6875>{st.Cfg.Chapter}</color>"
                     + ModsText(st) + "</size>";
            _lv.supportRichText = true;

            float r = st.Cfg.Fuel > 0 ? Mathf.Clamp01((float)st.Fuel / st.Cfg.Fuel) : 0;
            _fuelNum.text = Mathf.Max(0, st.Fuel).ToString();
            _fuelFill.fillAmount = r;
            bool low = r < 0.25f;
            _fuelFill.color = low ? Palette.Danger : Palette.Ember;
            _fuelNum.color = low ? Palette.Danger : Palette.Ember;

            SyncDots(_fireDots, _fireRow, st.Bonfires.Count, 22, 12,
                     i => st.Bonfires[i].Lit ? Palette.Fire : Palette.StoneLit);
            SyncDots(_pips, _pipRow, Mathf.Max(st.Cfg.Chalk, 1), 18, 10,
                     i => i < st.Marks.Count ? Palette.StoneLit : Palette.Chalk, true);

            // ── 한 줄 안에서 왼쪽부터 자리를 채운다 ──
            int slot = 0;
            bool dry = st.ChalkExhausted && AdManager.I != null && !AdManager.I.AdsRemoved;

            _mkArrow.gameObject.SetActive(!dry);
            _mkCross.gameObject.SetActive(!dry);
            _chalkAd.gameObject.SetActive(dry);

            if (dry) { Slot(_chalkAd.GetComponent<RectTransform>(), slot, 2); slot += 2; }
            else
            {
                Slot(_mkArrow.GetComponent<RectTransform>(), slot++);
                Slot(_mkCross.GetComponent<RectTransform>(), slot++);
            }

            slot = RefreshItems(st, slot);

            int held = 0;
            foreach (var k in ItemInfo.All) held += st.Count(k);
            bool noItems = st.Cfg.ItemsOn && held == 0
                        && AdManager.I != null && !AdManager.I.AdsRemoved
                        && PlayerProfile.AdGrantsLeft > 0;
            _itemAd.gameObject.SetActive(noItems);
            if (noItems)
            {
                Slot(_itemAd.GetComponent<RectTransform>(), slot, 2);
                var lbl = _itemAd.GetComponentInChildren<Text>();
                if (lbl != null) lbl.text = $"▶ 아이템 ({PlayerProfile.AdGrantsLeft})";
            }
        }

        string ModsText(RunState st)
        {
            string m = "";
            foreach (var x in ModInfo.Pool)
                if (st.Cfg.Has(x)) m += (m.Length > 0 ? " · " : "") + ModInfo.Name(x);
            return m.Length > 0 ? $"  <color=#F2A33C>{m}</color>" : "";
        }

        int RefreshItems(RunState st, int slot)
        {
            int shown = 0;
            foreach (var k in ItemInfo.All)
            {
                int c = st.Count(k);
                bool active = c > 0 || (k == ItemKind.Thread && st.ThreadSet);

                if (!_itemBtns.TryGetValue(k, out var e))
                {
                    var kind = k;
                    var b = UIKit.Btn(_row, "", 22, Palette.Chalk, new Color(0, 0, 0, 0.001f),
                                      () => OnUseItem?.Invoke(kind));
                    var rt = b.GetComponent<RectTransform>();
                    Icon(rt, ProcTex.ItemIcon(kind), Palette.Chalk, 52f, new Vector2(-16, 16));
                    var lbl = UIKit.Label(rt, "", 26, Palette.Chalk, TextAnchor.MiddleRight);
                    UIKit.At(lbl.rectTransform, new Vector2(0.5f, 0.3f), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-16, 0));
                    var nm = UIKit.Label(rt, ItemInfo.Name(kind), 17, Palette.Ash, TextAnchor.LowerCenter);
                    UIKit.At(nm.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 4), new Vector2(0, 30));
                    e = (rt, lbl);
                    _itemBtns[k] = e;
                }

                e.rt.gameObject.SetActive(active);
                if (!active) continue;

                e.label.text = k == ItemKind.Thread && st.ThreadSet ? "귀환" : $"{c}";
                Slot(e.rt, slot + shown);
                shown++;
            }
            return slot + shown;
        }

        void SyncDots(List<Image> list, RectTransform row, int n, float w, float gap,
                      Func<int, Color> col, bool rightAlign = false)
        {
            while (list.Count < n)
            {
                var img = UIKit.Box(row, Color.white);
                img.raycastTarget = false;
                list.Add(img);
            }
            for (int i = 0; i < list.Count; i++)
            {
                bool on = i < n;
                list[i].gameObject.SetActive(on);
                if (!on) continue;
                list[i].color = col(i);
                var rt = list[i].rectTransform;
                if (rightAlign)
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(1, 0.5f);
                    rt.pivot = new Vector2(1, 0.5f);
                    rt.anchoredPosition = new Vector2(-((n - 1 - i) * (w + gap)), 0);
                }
                else
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
                    rt.pivot = new Vector2(0, 0.5f);
                    rt.anchoredPosition = new Vector2(i * (w + gap), 0);
                }
                rt.sizeDelta = new Vector2(w, w);
            }
        }
    }
}
