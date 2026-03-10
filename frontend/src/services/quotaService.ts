import { getApiBaseUrl } from './config';
import { authStorage } from './authService';

// 获取 API 基础 URL
const getApiUrl = () => getApiBaseUrl();

/**
 * 使用额度信息
 */
export interface UsageQuotaInfo {
  success: boolean;
  error?: string;
  weeklyQuota: number;
  weeklyUsedCount: number;
  weeklyRemainingCount: number;
  bonusCount: number;
  totalRemainingCount: number;
  nextResetAt: string;
  canUseAI: boolean;
}

/**
 * 额度检查结果
 */
export interface QuotaCheckResult {
  canUse: boolean;
  denyReason?: string;
  remainingCount: number;
}

/**
 * 用户额度详情（管理员查看）
 */
export interface UserQuotaDetail {
  userId: string;
  email: string;
  nickname?: string;
  weeklyUsedCount: number;
  weeklyRemainingCount: number;
  bonusCount: number;
  totalRemainingCount: number;
  lastUsedAt?: string;
}

/**
 * 所有用户额度响应
 */
export interface AllUserQuotasResponse {
  success: boolean;
  error?: string;
  users: UserQuotaDetail[];
  totalCount: number;
}

/**
 * 赋予额度请求
 */
export interface GrantBonusQuotaRequest {
  userId: string;
  bonusCount: number;
  reason?: string;
}

/**
 * 赋予额度响应
 */
export interface GrantBonusQuotaResponse {
  success: boolean;
  message?: string;
  error?: string;
  currentBonusCount: number;
}

/**
 * 获取当前用户的使用额度信息
 */
export const getMyQuota = async (): Promise<UsageQuotaInfo> => {
  const token = authStorage.getToken();
  if (!token) {
    return {
      success: false,
      error: '请先登录',
      weeklyQuota: 10,
      weeklyUsedCount: 0,
      weeklyRemainingCount: 0,
      bonusCount: 0,
      totalRemainingCount: 0,
      nextResetAt: '',
      canUseAI: false,
    };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/quota`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      return {
        success: false,
        error: errorData.error || '获取额度信息失败',
        weeklyQuota: 10,
        weeklyUsedCount: 0,
        weeklyRemainingCount: 0,
        bonusCount: 0,
        totalRemainingCount: 0,
        nextResetAt: '',
        canUseAI: false,
      };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '获取额度信息失败',
      weeklyQuota: 10,
      weeklyUsedCount: 0,
      weeklyRemainingCount: 0,
      bonusCount: 0,
      totalRemainingCount: 0,
      nextResetAt: '',
      canUseAI: false,
    };
  }
};

/**
 * 检查当前用户是否可以使用AI功能
 */
export const checkQuota = async (): Promise<QuotaCheckResult> => {
  const token = authStorage.getToken();
  if (!token) {
    return {
      canUse: false,
      denyReason: '请先登录',
      remainingCount: 0,
    };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/quota/check`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      return {
        canUse: false,
        denyReason: errorData.error || '检查额度失败',
        remainingCount: 0,
      };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      canUse: false,
      denyReason: error instanceof Error ? error.message : '检查额度失败',
      remainingCount: 0,
    };
  }
};

/**
 * 获取所有用户的额度信息（管理员）
 */
export const getAllUserQuotas = async (
  page: number = 1,
  pageSize: number = 20
): Promise<AllUserQuotasResponse> => {
  const token = authStorage.getToken();
  if (!token) {
    return { success: false, error: '请先登录', users: [], totalCount: 0 };
  }

  try {
    const response = await fetch(
      `${getApiUrl()}/api/admin/quotas?page=${page}&pageSize=${pageSize}`,
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
        error: errorData.error || '获取用户额度列表失败',
        users: [],
        totalCount: 0,
      };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '获取用户额度列表失败',
      users: [],
      totalCount: 0,
    };
  }
};

/**
 * 为用户赋予额外使用次数（管理员）
 */
export const grantBonusQuota = async (
  request: GrantBonusQuotaRequest
): Promise<GrantBonusQuotaResponse> => {
  const token = authStorage.getToken();
  if (!token) {
    return { success: false, error: '请先登录', currentBonusCount: 0 };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/admin/quotas/grant`, {
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
        error: errorData.error || '赋予额外次数失败',
        currentBonusCount: 0,
      };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '赋予额外次数失败',
      currentBonusCount: 0,
    };
  }
};

/**
 * 格式化下次刷新时间
 */
export const formatNextResetTime = (nextResetAt: string): string => {
  if (!nextResetAt) return '未知';
  
  const nextReset = new Date(nextResetAt);
  const now = new Date();
  const diffMs = nextReset.getTime() - now.getTime();
  
  if (diffMs <= 0) {
    return '即将刷新';
  }
  
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
  const diffHours = Math.floor((diffMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
  
  if (diffDays > 0) {
    return `${diffDays}天${diffHours}小时后`;
  } else if (diffHours > 0) {
    return `${diffHours}小时后`;
  } else {
    const diffMinutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));
    return `${diffMinutes}分钟后`;
  }
};
