using System.Text;
using System.Text.Json;
using AiServiceApi.Data;
using AiServiceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AiServiceApi.Services;

/// <summary>
/// 玄学助手服务接口
/// </summary>
public interface IMysticAssistantService
{
    // 塔罗牌
    Task<TarotAnalysisResponse> AnalyzeTarotAsync(string userId, TarotAnalysisRequest request);
    Task<MysticChatResponse> ChatTarotAsync(string userId, TarotChatRequest request);
    
    // 星座运势
    Task<AstrologyAnalysisResponse> AnalyzeAstrologyAsync(string userId, AstrologyAnalysisRequest request);
    Task<MysticChatResponse> ChatAstrologyAsync(string userId, AstrologyChatRequest request);
    
    // 八字命理
    Task<BaziAnalysisResponse> AnalyzeBaziAsync(string userId, BaziAnalysisRequest request);
    Task<MysticChatResponse> ChatBaziAsync(string userId, BaziChatRequest request);
    
    // 会话管理
    Task<MysticSessionListResponse> GetUserSessionsAsync(string userId, MysticType? type = null, int skip = 0, int take = 20);
    Task<bool> DeleteSessionAsync(string sessionId, string userId);
}

/// <summary>
/// 玄学助手服务实现
/// </summary>
public class MysticAssistantService : IMysticAssistantService
{
    private readonly AppDbContext _dbContext;
    private readonly IAzureAIService _aiService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MysticAssistantService> _logger;

    // 塔罗牌数据
    private static readonly List<TarotCardDto> MajorArcana = new()
    {
        new() { Id = 0, Name = "愚者", NameEn = "The Fool", Keywords = new() { "新开始", "冒险", "纯真" } },
        new() { Id = 1, Name = "魔术师", NameEn = "The Magician", Keywords = new() { "创造力", "意志力", "技能" } },
        new() { Id = 2, Name = "女祭司", NameEn = "The High Priestess", Keywords = new() { "直觉", "神秘", "智慧" } },
        new() { Id = 3, Name = "女皇", NameEn = "The Empress", Keywords = new() { "丰收", "母性", "创造" } },
        new() { Id = 4, Name = "皇帝", NameEn = "The Emperor", Keywords = new() { "权威", "结构", "领导" } },
        new() { Id = 5, Name = "教皇", NameEn = "The Hierophant", Keywords = new() { "传统", "信仰", "指导" } },
        new() { Id = 6, Name = "恋人", NameEn = "The Lovers", Keywords = new() { "爱情", "选择", "和谐" } },
        new() { Id = 7, Name = "战车", NameEn = "The Chariot", Keywords = new() { "胜利", "决心", "控制" } },
        new() { Id = 8, Name = "力量", NameEn = "Strength", Keywords = new() { "勇气", "耐心", "内在力量" } },
        new() { Id = 9, Name = "隐士", NameEn = "The Hermit", Keywords = new() { "内省", "孤独", "指引" } },
        new() { Id = 10, Name = "命运之轮", NameEn = "Wheel of Fortune", Keywords = new() { "命运", "转变", "机遇" } },
        new() { Id = 11, Name = "正义", NameEn = "Justice", Keywords = new() { "公正", "真相", "因果" } },
        new() { Id = 12, Name = "倒吊人", NameEn = "The Hanged Man", Keywords = new() { "牺牲", "新视角", "等待" } },
        new() { Id = 13, Name = "死神", NameEn = "Death", Keywords = new() { "结束", "转变", "重生" } },
        new() { Id = 14, Name = "节制", NameEn = "Temperance", Keywords = new() { "平衡", "耐心", "调和" } },
        new() { Id = 15, Name = "恶魔", NameEn = "The Devil", Keywords = new() { "束缚", "诱惑", "阴影" } },
        new() { Id = 16, Name = "高塔", NameEn = "The Tower", Keywords = new() { "突变", "破坏", "启示" } },
        new() { Id = 17, Name = "星星", NameEn = "The Star", Keywords = new() { "希望", "灵感", "平静" } },
        new() { Id = 18, Name = "月亮", NameEn = "The Moon", Keywords = new() { "幻象", "直觉", "潜意识" } },
        new() { Id = 19, Name = "太阳", NameEn = "The Sun", Keywords = new() { "快乐", "成功", "活力" } },
        new() { Id = 20, Name = "审判", NameEn = "Judgement", Keywords = new() { "觉醒", "重生", "召唤" } },
        new() { Id = 21, Name = "世界", NameEn = "The World", Keywords = new() { "完成", "圆满", "成就" } }
    };

    // 星座数据
    private static readonly Dictionary<string, (string Chinese, string Element, string[] Traits)> ZodiacData = new()
    {
        ["aries"] = ("白羊座", "火", new[] { "热情", "冲动", "勇敢" }),
        ["taurus"] = ("金牛座", "土", new[] { "稳重", "务实", "固执" }),
        ["gemini"] = ("双子座", "风", new[] { "机智", "善变", "好奇" }),
        ["cancer"] = ("巨蟹座", "水", new[] { "敏感", "顾家", "情绪化" }),
        ["leo"] = ("狮子座", "火", new[] { "自信", "慷慨", "霸道" }),
        ["virgo"] = ("处女座", "土", new[] { "完美", "挑剔", "务实" }),
        ["libra"] = ("天秤座", "风", new[] { "公正", "优雅", "犹豫" }),
        ["scorpio"] = ("天蝎座", "水", new[] { "神秘", "执着", "敏锐" }),
        ["sagittarius"] = ("射手座", "火", new[] { "乐观", "自由", "直率" }),
        ["capricorn"] = ("摩羯座", "土", new[] { "野心", "自律", "保守" }),
        ["aquarius"] = ("水瓶座", "风", new[] { "独立", "创新", "叛逆" }),
        ["pisces"] = ("双鱼座", "水", new[] { "浪漫", "敏感", "梦幻" })
    };

    // 天干地支
    private static readonly string[] TianGan = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
    private static readonly string[] DiZhi = { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };
    private static readonly Dictionary<string, string> TianGanElement = new()
    {
        ["甲"] = "木", ["乙"] = "木", ["丙"] = "火", ["丁"] = "火", ["戊"] = "土",
        ["己"] = "土", ["庚"] = "金", ["辛"] = "金", ["壬"] = "水", ["癸"] = "水"
    };

    public MysticAssistantService(
        AppDbContext dbContext,
        IAzureAIService aiService,
        IConfiguration configuration,
        ILogger<MysticAssistantService> logger)
    {
        _dbContext = dbContext;
        _aiService = aiService;
        _configuration = configuration;
        _logger = logger;
    }

    #region Helper Methods

    /// <summary>
    /// 创建文本消息
    /// </summary>
    private static ChatMessage CreateTextMessage(string role, string text)
    {
        return new ChatMessage
        {
            Role = role,
            Content = new List<MessageContent>
            {
                new() { Type = "text", Text = text }
            }
        };
    }

    /// <summary>
    /// 调用AI服务获取响应
    /// </summary>
    private async Task<string> GetAiResponseAsync(string systemPrompt, string userPrompt)
    {
        var request = new ChatRequest
        {
            Messages = new List<ChatMessage>
            {
                CreateTextMessage("system", systemPrompt),
                CreateTextMessage("user", userPrompt)
            },
            Temperature = 0.8,
            MaxTokens = 10000  // 推理模型需要更多token用于内部推理
        };

        var response = await _aiService.SendChatMessageAsync(request);
        
        if (!response.Success || string.IsNullOrEmpty(response.Message))
        {
            throw new Exception(response.Error ?? "AI服务响应失败");
        }

        return response.Message;
    }

    /// <summary>
    /// 调用AI服务获取带历史的响应
    /// </summary>
    private async Task<string> GetAiResponseWithHistoryAsync(string systemPrompt, List<(string role, string content)> history, string userMessage)
    {
        var messages = new List<ChatMessage> { CreateTextMessage("system", systemPrompt) };
        
        foreach (var (role, content) in history)
        {
            messages.Add(CreateTextMessage(role, content));
        }
        
        messages.Add(CreateTextMessage("user", userMessage));

        var request = new ChatRequest
        {
            Messages = messages,
            Temperature = 0.8,
            MaxTokens = 8000  // 推理模型需要更多token用于内部推理
        };

        var response = await _aiService.SendChatMessageAsync(request);
        
        if (!response.Success || string.IsNullOrEmpty(response.Message))
        {
            throw new Exception(response.Error ?? "AI服务响应失败");
        }

        return response.Message;
    }

    #endregion

    #region 塔罗牌分析

    public async Task<TarotAnalysisResponse> AnalyzeTarotAsync(string userId, TarotAnalysisRequest request)
    {
        try
        {
            // 根据牌阵类型抽取牌
            var positions = GetTarotPositions(request.SpreadType);
            var drawnCards = DrawCards(positions.Count);
            
            // 组装位置和牌
            for (int i = 0; i < positions.Count; i++)
            {
                positions[i].Card = drawnCards[i];
            }

            // 构建AI提示
            var prompt = BuildTarotPrompt(request.Question, request.SpreadType, positions);
            
            // 调用AI获取解读
            var aiResponse = await GetAiResponseAsync(GetTarotSystemPrompt(), prompt);

            // 解析AI响应
            var interpretation = ParseTarotResponse(aiResponse);

            // 创建会话记录
            var session = new MysticSession
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Type = MysticType.Tarot,
                Title = string.IsNullOrEmpty(request.Question) 
                    ? $"塔罗占卜 - {GetSpreadTypeName(request.SpreadType)}" 
                    : request.Question.Length > 30 ? request.Question[..30] + "..." : request.Question,
                Question = request.Question,
                SessionData = JsonSerializer.Serialize(new { request.SpreadType, request.FocusArea, Positions = positions }),
                AnalysisResult = JsonSerializer.Serialize(interpretation),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.MysticSessions.Add(session);
            await _dbContext.SaveChangesAsync();

            return new TarotAnalysisResponse
            {
                Success = true,
                SessionId = session.Id,
                Reading = new TarotReadingDto
                {
                    SpreadType = request.SpreadType,
                    Question = request.Question,
                    Positions = positions,
                    Interpretation = interpretation.interpretation,
                    Advice = interpretation.advice,
                    LuckIndex = interpretation.luckIndex,
                    CreatedAt = session.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "塔罗牌分析失败");
            return new TarotAnalysisResponse
            {
                Success = false,
                Error = "塔罗牌分析失败，请稍后重试"
            };
        }
    }

    public async Task<MysticChatResponse> ChatTarotAsync(string userId, TarotChatRequest request)
    {
        try
        {
            var session = await _dbContext.MysticSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == userId);

            if (session == null)
            {
                return new MysticChatResponse { Success = false, Error = "会话不存在" };
            }

            // 构建历史消息
            var history = session.Messages
                .OrderBy(m => m.Timestamp)
                .TakeLast(10)
                .Select(m => (m.Role, m.Content))
                .ToList();

            // 调用AI
            var response = await GetAiResponseWithHistoryAsync(
                GetTarotChatSystemPrompt(session),
                history,
                request.Message);

            // 保存消息
            session.Messages.Add(new MysticChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = session.Id,
                Role = "user",
                Content = request.Message,
                Timestamp = DateTime.UtcNow
            });
            session.Messages.Add(new MysticChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = session.Id,
                Role = "assistant",
                Content = response,
                Timestamp = DateTime.UtcNow
            });
            session.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return new MysticChatResponse { Success = true, Response = response };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "塔罗牌对话失败");
            return new MysticChatResponse { Success = false, Error = "对话失败，请稍后重试" };
        }
    }

    private List<TarotPositionDto> GetTarotPositions(string spreadType)
    {
        return spreadType switch
        {
            "single" => new() { new() { Name = "答案" } },
            "three_cards" => new() { new() { Name = "过去" }, new() { Name = "现在" }, new() { Name = "未来" } },
            "celtic_cross" => new()
            {
                new() { Name = "现状" }, new() { Name = "阻碍" }, new() { Name = "意识" },
                new() { Name = "潜意识" }, new() { Name = "过去" }, new() { Name = "未来" },
                new() { Name = "自我" }, new() { Name = "环境" }, new() { Name = "希望/恐惧" },
                new() { Name = "结果" }
            },
            "relationship" => new()
            {
                new() { Name = "你的感受" }, new() { Name = "对方感受" }, new() { Name = "关系现状" },
                new() { Name = "挑战" }, new() { Name = "建议" }
            },
            "career" => new()
            {
                new() { Name = "当前工作" }, new() { Name = "挑战" }, new() { Name = "机遇" }, new() { Name = "建议" }
            },
            "yes_no" => new() { new() { Name = "答案" } },
            _ => new() { new() { Name = "过去" }, new() { Name = "现在" }, new() { Name = "未来" } }
        };
    }

    private List<TarotCardDto> DrawCards(int count)
    {
        var random = new Random();
        var availableCards = MajorArcana.ToList();
        var drawn = new List<TarotCardDto>();

        for (int i = 0; i < count && availableCards.Count > 0; i++)
        {
            var index = random.Next(availableCards.Count);
            var card = availableCards[index];
            availableCards.RemoveAt(index);
            
            drawn.Add(new TarotCardDto
            {
                Id = card.Id,
                Name = card.Name,
                NameEn = card.NameEn,
                Arcana = card.Arcana,
                IsReversed = random.NextDouble() > 0.7, // 30%概率逆位
                Keywords = card.Keywords
            });
        }

        return drawn;
    }

    private string GetSpreadTypeName(string spreadType) => spreadType switch
    {
        "single" => "单牌占卜",
        "three_cards" => "三牌阵",
        "celtic_cross" => "凯尔特十字",
        "relationship" => "感情牌阵",
        "career" => "事业牌阵",
        "yes_no" => "是否占卜",
        _ => "塔罗占卜"
    };

    private string GetTarotSystemPrompt() => @"你是一位神秘而专业的塔罗牌师，拥有多年的解读经验。

你的解读风格:
- 神秘但不晦涩，温暖而有洞察力
- 结合牌面含义和问题背景给出解读
- 正位和逆位有明显不同的解读
- 给出具体的建议和指引
- 适当使用比喻和意象
- 回答时保持神秘感但给出实质性的指引

【重要】你需要根据每张牌在对应位置的特殊含义进行解读，而不是简单地解释牌面本身。例如:
- 「过去」位置的牌代表影响现状的过往经历
- 「现在」位置的牌代表当前面临的状况和能量
- 「未来」位置的牌代表可能的发展趋势
- 「障碍」位置的牌代表需要克服的挑战
- 「建议」位置的牌代表可以采取的行动方向

请用JSON格式返回解读结果：
{
  ""interpretation"": ""详细解读内容（至少300字，要有深度和洞察力，结合问题和每张牌在其位置的特殊含义）"",
  ""advice"": ""具体可行的建议（至少100字）"",
  ""luckIndex"": 0-100的运势指数
}";

    private string BuildTarotPrompt(string question, string spreadType, List<TarotPositionDto> positions)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"问卜者的问题：{(string.IsNullOrEmpty(question) ? "请为我一般性地占卜" : question)}");
        sb.AppendLine($"使用牌阵：{GetSpreadTypeName(spreadType)}");
        sb.AppendLine("\n抽到的牌：");
        
        foreach (var pos in positions)
        {
            var reversedText = pos.Card?.IsReversed == true ? "（逆位）" : "（正位）";
            sb.AppendLine($"- {pos.Name}位：{pos.Card?.Name}{reversedText}");
        }

        sb.AppendLine("\n请根据以上牌面给出详细解读。");
        return sb.ToString();
    }

    private (string interpretation, string advice, int luckIndex) ParseTarotResponse(string aiResponse)
    {
        try
        {
            // 尝试提取JSON
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = aiResponse[jsonStart..(jsonEnd + 1)];
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                return (
                    root.TryGetProperty("interpretation", out var interp) ? interp.GetString() ?? "" : aiResponse,
                    root.TryGetProperty("advice", out var adv) ? adv.GetString() ?? "" : "",
                    root.TryGetProperty("luckIndex", out var luck) ? luck.GetInt32() : 70
                );
            }
        }
        catch
        {
            // 解析失败，返回原始响应
        }
        
        return (aiResponse, "", 70);
    }

    private string GetTarotChatSystemPrompt(MysticSession session)
    {
        return $@"你是一位塔罗牌占卜师，正在与问卜者进行后续交流。

之前的占卜问题：{session.Question ?? "一般性占卜"}
占卜结果：{session.AnalysisResult}

请基于之前的占卜结果，回答问卜者的追问。保持神秘但友善的语气，给出有洞察力的回答。";
    }

    #endregion

    #region 星座运势

    public async Task<AstrologyAnalysisResponse> AnalyzeAstrologyAsync(string userId, AstrologyAnalysisRequest request)
    {
        try
        {
            if (!ZodiacData.TryGetValue(request.Sign.ToLower(), out var zodiacInfo))
            {
                return new AstrologyAnalysisResponse { Success = false, Error = "无效的星座" };
            }

            // 构建AI提示
            var prompt = BuildAstrologyPrompt(request.Sign, zodiacInfo, request.Period);
            
            // 调用AI
            var aiResponse = await GetAiResponseAsync(GetAstrologySystemPrompt(), prompt);

            // 解析响应
            var reading = ParseAstrologyResponse(aiResponse, request.Sign, request.Period);

            // 创建会话
            var session = new MysticSession
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Type = MysticType.Astrology,
                Title = $"{zodiacInfo.Chinese} {GetPeriodName(request.Period)}运势",
                SessionData = JsonSerializer.Serialize(request),
                AnalysisResult = JsonSerializer.Serialize(reading),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.MysticSessions.Add(session);
            await _dbContext.SaveChangesAsync();

            return new AstrologyAnalysisResponse
            {
                Success = true,
                SessionId = session.Id,
                Reading = reading
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "星座运势分析失败");
            return new AstrologyAnalysisResponse { Success = false, Error = "分析失败，请稍后重试" };
        }
    }

    public async Task<MysticChatResponse> ChatAstrologyAsync(string userId, AstrologyChatRequest request)
    {
        try
        {
            var session = await _dbContext.MysticSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == userId);

            if (session == null)
            {
                return new MysticChatResponse { Success = false, Error = "会话不存在" };
            }

            var history = session.Messages
                .OrderBy(m => m.Timestamp)
                .TakeLast(10)
                .Select(m => (m.Role, m.Content))
                .ToList();

            var response = await GetAiResponseWithHistoryAsync(
                GetAstrologyChatSystemPrompt(session),
                history,
                request.Message);

            session.Messages.Add(new MysticChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = session.Id,
                Role = "user",
                Content = request.Message,
                Timestamp = DateTime.UtcNow
            });
            session.Messages.Add(new MysticChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = session.Id,
                Role = "assistant",
                Content = response,
                Timestamp = DateTime.UtcNow
            });
            session.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return new MysticChatResponse { Success = true, Response = response };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "星座对话失败");
            return new MysticChatResponse { Success = false, Error = "对话失败，请稍后重试" };
        }
    }

    private string GetAstrologySystemPrompt() => @"你是一位专业的占星师，精通西方占星学。

你的风格:
- 专业但易懂，深入浅出
- 结合星座特质和当前星象进行分析
- 给出各方面的详细运势分析
- 提供实用的建议和指引
- 解读要有深度，不要泛泛而谈

【重要】每个运势维度的summary至少50字，advice至少30字，要具体有针对性。

请用JSON格式返回运势分析:
{
  ""overall"": { ""score"": 0-100, ""summary"": ""总体运势概述（详细描述当前整体状态和趋势）"", ""advice"": ""总体建议"" },
  ""love"": { ""score"": 0-100, ""summary"": ""感情运势（单身/有伴侣分别分析）"", ""advice"": ""感情建议"" },
  ""career"": { ""score"": 0-100, ""summary"": ""事业运势（工作状态、发展机会）"", ""advice"": ""事业建议"" },
  ""wealth"": { ""score"": 0-100, ""summary"": ""财务运势（正财偏财分析）"", ""advice"": ""理财建议"" },
  ""health"": { ""score"": 0-100, ""summary"": ""健康运势（身心状态）"", ""advice"": ""健康建议"" },
  ""luckyColor"": ""幸运颜色"",
  ""luckyNumber"": 幸运数字,
  ""luckyDirection"": ""幸运方位"",
  ""compatibility"": [""最配星座1"", ""最配星座2""]
}";

    private string BuildAstrologyPrompt(string sign, (string Chinese, string Element, string[] Traits) info, string period)
    {
        return $@"请为{info.Chinese}分析{GetPeriodName(period)}运势。

星座信息：
- 名称：{info.Chinese}
- 元素：{info.Element}
- 特质：{string.Join("、", info.Traits)}
- 分析周期：{GetPeriodName(period)}
- 日期：{DateTime.Now:yyyy年MM月dd日}

请给出详细的运势分析。";
    }

    private string GetPeriodName(string period) => period switch
    {
        "daily" => "今日",
        "weekly" => "本周",
        "monthly" => "本月",
        "yearly" => "今年",
        _ => "今日"
    };

    private AstrologyReadingDto ParseAstrologyResponse(string aiResponse, string sign, string period)
    {
        var reading = new AstrologyReadingDto
        {
            Sign = sign,
            Period = period,
            Date = DateTime.Now.ToString("yyyy-MM-dd")
        };

        try
        {
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = aiResponse[jsonStart..(jsonEnd + 1)];
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                reading.Overall = ParseFortuneScore(root, "overall");
                reading.Love = ParseFortuneScore(root, "love");
                reading.Career = ParseFortuneScore(root, "career");
                reading.Wealth = ParseFortuneScore(root, "wealth");
                reading.Health = ParseFortuneScore(root, "health");

                if (root.TryGetProperty("luckyColor", out var color))
                    reading.LuckyColor = color.GetString() ?? "紫色";
                if (root.TryGetProperty("luckyNumber", out var num))
                    reading.LuckyNumber = num.GetInt32();
                if (root.TryGetProperty("luckyDirection", out var dir))
                    reading.LuckyDirection = dir.GetString() ?? "东方";
                if (root.TryGetProperty("compatibility", out var compat))
                {
                    reading.Compatibility = compat.EnumerateArray()
                        .Select(x => x.GetString() ?? "")
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();
                }
            }
        }
        catch
        {
            // 使用默认值
            reading.Overall = new FortuneScoreDto { Score = 75, Summary = aiResponse, Advice = "" };
        }

        return reading;
    }

    private FortuneScoreDto ParseFortuneScore(JsonElement root, string name)
    {
        if (root.TryGetProperty(name, out var element))
        {
            return new FortuneScoreDto
            {
                Score = element.TryGetProperty("score", out var s) ? s.GetInt32() : 70,
                Summary = element.TryGetProperty("summary", out var sum) ? sum.GetString() ?? "" : "",
                Advice = element.TryGetProperty("advice", out var adv) ? adv.GetString() ?? "" : ""
            };
        }
        return new FortuneScoreDto { Score = 70, Summary = "", Advice = "" };
    }

    private string GetAstrologyChatSystemPrompt(MysticSession session)
    {
        return $@"你是一位占星师，正在与用户讨论星座运势。

之前的运势分析：{session.AnalysisResult}

请基于之前的分析，回答用户的追问。保持专业友善的语气。";
    }

    #endregion

    #region 八字命理

    public async Task<BaziAnalysisResponse> AnalyzeBaziAsync(string userId, BaziAnalysisRequest request)
    {
        try
        {
            // 计算八字
            var chart = CalculateBaziChart(request.BirthDate, request.BirthTime);
            
            // 构建AI提示
            var prompt = BuildBaziPrompt(chart, request.Gender, request.AnalysisYear);
            
            // 调用AI
            var aiResponse = await GetAiResponseAsync(GetBaziSystemPrompt(), prompt);

            // 解析响应
            var reading = ParseBaziResponse(aiResponse, chart);

            // 创建会话
            var session = new MysticSession
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Type = MysticType.Bazi,
                Title = $"八字命理 - {chart.DayMaster}日主",
                SessionData = JsonSerializer.Serialize(new { request.BirthDate, request.BirthTime, request.Gender }),
                AnalysisResult = JsonSerializer.Serialize(reading),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.MysticSessions.Add(session);
            await _dbContext.SaveChangesAsync();

            return new BaziAnalysisResponse
            {
                Success = true,
                SessionId = session.Id,
                Reading = reading
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "八字分析失败");
            return new BaziAnalysisResponse { Success = false, Error = "分析失败，请稍后重试" };
        }
    }

    public async Task<MysticChatResponse> ChatBaziAsync(string userId, BaziChatRequest request)
    {
        try
        {
            var session = await _dbContext.MysticSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == userId);

            if (session == null)
            {
                return new MysticChatResponse { Success = false, Error = "会话不存在" };
            }

            var history = session.Messages
                .OrderBy(m => m.Timestamp)
                .TakeLast(10)
                .Select(m => (m.Role, m.Content))
                .ToList();

            var response = await GetAiResponseWithHistoryAsync(
                GetBaziChatSystemPrompt(session),
                history,
                request.Message);

            session.Messages.Add(new MysticChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = session.Id,
                Role = "user",
                Content = request.Message,
                Timestamp = DateTime.UtcNow
            });
            session.Messages.Add(new MysticChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = session.Id,
                Role = "assistant",
                Content = response,
                Timestamp = DateTime.UtcNow
            });
            session.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return new MysticChatResponse { Success = true, Response = response };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "八字对话失败");
            return new MysticChatResponse { Success = false, Error = "对话失败，请稍后重试" };
        }
    }

    private BaziChartDto CalculateBaziChart(string birthDate, string birthTime)
    {
        // 简化的八字计算（实际应使用专业的农历算法库）
        var date = DateTime.Parse(birthDate);
        var hour = int.Parse(birthTime.Split(':')[0]);

        // 基于年月日时计算天干地支（简化算法）
        var yearOffset = (date.Year - 4) % 60;
        var yearStem = TianGan[yearOffset % 10];
        var yearBranch = DiZhi[yearOffset % 12];

        var monthOffset = ((date.Year - 1900) * 12 + date.Month + 12) % 60;
        var monthStem = TianGan[monthOffset % 10];
        var monthBranch = DiZhi[(date.Month + 1) % 12];

        var dayOffset = (int)((date - new DateTime(1900, 1, 1)).TotalDays + 10) % 60;
        var dayStem = TianGan[dayOffset % 10];
        var dayBranch = DiZhi[dayOffset % 12];

        var hourBranchIndex = (hour + 1) / 2 % 12;
        var hourStemIndex = (dayOffset % 10 * 2 + hourBranchIndex) % 10;
        var hourStem = TianGan[hourStemIndex];
        var hourBranch = DiZhi[hourBranchIndex];

        // 统计五行
        var wuxingCount = new Dictionary<string, int>
        {
            ["金"] = 0, ["木"] = 0, ["水"] = 0, ["火"] = 0, ["土"] = 0
        };

        foreach (var stem in new[] { yearStem, monthStem, dayStem, hourStem })
        {
            if (TianGanElement.TryGetValue(stem, out var element))
            {
                wuxingCount[element]++;
            }
        }

        return new BaziChartDto
        {
            YearPillar = new PillarDto { Stem = yearStem, Branch = yearBranch, Element = TianGanElement.GetValueOrDefault(yearStem, "木") },
            MonthPillar = new PillarDto { Stem = monthStem, Branch = monthBranch, Element = TianGanElement.GetValueOrDefault(monthStem, "木") },
            DayPillar = new PillarDto { Stem = dayStem, Branch = dayBranch, Element = TianGanElement.GetValueOrDefault(dayStem, "木") },
            HourPillar = new PillarDto { Stem = hourStem, Branch = hourBranch, Element = TianGanElement.GetValueOrDefault(hourStem, "木") },
            DayMaster = dayStem,
            DayMasterElement = TianGanElement.GetValueOrDefault(dayStem, "木"),
            WuxingCount = wuxingCount,
            WuxingBalance = GetWuxingBalance(wuxingCount)
        };
    }

    private string GetWuxingBalance(Dictionary<string, int> wuxingCount)
    {
        var max = wuxingCount.MaxBy(x => x.Value);
        var min = wuxingCount.Where(x => x.Value == 0).Select(x => x.Key).ToList();
        
        if (min.Count > 0)
            return $"缺{string.Join("、", min)}";
        if (max.Value >= 3)
            return $"{max.Key}旺";
        return "五行平衡";
    }

    private string GetBaziSystemPrompt() => @"你是一位精通中国传统命理学的大师，擅长八字排盘和五行分析。

你的风格:
- 结合传统命理知识，深入分析命盘
- 详细分析五行生克关系和喜用神
- 给出具体的人生指导和建议
- 用现代语言解释古老智慧，通俗易懂
- 分析要有深度，结合命盘实际情况

【重要】
1. 请根据提供的八字四柱和五行分布进行专业分析
2. 喜用神要根据日主强弱和五行平衡来判断
3. 每个分析维度要详细具体，advice至少50字
4. 流年运势要结合命盘特点给出针对性分析

请用JSON格式返回八字分析:
{
  ""wuxingAnalysis"": ""五行格局详细分析（至少100字，分析日主强弱、五行生克、格局特点）"",
  ""personality"": {
    ""traits"": [""性格特点1"", ""性格特点2"", ""性格特点3""],
    ""strengths"": [""优势1"", ""优势2""],
    ""weaknesses"": [""需要注意的方面1"", ""需要注意的方面2""],
    ""advice"": ""性格发展建议（至少50字）""
  },
  ""career"": {
    ""suitableFields"": [""适合行业1"", ""适合行业2"", ""适合行业3""],
    ""luckyDirections"": [""有利方位1"", ""有利方位2""],
    ""advice"": ""事业发展建议（至少50字）""
  },
  ""relationship"": {
    ""idealPartner"": ""理想伴侣特征描述"",
    ""marriageAge"": ""适婚年龄段"",
    ""advice"": ""感情婚姻建议（至少50字）""
  },
  ""wealth"": {
    ""wealthType"": ""财富类型（正财/偏财/正偏财兼有等）"",
    ""luckyYears"": [""财运较好的年份1"", ""年份2""],
    ""advice"": ""理财投资建议（至少50字）""
  },
  ""health"": {
    ""weakOrgans"": [""需要关注的身体部位1"", ""部位2""],
    ""advice"": ""养生保健建议（至少50字）""
  },
  ""annualFortune"": {
    ""year"": 分析年份,
    ""summary"": ""流年运势详细分析（至少100字，结合命盘分析流年干支与命局的作用）"",
    ""luckyMonths"": [旺运月份数字],
    ""challenges"": [""本年需要注意的事项1"", ""注意事项2""]
  },
  ""luckyElements"": [""喜用五行1"", ""喜用五行2""],
  ""luckyColors"": [""幸运颜色1"", ""幸运颜色2""],
  ""luckyNumbers"": [幸运数字1, 幸运数字2]
}";

    private string BuildBaziPrompt(BaziChartDto chart, string gender, int? analysisYear)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"性别：{(gender == "male" ? "男" : "女")}");
        sb.AppendLine($"\n命盘四柱：");
        sb.AppendLine($"年柱：{chart.YearPillar.Stem}{chart.YearPillar.Branch}（{chart.YearPillar.Element}）");
        sb.AppendLine($"月柱：{chart.MonthPillar.Stem}{chart.MonthPillar.Branch}（{chart.MonthPillar.Element}）");
        sb.AppendLine($"日柱：{chart.DayPillar.Stem}{chart.DayPillar.Branch}（{chart.DayPillar.Element}）- 日主");
        sb.AppendLine($"时柱：{chart.HourPillar.Stem}{chart.HourPillar.Branch}（{chart.HourPillar.Element}）");
        sb.AppendLine($"\n五行分布：");
        foreach (var (element, count) in chart.WuxingCount)
        {
            sb.AppendLine($"  {element}：{count}");
        }
        sb.AppendLine($"五行状态：{chart.WuxingBalance}");
        
        if (analysisYear.HasValue)
        {
            sb.AppendLine($"\n请重点分析{analysisYear}年的流年运势。");
        }

        sb.AppendLine("\n请给出详细的命理分析。");
        return sb.ToString();
    }

    private BaziReadingDto ParseBaziResponse(string aiResponse, BaziChartDto chart)
    {
        var reading = new BaziReadingDto { Chart = chart };

        try
        {
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = aiResponse[jsonStart..(jsonEnd + 1)];
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // 解析五行分析
                if (root.TryGetProperty("wuxingAnalysis", out var wuxingAnalysis))
                {
                    reading.WuxingAnalysis = wuxingAnalysis.GetString() ?? "";
                }

                if (root.TryGetProperty("personality", out var personality))
                {
                    reading.Personality = new PersonalityDto
                    {
                        Traits = GetStringArray(personality, "traits"),
                        Strengths = GetStringArray(personality, "strengths"),
                        Weaknesses = GetStringArray(personality, "weaknesses"),
                        Advice = personality.TryGetProperty("advice", out var adv) ? adv.GetString() ?? "" : ""
                    };
                }

                if (root.TryGetProperty("career", out var career))
                {
                    reading.Career = new CareerDto
                    {
                        SuitableFields = GetStringArray(career, "suitableFields"),
                        LuckyDirections = GetStringArray(career, "luckyDirections"),
                        Advice = career.TryGetProperty("advice", out var adv) ? adv.GetString() ?? "" : ""
                    };
                }

                if (root.TryGetProperty("relationship", out var relationship))
                {
                    reading.Relationship = new RelationshipDto
                    {
                        IdealPartner = relationship.TryGetProperty("idealPartner", out var ip) ? ip.GetString() ?? "" : "",
                        MarriageAge = relationship.TryGetProperty("marriageAge", out var ma) ? ma.GetString() : null,
                        Advice = relationship.TryGetProperty("advice", out var adv) ? adv.GetString() ?? "" : ""
                    };
                }

                if (root.TryGetProperty("wealth", out var wealth))
                {
                    reading.Wealth = new WealthDto
                    {
                        WealthType = wealth.TryGetProperty("wealthType", out var wt) ? wt.GetString() ?? "" : "",
                        LuckyYears = GetStringArray(wealth, "luckyYears"),
                        Advice = wealth.TryGetProperty("advice", out var adv) ? adv.GetString() ?? "" : ""
                    };
                }

                if (root.TryGetProperty("health", out var health))
                {
                    reading.Health = new HealthDto
                    {
                        WeakOrgans = GetStringArray(health, "weakOrgans"),
                        Advice = health.TryGetProperty("advice", out var adv) ? adv.GetString() ?? "" : ""
                    };
                }

                // 解析流年运势
                if (root.TryGetProperty("annualFortune", out var annualFortune))
                {
                    reading.AnnualFortune = new AnnualFortuneDto
                    {
                        Year = annualFortune.TryGetProperty("year", out var year) ? year.GetInt32() : DateTime.Now.Year,
                        Summary = annualFortune.TryGetProperty("summary", out var sum) ? sum.GetString() ?? "" : "",
                        LuckyMonths = annualFortune.TryGetProperty("luckyMonths", out var months) 
                            ? months.EnumerateArray().Select(x => x.GetInt32()).ToList() 
                            : new List<int>(),
                        Challenges = GetStringArray(annualFortune, "challenges")
                    };
                }

                reading.LuckyElements = GetStringArray(root, "luckyElements");
                reading.LuckyColors = GetStringArray(root, "luckyColors");
                
                if (root.TryGetProperty("luckyNumbers", out var nums))
                {
                    reading.LuckyNumbers = nums.EnumerateArray().Select(x => x.GetInt32()).ToList();
                }
            }
        }
        catch
        {
            // 使用默认值
        }

        return reading;
    }

    private List<string> GetStringArray(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var arr))
        {
            return arr.EnumerateArray()
                .Select(x => x.GetString() ?? "")
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
        }
        return new List<string>();
    }

    private string GetBaziChatSystemPrompt(MysticSession session)
    {
        return $@"你是一位八字命理大师，正在与用户讨论命理分析。

之前的命理分析：{session.AnalysisResult}

请基于之前的分析，回答用户的追问。保持专业神秘的语气，给出有深度的回答。";
    }

    #endregion

    #region 会话管理

    public async Task<MysticSessionListResponse> GetUserSessionsAsync(string userId, MysticType? type = null, int skip = 0, int take = 20)
    {
        try
        {
            var query = _dbContext.MysticSessions
                .Where(s => s.UserId == userId && !s.IsDeleted)
                .OrderByDescending(s => s.UpdatedAt);

            if (type.HasValue)
            {
                query = (IOrderedQueryable<MysticSession>)query.Where(s => s.Type == type.Value);
            }

            var totalCount = await query.CountAsync();
            var sessions = await query.Skip(skip).Take(take).ToListAsync();

            return new MysticSessionListResponse
            {
                Success = true,
                Sessions = sessions.Select(s => new MysticSessionDto
                {
                    Id = s.Id,
                    Type = s.Type.ToString().ToLower(),
                    Title = s.Title,
                    Question = s.Question,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                }).ToList(),
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取会话列表失败");
            return new MysticSessionListResponse { Success = false, Error = "获取失败" };
        }
    }

    public async Task<bool> DeleteSessionAsync(string sessionId, string userId)
    {
        try
        {
            var session = await _dbContext.MysticSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session == null) return false;

            session.IsDeleted = true;
            session.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除会话失败");
            return false;
        }
    }

    #endregion
}
