using System;
using UnityEngine;

namespace ChalkMaze
{
    /// 광고 배치 지점. 각각이 "플레이어가 실제로 원하는 순간"에 걸려 있어야
    /// 리워드 시청률이 나온다. 억지로 끼워 넣은 광고는 이탈만 만든다.
    public enum AdSlot
    {
        RelightTorch,   // 연료 0 → 그 자리에서 계속. 가장 수요가 큰 순간.
        DoubleItem,     // 아이템 주움 → 하나 더
        FreeCompass,    // 길을 잃었을 때 → 출구 방향
        SupplyCache,    // 층 클리어 후 다음 층 보급품. 하루 상한이 있다.
        MoreChalk,      // 분필이 떨어졌을 때만. 필요한 순간에만 노출한다.
        FreeItem,       // 아이템이 하나도 없을 때만.
        LevelClear      // 층 클리어 전면광고 (N층마다)
    }

    /// Google Mobile Ads SDK를 임포트하기 전에도 컴파일되도록 감싼다.
    /// SDK 임포트 후 Player Settings > Scripting Define Symbols 에 CHALK_ADS 를 추가하면
    /// 실제 광고가 붙고, 없으면 즉시 성공하는 스텁으로 동작한다.
    public sealed class AdManager : MonoBehaviour
    {
        public static AdManager I { get; private set; }

        // 실제 ID / 테스트 ID 분기는 AdIds 가 빌드 종류로 강제한다.
        public bool AdsRemoved => PlayerProfile.AdsRemoved;

        int _clearsSinceInterstitial;
        const int InterstitialEvery = 3;   // 3층마다 1회. 이보다 잦으면 이탈이 급증한다.

        void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        bool _sdkOk;

        void Init()
        {
#if CHALK_ADS
          try
          {
            if (AdIds.TestDeviceIds.Length > 0)
            {
                GoogleMobileAds.Api.MobileAds.SetRequestConfiguration(
                    new GoogleMobileAds.Api.RequestConfiguration
                    {
                        TestDeviceIds = new System.Collections.Generic.List<string>(AdIds.TestDeviceIds)
                    });
            }

            GoogleMobileAds.Api.MobileAds.Initialize(_ =>
            {
                _sdkOk = true;
                Debug.Log($"[Ads] 초기화 완료 — {(AdIds.UsingTestAds ? "테스트" : "실제")} 광고");
                LoadRewarded();
                LoadInterstitial();
            });
          }
          catch (System.Exception e)
          {
              // 광고 SDK 가 게임을 죽여서는 안 된다. 리눅스처럼 클라이언트가 없는
              // 플랫폼에서는 초기화 자체가 실패한다.
              _sdkOk = false;
              Debug.LogWarning("[Ads] SDK 초기화 실패 — 광고 없이 진행합니다: " + e.Message);
          }
#else
            Debug.Log("[Ads] 스텁 모드 — SDK 미임포트. 리워드는 즉시 지급됩니다.");
#endif
        }

        /// 리워드 광고. onDone(true) 면 보상을 준다.
        public void ShowRewarded(AdSlot slot, Action<bool> onDone)
        {
#if CHALK_ADS
          try
          {
            if (_sdkOk && _rewarded != null && _rewarded.CanShowAd())
            {
                bool earned = false;
                _rewarded.OnAdFullScreenContentClosed += () => { onDone?.Invoke(earned); LoadRewarded(); };
                _rewarded.Show(_ => earned = true);
                return;
            }
            Debug.LogWarning("[Ads] 리워드 미준비 — 보상만 지급");
            if (_sdkOk) LoadRewarded();
            onDone?.Invoke(true);
          }
          catch (System.Exception e)
          {
              Debug.LogWarning("[Ads] 리워드 실패, 보상만 지급: " + e.Message);
              onDone?.Invoke(true);
          }
#else
            Debug.Log($"[Ads] (스텁) 리워드 {slot}");
            onDone?.Invoke(true);
#endif
        }

        /// 층 클리어 전면광고. 광고 제거를 구매했으면 건너뛴다.
        public void MaybeShowInterstitial(Action onDone)
        {
            if (AdsRemoved) { onDone?.Invoke(); return; }
            _clearsSinceInterstitial++;
            if (_clearsSinceInterstitial < InterstitialEvery) { onDone?.Invoke(); return; }
            _clearsSinceInterstitial = 0;

#if CHALK_ADS
          try
          {
            if (_sdkOk && _interstitial != null && _interstitial.CanShowAd())
            {
                _interstitial.OnAdFullScreenContentClosed += () => { onDone?.Invoke(); LoadInterstitial(); };
                _interstitial.Show();
                return;
            }
            if (_sdkOk) LoadInterstitial();
            onDone?.Invoke();
          }
          catch (System.Exception e)
          {
              Debug.LogWarning("[Ads] 전면광고 실패: " + e.Message);
              onDone?.Invoke();
          }
#else
            Debug.Log("[Ads] (스텁) 전면광고");
            onDone?.Invoke();
#endif
        }

#if CHALK_ADS
        GoogleMobileAds.Api.RewardedAd _rewarded;
        GoogleMobileAds.Api.InterstitialAd _interstitial;

        void LoadRewarded()
        {
            if (!_sdkOk) return;
            _rewarded?.Destroy(); _rewarded = null;
            var req = new GoogleMobileAds.Api.AdRequest();
            GoogleMobileAds.Api.RewardedAd.Load(AdIds.Rewarded, req, (ad, err) =>
            {
                if (err != null || ad == null) { Debug.LogWarning("[Ads] rewarded load fail"); return; }
                _rewarded = ad;
            });
        }

        void LoadInterstitial()
        {
            if (!_sdkOk) return;
            _interstitial?.Destroy(); _interstitial = null;
            var req = new GoogleMobileAds.Api.AdRequest();
            GoogleMobileAds.Api.InterstitialAd.Load(AdIds.Interstitial, req, (ad, err) =>
            {
                if (err != null || ad == null) return;
                _interstitial = ad;
            });
        }
#endif
    }
}
