# Captionize 專案深度分析報告：基於 Whisper.net 的高效能字幕生成工具

## 1. 專案標題與徽章

# Captionize

> **Captionize** 是一個高效能的桌面應用程式，專門用於透過 OpenAI 的 Whisper 語音識別模型，快速且精確地從影片或音訊檔案中提取並生成 SRT 格式的字幕。

| 狀態 | 資訊 |
| :--- | :--- |
| 程式語言 | [![Language](https://img.shields.io/badge/Language-C%23-blue.svg)](https://github.com/Nickyangtpe/Captionize) |
| 框架 | [![Framework](https://img.shields.io/badge/Framework-WinUI%203%20%26%20.NET%208-informational)](https://dotnet.microsoft.com/) |
| 版本 | [![Latest Release](https://img.shields.io/github/v/release/Nickyangtpe/Captionize?label=Version)](https://github.com/Nickyangtpe/Captionize/releases) |
| 授權 | [![License](https://img.shields.io/badge/License-Unspecified-lightgrey)](https://github.com/Nickyangtpe/Captionize) |
| Stars | [![GitHub stars](https://img.shields.io/github/stars/Nickyangtpe/Captionize.svg?style=flat)](https://github.com/Nickyangtpe/Captionize/stargazers) |
| Forks | [![GitHub forks](https://img.shields.io/github/forks/Nickyangtpe/Captionize.svg?style=flat)](https://github.com/Nickyangtpe/Captionize/network)

## 2. 專案簡介

Captionize 旨在為內容創作者提供一個快速、準確且完全離線的字幕生成解決方案。此專案基於 **WinUI 3** 框架開發，利用 **Whisper.net** 庫來實作語音轉文字（ASR）功能。它能夠處理各種影片和音訊格式，並自動透過 FFmpeg 進行音訊提取，最終輸出標準的 SRT 字幕檔案。

核心價值在於結合了強大的 AI 模型（Whisper）與 Windows 桌面應用的便利性，讓使用者能夠在本機環境中利用 GPU 加速來進行大規模的語音識別任務，無需依賴雲端服務，確保資料隱私和處理速度。

## 3. 核心功能特色

Captionize 提供了以下關鍵功能，以簡化字幕生成工作流程：

*   **高精度 AI 轉錄**: 採用先進的 **Whisper** 語音識別模型，提供業界領先的轉錄準確度。
*   **全格式支援**: 透過內建自動下載與配置的 **FFmpeg** 工具，支援從主流影片格式（如 MP4, MKV, MOV 等）中提取音訊。
*   **GPU 硬體加速**: 支援 **CUDA** 和 **Vulkan** 後端，使用者可以選擇利用 NVIDIA 或其他相容 GPU 進行加速，顯著縮短大型檔案的處理時間。
*   **多模型選擇與管理**: 支援 Whisper 模型的不同版本（如 tiny, base, small, medium 等），使用者可根據需求平衡速度與準確度。
*   **多語言識別**: 支援自動語言偵測或手動指定語言（如中文、英文等），提高轉錄品質。
*   **SRT 字幕輸出**: 直接生成標準的 `.srt` 格式字幕檔案，包含時間戳記和序列索引，即時可用於大多數影片播放器或編輯軟體。
*   **進度可視化**: 提供即時的轉錄進度條和日誌輸出，讓使用者清晰掌握任務狀態。

## 4. 技術架構與核心依賴

Captionize 是一個基於 Windows 平台的桌面應用程式，其技術核心圍繞著高性能的語音識別庫和現代的 Windows UI 框架。

### 技術棧

| 類別 | 技術/工具 | 說明 |
| :--- | :--- | :--- |
| **前端/UI** | WinUI 3, XAML | 現代 Windows 應用程式介面。 |
| **後端/邏輯** | C# (.NET 8) | 應用程式主要邏輯和非同步任務處理。 |
| **語音識別** | Whisper.net | C#/.NET 平台上的 Whisper 模型介面。 |
| **音訊處理** | FFmpeg, NAudio | FFmpeg 負責音訊提取；NAudio 可能用於音訊格式處理或時長獲取。 |
| **GPU 後端** | CUDA, Vulkan | 通過 `Whisper.net.Runtime.Cuda.Windows` 實現硬體加速。

### 核心技術亮點

1.  **動態 FFmpeg 管理**: 應用程式具備自我修復能力。如果本地缺乏 FFmpeg，它將自動從 GitHub 資源庫下載並解壓縮，確保音訊提取功能始終可用。
2.  **Whisper Runtime 選擇**: 程式碼中包含了明確的運行時選擇邏輯（判斷是否使用 CUDA/Vulkan），允許使用者根據硬體條件優化性能。
3.  **非同步與取消**: 使用 `CancellationTokenSource` 管理長時間運行的轉錄任務，確保使用者可以隨時安全地取消操作。

## 5. 系統需求

### 軟體要求

*   **作業系統**: Windows 10 (版本 17763.0 或更高版本) 或 Windows 11。
*   **運行時**: 需要支援 .NET 8 運行環境（通常在安裝 MSIX 包時會自動處理）。

### 硬體建議

為了獲得最佳的轉錄速度，特別是使用大型 Whisper 模型時：

| 元件 | 建議 | 備註 |
| :--- | :--- | :--- |
| **CPU** | 多核處理器 (i5/Ryzen 5 或更高) | 基礎轉錄使用。 |
| **記憶體 (RAM)** | 至少 8 GB | 處理大型模型時需要更多記憶體。 |
| **GPU** | NVIDIA 顯示卡 (支援 CUDA 11+) | 使用 CUDA 後端時性能提升最為顯著。 |

## 6. 快速開始

Captionize 應用程式主要透過 MSIX 安裝包發布，這簡化了安裝和部署過程。

### 安裝步驟

1.  **下載發布包**: 前往 [Release 頁面](https://github.com/Nickyangtpe/Captionize/releases) 下載最新的安裝檔案。
2.  **安裝應用程式**: 下載以下任一檔案進行安裝：
    *   `Captionize.Installer.exe` (推薦，包含依賴項檢查與安裝)。
    *   `Captionize_X.X.X.X_x64.msix` (Windows 應用程式包)。
    > 注意：由於這是未發布到 Microsoft Store 的應用程式，您可能需要先安裝隨附的 `.cer` 證書才能安裝 `.msix` 檔案。
3.  **首次運行設定**: 首次運行時，應用程式會檢查並在本地資料夾 (`%LOCALAPPDATA%\Captionize\models`) 中準備 AI 模型和 FFmpeg 執行檔。

### 運行與模型準備

1.  **選擇模型**: 在應用程式介面中選擇所需的 Whisper 模型（例如 `base` 或 `small`）。
2.  **下載模型**: 如果模型尚未下載，應用程式將提示您下載，模型檔案將存儲在本地，實現離線使用。

## 7. 詳細操作指南

### 基本工作流程

1.  **載入檔案**: 點擊「選擇影片/音訊檔案」按鈕，選擇您的媒體檔案。 (`_selectedVideoFile`)
2.  **配置參數**:
    *   **選擇模型**: 選擇您希望使用的 Whisper 模型大小。
    *   **選擇語言**: 選擇媒體檔案的語言，或保留「自動偵測」。
    *   **運行時後端**: 在運行時優先級列表中選擇 **CPU**、**CUDA** 或 **Vulkan** 以決定加速方式。
    *   **溫度 (Temperature)**: 調整模型的採樣溫度（通常保持預設值即可）。
3.  **開始轉錄**: 點擊「開始」按鈕。
    *   **FFmpeg 檢查**: 應用程式會首先檢查並下載 FFmpeg (如果需要)。
    *   **音訊提取**: FFmpeg 將音訊提取為 WAV 格式 (`ExtractAudioFromVideoUwp`)。
    *   **AI 分析**: Whisper 處理音訊流並生成字幕片段 (`StartAudioTranscriptionUwp`)。
4.  **結果輸出**: 轉錄完成後，完整的 SRT 格式字幕將顯示在文字區域中，您可以將其複製或儲存為 `.srt` 檔案。

### SRT 字幕結構

字幕結果由 `GenerateSRT()` 方法生成，遵循標準 SRT 格式：

```srt
1
00:00:00,000 --> 00:00:05,000
這是第一段字幕內容。

2
00:00:05,500 --> 00:00:10,123
這是第二段字幕內容。
```

## 8. 專案結構剖析

專案結構清晰，主要圍繞 WinUI 應用程式的標準佈局，核心邏輯集中在 `MainWindow.xaml.cs`。

```
Captionize/
├── .gitignore             # 忽略 Visual Studio, .NET, 和編譯產生的檔案 (包含 FFmpeg 相關 dll)
├── Captionize.sln         # Visual Studio 解決方案檔案
├── README.md              # 專案說明文件
└── Captionize/            # 應用程式專案目錄
    ├── Assets/            # 應用程式資源，如圖示和啟動畫面
    ├── Properties/
    │   └── launchSettings.json # 應用程式啟動配置
    ├── App.xaml.cs          # 應用程式入口點和生命週期管理
    ├── Captionize.csproj    # C# 專案檔案，定義依賴項和建構設定
    └── MainWindow.xaml.cs   # **核心檔案**: 包含 UI 邏輯、FFmpeg/Whisper 整合、轉錄流程控制
```

### 關鍵檔案說明

*   **`Captionize/MainWindow.xaml.cs`**: 
    *   負責所有使用者互動和後台處理。
    *   包含 `CheckFFmpegExistsAsync`, `DownloadFFmpeg`, `ExtractAudioFromVideoUwp` 等方法，處理媒體預處理。
    *   包含 Whisper 模型的載入、運行時配置 (`WhisperFactory.FromPath` with `UseGpu` option)，以及 `SubtitleSegment` 的處理和 SRT 字幕生成邏輯。
*   **`Captionize/Captionize.csproj`**: 
    *   明確定義了對 **Whisper.net 1.8.1** 及其所有運行時後端的依賴 (包括 CUDA, Vulkan, NoAvx)。
    *   定義了 WinUI 3 框架和目標 SDK 版本。
    *   包含多個 CUDA 相關 DLL 檔案的引用，這些 DLL 確保了 GPU 後端能夠正確運行。

## 9. 版本發布紀錄

以下是 Captionize 專案的近期發布紀錄，提供給使用者下載和追蹤更新。

| 版本標籤 | 發布日期 | 變更說明 | 下載連結 (x64 MSIX) |
| :--- | :--- | :--- | :--- |
| **1.0.1.0** | 2025-10-04 | 穩定性更新，無詳細說明。 | [MSIX 下載](https://github.com/Nickyangtpe/Captionize/releases/download/1.0.1.0/Captionize_1.0.1.0_x64.msix) |
| **1.0.0.0** | 2025-10-01 | 初次發布，核心功能實作。 | [MSIX 下載](https://github.com/Nickyangtpe/Captionize/releases/download/1.0.0.0/Captionize_1.0.0.0_x64.msix) |

## 10. 常見問題 (FAQ)

### Q1: 為什麼我的轉錄速度很慢？

**A:** 轉錄速度主要取決於您選擇的模型大小和運行時後端：

1.  **模型大小**: 如果您使用的是 `medium` 或 `large` 模型，處理速度自然較慢，但準確度最高。建議在速度要求較高時使用 `base` 或 `small` 模型。
2.  **運行時**: 確保您已在應用程式中選擇了 **CUDA** 或 **Vulkan** 後端，並且您的電腦具有相容的 GPU 和驅動程式。如果選擇 CPU 運行，即使是小型模型也會耗費較長時間。

### Q2: 應用程式說 FFmpeg 未準備就緒，我需要做什麼？

**A:** 應用程式在啟動轉錄流程時會自動嘗試下載 FFmpeg。如果下載失敗或被網路阻止，請檢查您的網路連線和防火牆設定。

> FFmpeg 預設從 `https://github.com/BtbN/FFmpeg-Builds/` 下載，並存儲在應用程式的本地資料夾中。

### Q3: 轉錄任務可以取消嗎？

**A:** 是的。專案代碼中使用了 `CancellationTokenSource` 來管理非同步任務。當使用者點擊取消按鈕時，任務將安全地終止，並在日誌中顯示「處理已由使用者取消」。

## 11. 授權與版權

本專案目前**未指定**明確的開源授權（License）。所有權利保留給專案擁有者。

**版權聲明**

© 2025 Nickyangtpe

## 12. 致謝與聯絡

本專案得以實現，主要歸功於以下開源技術和資源：

*   **Whisper.net**: 提供了強大的 C# 語言綁定，實現了高效的 Whisper 模型運行。
*   **FFmpeg**: 用於音訊和影片處理的核心工具。
*   **WinUI 3 / .NET**: 提供了現代 Windows 應用程式的開發環境。

**專案擁有者/聯絡方式**

*   **作者**: Nickyangtpe
*   **GitHub**: [https://github.com/Nickyangtpe](https://github.com/Nickyangtpe)
*   **專案頁面**: [https://github.com/Nickyangtpe/Captionize](https://github.com/Nickyangtpe/Captionize)
