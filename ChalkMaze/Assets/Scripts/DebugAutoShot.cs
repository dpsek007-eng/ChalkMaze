using System.Collections;
using UnityEngine;

namespace ChalkMaze
{
    /// 환경변수 CM_SHOT_DIR 이 있을 때만 붙는다.
    /// 창을 조작할 수 없는 환경에서 실제 화면을 확인하기 위한 개발 도구.
    /// 출시 빌드 동작에는 영향이 없다.
    public sealed class DebugAutoShot : MonoBehaviour
    {
        public GameController GC;
        public Overlay Ov;
        string _dir;

        void Start()
        {
            _dir = System.Environment.GetEnvironmentVariable("CM_SHOT_DIR");
            if (string.IsNullOrEmpty(_dir)) _dir = "/tmp";
            Debug.Log($"[SHOT] Start 진입 · dir={_dir}");
            StartCoroutine(Sequence());
        }

        void Shot(string name)
        {
            // CaptureScreenshot 은 프레임 끝에 알아서 찍는다. WaitForEndOfFrame 이 필요 없다.
            string path = System.IO.Path.Combine(_dir, name);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[SHOT] 요청 {path}");
        }

        IEnumerator Sequence()
        {
            Debug.Log("[SHOT] 시퀀스 시작");
            yield return new WaitForSeconds(1.5f);

            Shot("01-intro.png");
            yield return new WaitForSeconds(1.5f);

            Debug.Log("[SHOT] 인트로 닫기");
            if (Ov != null) Ov.ClickPrimary();
            yield return new WaitForSeconds(1.0f);

            Shot("02-start.png");
            yield return new WaitForSeconds(1.5f);

            Debug.Log("[SHOT] 이동 시작");
            int[] plan = { 2,2,1,1,2,3,2,2,1,0,1,1,2,2,3,3,2,1,1,2 };
            foreach (int d in plan)
            {
                if (GC != null) GC.TryMove(d);
                yield return new WaitForSeconds(0.1f);
            }

            Shot("03-walked.png");
            yield return new WaitForSeconds(1.5f);

            if (GC != null) GC.DoMark(MarkKind.Arrow);
            yield return new WaitForSeconds(0.4f);
            Shot("04-mark.png");
            yield return new WaitForSeconds(1.5f);

            Debug.Log("[SHOT] 완료 — 종료");
            Application.Quit();
        }
    }
}
