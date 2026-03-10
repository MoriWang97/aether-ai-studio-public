using AiServiceApi.Attributes;
using AiServiceApi.Models;
using AiServiceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiServiceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[TrackUsage(FeatureModule.Speech)]
[CheckUsageQuota]
public class SpeechController : ControllerBase
{
    private readonly IAzureSpeechService _speechService;
    private readonly ILogger<SpeechController> _logger;

    public SpeechController(IAzureSpeechService speechService, ILogger<SpeechController> logger)
    {
        _speechService = speechService;
        _logger = logger;
    }

    /// <summary>
    /// 语音转文字 (Speech-to-Text)
    /// </summary>
    /// <param name="request">包含音频数据的请求</param>
    /// <returns>识别出的文字</returns>
    [Authorize]
    [RequireApprovedUser]
    [HttpPost("transcribe")]
    public async Task<ActionResult<SpeechToTextResponse>> Transcribe([FromBody] SpeechToTextRequest request)
    {
        if (string.IsNullOrEmpty(request.AudioBase64))
        {
            return BadRequest(new SpeechToTextResponse
            {
                Success = false,
                Error = "音频数据不能为空"
            });
        }

        _logger.LogInformation("Received STT request, audio format: {Format}, language: {Language}", 
            request.AudioFormat, request.Language);

        var result = await _speechService.TranscribeAsync(request);

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
    /// 文字转语音 (Text-to-Speech)
    /// </summary>
    /// <param name="request">包含文本的请求</param>
    /// <returns>合成的音频数据</returns>
    [Authorize]
    [RequireApprovedUser]
    [HttpPost("synthesize")]
    public async Task<ActionResult<TextToSpeechResponse>> Synthesize([FromBody] TextToSpeechRequest request)
    {
        if (string.IsNullOrEmpty(request.Text))
        {
            return BadRequest(new TextToSpeechResponse
            {
                Success = false,
                Error = "文本内容不能为空"
            });
        }

        _logger.LogInformation("Received TTS request, text length: {Length}, voice: {Voice}", 
            request.Text.Length, request.VoiceName);

        var result = await _speechService.SynthesizeAsync(request);

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
    /// 获取可用语音列表
    /// </summary>
    /// <param name="locale">语言区域，默认 zh-CN</param>
    /// <returns>可用语音列表</returns>
    [Authorize]
    [HttpGet("voices")]
    public async Task<ActionResult<AvailableVoicesResponse>> GetVoices([FromQuery] string locale = "zh-CN")
    {
        _logger.LogInformation("Received voices request for locale: {Locale}", locale);

        var result = await _speechService.GetAvailableVoicesAsync(locale);

        return Ok(result);
    }
}
