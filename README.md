# point up

プレゼンテーション中にデスクトップ画面へフリーハンドの注釈線を描くポインティングツール。  
描いた線は一定時間後に自動でフェードアウトして消える。

## 機能

- **フリーハンド描画** — 左クリックドラッグで線を描く。離した瞬間からカウントダウン開始
- **自動フェードアウト** — 設定した時間が経過すると徐々に消える
- **全モニタ対応** — 仮想デスクトップ全体をカバー（マルチモニタ環境も可）
- **キーボード透過** — オーバーレイはフォーカスを奪わず、スライド操作など全てのキーボード入力は前面アプリへ素通し
- **速度連動太さ** — マウスをゆっくり動かすと太く、速く動かすと細くなるオプション
- **トレイ常駐** — タスクバーに表示されず、システムトレイから操作

## ショートカット

| 操作 | キー |
|---|---|
| 描画 ON / OFF | `Ctrl + Shift + D` |
| 全消去 | `Ctrl + Shift + C` |

> **左手だけで操作可能**なキーアサインです。右手はマウスに専念できます。  
> `settings.json` で任意のキーに変更できます。

## インストール

### ダウンロード（推奨）

[Releases](../../releases) から最新の `PointUp.exe` をダウンロードして実行。  
インストール不要、単一ファイルで動作します。

### ビルド

```powershell
git clone <this-repo>
cd pointup
dotnet publish PointUp.Wpf/PointUp.Wpf.csproj -c Release -o publish/
```

**要件:** .NET 10 SDK / Windows 10 以降

## 使い方

1. `PointUp.exe` を起動するとシステムトレイにアイコンが表示される
2. `Ctrl + Shift + D` で描画モードを ON にする（トレイに「描画中」と表示）
3. デスクトップ上で左クリックドラッグして線を描く
4. 線は設定した時間が経過すると自動で消える
5. `Ctrl + Shift + C` で全消去
6. `Ctrl + Shift + D` で描画モードを OFF に戻す（マウス操作が通常通りになる）
7. トレイアイコン右クリック →「終了」で完全終了

## 設定

初回起動時に `%APPDATA%\PointUp\settings.json` が自動生成される。  
トレイメニュー「設定ファイルを開く」で直接編集し、「設定再読込」で即時反映。

```jsonc
{
  "LineColor": "#FFFF3B30",        // 線の色（ARGB 16進数）
  "Thickness": 4.0,                // 線の太さ（px）
  "LifetimeMs": 1500,              // フェード開始までの時間（ms）
  "FadeMs": 500,                   // フェードアウトの所要時間（ms）
  "VelocityThicknessEnabled": false, // 速度連動太さ ON/OFF
  "MinThickness": 1.5,             // 速度連動: 最小太さ（最速時）
  "MaxThickness": 9.0,             // 速度連動: 最大太さ（最遅時）
  "VelocityAtMinThickness": 3.0,   // この速度(px/ms)以上で最細
  "VelocityAtMaxThickness": 0.2,   // この速度(px/ms)以下で最太
  "SmoothingFactor": 0.3,          // 太さ変化の平滑度（0=変化なし、1=即時）
  "StartEnabled": false,           // 起動時に描画モードを ON にするか
  "ToggleShortcut": {
    "Ctrl": true, "Shift": true, "Alt": false, "Win": false, "Key": "D"
  },
  "ClearShortcut": {
    "Ctrl": true, "Shift": true, "Alt": false, "Win": false, "Key": "C"
  }
}
```

## アーキテクチャ

クリーンアーキテクチャ 4 層構成。依存方向: `Wpf → Application → Core ← Infrastructure`

```
PointUp.Core/           # ドメインモデル・インターフェース（WPF非依存）
  Models/               #   AppSettings, ShortcutDefinition
  Interfaces/           #   ISettingsService, IGlobalHotkeyService

PointUp.Application/    # 純粋ロジック・ユースケース（テスト対象）
  UseCases/             #   CalculateVelocityThicknessUseCase
                        #   StrokeLifetimeUseCase

PointUp.Infrastructure/ # 永続化（WPF非依存）
  Services/             #   JsonSettingsService

PointUp.Wpf/            # エントリポイント・UI
  App.xaml.cs           #   DI配線（Microsoft.Extensions.Hosting）
  Views/                #   OverlayWindow（全画面透明オーバーレイ）
  ViewModels/           #   OverlayViewModel（CommunityToolkit.Mvvm）
  Services/             #   GlobalHotkeyService（RegisterHotKey）
                        #   TrayIconService（WinForms NotifyIcon）

PointUp.Tests/          # xUnit ユニットテスト
```

## 技術的なポイント

- **クリック透過:** `WS_EX_TRANSPARENT | WS_EX_LAYERED` で OFF 時はマウス完全素通し。ON 時は解除して描画を受け取る
- **キーボード透過:** `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW` でフォーカスを奪わず、ショートカット 2 組以外のキーは全て前面アプリへ届く
- **透明ウィンドウのヒットテスト:** `AllowsTransparency=True` の WPF レイヤードウィンドウは alpha=0 ピクセルが常時素通しになるため、ウィンドウ背景は `#01000000`（不透明度 0.4%）に設定

## ライセンス

MIT
