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
        RectTransform _fireRow, _pipRow, _itemRow, _toast;
        readonly List<Image> _fireDots = new List<Image>();
        readonly List<Image> _pips = new List<Image>();
        readonly Dictionary<ItemKind, (RectTransform rt, Text label)> _itemBtns
            = new Dictionary<ItemKind, (RectTransform, Text)>();

        Button _mkArrow, _mkCross, _chalkAd, _itemAd;
        float _toastUntil;

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

            var barBg = UIKit.Box(top, new Color(Palette.StoneLit.r, Palette.StoneLit.g, Palette.StoneLit.b, 1f));
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
            UIKit.At(_toast, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(-400, -60), new Vector2(400, 60));
            _toastBig = UIKit.Label(_toast, "", 46, Palette.Fire, TextAnchor.MiddleCenter);
            UIKit.At(_toastBig.rectTransform, new Vector2(0, 0.45f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            _toastSmall = UIKit.Label(_toast, "", 24, Palette.Ash, TextAnchor.UpperCenter);
            UIKit.At(_toastSmall.rectTransform, new Vector2(0, 0), new Vector2(1, 0.45f), Vector2.zero, Vector2.zero);
            _toast.gameObject.SetActive(false);

            // ── 아이템 줄 ──
            _itemRow = UIKit.Empty(canvas, "Items");
            // D-패드(70~370)·표식 버튼(90~330) 위로 올린다. 겹치면 터치가 먹힌다.
            UIKit.At(_itemRow, new Vector2(0, 0), new Vector2(1, 0), new Vector2(36, 96), new Vector2(-36, 194));

            // ── 하단 조작 ──
            // 방향키를 없앴으므로 표식 버튼이 아래쪽을 좌우로 나눠 쓴다.
            var marks = UIKit.Empty(canvas, "Marks");
            UIKit.At(marks, new Vector2(0, 0), new Vector2(1, 0), new Vector2(36, 212), new Vector2(-36, 344));

            _mkArrow = MakeMarkBtn(marks, "↑  지나간 방향", MarkKind.Arrow,
                                   new Vector2(0f, 0f), new Vector2(0.485f, 1f));
            _mkCross = MakeMarkBtn(marks, "✕  막다른 길", MarkKind.DeadEnd,
                                   new Vector2(0.515f, 0f), new Vector2(1f, 1f));

            // 분필이 떨어졌을 때만 이 버튼이 두 칸을 덮는다.
            // 평소에는 숨어 있어야 광고가 방해가 되지 않는다.
            _chalkAd = UIKit.Btn(marks, "▶  광고 보고 분필 +2", 26, Palette.Void, Palette.Fire,
                                 () => OnWatchForChalk?.Invoke());
            UIKit.At(_chalkAd.GetComponent<RectTransform>(),
                     new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            _chalkAd.gameObject.SetActive(false);
        }

        Button MakeMarkBtn(Transform parent, string txt, MarkKind kind, Vector2 aMin, Vector2 aMax)
        {
            var b = UIKit.Btn(parent, txt, 24, Palette.Chalk,
                              new Color(Palette.StoneLit.r, Palette.StoneLit.g, Palette.StoneLit.b, 0.55f),
                              () => OnMark?.Invoke(kind));
            UIKit.At(b.GetComponent<RectTransform>(), aMin, aMax, Vector2.zero, Vector2.zero);
            return b;
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
            string mods = "";
            foreach (var m in ModInfo.Pool)
                if (st.Cfg.Has(m)) mods += (mods.Length > 0 ? " · " : "") + ModInfo.Name(m);
            _lv.text = $"{st.Level}층   <color=#6E6875>{st.Runs}회차</color>\n"
                     + $"<size=20><color=#6E6875>{st.Cfg.Chapter}</color>"
                     + (mods.Length > 0 ? $"  <color=#F2A33C>{mods}</color>" : "") + "</size>";
            _lv.supportRichText = true;

            float r = st.Cfg.Fuel > 0 ? Mathf.Clamp01((float)st.Fuel / st.Cfg.Fuel) : 0;
            _fuelNum.text = Mathf.Max(0, st.Fuel).ToString();
            _fuelFill.fillAmount = r;
            bool low = r < 0.25f;
            _fuelFill.color = low ? Palette.Danger : Palette.Ember;
            _fuelNum.color  = low ? Palette.Danger : Palette.Ember;

            SyncDots(_fireDots, _fireRow, st.Bonfires.Count, 22, 12,
                     i => st.Bonfires[i].Lit ? Palette.Fire : Palette.StoneLit);
            SyncDots(_pips, _pipRow, st.Cfg.Chalk, 18, 10,
                     i => i < st.Marks.Count ? Palette.StoneLit : Palette.Chalk, true);

            // 아이템이 하나도 없으면 그 자리에 광고 버튼을 둔다.
            // 가진 게 있을 때는 숨어 있어야 방해가 되지 않는다.
            if (_itemAd == null)
            {
                _itemAd = UIKit.Btn(_itemRow, "▶  광고 보고 아이템 받기", 24,
                                    Palette.Void, Palette.Fire, () => OnWatchForItem?.Invoke());
                UIKit.At(_itemAd.GetComponent<RectTransform>(),
                         new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            }
            int held = 0;
            foreach (var k in ItemInfo.All) held += st.Count(k);
            bool noItems = st.Cfg.ItemsOn && held == 0
                        && AdManager.I != null && !AdManager.I.AdsRemoved
                        && PlayerProfile.AdGrantsLeft > 0;
            _itemAd.gameObject.SetActive(noItems);
            var lbl = _itemAd.GetComponentInChildren<Text>();
            if (lbl != null) lbl.text = $"▶  광고 보고 아이템 받기 (오늘 {PlayerProfile.AdGrantsLeft}회)";

            // 분필이 떨어졌으면 표식 버튼 자리를 광고 버튼이 덮는다
            bool dry = st.ChalkExhausted && AdManager.I != null && !AdManager.I.AdsRemoved;
            if (_chalkAd != null)
            {
                _chalkAd.gameObject.SetActive(dry);
                _mkArrow.gameObject.SetActive(!dry);
                _mkCross.gameObject.SetActive(!dry);
            }

            RefreshItems(st);
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
                float x = i * (w + gap);
                if (rightAlign)
                {
                    rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
                    rt.pivot = new Vector2(1, 0.5f);
                    rt.anchoredPosition = new Vector2(-((n - 1 - i) * (w + gap)), 0);
                }
                else
                {
                    rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
                    rt.pivot = new Vector2(0, 0.5f);
                    rt.anchoredPosition = new Vector2(x, 0);
                }
                rt.sizeDelta = new Vector2(w, w);
            }
        }

        void RefreshItems(RunState st)
        {
            int shown = 0;
            foreach (var k in ItemInfo.All)
            {
                int c = st.Count(k);
                bool active = c > 0 || (k == ItemKind.Thread && st.ThreadSet);
                if (!_itemBtns.TryGetValue(k, out var e))
                {
                    var kind = k;
                    var b = UIKit.Btn(_itemRow, "", 22, Palette.Chalk,
                                      new Color(Palette.StoneLit.r, Palette.StoneLit.g, Palette.StoneLit.b, 0.6f),
                                      () => OnUseItem?.Invoke(kind));
                    var rt = b.GetComponent<RectTransform>();
                    var lbl = b.GetComponentInChildren<Text>();
                    e = (rt, lbl);
                    _itemBtns[k] = e;
                }
                e.rt.gameObject.SetActive(active);
                if (!active) continue;

                string label = k == ItemKind.Thread && st.ThreadSet
                    ? "실 · 귀환"
                    : $"{ItemInfo.Name(k)} {c}";
                e.label.text = label;

                e.rt.anchorMin = new Vector2(0, 0); e.rt.anchorMax = new Vector2(0, 1);
                e.rt.pivot = new Vector2(0, 0.5f);
                e.rt.anchoredPosition = new Vector2(shown * 210, 0);
                e.rt.sizeDelta = new Vector2(196, 0);
                shown++;
            }
        }
    }

    /// 길게 누르면 계속 이동
    public sealed class HoldRepeat : MonoBehaviour,
        UnityEngine.EventSystems.IPointerDownHandler,
        UnityEngine.EventSystems.IPointerUpHandler,
        UnityEngine.EventSystems.IPointerExitHandler
    {
        public Action Action;
        public Image Flash;                 // 눌렀을 때 밝아지는 화살표
        bool _held; float _next;
        const float Delay = 0.28f, Rate = 0.135f;
        static readonly Color Idle = new Color(0.91f, 0.89f, 0.84f, 0.42f);
        static readonly Color Lit  = new Color(1f, 0.48f, 0.24f, 1f);

        void Glow(bool on) { if (Flash != null) Flash.color = on ? Lit : Idle; }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) { _held = true; _next = Time.time + Delay; Glow(true); }
        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e)   { _held = false; Glow(false); }
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) { _held = false; Glow(false); }

        void Update()
        {
            if (!_held) return;
            if (Time.time < _next) return;
            _next = Time.time + Rate;
            Action?.Invoke();
        }
    }
}
