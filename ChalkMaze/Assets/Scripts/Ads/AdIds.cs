namespace ChalkMaze
{
    /// 광고 단위 ID. 에디터·개발빌드에서는 무조건 테스트 ID가 쓰인다.
    ///
    /// 이렇게 나눠 두는 이유 : 개발 중 실제 광고를 한 번이라도 클릭하면
    /// AdMob 계정이 영구 정지되고 되돌릴 방법이 없다. 사람의 주의력에 맡기지 않고
    /// 빌드 종류로 강제한다.
    ///
    /// 이 값들은 앱 바이너리에 그대로 박혀 배포되는 공개 식별자다. 비밀이 아니다.
    public static class AdIds
    {
        // ── IJ컴퍼니 / 분필 미로 (실제) ──
        const string RealApp          = "ca-app-pub-1960290764423231~1952139201";
        const string RealRewarded     = "ca-app-pub-1960290764423231/8134404170";
        const string RealInterstitial = "ca-app-pub-1960290764423231/4967466153";

        // ── 구글 공개 테스트 ID ──
        const string TestRewarded     = "ca-app-pub-3940256099942544/5224354917";
        const string TestInterstitial = "ca-app-pub-3940256099942544/1033173712";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public const bool UsingTestAds = true;
        public const string Rewarded     = TestRewarded;
        public const string Interstitial = TestInterstitial;
#else
        public const bool UsingTestAds = false;
        public const string Rewarded     = RealRewarded;
        public const string Interstitial = RealInterstitial;
#endif

        /// 앱 ID 는 코드가 아니라 GoogleMobileAds 설정 에셋에 넣는다.
        /// Assets → Google Mobile Ads → Settings → Android App ID
        /// 확인용으로만 여기 둔다.
        public const string AndroidAppId = RealApp;

        /// 실기에서 실제 광고를 띄우되 클릭해도 안전하게 만들려면
        /// 기기를 테스트 기기로 등록한다. 기기 ID는 앱 첫 실행 시
        /// logcat 에 "Use RequestConfiguration.Builder.setTestDeviceIds(...)" 형태로 찍힌다.
        public static readonly string[] TestDeviceIds = { /* "여기에 기기 해시" */ };
    }
}
