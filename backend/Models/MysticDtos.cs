using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiServiceApi.Models;

// ===== 请求 DTOs =====

/// <summary>
/// 塔罗牌分析请求
/// </summary>
public class TarotAnalysisRequest
{
    public string SpreadType { get; set; } = "three_cards";
    public string Question { get; set; } = string.Empty;
    public string? FocusArea { get; set; } // love, career, wealth, health, general
}

/// <summary>
/// 塔罗牌对话请求
/// </summary>
public class TarotChatRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 星座运势请求
/// </summary>
public class AstrologyAnalysisRequest
{
    public string Sign { get; set; } = string.Empty;  // 星座标识，如 "aries"
    public string Period { get; set; } = "daily";     // daily, weekly, monthly, yearly
    public string? BirthDate { get; set; }
    public string? BirthTime { get; set; }
    public string? BirthPlace { get; set; }
}

/// <summary>
/// 星座对话请求
/// </summary>
public class AstrologyChatRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 八字命理请求
/// </summary>
public class BaziAnalysisRequest
{
    public string BirthDate { get; set; } = string.Empty;  // YYYY-MM-DD
    public string BirthTime { get; set; } = string.Empty;  // HH:mm
    public string? BirthPlace { get; set; }
    public string Gender { get; set; } = "male";  // male or female
    public string? Name { get; set; }
    public int? AnalysisYear { get; set; }
}

/// <summary>
/// 八字对话请求
/// </summary>
public class BaziChatRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

// ===== 响应 DTOs =====

/// <summary>
/// 塔罗牌响应
/// </summary>
public class TarotAnalysisResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? SessionId { get; set; }
    public TarotReadingDto? Reading { get; set; }
}

/// <summary>
/// 塔罗牌解读数据
/// </summary>
public class TarotReadingDto
{
    public string SpreadType { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public List<TarotPositionDto> Positions { get; set; } = new();
    public string Interpretation { get; set; } = string.Empty;
    public string Advice { get; set; } = string.Empty;
    public int? LuckIndex { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 塔罗牌位置
/// </summary>
public class TarotPositionDto
{
    public string Name { get; set; } = string.Empty;
    public TarotCardDto? Card { get; set; }
}

/// <summary>
/// 塔罗牌
/// </summary>
public class TarotCardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Arcana { get; set; } = "major";
    public bool IsReversed { get; set; }
    public List<string> Keywords { get; set; } = new();
}

/// <summary>
/// 星座运势响应
/// </summary>
public class AstrologyAnalysisResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? SessionId { get; set; }
    public AstrologyReadingDto? Reading { get; set; }
}

/// <summary>
/// 星座运势数据
/// </summary>
public class AstrologyReadingDto
{
    public string Sign { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    
    public FortuneScoreDto Overall { get; set; } = new();
    public FortuneScoreDto Love { get; set; } = new();
    public FortuneScoreDto Career { get; set; } = new();
    public FortuneScoreDto Wealth { get; set; } = new();
    public FortuneScoreDto Health { get; set; } = new();
    
    public string LuckyColor { get; set; } = string.Empty;
    public int LuckyNumber { get; set; }
    public string LuckyDirection { get; set; } = string.Empty;
    public List<string> Compatibility { get; set; } = new();
}

/// <summary>
/// 运势分数
/// </summary>
public class FortuneScoreDto
{
    public int Score { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Advice { get; set; } = string.Empty;
}

/// <summary>
/// 八字命理响应
/// </summary>
public class BaziAnalysisResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? SessionId { get; set; }
    public BaziReadingDto? Reading { get; set; }
}

/// <summary>
/// 八字命理数据
/// </summary>
public class BaziReadingDto
{
    public BaziChartDto Chart { get; set; } = new();
    public string WuxingAnalysis { get; set; } = string.Empty;  // 五行格局详细分析
    public PersonalityDto Personality { get; set; } = new();
    public CareerDto Career { get; set; } = new();
    public RelationshipDto Relationship { get; set; } = new();
    public WealthDto Wealth { get; set; } = new();
    public HealthDto Health { get; set; } = new();
    public AnnualFortuneDto? AnnualFortune { get; set; }
    public List<string> LuckyElements { get; set; } = new();
    public List<string> LuckyColors { get; set; } = new();
    public List<int> LuckyNumbers { get; set; } = new();
}

/// <summary>
/// 八字命盘
/// </summary>
public class BaziChartDto
{
    public PillarDto YearPillar { get; set; } = new();
    public PillarDto MonthPillar { get; set; } = new();
    public PillarDto DayPillar { get; set; } = new();
    public PillarDto HourPillar { get; set; } = new();
    public string DayMaster { get; set; } = string.Empty;
    public string DayMasterElement { get; set; } = string.Empty;
    public Dictionary<string, int> WuxingCount { get; set; } = new();
    public string WuxingBalance { get; set; } = string.Empty;
}

/// <summary>
/// 四柱
/// </summary>
public class PillarDto
{
    public string Stem { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string Element { get; set; } = string.Empty;
}

/// <summary>
/// 性格分析
/// </summary>
public class PersonalityDto
{
    public List<string> Traits { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public string Advice { get; set; } = string.Empty;
}

/// <summary>
/// 事业分析
/// </summary>
public class CareerDto
{
    public List<string> SuitableFields { get; set; } = new();
    public List<string> LuckyDirections { get; set; } = new();
    public string Advice { get; set; } = string.Empty;
}

/// <summary>
/// 感情分析
/// </summary>
public class RelationshipDto
{
    public string IdealPartner { get; set; } = string.Empty;
    public string? MarriageAge { get; set; }
    public string Advice { get; set; } = string.Empty;
}

/// <summary>
/// 财运分析
/// </summary>
public class WealthDto
{
    public string WealthType { get; set; } = string.Empty;
    public List<string> LuckyYears { get; set; } = new();
    public string Advice { get; set; } = string.Empty;
}

/// <summary>
/// 健康分析
/// </summary>
public class HealthDto
{
    public List<string> WeakOrgans { get; set; } = new();
    public string Advice { get; set; } = string.Empty;
}

/// <summary>
/// 流年运势
/// </summary>
public class AnnualFortuneDto
{
    public int Year { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<int> LuckyMonths { get; set; } = new();
    public List<string> Challenges { get; set; } = new();
}

/// <summary>
/// 对话响应
/// </summary>
public class MysticChatResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Response { get; set; }
}

/// <summary>
/// 会话列表响应
/// </summary>
public class MysticSessionListResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<MysticSessionDto> Sessions { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// 会话数据
/// </summary>
public class MysticSessionDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Question { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
