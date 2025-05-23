# VideoFrameExtractor

## 概要

**VideoFrameExtractor** は、C#で開発されたコマンドラインツールで、動画ファイルからフレームを抽出するために使用します。このツールは多くの一般的な動画フォーマットをサポートし、`ffmpeg`を利用してフレーム抽出を行います。動画ファイルの一括処理が必要なユーザーに最適です。

## 機能

- 多様な動画フォーマットをサポート：`mp4`, `avi`, `mkv`, `mov`, `flv`, `wmv`, `webm`
- 単一の動画ファイルまたはディレクトリ内の全動画ファイルを処理可能
- 出力ディレクトリの自動作成と抽出フレームの保存
- クロスプラットフォーム対応（WindowsとLinux）
- 内蔵`ffmpeg`管理による使用手順の簡素化

## インストール

### 必要条件

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download) 以上
- `ffmpeg`実行ファイル（プロジェクトに内蔵済み）

### ダウンロード

リポジトリをローカルにクローンします：

```bash
git clone https://github.com/starfield17/VideoFrameExtractor.git
```

### ビルド

プロジェクトディレクトリに移動し、プロジェクトをビルドします：

```bash
cd VideoFrameExtractor
dotnet build -c Release
```

ビルド完了後、実行ファイルは `bin/Release/net6.0` ディレクトリに配置されます。

## 使用方法

### コマンドライン使用法

```bash
VideoFrameExtractor <動画ファイルまたはディレクトリのパス>
```

### パラメータ説明

- `<動画ファイルまたはディレクトリのパス>`：単一の動画ファイルまたは動画ファイルを含むディレクトリを指定します。

### 使用例

単一の動画ファイルを処理：

```bash
VideoFrameExtractor /path/to/video/sample.mp4
```

動画ディレクトリ全体を処理：

```bash
VideoFrameExtractor /path/to/videos/
```

## サポートフォーマット

- **mp4**
- **avi**
- **mkv**
- **mov**
- **flv**
- **wmv**
- **webm**

## ffmpeg

このツールには、異なるオペレーティングシステム用の`ffmpeg`実行ファイルが内蔵されています。実行環境に応じて、適切なバージョンの`ffmpeg`が自動的に選択されます：

- **Windows**：`ffmpeg/windows/ffmpeg.exe`
- **Linux**：`ffmpeg/linux/ffmpeg`

`ffmpeg`が存在しないか実行できない場合、ツールはエラーメッセージを表示します。

## エラー処理

- **パスが存在しない**：指定されたパスが存在しない場合、プログラムはエラーメッセージを出力して終了します。
- **サポートされていないファイル形式**：ファイルがサポートされている動画フォーマットでない場合、エラーが表示されます。
- **ffmpeg処理エラー**：`ffmpeg`が動画処理中にエラーを起こした場合、エラーの詳細が出力されます。
- **権限の問題（Linux）**：ツールは`ffmpeg`に実行権限を設定しようとします。失敗した場合、警告メッセージが表示されます。
