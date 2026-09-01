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
            sb.Append("🕯️ 분필미로 #").Append(PlayerProfile.TodayIndex).Append('\n');
            sb.Append(st.Steps).Append("걸음 · ")
              .Append(st.Runs).Append("회차\n");

            for (int i = 0; i < st.Bonfires.Count; i++) sb.Append(st.Bonfires[i].Lit ? "🔥" : "▪️");
            sb.Append('\n');

            int used = st.Marks.Count;
            for (int i = 0; i < st.Cfg.Chalk; i++) sb.Append(i < used ? "⬜" : "⬛");
            sb.Append('\n');
            return sb.ToString();
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
