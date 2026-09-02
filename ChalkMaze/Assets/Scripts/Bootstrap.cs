using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChalkMaze
{
    /// 씬 전체를 코드로 세운다.
    /// 에디터에서 할 일 : 빈 씬에 빈 GameObject 하나 만들고 이 스크립트만 붙이면 끝.
    public sealed class Bootstrap : MonoBehaviour
    {
        void Awake()
        {
            Debug.Log("[Bootstrap] 시작 — 이 줄이 안 보이면 Main.unity 씬이 아니거나 Bootstrap 이 안 붙어 있다");
            Application.targetFrameRate = 60;
            // 창이 포커스를 잃어도 루프를 계속 돌린다.
            // 이게 꺼져 있으면 백그라운드에서 Update 가 멈춰 코루틴이 영원히 대기한다.
            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;

            // ── 카메라 ──
            // AudioListener 를 빠뜨리면 소리가 재생돼도 아무것도 들리지 않는다.
            // 예외도 나지 않아서 로그로는 절대 드러나지 않는다.
            var camGo = new GameObject("MainCamera",
                typeof(Camera), typeof(CameraRig), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.backgroundColor = Palette.Void;
            cam.clearFlags = CameraClearFlags.SolidColor;
            var rig = camGo.GetComponent<CameraRig>();

            // ── 월드 ──
            var meshGo = new GameObject("MazeMesh", typeof(MeshFilter), typeof(MeshRenderer), typeof(MazeMesh));
            var mesh = meshGo.GetComponent<MazeMesh>();

            var glyphGo = new GameObject("Glyphs", typeof(GlyphLayer));
            var glyphs = glyphGo.GetComponent<GlyphLayer>();

            var torchGo = new GameObject("Torch", typeof(Torch));
            var torch = torchGo.GetComponent<Torch>();

            // ── UI ──
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            var hud = canvasGo.AddComponent<Hud>();
            hud.Build(canvasGo.transform);

            var overlay = canvasGo.AddComponent<Overlay>();
            overlay.Build(canvasGo.transform);

            // ── 소리 ──
            if (Sfx.I == null) new GameObject("Sfx", typeof(Sfx));

            // ── 광고 ──
            if (AdManager.I == null)
                new GameObject("AdManager", typeof(AdManager));

            // ── 컨트롤러 ──
            var gcGo = new GameObject("GameController", typeof(GameController), typeof(TouchSteer));
            var gc = gcGo.GetComponent<GameController>();
            gc.Mesh = mesh; gc.Glyphs = glyphs; gc.Rig = rig;
            gc.TorchFx = torch; gc.Hud = hud; gc.Overlay = overlay;

            var steer = gcGo.GetComponent<TouchSteer>();
            steer.Cam = cam;
            steer.Player = glyphs.PlayerT;
            steer.OnMove += d => gc.TryMove(d);

            var ind = canvasGo.AddComponent<SteerIndicator>();
            ind.Steer = steer;
            ind.Build(canvasGo.transform, ProcTex.Ring(), ProcTex.ArrowMark());

            // 개발용 자동 촬영 — 환경변수 CM_SHOT_DIR 이 있을 때만.
            // 실행 인자는 플레이어까지 안 넘어오는 경우가 있어 환경변수를 쓴다.
            var shotDir = System.Environment.GetEnvironmentVariable("CM_SHOT_DIR");
            Debug.Log($"[Bootstrap] CM_SHOT_DIR='{shotDir}' args=[{string.Join(" ", System.Environment.GetCommandLineArgs())}]");
            if (!string.IsNullOrEmpty(shotDir))
            {
                var shot = gcGo.AddComponent<DebugAutoShot>();
                shot.GC = gc; shot.Ov = overlay;
                Debug.Log("[Bootstrap] 자동 촬영 활성");
            }

            Debug.Log($"[Bootstrap] 구성 완료 — 폰트 '{(UIKit.Font != null ? UIKit.Font.name : "없음")}' · "
                    + $"파이프라인 '{(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null ? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name : "Built-in")}'");
        }
    }
}
