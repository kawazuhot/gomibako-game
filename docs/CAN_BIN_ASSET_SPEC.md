# 缶用ゴミ箱アセット仕様書

## 目的

Unity製3D物理ゲームで使用する、缶用ゴミ箱のローポリ3Dモデルを制作する。

プレイヤーは飲み終わった空き缶を投げ、缶用ゴミ箱の丸い投入口へ入れる。
ゲームの気持ちよさは「狙って、投げて、穴を中央通過して成功する」ことにある。

## 採用デザイン

採用案は「C案：Game Deformed Type」。

特徴：

- ゲーム向けデフォルメ
- 青い本体
- オフホワイトの上部フタ
- 大きく見やすい黒い丸穴
- 白い太めの縁
- 正面に「あきかん」ラベル
- 下部に白ライン
- 自販機横に置いて違和感がない
- 低ポリでも見やすい

## 参考画像

参考画像は `docs/reference/can_bin_game_deformed_turnaround.png` とする。

ただし、画像は参考資料であり、最終仕様はこのmdを優先する。

## モデル制作方針

- Blenderで制作する
- ローポリ
- Unity取り込み用
- FBX出力前提
- 高品質化よりも、ゲーム中の読みやすさを優先
- 細かい傷、汚れ、複雑な装飾は不要
- 穴は見た目として黒い円形パーツで表現してよい
- 成功判定はUnity側で別管理するため、Blenderモデルに物理判定は作らない

## サイズ基準

ゲーム内の缶直径を 1.0 とする。

- 缶直径：1.0
- 見た目の穴直径：缶直径の 1.15 倍
- 穴の縁厚み：缶直径の 0.08 倍
- 成功判定半径：Unity側で管理する
- 成功判定の初期値：缶直径の 0.25 倍程度

## 外形

目安：

- 高さ：約 2.2
- 幅：約 1.4
- 奥行き：約 1.2

形状：

- 下部は安定した箱型
- 本体は少し上に向かってすぼまる
- 上部はオフホワイトのフタ
- 角は少し丸みがあるように見せる
- 完全なリアル形状ではなく、ゲーム向けに分かりやすくする

## 正面

正面には以下を配置する。

- 大きな丸い投入口
- 黒い穴
- 白い太めの縁
- 青い本体
- 缶アイコン風のラベル
- 「あきかん」表記
- 下部の白ライン

投入口は、プレイヤーが狙う場所なので最も目立たせる。

## 投入口

投入口の仕様：

- 位置：正面上部
- 形：円形
- 中央に黒い円
- 外側に白いリング
- 見た目の穴直径：缶直径の 1.15 倍
- リング厚み：缶直径の 0.08 倍

重要：

- 実際に穴を完全にくり抜かなくてもよい
- ただし、ゲーム画面上では穴に見えること
- 投入口中心に HoleCenter というEmptyを配置する

## HoleCenter

Blenderモデル内にEmptyを作る。

名前：
`HoleCenter`

位置：

- 投入口の中心
- 黒い円の中心
- Unity側で成功判定Triggerや中心通過判定を合わせるための目印

注意：

- HoleCenterは見た目用ではない
- Unity側で成功判定の基準点として使う
- FBX出力時にEmptyがUnityへ取り込まれるようにする

## 色指定

推奨カラー：

- 本体ブルー：`#2F6FB3` 付近
- フタ・リング：オフホワイト `#F2F1EA` 付近
- 穴：黒 `#050505`
- ライン：白または薄いグレー
- ラベル：青地に白アイコン

色数は少なめにする。

## ポリゴン方針

- ローポリ
- シルエット重視
- 小さすぎるディテールは作らない
- 丸穴は16〜24分割程度でよい
- 本体の角丸は簡易表現でよい
- 高ポリ化しない

## Blender上の構造

推奨構造：

```text
CanBin_GameDeformed
- Body
- TopCover
- HoleBlack
- HoleRim
- FrontLabel
- BottomLines
- SideHandle
- HoleCenter
```

親オブジェクト：
`CanBin_GameDeformed`

親オブジェクトの原点：

- 底面中央

正面方向：

- Unityに入れた時、ゴミ箱の正面がプレイヤー側を向くようにする

## Unity側で実装するもの

Blenderでは以下を作らない。

- 成功判定Trigger
- 本体Collider
- RimCollider
- Rigidbody
- ゲームロジック

これらはUnity側でPrefab化する時に追加する。

Unity側のPrefab構成：

```text
CanBinPrefab
- Visual
  - Blender製FBX
- BodyColliders
  - 本体のBoxCollider群
- RimColliders
  - 穴の縁用Collider群
- HoleCenter
  - Blender由来またはUnity側で配置
- SuccessDetector
  - 缶中心通過判定用スクリプト
```

## 当たり判定方針

ゴミ箱本体の物理判定はUnity側で実装する。

方針：

- Mesh Colliderには基本的に頼らない
- BoxColliderなどのプリミティブColliderで本体を近似する
- 穴の部分は塞がない
- 穴の縁には、缶が当たって弾かれるようにColliderを置く
- 成功判定はCollider接触ではなく、缶の中心がHoleCenter付近を通過したかで判定する

## 出力

Blenderスクリプト：
`blender/scripts/create_can_bin_game_deformed.py`

保存先：
`blender/source/can_bin_game_deformed.blend`

FBX出力先：
`blender/exports/can_bin_game_deformed.fbx`

プレビュー画像：
`blender/exports/can_bin_game_deformed_preview.png`

## まだやらないこと

- 風の実装
- 3ステージ化
- 残機UI
- ランク判定
- 高品質テクスチャ
- 複雑な汚れ表現
- Mesh Colliderによる成功判定
