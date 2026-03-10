using AiServiceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AiServiceApi.Data;

/// <summary>
/// 应用数据库上下文
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatHistoryMessage> ChatHistoryMessages { get; set; }
    public DbSet<LegalCase> LegalCases { get; set; }
    public DbSet<LegalEvidence> LegalEvidences { get; set; }
    public DbSet<MysticSession> MysticSessions { get; set; }
    public DbSet<MysticChatMessage> MysticChatMessages { get; set; }
    public DbSet<UsageLog> UsageLogs { get; set; }
    public DbSet<UserUsageQuota> UserUsageQuotas { get; set; }
    public DbSet<UserFeedback> UserFeedbacks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User 配置
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ApprovalStatus);
            entity.HasIndex(e => e.Role);
        });

        // ChatSession 配置
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.UserId, e.IsDeleted, e.ExpiresAt });

            entity.HasOne(e => e.User)
                .WithMany(u => u.ChatSessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatHistoryMessage 配置
        modelBuilder.Entity<ChatHistoryMessage>(entity =>
        {
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => new { e.SessionId, e.Order });

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LegalCase 配置
        modelBuilder.Entity<LegalCase>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CaseType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.UserId, e.IsDeleted, e.CaseType });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LegalEvidence 配置
        modelBuilder.Entity<LegalEvidence>(entity =>
        {
            entity.HasIndex(e => e.CaseId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Case)
                .WithMany(c => c.Evidences)
                .HasForeignKey(e => e.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MysticSession 配置
        modelBuilder.Entity<MysticSession>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.UserId, e.Type, e.CreatedAt });

            entity.Property(e => e.SessionData)
                .HasColumnType("jsonb");
            
            entity.Property(e => e.AnalysisResult)
                .HasColumnType("jsonb");
        });

        // MysticChatMessage 配置
        modelBuilder.Entity<MysticChatMessage>(entity =>
        {
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => new { e.SessionId, e.Timestamp });

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UsageLog 配置 - 用户使用统计日志
        modelBuilder.Entity<UsageLog>(entity =>
        {
            // 常用查询的索引
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Module);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.IsSuccess);
            
            // 复合索引：用于时间范围 + 用户查询
            entity.HasIndex(e => new { e.UserId, e.Timestamp });
            
            // 复合索引：用于模块 + 时间范围查询
            entity.HasIndex(e => new { e.Module, e.Timestamp });
            
            // 复合索引：用于统计分组查询
            entity.HasIndex(e => new { e.Timestamp, e.Module, e.UserId });

            // JSON 列配置
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");

            // 外键关系
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserUsageQuota 配置 - 用户使用额度
        modelBuilder.Entity<UserUsageQuota>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.WeeklyResetAt);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserFeedback 配置 - 用户反馈
        modelBuilder.Entity<UserFeedback>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Status, e.CreatedAt });

            entity.Property(e => e.Screenshots)
                .HasColumnType("jsonb");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
