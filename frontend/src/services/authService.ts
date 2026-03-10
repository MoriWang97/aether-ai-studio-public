import { getApiBaseUrl } from './config';

// 获取 API 基础 URL（动态获取）
const getApiUrl = () => getApiBaseUrl();

// 用户信息
export interface UserInfo {
  id: string;
  email?: string;
  nickname?: string;
  avatarUrl?: string;
  isAdmin?: boolean;
  isApproved?: boolean;
  approvalStatus?: 'Pending' | 'Approved' | 'Rejected';
  rejectionReason?: string;
}

// 登录响应
export interface LoginResponse {
  success: boolean;
  token?: string;
  refreshToken?: string;
  expiresAt?: string;
  user?: UserInfo;
  error?: string;
}

// 会话DTO
export interface ChatSessionDto {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
  messageCount: number;
}

// 会话列表响应
export interface ChatSessionListResponse {
  success: boolean;
  sessions: ChatSessionDto[];
  totalCount: number;
  error?: string;
}

// 消息DTO
export interface ChatMessageDto {
  id: string;
  role: string;
  textContent?: string;
  imageUrls?: string[];
  createdAt: string;
}

// 会话详情
export interface ChatSessionDetailDto {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
  messages: ChatMessageDto[];
}

// Token管理
const TOKEN_KEY = 'auth_token';
const REFRESH_TOKEN_KEY = 'refresh_token';
const USER_KEY = 'user_info';
const TOKEN_EXPIRY_KEY = 'token_expiry';

export const authStorage = {
  getToken: (): string | null => {
    return localStorage.getItem(TOKEN_KEY);
  },

  setToken: (token: string): void => {
    localStorage.setItem(TOKEN_KEY, token);
  },

  getRefreshToken: (): string | null => {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  },

  setRefreshToken: (token: string): void => {
    localStorage.setItem(REFRESH_TOKEN_KEY, token);
  },

  getTokenExpiry: (): string | null => {
    return localStorage.getItem(TOKEN_EXPIRY_KEY);
  },

  setTokenExpiry: (expiry: string): void => {
    localStorage.setItem(TOKEN_EXPIRY_KEY, expiry);
  },

  getUser: (): UserInfo | null => {
    const userStr = localStorage.getItem(USER_KEY);
    if (userStr) {
      try {
        return JSON.parse(userStr);
      } catch {
        return null;
      }
    }
    return null;
  },

  setUser: (user: UserInfo): void => {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  },

  clear: (): void => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(TOKEN_EXPIRY_KEY);
  },

  isLoggedIn: (): boolean => {
    return !!localStorage.getItem(TOKEN_KEY);
  },

  // 检查令牌是否即将过期（提前30分钟刷新）
  isTokenExpiringSoon: (): boolean => {
    const expiry = localStorage.getItem(TOKEN_EXPIRY_KEY);
    if (!expiry) return false;
    const expiryTime = new Date(expiry).getTime();
    const now = Date.now();
    const thirtyMinutes = 30 * 60 * 1000;
    return expiryTime - now < thirtyMinutes;
  }
};

// 刷新令牌（防止并发刷新）
let refreshPromise: Promise<boolean> | null = null;

const refreshAccessToken = async (): Promise<boolean> => {
  if (refreshPromise) return refreshPromise;

  refreshPromise = (async () => {
    try {
      const refreshToken = authStorage.getRefreshToken();
      const token = authStorage.getToken();
      if (!refreshToken || !token) return false;

      const response = await fetch(`${getApiUrl()}/api/auth/refresh`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify({ refreshToken }),
      });

      if (!response.ok) return false;

      const data: LoginResponse = await response.json();
      if (data.success && data.token) {
        authStorage.setToken(data.token);
        if (data.refreshToken) {
          authStorage.setRefreshToken(data.refreshToken);
        }
        if (data.expiresAt) {
          authStorage.setTokenExpiry(data.expiresAt);
        }
        if (data.user) {
          authStorage.setUser(data.user);
        }
        return true;
      }
      return false;
    } catch {
      return false;
    } finally {
      refreshPromise = null;
    }
  })();

  return refreshPromise;
};

// 带认证的fetch封装
export const authFetch = async (url: string, options: RequestInit = {}): Promise<Response> => {
  // 如果令牌即将过期，先尝试刷新
  if (authStorage.isTokenExpiringSoon() && authStorage.getRefreshToken()) {
    await refreshAccessToken();
  }

  const token = authStorage.getToken();
  
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
  }

  let response = await fetch(url, {
    ...options,
    headers,
  });

  // 如果401，尝试刷新令牌后重试
  if (response.status === 401 && authStorage.getRefreshToken()) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      // 用新令牌重试请求
      const newToken = authStorage.getToken();
      if (newToken) {
        (headers as Record<string, string>)['Authorization'] = `Bearer ${newToken}`;
      }
      response = await fetch(url, {
        ...options,
        headers,
      });
    }

    // 刷新失败或重试后仍然401，清除认证信息
    if (response.status === 401) {
      authStorage.clear();
    }
  }

  return response;
};

// 发送验证码响应
export interface SendVerificationCodeResponse {
  success: boolean;
  message?: string;
  error?: string;
  cooldownSeconds?: number;
}

// 发送验证码
export const sendVerificationCode = async (email: string): Promise<SendVerificationCodeResponse> => {
  try {
    const response = await fetch(`${getApiUrl()}/api/auth/send-verification-code`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email })
    });
    return await response.json();
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '发送验证码失败'
    };
  }
};

// 邮箱注册（带验证码）
export const emailRegister = async (email: string, password: string, verificationCode: string, nickname?: string): Promise<LoginResponse> => {
  try {
    const response = await fetch(`${getApiUrl()}/api/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password, verificationCode, nickname })
    });
    const data = await response.json();
    
    if (data.success && data.token) {
      authStorage.setToken(data.token);
      if (data.refreshToken) {
        authStorage.setRefreshToken(data.refreshToken);
      }
      if (data.expiresAt) {
        authStorage.setTokenExpiry(data.expiresAt);
      }
      if (data.user) {
        authStorage.setUser(data.user);
      }
    }
    
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '注册失败'
    };
  }
};

// 邮箱登录
export const emailLogin = async (email: string, password: string): Promise<LoginResponse> => {
  try {
    const response = await fetch(`${getApiUrl()}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });
    const data = await response.json();
    
    if (data.success && data.token) {
      authStorage.setToken(data.token);
      if (data.refreshToken) {
        authStorage.setRefreshToken(data.refreshToken);
      }
      if (data.expiresAt) {
        authStorage.setTokenExpiry(data.expiresAt);
      }
      if (data.user) {
        authStorage.setUser(data.user);
      }
    }
    
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '登录失败'
    };
  }
};

// 获取当前用户信息
export const getCurrentUser = async (): Promise<{ success: boolean; user?: UserInfo; error?: string }> => {
  try {
    const response = await authFetch(`${getApiUrl()}/api/auth/me`);
    return await response.json();
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '获取用户信息失败'
    };
  }
};

// 登出
export const logout = (): void => {
  authStorage.clear();
};

// 获取会话列表
export const getChatSessions = async (): Promise<ChatSessionListResponse> => {
  try {
    const response = await authFetch(`${getApiUrl()}/api/chathistory/sessions`);
    
    if (!response.ok) {
      return { success: false, sessions: [], totalCount: 0, error: '获取会话列表失败' };
    }
    
    return await response.json();
  } catch (error) {
    return {
      success: false,
      sessions: [],
      totalCount: 0,
      error: error instanceof Error ? error.message : '获取会话列表失败'
    };
  }
};

// 获取会话详情
export const getChatSessionDetail = async (sessionId: string): Promise<{ success: boolean; session?: ChatSessionDetailDto; error?: string }> => {
  try {
    const response = await authFetch(`${getApiUrl()}/api/chathistory/sessions/${sessionId}`);
    
    if (!response.ok) {
      return { success: false, error: '获取会话详情失败' };
    }
    
    return await response.json();
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '获取会话详情失败'
    };
  }
};

// 创建新会话
export const createChatSession = async (title?: string): Promise<{ success: boolean; session?: ChatSessionDto; error?: string }> => {
  try {
    const response = await authFetch(`${getApiUrl()}/api/chathistory/sessions`, {
      method: 'POST',
      body: JSON.stringify({ title })
    });
    
    if (!response.ok) {
      return { success: false, error: '创建会话失败' };
    }
    
    return await response.json();
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '创建会话失败'
    };
  }
};

// 删除会话
export const deleteChatSession = async (sessionId: string): Promise<{ success: boolean; error?: string }> => {
  try {
    const response = await authFetch(`${getApiUrl()}/api/chathistory/sessions/${sessionId}`, {
      method: 'DELETE'
    });
    
    if (!response.ok) {
      return { success: false, error: '删除会话失败' };
    }
    
    return await response.json();
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '删除会话失败'
    };
  }
};

// 更新会话标题
export const updateSessionTitle = async (sessionId: string, title: string): Promise<{ success: boolean; error?: string }> => {
  try {
    const response = await authFetch(`${getApiUrl()}/api/chathistory/sessions/${sessionId}/title`, {
      method: 'PUT',
      body: JSON.stringify({ title })
    });
    
    if (!response.ok) {
      return { success: false, error: '更新标题失败' };
    }
    
    return await response.json();
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '更新标题失败'
    };
  }
};

// 申请权限响应
export interface ApprovalRequestResponse {
  success: boolean;
  message?: string;
  error?: string;
  status?: 'Pending' | 'Approved' | 'Rejected';
}

// 申请使用权限
export const requestApproval = async (reason?: string): Promise<ApprovalRequestResponse> => {
  try {
    const response = await authFetch(`${getApiUrl()}/api/auth/request-approval`, {
      method: 'POST',
      body: JSON.stringify({ reason })
    });
    
    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '申请失败'
    };
  }
};

// 获取权限状态响应
export interface PermissionStatusResponse {
  success: boolean;
  isAdmin?: boolean;
  isApproved?: boolean;
  approvalStatus?: 'Pending' | 'Approved' | 'Rejected';
  rejectionReason?: string;
  approvalRequestedAt?: string;
  error?: string;
}

// 获取当前用户权限状态
export const getPermissionStatus = async (): Promise<PermissionStatusResponse> => {
  try {
    const response = await authFetch(`${getApiUrl()}/api/auth/permission-status`);
    
    if (!response.ok) {
      return { success: false, error: '获取权限状态失败' };
    }
    
    return await response.json();
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '获取权限状态失败'
    };
  }
};
