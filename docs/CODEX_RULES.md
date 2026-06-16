# CODEX RULES

このプロジェクトはUnity 3Dのゴミ箱投げゲームです。

## Codexが触ってよい場所
- unity/
- blender/scripts/
- blender/source/
- blender/exports/
- docs/
- prompts/
- logs/

## Codexが触ってはいけない場所
- .git/
- Library/
- Temp/
- Obj/
- Build/
- Builds/
- UserSettings/
- 大量の自動生成ファイル

## Unity注意
- .metaファイルを削除しない
- 既存Prefab名を勝手に変えない
- 一度に大規模リファクタしない
- まずコンパイルが通ることを最優先する
- 見た目より遊びの核を優先する

## Blender注意
- Blender作業は可能な限りPythonスクリプトで行う
- Unityには基本的にFBXを書き出して取り込む
- .blendは元データ、.fbxはUnity取り込み用
- 低ポリ、軽量、MVP優先
- 細かい造形より、ゲーム中に読みやすい形を優先する

## Codex作業方針
- 1回の依頼で1タスクだけ実装する
- 変更前に現状を確認する
- 実装後に変更ファイル一覧を書く
- 実装後にUnityでの確認手順を書く
- 不明点は勝手に大きく決めず、最小実装にする
