using System.Security.Claims;
using AiServiceApi.Attributes;
using AiServiceApi.Models;
using AiServiceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiServiceApi.Controllers;

/// <summary>
/// 玄学助手API控制器
/// 提供塔罗牌、星座运势、八字命理等分析服务
/// </summary>
[Authorize]
[ApiController]
[Route("api/mystic")]
[TrackUsage(FeatureModule.Mystic)]
[CheckUsageQuota]
public class MysticController : ControllerBase
{
    private readonly IMysticAssistantService _mysticService;
    private readonly ILogger<MysticController> _logger;

    public MysticController(
        IMysticAssistantService mysticService,
        ILogger<MysticController> logger)
    {
        _mysticService = mysticService;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
               throw new UnauthorizedAccessException("用户ID不存在");
    }

    #region 塔罗牌

    /// <summary>
    /// 塔罗牌占卜分析
    /// </summary>
    /// <param name="request">占卜请求，包含牌阵类型和问题</param>
    /// <returns>塔罗牌解读结果</returns>
    [HttpPost("tarot/analyze")]
    public async Task<IActionResult> AnalyzeTarot([FromBody] TarotAnalysisRequest request)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("用户 {UserId} 请求塔罗牌占卜，牌阵: {SpreadType}", userId, request.SpreadType);

            var response = await _mysticService.AnalyzeTarotAsync(userId, request);

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
            _logger.LogError(ex, "塔罗牌占卜失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 塔罗牌后续对话
    /// </summary>
    /// <param name="request">对话请求</param>
    /// <returns>AI回复</returns>
    [HttpPost("tarot/chat")]
    public async Task<IActionResult> ChatTarot([FromBody] TarotChatRequest request)
    {
        try
        {
            var userId = GetUserId();
            var response = await _mysticService.ChatTarotAsync(userId, request);

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
            _logger.LogError(ex, "塔罗牌对话失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    #endregion

    #region 星座运势

    /// <summary>
    /// 星座运势分析
    /// </summary>
    /// <param name="request">星座和周期</param>
    /// <returns>运势分析结果</returns>
    [HttpPost("astrology/analyze")]
    public async Task<IActionResult> AnalyzeAstrology([FromBody] AstrologyAnalysisRequest request)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("用户 {UserId} 请求星座运势，星座: {Sign}, 周期: {Period}", 
                userId, request.Sign, request.Period);

            var response = await _mysticService.AnalyzeAstrologyAsync(userId, request);

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
            _logger.LogError(ex, "星座运势分析失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 星座后续对话
    /// </summary>
    /// <param name="request">对话请求</param>
    /// <returns>AI回复</returns>
    [HttpPost("astrology/chat")]
    public async Task<IActionResult> ChatAstrology([FromBody] AstrologyChatRequest request)
    {
        try
        {
            var userId = GetUserId();
            var response = await _mysticService.ChatAstrologyAsync(userId, request);

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
            _logger.LogError(ex, "星座对话失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    #endregion

    #region 八字命理

    /// <summary>
    /// 八字命理分析
    /// </summary>
    /// <param name="request">出生时间和地点</param>
    /// <returns>命理分析结果</returns>
    [HttpPost("bazi/analyze")]
    public async Task<IActionResult> AnalyzeBazi([FromBody] BaziAnalysisRequest request)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("用户 {UserId} 请求八字命理分析", userId);

            var response = await _mysticService.AnalyzeBaziAsync(userId, request);

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
            _logger.LogError(ex, "八字命理分析失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 八字后续对话
    /// </summary>
    /// <param name="request">对话请求</param>
    /// <returns>AI回复</returns>
    [HttpPost("bazi/chat")]
    public async Task<IActionResult> ChatBazi([FromBody] BaziChatRequest request)
    {
        try
        {
            var userId = GetUserId();
            var response = await _mysticService.ChatBaziAsync(userId, request);

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
            _logger.LogError(ex, "八字对话失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    #endregion

    #region 会话管理

    /// <summary>
    /// 获取用户的玄学分析会话列表
    /// </summary>
    /// <param name="type">会话类型过滤（tarot/astrology/bazi）</param>
    /// <param name="skip">跳过记录数</param>
    /// <param name="take">获取记录数</param>
    /// <returns>会话列表</returns>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(
        [FromQuery] string? type = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        try
        {
            var userId = GetUserId();
            MysticType? mysticType = type?.ToLower() switch
            {
                "tarot" => MysticType.Tarot,
                "astrology" => MysticType.Astrology,
                "bazi" => MysticType.Bazi,
                _ => null
            };

            var response = await _mysticService.GetUserSessionsAsync(userId, mysticType, skip, take);

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
            _logger.LogError(ex, "获取会话列表失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 删除会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(string sessionId)
    {
        try
        {
            var userId = GetUserId();
            var success = await _mysticService.DeleteSessionAsync(sessionId, userId);

            if (!success)
            {
                return NotFound(new { error = "会话不存在" });
            }

            return Ok(new { success = true, message = "删除成功" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "未授权访问" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除会话失败");
            return StatusCode(500, new { error = "服务器内部错误" });
        }
    }

    #endregion
}
