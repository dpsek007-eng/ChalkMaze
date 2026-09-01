using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ChalkMaze.EditorTools
{
    /// 안드로이드 출시 설정. Android Build Support 모듈이 설치돼 있어야 빌드가 되지만
    /// 설정 자체는 모듈 없이도 기록된다.
    public static class SetupAndroid
    {
        [MenuItem("분필 미로/5. 안드로이드 출시 설정")]
        public static void Run()
        {
            // ── 아이콘 ──
            var icon  = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icons/icon.png");
            var fore  = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icons/adaptive-foreground.png");
            var back  = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icons/adaptive-background.png");

            if (icon != null) ApplyIcons(icon, fore, back);
            else Debug.LogWarning("[Android] Assets/Icons/icon.png 을 찾지 못했다");

            // ── 아키텍처 / 백엔드 ──
            // Play 는 64비트를 요구한다. ARM64 는 IL2CPP 에서만 된다.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;

            // Play 는 APK 가 아니라 AAB(App Bundle) 를 받는다.
            EditorUserBuildSettings.buildAppBundle = true;

            // ── 서명 ──
            // 비밀번호를 스크립트에 박지 않는다. 저장소에 올라가면 그대로 유출된다.
            string ksPath = Path.GetFullPath("../keystore/chalkmaze-upload.keystore");
            string pass = System.Environment.GetEnvironmentVariable("CM_KEYSTORE_PASS");

            if (File.Exists(ksPath) && !string.IsNullOrEmpty(pass))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = ksPath;
                PlayerSettings.Android.keystorePass = pass;
                PlayerSettings.Android.keyaliasName = "chalkmaze";
                PlayerSettings.Android.keyaliasPass = pass;
                Debug.Log($"[Android] 서명키 연결: {ksPath}");
            }
            else
            {
                Debug.LogWarning($"[Android] 서명 미설정 — keystore={File.Exists(ksPath)}, CM_KEYSTORE_PASS={(string.IsNullOrEmpty(pass) ? "없음" : "있음")}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Android] 완료 — {PlayerSettings.productName} / {PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)} / AAB={EditorUserBuildSettings.buildAppBundle}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// 아이콘 종류(Legacy/Round/Adaptive)는 유니티 버전마다 열거형이 다르다.
        /// 이름으로 판별해서 버전에 묶이지 않게 한다.
        static void ApplyIcons(Texture2D icon, Texture2D fore, Texture2D back)
        {
            var t = NamedBuildTarget.Android;

            try
            {
                var kinds = PlayerSettings.GetSupportedIconKinds(t);
                if (kinds != null && kinds.Length > 0)
                {
                    foreach (var kind in kinds)
                    {
                        var slots = PlayerSettings.GetPlatformIcons(t, kind);
                        if (slots == null || slots.Length == 0) continue;

                        bool adaptive = kind.ToString().ToLower().Contains("adaptive");
                        foreach (var slot in slots)
                        {
                            if (adaptive && fore != null && back != null && slot.maxLayerCount >= 2)
                            {
                                slot.SetTexture(back, 0);   // 0 = 뒷면
                                slot.SetTexture(fore, 1);   // 1 = 앞면
                            }
                            else slot.SetTexture(icon, 0);
                        }
                        PlayerSettings.SetPlatformIcons(t, kind, slots);
                        Debug.Log($"[Android] 아이콘 적용: {kind} ({slots.Length}개 슬롯, adaptive={adaptive})");
                    }
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Android] 플랫폼 아이콘 API 사용 불가 — 기본 방식으로: " + e.Message);
            }

            // Android 모듈이 없으면 위 API 가 비어 있다. 기본 아이콘만이라도 넣는다.
            var sizes = PlayerSettings.GetIconSizes(t, IconKind.Application);
            var arr = new Texture2D[sizes.Length];
            for (int i = 0; i < arr.Length; i++) arr[i] = icon;
            PlayerSettings.SetIcons(t, arr, IconKind.Application);
            Debug.Log($"[Android] 기본 아이콘 적용 ({arr.Length}개)");
        }
    }
}
