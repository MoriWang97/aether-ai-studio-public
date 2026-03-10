using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiServiceApi.Models;

/// <summary>
/// 玄学分析类型
/// </summary>
public enum MysticType
{
    /// <summary>
    /// 塔罗牌
    /// </summary>
    Tarot = 0,
    
    /// <summary>
    /// 星座运势
    /// </summary>
    Astrology = 1,
    
    /// <summary>
    /// 八字命理
    /// </summary>
    Bazi = 2
}

/// <summary>
/// 塔罗牌阵类型
/// </summary>
public enum TarotSpreadType
{
    Single = 0,          // 单牌
    ThreeCards = 1,      // 三牌阵
    CelticCross = 2,     // 凯尔特十字
    Relationship = 3,    // 关系牌阵
    Career = 4,          // 事业牌阵
    YesNo = 5            // 是否牌阵
}

/// <summary>
/// 星座类型
/// </summary>
public enum ZodiacSign
{
    Aries = 0,       // 白羊座
    Taurus = 1,      // 金牛座
    Gemini = 2,      // 双子座
    Cancer = 3,      // 巨蟹座
    Leo = 4,         // 狮子座
    Virgo = 5,       // 处女座
    Libra = 6,       // 天秤座
    Scorpio = 7,     // 天蝎座
    Sagittarius = 8, // 射手座
    Capricorn = 9,   // 摩羯座
    Aquarius = 10,   // 水瓶座
    Pisces = 11      // 双鱼座
}

/// <summary>
/// 运势周期
/// </summary>
public enum AstrologyPeriod
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Yearly = 3
}

/// <summary>
/// 五行
/// </summary>
public enum WuXingElement
{
    Metal = 0,  // 金
    Wood = 1,   // 木
    Water = 2,  // 水
    Fire = 3,   // 火
    Earth = 4   // 土
}

/// <summary>
/// 玄学会话记录
/// </summary>
public class MysticSession
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public MysticType Type { get; set; }
    
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Question { get; set; }
    
    /// <summary>
    /// 会话数据（JSON格式存储具体信息）
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string SessionData { get; set; } = "{}";
    
    /// <summary>
    /// 分析结果（JSON格式）
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? AnalysisResult { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsDeleted { get; set; } = false;
    
    // 导航属性
    public User? User { get; set; }
    public ICollection<MysticChatMessage> Messages { get; set; } = new List<MysticChatMessage>();
}

/// <summary>
/// 玄学对话消息
/// </summary>
public class MysticChatMessage
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string SessionId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "user"; // "user" or "assistant"
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    public MysticSession? Session { get; set; }
}
