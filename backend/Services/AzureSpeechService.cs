using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AiServiceApi.Models;
using FFMpegCore;

namespace AiServiceApi.Services;

public interface IAzureSpeechService
{
    /// <summary>
    /// 语音转文字
    /// </summary>
    Task<SpeechToTextResponse> TranscribeAsync(SpeechToTextRequest request);
    
    /// <summary>
    /// 文字转语音
    /// </summary>
    Task<TextToSpeechResponse> SynthesizeAsync(TextToSpeechRequest request);
    
    /// <summary>
    /// 获取可用语音列表
    /// </summary>
    Task<AvailableVoicesResponse> GetAvailableVoicesAsync(string locale = "zh-CN");
}

public class AzureSpeechService : IAzureSpeechService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureSpeechService> _logger;

    public AzureSpeechService(HttpClient httpClient, IConfiguration configuration, ILogger<AzureSpeechService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 语音转文字 (STT)
    /// </summary>
    public async Task<SpeechToTextResponse> TranscribeAsync(SpeechToTextRequest request)
    {
        try
        {
            var apiKey = _configuration["AzureSpeech:ApiKey"];
            var region = _configuration["AzureSpeech:Region"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(region))
            {
                return new SpeechToTextResponse
                {
                    Success = false,
                    Error = "Azure Speech service not configured"
                };
            }

            // 解码 Base64 音频数据
            byte[] audioData;
            try
            {
                audioData = Convert.FromBase64String(request.AudioBase64);
            }
            catch (FormatException)
            {
                return new SpeechToTextResponse
                {
                    Success = false,
                    Error = "Invalid audio data format"
                };
            }

            // 如果是 webm 格式，需要转换为 wav（Azure STT REST API 不支持 webm）
            var audioFormat = request.AudioFormat.ToLower();
            if (audioFormat == "webm")
            {
                _logger.LogInformation("Converting webm audio to wav format, original size: {Size} bytes", audioData.Length);
                var convertedData = await ConvertWebmToWavAsync(audioData);
                if (convertedData != null)
                {
                    audioData = convertedData;
                    audioFormat = "wav";
                    _logger.LogInformation("Audio converted to wav, new size: {Size} bytes", audioData.Length);
                }
                else
                {
                    _logger.LogWarning("Failed to convert webm to wav, trying original format");
                }
            }

            // 构建请求 URL
            var endpoint = $"https://{region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1";
            var queryParams = $"?language={request.Language}&format=detailed";
            var requestUrl = endpoint + queryParams;

            // 确定音频类型
            var contentType = audioFormat switch
            {
                "wav" => "audio/wav",
                "ogg" => "audio/ogg",
                "mp3" => "audio/mpeg",
                "m4a" => "audio/mp4",
                "webm" => "audio/webm",
                _ => "audio/wav"
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
            httpRequest.Content = new ByteArrayContent(audioData);
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            _logger.LogInformation("Sending STT request to Azure Speech, audio size: {Size} bytes, format: {Format}, contentType: {ContentType}", 
                audioData.Length, audioFormat, contentType);

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Azure STT response: {StatusCode}, {Content}", response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                var recognitionStatus = result.GetProperty("RecognitionStatus").GetString();
                
                if (recognitionStatus == "Success")
                {
                    var displayText = result.GetProperty("DisplayText").GetString();
                    double? confidence = null;
                    
                    if (result.TryGetProperty("NBest", out var nBest) && nBest.GetArrayLength() > 0)
                    {
                        confidence = nBest[0].GetProperty("Confidence").GetDouble();
                    }

                    return new SpeechToTextResponse
                    {
                        Success = true,
                        Text = displayText,
                        Confidence = confidence
                    };
                }
                else if (recognitionStatus == "NoMatch")
                {
                    return new SpeechToTextResponse
                    {
                        Success = false,
                        Error = "无法识别语音内容，请重新录制"
                    };
                }
                else
                {
                    return new SpeechToTextResponse
                    {
                        Success = false,
                        Error = $"语音识别失败: {recognitionStatus}"
                    };
                }
            }
            else
            {
                _logger.LogError("Azure STT request failed: {StatusCode}, {Content}", response.StatusCode, responseContent);
                return new SpeechToTextResponse
                {
                    Success = false,
                    Error = $"语音识别服务错误: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during speech transcription");
            return new SpeechToTextResponse
            {
                Success = false,
                Error = $"语音识别异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 文字转语音 (TTS)
    /// </summary>
    public async Task<TextToSpeechResponse> SynthesizeAsync(TextToSpeechRequest request)
    {
        try
        {
            var apiKey = _configuration["AzureSpeech:ApiKey"];
            var region = _configuration["AzureSpeech:Region"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(region))
            {
                return new TextToSpeechResponse
                {
                    Success = false,
                    Error = "Azure Speech service not configured"
                };
            }

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return new TextToSpeechResponse
                {
                    Success = false,
                    Error = "Text is required"
                };
            }

            // 限制文本长度（Azure 限制约 10000 字符）
            var text = request.Text.Length > 5000 ? request.Text.Substring(0, 5000) : request.Text;

            // 从语音名称中提取语言区域（如 zh-CN-XiaoxiaoNeural -> zh-CN）
            var voiceLocale = ExtractLocaleFromVoiceName(request.VoiceName);

            // 构建 SSML
            var ratePercent = ((request.Rate - 1) * 100).ToString("+0;-0;0") + "%";
            var pitchPercent = ((request.Pitch - 1) * 100).ToString("+0;-0;0") + "%";
            
            var escapedText = EscapeAndSanitizeForSsml(text);
            var ssml = $@"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{voiceLocale}'>
    <voice name='{request.VoiceName}'>
        <prosody rate='{ratePercent}' pitch='{pitchPercent}'>
            {escapedText}
        </prosody>
    </voice>
</speak>";

            // 构建请求
            var endpoint = $"https://{region}.tts.speech.microsoft.com/cognitiveservices/v1";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
            httpRequest.Headers.Add("X-Microsoft-OutputFormat", GetOutputFormat(request.OutputFormat));
            httpRequest.Headers.Add("User-Agent", "AetherAIStudio");
            
            // 使用 ByteArrayContent 以避免 StringContent 自动添加 charset
            var ssmlBytes = Encoding.UTF8.GetBytes(ssml);
            httpRequest.Content = new ByteArrayContent(ssmlBytes);
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/ssml+xml");

            _logger.LogInformation("Sending TTS request to Azure Speech, text length: {Length}, voice: {Voice}, locale: {Locale}", 
                text.Length, request.VoiceName, voiceLocale);
            _logger.LogInformation("SSML content: {Ssml}", ssml);

            var response = await _httpClient.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
            {
                var audioBytes = await response.Content.ReadAsByteArrayAsync();
                var audioBase64 = Convert.ToBase64String(audioBytes);

                _logger.LogInformation("TTS synthesis successful, audio size: {Size} bytes", audioBytes.Length);

                return new TextToSpeechResponse
                {
                    Success = true,
                    AudioBase64 = audioBase64,
                    AudioFormat = request.OutputFormat
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var responseHeaders = string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(",", h.Value)}"));
                _logger.LogError("Azure TTS request failed: {StatusCode}, Headers: {Headers}, Content: {Content}", 
                    response.StatusCode, responseHeaders, errorContent);
                return new TextToSpeechResponse
                {
                    Success = false,
                    Error = $"语音合成服务错误: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during speech synthesis");
            return new TextToSpeechResponse
            {
                Success = false,
                Error = $"语音合成异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 获取可用语音列表
    /// </summary>
    public async Task<AvailableVoicesResponse> GetAvailableVoicesAsync(string locale = "zh-CN")
    {
        try
        {
            var apiKey = _configuration["AzureSpeech:ApiKey"];
            var region = _configuration["AzureSpeech:Region"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(region))
            {
                // 返回默认语音列表
                return new AvailableVoicesResponse
                {
                    Success = true,
                    Voices = GetDefaultVoices()
                };
            }

            var endpoint = $"https://{region}.tts.speech.microsoft.com/cognitiveservices/voices/list";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
            httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

            var response = await _httpClient.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var voices = JsonSerializer.Deserialize<List<JsonElement>>(content) ?? new List<JsonElement>();

                var filteredVoices = voices
                    .Where(v => v.GetProperty("Locale").GetString()?.StartsWith(locale.Split('-')[0]) == true)
                    .Select(v => new VoiceInfo
                    {
                        Name = v.GetProperty("ShortName").GetString() ?? "",
                        DisplayName = v.GetProperty("DisplayName").GetString() ?? "",
                        LocalName = v.GetProperty("LocalName").GetString() ?? "",
                        Gender = v.GetProperty("Gender").GetString() ?? "",
                        Locale = v.GetProperty("Locale").GetString() ?? ""
                    })
                    .OrderBy(v => v.Locale)
                    .ThenBy(v => v.DisplayName)
                    .ToList();

                return new AvailableVoicesResponse
                {
                    Success = true,
                    Voices = filteredVoices
                };
            }
            else
            {
                // 返回默认语音列表
                return new AvailableVoicesResponse
                {
                    Success = true,
                    Voices = GetDefaultVoices()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available voices");
            return new AvailableVoicesResponse
            {
                Success = true,
                Voices = GetDefaultVoices()
            };
        }
    }

    private static List<VoiceInfo> GetDefaultVoices()
    {
        return new List<VoiceInfo>
        {
            new() { Name = "zh-CN-XiaoxiaoNeural", DisplayName = "Xiaoxiao", LocalName = "晓晓", Gender = "Female", Locale = "zh-CN" },
            new() { Name = "zh-CN-YunxiNeural", DisplayName = "Yunxi", LocalName = "云希", Gender = "Male", Locale = "zh-CN" },
            new() { Name = "zh-CN-YunjianNeural", DisplayName = "Yunjian", LocalName = "云健", Gender = "Male", Locale = "zh-CN" },
            new() { Name = "zh-CN-XiaoyiNeural", DisplayName = "Xiaoyi", LocalName = "晓伊", Gender = "Female", Locale = "zh-CN" },
            new() { Name = "zh-CN-YunyangNeural", DisplayName = "Yunyang", LocalName = "云扬", Gender = "Male", Locale = "zh-CN" },
            new() { Name = "zh-CN-XiaochenNeural", DisplayName = "Xiaochen", LocalName = "晓辰", Gender = "Female", Locale = "zh-CN" },
            new() { Name = "en-US-JennyNeural", DisplayName = "Jenny", LocalName = "Jenny", Gender = "Female", Locale = "en-US" },
            new() { Name = "en-US-GuyNeural", DisplayName = "Guy", LocalName = "Guy", Gender = "Male", Locale = "en-US" },
            new() { Name = "en-US-AriaNeural", DisplayName = "Aria", LocalName = "Aria", Gender = "Female", Locale = "en-US" },
            new() { Name = "ja-JP-NanamiNeural", DisplayName = "Nanami", LocalName = "七海", Gender = "Female", Locale = "ja-JP" },
            new() { Name = "ja-JP-KeitaNeural", DisplayName = "Keita", LocalName = "圭太", Gender = "Male", Locale = "ja-JP" },
        };
    }

    private static string GetOutputFormat(string format)
    {
        return format.ToLower() switch
        {
            "mp3" => "audio-24khz-160kbitrate-mono-mp3",
            "wav" => "riff-24khz-16bit-mono-pcm",
            "ogg" => "ogg-24khz-16bit-mono-opus",
            _ => "audio-24khz-160kbitrate-mono-mp3"
        };
    }

    /// <summary>
    /// 从语音名称中提取语言区域代码
    /// 例如: zh-CN-XiaoxiaoNeural -> zh-CN, en-US-JennyNeural -> en-US
    /// </summary>
    private static string ExtractLocaleFromVoiceName(string voiceName)
    {
        if (string.IsNullOrEmpty(voiceName))
            return "zh-CN";

        // 语音名称格式: {locale}-{name}Neural, 如 zh-CN-XiaoxiaoNeural
        var parts = voiceName.Split('-');
        if (parts.Length >= 2)
        {
            return $"{parts[0]}-{parts[1]}";
        }
        return "zh-CN";
    }

    /// <summary>
    /// 转义并清理文本以用于 SSML
    /// 移除无效的 XML 字符并转义特殊字符
    /// </summary>
    private static string EscapeAndSanitizeForSsml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length);
        
        foreach (var c in text)
        {
            // 跳过无效的 XML 字符
            // XML 1.0 允许的字符: #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]
            if (c == '\t' || c == '\n' || c == '\r' || 
                (c >= 0x20 && c <= 0xD7FF) || 
                (c >= 0xE000 && c <= 0xFFFD))
            {
                sb.Append(c);
            }
            // 其他字符（如控制字符）将被跳过
        }

        var sanitized = sb.ToString();
        
        // 转义 XML 特殊字符
        return sanitized
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    private static string EscapeXml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    /// <summary>
    /// 使用 FFMpegCore 将 webm 音频转换为 wav 格式
    /// FFMpegCore 会自动查找系统中安装的 FFmpeg
    /// 部署时需要确保服务器上安装了 FFmpeg：
    /// - Linux: apt-get install ffmpeg
    /// - Windows: winget install Gyan.FFmpeg
    /// - Docker: 在 Dockerfile 中添加 RUN apt-get install -y ffmpeg
    /// </summary>
    private async Task<byte[]?> ConvertWebmToWavAsync(byte[] webmData)
    {
        var tempInputPath = Path.GetTempFileName() + ".webm";
        var tempOutputPath = Path.GetTempFileName() + ".wav";

        try
        {
            // 检查 FFmpeg 是否可用
            if (!await IsFfmpegAvailableAsync())
            {
                _logger.LogWarning("FFmpeg not found. Please install FFmpeg on the server.");
                return null;
            }

            // 写入临时 webm 文件
            await File.WriteAllBytesAsync(tempInputPath, webmData);

            // 使用 FFMpegCore 转换
            // 参数: 16kHz 采样率, 单声道, 16-bit PCM (Azure Speech 推荐格式)
            var success = await FFMpegArguments
                .FromFileInput(tempInputPath)
                .OutputToFile(tempOutputPath, true, options => options
                    .WithAudioCodec("pcm_s16le")
                    .WithAudioSamplingRate(16000)
                    .WithCustomArgument("-ac 1")
                    .ForceFormat("wav"))
                .ProcessAsynchronously();

            if (!success)
            {
                _logger.LogError("FFMpegCore conversion failed");
                return null;
            }

            // 读取转换后的 wav 文件
            if (File.Exists(tempOutputPath))
            {
                return await File.ReadAllBytesAsync(tempOutputPath);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting webm to wav using FFMpegCore");
            return null;
        }
        finally
        {
            // 清理临时文件
            try
            {
                if (File.Exists(tempInputPath)) File.Delete(tempInputPath);
                if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath);
            }
            catch { /* 忽略清理错误 */ }
        }
    }

    /// <summary>
    /// 检查 FFmpeg 是否可用
    /// </summary>
    private async Task<bool> IsFfmpegAvailableAsync()
    {
        try
        {
            // 配置 FFmpeg 路径（如果在配置中指定）
            var configPath = _configuration["FFmpeg:Path"];
            if (!string.IsNullOrEmpty(configPath))
            {
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    GlobalFFOptions.Configure(options => options.BinaryFolder = directory);
                }
            }

            // 尝试获取 FFprobe 媒体分析来验证 FFmpeg 是否可用
            // 创建一个简单的测试音频来验证
            var testFile = Path.GetTempFileName();
            try
            {
                // 简单检查 FFmpeg 二进制是否存在
                var ffmpegPath = GlobalFFOptions.Current.BinaryFolder;
                var ffmpegExe = Path.Combine(ffmpegPath ?? "", "ffmpeg.exe");
                var ffmpegUnix = Path.Combine(ffmpegPath ?? "", "ffmpeg");
                
                // 检查常见位置
                var exists = File.Exists(ffmpegExe) || 
                             File.Exists(ffmpegUnix) ||
                             await CheckFfmpegInPathAsync();
                
                if (exists)
                {
                    _logger.LogInformation("FFmpeg is available");
                }
                return exists;
            }
            finally
            {
                if (File.Exists(testFile)) File.Delete(testFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FFmpeg not available");
            return false;
        }
    }
    
    /// <summary>
    /// 检查 FFmpeg 是否在 PATH 中
    /// </summary>
    private static async Task<bool> CheckFfmpegInPathAsync()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
