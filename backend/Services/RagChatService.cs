using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiServiceApi.Domain.Interfaces;
using AiServiceApi.Models;

namespace AiServiceApi.Services;

/// <summary>
/// RAG 聊天服务接口 - 支持 Web 搜索增强的对话
/// </summary>
public interface IRagChatService
{
    /// <summary>
    /// 发送支持 RAG 的聊天消息
    /// LLM 将自动判断是否需要搜索，并基于搜索结果生成回答
    /// </summary>
    Task<ChatResponseWithSources> SendRagChatMessageAsync(ChatRequest request, bool enableWebSearch = true);
}

/// <summary>
/// RAG 聊天服务实现 - 基于 Tool Calling 模式
/// 
/// 流程：
/// 1️⃣ 将用户消息和工具定义发送给 LLM
/// 2️⃣ LLM 判断是否需要调用 web_search 工具
/// 3️⃣ 如果需要，执行 Tavily 搜索并将结果返回给 LLM
/// 4️⃣ LLM 基于搜索结果生成最终回答
/// </summary>
public class RagChatService : IRagChatService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IWebSearchService _webSearchService;
    private readonly ILogger<RagChatService> _logger;

    // Web 搜索工具定义
    private static readonly ToolDefinition WebSearchTool = new()
    {
        Type = "function",
        Function = new FunctionDefinition
        {
            Name = "web_search",
            Description = @"Search the web for current information. Use this tool when:
- User asks about current events, news, or recent developments
- User asks about weather, stock prices, or real-time data
- User asks about specific facts you're unsure about
- The question requires up-to-date information beyond your training data
- User explicitly asks to search the web

Do NOT use this tool for:
- General knowledge questions you can answer confidently
- Creative writing or brainstorming tasks
- Personal opinions or advice",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, ParameterProperty>
                {
                    ["query"] = new ParameterProperty
                    {
                        Type = "string",
                        Description = "The search query. Be specific and include relevant context. For weather, include the location. For news, include keywords and time frame if relevant."
                    },
                    ["max_results"] = new ParameterProperty
                    {
                        Type = "integer",
                        Description = "Maximum number of search results to return. Default is 5, max is 10."
                    }
                },
                Required = new List<string> { "query" }
            }
        }
    };

    // RAG 系统提示词 - 强制基于搜索结果回答，输出用户友好的内容
    private const string RagSystemPrompt = @"你是一个友好的 AI 助手，具备实时网络搜索能力。请用中文回答用户问题。

## 回答规范：

### 内容要求：
1. **只使用搜索结果中的信息** - 不要编造或臆测
2. **用自然、流畅的语言回答** - 像朋友聊天一样，不要生硬
3. **信息要清晰易读** - 使用适当的格式让内容更易理解
4. **实时信息要标注时间** - 如天气、新闻等

### 格式规范：
- 使用 Markdown 格式，但保持简洁
- 重点信息用 **粗体** 标注
- 多条信息用列表展示
- 不要在正文中显示长 URL
- 不要在回答末尾重复列出来源（系统会自动显示来源卡片）

### 回答示例（天气）：
今天北京天气 **晴转多云**，气温 **5~15℃**，风力 2-3 级。空气质量良好，适合户外活动。

### 回答示例（新闻）：
关于这个话题，最新情况是：

1. **要点一**：具体内容...
2. **要点二**：具体内容...

### 注意事项：
- 如果搜索结果不足，坦诚说明并提供已知信息
- 不要说「根据搜索结果」「根据网页显示」等冗余表述
- 直接给出答案，像一个知识渊博的朋友在回答问题";

    public RagChatService(
        HttpClient httpClient,
        IConfiguration configuration,
        IWebSearchService webSearchService,
        ILogger<RagChatService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _webSearchService = webSearchService;
        _logger = logger;
    }

    public async Task<ChatResponseWithSources> SendRagChatMessageAsync(ChatRequest request, bool enableWebSearch = true)
    {
        try
        {
            var endpoint = _configuration["AzureAI:Endpoint"];
            var apiKey = _configuration["AzureAI:ApiKey"];
            var chatDeploymentName = _configuration["AzureAI:ChatDeploymentName"] ?? "gpt-5.4";

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                return new ChatResponseWithSources
                {
                    Success = false,
                    Error = "Azure AI endpoint or API key not configured"
                };
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

            // 第一步：发送消息给 LLM，带上工具定义
            var firstResponse = await SendChatWithToolsAsync(
                endpoint, 
                chatDeploymentName, 
                request, 
                enableWebSearch);

            if (!firstResponse.Success)
            {
                return firstResponse;
            }

            // 检查是否有工具调用
            if (firstResponse.ToolCalls != null && firstResponse.ToolCalls.Count > 0)
            {
                _logger.LogInformation("LLM requested {Count} tool call(s)", firstResponse.ToolCalls.Count);
                
                // 执行工具调用并获取最终回答
                return await ExecuteToolCallsAndGetFinalResponseAsync(
                    endpoint,
                    chatDeploymentName,
                    request,
                    firstResponse);
            }

            // 没有工具调用，直接返回回答
            return firstResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RAG chat service");
            return new ChatResponseWithSources
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 发送带工具定义的聊天请求
    /// </summary>
    private async Task<RagChatResponse> SendChatWithToolsAsync(
        string endpoint,
        string deploymentName,
        ChatRequest request,
        bool enableWebSearch)
    {
        var url = $"{endpoint}/openai/deployments/{deploymentName}/chat/completions?api-version=2025-01-01-preview";
        _logger.LogInformation("Sending RAG chat request to: {Url}", url);

        // 构建消息列表，添加 RAG 系统提示
        var messages = new List<object>();

        // 添加系统消息 (如果启用搜索)
        if (enableWebSearch)
        {
            messages.Add(new { role = "system", content = RagSystemPrompt });
        }

        // 添加用户消息
        foreach (var m in request.Messages)
        {
            var messageContent = m.Content.Count == 1 && m.Content[0].Type == "text"
                ? (object)m.Content[0].Text!
                : m.Content.Select(c => new
                {
                    type = c.Type,
                    text = c.Text,
                    image_url = c.ImageUrl != null ? new { url = c.ImageUrl.Url } : null
                }).ToArray();

            messages.Add(new { role = m.Role, content = messageContent });
        }

        var requestBody = new Dictionary<string, object>
        {
            ["messages"] = messages,
            ["temperature"] = request.Temperature,
            ["max_completion_tokens"] = request.MaxTokens,
            ["stream"] = false
        };

        // 添加工具定义
        if (enableWebSearch)
        {
            requestBody["tools"] = new[] { WebSearchTool };
            requestBody["tool_choice"] = "auto"; // 让 LLM 自己决定是否使用工具
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var jsonContent = JsonSerializer.Serialize(requestBody, jsonOptions);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        _logger.LogDebug("RAG chat request body: {Body}", jsonContent);

        var response = await _httpClient.PostAsync(url, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Azure AI RAG chat response status: {StatusCode}", response.StatusCode);
        _logger.LogDebug("Azure AI RAG chat response: {Content}", responseContent);

        if (response.IsSuccessStatusCode)
        {
            return ParseChatResponse(responseContent);
        }
        else
        {
            _logger.LogError("Azure AI RAG chat failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
            return new RagChatResponse
            {
                Success = false,
                Error = $"API request failed: {response.StatusCode} - {responseContent}"
            };
        }
    }

    /// <summary>
    /// 解析聊天响应，提取消息内容或工具调用
    /// </summary>
    private RagChatResponse ParseChatResponse(string responseContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            var choices = root.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                return new RagChatResponse
                {
                    Success = false,
                    Error = "No choices in response"
                };
            }

            var choice = choices[0];
            var message = choice.GetProperty("message");
            var finishReason = choice.GetProperty("finish_reason").GetString();

            var result = new RagChatResponse { Success = true };

            // 检查是否有工具调用
            if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
            {
                result.ToolCalls = new List<ToolCall>();
                foreach (var tc in toolCalls.EnumerateArray())
                {
                    result.ToolCalls.Add(new ToolCall
                    {
                        Id = tc.GetProperty("id").GetString() ?? "",
                        Type = tc.GetProperty("type").GetString() ?? "function",
                        Function = new FunctionCall
                        {
                            Name = tc.GetProperty("function").GetProperty("name").GetString() ?? "",
                            Arguments = tc.GetProperty("function").GetProperty("arguments").GetString() ?? ""
                        }
                    });
                }
            }

            // 检查是否有内容
            if (message.TryGetProperty("content", out var contentElement) && 
                contentElement.ValueKind != JsonValueKind.Null)
            {
                result.Message = contentElement.GetString();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse chat response");
            return new RagChatResponse
            {
                Success = false,
                Error = $"Failed to parse response: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 执行工具调用并获取最终回答
    /// </summary>
    private async Task<ChatResponseWithSources> ExecuteToolCallsAndGetFinalResponseAsync(
        string endpoint,
        string deploymentName,
        ChatRequest originalRequest,
        RagChatResponse firstResponse)
    {
        var sources = new List<SourceReference>();
        var toolResults = new List<ToolResultMessage>();
        string? searchQuery = null;

        foreach (var toolCall in firstResponse.ToolCalls!)
        {
            if (toolCall.Function.Name == "web_search")
            {
                var searchResult = await ExecuteWebSearchToolAsync(toolCall);
                toolResults.Add(new ToolResultMessage
                {
                    Role = "tool",
                    ToolCallId = toolCall.Id,
                    Content = searchResult.Content
                });

                searchQuery = searchResult.Query;
                sources.AddRange(searchResult.Sources);
            }
            else
            {
                _logger.LogWarning("Unknown tool call: {Name}", toolCall.Function.Name);
                toolResults.Add(new ToolResultMessage
                {
                    Role = "tool",
                    ToolCallId = toolCall.Id,
                    Content = $"Error: Unknown tool '{toolCall.Function.Name}'"
                });
            }
        }

        // 构建包含工具结果的完整对话
        var url = $"{endpoint}/openai/deployments/{deploymentName}/chat/completions?api-version=2025-01-01-preview";

        var messages = new List<object>();

        // 系统消息
        messages.Add(new { role = "system", content = RagSystemPrompt });

        // 原始用户消息
        foreach (var m in originalRequest.Messages)
        {
            var messageContent = m.Content.Count == 1 && m.Content[0].Type == "text"
                ? (object)m.Content[0].Text!
                : m.Content.Select(c => new
                {
                    type = c.Type,
                    text = c.Text,
                    image_url = c.ImageUrl != null ? new { url = c.ImageUrl.Url } : null
                }).ToArray();

            messages.Add(new { role = m.Role, content = messageContent });
        }

        // 助手的工具调用消息
        messages.Add(new
        {
            role = "assistant",
            content = (string?)null,
            tool_calls = firstResponse.ToolCalls.Select(tc => new
            {
                id = tc.Id,
                type = tc.Type,
                function = new
                {
                    name = tc.Function.Name,
                    arguments = tc.Function.Arguments
                }
            }).ToArray()
        });

        // 工具结果消息
        foreach (var result in toolResults)
        {
            messages.Add(new
            {
                role = result.Role,
                tool_call_id = result.ToolCallId,
                content = result.Content
            });
        }

        var requestBody = new Dictionary<string, object>
        {
            ["messages"] = messages,
            ["temperature"] = originalRequest.Temperature,
            ["max_completion_tokens"] = originalRequest.MaxTokens,
            ["stream"] = false
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var jsonContent = JsonSerializer.Serialize(requestBody, jsonOptions);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        _logger.LogDebug("Final RAG request with tool results: {Body}", jsonContent);

        var response = await _httpClient.PostAsync(url, httpContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var finalResponse = ParseChatResponse(responseContent);
            
            return new ChatResponseWithSources
            {
                Success = true,
                Message = finalResponse.Message,
                UsedWebSearch = true,
                SearchQuery = searchQuery,
                Sources = sources.Count > 0 ? sources : null
            };
        }
        else
        {
            _logger.LogError("Final RAG chat failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
            return new ChatResponseWithSources
            {
                Success = false,
                Error = $"API request failed: {response.StatusCode}"
            };
        }
    }

    /// <summary>
    /// 执行 Web 搜索工具
    /// </summary>
    private async Task<WebSearchToolResult> ExecuteWebSearchToolAsync(ToolCall toolCall)
    {
        try
        {
            var args = JsonSerializer.Deserialize<WebSearchToolArguments>(
                toolCall.Function.Arguments,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (args == null || string.IsNullOrEmpty(args.Query))
            {
                return new WebSearchToolResult
                {
                    Content = "Error: Invalid search query",
                    Query = "",
                    Sources = new List<SourceReference>()
                };
            }

            var maxResults = Math.Min(args.MaxResults > 0 ? args.MaxResults : 5, 10);
            _logger.LogInformation("Executing web search: {Query} (max {Max} results)", args.Query, maxResults);

            var searchResult = await _webSearchService.SearchAsync(args.Query, maxResults);

            if (!searchResult.Success)
            {
                return new WebSearchToolResult
                {
                    Content = $"Search failed: {searchResult.Error}",
                    Query = args.Query,
                    Sources = new List<SourceReference>()
                };
            }

            // 构建结构化的搜索结果给 LLM（简洁格式）
            var sb = new StringBuilder();
            sb.AppendLine($"【搜索查询】{args.Query}");
            sb.AppendLine($"【搜索时间】{searchResult.SearchedAt:yyyy年M月d日 HH:mm}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(searchResult.Answer))
            {
                sb.AppendLine("【快速摘要】");
                sb.AppendLine(searchResult.Answer);
                sb.AppendLine();
            }

            sb.AppendLine("【详细信息】");
            var sources = new List<SourceReference>();

            for (int i = 0; i < searchResult.Results.Count; i++)
            {
                var r = searchResult.Results[i];
                sb.AppendLine($"[来源{i + 1}] {r.Title}");
                if (r.PublishedDate.HasValue)
                {
                    sb.AppendLine($"发布时间: {r.PublishedDate:yyyy-MM-dd}");
                }
                sb.AppendLine($"内容: {r.Content}");
                sb.AppendLine();

                sources.Add(new SourceReference
                {
                    Title = r.Title,
                    Url = r.Url,
                    Snippet = r.Content.Length > 150 ? r.Content[..150] + "..." : r.Content
                });
            }

            sb.AppendLine("---");
            sb.AppendLine("请根据以上搜索结果用自然流畅的中文回答用户问题。不要在回答中显示 URL 或重复列出来源。");

            return new WebSearchToolResult
            {
                Content = sb.ToString(),
                Query = args.Query,
                Sources = sources
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing web search tool");
            return new WebSearchToolResult
            {
                Content = $"Error executing search: {ex.Message}",
                Query = "",
                Sources = new List<SourceReference>()
            };
        }
    }

    /// <summary>
    /// 内部响应类型，包含工具调用
    /// </summary>
    private class RagChatResponse : ChatResponseWithSources
    {
        public List<ToolCall>? ToolCalls { get; set; }
    }

    /// <summary>
    /// Web 搜索工具结果
    /// </summary>
    private class WebSearchToolResult
    {
        public string Content { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public List<SourceReference> Sources { get; set; } = new();
    }
}
