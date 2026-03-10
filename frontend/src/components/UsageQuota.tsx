import React from 'react';
import { UsageQuotaInfo, formatNextResetTime } from '../services/quotaService';
import { useQuota } from '../contexts/QuotaContext';
import './UsageQuota.css';

interface UsageQuotaProps {
  showDetailed?: boolean;
  className?: string;
  onQuotaLoaded?: (quota: UsageQuotaInfo) => void;
}

/**
 * 使用额度显示组件
 * 显示用户的AI功能使用额度信息
 */
export const UsageQuota: React.FC<UsageQuotaProps> = ({
  showDetailed = false,
  className = '',
}) => {
  const { quota, loading, error } = useQuota();
  if (loading) {
    return (
      <div className={`usage-quota ${className}`}>
        <div className="usage-quota-loading">加载中...</div>
      </div>
    );
  }

  if (error || !quota) {
    return (
      <div className={`usage-quota usage-quota-error ${className}`}>
        <span className="quota-icon">⚠️</span>
        <span>{error || '无法获取额度信息'}</span>
      </div>
    );
  }

  const usagePercentage = quota.weeklyQuota > 0 
    ? Math.min(100, (quota.weeklyUsedCount / quota.weeklyQuota) * 100) 
    : 0;
  
  const getStatusClass = () => {
    if (!quota.canUseAI) return 'quota-exhausted';
    if (quota.totalRemainingCount <= 2) return 'quota-low';
    if (quota.totalRemainingCount <= 5) return 'quota-medium';
    return 'quota-normal';
  };

  // 简洁模式：只显示剩余次数
  if (!showDetailed) {
    return (
      <div className={`usage-quota usage-quota-compact ${getStatusClass()} ${className}`}>
        <span className="quota-icon">🎫</span>
        <span className="quota-remaining">
          剩余 <strong>{quota.totalRemainingCount}</strong> 次
        </span>
        {quota.bonusCount > 0 && (
          <span className="quota-bonus" title="额外赠送次数">
            (+{quota.bonusCount})
          </span>
        )}
      </div>
    );
  }

  // 详细模式：显示完整信息
  return (
    <div className={`usage-quota usage-quota-detailed ${getStatusClass()} ${className}`}>
      <div className="quota-header">
        <span className="quota-icon">🎫</span>
        <span className="quota-title">使用额度</span>
        {!quota.canUseAI && (
          <span className="quota-warning">额度不足</span>
        )}
      </div>
      
      <div className="quota-progress">
        <div className="progress-bar">
          <div 
            className="progress-fill"
            style={{ width: `${usagePercentage}%` }}
          />
        </div>
        <div className="progress-text">
          本周已用 {quota.weeklyUsedCount} / {quota.weeklyQuota}
        </div>
      </div>

      <div className="quota-details">
        <div className="quota-item">
          <span className="quota-label">本周剩余</span>
          <span className="quota-value">{quota.weeklyRemainingCount} 次</span>
        </div>
        {quota.bonusCount > 0 && (
          <div className="quota-item quota-bonus-item">
            <span className="quota-label">额外次数</span>
            <span className="quota-value">+{quota.bonusCount} 次</span>
          </div>
        )}
        <div className="quota-item quota-total">
          <span className="quota-label">可用总计</span>
          <span className="quota-value">{quota.totalRemainingCount} 次</span>
        </div>
      </div>

      <div className="quota-footer">
        <span className="quota-reset">
          🔄 {formatNextResetTime(quota.nextResetAt)} 刷新
        </span>
      </div>
    </div>
  );
};

export default UsageQuota;
