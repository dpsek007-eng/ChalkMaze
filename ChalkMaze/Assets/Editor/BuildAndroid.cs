using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ChalkMaze.EditorTools
{
    /// Play 에 올릴 AAB 를 만든다. 서명 비밀번호는 환경변수 CM_KEYSTORE_PASS 로만 받는다.
    public static class BuildAndroid
    {
        [MenuItem("분필 미로/6. AAB 빌드 (Play 업로드용)")]
        public static void Run()
        {
            string outPath = Environment.GetEnvironmentVariable("CM_AAB_PATH");
            if (string.IsNullOrEmpty(outPath)) outPath = "/tmp/chalkmaze/ChalkMaze.aab";
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));

            string pass = Environment.GetEnvironmentVariable("CM_KEYSTORE_PASS");
            string ks = Path.GetFullPath("../keystore/chalkmaze-upload.keystore");

            if (!File.Exists(ks) || string.IsNullOrEmpty(pass))
            {
                Debug.LogError($"[AAB] 서명 불가 — keystore={File.Exists(ks)}, 비밀번호={(string.IsNullOrEmpty(pass) ? "없음" : "있음")}");
                if (Application.isBatchMode) EditorApplication.Exit(2);
                return;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = ks;
            PlayerSettings.Android.keystorePass = pass;
            PlayerSettings.Android.keyaliasName = "chalkmaze";
            PlayerSettings.Android.keyaliasPass = pass;

            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Debugging;

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = outPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None          // 릴리스 — 실제 광고 ID 가 들어간다
            };

            Debug.Log($"[AAB] 시작 — {PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)} " +
                      $"v{PlayerSettings.bundleVersion}({PlayerSettings.Android.bundleVersionCode})");

            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log($"[AAB] {s.result} · {s.totalSize / 1024 / 1024}MB · 에러 {s.totalErrors} · 경고 {s.totalWarnings} · {outPath}");

            if (Application.isBatchMode)
                EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
