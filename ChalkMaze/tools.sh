#!/usr/bin/env bash
# 분필 미로 — 에디터를 열지 않고 돌리는 검사/빌드
set -e
UNITY="${UNITY:-$HOME/Unity/Hub/Editor/6000.5.10f1/Editor/Unity}"
PROJ="$(cd "$(dirname "$0")" && pwd)"
OUT="${OUT:-/tmp/chalkmaze}"

case "${1:-help}" in
  compile)  "$UNITY" -batchmode -quit -nographics -projectPath "$PROJ" -logFile "$OUT/compile.log"
            grep -cE "error CS" "$OUT/compile.log" && echo "에러 있음" || echo "✅ 컴파일 통과" ;;
  test)     "$UNITY" -batchmode -nographics -projectPath "$PROJ" \
              -executeMethod ChalkMaze.EditorTools.SelfTest.Run -logFile "$OUT/test.log"
            grep -E "\[TEST" "$OUT/test.log" ;;
  setup)    "$UNITY" -batchmode -quit -nographics -projectPath "$PROJ" \
              -executeMethod ChalkMaze.EditorTools.ProjectSetup.Run -logFile "$OUT/setup.log" ;;
  urp)      "$UNITY" -batchmode -projectPath "$PROJ" \
              -executeMethod ChalkMaze.EditorTools.SetupURP.Run -logFile "$OUT/urp.log"
            grep -E "\[URP\]" "$OUT/urp.log" ;;
  build)    mkdir -p "$OUT/build"
            CM_BUILD_PATH="$OUT/build/ChalkMaze.x86_64" "$UNITY" -batchmode -nographics \
              -projectPath "$PROJ" -executeMethod ChalkMaze.EditorTools.BuildLinux.Run -logFile "$OUT/build.log"
            grep -E "\[BUILD\]" "$OUT/build.log" ;;
  run)      cd "$OUT/build" && ./ChalkMaze.x86_64 -screen-width 540 -screen-height 960 -screen-fullscreen 0 ;;

  # 고치고 바로 보기 : 워터마크 없는 빌드를 만들어 preview/ 로 옮기고 실행한다
  preview)  PREV="$PROJ/../preview"
            mkdir -p "$OUT/build"
            CM_RELEASE=1 CM_BUILD_PATH="$OUT/build/ChalkMaze.x86_64" "$UNITY" -batchmode -nographics \
              -projectPath "$PROJ" -executeMethod ChalkMaze.EditorTools.BuildLinux.Run -logFile "$OUT/preview.log"
            grep -E "\[BUILD\]" "$OUT/preview.log" || { echo "빌드 실패 — $OUT/preview.log 확인"; exit 1; }
            rm -rf "$PREV"; mkdir -p "$PREV"; cp -r "$OUT/build/"* "$PREV/"
            chmod +x "$PREV/ChalkMaze.x86_64"
            cat > "$PREV/미리보기.sh" <<'RUN'
#!/usr/bin/env bash
cd "$(dirname "$0")"
./ChalkMaze.x86_64 -screen-width 480 -screen-height 1040 -screen-fullscreen 0 "$@"
RUN
            chmod +x "$PREV/미리보기.sh"
            echo "준비됨 → $PREV/미리보기.sh"
            "$PREV/미리보기.sh" ;;
  *)        echo "사용법: ./tools.sh {compile|test|setup|urp|build|run|preview}" ;;
esac
