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
  *)        echo "사용법: ./tools.sh {compile|test|setup|urp|build|run}" ;;
esac
