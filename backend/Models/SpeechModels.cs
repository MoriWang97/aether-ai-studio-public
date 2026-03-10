namespace AiServiceApi.Models;

/// <summary>
/// 语音转文字请求
/// </summary>
public class SpeechToTextRequest
{
    /// <summary>
    /// 音频文件的 Base64 编码
    /// </summary>
    public string AudioBase64 { get; set; } = string.Empty;
    
    /// <summary>
    /// 音频格式，支持 webm, mp3, wav, ogg 等
    /// </summary>
    public string AudioFormat { get; set; } = "webm";
    
    /// <summary>
    /// 识别语言，默认中文
    /// </summary>
    public string Language { get; set; } = "zh-CN";
}

/// <summary>
/// 语音转文字响应
/// </summary>
public class SpeechToTextResponse
{
    public bool Success { get; set; }
    public string? Text { get; set; }
    public string? Error { get; set; }
    public double? Confidence { get; set; }
}

/// <summary>
/// 文字转语音请求
/// </summary>
public class TextToSpeechRequest
{
    /// <summary>
    /// 要转换的文本
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// 语音名称，默认使用晓晓（中文女声）
    /// 可选值：zh-CN-XiaoxiaoNeural, zh-CN-YunxiNeural, zh-CN-YunjianNeural 等
    /// </summary>
    public string VoiceName { get; set; } = "zh-CN-XiaoxiaoNeural";
    
    /// <summary>
    /// 语速，范围 0.5-2.0，默认 1.0
    /// </summary>
    public double Rate { get; set; } = 1.0;
    
    /// <summary>
    /// 音调，范围 0.5-2.0，默认 1.0
    /// </summary>
    public double Pitch { get; set; } = 1.0;
    
    /// <summary>
    /// 输出格式，默认 mp3
    /// </summary>
    public string OutputFormat { get; set; } = "mp3";
}

/// <summary>
/// 文字转语音响应
/// </summary>
public class TextToSpeechResponse
{
    public bool Success { get; set; }
    
    /// <summary>
    /// 音频数据的 Base64 编码
    /// </summary>
    public string? AudioBase64 { get; set; }
    
    /// <summary>
    /// 音频格式
    /// </summary>
    public string? AudioFormat { get; set; }
    
    public string? Error { get; set; }
}

/// <summary>
/// 可用语音列表响应
/// </summary>
public class AvailableVoicesResponse
{
    public bool Success { get; set; }
    public List<VoiceInfo> Voices { get; set; } = new();
    public string? Error { get; set; }
}

/// <summary>
/// 语音信息
/// </summary>
public class VoiceInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LocalName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
}
