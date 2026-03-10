using AiServiceApi.Attributes;
using AiServiceApi.Models;
using AiServiceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiServiceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[TrackUsage(FeatureModule.Image)]
[CheckUsageQuota]
public class ImageController : ControllerBase
{
    private readonly IAzureAIService _aiService;
    private readonly ILogger<ImageController> _logger;

    public ImageController(IAzureAIService aiService, ILogger<ImageController> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// 生成图片 - 需要登录且已被批准
    /// </summary>
    [Authorize]
    [RequireApprovedUser]
    [HttpPost("generate")]
    public async Task<ActionResult<ImageGenerationResponse>> GenerateImage([FromBody] ImageGenerationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new ImageGenerationResponse
            {
                Success = false,
                Error = "Prompt is required"
            });
        }

        _logger.LogInformation("Received image generation request with prompt: {Prompt}", request.Prompt);

        var result = await _aiService.GenerateImageAsync(request);
        
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return StatusCode(500, result);
        }
    }

    /// <summary>
    /// 健康检查
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
