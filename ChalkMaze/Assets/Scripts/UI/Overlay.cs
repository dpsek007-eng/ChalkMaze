using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChalkMaze
{
    public sealed class Overlay : MonoBehaviour
    {
        RectTransform _root, _btnCol;
        Text _eyebrow, _title, _body, _stats;
        Image _crest, _halo, _rule;
        Sprite _outline, _fill;
        Button _gear, _help, _daily;
        RectTransform _descend;
        Image _chevron;
        Text _cta;
        Button _tapAnywhere;
        bool _titleMode;
        float _flicker;
        readonly List<Button> _buttons = new List<Button>();

        public bool IsOpen => _root.gameObject.activeSelf;

        public void Build(Transform canvas)
        {
            // 완전히 덮으면 게임 세계가 사라져 문서처럼 보인다.
            // 미로가 어렴풋이 비치도록 남겨 둔다.
            _root = UIKit.Panel(canvas, "Overlay", new Color(0.043f, 0.039f, 0.047f, 0.88f));
            UIKit.Stretch(_root);

            // 상징 뒤에서 타오르는 불빛 — 어둠에 깊이를 준다
            var haloGo = new GameObject("halo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            haloGo.transform.SetParent(_root, false);
            _halo = haloGo.GetComponent<Image>();
            _halo.sprite = ProcTex.Glow();
            _halo.color = new Color(Palette.Ember.r, Palette.Ember.g, Palette.Ember.b, 0.30f);
            _halo.raycastTarget = false;
            var hrt = _halo.rectTransform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f);
            hrt.anchoredPosition = new Vector2(0, 430);
            hrt.sizeDelta = new Vector2(900, 900);

            // 화면 아무 데나 눌러 시작. 버튼 상자는 게임 타이틀에 어울리지 않는다.
            _tapAnywhere = _root.gameObject.AddComponent<Button>();
            _tapAnywhere.targetGraphic = _root.GetComponent<Image>();
            _tapAnywhere.transition = Selectable.Transition.None;
            _tapAnywhere.enabled = false;

            _outline = ProcTex.RectOutline();
            _fill = ProcTex.RectFill();

            // 설정·규칙은 구석 아이콘으로 뺀다. 목록에 끼면 시작 버튼이 묻힌다.
            _gear = CornerBtn(_root, ProcTex.GearIcon(), new Vector2(1,1), new Vector2(-104,-104));
            _help = CornerBtn(_root, ProcTex.QuestionIcon(), new Vector2(1,1), new Vector2(-216,-104));

            // 오늘의 미로 — 타이틀에서만 보이는 부차 진입로
            _daily = UIKit.Btn(_root, "", 30, Palette.Chalk, new Color(0,0,0,0.001f), null);
            var drt = _daily.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.5f, 0); drt.anchorMax = new Vector2(0.5f, 0);
            drt.pivot = new Vector2(0.5f, 0);
            drt.anchoredPosition = new Vector2(0, 190);
            drt.sizeDelta = new Vector2(620, 96);
            _daily.gameObject.SetActive(false);

            var card = UIKit.Empty(_root, "Card");
            UIKit.At(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                     new Vector2(-420, -520), new Vector2(420, 520));

            // 게임의 상징(벽 사이의 화살표)을 얹는다. 글자만 있으면 문서처럼 보인다.
            var crestGo = new GameObject("crest", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            crestGo.transform.SetParent(card, false);
            _crest = crestGo.GetComponent<Image>();
            _crest.sprite = ProcTex.Crest();
            _crest.color = new Color(Palette.Chalk.r, Palette.Chalk.g, Palette.Chalk.b, 0.9f);
            _crest.raycastTarget = false;
            UIKit.At(_crest.rectTransform, new Vector2(0.5f,1), new Vector2(0.5f,1),
                     new Vector2(-90, -196), new Vector2(90, -16));

            _eyebrow = UIKit.Label(card, "", 22, Palette.Ember, TextAnchor.UpperCenter);
            UIKit.At(_eyebrow.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(0,-238), new Vector2(0,-204));

            _title = UIKit.Label(card, "", 72, Palette.Chalk, TextAnchor.UpperCenter);
            UIKit.At(_title.rectTransform, new Vector2(0,1), new Vector2(1,1), new Vector2(0,-336), new Vector2(0,-244));
            _title.supportRichText = true;

            // 분필로 그은 획. 반듯한 선은 문서 같아 보인다.
            var ruleGo = new GameObject("rule", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ruleGo.transform.SetParent(card, false);
            _rule = ruleGo.GetComponent<Image>();
            _rule.sprite = ProcTex.ChalkRule();
            _rule.color = new Color(Palette.Chalk.r, Palette.Chalk.g, Palette.Chalk.b, 0.42f);
            _rule.raycastTarget = false;
            UIKit.At(_rule.rectTransform, new Vector2(0.5f,1), new Vector2(0.5f,1),
                     new Vector2(-130, -376), new Vector2(130, -352));

            _body = UIKit.Label(card, "", 27, Palette.Ash, TextAnchor.UpperCenter);
            UIKit.At(_body.rectTransform, new Vector2(0,0.47f), new Vector2(1,1), new Vector2(0,0), new Vector2(0,-392));
            _body.supportRichText = true;

            _stats = UIKit.Label(card, "", 24, Palette.Ash, TextAnchor.LowerCenter);
            UIKit.At(_stats.rectTransform, new Vector2(0,0.37f), new Vector2(1,0.47f), new Vector2(0,0), new Vector2(0,-14));
            _stats.supportRichText = true;

            // 내려가는 표시 — 분필로 그은 아래 화살표가 숨쉬듯 뛴다
            _descend = UIKit.Empty(_root, "Descend");
            UIKit.At(_descend, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                     new Vector2(-220, 420), new Vector2(220, 600));

            var chGo = new GameObject("chevron", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            chGo.transform.SetParent(_descend, false);
            _chevron = chGo.GetComponent<Image>();
            _chevron.sprite = ProcTex.ArrowMark();
            _chevron.color = new Color(Palette.Chalk.r, Palette.Chalk.g, Palette.Chalk.b, 0.75f);
            _chevron.raycastTarget = false;
            _chevron.preserveAspect = true;
            var crt = _chevron.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(78, 78);
            crt.anchoredPosition = new Vector2(0, -46);
            crt.localRotation = Quaternion.Euler(0, 0, 180f);   // 아래를 가리킨다

            _cta = UIKit.Label(_descend, "", 26, Palette.Ash, TextAnchor.LowerCenter);
            UIKit.At(_cta.rectTransform, new Vector2(0,0), new Vector2(1,0), new Vector2(0,6), new Vector2(0,44));
            _descend.gameObject.SetActive(false);

            _btnCol = UIKit.Empty(card, "Buttons");
            UIKit.At(_btnCol, new Vector2(0,0), new Vector2(1,0.36f), Vector2.zero, Vector2.zero);

            _root.gameObject.SetActive(false);
        }

        Button CornerBtn(Transform parent, Sprite sp, Vector2 anchor, Vector2 pos)
        {
            var go = new GameObject("corner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.color = new Color(Palette.Chalk.r, Palette.Chalk.g, Palette.Chalk.b, 0.45f);
            img.preserveAspect = true;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(84, 84);
            rt.anchoredPosition = pos;
            return go.AddComponent<Button>();
        }

        /// 구석 아이콘을 켜고 끈다. 시작 화면에서만 보인다.
        /// 오늘의 미로 진입. 타이틀에서만 켠다.
        public void SetDaily(string label, Action onPick)
        {
            _daily.gameObject.SetActive(onPick != null);
            _daily.onClick.RemoveAllListeners();
            if (onPick == null) return;
            var t = _daily.GetComponentInChildren<Text>();
            if (t != null) { t.text = label; t.color = Palette.Fire; }
            _daily.onClick.AddListener(() => { Hide(); onPick(); });
        }

        public void SetCorner(Action settings, Action rules)
        {
            _gear.gameObject.SetActive(settings != null);
            _help.gameObject.SetActive(rules != null);
            _gear.onClick.RemoveAllListeners();
            _help.onClick.RemoveAllListeners();
            if (settings != null) _gear.onClick.AddListener(() => { Hide(); settings(); });
            if (rules != null) _help.onClick.AddListener(() => { Hide(); rules(); });
        }

        public struct Choice
        {
            public string Label;
            public Action OnPick;
            public bool Primary;
            public bool IsAd;
        }

        public void Show(string eyebrow, string title, string body, string stats,
                         params Choice[] choices)
        {
            SetCorner(null, null);   // 기본은 숨김. 시작 화면만 따로 켠다.
            _daily.gameObject.SetActive(false);
            _titleMode = false;
            _descend.gameObject.SetActive(false);
            _btnCol.gameObject.SetActive(true);
            _tapAnywhere.enabled = false;
            _tapAnywhere.onClick.RemoveAllListeners();
            _eyebrow.text = eyebrow;
            _title.text = title;
            _body.text = body;
            _stats.text = stats;

            for (int i = 0; i < _buttons.Count; i++) _buttons[i].gameObject.SetActive(false);

            int n = choices.Length;
            for (int i = 0; i < n; i++)
            {
                Button b;
                if (i < _buttons.Count) { b = _buttons[i]; b.gameObject.SetActive(true); }
                else
                {
                    b = UIKit.Btn(_btnCol, "", 28, Palette.Chalk, Palette.StoneLit, null);
                    _buttons.Add(b);
                }
                var c = choices[i];
                var txt = b.GetComponentInChildren<Text>();
                txt.text = c.IsAd ? "▶  " + c.Label : c.Label;
                txt.color = c.Primary ? Palette.Void : Palette.Chalk;

                var img = b.GetComponent<Image>();
                if (c.Primary)
                {
                    img.sprite = _fill;
                    img.type = Image.Type.Sliced;
                    img.color = Palette.Ember;
                }
                else
                {
                    // 채우지 않고 테두리만 — 주 버튼 하나만 눈에 들어오게 한다
                    img.sprite = _outline;
                    img.type = Image.Type.Sliced;
                    img.color = c.IsAd
                        ? new Color(Palette.Fire.r, Palette.Fire.g, Palette.Fire.b, 0.85f)
                        : new Color(Palette.Chalk.r, Palette.Chalk.g, Palette.Chalk.b, 0.28f);
                }
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = c.Primary ? Palette.Void
                          : c.IsAd   ? Palette.Fire
                                     : Palette.Chalk;

                b.onClick.RemoveAllListeners();
                var pick = c.OnPick;
                b.onClick.AddListener(() => { Hide(); pick?.Invoke(); });

                // 버튼 수로 영역을 n등분하면 1개일 때 화면의 3분의 1을 먹는다.
                // 높이를 고정하고 아래에서부터 쌓는다.
                const float H = 104f, Gap = 12f;
                var rt = b.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(0.5f, 0);
                rt.offsetMin = new Vector2(0, (n - 1 - i) * (H + Gap));
                rt.offsetMax = new Vector2(0, (n - 1 - i) * (H + Gap) + H);
            }

            _root.gameObject.SetActive(true);
        }

        void Update()
        {
            // 불빛이 살아 있게 — 정지 화면은 죽어 보인다
            if (_halo == null || !_root.gameObject.activeSelf) return;
            _flicker = 1f + Mathf.Sin(Time.unscaledTime * 1.7f) * 0.06f
                          + Mathf.Sin(Time.unscaledTime * 4.3f) * 0.03f;
            _halo.rectTransform.localScale = Vector3.one * _flicker;
            var c = _halo.color;
            _halo.color = new Color(c.r, c.g, c.b, 0.30f * (0.85f + 0.15f * _flicker));
            if (_crest != null)
                _crest.color = new Color(Palette.Chalk.r, Palette.Chalk.g, Palette.Chalk.b,
                                         0.88f + 0.08f * (_flicker - 1f) * 10f);

            if (_titleMode && _chevron != null)
            {
                // 아래로 살짝 내려갔다 올라오며 알파가 함께 오르내린다
                float t = Mathf.Sin(Time.unscaledTime * 2.1f);
                _chevron.rectTransform.anchoredPosition = new Vector2(0, -46f + t * 7f);
                _chevron.color = new Color(Palette.Chalk.r, Palette.Chalk.g, Palette.Chalk.b,
                                           0.55f + 0.3f * (t * 0.5f + 0.5f));
                if (_cta != null)
                    _cta.color = new Color(Palette.Ash.r, Palette.Ash.g, Palette.Ash.b,
                                           0.6f + 0.35f * (t * 0.5f + 0.5f));
            }
        }

        /// 타이틀 전용 — 버튼 상자 대신 '내려가기' 표시를 띄우고 화면 전체를 누르게 한다.
        public void ShowTitle(string eyebrow, string title, string body, string stats,
                              string cta, Action onStart)
        {
            Show(eyebrow, title, body, stats);          // 버튼 없이
            _titleMode = true;
            _btnCol.gameObject.SetActive(false);
            _descend.gameObject.SetActive(true);
            _cta.text = cta;

            _tapAnywhere.enabled = true;
            _tapAnywhere.onClick.RemoveAllListeners();
            _tapAnywhere.onClick.AddListener(() => { Hide(); onStart?.Invoke(); });
        }

        public void Hide() => _root.gameObject.SetActive(false);

        /// 데스크톱에서 스페이스/엔터로 진행할 수 있게 한다.
        public void ClickPrimary()
        {
            if (!IsOpen) return;
            // 타이틀에는 버튼이 없다. 화면 누르기를 대신 실행한다.
            if (_titleMode) { _tapAnywhere.onClick.Invoke(); return; }
            for (int i = 0; i < _buttons.Count; i++)
                if (_buttons[i].gameObject.activeSelf) { _buttons[i].onClick.Invoke(); return; }
        }
    }
}
