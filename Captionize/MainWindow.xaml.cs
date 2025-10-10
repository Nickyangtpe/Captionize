using Captionize.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.LibraryLoader;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace Captionize
{
    public class SubtitleSegment
    {
        public int Index { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public string Text { get; set; }
    }

    public sealed partial class MainWindow : Window
    {
        private readonly HttpClient _http = new HttpClient();
        private CancellationTokenSource? _cts;
        private const string ModelsFolder = "models";
        private const string FFmpegFolder = "ffmpeg";

        private WhisperProcessor? _processor;
        private readonly List<ModelInfo> _models = WhisperModels.All;
        private StorageFile? _selectedVideoFile;
        private StorageFile? _ffmpegFile;
        private bool _ffmpegReady = false;
        private readonly List<SubtitleSegment> _subtitleSegments = new List<SubtitleSegment>();

        public MainWindow()
        {
            this.InitializeComponent();
            _ = InitializeAppAsync();
        }

        private async Task InitializeAppAsync()
        {
            await ApplicationData.Current.LocalFolder.CreateFolderAsync(ModelsFolder, CreationCollisionOption.OpenIfExists);
            await CheckFFmpegExistsAsync();
            InitUI();
        }

        private void InitUI()
        {
            foreach (var m in _models)
            {
                ModelComboBox.Items.Add(m.DisplayName + $"({m.Size})");
            }
            if (ModelComboBox.Items.Count > 0) ModelComboBox.SelectedIndex = 0;
            Log("應用程式已啟動，準備就緒。");
        }

        private async Task CheckFFmpegExistsAsync()
        {
            try
            {
                var ffmpegDir = await ApplicationData.Current.LocalFolder.CreateFolderAsync(FFmpegFolder, CreationCollisionOption.OpenIfExists);
                _ffmpegFile = await ffmpegDir.TryGetItemAsync("ffmpeg.exe") as StorageFile;
                if (_ffmpegFile != null)
                {
                    _ffmpegReady = true;
                    Log("✓ FFmpeg 已就緒");
                }
                else
                {
                    Log("⚠ FFmpeg 未安裝，開始處理時將自動下載。");
                }
            }
            catch (Exception ex)
            {
                Log($"✗ 檢查 FFmpeg 失敗: {ex.Message}");
            }
        }

        private async Task EnsureFFmpegReady()
        {
            if (_ffmpegReady) return;
            try
            {
                var ffmpegDir = await ApplicationData.Current.LocalFolder.CreateFolderAsync(FFmpegFolder, CreationCollisionOption.OpenIfExists);
                var ffmpegFile = await ffmpegDir.TryGetItemAsync("ffmpeg.exe") as StorageFile;
                if (ffmpegFile != null)
                {
                    _ffmpegFile = ffmpegFile;
                    _ffmpegReady = true;
                    return;
                }

                DispatcherQueue.TryEnqueue(() =>
                {
                    FFmpegDownloadPanel.Visibility = Visibility.Visible;
                    FFmpegDownloadProgress.Value = 0;
                    FFmpegDownloadStatus.Text = "準備下載...";
                });

                await DownloadFFmpeg(ffmpegDir);
                _ffmpegFile = await ffmpegDir.GetFileAsync("ffmpeg.exe");
                _ffmpegReady = true;

                DispatcherQueue.TryEnqueue(() => FFmpegDownloadPanel.Visibility = Visibility.Collapsed);
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(() => FFmpegDownloadPanel.Visibility = Visibility.Collapsed);
                Log($"✗ FFmpeg 準備失敗: {ex.Message}");
                throw;
            }
        }

        private async Task DownloadFFmpeg(StorageFolder targetFolder)
        {
            const string ffmpegUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
            Log("正在下載 FFmpeg...");
            var tempZipFile = await targetFolder.CreateFileAsync("ffmpeg.zip", CreationCollisionOption.ReplaceExisting);
            using var response = await _http.GetAsync(ffmpegUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await DownloadWithProgressAsync(response, tempZipFile, FFmpegDownloadProgress, FFmpegDownloadStatus);
            Log("✓ FFmpeg 下載完成，正在解壓縮...");
            await ExtractFFmpegFromZip(tempZipFile, targetFolder);
            await tempZipFile.DeleteAsync();
            Log("✓ FFmpeg 安裝完成");
        }

        private async Task ExtractFFmpegFromZip(StorageFile zipFile, StorageFolder targetFolder)
        {
            using var zipStream = await zipFile.OpenStreamForReadAsync();
            using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);
            var ffmpegEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("bin/ffmpeg.exe", StringComparison.OrdinalIgnoreCase));
            if (ffmpegEntry != null)
            {
                var destFile = await targetFolder.CreateFileAsync("ffmpeg.exe", CreationCollisionOption.ReplaceExisting);
                using var entryStream = ffmpegEntry.Open();
                using var fileStream = await destFile.OpenStreamForWriteAsync();
                await entryStream.CopyToAsync(fileStream);
                Log("✓ FFmpeg 解壓縮完成");
            }
            else
            {
                throw new FileNotFoundException("在下載的壓縮檔中找不到 'bin/ffmpeg.exe'。");
            }
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVideoFile == null) { Log("✗ 請先選擇影片或音訊檔案。"); return; }
            if (ModelComboBox.SelectedIndex < 0) { Log("✗ 請先選擇 AI 模型。"); return; }

            var model = _models[ModelComboBox.SelectedIndex];
            var modelsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(ModelsFolder);
            var modelFile = await modelsFolder.TryGetItemAsync(model.Name) as StorageFile;
            if (modelFile == null) { Log($"✗ 模型檔案 '{model.Name}' 不存在，請先下載。"); return; }

            SetBusyState(true);
            _subtitleSegments.Clear();
            TxtTranscript.Text = "";

            try
            {
                await EnsureFFmpegReady();

                var runtimeStr = (RuntimePriorityList.SelectedItem as ComboBoxItem)?.Content.ToString();
                bool UseGPU = false;

                switch (runtimeStr)
                {
                    case "CUDA":
                        UseGPU = true;
                        break;
                    case "Vulkan":
                        UseGPU = true;
                        break;
                    default:
                        UseGPU = false;
                        break;
                }
                Log($"運行後端: {UseGPU}");

                var factory = WhisperFactory.FromPath(modelFile.Path, new WhisperFactoryOptions { UseGpu = UseGPU });

                var language = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "auto";

                var builder = factory.CreateBuilder()
                    .WithLanguage(language)
                    .WithTemperature((float)SliderTemp.Value);

                if (ToggleTranslate.IsOn)
                    builder.WithTranslate();

                if (!string.IsNullOrEmpty(TxtPrompt.Text))
                    builder.WithPrompt(TxtPrompt.Text);

                _processor = builder.Build();

                Log("✓ 模型載入完成");

                TxtStatus.Text = "正在提取音訊...";
                var audioFile = await ExtractAudioFromVideoUwp(_selectedVideoFile);
                if (audioFile == null)
                    throw new Exception("音訊提取失敗，請檢查日誌。");

                TxtStatus.Text = "正在分析音訊...";
                var audioDuration = await GetAudioDurationAsync(audioFile);
                if (audioDuration == TimeSpan.Zero)
                    Log("⚠ 無法獲取音訊時長，將無法顯示進度。");

                TxtStatus.Text = "正在生成字幕...";
                DispatcherQueue.TryEnqueue(() =>
                {
                    TranscriptionProgressPanel.Visibility = Visibility.Visible;
                    TranscriptionProgress.Value = 0;
                    TranscriptionProgressText.Text = "0%";
                });

                await StartAudioTranscriptionUwp(audioFile, audioDuration);

                await audioFile.DeleteAsync();
                TxtTranscript.Text = GenerateSRT();
                Log("✓ 字幕生成完成！");
                TxtStatus.Text = "完成";
            }
            catch (OperationCanceledException)
            {
                Log("✗ 處理已由使用者取消。");
                TxtStatus.Text = "已取消";
            }
            catch (Exception ex)
            {
                Log($"✗ 處理過程中發生錯誤: {ex.Message}");
                TxtStatus.Text = "處理失敗";
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private async Task StartAudioTranscriptionUwp(StorageFile audioFile, TimeSpan totalDuration)
        {
            if (_processor == null) return;

            using var stream = await audioFile.OpenStreamForReadAsync();
            var startTime = DateTime.Now;
            var segmentCount = 0;

            await foreach (var result in _processor.ProcessAsync(stream, _cts.Token))
            {
                segmentCount++;
                _subtitleSegments.Add(new SubtitleSegment
                {
                    Index = segmentCount,
                    Start = result.Start,
                    End = result.End,
                    Text = result.Text.Trim()
                });

                if (totalDuration > TimeSpan.Zero)
                {
                    var progress = (int)(result.End.TotalMilliseconds / totalDuration.TotalMilliseconds * 100);
                    progress = Math.Clamp(progress, 0, 100);
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        TranscriptionProgress.Value = progress;
                        TranscriptionProgressText.Text = $"{progress}%";
                    });
                }
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                TranscriptionProgress.Value = 100;
                TranscriptionProgressText.Text = "100%";
            });

            var totalTime = (DateTime.Now - startTime).TotalSeconds;
            Log($"✓ 轉錄完成，共 {segmentCount} 個片段，耗時 {totalTime:F1} 秒。");
        }

        private async Task<TimeSpan> GetAudioDurationAsync(StorageFile audioFile)
        {
            if (_ffmpegFile == null) return TimeSpan.Zero;

            var arguments = $"-i \"{audioFile.Path}\"";
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffmpegFile.Path,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return TimeSpan.Zero;

            string output = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var regex = new Regex(@"Duration: (\d{2}):(\d{2}):(\d{2})\.(\d{2})");
            var match = regex.Match(output);

            if (match.Success)
            {
                return new TimeSpan(0, int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value) * 10);
            }
            return TimeSpan.Zero;
        }

        private async Task<StorageFile?> ExtractAudioFromVideoUwp(StorageFile videoFile)
        {
            if (_ffmpegFile == null) { Log("✗ FFmpeg 執行檔未找到。"); return null; }
            var tempFolder = ApplicationData.Current.TemporaryFolder;
            var outputAudioFile = await tempFolder.CreateFileAsync($"temp_audio_{DateTime.Now.Ticks}.wav", CreationCollisionOption.ReplaceExisting);
            var arguments = $"-i \"{videoFile.Path}\" -ar 16000 -ac 1 -c:a pcm_s16le \"{outputAudioFile.Path}\" -y";
            Log($"執行 FFmpeg 音訊提取...");
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffmpegFile.Path,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            using var process = Process.Start(processInfo);
            if (process == null) { Log("✗ 無法啟動 FFmpeg 程序。請確認專案已設定 'runFullTrust' 權限。"); return null; }
            string errorOutput = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(_cts.Token);
            if (process.ExitCode != 0) { Log($"✗ FFmpeg 執行錯誤 (代碼: {process.ExitCode}): {errorOutput}"); return null; }
            Log("✓ 音訊提取完成。");
            return outputAudioFile;
        }

        private void ToggleTranslate_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                TranslateLanguageComboBox.IsEnabled = toggleSwitch.IsOn;
            }
        }

        private async void BtnSelectVideo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Title = "選擇影片或音訊檔案",
                Filter = "影片檔案 (*.mp4;*.mkv;*.mov;*.avi;*.flv;*.webm;*.vob;*.mpg;*.mpeg;*.ts;*.m2ts;*.3gp;*.3g2;*.f4v;*.swf;*.rm;*.rmvb;*.asf)|*.mp4;*.mkv;*.mov;*.avi;*.flv;*.webm;*.vob;*.mpg;*.mpeg;*.ts;*.m2ts;*.3gp;*.3g2;*.f4v;*.swf;*.rm;*.rmvb;*.asf|" +
             "音訊檔案 (*.mp3;*.wav;*.aac;*.flac;*.ogg;*.m4a;*.opus;*.wma;*.alac;*.amr;*.dts;*.mp2;*.aiff;*.pcm;*.raw)|*.mp3;*.wav;*.aac;*.flac;*.ogg;*.m4a;*.opus;*.wma;*.alac;*.amr;*.dts;*.mp2;*.aiff;*.pcm;*.raw|" +
             "所有檔案 (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var path = dialog.FileName;
                var file = await StorageFile.GetFileFromPathAsync(path);
                _selectedVideoFile = file;
                var props = await file.GetBasicPropertiesAsync();
                TxtSelectedVideo.Text = $"✓ {file.Name} ({FormatFileSize(props.Size)})";
                Log($"已選擇檔案: {file.Name}");
            }
        }

        private void RootGrid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private void RootGrid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                DragDropOverlay.Visibility = Visibility.Visible;
            }
        }

        private void RootGrid_DragLeave(object sender, DragEventArgs e)
        {
            DragDropOverlay.Visibility = Visibility.Collapsed;
        }

        private async void RootGrid_Drop(object sender, DragEventArgs e)
        {
            DragDropOverlay.Visibility = Visibility.Collapsed;

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Any())
                {
                    if (items[0] is StorageFile file)
                    {
                        _selectedVideoFile = file;
                        var props = await file.GetBasicPropertiesAsync();
                        TxtSelectedVideo.Text = $"✓ {file.Name} ({FormatFileSize(props.Size)})";
                        Log($"已透過拖曳方式選擇檔案: {file.Name}");
                    }
                }
            }
        }

        private async void BtnOpenModelFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var modelsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(ModelsFolder);
                var folderPath = modelsFolder.Path;

                Process.Start(new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true,
                    Verb = "open"
                });

                Log($"✓ 已打開模型資料夾: {folderPath}");
            }
            catch (Exception ex)
            {
                Log($"✗ 無法打開模型資料夾: {ex.Message}");
            }
        }

        private async void BtnDownloadModel_Click(object sender, RoutedEventArgs e)
        {
            if (ModelComboBox.SelectedIndex < 0) return;

            var model = _models[ModelComboBox.SelectedIndex];
            var modelsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(ModelsFolder);

            var existingFile = await modelsFolder.TryGetItemAsync(model.Name) as StorageFile;
            if (existingFile != null)
            {
                var properties = await existingFile.GetBasicPropertiesAsync();
                ulong fileSize = properties.Size;
                ulong modelSizeBytes = (ulong)ParseSizeToBytes(model.Size);

                const ulong tolerance = 10 * 1024 * 1024;

                if (Math.Abs((long)fileSize - (long)modelSizeBytes) <= (long)tolerance)
                {
                    Log($"{model.DisplayName} 已存在 ，跳過下載。");
                    return;
                }
                else
                {
                    Log($"{model.DisplayName} 檔案大小不符或損壞，重新下載。");
                }
            }

            BtnDownloadModel.IsEnabled = false;
            ModelDownloadPanel.Visibility = Visibility.Visible;
            ModelDownloadStatus.Text = "準備下載...";

            try
            {
                Log($"開始下載模型: {model.DisplayName}");
                var destFile = await modelsFolder.CreateFileAsync(model.Name, CreationCollisionOption.ReplaceExisting);
                using var response = await _http.GetAsync(model.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                await DownloadWithProgressAsync(response, destFile, ModelDownloadProgress, ModelDownloadStatus);
                Log($"✓ 下載完成: {model.DisplayName}");
            }
            catch (Exception ex)
            {
                Log($"✗ 模型下載失敗: {ex.Message}");
            }
            finally
            {
                BtnDownloadModel.IsEnabled = true;
                await Task.Delay(500);
                ModelDownloadPanel.Visibility = Visibility.Collapsed;
            }
        }

        private long ParseSizeToBytes(string sizeString)
        {
            sizeString = sizeString.Trim().ToUpper();

            if (sizeString.EndsWith("GB"))
            {
                if (double.TryParse(sizeString.Replace("GB", ""), out double gb))
                    return (long)(gb * 1024 * 1024 * 1024);
            }
            else if (sizeString.EndsWith("MB"))
            {
                if (double.TryParse(sizeString.Replace("MB", ""), out double mb))
                    return (long)(mb * 1024 * 1024);
            }
            else if (sizeString.EndsWith("KB"))
            {
                if (double.TryParse(sizeString.Replace("KB", ""), out double kb))
                    return (long)(kb * 1024);
            }
            else if (long.TryParse(sizeString, out long b))
            {
                return b;
            }

            return 0;
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        private async void BtnCopyTranscript_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtTranscript.Text)) return;
            var dataPackage = new DataPackage();
            dataPackage.SetText(TxtTranscript.Text);
            Clipboard.SetContent(dataPackage);
            Log("✓ SRT 字幕已複製到剪貼簿。");
            await ShowTemporaryStatusAsync("✓ 已複製");
        }

        private async void BtnSaveTranscript_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtTranscript.Text)) return;
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("SRT Subtitles", new List<string>() { ".srt" });
            savePicker.SuggestedFileName = _selectedVideoFile != null ? Path.GetFileNameWithoutExtension(_selectedVideoFile.Name) : "subtitles";
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteTextAsync(file, TxtTranscript.Text, UnicodeEncoding.Utf8);
                Log($"✓ 字幕已儲存至: {file.Name}");
                await ShowTemporaryStatusAsync("✓ 已儲存");
            }
        }

        private void BtnAutoDetectCuda_Click(object sender, RoutedEventArgs e)
        {
            Log("ⓘ CUDA 偵測功能在 UWP 沙箱中受限，結果可能不準確。");
            try
            {
                bool hasCuda = DetectCuda();
                if (hasCuda) RuntimePriorityList.SelectedIndex = 0;
                Log($"CUDA 偵測結果: {(hasCuda ? "可用" : "不可用")}");
            }
            catch (Exception ex)
            {
                Log($"CUDA 偵測錯誤: {ex.Message}");
            }
        }

        private bool DetectCuda()
        {
            string[] tryNames = { "nvcuda.dll", "cudart64_12.dll", "cudart64_11.dll" };
            foreach (var n in tryNames)
            {
                if (NativeLibrary.TryLoad(n, out var handle))
                {
                    NativeLibrary.Free(handle);
                    return true;
                }
            }
            return false;
        }

        private void SliderTemp_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (LblTempVal != null) LblTempVal.Text = e.NewValue.ToString("F1");
        }

        private void SetBusyState(bool isBusy)
        {
            if (isBusy)
            {
                _cts = new CancellationTokenSource();
                ProcessingRing.IsActive = true;
                BtnStart.IsEnabled = false;
                BtnStop.IsEnabled = true;
            }
            else
            {
                _cts?.Dispose();
                _cts = null;
                ProcessingRing.IsActive = false;
                BtnStart.IsEnabled = true;
                BtnStop.IsEnabled = false;
                DispatcherQueue.TryEnqueue(() => TranscriptionProgressPanel.Visibility = Visibility.Collapsed);
            }
        }

        private async Task DownloadWithProgressAsync(HttpResponseMessage response, StorageFile destFile, ProgressBar progressBar, TextBlock statusTextBlock)
        {
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = await destFile.OpenStreamForWriteAsync();

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;
            var stopwatch = Stopwatch.StartNew();
            long lastRead = 0;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                if (stopwatch.ElapsedMilliseconds > 500)
                {
                    var bytesSinceLastUpdate = totalRead - lastRead;
                    var speed = bytesSinceLastUpdate / stopwatch.Elapsed.TotalSeconds;
                    var progress = totalBytes > 0 ? (double)totalRead / totalBytes * 100 : 0;

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        progressBar.Value = progress;
                        statusTextBlock.Text = $"{progress:F1}% - {FormatFileSize((ulong)totalRead)} / {FormatFileSize((ulong)totalBytes)} ({FormatSpeed(speed)})";
                    });

                    stopwatch.Restart();
                    lastRead = totalRead;
                }
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                progressBar.Value = 100;
                statusTextBlock.Text = $"下載完成 - {FormatFileSize((ulong)totalBytes)}";
            });
        }

        private string GenerateSRT()
        {
            var sb = new StringBuilder();
            foreach (var segment in _subtitleSegments)
            {
                sb.AppendLine(segment.Index.ToString());
                sb.AppendLine($"{FormatSRTTime(segment.Start)} --> {FormatSRTTime(segment.End)}");
                sb.AppendLine(segment.Text);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private string FormatSRTTime(TimeSpan ts) => $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2},{ts.Milliseconds:D3}";

        private string FormatFileSize(ulong bytes)
        {
            if (bytes == 0) return "0 B";
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond < 1024) return $"{bytesPerSecond:0} B/s";
            string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s" };
            double len = bytesPerSecond;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private async Task ShowTemporaryStatusAsync(string message, int durationMs = 1500)
        {
            var originalText = TxtStatus.Text;
            TxtStatus.Text = message;
            await Task.Delay(durationMs);
            if (TxtStatus.Text == message)
            {
                TxtStatus.Text = originalText;
            }
        }

        private void Log(string text)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                TxtLog.Text = $"[{DateTime.Now:HH:mm:ss}] {text}\n" + TxtLog.Text;
            });
        }
    }
}
