using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ChalkMaze.EditorTools
{
    /// 실기 빌드 전에 렌더링·UI·입력이 실제로 도는지 확인하기 위한 리눅스 빌드.
    public static class BuildLinux
    {
        [MenuItem("분필 미로/4. 리눅스 빌드")]
        public static void Run()
        {
            string outPath = System.Environment.GetEnvironmentVariable("CM_BUILD_PATH");
            if (string.IsNullOrEmpty(outPath)) outPath = "/tmp/ChalkMaze/ChalkMaze.x86_64";

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = outPath,
                target = BuildTarget.StandaloneLinux64,
                // CM_RELEASE=1 이면 개발 빌드 워터마크가 없는 빌드를 만든다.
                // 스토어 스크린샷에는 워터마크가 들어가면 안 된다.
                options = System.Environment.GetEnvironmentVariable("CM_RELEASE") == "1"
                          ? BuildOptions.None
                          : BuildOptions.Development
            };

            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log($"[BUILD] {s.result} · {s.totalSize / 1024 / 1024}MB · 에러 {s.totalErrors} · 경고 {s.totalWarnings}");
            if (Application.isBatchMode)
                EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
