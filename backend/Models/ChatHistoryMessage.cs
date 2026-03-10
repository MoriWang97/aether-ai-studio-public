using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiServiceApi.Models;

/// <summary>
/// 聊天历史消息实体
/// </summary>
public class ChatHistoryMessage
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 所属会话ID
    /// </summary>
    [Required]
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// 消息角色 (user/assistant/system)
    /// </summary>
    [Required]
    [MaxLength(16)]
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// 消息文本内容
    /// </summary>
    public string? TextContent { get; set; }
    
    /// <summary>
    /// 图片URL列表（JSON格式存储）
    /// </summary>
    public string? ImageUrls { get; set; }
    
    /// <summary>
    /// 消息顺序
    /// </summary>
    public int Order { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 关联的会话
    /// </summary>
    [ForeignKey(nameof(SessionId))]
    public virtual ChatSession? Session { get; set; }
}
