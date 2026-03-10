import React, { useState, useEffect, useCallback } from 'react';
import {
  getAllUserQuotas,
  grantBonusQuota,
  UserQuotaDetail,
} from '../services/quotaService';
import './QuotaManagement.css';

interface QuotaManagementProps {
  onRefreshNeeded?: () => void;
}

/**
 * 用户额度管理组件（管理员用）
 */
const QuotaManagement: React.FC<QuotaManagementProps> = ({ onRefreshNeeded }) => {
  const [users, setUsers] = useState<UserQuotaDetail[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 20;

  // 赋予额度弹窗状态
  const [showGrantModal, setShowGrantModal] = useState<UserQuotaDetail | null>(null);
  const [grantAmount, setGrantAmount] = useState(10);
  const [grantReason, setGrantReason] = useState('');
  const [granting, setGranting] = useState(false);
  const [grantMessage, setGrantMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const loadUsers = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await getAllUserQuotas(page, pageSize);
      if (result.success) {
        setUsers(result.users);
        setTotalCount(result.totalCount);
      } else {
        setError(result.error || '加载失败');
      }
    } catch (err) {
      setError('加载失败');
    } finally {
      setLoading(false);
    }
  }, [page]);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  const handleGrant = async () => {
    if (!showGrantModal) return;
    
    setGranting(true);
    setGrantMessage(null);
    try {
      const result = await grantBonusQuota({
        userId: showGrantModal.userId,
        bonusCount: grantAmount,
        reason: grantReason || undefined,
      });
      
      if (result.success) {
        setGrantMessage({ type: 'success', text: result.message || '赋予成功' });
        // 刷新列表
        loadUsers();
        onRefreshNeeded?.();
        // 延迟关闭
        setTimeout(() => {
          setShowGrantModal(null);
          setGrantAmount(10);
          setGrantReason('');
          setGrantMessage(null);
        }, 1500);
      } else {
        setGrantMessage({ type: 'error', text: result.error || '赋予失败' });
      }
    } catch (err) {
      setGrantMessage({ type: 'error', text: '赋予失败' });
    } finally {
      setGranting(false);
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="quota-management">
      <div className="quota-management-header">
        <h4>用户额度管理</h4>
        <button className="refresh-btn" onClick={loadUsers} disabled={loading}>
          🔄 刷新
        </button>
      </div>

      {loading && (
        <div className="loading-state">
          <span className="spinner"></span>
          加载中...
        </div>
      )}

      {error && (
        <div className="error-state">
          ⚠️ {error}
          <button onClick={loadUsers}>重试</button>
        </div>
      )}

      {!loading && !error && (
        <>
          <div className="quota-table-container">
            <table className="quota-table">
              <thead>
                <tr>
                  <th>用户</th>
                  <th>本周使用</th>
                  <th>本周剩余</th>
                  <th>额外次数</th>
                  <th>总可用</th>
                  <th>操作</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user) => (
                  <tr key={user.userId}>
                    <td className="user-cell">
                      <div className="user-email">{user.email}</div>
                      {user.nickname && (
                        <div className="user-nickname">{user.nickname}</div>
                      )}
                    </td>
                    <td className="number-cell">{user.weeklyUsedCount}</td>
                    <td className="number-cell">{user.weeklyRemainingCount}</td>
                    <td className="number-cell bonus">{user.bonusCount}</td>
                    <td className={`number-cell total ${user.totalRemainingCount <= 0 ? 'exhausted' : ''}`}>
                      {user.totalRemainingCount}
                    </td>
                    <td className="action-cell">
                      <button
                        className="grant-btn"
                        onClick={() => setShowGrantModal(user)}
                      >
                        ➕ 赋予
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {totalPages > 1 && (
            <div className="pagination">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
              >
                ← 上一页
              </button>
              <span className="page-info">
                第 {page} 页 / 共 {totalPages} 页
              </span>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
              >
                下一页 →
              </button>
            </div>
          )}
        </>
      )}

      {/* 赋予额度弹窗 */}
      {showGrantModal && (
        <div className="grant-modal-overlay" onClick={() => !granting && setShowGrantModal(null)}>
          <div className="grant-modal" onClick={e => e.stopPropagation()}>
            <h4>赋予额外次数</h4>
            <div className="grant-user-info">
              用户: <strong>{showGrantModal.email}</strong>
            </div>
            <div className="grant-current-info">
              当前额外次数: <strong>{showGrantModal.bonusCount}</strong> 次
            </div>
            
            {grantMessage && (
              <div className={`grant-message ${grantMessage.type}`}>
                {grantMessage.type === 'success' ? '✅' : '❌'} {grantMessage.text}
              </div>
            )}
            
            <div className="grant-form-group">
              <label>赋予次数</label>
              <input
                type="number"
                min="1"
                max="10000"
                value={grantAmount}
                onChange={(e) => setGrantAmount(Math.max(1, parseInt(e.target.value) || 1))}
                disabled={granting}
              />
            </div>
            
            <div className="grant-form-group">
              <label>备注（可选）</label>
              <textarea
                placeholder="例如：活动奖励、VIP用户等"
                value={grantReason}
                onChange={(e) => setGrantReason(e.target.value)}
                rows={2}
                disabled={granting}
              />
            </div>
            
            <div className="modal-actions">
              <button
                className="cancel-btn"
                onClick={() => setShowGrantModal(null)}
                disabled={granting}
              >
                取消
              </button>
              <button
                className="confirm-grant-btn"
                onClick={handleGrant}
                disabled={granting}
              >
                {granting ? '处理中...' : '确认赋予'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default QuotaManagement;
