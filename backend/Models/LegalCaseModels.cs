using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AiServiceApi.Models;

/// <summary>
/// 法律案件类型
/// </summary>
public enum LegalCaseType
{
    /// <summary>
    /// 离婚财产分析
    /// </summary>
    Divorce = 0,
    
    /// <summary>
    /// 劳动仲裁
    /// </summary>
    Labor = 1,
    
    /// <summary>
    /// 租房纠纷
    /// </summary>
    Rental = 2
}

/// <summary>
/// 案件状态
/// </summary>
public enum LegalCaseStatus
{
    /// <summary>
    /// 草稿
    /// </summary>
    Draft = 0,
    
    /// <summary>
    /// 分析中
    /// </summary>
    Analyzing = 1,
    
    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// 已归档
    /// </summary>
    Archived = 3
}

/// <summary>
/// 证据类型
/// </summary>
public enum EvidenceType
{
    Audio = 0,      // 录音
    Image = 1,      // 图片
    Document = 2,   // 文档
    Contract = 3,   // 合同
    Chat = 4,       // 聊天记录
    Bank = 5,       // 银行流水
    Property = 6,   // 房产证明
    Other = 7       // 其他
}

/// <summary>
/// 法律案件实体
/// </summary>
public class LegalCase
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 所属用户ID
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 案件类型
    /// </summary>
    public LegalCaseType CaseType { get; set; }
    
    /// <summary>
    /// 案件状态
    /// </summary>
    public LegalCaseStatus Status { get; set; } = LegalCaseStatus.Draft;
    
    /// <summary>
    /// 案件标题
    /// </summary>
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 案件描述
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 案件数据（JSON格式，存储具体案件信息）
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string CaseData { get; set; } = "{}";
    
    /// <summary>
    /// AI分析结果（JSON格式）
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? AnalysisResult { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 是否已删除（软删除）
    /// </summary>
    public bool IsDeleted { get; set; } = false;
    
    /// <summary>
    /// 关联的用户
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
    
    /// <summary>
    /// 关联的证据列表
    /// </summary>
    public virtual ICollection<LegalEvidence> Evidences { get; set; } = new List<LegalEvidence>();
}

/// <summary>
/// 法律案件证据实体
/// </summary>
public class LegalEvidence
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 所属案件ID
    /// </summary>
    [Required]
    public string CaseId { get; set; } = string.Empty;
    
    /// <summary>
    /// 证据类型
    /// </summary>
    public EvidenceType Type { get; set; }
    
    /// <summary>
    /// 证据名称
    /// </summary>
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 证据描述
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 文件URL（存储在云端）
    /// </summary>
    [MaxLength(1024)]
    public string? FileUrl { get; set; }
    
    /// <summary>
    /// 转写文本（录音转文字）
    /// </summary>
    public string? Transcript { get; set; }
    
    /// <summary>
    /// AI提取的关键信息
    /// </summary>
    public string? ExtractedInfo { get; set; }
    
    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long? FileSize { get; set; }
    
    /// <summary>
    /// 文件MIME类型
    /// </summary>
    [MaxLength(128)]
    public string? MimeType { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 关联的案件
    /// </summary>
    [ForeignKey(nameof(CaseId))]
    public virtual LegalCase? Case { get; set; }
}
