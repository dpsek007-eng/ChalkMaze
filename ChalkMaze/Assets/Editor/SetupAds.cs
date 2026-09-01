using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ChalkMaze.EditorTools
{
    /// AdMob App ID 를 설정 에셋에 넣고 CHALK_ADS 를 켠다.
    /// App ID 는 코드가 아니라 이 에셋에 있어야 AndroidManifest 에 반영된다.
    public static class SetupAds
    {
        const string Define = "CHALK_ADS";

        [MenuItem("분필 미로/8. 광고 SDK 설정")]
        public static void Run()
        {
            // ── App ID ──
            var type = System.Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, Assembly-CSharp-Editor")
                    ?? System.Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor");

            if (type == null)
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings");
                    if (type != null) break;
                }
            }

            if (type == null) Debug.LogError("[Ads] GoogleMobileAdsSettings 타입을 찾지 못했다");
            else
            {
                // 싱글턴 프로퍼티/메서드 이름이 버전마다 달라 이름으로 훑는다
                // LoadInstance 는 internal static 이다. NonPublic 을 포함해야 잡힌다.
                const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                object settings = null;
                var m = type.GetMethod("LoadInstance", Any);
                if (m != null) settings = m.Invoke(null, null);
                if (settings == null)
                {
                    var prop = type.GetProperty("Instance", Any);
                    if (prop != null) settings = prop.GetValue(null);
                }

                if (settings == null) Debug.LogError("[Ads] 설정 인스턴스를 얻지 못했다");
                else
                {
                    SetMember(settings, "GoogleMobileAdsAndroidAppId", AdIds.AndroidAppId);
                    // 프로퍼티가 막히면 직렬화 필드에 직접 쓴다
                    SetMember(settings, "adMobAndroidAppId", AdIds.AndroidAppId);
                    EditorUtility.SetDirty((Object)settings);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[Ads] App ID 설정: {AdIds.AndroidAppId}");
                }
            }

            // ── CHALK_ADS 정의 ──
            var target = NamedBuildTarget.Android;
            var cur = PlayerSettings.GetScriptingDefineSymbols(target);
            if (!cur.Split(';').Contains(Define))
            {
                PlayerSettings.SetScriptingDefineSymbols(target,
                    string.IsNullOrEmpty(cur) ? Define : cur + ";" + Define);
                Debug.Log($"[Ads] {Define} 추가 (Android)");
            }
            else Debug.Log($"[Ads] {Define} 이미 있음");

            // 리눅스 빌드로도 검증하므로 스탠드얼론에도 넣는다
            var sa = NamedBuildTarget.Standalone;
            var cur2 = PlayerSettings.GetScriptingDefineSymbols(sa);
            if (!cur2.Split(';').Contains(Define))
                PlayerSettings.SetScriptingDefineSymbols(sa,
                    string.IsNullOrEmpty(cur2) ? Define : cur2 + ";" + Define);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Ads] 완료 — Android defines: {PlayerSettings.GetScriptingDefineSymbols(target)}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void SetMember(object obj, string name, string value)
        {
            var t = obj.GetType();
            var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.CanWrite) { p.SetValue(obj, value); return; }
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) { f.SetValue(obj, value); return; }

        }
    }
}
