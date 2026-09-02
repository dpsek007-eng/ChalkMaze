using System.Collections;
using UnityEngine;

namespace ChalkMaze
{
    /// 공유 이미지를 실제로 뽑아 눈으로 확인하기 위한 개발 도구.
    /// 환경변수 CM_SHARE_DIR 이 있을 때만 붙는다.
    public sealed class DebugShareShot : MonoBehaviour
    {
        public GameController GC;

        IEnumerator Start()
        {
            string dir = System.Environment.GetEnvironmentVariable("CM_SHARE_DIR");
            if (string.IsNullOrEmpty(dir)) yield break;

            yield return new WaitForSeconds(1.2f);

            var st = GC.State;
            st.LoadDaily(PlayerProfile.TodayIndex);
            for (int i = 0; i < 40; i++) { st.TryMove(Random.Range(0, 4)); yield return null; }
            st.ToggleMark(MarkKind.Arrow);

            yield return new WaitForEndOfFrame();
            var tex = ShareImage.Build(st, PlayerProfile.TodayIndex, true);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "share-daily.png"), tex.EncodeToPNG());
            Debug.Log("[SHARE] daily 저장");

            st.LoadLevel(12, new System.Random(7));
            for (int i = 0; i < 25; i++) { st.TryMove(Random.Range(0, 4)); yield return null; }
            yield return new WaitForEndOfFrame();
            var t2 = ShareImage.Build(st, PlayerProfile.TodayIndex, false);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "share-level.png"), t2.EncodeToPNG());
            Debug.Log("[SHARE] level 저장");

            Application.Quit();
        }
    }
}
