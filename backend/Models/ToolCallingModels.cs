using System.Text.Json.Serialization;

namespace AiServiceApi.Models;

#region Tool Calling Models - OpenAI Function Calling 格式

/// <summary>
/// 工具定义 - 描述 LLM 可调用的工具
/// </summary>
public class ToolDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";
    
    [JsonPropertyName("function")]
    public FunctionDefinition Function { get; set; } = new();
}

/// <summary>
/// 函数定义
/// </summary>
public class FunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("parameters")]
    public FunctionParameters Parameters { get; set; } = new();
}

/// <summary>
/// 函数参数 (JSON Schema 格式)
/// </summary>
public class FunctionParameters
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";
    
    [JsonPropertyName("properties")]
    public Dictionary<string, ParameterProperty> Properties { get; set; } = new();
    
    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}

/// <summary>
/// 参数属性
/// </summary>
public class ParameterProperty
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("enum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Enum { get; set; }
}

/// <summary>
/// 工具调用请求 (来自 LLM)
/// </summary>
public class ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";
    
    [JsonPropertyName("function")]
    public FunctionCall Function { get; set; } = new();
}

/// <summary>
/// 函数调用详情
/// </summary>
public class FunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty; // JSON 字符串
}

/// <summary>
/// 工具调用结果消息
/// </summary>
public class ToolResultMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "tool";
    
    [JsonPropertyName("tool_call_id")]
    public string ToolCallId { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

#endregion

#region Web Search Tool 特定模型

/// <summary>
/// Web 搜索工具参数
/// </summary>
public class WebSearchToolArguments
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
    
    [JsonPropertyName("max_results")]
    public int MaxResults { get; set; } = 5;
}

#endregion

#region 增强的 Chat 响应模型

/// <summary>
/// 带 RAG 信息的聊天响应
/// </summary>
public class ChatResponseWithSources : ChatResponse
{
    /// <summary>
    /// 引用的来源列表
    /// </summary>
    public List<SourceReference>? Sources { get; set; }
    
    /// <summary>
    /// 是否使用了 Web 搜索
    /// </summary>
    public bool UsedWebSearch { get; set; }
    
    /// <summary>
    /// 搜索查询 (如果使用了搜索)
    /// </summary>
    public string? SearchQuery { get; set; }
}

/// <summary>
/// 来源引用
/// </summary>
public class SourceReference
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Snippet { get; set; }
}

#endregion
