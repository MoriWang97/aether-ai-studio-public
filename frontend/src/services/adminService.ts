import { getApiBaseUrl } from './config';
import { authStorage } from './authService';

// 获取 API 基础 URL
const getApiUrl = () => getApiBaseUrl();

// 审批状态枚举 (与后端 C# 枚举对应)
export enum ApprovalStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2
}

// 用户角色枚举 (与后端 C# 枚举对应)
export enum UserRoleEnum {
  User = 0,
  Admin = 1
}

// 待审批用户信息
export interface PendingUserInfo {
  id: string;
  email: string;
  nickname?: string;
  approvalRequestReason?: string;
  approvalRequestedAt?: string;
  createdAt: string;
  approvalStatus: ApprovalStatus;
}

// 获取待审批用户响应
export interface PendingUsersResponse {
  success: boolean;
  users: PendingUserInfo[];
  totalCount: number;
  error?: string;
}

// 审批用户请求
export interface ApproveUserRequest {
  userId: string;
  approve: boolean;
  rejectionReason?: string;
}

// 审批用户响应
export interface ApproveUserResponse {
  success: boolean;
  message?: string;
  error?: string;
}

// 检查管理员状态响应
export interface CheckAdminResponse {
  success: boolean;
  isAdmin: boolean;
  error?: string;
}

// 检查当前用户是否为管理员
export const checkAdminStatus = async (): Promise<CheckAdminResponse> => {
  const token = authStorage.getToken();
  if (!token) {
    return { success: false, isAdmin: false, error: '未登录' };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/admin/check-admin`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    if (response.status === 401) {
      return { success: false, isAdmin: false, error: '未授权' };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      isAdmin: false,
      error: error instanceof Error ? error.message : '检查管理员状态失败',
    };
  }
};

// 获取待审批用户列表
export const getPendingUsers = async (): Promise<PendingUsersResponse> => {
  const token = authStorage.getToken();
  if (!token) {
    return { success: false, users: [], totalCount: 0, error: '未登录' };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/admin/pending-users`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    if (response.status === 401) {
      return { success: false, users: [], totalCount: 0, error: '未授权' };
    }

    if (response.status === 403) {
      return { success: false, users: [], totalCount: 0, error: '需要管理员权限' };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      users: [],
      totalCount: 0,
      error: error instanceof Error ? error.message : '获取待审批用户失败',
    };
  }
};

// 用户详细信息（管理员查看）
export interface UserDetailInfo {
  id: string;
  email: string;
  nickname?: string;
  role: number;  // UserRoleEnum: 0=User, 1=Admin
  approvalStatus: number;  // ApprovalStatus: 0=Pending, 1=Approved, 2=Rejected
  approvalRequestReason?: string;
  approvalRequestedAt?: string;
  approvedAt?: string;
  rejectionReason?: string;
  createdAt: string;
  lastLoginAt: string;
}

// 获取所有用户响应
export interface AllUsersResponse {
  success: boolean;
  users: UserDetailInfo[];
  totalCount: number;
  error?: string;
}

// 撤销用户权限请求
export interface RevokeUserRequest {
  userId: string;
}

// 通用操作响应
export interface UserOperationResponse {
  success: boolean;
  message?: string;
  error?: string;
}

// 审批用户
export const approveUser = async (request: ApproveUserRequest): Promise<ApproveUserResponse> => {
  const token = authStorage.getToken();
  if (!token) {
    return { success: false, error: '未登录' };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/admin/approve-user`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (response.status === 401) {
      return { success: false, error: '未授权' };
    }

    if (response.status === 403) {
      return { success: false, error: '需要管理员权限' };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '审批操作失败',
    };
  }
};

// 获取所有用户列表
export const getAllUsers = async (page: number = 1, pageSize: number = 20): Promise<AllUsersResponse> => {
  const token = authStorage.getToken();
  if (!token) {
    return { success: false, users: [], totalCount: 0, error: '未登录' };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/admin/all-users?page=${page}&pageSize=${pageSize}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    if (response.status === 401) {
      return { success: false, users: [], totalCount: 0, error: '未授权' };
    }

    if (response.status === 403) {
      return { success: false, users: [], totalCount: 0, error: '需要管理员权限' };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      users: [],
      totalCount: 0,
      error: error instanceof Error ? error.message : '获取用户列表失败',
    };
  }
};

// 撤销用户权限
export const revokeUser = async (request: RevokeUserRequest): Promise<UserOperationResponse> => {
  const token = authStorage.getToken();
  if (!token) {
    return { success: false, error: '未登录' };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/admin/revoke-user`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (response.status === 401) {
      return { success: false, error: '未授权' };
    }

    if (response.status === 403) {
      return { success: false, error: '需要管理员权限' };
    }

    const data = await response.json();
    return data;
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '撤销权限失败',
    };
  }
};

// ==================== 使用统计相关 ====================

// 功能模块枚举
export enum FeatureModule {
  Chat = 1,
  Image = 2,
  Speech = 3,
  Legal = 4,
  Mystic = 5,
  RagChat = 6,
  Admin = 100,
  Other = 999
}

// 模块名称映射
export const ModuleNames: Record<FeatureModule, string> = {
  [FeatureModule.Chat]: '聊天',
  [FeatureModule.Image]: '图片生成',
  [FeatureModule.Speech]: '语音',
  [FeatureModule.Legal]: '法律助手',
  [FeatureModule.Mystic]: '玄学助手',
  [FeatureModule.RagChat]: '知识库聊天',
  [FeatureModule.Admin]: '管理功能',
  [FeatureModule.Other]: '其他'
};

// 统计分组方式
export enum StatisticsGroupBy {
  Hour = 'Hour',
  Day = 'Day',
  Week = 'Week',
  Month = 'Month'
}

// 模块使用统计
export interface ModuleUsageStats {
  module: FeatureModule;
  moduleName: string;
  requestCount: number;
  successCount: number;
  uniqueUsers: number;
  averageResponseTimeMs: number;
  percentage: number;
}

// 时间趋势统计
export interface TimeTrendStats {
  period: string;
  periodLabel: string;
  requestCount: number;
  activeUsers: number;
  successRate: number;
}

// 用户活跃度统计
export interface UserActivityStats {
  userId: string;
  email: string;
  nickname?: string;
  totalRequests: number;
  successfulRequests: number;
  lastActiveAt: string;
  modulesUsed: number;
  mostUsedModule: FeatureModule;
}

// 使用统计概览响应
export interface UsageStatisticsOverview {
  success: boolean;
  error?: string;
  totalRequests: number;
  successfulRequests: number;
  failedRequests: number;
  activeUsers: number;
  averageResponseTimeMs: number;
  moduleStats: ModuleUsageStats[];
  trendStats: TimeTrendStats[];
  topActiveUsers: UserActivityStats[];
  queryStartDate?: string;
  queryEndDate?: string;
}

// 使用日志项
export interface UsageLogItem {
  id: string;
  userId: string;
  userEmail?: string;
  userNickname?: string;
  module: FeatureModule;
  moduleName: string;
  action: string;
  requestPath?: string;
  httpMethod?: string;
  timestamp: string;
  isSuccess: boolean;
  statusCode?: number;
  responseTimeMs?: number;
}

// 使用日志列表响应
export interface UsageLogListResponse {
  success: boolean;
  error?: string;
  logs: UsageLogItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// 模块信息
export interface ModuleInfo {
  module: FeatureModule;
  name: string;
  description: string;
}

// 统计查询参数
export interface StatisticsQuery {
  startDate?: string;
  endDate?: string;
  userId?: string;
  module?: FeatureModule;
  groupBy?: StatisticsGroupBy;
  page?: number;
  pageSize?: number;
}

// 获取统计概览
export const getStatisticsOverview = async (query: StatisticsQuery = {}): Promise<UsageStatisticsOverview> => {
  const token = authStorage.getToken();
  if (!token) {
    return { 
      success: false, 
      error: '未登录',
      totalRequests: 0,
      successfulRequests: 0,
      failedRequests: 0,
      activeUsers: 0,
      averageResponseTimeMs: 0,
      moduleStats: [],
      trendStats: [],
      topActiveUsers: []
    };
  }

  try {
    const params = new URLSearchParams();
    if (query.startDate) params.append('startDate', query.startDate);
    if (query.endDate) params.append('endDate', query.endDate);
    if (query.userId) params.append('userId', query.userId);
    if (query.module !== undefined) params.append('module', query.module.toString());
    if (query.groupBy) params.append('groupBy', query.groupBy);

    const response = await fetch(`${getApiUrl()}/api/admin/statistics/overview?${params}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    if (response.status === 401 || response.status === 403) {
      return { 
        success: false, 
        error: response.status === 401 ? '未授权' : '需要管理员权限',
        totalRequests: 0,
        successfulRequests: 0,
        failedRequests: 0,
        activeUsers: 0,
        averageResponseTimeMs: 0,
        moduleStats: [],
        trendStats: [],
        topActiveUsers: []
      };
    }

    return await response.json();
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '获取统计数据失败',
      totalRequests: 0,
      successfulRequests: 0,
      failedRequests: 0,
      activeUsers: 0,
      averageResponseTimeMs: 0,
      moduleStats: [],
      trendStats: [],
      topActiveUsers: []
    };
  }
};

// 获取使用日志列表
export const getUsageLogs = async (query: StatisticsQuery = {}): Promise<UsageLogListResponse> => {
  const token = authStorage.getToken();
  if (!token) {
    return { success: false, error: '未登录', logs: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };
  }

  try {
    const params = new URLSearchParams();
    if (query.startDate) params.append('startDate', query.startDate);
    if (query.endDate) params.append('endDate', query.endDate);
    if (query.userId) params.append('userId', query.userId);
    if (query.module !== undefined) params.append('module', query.module.toString());
    if (query.page) params.append('page', query.page.toString());
    if (query.pageSize) params.append('pageSize', query.pageSize.toString());

    const response = await fetch(`${getApiUrl()}/api/admin/statistics/logs?${params}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    if (response.status === 401 || response.status === 403) {
      return { 
        success: false, 
        error: response.status === 401 ? '未授权' : '需要管理员权限',
        logs: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0
      };
    }

    return await response.json();
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : '获取日志失败',
      logs: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0
    };
  }
};

// 获取今日统计
export const getTodayStatistics = async (): Promise<UsageStatisticsOverview> => {
  const token = authStorage.getToken();
  if (!token) {
    return { 
      success: false, error: '未登录',
      totalRequests: 0, successfulRequests: 0, failedRequests: 0,
      activeUsers: 0, averageResponseTimeMs: 0,
      moduleStats: [], trendStats: [], topActiveUsers: []
    };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/admin/statistics/today`, {
      method: 'GET',
      headers: { 'Authorization': `Bearer ${token}` },
    });
    return await response.json();
  } catch (error) {
    return {
      success: false, error: error instanceof Error ? error.message : '获取今日统计失败',
      totalRequests: 0, successfulRequests: 0, failedRequests: 0,
      activeUsers: 0, averageResponseTimeMs: 0,
      moduleStats: [], trendStats: [], topActiveUsers: []
    };
  }
};

// 获取本周统计
export const getThisWeekStatistics = async (): Promise<UsageStatisticsOverview> => {
  const token = authStorage.getToken();
  if (!token) {
    return { 
      success: false, error: '未登录',
      totalRequests: 0, successfulRequests: 0, failedRequests: 0,
      activeUsers: 0, averageResponseTimeMs: 0,
      moduleStats: [], trendStats: [], topActiveUsers: []
    };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/admin/statistics/this-week`, {
      method: 'GET',
      headers: { 'Authorization': `Bearer ${token}` },
    });
    return await response.json();
  } catch (error) {
    return {
      success: false, error: error instanceof Error ? error.message : '获取本周统计失败',
      totalRequests: 0, successfulRequests: 0, failedRequests: 0,
      activeUsers: 0, averageResponseTimeMs: 0,
      moduleStats: [], trendStats: [], topActiveUsers: []
    };
  }
};

// 获取本月统计
export const getThisMonthStatistics = async (): Promise<UsageStatisticsOverview> => {
  const token = authStorage.getToken();
  if (!token) {
    return { 
      success: false, error: '未登录',
      totalRequests: 0, successfulRequests: 0, failedRequests: 0,
      activeUsers: 0, averageResponseTimeMs: 0,
      moduleStats: [], trendStats: [], topActiveUsers: []
    };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/admin/statistics/this-month`, {
      method: 'GET',
      headers: { 'Authorization': `Bearer ${token}` },
    });
    return await response.json();
  } catch (error) {
    return {
      success: false, error: error instanceof Error ? error.message : '获取本月统计失败',
      totalRequests: 0, successfulRequests: 0, failedRequests: 0,
      activeUsers: 0, averageResponseTimeMs: 0,
      moduleStats: [], trendStats: [], topActiveUsers: []
    };
  }
};

// 获取可用模块列表
export const getAvailableModules = async (): Promise<ModuleInfo[]> => {
  const token = authStorage.getToken();
  if (!token) return [];

  try {
    const response = await fetch(`${getApiUrl()}/api/admin/statistics/modules`, {
      method: 'GET',
      headers: { 'Authorization': `Bearer ${token}` },
    });
    return await response.json();
  } catch {
    return [];
  }
};
