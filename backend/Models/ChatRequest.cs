namespace AiServiceApi.Models;

/// <summary>
/// 聊天消息
/// </summary>
public class ChatMessage
{
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
    public List<MessageContent> Content { get; set; } = new();
}

/// <summary>
/// 消息内容（支持文本和图片）
/// </summary>
public class MessageContent
{
    public string Type { get; set; } = string.Empty; // "text" or "image_url"
    public string? Text { get; set; }
    public ImageUrl? ImageUrl { get; set; }
}

/// <summary>
/// 图片URL对象
/// </summary>
public class ImageUrl
{
    public string Url { get; set; } = string.Empty; // 支持 http(s):// 或 data:image/...;base64,...
}

/// <summary>
/// 聊天请求
/// </summary>
public class ChatRequest
{
    public List<ChatMessage> Messages { get; set; } = new();
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
}

/// <summary>
/// 聊天响应
/// </summary>
public class ChatResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
