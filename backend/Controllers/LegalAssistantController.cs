using System.Security.Claims;
using AiServiceApi.Attributes;
using AiServiceApi.Models;
using AiServiceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiServiceApi.Controllers;

/// <summary>
/// 法律助手API控制器
/// </summary>
[Authorize]
[ApiController]
[Route("api/legal")]
[TrackUsage(FeatureModule.Legal)]
[CheckUsageQuota]
public class LegalAssistantController : ControllerBase
{
    private readonly ILegalAssistantService _legalAssistantService;
    private readonly ILogger<LegalAssistantController> _logger;

    public LegalAssistantController(
        ILegalAssistantService legalAssistantService,
        ILogger<LegalAssistantController> logger)
    {
        _legalAssistantService = legalAssistantService;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
               throw new UnauthorizedAccessException("用户ID不存在");
    }

    #region 案件管理

    /// <summary>
    /// 获取用户的案件列表
    /// </summary>
    [HttpGet("cases")]
    public async Task<IActionResult> GetCases([FromQuery] string? caseType = null)
    {
        try
        {
            var userId = GetUserId();
            LegalCaseType? type = null;
            
            if (!string.IsNullOrEmpty(caseType))
            {
                type = caseType.ToLower() switch
                {
                    "divorce" => LegalCaseType.Divorce,
                    "labor" => LegalCaseType.Labor,
                    "rental" => LegalCaseType.Rental,
                    _ => null
                };
            }

            var response = await _legalAssistantService.GetUserCasesAsync(userId, type);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "未授权访问" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取案件列表失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取案件详情
    /// </summary>
    [HttpGet("cases/{caseId}")]
    public async Task<IActionResult> GetCase(string caseId)
    {
        try
        {
            var userId = GetUserId();
            var response = await _legalAssistantService.GetCaseAsync(caseId, userId);
            
            if (!response.Success)
            {
                return NotFound(response);
            }
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "未授权访问" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取案件详情失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 创建新案件
    /// </summary>
    [HttpPost("cases")]
    public async Task<IActionResult> CreateCase([FromBody] CreateLegalCaseRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Title))
            {
                return BadRequest(new { error = "案件标题不能为空" });
            }

            if (string.IsNullOrEmpty(request.CaseType))
            {
                return BadRequest(new { error = "案件类型不能为空" });
            }

            var userId = GetUserId();
            var response = await _legalAssistantService.CreateCaseAsync(userId, request);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }
            
            return CreatedAtAction(nameof(GetCase), new { caseId = response.Case!.Id }, response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "未授权访问" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建案件失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 更新案件
    /// </summary>
    [HttpPut("cases/{caseId}")]
    public async Task<IActionResult> UpdateCase(string caseId, [FromBody] UpdateLegalCaseRequest request)
    {
        try
        {
            var userId = GetUserId();
            var response = await _legalAssistantService.UpdateCaseAsync(caseId, userId, request);
            
            if (!response.Success)
            {
                return NotFound(response);
            }
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "未授权访问" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新案件失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 删除案件
    /// </summary>
    [HttpDelete("cases/{caseId}")]
    public async Task<IActionResult> DeleteCase(string caseId)
    {
        try
        {
            var userId = GetUserId();
            var success = await _legalAssistantService.DeleteCaseAsync(caseId, userId);
            
            if (!success)
            {
                return NotFound(new { error = "案件不存在" });
            }
            
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "未授权访问" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除案件失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    #endregion

    #region 证据管理

    /// <summary>
    /// 添加证据
    /// </summary>
    [HttpPost("cases/{caseId}/evidence")]
    public async Task<IActionResult> AddEvidence(string caseId, [FromBody] AddEvidenceRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                return BadRequest(new { error = "证据名称不能为空" });
            }

            if (string.IsNullOrEmpty(request.Type))
            {
                return BadRequest(new { error = "证据类型不能为空" });
            }

            var userId = GetUserId();
            var response = await _legalAssistantService.AddEvidenceAsync(caseId, userId, request);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "未授权访问" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加证据失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 删除证据
    /// </summary>
    [HttpDelete("evidence/{evidenceId}")]
    public async Task<IActionResult> DeleteEvidence(string evidenceId)
    {
        try
        {
            var userId = GetUserId();
            var success = await _legalAssistantService.DeleteEvidenceAsync(evidenceId, userId);
            
            if (!success)
            {
                return NotFound(new { error = "证据不存在" });
            }
            
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "未授权访问" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除证据失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 转写音频证据
    /// </summary>
    [HttpPost("evidence/{evidenceId}/transcribe")]
    public async Task<IActionResult> TranscribeEvidence(string evidenceId)
    {
        try
        {
            var userId = GetUserId();
            var response = await _legalAssistantService.TranscribeEvidenceAsync(evidenceId, userId);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "未授权访问" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "转写证据失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    #endregion

    #region AI分析

    /// <summary>
    /// AI分析案件
    /// </summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeCase([FromBody] LegalAnalysisRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CaseId))
            {
                return BadRequest(new { error = "案件ID不能为空" });
            }

            var userId = GetUserId();
            var response = await _legalAssistantService.AnalyzeCaseAsync(userId, request);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "未授权访问" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI分析失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 生成法律文书
    /// </summary>
    [HttpPost("generate-document")]
    public async Task<IActionResult> GenerateDocument([FromBody] LegalAnalysisRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CaseId))
            {
                return BadRequest(new { error = "案件ID不能为空" });
            }

            var userId = GetUserId();
            var response = await _legalAssistantService.GenerateDocumentAsync(userId, request);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "未授权访问" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成文书失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 直接AI分析（无需保存案件）
    /// </summary>
    [HttpPost("analyze-direct")]
    public async Task<IActionResult> AnalyzeDirect([FromBody] LegalDirectAnalysisRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CaseType))
            {
                return BadRequest(new { error = "案件类型不能为空" });
            }

            var response = await _legalAssistantService.AnalyzeDirectAsync(request);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "未授权访问" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "直接分析失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 直接生成法律文书（无需保存案件）
    /// </summary>
    [HttpPost("generate-document-direct")]
    public async Task<IActionResult> GenerateDocumentDirect([FromBody] LegalDirectDocumentRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.CaseType))
            {
                return BadRequest(new { error = "案件类型不能为空" });
            }

            var response = await _legalAssistantService.GenerateDocumentDirectAsync(request);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "未授权访问" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "直接生成文书失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    #endregion
}
