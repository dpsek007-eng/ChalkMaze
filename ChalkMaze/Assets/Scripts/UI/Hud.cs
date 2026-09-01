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

        Text _lv, _fuelNum, _toastBig, _toastSmall;
        Image _fuelFill;
        RectTransform _fireRow, _pipRow, _itemRow, _toast;
        readonly List<Image> _fireDots = new List<Image>();
        readonly List<Image> _pips = new List<Image>();
        readonly Dictionary<ItemKind, (RectTransform rt, Text label)> _itemBtns
            = new Dictionary<ItemKind, (RectTransform, Text)>();

        Button _mkArrow, _mkCross;
        Sprite _dirArrow;
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
            // D-패드가 y 48~468 을 쓰므로 그 위로 올린다. 겹치면 터치가 가로채인다.
            UIKit.At(_itemRow, new Vector2(0, 0), new Vector2(1, 0), new Vector2(36, 500), new Vector2(-36, 598));

            // ── 하단 조작 ──
            _dirArrow = ProcTex.ArrowMark();
            var pad = UIKit.Empty(canvas, "Dpad");
            UIKit.At(pad, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 48), new Vector2(444, 468));
            MakeDirBtn(pad, 0, new Vector2(0.33f, 0.66f), new Vector2(0.67f, 1f),    0f);
            MakeDirBtn(pad, 3, new Vector2(0f,    0.33f), new Vector2(0.34f, 0.67f), 90f);
            MakeDirBtn(pad, 1, new Vector2(0.66f, 0.33f), new Vector2(1f,    0.67f), -90f);
            MakeDirBtn(pad, 2, new Vector2(0.33f, 0f),    new Vector2(0.67f, 0.34f), 180f);

            var marks = UIKit.Empty(canvas, "Marks");
            UIKit.At(marks, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-340, 90), new Vector2(-40, 330));

            _mkArrow = MakeMarkBtn(marks, "↑  지나간 방향", MarkKind.Arrow,
                                   new Vector2(0, 0.54f), new Vector2(1, 1));
            _mkCross = MakeMarkBtn(marks, "✕  막다른 길", MarkKind.DeadEnd,
                                   new Vector2(0, 0f), new Vector2(1, 0.46f));
        }

        /// 방향키는 UI 박스가 아니라 분필로 그은 화살표다.
        /// 게임 안의 분필 자국과 같은 스프라이트를 써서 화면이 한 세계로 읽히게 한다.
        /// 터치 영역은 보이는 것보다 넓다 — 작은 화면에서 오조작이 나지 않도록.
        void MakeDirBtn(Transform parent, int dir, Vector2 aMin, Vector2 aMax, float rotDeg)
        {
            // 투명한 넓은 터치 영역
            var hit = UIKit.Panel(parent, "dir", new Color(0, 0, 0, 0.001f));
            UIKit.At(hit, aMin, aMax, Vector2.zero, Vector2.zero);
            var b = hit.gameObject.AddComponent<Button>();
            b.targetGraphic = hit.GetComponent<Image>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => OnDir?.Invoke(dir));

            // 그 안에 화살표
            var go = new GameObject("arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(hit, false);
            var img = go.GetComponent<Image>();
            img.sprite = _dirArrow;
            img.color = new Color(Palette.Chalk.r, Palette.Chalk.g, Palette.Chalk.b, 0.42f);
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(120, 120);
            rt.localRotation = Quaternion.Euler(0, 0, rotDeg);

            var rep = hit.gameObject.AddComponent<HoldRepeat>();
            rep.Action = () => OnDir?.Invoke(dir);
            rep.Flash = img;
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
