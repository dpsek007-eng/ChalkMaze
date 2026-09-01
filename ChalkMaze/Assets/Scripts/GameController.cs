using System;
using UnityEngine;

namespace ChalkMaze
{
    public sealed class GameController : MonoBehaviour
    {
        public MazeMesh Mesh;
        public GlyphLayer Glyphs;
        public CameraRig Rig;
        public Torch TorchFx;
        public Hud Hud;
        public Overlay Overlay;

        RunState _st;
        System.Random _rng;
        int _resumeLevel = 1;
        bool _dailyMode;
        bool _busy;

        public RunState State => _st;

        void Start()
        {
            _st = new RunState();
            _rng = new System.Random(Environment.TickCount);

            Hud.OnDir     += TryMove;
            Hud.OnMark    += DoMark;
            Hud.OnUseItem += UseItem;

            int streak = PlayerProfile.TouchStreak();
            string reward = PlayerProfile.ClaimStreakReward();

            _resumeLevel = Mathf.Max(1, PlayerProfile.BestLevel);
            LoadLevel(1);
            ShowIntro(streak, reward);
        }

        void LoadLevel(int lv)
        {
            _st.LoadLevel(lv, _rng);

            // 영구 업그레이드 반영
            if (PlayerProfile.ChalkBonus > 0) _st.GrantChalk(PlayerProfile.ChalkBonus);
            _st.BonusSight = PlayerProfile.LanternLevel;   // 영구 랜턴 강화
            foreach (var k in ItemInfo.All)
            {
                int owned = PlayerProfile.ItemCount(k);
                if (owned > 0) { _st.Add(k, owned); PlayerProfile.AddItem(k, -owned); }
            }

            Mesh.Build(_st.Maze);
            RefreshAll();
            Rig.SnapTo(Glyphs.PlayerT.position);
        }

        void RefreshAll()
        {
            Mesh.ApplyVisibility(_st.Visible);
            Glyphs.Refresh(_st);
            Hud.Refresh(_st);
            TorchFx.SetFuel(_st.Cfg.Fuel > 0 ? (float)_st.Fuel / _st.Cfg.Fuel : 0f, _st.SightRange);
            TorchFx.transform.position = Glyphs.PlayerT.position;
            Rig.Target = Glyphs.PlayerT;
        }

        void Update()
        {
            if (Overlay.IsOpen)
            {
                if (InputProbe.SubmitPressed()) Overlay.ClickPrimary();
                return;
            }
            if (_busy) return;
            for (int d = 0; d < 4; d++)
                if (InputProbe.DirPressed(d)) TryMove(d);
            TorchFx.transform.position = Glyphs.PlayerT.position;
            Glyphs.TickPulse(_st);
        }

        public void TryMove(int dir)
        {
            if (_busy || Overlay.IsOpen) return;
            var res = _st.TryMove(dir);

            switch (res)
            {
                case MoveResult.Blocked: return;

                case MoveResult.BlockedByPit:
                    Hud.Toast("구덩이", _st.Count(ItemKind.Plank) > 0 ? "판자를 놓으면 건널 수 있다" : "판자가 필요하다");
                    return;

                case MoveResult.LockedExit:
                    Hud.Toast("문이 잠겼다", "이 층 어딘가에 열쇠가 있다");
                    RefreshAll();
                    return;

                case MoveResult.GotKey:
                    Rig.Kick(0.7f);
                    Hud.Toast("열쇠를 찾았다", "이제 문으로 돌아가면 된다");
                    break;

                case MoveResult.LitBonfire:
                    Rig.Kick(1f);
                    Hud.Toast("화톳불을 밝혔다", "여기서 다시 시작합니다 · 횃불 회복");
                    break;

                case MoveResult.PickedUp:
                    Hud.Toast(ItemInfo.Name(_st.LastPickup), ItemInfo.Hint(_st.LastPickup));
                    OfferDoubleItem();
                    break;

                case MoveResult.Won:  RefreshAll(); ShowWin(); return;
                case MoveResult.Died: RefreshAll(); ShowDead(); return;
            }
            RefreshAll();
        }

        public void DoMark(MarkKind kind)
        {
            if (!_st.ToggleMark(kind)) { Hud.Toast("분필이 없다", "이미 찍은 자국을 회수해서 옮기세요"); return; }
            RefreshAll();
        }

        void UseItem(ItemKind k)
        {
            bool ok = k switch
            {
                ItemKind.Oil     => _st.UseOil(),
                ItemKind.Shovel  => _st.UseShovel(_st.LastDir),
                ItemKind.Plank   => _st.UsePlank(_st.LastDir),
                ItemKind.Thread  => _st.UseThread(),
                ItemKind.Compass => _st.UseCompass(),
                ItemKind.Lantern => _st.UseLantern(),
                _ => false
            };

            if (!ok)
            {
                Hud.Toast("쓸 수 없다",
                    k == ItemKind.Shovel ? "바라보는 쪽에 뚫을 벽이 없다"
                  : k == ItemKind.Plank  ? "바라보는 쪽에 구덩이가 없다"
                  : "지금은 사용할 수 없다");
                return;
            }

            if (k == ItemKind.Shovel) { Rig.Kick(0.8f); Mesh.Build(_st.Maze); Hud.Toast("벽을 뚫었다", "어디를 뚫었는지 기억하세요"); }
            if (k == ItemKind.Thread) Hud.Toast(_st.ThreadSet ? "매듭을 묶었다" : "실을 따라 돌아왔다", "");
            if (k == ItemKind.Compass) Hud.Toast("나침반", "12걸음 동안 출구 방향이 보인다");
            if (k == ItemKind.Lantern) Hud.Toast("랜턴", $"{RunState.LanternDuration}걸음 동안 시야 +{RunState.LanternBoost}");
            RefreshAll();
        }

        // ── 광고 연동 ─────────────────────────────────
        void OfferDoubleItem()
        {
            // 아이템을 주운 직후가 리워드 시청률이 가장 높은 순간이다.
            if (AdManager.I == null) return;
            if (UnityEngine.Random.value > 0.45f) return;   // 매번 띄우면 피로해진다
            var got = _st.LastPickup;
            Overlay.Show("아이템 발견", ItemInfo.Name(got), ItemInfo.Hint(got), "",
                new Overlay.Choice { Label = "광고 보고 하나 더", IsAd = true, OnPick = () =>
                    AdManager.I.ShowRewarded(AdSlot.DoubleItem, ok => { if (ok) _st.Add(got); RefreshAll(); }) },
                new Overlay.Choice { Label = "그냥 계속", OnPick = RefreshAll });
        }

        void ShowIntro(int streak, string reward)
        {
            string body =
                "횃불은 <color=#E8E3D6>정해진 걸음 수</color>만큼만 탑니다. 지나온 길은 다시 어두워지고,\n" +
                "남는 건 <color=#E8E3D6>분필 자국과 화톳불</color>뿐입니다.\n\n" +
                "<color=#F2A33C>화톳불</color> — 밝히면 여기서 부활하고 횃불이 가득 찹니다. 어둠 너머에서도 보입니다.\n" +
                $"<color=#7C7490>{LevelConfig.ItemsFromLevel}층부터 아이템, {LevelConfig.PitsFromLevel}층부터 구덩이가 나옵니다.</color>";

            var choices = new System.Collections.Generic.List<Overlay.Choice>();
            if (_resumeLevel > 1)
                choices.Add(new Overlay.Choice
                {
                    Label = $"{_resumeLevel}층부터 이어하기 · {LevelConfig.ChapterOf(_resumeLevel)}",
                    Primary = true,
                    OnPick = () => LoadLevel(_resumeLevel)
                });
            choices.Add(new Overlay.Choice
            {
                Label = _resumeLevel > 1 ? "1층부터 다시" : "내려가기",
                Primary = _resumeLevel <= 1,
                OnPick = () => { LoadLevel(1); RefreshAll(); }
            });

            Overlay.Show($"전 {LevelConfig.FinalLevel}층", "분필 <color=#FF7A3D>미로</color>", body,
                $"<color=#6E6875>연속 출석 {streak}일 · {reward}</color>",
                choices.ToArray());
        }

        void ShowDead()
        {
            int lit = _st.FiresLit;
            string where = lit > 0 ? "마지막으로 밝힌 <color=#F2A33C>화톳불</color>에서" : "<color=#E8E3D6>입구</color>에서";
            string stats = $"{_st.Runs}회차 · 총 {_st.Steps}걸음 · 화톳불 {lit}/{_st.Bonfires.Count}";

            Overlay.Show("횃불이 꺼졌다", "어둠 속에 <color=#FF7A3D>남다</color>",
                $"{where} 다시 시작합니다.\n분필 자국과 밝힌 화톳불은 그대로입니다.", stats,
                new Overlay.Choice { Label = "광고 보고 그 자리에서 계속", IsAd = true, OnPick = () =>
                    AdManager.I?.ShowRewarded(AdSlot.RelightTorch, ok =>
                    {
                        if (ok) { _st.RefillFuel(); RefreshAll(); }
                        else { Restart(); }
                    }) },
                new Overlay.Choice { Label = "다시 들어가기", Primary = true, OnPick = Restart });
        }

        void Restart()
        {
            _st.Runs++;
            _st.Respawn();
            RefreshAll();
            Rig.SnapTo(Glyphs.PlayerT.position);
        }

        void ShowWin()
        {
            int lv = _st.Level;
            if (lv > PlayerProfile.BestLevel) PlayerProfile.BestLevel = lv;

            var next = LevelConfig.For(lv + 1);
            string stats = $"{_st.Runs}회차 · 총 {_st.Steps}걸음 · 쓴 분필 {_st.Marks.Count}";
            string body = $"다음은 <color=#E8E3D6>{next.Size}×{next.Size}</color> 미로, 분필 <color=#E8E3D6>{next.Chalk}개</color>, 화톳불 <color=#E8E3D6>{next.Bonfires}개</color>.";

            if (next.Chapter != _st.Cfg.Chapter)
                body += $"\n\n<color=#F2A33C>새 구역 — {next.Chapter}</color>";

            var fresh = LevelConfig.NewIn(lv + 1);
            foreach (var m in ModInfo.Pool)
                if ((fresh & m) != 0)
                    body += $"\n\n<color=#F2A33C>{ModInfo.Name(m)}</color>  <color=#6E6875>{ModInfo.Desc(m)}</color>";

            if (lv + 1 == LevelConfig.ItemsFromLevel) body += "\n\n<color=#F2A33C>다음 층부터 바닥에 아이템이 떨어져 있습니다.</color>";
            if (lv + 1 == LevelConfig.PitsFromLevel)  body += "\n\n<color=#F2A33C>다음 층부터 구덩이가 나타납니다. 판자가 필요합니다.</color>";

            string eyebrow = lv >= LevelConfig.FinalLevel
                ? "최심부 도달"
                : $"{_st.Cfg.Chapter} · {lv}층 돌파";
            var picks = new System.Collections.Generic.List<Overlay.Choice>
            {
                new Overlay.Choice { Label = $"{lv + 1}층으로", Primary = true, OnPick = () => GoNext(lv + 1) }
            };

            // 보급품 : 층을 넘어가는 순간에만 제안한다. 새 방해를 만들지 않는다.
            // 하루 상한을 두어 '광고를 많이 본 사람이 강한' 구조가 되지 않게 한다.
            if (next.ItemsOn && PlayerProfile.AdGrantsLeft > 0 && AdManager.I != null)
            {
                picks.Add(new Overlay.Choice
                {
                    Label = $"보급품 받기 (오늘 {PlayerProfile.AdGrantsLeft}회 남음)",
                    IsAd = true,
                    OnPick = () => AdManager.I.ShowRewarded(AdSlot.SupplyCache, ok =>
                    {
                        if (ok)
                        {
                            var got = ItemInfo.Roll(_rng);
                            PlayerProfile.AddItem(got, 1);
                            PlayerProfile.ConsumeAdGrant();
                            Hud.Toast(ItemInfo.Name(got), "다음 층에서 쓸 수 있습니다");
                        }
                        GoNext(lv + 1);
                    })
                });
            }

            picks.Add(new Overlay.Choice { Label = "결과 공유", OnPick = () =>
            {
                ShareCard.Share(ShareCard.Build(_st));
                ShowWin();
            } });

            Overlay.Show(eyebrow, "출구를 <color=#F2A33C>찾았다</color>", body, stats, picks.ToArray());
        }

        void GoNext(int lv)
        {
            _busy = true;
            if (AdManager.I != null)
                AdManager.I.MaybeShowInterstitial(() => { _busy = false; LoadLevel(lv); });
            else { _busy = false; LoadLevel(lv); }
        }
    }
}
