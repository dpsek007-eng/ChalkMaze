# 분필 미로 (Chalk Maze)

어둠 속 미로를 **제한된 분필**로 정복하는 안드로이드 기억력 게임.
IJ컴퍼니 · Unity 6000.5 · URP 2D

지나온 길은 다시 어두워진다. 남는 것은 벽에 남긴 분필 자국과 밝혀 둔 화톳불뿐이다.
분필은 층마다 몇 자루뿐이라, 이 게임의 질문은 "얼마나 잘 외우느냐"가 아니라
**"어디를 외우고 어디에 분필을 쓸 것인가"** 다.

## 구조

```
ChalkMaze/     Unity 프로젝트 (자세한 내용은 ChalkMaze/README.md)
  Assets/Scripts/Model/   순수 C# — 규칙 전부. Unity 참조 없음
  Assets/Scripts/View/    렌더링. 임포트할 이미지 에셋이 없다
  Assets/Editor/          배치 도구 (설정·검사·빌드)
docs/          개인정보처리방침, 스토어 등록정보, 수익 설계
proto/         웹 프로토타입 (설계 검증용)
```

## 에디터 없이 돌리기

```bash
cd ChalkMaze
./tools.sh compile   # 컴파일
./tools.sh test      # 1000층 규칙 전수 검사
./tools.sh urp       # URP 자산 생성 + 셰이더 스트리핑 방지
./tools.sh build     # 리눅스 빌드
./tools.sh run       # 실행
```

## 서명키

`keystore/` 는 **의도적으로 저장소에서 제외**되어 있다 (`.gitignore`).
빌드하려면 로컬에 업로드 키스토어를 두고 환경변수로 비밀번호를 넘긴다.

```bash
CM_KEYSTORE_PASS='...' ./tools.sh build
```

키스토어를 잃으면 앱을 업데이트할 수 없다. 반드시 별도 백업할 것.
