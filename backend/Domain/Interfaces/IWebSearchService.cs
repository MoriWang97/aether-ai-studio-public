namespace AiServiceApi.Domain.Interfaces;

/// <summary>
/// Web 搜索服务接口 - 策略模式的抽象
/// 允许切换不同的搜索提供商 (Tavily, Bing, Google等)
/// </summary>
public interface IWebSearchService
{
    /// <summary>
    /// 执行 Web 搜索
    /// </summary>
    /// <param name="query">搜索查询</param>
    /// <param name="maxResults">最大结果数</param>
    /// <param name="searchDepth">搜索深度 (basic/advanced)</param>
    /// <returns>搜索结果</returns>
    Task<WebSearchResult> SearchAsync(string query, int maxResults = 5, string searchDepth = "basic");
}

/// <summary>
/// Web 搜索结果
/// </summary>
public class WebSearchResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string Query { get; set; } = string.Empty;
    public List<WebSearchItem> Results { get; set; } = new();
    public string? Answer { get; set; } // Tavily 提供的直接回答 (可选)
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 单个搜索结果项
/// </summary>
public class WebSearchItem
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty; // 摘要内容
    public string? RawContent { get; set; } // 原始完整内容 (仅 advanced 模式)
    public double Score { get; set; } // 相关性得分
    public DateTime? PublishedDate { get; set; }
}
