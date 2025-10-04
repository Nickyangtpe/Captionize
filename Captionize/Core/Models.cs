using System;
using System.Collections.Generic;

namespace Captionize.Core
{
    public class ModelInfo
    {
        public string Name { get; set; }       // 模型檔名
        public string Size { get; set; }       // 檔案大小
        public string DownloadUrl { get; set; } // HuggingFace 下載連結
        public string DisplayName { get; set; } // 中文名稱 + 英文名稱 + 輕量標示
    }

    public static class WhisperModels
    {
        public static readonly List<ModelInfo> All = new List<ModelInfo>
        {
            // 🟢 Tiny
            new ModelInfo { Name = "ggml-tiny.bin", Size = "77.7 MB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin?download=true", DisplayName = "超小型 Tiny（完整模型）" },
            new ModelInfo { Name = "ggml-tiny-q5_1.bin", Size = "32.2 MB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny-q5_1.bin?download=true", DisplayName = "超小型 Tiny（輕量模型）" },
            new ModelInfo { Name = "ggml-tiny-q8_0.bin", Size = "43.5 MB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny-q8_0.bin?download=true", DisplayName = "超小型 Tiny（微輕量模型）" },

            // 🟡 Small
            new ModelInfo { Name = "ggml-small.bin", Size = "488 MB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin?download=true", DisplayName = "小型 Small（完整模型）" },
            new ModelInfo { Name = "ggml-small-q5_1.bin", Size = "190 MB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small-q5_1.bin?download=true", DisplayName = "小型 Small（輕量模型）" },
            new ModelInfo { Name = "ggml-small-q8_0.bin", Size = "264 MB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small-q8_0.bin?download=true", DisplayName = "小型 Small（微輕量模型）" },

            // 🔵 Medium
            new ModelInfo { Name = "ggml-medium.bin", Size = "1.53 GB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin?download=true", DisplayName = "中型 Medium（完整模型）" },
            new ModelInfo { Name = "ggml-medium-q5_0.bin", Size = "539 MB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium-q5_0.bin?download=true", DisplayName = "中型 Medium（輕量模型）" },
            new ModelInfo { Name = "ggml-medium-q8_0.bin", Size = "823 MB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium-q8_0.bin?download=true", DisplayName = "中型 Medium（微輕量模型）" },

            // 🔴 Large V1
            new ModelInfo { Name = "ggml-large-v1.bin", Size = "3.09 GB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v1.bin?download=true", DisplayName = "大型 Large V1（完整模型）" },

            // 🔴 Large V2
            new ModelInfo { Name = "ggml-large-v2.bin", Size = "3.09 GB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v2.bin?download=true", DisplayName = "大型 Large V2（完整模型）" },
            new ModelInfo { Name = "ggml-large-v2-q5_0.bin", Size = "1.08 GB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v2-q5_0.bin?download=true", DisplayName = "大型 Large V2（輕量模型）" },
            new ModelInfo { Name = "ggml-large-v2-q8_0.bin", Size = "1.66 GB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v2-q8_0.bin?download=true", DisplayName = "大型 Large V2（微輕量模型）" },

            // 🔴 Large V3
            new ModelInfo { Name = "ggml-large-v3.bin", Size = "3.1 GB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin?download=true", DisplayName = "大型 Large V3（完整模型）" },
            new ModelInfo { Name = "ggml-large-v3-q5_0.bin", Size = "1.08 GB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3-q5_0.bin?download=true", DisplayName = "大型 Large V3（輕量模型）" },

            // 🔴 Large V3 Turbo
            new ModelInfo { Name = "ggml-large-v3-turbo.bin", Size = "1.62 GB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3-turbo.bin?download=true", DisplayName = "大型 Large V3 Turbo（完整模型）" },
            new ModelInfo { Name = "ggml-large-v3-turbo-q5_0.bin", Size = "574 MB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3-turbo-q5_0.bin?download=true", DisplayName = "大型 Large V3 Turbo（輕量模型）" },
            new ModelInfo { Name = "ggml-large-v3-turbo-q8_0.bin", Size = "874 MB", DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3-turbo-q8_0.bin?download=true", DisplayName = "大型 Large V3 Turbo（微輕量模型）" },
        };
    }
}
