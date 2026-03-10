using System.Text;
using System.Text.Json;
using AiServiceApi.Data;
using AiServiceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AiServiceApi.Services;

/// <summary>
/// 法律助手服务接口
/// </summary>
public interface ILegalAssistantService
{
    // 案件管理
    Task<LegalCaseListResponse> GetUserCasesAsync(string userId, LegalCaseType? caseType = null);
    Task<LegalCaseResponse> GetCaseAsync(string caseId, string userId);
    Task<LegalCaseResponse> CreateCaseAsync(string userId, CreateLegalCaseRequest request);
    Task<LegalCaseResponse> UpdateCaseAsync(string caseId, string userId, UpdateLegalCaseRequest request);
    Task<bool> DeleteCaseAsync(string caseId, string userId);
    
    // 证据管理
    Task<LegalEvidenceResponse> AddEvidenceAsync(string caseId, string userId, AddEvidenceRequest request);
    Task<bool> DeleteEvidenceAsync(string evidenceId, string userId);
    Task<LegalEvidenceResponse> TranscribeEvidenceAsync(string evidenceId, string userId);
    
    // AI分析
    Task<LegalAnalysisResponse> AnalyzeCaseAsync(string userId, LegalAnalysisRequest request);
    Task<LegalAnalysisResponse> GenerateDocumentAsync(string userId, LegalAnalysisRequest request);
    
    // 直接分析（无需保存案件）
    Task<LegalAnalysisResponse> AnalyzeDirectAsync(LegalDirectAnalysisRequest request);
    Task<LegalAnalysisResponse> GenerateDocumentDirectAsync(LegalDirectDocumentRequest request);
}

/// <summary>
/// 法律助手服务实现
/// </summary>
public class LegalAssistantService : ILegalAssistantService
{
    private readonly AppDbContext _dbContext;
    private readonly IAzureAIService _aiService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LegalAssistantService> _logger;
    private readonly HttpClient _httpClient;

    public LegalAssistantService(
        AppDbContext dbContext,
        IAzureAIService aiService,
        IConfiguration configuration,
        ILogger<LegalAssistantService> logger,
        HttpClient httpClient)
    {
        _dbContext = dbContext;
        _aiService = aiService;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    #region 案件管理

    public async Task<LegalCaseListResponse> GetUserCasesAsync(string userId, LegalCaseType? caseType = null)
    {
        try
        {
            var query = _dbContext.LegalCases
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .Include(c => c.Evidences)
                .OrderByDescending(c => c.UpdatedAt);

            if (caseType.HasValue)
            {
                query = (IOrderedQueryable<LegalCase>)query.Where(c => c.CaseType == caseType.Value);
            }

            var cases = await query.ToListAsync();

            return new LegalCaseListResponse
            {
                Success = true,
                Cases = cases.Select(MapToDto).ToList(),
                TotalCount = cases.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户案件列表失败");
            return new LegalCaseListResponse
            {
                Success = false,
                Error = "获取案件列表失败"
            };
        }
    }

    public async Task<LegalCaseResponse> GetCaseAsync(string caseId, string userId)
    {
        try
        {
            var legalCase = await _dbContext.LegalCases
                .Include(c => c.Evidences)
                .FirstOrDefaultAsync(c => c.Id == caseId && c.UserId == userId && !c.IsDeleted);

            if (legalCase == null)
            {
                return new LegalCaseResponse
                {
                    Success = false,
                    Error = "案件不存在"
                };
            }

            return new LegalCaseResponse
            {
                Success = true,
                Case = MapToDto(legalCase)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取案件详情失败");
            return new LegalCaseResponse
            {
                Success = false,
                Error = "获取案件详情失败"
            };
        }
    }

    public async Task<LegalCaseResponse> CreateCaseAsync(string userId, CreateLegalCaseRequest request)
    {
        try
        {
            var caseType = request.CaseType.ToLower() switch
            {
                "divorce" => LegalCaseType.Divorce,
                "labor" => LegalCaseType.Labor,
                "rental" => LegalCaseType.Rental,
                _ => throw new ArgumentException($"无效的案件类型: {request.CaseType}")
            };

            var legalCase = new LegalCase
            {
                UserId = userId,
                CaseType = caseType,
                Title = request.Title,
                Description = request.Description,
                CaseData = request.CaseData.HasValue 
                    ? request.CaseData.Value.GetRawText() 
                    : "{}",
                Status = LegalCaseStatus.Draft
            };

            _dbContext.LegalCases.Add(legalCase);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("用户 {UserId} 创建了新案件 {CaseId}", userId, legalCase.Id);

            return new LegalCaseResponse
            {
                Success = true,
                Case = MapToDto(legalCase)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建案件失败");
            return new LegalCaseResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<LegalCaseResponse> UpdateCaseAsync(string caseId, string userId, UpdateLegalCaseRequest request)
    {
        try
        {
            var legalCase = await _dbContext.LegalCases
                .Include(c => c.Evidences)
                .FirstOrDefaultAsync(c => c.Id == caseId && c.UserId == userId && !c.IsDeleted);

            if (legalCase == null)
            {
                return new LegalCaseResponse
                {
                    Success = false,
                    Error = "案件不存在"
                };
            }

            if (!string.IsNullOrEmpty(request.Title))
                legalCase.Title = request.Title;

            if (request.Description != null)
                legalCase.Description = request.Description;

            if (!string.IsNullOrEmpty(request.Status))
            {
                legalCase.Status = request.Status.ToLower() switch
                {
                    "draft" => LegalCaseStatus.Draft,
                    "analyzing" => LegalCaseStatus.Analyzing,
                    "completed" => LegalCaseStatus.Completed,
                    "archived" => LegalCaseStatus.Archived,
                    _ => legalCase.Status
                };
            }

            if (request.CaseData.HasValue)
                legalCase.CaseData = request.CaseData.Value.GetRawText();

            if (request.AnalysisResult.HasValue)
                legalCase.AnalysisResult = request.AnalysisResult.Value.GetRawText();

            legalCase.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return new LegalCaseResponse
            {
                Success = true,
                Case = MapToDto(legalCase)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新案件失败");
            return new LegalCaseResponse
            {
                Success = false,
                Error = "更新案件失败"
            };
        }
    }

    public async Task<bool> DeleteCaseAsync(string caseId, string userId)
    {
        try
        {
            var legalCase = await _dbContext.LegalCases
                .FirstOrDefaultAsync(c => c.Id == caseId && c.UserId == userId && !c.IsDeleted);

            if (legalCase == null)
                return false;

            // 软删除
            legalCase.IsDeleted = true;
            legalCase.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("用户 {UserId} 删除了案件 {CaseId}", userId, caseId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除案件失败");
            return false;
        }
    }

    #endregion

    #region 证据管理

    public async Task<LegalEvidenceResponse> AddEvidenceAsync(string caseId, string userId, AddEvidenceRequest request)
    {
        try
        {
            var legalCase = await _dbContext.LegalCases
                .FirstOrDefaultAsync(c => c.Id == caseId && c.UserId == userId && !c.IsDeleted);

            if (legalCase == null)
            {
                return new LegalEvidenceResponse
                {
                    Success = false,
                    Error = "案件不存在"
                };
            }

            var evidenceType = request.Type.ToLower() switch
            {
                "audio" => EvidenceType.Audio,
                "image" => EvidenceType.Image,
                "document" => EvidenceType.Document,
                "contract" => EvidenceType.Contract,
                "chat" => EvidenceType.Chat,
                "bank" => EvidenceType.Bank,
                "property" => EvidenceType.Property,
                _ => EvidenceType.Other
            };

            var evidence = new LegalEvidence
            {
                CaseId = caseId,
                Type = evidenceType,
                Name = request.Name,
                Description = request.Description,
                MimeType = request.MimeType
            };

            // 如果有文件数据，可以存储到云端（这里暂时存储为空，后续可以集成Azure Blob Storage）
            if (!string.IsNullOrEmpty(request.FileData))
            {
                // TODO: 上传到Azure Blob Storage并获取URL
                // evidence.FileUrl = await UploadToBlob(request.FileData, request.MimeType);
                evidence.FileSize = Convert.FromBase64String(request.FileData).Length;
            }

            _dbContext.LegalEvidences.Add(evidence);
            
            // 更新案件时间
            legalCase.UpdatedAt = DateTime.UtcNow;
            
            await _dbContext.SaveChangesAsync();

            return new LegalEvidenceResponse
            {
                Success = true,
                Evidence = MapEvidenceToDto(evidence)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加证据失败");
            return new LegalEvidenceResponse
            {
                Success = false,
                Error = "添加证据失败"
            };
        }
    }

    public async Task<bool> DeleteEvidenceAsync(string evidenceId, string userId)
    {
        try
        {
            var evidence = await _dbContext.LegalEvidences
                .Include(e => e.Case)
                .FirstOrDefaultAsync(e => e.Id == evidenceId && e.Case != null && e.Case.UserId == userId);

            if (evidence == null)
                return false;

            _dbContext.LegalEvidences.Remove(evidence);
            
            // 更新案件时间
            if (evidence.Case != null)
            {
                evidence.Case.UpdatedAt = DateTime.UtcNow;
            }
            
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除证据失败");
            return false;
        }
    }

    public async Task<LegalEvidenceResponse> TranscribeEvidenceAsync(string evidenceId, string userId)
    {
        try
        {
            var evidence = await _dbContext.LegalEvidences
                .Include(e => e.Case)
                .FirstOrDefaultAsync(e => e.Id == evidenceId && e.Case != null && e.Case.UserId == userId);

            if (evidence == null)
            {
                return new LegalEvidenceResponse
                {
                    Success = false,
                    Error = "证据不存在"
                };
            }

            if (evidence.Type != EvidenceType.Audio)
            {
                return new LegalEvidenceResponse
                {
                    Success = false,
                    Error = "只有音频证据可以转写"
                };
            }

            // TODO: 调用Azure Speech Service进行转写
            // evidence.Transcript = await TranscribeAudio(evidence.FileUrl);
            evidence.Transcript = "（转写功能开发中）";

            await _dbContext.SaveChangesAsync();

            return new LegalEvidenceResponse
            {
                Success = true,
                Evidence = MapEvidenceToDto(evidence)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "转写证据失败");
            return new LegalEvidenceResponse
            {
                Success = false,
                Error = "转写失败"
            };
        }
    }

    #endregion

    #region AI分析

    public async Task<LegalAnalysisResponse> AnalyzeCaseAsync(string userId, LegalAnalysisRequest request)
    {
        try
        {
            var legalCase = await _dbContext.LegalCases
                .Include(c => c.Evidences)
                .FirstOrDefaultAsync(c => c.Id == request.CaseId && c.UserId == userId && !c.IsDeleted);

            if (legalCase == null)
            {
                return new LegalAnalysisResponse
                {
                    Success = false,
                    Error = "案件不存在"
                };
            }

            // 更新状态为分析中
            legalCase.Status = LegalCaseStatus.Analyzing;
            await _dbContext.SaveChangesAsync();

            // 构建分析Prompt
            var systemPrompt = GetSystemPrompt(legalCase.CaseType);
            var userPrompt = BuildAnalysisPrompt(legalCase);

            // 调用AI服务
            var chatRequest = new ChatRequest
            {
                Messages = new List<ChatMessage>
                {
                    new() { Role = "system", Content = new List<MessageContent> { new() { Type = "text", Text = systemPrompt } } },
                    new() { Role = "user", Content = new List<MessageContent> { new() { Type = "text", Text = userPrompt } } }
                },
                Temperature = 0.3f,
                MaxTokens = 4000
            };

            var response = await _aiService.SendChatMessageAsync(chatRequest);

            if (!response.Success || string.IsNullOrEmpty(response.Message))
            {
                legalCase.Status = LegalCaseStatus.Draft;
                await _dbContext.SaveChangesAsync();
                
                return new LegalAnalysisResponse
                {
                    Success = false,
                    Error = response.Error ?? "AI分析失败"
                };
            }

            // 解析AI响应
            var analysis = ParseAnalysisResponse(response.Message);

            // 保存分析结果
            legalCase.AnalysisResult = JsonSerializer.Serialize(analysis);
            legalCase.Status = LegalCaseStatus.Completed;
            legalCase.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return new LegalAnalysisResponse
            {
                Success = true,
                Analysis = JsonDocument.Parse(legalCase.AnalysisResult).RootElement
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI分析案件失败");
            return new LegalAnalysisResponse
            {
                Success = false,
                Error = "分析失败，请稍后重试"
            };
        }
    }

    public async Task<LegalAnalysisResponse> GenerateDocumentAsync(string userId, LegalAnalysisRequest request)
    {
        try
        {
            var legalCase = await _dbContext.LegalCases
                .Include(c => c.Evidences)
                .FirstOrDefaultAsync(c => c.Id == request.CaseId && c.UserId == userId && !c.IsDeleted);

            if (legalCase == null)
            {
                return new LegalAnalysisResponse
                {
                    Success = false,
                    Error = "案件不存在"
                };
            }

            // 构建文书生成Prompt
            var systemPrompt = GetSystemPrompt(legalCase.CaseType);
            var casePrompt = BuildAnalysisPrompt(legalCase);
            var docPrompt = request.DocumentType?.ToLower() switch
            {
                "application" => "请根据以上案件信息，生成一份正式的仲裁申请书/起诉状，格式规范，内容完整。",
                "letter" => "请根据以上案件信息，生成一份正式的律师函/催告函，语气专业，有理有据。",
                "complaint" => "请根据以上案件信息，生成一份向相关部门的投诉信，内容详实，诉求明确。",
                _ => "请根据以上案件信息，生成相关法律文书。"
            };

            var chatRequest = new ChatRequest
            {
                Messages = new List<ChatMessage>
                {
                    new() { Role = "system", Content = new List<MessageContent> { new() { Type = "text", Text = systemPrompt } } },
                    new() { Role = "user", Content = new List<MessageContent> { new() { Type = "text", Text = casePrompt } } },
                    new() { Role = "user", Content = new List<MessageContent> { new() { Type = "text", Text = docPrompt } } }
                },
                Temperature = 0.2f,
                MaxTokens = 3000
            };

            var response = await _aiService.SendChatMessageAsync(chatRequest);

            if (!response.Success || string.IsNullOrEmpty(response.Message))
            {
                return new LegalAnalysisResponse
                {
                    Success = false,
                    Error = response.Error ?? "文书生成失败"
                };
            }

            return new LegalAnalysisResponse
            {
                Success = true,
                Document = response.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成法律文书失败");
            return new LegalAnalysisResponse
            {
                Success = false,
                Error = "文书生成失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 直接分析案件数据（无需保存到数据库）
    /// </summary>
    public async Task<LegalAnalysisResponse> AnalyzeDirectAsync(LegalDirectAnalysisRequest request)
    {
        try
        {
            // 解析案件类型
            var caseType = request.CaseType?.ToLower() switch
            {
                "divorce" => LegalCaseType.Divorce,
                "labor" => LegalCaseType.Labor,
                "rental" => LegalCaseType.Rental,
                _ => (LegalCaseType?)null
            };

            if (caseType == null)
            {
                return new LegalAnalysisResponse
                {
                    Success = false,
                    Error = "不支持的案件类型"
                };
            }

            // 获取系统提示词
            var systemPrompt = !string.IsNullOrEmpty(request.SystemPrompt) 
                ? request.SystemPrompt 
                : GetSystemPrompt(caseType.Value);

            // 构建用户提示词
            var userPrompt = !string.IsNullOrEmpty(request.UserPrompt)
                ? request.UserPrompt
                : BuildDirectAnalysisPrompt(caseType.Value, request.CaseData);

            // 调用AI服务
            var chatRequest = new ChatRequest
            {
                Messages = new List<ChatMessage>
                {
                    new() { Role = "system", Content = new List<MessageContent> { new() { Type = "text", Text = systemPrompt } } },
                    new() { Role = "user", Content = new List<MessageContent> { new() { Type = "text", Text = userPrompt } } }
                },
                Temperature = 0.3f,
                MaxTokens = 4000
            };

            var response = await _aiService.SendChatMessageAsync(chatRequest);

            if (!response.Success || string.IsNullOrEmpty(response.Message))
            {
                return new LegalAnalysisResponse
                {
                    Success = false,
                    Error = response.Error ?? "AI分析失败"
                };
            }

            return new LegalAnalysisResponse
            {
                Success = true,
                Message = response.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "直接分析案件失败");
            return new LegalAnalysisResponse
            {
                Success = false,
                Error = "分析失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 直接生成法律文书（无需保存案件）
    /// </summary>
    public async Task<LegalAnalysisResponse> GenerateDocumentDirectAsync(LegalDirectDocumentRequest request)
    {
        try
        {
            // 解析案件类型
            var caseType = request.CaseType?.ToLower() switch
            {
                "divorce" => LegalCaseType.Divorce,
                "labor" => LegalCaseType.Labor,
                "rental" => LegalCaseType.Rental,
                _ => (LegalCaseType?)null
            };

            if (caseType == null)
            {
                return new LegalAnalysisResponse
                {
                    Success = false,
                    Error = "不支持的案件类型"
                };
            }

            // 获取系统提示词
            var systemPrompt = GetSystemPrompt(caseType.Value);

            // 构建案件提示词
            var casePrompt = BuildDirectAnalysisPrompt(caseType.Value, request.CaseData);

            // 文书类型提示
            var docPrompt = request.DocumentType?.ToLower() switch
            {
                "application" => "请根据以上案件信息，生成一份正式的仲裁申请书/起诉状，格式规范，内容完整。",
                "letter" => "请根据以上案件信息，生成一份正式的律师函/催告函，语气专业，有理有据。",
                "complaint" => "请根据以上案件信息，生成一份向相关部门的投诉信，内容详实，诉求明确。",
                _ => "请根据以上案件信息，生成相关法律文书。"
            };

            var chatRequest = new ChatRequest
            {
                Messages = new List<ChatMessage>
                {
                    new() { Role = "system", Content = new List<MessageContent> { new() { Type = "text", Text = systemPrompt } } },
                    new() { Role = "user", Content = new List<MessageContent> { new() { Type = "text", Text = casePrompt } } },
                    new() { Role = "user", Content = new List<MessageContent> { new() { Type = "text", Text = docPrompt } } }
                },
                Temperature = 0.2f,
                MaxTokens = 3000
            };

            var response = await _aiService.SendChatMessageAsync(chatRequest);

            if (!response.Success || string.IsNullOrEmpty(response.Message))
            {
                return new LegalAnalysisResponse
                {
                    Success = false,
                    Error = response.Error ?? "文书生成失败"
                };
            }

            return new LegalAnalysisResponse
            {
                Success = true,
                Document = response.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "直接生成文书失败");
            return new LegalAnalysisResponse
            {
                Success = false,
                Error = "文书生成失败，请稍后重试"
            };
        }
    }

    private string BuildDirectAnalysisPrompt(LegalCaseType caseType, JsonElement? caseData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("请分析以下案件：");
        sb.AppendLine();
        sb.AppendLine($"## 案件类型：{GetCaseTypeName(caseType)}");
        sb.AppendLine();
        sb.AppendLine("## 案件详细信息：");
        
        if (caseData.HasValue)
        {
            sb.AppendLine(caseData.Value.GetRawText());
        }
        else
        {
            sb.AppendLine("（未提供案件数据）");
        }

        sb.AppendLine();
        sb.AppendLine("请提供详细的分析结果，包括：");
        sb.AppendLine("1. 法律定性分析");
        sb.AppendLine("2. 权益评估");
        sb.AppendLine("3. 建议收集的证据");
        sb.AppendLine("4. 维权建议");
        sb.AppendLine("5. 风险提示");
        
        return sb.ToString();
    }

    #endregion

    #region 私有方法

    private string GetSystemPrompt(LegalCaseType caseType)
    {
        return caseType switch
        {
            LegalCaseType.Divorce => @"你是一位专业的婚姻家事律师，精通中国婚姻法和最新的民法典相关规定。
你的任务是帮助用户分析离婚财产分割情况，提供专业、客观的法律建议。

重要法律依据：
- 《民法典》婚姻家庭编（2021年施行）
- 夫妻共同财产：婚后所得原则上均为共同财产
- 个人财产：婚前财产、遗嘱/赠与明确归一方的财产
- 子女抚养费：根据当地生活水平和支付方计算能力确定

请始终：
1. 给出具体的法律条文引用
2. 计算要精确，说明计算依据
3. 指出潜在风险和注意事项
4. 建议用户收集的关键证据",

            LegalCaseType.Labor => @"你是一位专业的劳动法律师，精通中国劳动法、劳动合同法和相关司法解释。
你的任务是帮助劳动者分析其权益受损情况，计算应得赔偿，并提供维权指导。

重要法律依据：
- 《劳动合同法》（2012年修正）
- 《劳动争议调解仲裁法》
- 未签合同双倍工资：最多11个月
- 违法解除赔偿金：2N（N为工作年限每满一年算1个月工资）
- 经济补偿金：N 或 N+1
- 加班费：平日1.5倍、周末2倍、法定节假日3倍

请始终：
1. 精确计算各项赔偿金额
2. 列出完整的证据清单
3. 提供仲裁申请的关键时间节点
4. 指出对方可能的抗辩点",

            LegalCaseType.Rental => @"你是一位专业的房产律师，精通中国合同法、民法典租赁合同编和各地租房管理条例。
你的任务是帮助租客分析租房纠纷，评估维权可能性，并提供具体的维权方案。

重要法律依据：
- 《民法典》合同编-租赁合同章节
- 《商品房屋租赁管理办法》
- 各地住房租赁条例
- 押金退还：无特别约定时，合同到期后房东应全额退还
- 违约责任：看合同约定，无约定按实际损失

请始终：
1. 分析合同条款的合法性
2. 评估胜诉概率
3. 提供多种维权途径（协商、投诉、诉讼）
4. 草拟维权文书",

            _ => "你是一位专业的法律顾问，请根据用户提供的信息给出专业的法律建议。"
        };
    }

    private string BuildAnalysisPrompt(LegalCase legalCase)
    {
        var sb = new StringBuilder();
        sb.AppendLine("请分析以下案件：");
        sb.AppendLine();
        sb.AppendLine($"## 案件类型：{GetCaseTypeName(legalCase.CaseType)}");
        sb.AppendLine($"## 案件标题：{legalCase.Title}");
        
        if (!string.IsNullOrEmpty(legalCase.Description))
        {
            sb.AppendLine($"## 案件描述：{legalCase.Description}");
        }
        
        sb.AppendLine();
        sb.AppendLine("## 案件详细信息：");
        sb.AppendLine(legalCase.CaseData);
        
        if (legalCase.Evidences.Any())
        {
            sb.AppendLine();
            sb.AppendLine("## 已收集证据：");
            foreach (var evidence in legalCase.Evidences)
            {
                sb.AppendLine($"- {evidence.Name}（{GetEvidenceTypeName(evidence.Type)}）");
                if (!string.IsNullOrEmpty(evidence.Transcript))
                {
                    sb.AppendLine($"  转写内容：{evidence.Transcript.Substring(0, Math.Min(200, evidence.Transcript.Length))}...");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("请提供详细的分析结果，以JSON格式返回。");
        
        return sb.ToString();
    }

    private object ParseAnalysisResponse(string response)
    {
        try
        {
            // 尝试从响应中提取JSON
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(response, @"\{[\s\S]*\}");
            if (jsonMatch.Success)
            {
                return JsonSerializer.Deserialize<JsonElement>(jsonMatch.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析AI分析结果失败，返回原始文本");
        }
        
        // 如果无法解析JSON，返回包含原始文本的对象
        return new { rawAnalysis = response };
    }

    private string GetCaseTypeName(LegalCaseType caseType) => caseType switch
    {
        LegalCaseType.Divorce => "离婚财产分析",
        LegalCaseType.Labor => "劳动仲裁",
        LegalCaseType.Rental => "租房纠纷",
        _ => "未知类型"
    };

    private string GetEvidenceTypeName(EvidenceType type) => type switch
    {
        EvidenceType.Audio => "录音",
        EvidenceType.Image => "图片",
        EvidenceType.Document => "文档",
        EvidenceType.Contract => "合同",
        EvidenceType.Chat => "聊天记录",
        EvidenceType.Bank => "银行流水",
        EvidenceType.Property => "房产证明",
        _ => "其他"
    };

    private LegalCaseDto MapToDto(LegalCase legalCase)
    {
        JsonElement? caseData = null;
        JsonElement? analysisResult = null;

        try
        {
            if (!string.IsNullOrEmpty(legalCase.CaseData))
                caseData = JsonDocument.Parse(legalCase.CaseData).RootElement;
            if (!string.IsNullOrEmpty(legalCase.AnalysisResult))
                analysisResult = JsonDocument.Parse(legalCase.AnalysisResult).RootElement;
        }
        catch { }

        return new LegalCaseDto
        {
            Id = legalCase.Id,
            CaseType = legalCase.CaseType.ToString().ToLower(),
            Status = legalCase.Status.ToString().ToLower(),
            Title = legalCase.Title,
            Description = legalCase.Description,
            CaseData = caseData,
            AnalysisResult = analysisResult,
            Evidences = legalCase.Evidences.Select(MapEvidenceToDto).ToList(),
            CreatedAt = legalCase.CreatedAt,
            UpdatedAt = legalCase.UpdatedAt
        };
    }

    private LegalEvidenceDto MapEvidenceToDto(LegalEvidence evidence)
    {
        return new LegalEvidenceDto
        {
            Id = evidence.Id,
            Type = evidence.Type.ToString().ToLower(),
            Name = evidence.Name,
            Description = evidence.Description,
            FileUrl = evidence.FileUrl,
            Transcript = evidence.Transcript,
            ExtractedInfo = evidence.ExtractedInfo,
            CreatedAt = evidence.CreatedAt
        };
    }

    #endregion
}
