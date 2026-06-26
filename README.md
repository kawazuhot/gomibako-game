# 世界清浄機 WebGL 公開手順

このリポジトリでは、友達へのテストプレイ共有用に Unity WebGL ビルド成果物を `docs/` から GitHub Pages で公開します。

Unityプロジェクト全体を公開対象にするのではなく、公開に必要な `index.html`、`Build/`、`TemplateData/` だけを `docs/` に配置します。

## WebGLビルド成果物の配置

UnityでWebGLビルドしたら、ビルド結果を以下の構成で配置します。

```text
docs/
  index.html
  Build/
  TemplateData/
  .nojekyll
```

`docs/.nojekyll` は GitHub Pages で Unity WebGL のファイルをそのまま配信するために必要です。

## GitHub Pagesで公開する手順

1. UnityでWebGLビルドする
2. ビルド成果物の `index.html`、`Build/`、`TemplateData/` を `docs/` に配置する
3. 変更をGitにコミットしてGitHubへpushする
4. GitHubのリポジトリ画面で `Settings > Pages` を開く
5. `Source` を `Deploy from a branch` にする
6. `Branch` を `main` にする
7. `Folder` を `/docs` にする
8. `Save` を押す
9. 数分後に表示されるGitHub Pages URLを友達に共有する

公開URLは通常、以下の形式になります。

```text
https://<GitHubユーザー名>.github.io/<リポジトリ名>/
```

## Unity WebGLビルド時の注意

初回テストでは、UnityのWebGL Publishing Settingsで `Compression Format` を `Disabled` にするとトラブルが少ないです。

圧縮を使う場合は、GitHub Pagesで読み込みに失敗する可能性があるため、`Decompression Fallback` の有効化も確認してください。

## ローカル確認

`docs/` をローカルサーバーで配信すると、GitHub Pagesへpushする前にWebGL表示を確認できます。

```sh
python3 -m http.server 8000 -d docs
```

ブラウザで以下を開きます。

```text
http://localhost:8000/
```
