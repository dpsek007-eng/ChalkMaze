using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChalkMaze.EditorTools
{
    /// 에디터에서 손으로 할 조립을 전부 대신한다. 배치 모드에서 호출된다.
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("분필 미로/1. 프로젝트 설정 적용")]
        public static void Run()
        {
            // ── Player 설정 ──
            PlayerSettings.companyName = "IJ Company";
            PlayerSettings.productName = "분필 미로";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.ijcompany.chalkmaze");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            // 26 이 현재 허용되는 최저값이다. 더 올리면 기기 도달률만 줄어든다.
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;

            // 레거시 Input 을 쓰므로 구/신 입력을 모두 켠다 (0=구, 1=신, 2=둘 다)
            try { PlayerSettings.SetPropertyInt("ActiveInputHandler", 2, BuildTargetGroup.Standalone); }
            catch (System.Exception e) { Debug.LogWarning("[Setup] ActiveInputHandler: " + e.Message); }

            // ── 씬 ──
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Bootstrap");
            go.AddComponent<Bootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] 완료 — 씬/Player 설정 적용됨");
        }
    }
}
