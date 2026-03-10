namespace AiServiceApi.Models;

public class ImageGenerationRequest
{
    public string Prompt { get; set; } = string.Empty;
    public List<string>? Images { get; set; }  // 可选：用户上传的图片列表（base64编码），最多5张
}

public class ImageGenerationResponse
{
    public bool Success { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageBase64 { get; set; }
    public string? Error { get; set; }
}
