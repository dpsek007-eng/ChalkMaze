using System.Text;
using UnityEngine;

namespace ChalkMaze
{
    /// 미로 자체는 절대 노출하지 않고 "결과의 모양"만 공유한다.
    /// 워들이 증명한 구조 — 스포일러가 없어서 마음 놓고 퍼진다.
    public static class ShareCard
    {
        public static string Build(RunState st)
        {
            var sb = new StringBuilder();
            if (st.IsDaily)
                sb.Append("🕯️ 분필미로 오늘의 미로 #").Append(PlayerProfile.TodayIndex).Append('\n');
            else
                sb.Append("🕯️ 분필미로 ").Append(st.Level).Append("층\n");
            sb.Append(st.Steps).Append("걸음 · ")
              .Append(st.Runs).Append("회차\n");

            for (int i = 0; i < st.Bonfires.Count; i++) sb.Append(st.Bonfires[i].Lit ? "🔥" : "▪️");
            sb.Append('\n');

            int used = st.Marks.Count;
            for (int i = 0; i < st.Cfg.Chalk; i++) sb.Append(i < used ? "⬜" : "⬛");
            sb.Append('\n');
            return sb.ToString();
        }

        /// 그림과 글을 함께 보낸다. 인스타·틱톡은 이미지가 없으면 사실상 퍼지지 않는다.
        /// 파일 공유가 어떤 이유로든 실패하면 글만이라도 보낸다.
        public static void ShareWithImage(string text, Texture2D image)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                string path = System.IO.Path.Combine(Application.temporaryCachePath, "chalkmaze-result.png");
                System.IO.File.WriteAllBytes(path, image.EncodeToPNG());

                using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                using var file = new AndroidJavaObject("java.io.File", path);

                // 안드로이드 7 부터 file:// 를 넘기면 죽는다. content:// 로 바꿔야 한다.
                string authority = Application.identifier + ".fileprovider";
                using var provider = new AndroidJavaClass("androidx.core.content.FileProvider");
                using var uri = provider.CallStatic<AndroidJavaObject>(
                    "getUriForFile", activity, authority, file);

                using var intentClass = new AndroidJavaClass("android.content.Intent");
                using var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intent.Call<AndroidJavaObject>("setType", "image/png");
                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uri);
                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);
                intent.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION"));

                using var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "결과 공유");
                activity.Call("startActivity", chooser);
                return;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Share] 이미지 공유 실패, 글만 보냅니다: " + e.Message);
            }
#else
            if (image != null)
            {
                string p = System.IO.Path.Combine(Application.temporaryCachePath, "chalkmaze-result.png");
                System.IO.File.WriteAllBytes(p, image.EncodeToPNG());
                Debug.Log("[Share] 이미지 저장: " + p);
            }
#endif
            Share(text);
        }

        /// 안드로이드 네이티브 공유 시트. 에디터에서는 로그로 대체.
        public static void Share(string text)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var intentClass = new AndroidJavaClass("android.content.Intent");
                using var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intent.Call<AndroidJavaObject>("setType", "text/plain");
                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);

                using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                using var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "결과 공유");
                activity.Call("startActivity", chooser);
            }
            catch (System.Exception e) { Debug.LogWarning("[Share] " + e.Message); }
#else
            Debug.Log("[Share]\n" + text);
            GUIUtility.systemCopyBuffer = text;
#endif
        }
    }
}
