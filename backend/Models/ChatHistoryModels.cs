namespace AiServiceApi.Models;

/// <summary>
/// 聊天会话DTO
/// </summary>
public class ChatSessionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
}

/// <summary>
/// 聊天消息DTO
/// </summary>
public class ChatMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public List<string>? ImageUrls { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 会话详情响应
/// </summary>
public class ChatSessionDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = new();
}

/// <summary>
/// 创建会话请求
/// </summary>
public class CreateSessionRequest
{
    public string? Title { get; set; }
}

/// <summary>
/// 更新会话标题请求
/// </summary>
public class UpdateSessionTitleRequest
{
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// 会话列表响应
/// </summary>
public class ChatSessionListResponse
{
    public bool Success { get; set; }
    public List<ChatSessionDto> Sessions { get; set; } = new();
    public int TotalCount { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 带会话ID的聊天请求
/// </summary>
public class ChatRequestWithSession : ChatRequest
{
    /// <summary>
    /// 会话ID（可选，不提供则创建新会话）
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// 带会话信息的聊天响应
/// </summary>
public class ChatResponseWithSession : ChatResponse
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// 会话标题
    /// </summary>
    public string? SessionTitle { get; set; }
}

/// <summary>
/// 带 RAG 功能的聊天请求
/// </summary>
public class ChatRequestWithRag : ChatRequestWithSession
{
    /// <summary>
    /// 是否启用 Web 搜索（默认使用配置值）
    /// </summary>
    public bool? EnableWebSearch { get; set; }
}

/// <summary>
/// 带会话信息和来源引用的聊天响应
/// </summary>
public class ChatResponseWithSessionAndSources : ChatResponseWithSession
{
    /// <summary>
    /// 是否使用了 Web 搜索
    /// </summary>
    public bool UsedWebSearch { get; set; }
    
    /// <summary>
    /// 搜索查询（如果使用了搜索）
    /// </summary>
    public string? SearchQuery { get; set; }
    
    /// <summary>
    /// 引用的来源列表
    /// </summary>
    public List<SourceReference>? Sources { get; set; }
}
