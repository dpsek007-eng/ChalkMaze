using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ChalkMaze.EditorTools
{
    /// Built-in 렌더 파이프라인이 지원 중단되어 URP 2D 로 옮긴다.
    /// 2D 렌더러는 모바일에서 Built-in 보다 가볍고, 이 게임은 셰이더 의존이 거의 없어
    /// 프로젝트 초기인 지금이 전환 비용이 가장 싸다.
    public static class SetupURP
    {
        const string Dir       = "Assets/Settings";
        const string RendPath  = Dir + "/Renderer2D.asset";
        const string AssetPath = Dir + "/URP-2D.asset";

        [MenuItem("분필 미로/2. URP 적용 + 셰이더 보호")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets", "Settings");

            var rend = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendPath);
            if (rend == null)
            {
                rend = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(rend, RendPath);
            }

            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath);
            if (urp == null)
            {
                urp = UniversalRenderPipelineAsset.Create(rend);
                AssetDatabase.CreateAsset(urp, AssetPath);
            }

            GraphicsSettings.defaultRenderPipeline = urp;
            QualitySettings.renderPipeline = urp;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EnsureAlwaysIncluded();

            var active = GraphicsSettings.currentRenderPipeline;
            Debug.Log($"[URP] 적용 완료 — {(active != null ? active.name : "null")}");
            if (Application.isBatchMode) EditorApplication.Exit(active != null ? 0 : 1);
        }

        /// Shader.Find 로만 참조하는 셰이더는 어떤 에셋도 안 물고 있으면 빌드에서 제거된다.
        /// 안드로이드 릴리스 빌드에서 특히 공격적이라, 폰에서만 화면이 자홍색이 되거나
        /// 아무것도 안 보이는 사고가 난다. 항상 포함 목록에 넣어 막는다.
        static void EnsureAlwaysIncluded()
        {
            string[] want =
            {
                "Universal Render Pipeline/2D/Sprite-Unlit-Default",
                "Sprites/Default"
            };

            var gsAsset = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (gsAsset == null)
            {
                var all = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
                if (all != null && all.Length > 0) gsAsset = all[0];
            }
            if (gsAsset == null) { Debug.LogWarning("[URP] GraphicsSettings 을 열 수 없다"); return; }

            var so = new SerializedObject(gsAsset);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) { Debug.LogWarning("[URP] m_AlwaysIncludedShaders 없음"); return; }

            foreach (var name in want)
            {
                var sh = Shader.Find(name);
                if (sh == null) { Debug.Log($"[URP] 셰이더 없음(건너뜀): {name}"); continue; }

                bool already = false;
                for (int i = 0; i < arr.arraySize; i++)
                    if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) { already = true; break; }
                if (already) continue;

                arr.InsertArrayElementAtIndex(arr.arraySize);
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
                Debug.Log($"[URP] 항상 포함에 추가: {name}");
            }
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
    }
}
