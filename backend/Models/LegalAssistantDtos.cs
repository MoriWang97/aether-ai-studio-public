using System.Text.Json;

namespace AiServiceApi.Models;

// ========== 请求模型 ==========

/// <summary>
/// 创建案件请求
/// </summary>
public class CreateLegalCaseRequest
{
    /// <summary>
    /// 案件类型：divorce, labor, rental
    /// </summary>
    public string CaseType { get; set; } = string.Empty;
    
    /// <summary>
    /// 案件标题
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 案件描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 案件具体数据
    /// </summary>
    public JsonElement? CaseData { get; set; }
}

/// <summary>
/// 更新案件请求
/// </summary>
public class UpdateLegalCaseRequest
{
    /// <summary>
    /// 案件标题
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// 案件描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 案件状态
    /// </summary>
    public string? Status { get; set; }
    
    /// <summary>
    /// 案件具体数据
    /// </summary>
    public JsonElement? CaseData { get; set; }
    
    /// <summary>
    /// AI分析结果
    /// </summary>
    public JsonElement? AnalysisResult { get; set; }
}

/// <summary>
/// 添加证据请求
/// </summary>
public class AddEvidenceRequest
{
    /// <summary>
    /// 证据类型
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// 证据名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 证据描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 文件数据（Base64）
    /// </summary>
    public string? FileData { get; set; }
    
    /// <summary>
    /// 文件MIME类型
    /// </summary>
    public string? MimeType { get; set; }
}

/// <summary>
/// AI分析请求
/// </summary>
public class LegalAnalysisRequest
{
    /// <summary>
    /// 案件ID
    /// </summary>
    public string CaseId { get; set; } = string.Empty;
    
    /// <summary>
    /// 分析类型：full（完整分析）, quick（快速分析）, evidence（证据分析）, document（文书生成）
    /// </summary>
    public string AnalysisType { get; set; } = "full";
    
    /// <summary>
    /// 文书类型（当AnalysisType为document时）：application（仲裁申请书）, letter（律师函）, complaint（投诉信）
    /// </summary>
    public string? DocumentType { get; set; }
}

/// <summary>
/// 直接AI分析请求（无需保存案件）
/// </summary>
public class LegalDirectAnalysisRequest
{
    /// <summary>
    /// 案件类型：divorce, labor, rental
    /// </summary>
    public string CaseType { get; set; } = string.Empty;
    
    /// <summary>
    /// 案件数据（JSON格式）
    /// </summary>
    public JsonElement? CaseData { get; set; }
    
    /// <summary>
    /// 系统提示词（可选，不传则使用默认）
    /// </summary>
    public string? SystemPrompt { get; set; }
    
    /// <summary>
    /// 用户提示词（可选，不传则自动构建）
    /// </summary>
    public string? UserPrompt { get; set; }
}

/// <summary>
/// 直接文书生成请求
/// </summary>
public class LegalDirectDocumentRequest
{
    /// <summary>
    /// 案件类型：divorce, labor, rental
    /// </summary>
    public string CaseType { get; set; } = string.Empty;
    
    /// <summary>
    /// 案件数据（JSON格式）
    /// </summary>
    public JsonElement? CaseData { get; set; }
    
    /// <summary>
    /// 文书类型：application（申请书）, letter（律师函）, complaint（投诉信）
    /// </summary>
    public string DocumentType { get; set; } = "application";
}

/// <summary>
/// 证据转写请求
/// </summary>
public class TranscribeEvidenceRequest
{
    /// <summary>
    /// 证据ID
    /// </summary>
    public string EvidenceId { get; set; } = string.Empty;
}

// ========== 响应模型 ==========

/// <summary>
/// 法律案件DTO
/// </summary>
public class LegalCaseDto
{
    public string Id { get; set; } = string.Empty;
    public string CaseType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public JsonElement? CaseData { get; set; }
    public JsonElement? AnalysisResult { get; set; }
    public List<LegalEvidenceDto> Evidences { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 法律证据DTO
/// </summary>
public class LegalEvidenceDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FileUrl { get; set; }
    public string? Transcript { get; set; }
    public string? ExtractedInfo { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 案件列表响应
/// </summary>
public class LegalCaseListResponse
{
    public bool Success { get; set; }
    public List<LegalCaseDto> Cases { get; set; } = new();
    public int TotalCount { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 案件详情响应
/// </summary>
public class LegalCaseResponse
{
    public bool Success { get; set; }
    public LegalCaseDto? Case { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// AI分析响应
/// </summary>
public class LegalAnalysisResponse
{
    public bool Success { get; set; }
    public JsonElement? Analysis { get; set; }
    public string? Document { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 证据操作响应
/// </summary>
public class LegalEvidenceResponse
{
    public bool Success { get; set; }
    public LegalEvidenceDto? Evidence { get; set; }
    public string? Error { get; set; }
}
