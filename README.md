# Aether AI Studio

一个功能丰富的全栈 AI 服务平台，基于 Azure AI 构建，支持多模态对话、RAG Web 搜索、AI 图像生成、语音交互、法律助手、玄学助手等多种 AI 能力，并配备完善的用户管理、配额控制和后台管理系统。

## ✨ 功能特性

### 核心 AI 功能
- 💬 **智能对话** — 多轮对话，支持图片附件、Markdown 渲染、会话历史自动保存
- 🔍 **RAG Web 搜索** — 基于 LLM Tool Calling + Tavily 搜索引擎，AI 自动判断是否需要联网查询，返回带来源引用的答案
- 🎨 **AI 图像生成** — 基于 Azure AI（DALL-E）的文生图，支持多张参考图片
- 🎙️ **语音交互** — Azure Speech 驱动的语音识别（STT）与语音合成（TTS），支持录音输入和语音播放

### 专业助手
- ⚖️ **法律助手** — 支持离婚、劳动、租房三类案件管理，证据收集与音频转写，AI 案情分析与法律文书生成
- 🔮 **玄学助手** — 塔罗牌占卜（多种牌阵）、星座运势分析、八字命理解析，每种分析均支持追问对话

### 平台管理
- 🔐 **用户认证** — 邮箱验证码注册、邮箱密码登录、JWT Access + Refresh Token
- ✅ **用户审批** — 用户需申请权限，管理员审批/拒绝/撤销
- 📊 **用量配额** — 按周配额控制，管理员可授予额外配额
- 📈 **使用统计** — 按模块（对话/图像/语音/法律/玄学/RAG）追踪，支持多维度筛选
- 💬 **反馈系统** — 用户提交 Bug/功能/体验反馈，管理员响应与状态管理
- 🛠️ **管理后台** — 用户管理、配额管理、反馈管理、使用统计四大面板

## 🛠 技术栈

| 层 | 技术 |
|----|------|
| **前端** | React 19 + TypeScript, react-markdown, remark-gfm |
| **后端** | .NET 8 / ASP.NET Core Web API |
| **数据库** | PostgreSQL（EF Core + 自动迁移） |
| **认证** | JWT Bearer（Symmetric Key），Azure Key Vault 存储密钥 |
| **AI 服务** | Azure OpenAI (Chat & Image)、Azure Speech (STT/TTS)、Tavily Web Search |
| **音视频处理** | FFMpegCore（音频格式转换）、SkiaSharp（图像处理） |
| **部署** | Azure App Service + Static Web App，Managed Identity + Key Vault |

## 📁 项目结构

```
aether-ai-studio/
├── backend/                    # C# ASP.NET Core Web API
│   ├── Controllers/            # API 控制器
│   │   ├── AuthController.cs           # 认证（注册/登录/审批）
│   │   ├── ChatController.cs           # AI 对话 & RAG 搜索
│   │   ├── ChatHistoryController.cs    # 会话历史 CRUD
│   │   ├── ImageController.cs          # AI 图像生成
│   │   ├── SpeechController.cs         # 语音识别 & 合成
│   │   ├── LegalAssistantController.cs # 法律助手
│   │   ├── MysticController.cs         # 玄学助手
│   │   ├── AdminController.cs          # 后台管理 & 统计
│   │   ├── QuotaController.cs          # 配额查询
│   │   └── FeedbackController.cs       # 反馈管理
│   ├── Services/               # 业务逻辑层
│   │   ├── AzureAIService.cs           # Azure OpenAI 调用
│   │   ├── RagChatService.cs           # RAG + Tool Calling
│   │   ├── AzureSpeechService.cs       # Azure Speech 服务
│   │   ├── LegalAssistantService.cs    # 法律案件分析
│   │   ├── MysticAssistantService.cs   # 玄学分析
│   │   ├── ChatHistoryService.cs       # 会话历史管理
│   │   ├── JwtService.cs              # JWT 令牌
│   │   ├── EmailService.cs            # 邮件发送
│   │   ├── UsageQuotaService.cs       # 配额管理
│   │   ├── UsageStatisticsService.cs  # 使用统计
│   │   └── FeedbackService.cs         # 反馈处理
│   ├── Data/                   # 数据库上下文 & EF Core
│   ├── Models/                 # 数据模型
│   ├── Attributes/             # 自定义特性（TrackUsage/CheckQuota/RequireApproved）
│   └── Migrations/             # 数据库迁移
└── frontend/                   # React TypeScript 前端
    └── src/
        ├── components/
        │   ├── ChatInterface.tsx       # AI 对话界面（含 RAG 开关、图片上传）
        │   ├── ChatHistory.tsx         # 会话历史侧边栏
        │   ├── ImageGenerator.tsx      # 图像生成界面
        │   ├── VoiceRecordButton.tsx   # 语音录入按钮
        │   ├── VoicePlayButton.tsx     # 语音播放按钮
        │   ├── LoginModal.tsx          # 登录/注册弹窗
        │   ├── AdminPanel.tsx          # 管理后台面板
        │   ├── FeedbackForm.tsx        # 反馈表单
        │   ├── UsageQuota.tsx          # 配额显示
        │   ├── UsageStatistics.tsx     # 使用统计图表
        │   └── UserManagement.tsx      # 用户管理
        ├── features/
        │   ├── legal-assistant/        # 法律助手模块
        │   └── mystic-assistant/       # 玄学助手模块
        ├── contexts/
        │   ├── AuthContext.tsx          # 认证状态管理
        │   └── QuotaContext.tsx         # 配额状态管理
        ├── services/                   # API 调用服务
        └── hooks/                      # 自定义 Hooks
```

## 🚀 本地开发

### 前置条件

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v18+)
- [PostgreSQL](https://www.postgresql.org/) 或使用 SQLite（开发模式）
- [FFmpeg](https://ffmpeg.org/)（语音功能需要）
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)（访问 Key Vault 需要先 `az login`）

### 后端

```bash
cd backend
# 1. 复制配置模板并填写
cp appsettings.json.template appsettings.json
# 2. 配置 Azure Key Vault URL（或本地设置密钥）
# 3. 启动
dotnet run
```

后端运行在: `http://localhost:5205`

### 前端

```bash
cd frontend
npm install
npm start
```

前端运行在: `http://localhost:3000`

## ⚙️ 配置说明

### 后端配置 (appsettings.json)

基于 `appsettings.json.template` 创建，敏感信息存储在 Azure Key Vault 中：

```jsonc
{
  "KeyVault": {
    "Url": "https://your-keyvault-name.vault.azure.net/"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=aiservice;Username=xxx;Password=xxx"
  },
  "Jwt": {
    "Issuer": "AiServiceApi",
    "Audience": "AiServiceApi",
    "AccessTokenExpiryMinutes": "10080"
  },
  "AzureAI": {
    "Endpoint": "https://your-azure-ai-endpoint.cognitiveservices.azure.com",
    "DeploymentName": "your-image-deployment-name",
    "ChatDeploymentName": "your-chat-deployment-name"
  },
  "Tavily": { "ApiKey": "" },
  "Rag": { "EnableWebSearch": true, "MaxSearchResults": 5 },
  "AzureSpeech": { "Region": "eastasia" },
  "Development": { "EnableDevLogin": true }
}
```

### Azure Key Vault Secrets

以下密钥需要在 Key Vault 中创建（名称中用 `--` 代替 `:` 分隔符）：

| Secret 名称 | 说明 |
|---|---|
| `Jwt--SecretKey` | JWT 签名密钥（至少 32 字节） |
| `AzureAI--ApiKey` | Azure AI 服务 API 密钥 |
| `Email--SmtpHost` | SMTP 服务器地址 |
| `Email--SmtpPort` | SMTP 端口 |
| `Email--SmtpUser` | SMTP 用户名 |
| `Email--SmtpPassword` | SMTP 密码/授权码 |
| `Email--FromEmail` | 发件人邮箱 |
| `Email--FromName` | 发件人名称 |
| `Tavily--ApiKey` | Tavily Web 搜索 API 密钥 |
| `AzureSpeech--ApiKey` | Azure Speech 服务 API 密钥 |

### 前端配置 (frontend/.env)

```
REACT_APP_API_URL=http://localhost:5205
```

## 🔐 API 接口

### 认证 (`/api/auth`)

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| POST | `/send-verification-code` | 否 | 发送邮箱验证码 |
| POST | `/register` | 否 | 邮箱 + 验证码注册 |
| POST | `/login` | 否 | 邮箱密码登录，返回 JWT |
| GET | `/me` | 是 | 获取当前用户信息 |
| POST | `/refresh` | 否 | 刷新 Access Token |
| POST | `/request-approval` | 是 | 申请 AI 功能使用权限 |
| GET | `/permission-status` | 是 | 查询审批状态 |

### AI 对话 (`/api/chat`)

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| POST | `/send` | 已审批 | 发送消息（支持文本 + 图片） |
| POST | `/send-with-history` | 已审批 | 发送并保存会话历史 |
| POST | `/send-with-rag` | 已审批 | RAG 对话（可联网搜索），返回来源引用 |
| GET | `/health` | 否 | 健康检查 |

### 会话历史 (`/api/chathistory`)

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | `/sessions` | 是 | 获取会话列表 |
| GET | `/sessions/{id}` | 是 | 获取会话详情及消息 |
| POST | `/sessions` | 是 | 创建新会话 |
| PUT | `/sessions/{id}/title` | 是 | 更新会话标题 |
| DELETE | `/sessions/{id}` | 是 | 删除会话 |

### 图像生成 (`/api/image`)

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| POST | `/generate` | 已审批 | 根据提示词生成图片（支持参考图） |
| GET | `/health` | 否 | 健康检查 |

### 语音服务 (`/api/speech`)

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| POST | `/transcribe` | 已审批 | 语音转文字（base64 音频输入） |
| POST | `/synthesize` | 已审批 | 文字转语音（返回 base64 音频） |
| GET | `/voices` | 是 | 获取可用语音列表 |

### 法律助手 (`/api/legal`)

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | `/cases` | 是 | 获取案件列表（可按类型筛选：divorce/labor/rental） |
| GET | `/cases/{id}` | 是 | 获取案件详情 |
| POST | `/cases` | 是 | 创建案件 |
| PUT | `/cases/{id}` | 是 | 更新案件 |
| DELETE | `/cases/{id}` | 是 | 删除案件 |
| POST | `/cases/{id}/evidence` | 是 | 添加证据 |
| DELETE | `/evidence/{id}` | 是 | 删除证据 |
| POST | `/evidence/{id}/transcribe` | 是 | 音频证据转写 |
| POST | `/analyze` | 是 | AI 案情分析 |
| POST | `/generate-document` | 是 | 生成法律文书 |
| POST | `/analyze-direct` | 是 | 直接分析（无需保存案件） |
| POST | `/generate-document-direct` | 是 | 直接生成文书（无需保存案件） |

### 玄学助手 (`/api/mystic`)

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| POST | `/tarot/analyze` | 是 | 塔罗牌占卜（选择牌阵 + 问题） |
| POST | `/tarot/chat` | 是 | 塔罗追问对话 |
| POST | `/astrology/analyze` | 是 | 星座运势分析 |
| POST | `/astrology/chat` | 是 | 星座追问对话 |
| POST | `/bazi/analyze` | 是 | 八字命理分析 |
| POST | `/bazi/chat` | 是 | 八字追问对话 |
| GET | `/sessions` | 是 | 获取玄学会话列表（可按类型筛选） |
| DELETE | `/sessions/{id}` | 是 | 删除玄学会话 |

### 配额 (`/api/quota`)

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | `/` | 是 | 获取当前用户配额信息 |
| GET | `/check` | 是 | 检查是否可使用 AI 功能 |

### 反馈 (`/api/feedback`)

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| POST | `/` | 是 | 提交反馈（Bug/Feature/Experience） |
| GET | `/my` | 是 | 获取我的反馈列表 |
| GET | `/{id}` | 是 | 获取反馈详情 |
| GET | `/all` | 管理员 | 获取所有反馈（可筛选） |
| POST | `/respond` | 管理员 | 回复反馈 |
| PUT | `/{id}/status` | 管理员 | 更新反馈状态 |
| GET | `/statistics` | 管理员 | 反馈统计 |

### 管理后台 (`/api/admin`)

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | `/pending-users` | 管理员 | 待审批用户列表 |
| GET | `/all-users` | 管理员 | 全部用户列表（分页） |
| POST | `/approve-user` | 管理员 | 审批/拒绝用户 |
| POST | `/revoke-user` | 管理员 | 撤销用户权限 |
| GET | `/check-admin` | 是 | 检查当前用户是否为管理员 |
| GET | `/statistics/overview` | 管理员 | 使用统计概览（可筛选时间/用户/模块） |
| GET | `/statistics/logs` | 管理员 | 使用日志明细（分页） |
| GET | `/statistics/user/{userId}` | 管理员 | 指定用户统计 |
| GET | `/statistics/modules` | 管理员 | 可用功能模块列表 |
| GET | `/statistics/today` | 管理员 | 今日统计 |
| GET | `/statistics/this-week` | 管理员 | 本周统计 |
| GET | `/statistics/this-month` | 管理员 | 本月统计 |
| GET | `/quotas` | 管理员 | 全部用户配额（分页） |
| POST | `/quotas/grant` | 管理员 | 授予用户额外配额 |

## 🌐 Azure 部署

### 方案：Azure App Service + Static Web App

1. **创建资源组**
   ```bash
   az group create --name AetherAIStudioRG --location eastasia
   ```

2. **部署后端 API**
   ```bash
   cd backend
   dotnet publish -c Release -o ./publish

   # 创建 App Service
   az appservice plan create --name AetherPlan --resource-group AetherAIStudioRG --sku B1 --is-linux
   az webapp create --name your-api-name --resource-group AetherAIStudioRG --plan AetherPlan --runtime "DOTNET|8.0"

   # 配置 Key Vault
   # 1. 为 App Service 启用 System-assigned Managed Identity
   # 2. 在 Key Vault Access Policies 中授予该 Identity Get + List 权限
   # 3. 设置 App Setting: KeyVault__Url = https://your-keyvault.vault.azure.net/

   # 部署代码
   az webapp deploy --resource-group AetherAIStudioRG --name your-api-name --src-path ./publish.zip
   ```

3. **部署前端**
   ```bash
   cd frontend
   # 修改 .env 中的 API 地址为后端生产 URL
   npm run build
   az staticwebapp create --name your-frontend-name --resource-group AetherAIStudioRG --source ./build
   ```

### 推荐区域
- **East Asia (香港)** — 推荐，中国大陆访问速度快
- **Southeast Asia (新加坡)** — 备选

## 💰 预算估算

| 服务 | SKU | 预估成本 |
|------|-----|----------|
| App Service (后端) | B1 | ~$13/月 |
| Static Web App (前端) | Free | $0 |
| PostgreSQL Flexible Server | Burstable B1ms | ~$15/月 |
| Azure AI (Chat + Image) | 按用量 | ~$30-80/月 |
| Azure Speech | 按用量 | ~$5-15/月 |
| Azure Key Vault | Standard | < $1/月 |
| **总计** | | **~$65-125/月** |

## 🔧 开发提示

1. 在 [Azure AI Foundry](https://ai.azure.com/) 创建 Chat 和 Image 部署获取 Endpoint 和 Key
2. 在 [Tavily](https://tavily.com/) 注册获取 API Key 以启用 Web 搜索功能
3. 在 [Azure Portal](https://portal.azure.com/) 创建 Speech 资源获取 API Key
4. 本地开发通过 `az login` 访问 Key Vault，生产使用 Managed Identity
5. 数据库支持 PostgreSQL（生产）和 SQLite（本地开发），EF Core 启动时自动迁移
6. 后端 CORS 已配置允许 `localhost:3000` 和 `localhost:5173`，部署时需更新
7. 管理员通过 `appsettings.json` 中的 `Admin:Email` 配置，匹配的用户自动提升为管理员
8. 后台任务每 24 小时自动清理过期会话

## 📄 License

MIT
