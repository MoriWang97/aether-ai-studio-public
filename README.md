# Aether AI Studio

一个功能丰富的 AI 服务平台，支持多模态对话、图像生成和 Web 搜索。现已支持用户登录和聊天历史记录功能。

## ✨ 功能特性

- 🔐 **用户登录系统** - 支持邮箱注册和登录
- 💬 **聊天历史记录** - 登录后自动保存对话，历史保留30天
- 📋 **会话管理** - 可查看、切换、删除历史会话
- 🎨 **AI 图像生成** - 基于 Azure AI 的图像生成服务

## 📁 项目结构

```
aether-ai-studio/
├── backend/              # C# ASP.NET Core Web API
│   ├── Controllers/      # API 控制器 
│   │   ├── AuthController.cs        # 认证相关（邮箱注册/登录）
│   │   ├── ChatController.cs        # 聊天API
│   │   ├── ChatHistoryController.cs # 历史记录API
│   │   └── ImageController.cs       # 图像生成API
│   ├── Data/             # 数据库上下文
│   │   └── AppDbContext.cs
│   ├── Models/           # 数据模型
│   │   ├── User.cs
│   │   ├── ChatSession.cs
│   │   └── ChatHistoryMessage.cs
│   └── Services/         # 服务层
│       ├── AzureAIService.cs
│       ├── JwtService.cs
│       └── ChatHistoryService.cs
└── frontend/             # React TypeScript 前端
    └── src/
        ├── components/
        │   ├── ChatInterface.tsx     # 聊天界面
        │   ├── ChatHistory.tsx       # 历史侧边栏
        │   ├── LoginModal.tsx        # 登录弹窗
        │   └── ImageGenerator.tsx    # 图像生成
        ├── contexts/
        │   └── AuthContext.tsx       # 认证状态管理
        └── services/
            ├── api.ts                # API调用
            └── authService.ts        # 认证服务
```

## 🚀 本地开发

### 后端 (C# API)

```bash
cd backend
# 配置 appsettings.json 中的 Azure AI 信息
dotnet run
```

后端运行在: `http://localhost:5205`

### 前端 (React)

```bash
cd frontend
npm install
npm start
```

前端运行在: `http://localhost:3000`

## ⚙️ 配置说明

### 后端配置 (backend/appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=aiservice.db"
  },
  "Jwt": {
    "SecretKey": "替换为你的密钥（至少32字符）",
    "Issuer": "AiServiceApi",
    "Audience": "AiServiceApi",
    "AccessTokenExpiryMinutes": "1440"
  },
  "AzureAI": {
    "Endpoint": "你的 Azure AI Foundry Endpoint",
    "ApiKey": "你的 API Key",
    "DeploymentName": "gpt-image",
    "ChatDeploymentName": "gpt-4"
  }
}
```

### 前端配置 (frontend/.env)

```
REACT_APP_API_URL=http://localhost:5205
```

## 🔐 API 接口说明

### 认证相关

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/auth/register` | 邮箱注册 |
| POST | `/api/auth/login` | 邮箱登录 |
| GET | `/api/auth/me` | 获取当前用户信息（需登录）|

### 聊天相关

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/chat/send` | 发送消息（无需登录）|
| POST | `/api/chat/send-with-history` | 发送消息并保存历史（需登录）|

### 历史记录

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/chathistory/sessions` | 获取会话列表 |
| GET | `/api/chathistory/sessions/{id}` | 获取会话详情 |
| POST | `/api/chathistory/sessions` | 创建新会话 |
| PUT | `/api/chathistory/sessions/{id}/title` | 更新会话标题 |
| DELETE | `/api/chathistory/sessions/{id}` | 删除会话 |

### 图像生成

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/image/generate` | 生成图片 |
| GET | `/api/image/health` | 健康检查 |

## 🌐 Azure 部署（适合中国访问）

### 方案：Azure App Service

1. **创建资源组**
   ```bash
   az group create --name BuildingDesignRG --location eastasia
   ```

2. **部署后端 API**
   ```bash
   cd backend
   dotnet publish -c Release -o ./publish
   
   # 创建 App Service Plan (B1 基础版约 $13/月)
   az appservice plan create --name BuildingDesignPlan --resource-group BuildingDesignRG --sku B1 --is-linux
   
   # 创建 Web App
   az webapp create --name your-api-name --resource-group BuildingDesignRG --plan BuildingDesignPlan --runtime "DOTNET|8.0"
   
   # 部署代码
   az webapp deploy --resource-group BuildingDesignRG --name your-api-name --src-path ./publish.zip
   ```

3. **部署前端**
   ```bash
   cd frontend
   # 修改 .env 中的 API 地址
   npm run build
   
   # 创建静态 Web App (免费或低成本)
   az staticwebapp create --name your-frontend-name --resource-group BuildingDesignRG --source ./build
   ```

### 推荐区域（中国访问友好）
- **East Asia (香港)** - 推荐，中国访问速度快
- **Southeast Asia (新加坡)** - 备选

## 💰 预算估算（$150/月内）

| 服务 | SKU | 预估成本 |
|------|-----|----------|
| App Service (后端) | B1 | ~$13/月 |
| Static Web App (前端) | Free | $0 |
| Azure AI Foundry | 按使用量 | ~$50-100/月 |
| **总计** | | **< $150/月** |

## 📝 图像生成 API

### POST /api/image/generate

生成图片

**请求体:**
```json
{
  "prompt": "描述文字",
  "imageBase64": "可选的参考图片base64"
}
```

**响应:**
```json
{
  "success": true,
  "imageUrl": "生成的图片URL"
}
```

## 🔧 开发提示

1. 在 Azure AI Foundry 获取你的 endpoint 和 key
2. 确保后端 CORS 配置正确（已配置允许前端访问）
3. 部署时记得更新 frontend/.env 中的 API 地址
4. 数据库使用 SQLite，会自动创建 `aiservice.db` 文件
5. JWT 密钥请使用足够长度的随机字符串（至少32字符）
