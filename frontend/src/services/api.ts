import { getApiBaseUrl } from './config';

// 获取 API 基础 URL（动态获取，支持运行时配置）
const getApiUrl = () => getApiBaseUrl();

export interface ImageGenerationRequest {
  prompt: string;
  images?: string[];  // 可选：多张参考图片（base64编码），最多16张
}

export interface ImageGenerationResponse {
  success: boolean;
  imageUrl?: string;
  imageBase64?: string;
  error?: string;
}

// 聊天消息内容
export interface MessageContent {
  type: 'text' | 'image_url';
  text?: string;
  imageUrl?: {
    url: string;
  };
}

// 聊天消息
export interface ChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: MessageContent[];
}

// 聊天请求
export interface ChatRequest {
  messages: ChatMessage[];
  temperature?: number;
  maxTokens?: number;
}

// 聊天响应
export interface ChatResponse {
  success: boolean;
  message?: string;
  error?: string;
}

export const generateImage = async (request: ImageGenerationRequest): Promise<ImageGenerationResponse> => {
  const { authStorage } = await import('./authService');
  const token = authStorage.getToken();
  
  if (!token) {
    return {
      success: false,
      error: '请先登录',
    };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/image/generate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      if (response.status === 401) {
        authStorage.clear();
        return {
          success: false,
          error: '登录已过期，请重新登录',
        };
      }
      if (response.status === 403) {
        const errorData = await response.json().catch(() => ({}));
        return {
          success: false,
          error: errorData.error || '您没有权限使用此功能，请等待管理员审批',
        };
      }
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error occurred',
    };
  }
};

export const healthCheck = async (): Promise<boolean> => {
  try {
    const response = await fetch(`${getApiUrl()}/api/image/health`);
    return response.ok;
  } catch {
    return false;
  }
};

// 发送聊天消息（需要登录且已批准）
export const sendChatMessage = async (request: ChatRequest): Promise<ChatResponse> => {
  const { authStorage } = await import('./authService');
  const token = authStorage.getToken();
  
  if (!token) {
    return {
      success: false,
      error: '请先登录',
    };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/chat/send`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
      },
      body: JSON.stringify(request),
    });

    // 检查响应是否成功
    if (!response.ok) {
      if (response.status === 401) {
        authStorage.clear();
        return {
          success: false,
          error: '登录已过期，请重新登录',
        };
      }
      if (response.status === 403) {
        const errorData = await response.json().catch(() => ({}));
        return {
          success: false,
          error: errorData.error || '您没有权限使用此功能，请等待管理员审批',
        };
      }
      const errorText = await response.text();
      return {
        success: false,
        error: `服务器错误 (${response.status}): ${errorText || response.statusText}`,
      };
    }

    // 检查响应是否为空
    const text = await response.text();
    if (!text) {
      return {
        success: false,
        error: '服务器返回空响应',
      };
    }

    // 解析JSON
    const data = JSON.parse(text);
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error occurred',
    };
  }
};

// 带会话历史的聊天请求
export interface ChatRequestWithSession extends ChatRequest {
  sessionId?: string;
}

// 带会话信息的聊天响应
export interface ChatResponseWithSession extends ChatResponse {
  sessionId?: string;
  sessionTitle?: string;
}

// 来源引用
export interface SourceReference {
  title: string;
  url: string;
  snippet?: string;
}

// 带 RAG 功能的聊天请求
export interface ChatRequestWithRag extends ChatRequestWithSession {
  enableWebSearch?: boolean;
}

// 带会话信息和来源引用的聊天响应
export interface ChatResponseWithSessionAndSources extends ChatResponseWithSession {
  usedWebSearch?: boolean;
  searchQuery?: string;
  sources?: SourceReference[];
}

// 发送带历史记录的聊天消息（需要登录）
export const sendChatMessageWithHistory = async (request: ChatRequestWithSession): Promise<ChatResponseWithSession> => {
  const { authStorage } = await import('./authService');
  const token = authStorage.getToken();
  
  if (!token) {
    return {
      success: false,
      error: '请先登录',
    };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/chat/send-with-history`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      if (response.status === 401) {
        authStorage.clear();
        return {
          success: false,
          error: '登录已过期，请重新登录',
        };
      }
      if (response.status === 403) {
        const errorData = await response.json().catch(() => ({}));
        return {
          success: false,
          error: errorData.error || '您没有权限使用此功能，请等待管理员审批',
        };
      }
      const errorText = await response.text();
      return {
        success: false,
        error: `服务器错误 (${response.status}): ${errorText || response.statusText}`,
      };
    }

    const text = await response.text();
    if (!text) {
      return {
        success: false,
        error: '服务器返回空响应',
      };
    }

    const data = JSON.parse(text);
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error occurred',
    };
  }
};

// 发送支持 RAG 的聊天消息（LLM 自动判断是否需要 Web 搜索）
export const sendChatMessageWithRag = async (request: ChatRequestWithRag): Promise<ChatResponseWithSessionAndSources> => {
  const { authStorage } = await import('./authService');
  const token = authStorage.getToken();
  
  if (!token) {
    return {
      success: false,
      error: '请先登录',
    };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/chat/send-with-rag`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      if (response.status === 401) {
        authStorage.clear();
        return {
          success: false,
          error: '登录已过期，请重新登录',
        };
      }
      if (response.status === 403) {
        const errorData = await response.json().catch(() => ({}));
        return {
          success: false,
          error: errorData.error || '您没有权限使用此功能，请等待管理员审批',
        };
      }
      const errorText = await response.text();
      return {
        success: false,
        error: `服务器错误 (${response.status}): ${errorText || response.statusText}`,
      };
    }

    const text = await response.text();
    if (!text) {
      return {
        success: false,
        error: '服务器返回空响应',
      };
    }

    const data = JSON.parse(text);
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error occurred',
    };
  }
};
