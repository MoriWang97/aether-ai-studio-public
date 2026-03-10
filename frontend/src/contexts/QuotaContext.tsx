import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';
import { getMyQuota, UsageQuotaInfo } from '../services/quotaService';
import { useAuth } from './AuthContext';

// 定义刷新事件名称
export const QUOTA_REFRESH_EVENT = 'quota-refresh';

// 全局触发额度刷新的函数
export const triggerQuotaRefresh = () => {
  window.dispatchEvent(new CustomEvent(QUOTA_REFRESH_EVENT));
};

interface QuotaContextType {
  quota: UsageQuotaInfo | null;
  loading: boolean;
  error: string | null;
  refreshQuota: () => Promise<void>;
}

const QuotaContext = createContext<QuotaContextType | undefined>(undefined);

export const QuotaProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isLoggedIn, user } = useAuth();
  const [quota, setQuota] = useState<UsageQuotaInfo | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const refreshQuota = useCallback(async () => {
    // 管理员不需要显示额度
    if (!isLoggedIn || user?.isAdmin) {
      setQuota(null);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const result = await getMyQuota();
      if (result.success) {
        setQuota(result);
      } else {
        setError(result.error || '获取额度信息失败');
      }
    } catch (err) {
      setError('获取额度信息失败');
    } finally {
      setLoading(false);
    }
  }, [isLoggedIn, user?.isAdmin]);

  // 登录状态变化时刷新额度
  useEffect(() => {
    if (isLoggedIn && !user?.isAdmin) {
      refreshQuota();
      // 每分钟刷新一次
      const interval = setInterval(refreshQuota, 60000);
      return () => clearInterval(interval);
    } else {
      setQuota(null);
    }
  }, [isLoggedIn, user?.isAdmin, refreshQuota]);

  // 监听全局刷新事件
  useEffect(() => {
    const handleRefresh = () => {
      refreshQuota();
    };

    window.addEventListener(QUOTA_REFRESH_EVENT, handleRefresh);
    return () => {
      window.removeEventListener(QUOTA_REFRESH_EVENT, handleRefresh);
    };
  }, [refreshQuota]);

  return (
    <QuotaContext.Provider value={{ quota, loading, error, refreshQuota }}>
      {children}
    </QuotaContext.Provider>
  );
};

export const useQuota = (): QuotaContextType => {
  const context = useContext(QuotaContext);
  if (context === undefined) {
    throw new Error('useQuota must be used within a QuotaProvider');
  }
  return context;
};

export default QuotaContext;
