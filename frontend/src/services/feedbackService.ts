import { getApiBaseUrl } from './config';
import { authStorage } from './authService';

// 获取 API 基础 URL
const getApiUrl = () => getApiBaseUrl();

/**
 * 反馈类型枚举
 */
export enum FeedbackType {
  Bug = 0,
  FeatureRequest = 1,
  Experience = 2,
  Other = 3,
}

/**
 * 反馈类型名称映射
 */
export const FeedbackTypeNames: Record<FeedbackType, string> = {
  [FeedbackType.Bug]: 'Bug报告',
  [FeedbackType.FeatureRequest]: '功能建议',
  [FeedbackType.Experience]: '使用体验',
  [FeedbackType.Other]: '其他',
};

/**
 * 反馈状态枚举
 */
export enum FeedbackStatus {
  Pending = 0,
  InProgress = 1,
  Resolved = 2,
  Closed = 3,
}

/**
 * 反馈状态名称映射
 */
export const FeedbackStatusNames: Record<FeedbackStatus, string> = {
  [FeedbackStatus.Pending]: '待处理',
  [FeedbackStatus.InProgress]: '处理中',
  [FeedbackStatus.Resolved]: '已解决',
  [FeedbackStatus.Closed]: '已关闭',
};

/**
 * 功能模块枚举（与后端对应）
 */
export enum FeatureModule {
  Chat = 1,
  Image = 2,
  Speech = 3,
  Legal = 4,
  Mystic = 5,
  RagChat = 6,
  Admin = 100,
  Other = 999,
}

/**
 * 功能模块名称映射
 */
export const FeatureModuleNames: Record<FeatureModule, string> = {
  [FeatureModule.Chat]: '聊天',
  [FeatureModule.Image]: '图片生成',
  [FeatureModule.Speech]: '语音',
  [FeatureModule.Legal]: '法律助手',
  [FeatureModule.Mystic]: '玄学助手',
  [FeatureModule.RagChat]: 'RAG聊天',
  [FeatureModule.Admin]: '管理功能',
  [FeatureModule.Other]: '其他',
};

/**
 * 创建反馈请求
 */
export interface CreateFeedbackRequest {
  type: FeedbackType;
  title: string;
  content: string;
  relatedModule?: FeatureModule;
  screenshots?: string[];
}

/**
 * 创建反馈响应
 */
export interface CreateFeedbackResponse {
  success: boolean;
  message?: string;
  error?: string;
  feedbackId?: string;
}

/**
 * 反馈详情信息
 */
export interface FeedbackDetail {
  id: string;
  userId: string;
  userEmail: string;
  userNickname?: string;
  type: FeedbackType;
  typeName: string;
  title: string;
  content: string;
  relatedModule?: FeatureModule;
  relatedModuleName?: string;
  screenshots?: string[];
  status: FeedbackStatus;
  statusName: string;
  adminResponse?: string;
  respondedAt?: string;
  createdAt: string;
  updatedAt: string;
}

/**
 * 反馈列表响应
 */
export interface FeedbackListResponse {
  success: boolean;
  error?: string;
  feedbacks: FeedbackDetail[];
  totalCount: number;
}

/**
 * 回复反馈请求
 */
export interface RespondFeedbackRequest {
  feedbackId: string;
  response: string;
  newStatus?: FeedbackStatus;
}

/**
 * 回复反馈响应
 */
export interface RespondFeedbackResponse {
  success: boolean;
  message?: string;
  error?: string;
}

/**
 * 反馈统计信息
 */
export interface FeedbackStatistics {
  totalCount: number;
  pendingCount: number;
  inProgressCount: number;
  resolvedCount: number;
  closedCount: number;
  byType: Record<string, number>;
}

/**
 * 反馈统计响应
 */
export interface FeedbackStatisticsResponse {
  success: boolean;
  error?: string;
  statistics?: FeedbackStatistics;
}

/**
 * 提交反馈
 */
export const createFeedback = async (
  request: CreateFeedbackRequest
): Promise<CreateFeedbackResponse> => {
  const token = authStorage.getToken();
  if (!token) {
    return { success: false, error: '请先登录' };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/feedback`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      return {
        success: false,
        error: errorData.error || '提交反馈失败',
      };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '提交反馈失败',
    };
  }
};

/**
 * 获取当前用户的反馈列表
 */
export const getMyFeedbacks = async (
  page: number = 1,
  pageSize: number = 20
): Promise<FeedbackListResponse> => {
  const token = authStorage.getToken();
  if (!token) {
    return { success: false, error: '请先登录', feedbacks: [], totalCount: 0 };
  }

  try {
    const response = await fetch(
      `${getApiUrl()}/api/feedback/my?page=${page}&pageSize=${pageSize}`,
      {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      }
    );

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      return {
        success: false,
        error: errorData.error || '获取反馈列表失败',
        feedbacks: [],
        totalCount: 0,
      };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '获取反馈列表失败',
      feedbacks: [],
      totalCount: 0,
    };
  }
};

/**
 * 获取反馈详情
 */
export const getFeedbackDetail = async (
  feedbackId: string
): Promise<FeedbackDetail | null> => {
  const token = authStorage.getToken();
  if (!token) {
    return null;
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/feedback/${feedbackId}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    if (!response.ok) {
      return null;
    }

    const data = await response.json();
    return data;
  } catch {
    return null;
  }
};

/**
 * 获取所有反馈列表（管理员）
 */
export const getAllFeedbacks = async (
  status?: FeedbackStatus,
  type?: FeedbackType,
  page: number = 1,
  pageSize: number = 20
): Promise<FeedbackListResponse> => {
  const token = authStorage.getToken();
  if (!token) {
    return { success: false, error: '请先登录', feedbacks: [], totalCount: 0 };
  }

  let url = `${getApiUrl()}/api/feedback/all?page=${page}&pageSize=${pageSize}`;
  if (status !== undefined) {
    url += `&status=${status}`;
  }
  if (type !== undefined) {
    url += `&type=${type}`;
  }

  try {
    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      return {
        success: false,
        error: errorData.error || '获取反馈列表失败',
        feedbacks: [],
        totalCount: 0,
      };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '获取反馈列表失败',
      feedbacks: [],
      totalCount: 0,
    };
  }
};

/**
 * 回复反馈（管理员）
 */
export const respondFeedback = async (
  request: RespondFeedbackRequest
): Promise<RespondFeedbackResponse> => {
  const token = authStorage.getToken();
  if (!token) {
    return { success: false, error: '请先登录' };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/feedback/respond`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      return {
        success: false,
        error: errorData.error || '回复反馈失败',
      };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '回复反馈失败',
    };
  }
};

/**
 * 更新反馈状态（管理员）
 */
export const updateFeedbackStatus = async (
  feedbackId: string,
  status: FeedbackStatus
): Promise<RespondFeedbackResponse> => {
  const token = authStorage.getToken();
  if (!token) {
    return { success: false, error: '请先登录' };
  }

  try {
    const response = await fetch(
      `${getApiUrl()}/api/feedback/${feedbackId}/status`,
      {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify({ feedbackId, status }),
      }
    );

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      return {
        success: false,
        error: errorData.error || '更新状态失败',
      };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '更新状态失败',
    };
  }
};

/**
 * 获取反馈统计信息（管理员）
 */
export const getFeedbackStatistics = async (): Promise<FeedbackStatisticsResponse> => {
  const token = authStorage.getToken();
  if (!token) {
    return { success: false, error: '请先登录' };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/feedback/statistics`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      return {
        success: false,
        error: errorData.error || '获取统计信息失败',
      };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '获取统计信息失败',
    };
  }
};

/**
 * 格式化日期
 */
export const formatDate = (dateString: string): string => {
  if (!dateString) return '未知';
  
  const date = new Date(dateString);
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
};
