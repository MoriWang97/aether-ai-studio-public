using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiServiceApi.Models;

/// <summary>
/// 功能模块枚举
/// 新增功能模块时，只需在此添加枚举值即可自动被统计
/// </summary>
public enum FeatureModule
{
    /// <summary>
    /// 聊天功能
    /// </summary>
    Chat = 1,
    
    /// <summary>
    /// 图片生成
    /// </summary>
    Image = 2,
    
    /// <summary>
    /// 语音功能
    /// </summary>
    Speech = 3,
    
    /// <summary>
    /// 法律助手
    /// </summary>
    Legal = 4,
    
    /// <summary>
    /// 玄学助手
    /// </summary>
    Mystic = 5,
    
    /// <summary>
    /// RAG 聊天
    /// </summary>
    RagChat = 6,
    
    /// <summary>
    /// 管理功能
    /// </summary>
    Admin = 100,
    
    /// <summary>
    /// 其他/未分类
    /// </summary>
    Other = 999
}

/// <summary>
/// 用户使用日志实体
/// 记录每一次功能调用
/// </summary>
public class UsageLog
{
    [Key]
    public Guid Id { get; set; }
    
    /// <summary>
    /// 用户ID（外键）
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户导航属性
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
    
    /// <summary>
    /// 功能模块
    /// </summary>
    public FeatureModule Module { get; set; }
    
    /// <summary>
    /// 具体操作/动作名称
    /// 例如：SendMessage, GenerateImage, AnalyzeTarot
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求路径
    /// </summary>
    [MaxLength(500)]
    public string? RequestPath { get; set; }
    
    /// <summary>
    /// HTTP 方法
    /// </summary>
    [MaxLength(10)]
    public string? HttpMethod { get; set; }
    
    /// <summary>
    /// 记录时间（UTC）
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// 请求是否成功
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// HTTP 响应状态码
    /// </summary>
    public int? StatusCode { get; set; }
    
    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public int? ResponseTimeMs { get; set; }
    
    /// <summary>
    /// 用户IP地址（脱敏处理）
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// 用户代理（浏览器/客户端信息）
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// 额外的元数据（JSON格式）
    /// 可存储特定功能的额外信息
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }
}

#region 统计查询请求/响应 DTOs

/// <summary>
/// 使用统计查询请求
/// </summary>
public class UsageStatisticsQuery
{
    /// <summary>
    /// 开始时间（可选）
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// 结束时间（可选）
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// 指定用户ID（可选，不指定则查询所有用户）
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// 指定功能模块（可选，不指定则查询所有模块）
    /// </summary>
    public FeatureModule? Module { get; set; }
    
    /// <summary>
    /// 分组方式
    /// </summary>
    public StatisticsGroupBy GroupBy { get; set; } = StatisticsGroupBy.Day;
    
    /// <summary>
    /// 分页 - 页码
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// 分页 - 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 统计分组方式
/// </summary>
public enum StatisticsGroupBy
{
    Hour,
    Day,
    Week,
    Month,
    Module,
    User,
    Action
}

/// <summary>
/// 使用统计概览响应
/// </summary>
public class UsageStatisticsOverview
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    
    /// <summary>
    /// 总请求数
    /// </summary>
    public long TotalRequests { get; set; }
    
    /// <summary>
    /// 成功请求数
    /// </summary>
    public long SuccessfulRequests { get; set; }
    
    /// <summary>
    /// 失败请求数
    /// </summary>
    public long FailedRequests { get; set; }
    
    /// <summary>
    /// 活跃用户数（有请求记录的用户）
    /// </summary>
    public int ActiveUsers { get; set; }
    
    /// <summary>
    /// 平均响应时间（毫秒）
    /// </summary>
    public double AverageResponseTimeMs { get; set; }
    
    /// <summary>
    /// 各功能模块使用统计
    /// </summary>
    public List<ModuleUsageStats> ModuleStats { get; set; } = new();
    
    /// <summary>
    /// 时间趋势数据
    /// </summary>
    public List<TimeTrendStats> TrendStats { get; set; } = new();
    
    /// <summary>
    /// 用户活跃度排行
    /// </summary>
    public List<UserActivityStats> TopActiveUsers { get; set; } = new();
    
    /// <summary>
    /// 查询的时间范围
    /// </summary>
    public DateTime? QueryStartDate { get; set; }
    public DateTime? QueryEndDate { get; set; }
}

/// <summary>
/// 各模块使用统计
/// </summary>
public class ModuleUsageStats
{
    /// <summary>
    /// 功能模块
    /// </summary>
    public FeatureModule Module { get; set; }
    
    /// <summary>
    /// 模块名称
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求次数
    /// </summary>
    public long RequestCount { get; set; }
    
    /// <summary>
    /// 成功次数
    /// </summary>
    public long SuccessCount { get; set; }
    
    /// <summary>
    /// 独立用户数
    /// </summary>
    public int UniqueUsers { get; set; }
    
    /// <summary>
    /// 平均响应时间
    /// </summary>
    public double AverageResponseTimeMs { get; set; }
    
    /// <summary>
    /// 占总量百分比
    /// </summary>
    public double Percentage { get; set; }
}

/// <summary>
/// 时间趋势统计
/// </summary>
public class TimeTrendStats
{
    /// <summary>
    /// 时间点/时间段
    /// </summary>
    public DateTime Period { get; set; }
    
    /// <summary>
    /// 时间段标签（如 "2026-02-12", "2026年2月 第2周"）
    /// </summary>
    public string PeriodLabel { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求数
    /// </summary>
    public long RequestCount { get; set; }
    
    /// <summary>
    /// 活跃用户数
    /// </summary>
    public int ActiveUsers { get; set; }
    
    /// <summary>
    /// 成功率
    /// </summary>
    public double SuccessRate { get; set; }
}

/// <summary>
/// 用户活跃度统计
/// </summary>
public class UserActivityStats
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户邮箱
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户昵称
    /// </summary>
    public string? Nickname { get; set; }
    
    /// <summary>
    /// 总请求数
    /// </summary>
    public long TotalRequests { get; set; }
    
    /// <summary>
    /// 成功请求数
    /// </summary>
    public long SuccessfulRequests { get; set; }
    
    /// <summary>
    /// 最后活跃时间
    /// </summary>
    public DateTime LastActiveAt { get; set; }
    
    /// <summary>
    /// 使用的功能模块数
    /// </summary>
    public int ModulesUsed { get; set; }
    
    /// <summary>
    /// 最常用模块
    /// </summary>
    public FeatureModule MostUsedModule { get; set; }
}

/// <summary>
/// 详细使用日志查询响应
/// </summary>
public class UsageLogListResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    
    /// <summary>
    /// 日志列表
    /// </summary>
    public List<UsageLogItem> Logs { get; set; } = new();
    
    /// <summary>
    /// 总数量
    /// </summary>
    public long TotalCount { get; set; }
    
    /// <summary>
    /// 当前页
    /// </summary>
    public int Page { get; set; }
    
    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// 使用日志项
/// </summary>
public class UsageLogItem
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string? UserNickname { get; set; }
    public FeatureModule Module { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsSuccess { get; set; }
    public int? StatusCode { get; set; }
    public int? ResponseTimeMs { get; set; }
}

/// <summary>
/// 用户个人使用统计
/// </summary>
public class UserPersonalStats
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Nickname { get; set; }
    
    /// <summary>
    /// 总使用次数
    /// </summary>
    public long TotalUsage { get; set; }
    
    /// <summary>
    /// 各模块使用统计
    /// </summary>
    public List<ModuleUsageStats> ModuleStats { get; set; } = new();
    
    /// <summary>
    /// 最近7天趋势
    /// </summary>
    public List<TimeTrendStats> RecentTrend { get; set; } = new();
    
    /// <summary>
    /// 最后活跃时间
    /// </summary>
    public DateTime? LastActiveAt { get; set; }
    
    /// <summary>
    /// 首次使用时间
    /// </summary>
    public DateTime? FirstUsedAt { get; set; }
}

#endregion
