using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AiServiceApi.Models;
using SkiaSharp;

namespace AiServiceApi.Services;

public interface IAzureAIService
{
    Task<ImageGenerationResponse> GenerateImageAsync(ImageGenerationRequest request);
    Task<ChatResponse> SendChatMessageAsync(ChatRequest request);
}

public class AzureAIService : IAzureAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureAIService> _logger;

    public AzureAIService(HttpClient httpClient, IConfiguration configuration, ILogger<AzureAIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ImageGenerationResponse> GenerateImageAsync(ImageGenerationRequest request)
    {
        try
        {
            var endpoint = _configuration["AzureAI:Endpoint"];
            var apiKey = _configuration["AzureAI:ApiKey"];
            var deploymentName = _configuration["AzureAI:DeploymentName"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                return new ImageGenerationResponse
                {
                    Success = false,
                    Error = "Azure AI endpoint or API key not configured"
                };
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

            HttpResponseMessage response;
            string responseContent;

            if (request.Images != null && request.Images.Count > 0)
            {
                // 图生图模式 - 使用 /images/edits 端点和 multipart/form-data
                response = await GenerateImageEditAsync(endpoint, deploymentName, request);
            }
            else
            {
                // 文生图模式 - 使用 /images/generations 端点
                response = await GenerateImageFromTextAsync(endpoint, deploymentName, request);
            }

            responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Azure AI response status: {StatusCode}", response.StatusCode);
            _logger.LogDebug("Azure AI response content: {Content}", responseContent);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                // gpt-image-1 返回 base64 编码的图像，不是 URL
                var dataArray = result.GetProperty("data");
                if (dataArray.GetArrayLength() > 0)
                {
                    var firstItem = dataArray[0];
                    
                    // 优先检查 b64_json（gpt-image-1 默认返回格式）
                    if (firstItem.TryGetProperty("b64_json", out var b64Json))
                    {
                        var base64Data = b64Json.GetString();
                        return new ImageGenerationResponse
                        {
                            Success = true,
                            ImageBase64 = $"data:image/png;base64,{base64Data}"
                        };
                    }
                    // 兼容返回 URL 的情况
                    else if (firstItem.TryGetProperty("url", out var urlElement))
                    {
                        return new ImageGenerationResponse
                        {
                            Success = true,
                            ImageUrl = urlElement.GetString()
                        };
                    }
                }
                
                _logger.LogError("Unexpected response format: {Content}", responseContent);
                return new ImageGenerationResponse
                {
                    Success = false,
                    Error = "Unexpected response format from Azure AI"
                };
            }
            else
            {
                _logger.LogError("Azure AI request failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new ImageGenerationResponse
                {
                    Success = false,
                    Error = $"API request failed: {response.StatusCode} - {responseContent}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Azure AI service");
            return new ImageGenerationResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 文生图 - 使用 /images/generations 端点
    /// </summary>
    private async Task<HttpResponseMessage> GenerateImageFromTextAsync(string endpoint, string deploymentName, ImageGenerationRequest request)
    {
        var url = $"{endpoint}/openai/deployments/{deploymentName}/images/generations?api-version=2025-04-01-preview";
        _logger.LogInformation("Calling Azure OpenAI text-to-image at: {Url}", url);

        var requestBody = new
        {
            prompt = request.Prompt,
            n = 1,
            size = "1024x1024",
            quality = "auto"
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        return await _httpClient.PostAsync(url, content);
    }

    /// <summary>
    /// 图生图 - 使用 /images/edits 端点和 multipart/form-data，支持多图（最多16张）
    /// </summary>
    private async Task<HttpResponseMessage> GenerateImageEditAsync(string endpoint, string deploymentName, ImageGenerationRequest request)
    {
        var url = $"{endpoint}/openai/deployments/{deploymentName}/images/edits?api-version=2025-04-01-preview";
        _logger.LogInformation("Calling Azure OpenAI image edit at: {Url}", url);

        // 使用 multipart/form-data 格式
        var formContent = new MultipartFormDataContent();
        
        // 添加所有图片文件（限制最多5张图片）
        var images = request.Images!.Take(5).ToList();
        _logger.LogInformation("Processing {Count} images for edit", images.Count);

        for (int i = 0; i < images.Count; i++)
        {
            var imageBase64 = images[i];
            var pureBase64 = ExtractPureBase64(imageBase64);
            var imageBytes = Convert.FromBase64String(pureBase64);
            var imageFormat = GetImageFormat(imageBase64);

            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue($"image/{imageFormat}");
            // 多图时使用 image[] 格式
            formContent.Add(imageContent, "image[]", $"image{i}.{imageFormat}");
            
            _logger.LogInformation("Added image {Index} with format: {Format}", i, imageFormat);
        }

        // 获取第一张图片的尺寸作为输出尺寸参考
        var size = GetImageSize(images[0]);
        _logger.LogInformation("Using output size: {Size}", size);

        // 添加其他参数
        formContent.Add(new StringContent(request.Prompt), "prompt");
        formContent.Add(new StringContent("1"), "n");
        formContent.Add(new StringContent(size), "size");

        return await _httpClient.PostAsync(url, formContent);
    }

    /// <summary>
    /// 从 base64 数据 URI 获取图片格式
    /// </summary>
    private string GetImageFormat(string base64Data)
    {
        if (base64Data.StartsWith("data:image/png"))
            return "png";
        if (base64Data.StartsWith("data:image/jpeg") || base64Data.StartsWith("data:image/jpg"))
            return "jpeg";
        if (base64Data.StartsWith("data:image/webp"))
            return "webp";
        
        // 默认返回 png
        return "png";
    }

    /// <summary>
    /// 从 base64 图片数据中提取纯 base64 字符串（去除 data:image/xxx;base64, 前缀）
    /// </summary>
    private string ExtractPureBase64(string base64Data)
    {
        if (base64Data.Contains(","))
        {
            return base64Data.Split(',')[1];
        }
        return base64Data;
    }

    /// <summary>
    /// 获取图片尺寸，返回 API 支持的最接近尺寸
    /// gpt-image-1 支持的尺寸: 1024x1024, 1536x1024 (横向), 1024x1536 (纵向), auto
    /// </summary>
    private string GetImageSize(string base64Data)
    {
        try
        {
            var pureBase64 = ExtractPureBase64(base64Data);
            var imageBytes = Convert.FromBase64String(pureBase64);
            
            using var stream = new MemoryStream(imageBytes);
            using var codec = SKCodec.Create(stream);
            
            if (codec != null)
            {
                var width = codec.Info.Width;
                var height = codec.Info.Height;
                
                _logger.LogInformation("Original image dimensions: {Width}x{Height}", width, height);
                
                // 根据宽高比选择最接近的支持尺寸
                var aspectRatio = (double)width / height;
                
                if (aspectRatio > 1.2)
                {
                    // 横向图片
                    return "1536x1024";
                }
                else if (aspectRatio < 0.8)
                {
                    // 纵向图片
                    return "1024x1536";
                }
                else
                {
                    // 接近正方形
                    return "1024x1024";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect image size, using default");
        }
        
        return "1024x1024"; // 默认尺寸
    }

    /// <summary>
    /// 发送聊天消息（支持多轮对话和视觉功能）
    /// </summary>
    public async Task<ChatResponse> SendChatMessageAsync(ChatRequest request)
    {
        try
        {
            var endpoint = _configuration["AzureAI:Endpoint"];
            var apiKey = _configuration["AzureAI:ApiKey"];
            var chatDeploymentName = _configuration["AzureAI:ChatDeploymentName"] ?? "gpt-5.4";

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                return new ChatResponse
                {
                    Success = false,
                    Error = "Azure AI endpoint or API key not configured"
                };
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

            var url = $"{endpoint}/openai/deployments/{chatDeploymentName}/chat/completions?api-version=2025-01-01-preview";
            _logger.LogInformation("Calling Azure OpenAI chat at: {Url}", url);

            // 构建请求体
            var messages = request.Messages.Select(m => new
            {
                role = m.Role,
                content = m.Content.Count == 1 && m.Content[0].Type == "text"
                    ? (object)m.Content[0].Text  // 纯文本消息
                    : m.Content.Select(c => new  // 多模态消息（文本+图片）
                    {
                        type = c.Type,
                        text = c.Text,
                        image_url = c.ImageUrl != null ? new { url = c.ImageUrl.Url } : null
                    }).ToArray()
            }).ToList();

            var requestBody = new
            {
                messages = messages,
                temperature = request.Temperature,
                max_completion_tokens = request.MaxTokens,
                stream = false
            };

            var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions 
            { 
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull 
            });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogDebug("Chat request body: {Body}", jsonContent);

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Azure AI chat response status: {StatusCode}", response.StatusCode);
            _logger.LogDebug("Azure AI chat response content: {Content}", responseContent);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                var choices = result.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var choice = choices[0];
                    var messageObj = choice.GetProperty("message");
                    
                    // 检查是否有 content 属性且不为空
                    string? message = null;
                    if (messageObj.TryGetProperty("content", out var contentElement))
                    {
                        message = contentElement.GetString();
                    }
                    
                    // 检查 finish_reason 是否为 content_filter
                    var finishReason = choice.TryGetProperty("finish_reason", out var fr) ? fr.GetString() : null;
                    
                    if (string.IsNullOrEmpty(message))
                    {
                        // 检查是否有 refusal 信息
                        var refusal = messageObj.TryGetProperty("refusal", out var r) ? r.GetString() : null;
                        
                        _logger.LogWarning("Azure AI返回空内容. finish_reason: {FinishReason}, refusal: {Refusal}, 完整响应: {Response}", 
                            finishReason, refusal, responseContent);
                        
                        return new ChatResponse
                        {
                            Success = false,
                            Error = finishReason == "content_filter" 
                                ? "内容被安全过滤器拦截，请调整您的请求" 
                                : refusal ?? "AI返回了空响应，请重试"
                        };
                    }
                    
                    return new ChatResponse
                    {
                        Success = true,
                        Message = message
                    };
                }

                _logger.LogError("Unexpected chat response format: {Content}", responseContent);
                return new ChatResponse
                {
                    Success = false,
                    Error = "Unexpected response format from Azure AI"
                };
            }
            else
            {
                _logger.LogError("Azure AI chat request failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new ChatResponse
                {
                    Success = false,
                    Error = $"API request failed: {response.StatusCode} - {responseContent}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Azure AI chat service");
            return new ChatResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}
