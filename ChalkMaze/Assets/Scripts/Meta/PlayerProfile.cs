using System;
using UnityEngine;

namespace ChalkMaze
{
    /// 다시 켤 이유를 만드는 층. 게임 자체보다 리텐션에 더 큰 영향을 준다.
    public static class PlayerProfile
    {
        const string KBest       = "cm.bestLevel";
        const string KLastDay    = "cm.lastDay";
        const string KStreak     = "cm.streak";
        const string KAdsRemoved = "cm.adsRemoved";
        const string KLanternLv  = "cm.lanternLv";
        const string KChalkBonus = "cm.chalkBonus";
        const string KDailyDone  = "cm.dailyDone";
        const string KDailySteps = "cm.dailySteps";

        public static int  BestLevel  { get => PlayerPrefs.GetInt(KBest, 0);       set { PlayerPrefs.SetInt(KBest, value); PlayerPrefs.Save(); } }
        public static int  Streak     { get => PlayerPrefs.GetInt(KStreak, 0);     private set { PlayerPrefs.SetInt(KStreak, value); PlayerPrefs.Save(); } }
        public static bool AdsRemoved { get => PlayerPrefs.GetInt(KAdsRemoved,0)==1; set { PlayerPrefs.SetInt(KAdsRemoved, value?1:0); PlayerPrefs.Save(); } }

        /// 영구 업그레이드 — IAP 및 출석 보상의 대상
        public static int LanternLevel { get => PlayerPrefs.GetInt(KLanternLv, 0);  set { PlayerPrefs.SetInt(KLanternLv, value); PlayerPrefs.Save(); } }
        public static int ChalkBonus   { get => PlayerPrefs.GetInt(KChalkBonus, 0); set { PlayerPrefs.SetInt(KChalkBonus, value); PlayerPrefs.Save(); } }

        public static int ItemCount(ItemKind k) => PlayerPrefs.GetInt("cm.item." + k, 0);
        public static void AddItem(ItemKind k, int n)
        {
            PlayerPrefs.SetInt("cm.item." + k, Mathf.Max(0, ItemCount(k) + n));
            PlayerPrefs.Save();
        }

        /// UTC 기준 일련 번호. 전 세계가 같은 날 같은 미로를 푼다.
        public static int TodayIndex =>
            (int)(DateTime.UtcNow.Date - new DateTime(2026, 1, 1)).TotalDays;

        /// 오늘의 미로 시드 — 날짜에서 결정되므로 서버가 필요 없다.
        public static int DailySeed => unchecked(TodayIndex * 2654435761u).GetHashCode();

        public static bool DailyDoneToday => PlayerPrefs.GetInt(KDailyDone, -1) == TodayIndex;
        public static int  DailySteps     => PlayerPrefs.GetInt(KDailySteps, 0);

        public static void RecordDaily(int steps)
        {
            PlayerPrefs.SetInt(KDailyDone, TodayIndex);
            PlayerPrefs.SetInt(KDailySteps, steps);
            PlayerPrefs.Save();
        }

        // ── 광고 보상 상한 ────────────────────────────
        // 상한이 없으면 난이도가 '실력'이 아니라 '광고를 몇 편 참느냐'로 결정된다.
        // 그 순간 자원 배분이라는 이 게임의 핵심이 무너진다.
        public const int DailyAdGrantCap = 5;
        const string KGrantDay = "cm.grantDay";
        const string KGrantCnt = "cm.grantCnt";

        public static int AdGrantsUsedToday =>
            PlayerPrefs.GetInt(KGrantDay, -1) == TodayIndex ? PlayerPrefs.GetInt(KGrantCnt, 0) : 0;

        public static int AdGrantsLeft => Mathf.Max(0, DailyAdGrantCap - AdGrantsUsedToday);

        public static void ConsumeAdGrant()
        {
            int used = AdGrantsUsedToday;
            PlayerPrefs.SetInt(KGrantDay, TodayIndex);
            PlayerPrefs.SetInt(KGrantCnt, used + 1);
            PlayerPrefs.Save();
        }

        /// 앱을 켤 때 한 번 호출. 연속 출석을 갱신하고 끊겼는지 알려준다.
        public static int TouchStreak()
        {
            int today = TodayIndex;
            int last = PlayerPrefs.GetInt(KLastDay, -999);
            if (last == today) return Streak;

            Streak = (last == today - 1) ? Streak + 1 : 1;
            PlayerPrefs.SetInt(KLastDay, today);
            PlayerPrefs.Save();
            return Streak;
        }

        /// 출석 보상 — 3일마다 삽, 7일에 실
        public static string ClaimStreakReward()
        {
            int s = Streak;
            if (s > 0 && s % 7 == 0) { AddItem(ItemKind.Thread, 1); return "7일 연속 · 아리아드네의 실 +1"; }
            if (s > 0 && s % 3 == 0) { AddItem(ItemKind.Shovel, 1); return "3일 연속 · 삽 +1"; }
            AddItem(ItemKind.Oil, 1);
            return "출석 · 기름병 +1";
        }
    }
}
