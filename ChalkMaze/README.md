# 분필 미로 (Chalk Maze) — Unity 프로젝트

## 검증 상태 (2026-09-01)

Unity 6000.5.10f1 배치 모드로 실행한 결과다.

| 검사 | 결과 |
|---|---|
| 스크립트 컴파일 | ✅ 에러 0 · 경고 0 |
| 게임 규칙 자체 테스트 | ✅ 1,004개 층 전수 통과 |
| 리눅스 빌드 (URP) | ✅ 167MB · 에러 0 · 경고 0 |
| 실행 (14초) | ✅ 런타임 예외 0건 |
| 셰이더 해석 | ✅ `Universal Render Pipeline/2D/Sprite-Unlit-Default` |
| 한글 렌더링 | ✅ NanumGothic 번들 (내장 폰트엔 한글 글리프가 없다) |
| 화면 육안 확인 | ✅ 스크린샷 4장으로 확인 |

### 화면 확인 방법 (창을 조작할 수 없을 때)

환경변수 `CM_SHOT_DIR` 를 주고 실행하면 `DebugAutoShot` 이 붙어
인트로 → 시작 → 이동 → 표식 순서로 스크린샷을 찍고 스스로 종료한다.

```bash
CM_SHOT_DIR=/tmp/shots ./ChalkMaze.x86_64 -screen-width 540 -screen-height 960
```

**`Application.runInBackground = true` 가 반드시 켜져 있어야 한다.**
꺼져 있으면 창이 포커스를 잃는 순간 Update 가 멈춰 코루틴이 영원히 대기한다.
자동 촬영이 멈춘다면 이것부터 확인할 것.

## 렌더 파이프라인 — URP 2D

Built-in 파이프라인이 Unity 6 에서 지원 중단되어 **URP 17.5.0 / 2D 렌더러**로 전환했다.
셰이더 의존이 `MazeMesh` 한 곳뿐이라 프로젝트 초기인 지금이 전환 비용이 가장 쌌다.

- `Assets/Settings/URP-2D.asset` + `Renderer2D.asset` — 코드로 생성, GraphicsSettings 에 지정됨
- `MazeMesh.FindSpriteShader()` 는 URP → Built-in → Unlit 순으로 후보를 훑어 파이프라인에 무관하게 동작

**셰이더 스트리핑 주의.** `Shader.Find` 로만 참조하는 셰이더는 어떤 에셋도 물고 있지 않으면
빌드에서 제거된다. 안드로이드 릴리스 빌드에서 특히 공격적이라 폰에서만 화면이 자홍색이 되거나
아무것도 안 보이는 사고가 난다. `SetupURP.Run()` 이 해당 셰이더를
**Graphics Settings → Always Included Shaders** 에 등록해 이를 막는다.
셰이더를 추가로 쓰게 되면 그 목록에도 같이 넣을 것.

**아직 아무도 화면을 눈으로 보지 않았다.** 크래시 없이 도는 것과 제대로 보이는 것은 다르다.

### 에디터 없이 돌리는 명령

```bash
./tools.sh compile   # 컴파일만
./tools.sh test      # 1000층 규칙 검사
./tools.sh setup     # 씬 + Player 설정 재적용
./tools.sh urp       # URP 자산 생성/지정 + 셰이더 스트리핑 방지
./tools.sh build     # 리눅스 빌드
./tools.sh run       # 빌드 실행
```

## 열기

1. Unity Hub → **Add → Add project from disk** → 이 폴더(`ChalkMaze`) 선택
2. Unity **6 LTS** 로 열기 (`ProjectSettings/ProjectVersion.txt` 기준)
3. **씬은 이미 만들어져 있다** — `Assets/Scenes/Main.unity` (Bootstrap 부착 완료, 빌드 설정 등록 완료)
4. Play

카메라·캔버스·HUD·메시·텍스처를 전부 코드가 만듭니다. **에디터에서 조립할 것이 없습니다.**

## 회사 / 앱 식별 정보

| 항목 | 값 |
|---|---|
| 회사명 (Company Name) | `IJ Company` |
| 제품명 (Product Name) | `분필 미로` |
| 번들 ID (Package Name) | `com.ijcompany.chalkmaze` |

`Edit → Project Settings → Player` 에서 위 셋을 설정한다.

> ⚠️ **번들 ID는 Play 스토어에 한 번 출시하면 영구히 바꿀 수 없다.**
> 바꾸려면 새 앱으로 등록해야 하고 기존 설치·리뷰·순위가 전부 날아간다. 지금 확정할 것.

## 확인 필요한 설정

- **Edit → Project Settings → Player → Active Input Handling** 이 `Input Manager (Old)` 또는 `Both` 인지 확인.
  `Input System Package (New)` 단독이면 키보드 입력과 버튼이 동작하지 않습니다.
- **Player → Resolution → Default Orientation** 을 `Portrait` 로.

## 구조

```
Model/     순수 C# — Unity 참조 없음. 규칙 전부가 여기 있고 단위 테스트가 가능하다.
  Maze.cs         미로 생성(재귀 백트래커 + 순환로), BFS 거리, 복도 시야
  LevelConfig.cs  층별 파라미터 — 아이템 4층, 구덩이 6층부터
  Items.cs        기름/삽/판자/실/나침반
  RunState.cs     한 층의 진행 상태와 모든 규칙

View/      렌더링. 임포트할 이미지 에셋이 하나도 없다.
  ProcTex.cs      스프라이트를 런타임에 그림
  MazeMesh.cs     칸당 20정점 고정 스트라이드, 안개는 정점 알파로만 갱신
  GlyphLayer.cs   개체 스프라이트 풀
  Torch.cs        어둠 = 가운데 뚫린 거대 스프라이트. 라이팅 세팅 불필요
  CameraRig.cs    추적 + 흔들림

UI/        런타임 uGUI (TMP 미사용 — 에센셜 임포트 단계를 없애기 위함)
Ads/       AdMob 래퍼. SDK 없이도 컴파일됨
Meta/      영구 진행 · 연속 출석 · 일일 미로 시드 · 공유 카드
```

## 1000층 설계

미로는 절차적으로 무한 생성되므로 "층을 만드는" 작업은 없다.
깊이를 만드는 것은 **변형 규칙(Mods)의 조합**이다. 크기만 키우면 9층과 500층이 똑같아진다.

| 구역 | 층 | 성격 |
|---|---|---|
| 채석장 | 1–20 | 규칙 학습 |
| 수몰층 | 21–99 | 변형 1개 |
| 뼈의 회랑 | 100–299 | 변형 1–2개 |
| 재의 심도 | 300–599 | 변형 2개 |
| 무광층 | 600–999 | 변형 2–3개 |
| 최심부 | 1000 | 도달 목표 |
| 심연 | 1001+ | 무한 |

변형 규칙과 최초 등장 층:

| 규칙 | 층 | 내용 |
|---|---|---|
| 잠긴 문 | 12 | 열쇠를 찾아 되돌아와야 한다 |
| 역행 | 25 | 가장 깊은 곳에서 시작해 입구로 |
| 짧은 횃불 | 40 | 연료 66% |
| 외딴 화톳불 | 70 | 화톳불 1개 |
| 지워지는 분필 | 120 | 자국이 40걸음 뒤 사라짐 |
| 암흑 | 200 | 시야 반경 1칸 |
| 분필 없음 | 350 | 순수 기억력 |

**설계 제약 두 가지** — `LevelConfig.For()` 가 보장한다.

1. 정보를 빼앗는 규칙(암흑·분필없음·지워지는분필)은 **한 층에 하나까지**.
   둘 이상 겹치면 실력이 개입할 여지가 사라져 도박이 된다.
2. 아이템·구덩이 수에 상한(5/6)이 있다. 상한이 없으면 1000층에서 441칸 미로에 구덩이 499개가 배치된다.

층 조건은 **층 번호만으로 결정**된다(`Hash(level, salt)`). 같은 층은 누가 언제 해도 같은 조건이라
"300층 해봤어?" 같은 대화가 성립한다. 미로 형태 자체는 매번 새로 생성된다.

1~1000층 전수 시뮬레이션 결과: 서로 다른 조건 **255종**, 병리적 조합 0건.

## 광고 붙이기

광고 ID는 이미 `Assets/Scripts/Ads/AdIds.cs` 에 들어가 있다 (IJ컴퍼니 / 분필 미로).

| | 값 |
|---|---|
| 앱 ID | `ca-app-pub-1960290764423231~1952139201` |
| 보상형 | `ca-app-pub-1960290764423231/8134404170` |
| 전면 | `ca-app-pub-1960290764423231/4967466153` |

**에디터와 개발빌드에서는 자동으로 테스트 ID가 쓰인다.** 실제 ID는 릴리스 빌드에서만 나간다.
개발 중 실제 광고를 한 번이라도 클릭하면 AdMob 계정이 영구 정지되므로,
사람의 주의력에 맡기지 않고 빌드 종류로 강제했다.

### 설치 순서

1. [googleads-mobile-unity releases](https://github.com/googleads/googleads-mobile-unity/releases) 에서 최신 `.unitypackage` 다운로드
2. Unity → `Assets → Import Package → Custom Package` → 전부 임포트
3. `Assets → Google Mobile Ads → Settings` 열기
   → **Android App ID** 칸에 `ca-app-pub-1960290764423231~1952139201` 입력
   (이 값은 코드가 아니라 여기 들어가야 AndroidManifest 에 반영된다)
4. `Edit → Project Settings → Player → Scripting Define Symbols` 에 `CHALK_ADS` 추가
5. 빌드

`CHALK_ADS` 가 없으면 광고 코드는 전부 스텁으로 동작한다. SDK 없이도 게임 전체 흐름을 테스트할 수 있다.

### SDK 버전 차이 주의

`AdManager.Init()` 의 `MobileAds.SetRequestConfiguration(...)` 호출부는 플러그인 버전에 따라
API 형태가 다르다 (구버전은 `RequestConfiguration.Builder` 패턴). 컴파일 에러가 나면 그 블록만
설치한 SDK 문서에 맞춰 고치면 된다. 테스트 기기 등록은 선택 사항이라 통째로 지워도 동작한다.

### 실기에서 실제 광고를 안전하게 보려면

앱을 실기에서 처음 실행하면 logcat 에 테스트 기기 해시가 찍힌다.
그 값을 `AdIds.TestDeviceIds` 에 넣으면 실제 광고 단위를 쓰면서도 클릭이 집계되지 않는다.

```
adb logcat | grep -i "test device"
```

### 수익이 언제부터 발생하나

AdMob 앱 상태가 **검토 필요**인 동안에는 실제 광고가 게재되지 않는다.
Play 스토어 출시 → 스토어 등록정보 연결 → 검토 통과 이후부터 수익이 잡힌다.
