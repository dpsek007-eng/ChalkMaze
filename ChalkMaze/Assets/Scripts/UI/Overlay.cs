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
        Image _crest;
        readonly List<Button> _buttons = new List<Button>();

        public bool IsOpen => _root.gameObject.activeSelf;

        public void Build(Transform canvas)
        {
            _root = UIKit.Panel(canvas, "Overlay", new Color(0.043f, 0.039f, 0.047f, 0.95f));
            UIKit.Stretch(_root);

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

            // 제목과 본문 사이 가는 선 — 문단이 떠다니지 않게 잡아 준다
            var rule = UIKit.Box(card, new Color(Palette.StoneLit.r, Palette.StoneLit.g, Palette.StoneLit.b, 1f));
            UIKit.At(rule.rectTransform, new Vector2(0.5f,1), new Vector2(0.5f,1),
                     new Vector2(-70, -362), new Vector2(70, -360));

            _body = UIKit.Label(card, "", 27, Palette.Ash, TextAnchor.UpperCenter);
            UIKit.At(_body.rectTransform, new Vector2(0,0.40f), new Vector2(1,1), new Vector2(0,0), new Vector2(0,-392));
            _body.supportRichText = true;

            _stats = UIKit.Label(card, "", 24, Palette.Ash, TextAnchor.LowerCenter);
            UIKit.At(_stats.rectTransform, new Vector2(0,0.29f), new Vector2(1,0.40f), Vector2.zero, Vector2.zero);
            _stats.supportRichText = true;

            _btnCol = UIKit.Empty(card, "Buttons");
            UIKit.At(_btnCol, new Vector2(0,0), new Vector2(1,0.29f), Vector2.zero, Vector2.zero);

            _root.gameObject.SetActive(false);
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
                img.color = c.Primary ? Palette.Ember
                          : c.IsAd   ? new Color(Palette.Fire.r, Palette.Fire.g, Palette.Fire.b, 0.18f)
                                     : new Color(Palette.StoneLit.r, Palette.StoneLit.g, Palette.StoneLit.b, 0.55f);
                txt.alignment = TextAnchor.MiddleCenter;

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

        public void Hide() => _root.gameObject.SetActive(false);

        /// 데스크톱에서 스페이스/엔터로 진행할 수 있게 한다.
        public void ClickPrimary()
        {
            if (!IsOpen) return;
            for (int i = 0; i < _buttons.Count; i++)
                if (_buttons[i].gameObject.activeSelf) { _buttons[i].onClick.Invoke(); return; }
        }
    }
}
