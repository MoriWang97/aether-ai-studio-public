using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiServiceApi.Domain.Interfaces;

namespace AiServiceApi.Infrastructure.Services;

/// <summary>
/// Tavily Web 搜索服务实现
/// Tavily 是专为 AI Agent 设计的搜索引擎，提供高质量的结构化搜索结果
/// https://tavily.com/
/// </summary>
public class TavilySearchService : IWebSearchService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TavilySearchService> _logger;
    private const string TavilyApiBaseUrl = "https://api.tavily.com";

    public TavilySearchService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TavilySearchService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 执行 Tavily Web 搜索
    /// </summary>
    public async Task<WebSearchResult> SearchAsync(string query, int maxResults = 5, string searchDepth = "basic")
    {
        try
        {
            var apiKey = _configuration["Tavily:ApiKey"];
            
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Tavily API key not configured");
                return new WebSearchResult
                {
                    Success = false,
                    Error = "Tavily API key not configured",
                    Query = query
                };
            }

            var requestBody = new TavilySearchRequest
            {
                ApiKey = apiKey,
                Query = query,
                SearchDepth = searchDepth,
                MaxResults = maxResults,
                IncludeAnswer = true, // 请求 Tavily 提供直接回答
                IncludeRawContent = searchDepth == "advanced",
                IncludeImages = false
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var jsonContent = JsonSerializer.Serialize(requestBody, jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling Tavily search API for query: {Query}", query);

            var response = await _httpClient.PostAsync($"{TavilyApiBaseUrl}/search", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tavilyResponse = JsonSerializer.Deserialize<TavilySearchResponse>(responseContent, jsonOptions);
                
                if (tavilyResponse == null)
                {
                    return new WebSearchResult
                    {
                        Success = false,
                        Error = "Failed to parse Tavily response",
                        Query = query
                    };
                }

                _logger.LogInformation("Tavily search returned {Count} results for query: {Query}", 
                    tavilyResponse.Results?.Count ?? 0, query);

                return new WebSearchResult
                {
                    Success = true,
                    Query = query,
                    Answer = tavilyResponse.Answer,
                    Results = tavilyResponse.Results?.Select(r => new WebSearchItem
                    {
                        Title = r.Title ?? string.Empty,
                        Url = r.Url ?? string.Empty,
                        Content = r.Content ?? string.Empty,
                        RawContent = r.RawContent,
                        Score = r.Score,
                        PublishedDate = ParseDate(r.PublishedDate)
                    }).ToList() ?? new List<WebSearchItem>()
                };
            }
            else
            {
                _logger.LogError("Tavily API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new WebSearchResult
                {
                    Success = false,
                    Error = $"Tavily API error: {response.StatusCode} - {responseContent}",
                    Query = query
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Tavily search service");
            return new WebSearchResult
            {
                Success = false,
                Error = ex.Message,
                Query = query
            };
        }
    }

    private DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return null;
        return DateTime.TryParse(dateStr, out var date) ? date : null;
    }
}

#region Tavily API Models

/// <summary>
/// Tavily 搜索请求
/// </summary>
internal class TavilySearchRequest
{
    public string ApiKey { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string SearchDepth { get; set; } = "basic"; // "basic" or "advanced"
    public int MaxResults { get; set; } = 5;
    public bool IncludeAnswer { get; set; } = true;
    public bool IncludeRawContent { get; set; } = false;
    public bool IncludeImages { get; set; } = false;
    public List<string>? IncludeDomains { get; set; }
    public List<string>? ExcludeDomains { get; set; }
}

/// <summary>
/// Tavily 搜索响应
/// </summary>
internal class TavilySearchResponse
{
    public string? Query { get; set; }
    public string? Answer { get; set; }
    public List<TavilySearchResult>? Results { get; set; }
    public double ResponseTime { get; set; }
}

/// <summary>
/// Tavily 单个搜索结果
/// </summary>
internal class TavilySearchResult
{
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? Content { get; set; }
    public string? RawContent { get; set; }
    public double Score { get; set; }
    public string? PublishedDate { get; set; }
}

#endregion
