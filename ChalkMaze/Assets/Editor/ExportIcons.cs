using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChalkMaze.EditorTools
{
    /// 아이콘이 실제로 알아볼 만한지 눈으로 확인하기 위한 도구.
    public static class ExportIcons
    {
        [MenuItem("분필 미로/9. 아이콘 미리보기 내보내기")]
        public static void Run()
        {
            string dir = System.Environment.GetEnvironmentVariable("CM_ICON_DIR");
            if (string.IsNullOrEmpty(dir)) dir = "/tmp/cm-icons";
            Directory.CreateDirectory(dir);

            void Save(string name, Sprite sp)
            {
                var src = sp.texture;
                // 어두운 바탕 위에 흰 아이콘을 얹어 실제 보이는 대로 저장
                var outT = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
                for (int y = 0; y < src.height; y++)
                for (int x = 0; x < src.width; x++)
                {
                    var p = src.GetPixel(x, y);
                    var bg = new Color(0.043f, 0.039f, 0.047f, 1f);
                    var fg = new Color(0.91f, 0.89f, 0.84f, 1f);
                    outT.SetPixel(x, y, Color.Lerp(bg, fg, p.a));
                }
                outT.Apply();
                File.WriteAllBytes(Path.Combine(dir, name + ".png"), outT.EncodeToPNG());
            }

            foreach (var k in ItemInfo.All) Save(k.ToString(), ProcTex.ItemIcon(k));
            Save("Gear", ProcTex.GearIcon());
            Save("Question", ProcTex.QuestionIcon());
            Save("Arrow", ProcTex.ArrowMark());
            Save("Cross", ProcTex.CrossMark());
            Save("Crest", ProcTex.Crest());

            Debug.Log($"[ICON] 내보냄 → {dir}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
