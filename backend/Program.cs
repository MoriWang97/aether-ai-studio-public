using System.Text;
using AiServiceApi.Data;
using AiServiceApi.Domain.Interfaces;
using AiServiceApi.Infrastructure.Services;
using AiServiceApi.Services;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 配置 Azure Key Vault
var keyVaultUrl = builder.Configuration["KeyVault:Url"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    // 使用 DefaultAzureCredential，本地开发时会尝试多种认证方式（VS登录、Azure CLI等）
    // 部署到 App Service 后会自动使用 Managed Identity
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 使用 camelCase 命名策略，与前端 JavaScript 保持一致
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 配置 PostgreSQL 数据库
// 生产环境从 Key Vault 读取 (PostgreConnectionString)，开发环境从 appsettings.Development.json 读取
var connectionString = builder.Configuration["PostgreConnectionString"] 
    ?? builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Database connection string not configured");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 配置 JWT 认证
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] 
    ?? throw new InvalidOperationException("JWT SecretKey not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AiServiceApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AiServiceApi";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// 配置 HttpClient 和 Azure AI Service
builder.Services.AddHttpClient<IAzureAIService, AzureAIService>(client =>
{
    // 增加超时时间到 5 分钟，处理多张图片时需要更长时间
    client.Timeout = TimeSpan.FromMinutes(5);
});

// 注册JWT服务
builder.Services.AddSingleton<IJwtService, JwtService>();

// 注册邮箱验证码服务
builder.Services.AddSingleton<IEmailVerificationService, EmailVerificationService>();

// 注册邮件服务
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();

// 注册聊天历史服务
builder.Services.AddScoped<IChatHistoryService, ChatHistoryService>();

// 注册 Web 搜索服务 (Tavily)
builder.Services.AddHttpClient<IWebSearchService, TavilySearchService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 注册 RAG 聊天服务
builder.Services.AddHttpClient<IRagChatService, RagChatService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});

// 注册 Azure 语音服务 (STT/TTS)
builder.Services.AddHttpClient<IAzureSpeechService, AzureSpeechService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(2);
});

// 注册法律助手服务
builder.Services.AddHttpClient<ILegalAssistantService, LegalAssistantService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});

// 注册玄学助手服务（塔罗/星座/八字）
builder.Services.AddScoped<IMysticAssistantService, MysticAssistantService>();

// 注册使用统计服务
builder.Services.AddScoped<IUsageStatisticsService, UsageStatisticsService>();

// 注册使用额度服务
builder.Services.AddScoped<IUsageQuotaService, UsageQuotaService>();

// 注册反馈服务
builder.Services.AddScoped<IFeedbackService, FeedbackService>();

// 配置 CORS - 允许前端跨域访问
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",    // React 开发环境
                "http://localhost:5173",    // Vite 开发环境
                "https://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
    
    // 生产环境 - 允许所有来源（部署时可以限制具体域名）
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// 应用数据库迁移
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied");
    }
    catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P07") // relation already exists
    {
        logger.LogWarning("Tables already exist, marking migration as applied...");
        // 表已存在，手动插入迁移记录
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        foreach (var migration in pendingMigrations)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
                migration, "8.0.11");
        }
        logger.LogInformation("Migration records inserted");
    }
    
    // 确保所有缺失的列都存在（从 SQLite 迁移到 PostgreSQL 时可能缺失）
    logger.LogInformation("Ensuring all columns exist...");
    var alterTableSql = @"
        ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""ApprovedBy"" character varying(128);
        ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""RejectionReason"" character varying(500);
        ALTER TABLE ""ChatHistoryMessages"" ADD COLUMN IF NOT EXISTS ""ImageUrls"" text;
    ";
    await dbContext.Database.ExecuteSqlRawAsync(alterTableSql);
    logger.LogInformation("Column check completed");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowFrontend");
}
else
{
    app.UseCors("AllowAll");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 定期清理过期会话的后台任务
_ = Task.Run(async () =>
{
    while (true)
    {
        try
        {
            await Task.Delay(TimeSpan.FromHours(24)); // 每24小时执行一次
            using var scope = app.Services.CreateScope();
            var chatHistoryService = scope.ServiceProvider.GetRequiredService<IChatHistoryService>();
            var cleanedCount = await chatHistoryService.CleanupExpiredSessionsAsync();
            if (cleanedCount > 0)
            {
                app.Logger.LogInformation("Cleaned up {Count} expired sessions", cleanedCount);
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Error during session cleanup");
        }
    }
});

app.Run();
