import React, { useState, useEffect, useCallback } from 'react';
import { 
  getAllUsers, 
  revokeUser, 
  approveUser,
  UserDetailInfo, 
  ApprovalStatus,
  UserRoleEnum
} from '../services/adminService';
import './UserManagement.css';

interface UserManagementProps {
  onRefreshNeeded?: () => void;
}

const UserManagement: React.FC<UserManagementProps> = ({ onRefreshNeeded }) => {
  const [users, setUsers] = useState<UserDetailInfo[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [processingUserId, setProcessingUserId] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<number | 'All'>('All');
  const [confirmAction, setConfirmAction] = useState<{userId: string; action: 'revoke' | 'approve'} | null>(null);
  
  const pageSize = 15;

  // 加载用户列表
  const loadUsers = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await getAllUsers(page, pageSize);
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

  // 撤销用户权限
  const handleRevoke = async (userId: string) => {
    setProcessingUserId(userId);
    try {
      const result = await revokeUser({ userId });
      if (result.success) {
        // 重新加载用户列表
        await loadUsers();
        onRefreshNeeded?.();
      } else {
        setError(result.error || '撤销权限失败');
      }
    } catch (err) {
      setError('撤销权限失败');
    } finally {
      setProcessingUserId(null);
      setConfirmAction(null);
    }
  };

  // 快速批准用户
  const handleQuickApprove = async (userId: string) => {
    setProcessingUserId(userId);
    try {
      const result = await approveUser({ userId, approve: true });
      if (result.success) {
        await loadUsers();
        onRefreshNeeded?.();
      } else {
        setError(result.error || '批准失败');
      }
    } catch (err) {
      setError('批准失败');
    } finally {
      setProcessingUserId(null);
      setConfirmAction(null);
    }
  };

  // 格式化时间
  const formatTime = (dateString?: string) => {
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

  // 获取状态标签
  const getStatusBadge = (status: number) => {
    switch (status) {
      case ApprovalStatus.Approved:
        return <span className="status-badge approved">已批准</span>;
      case ApprovalStatus.Pending:
        return <span className="status-badge pending">待审批</span>;
      case ApprovalStatus.Rejected:
        return <span className="status-badge rejected">已拒绝</span>;
      default:
        return <span className="status-badge">未知</span>;
    }
  };

  // 获取角色标签
  const getRoleBadge = (role: number) => {
    if (role === UserRoleEnum.Admin) {
      return <span className="role-badge admin">管理员</span>;
    }
    return <span className="role-badge user">普通用户</span>;
  };

  // 过滤用户
  const filteredUsers = users.filter(user => {
    const matchesSearch = searchQuery === '' || 
      user.email.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (user.nickname && user.nickname.toLowerCase().includes(searchQuery.toLowerCase()));
    
    const matchesStatus = statusFilter === 'All' || user.approvalStatus === statusFilter;
    
    return matchesSearch && matchesStatus;
  });

  // 计算总页数
  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="user-management">
      {/* 工具栏 */}
      <div className="management-toolbar">
        <div className="search-box">
          <input
            type="text"
            placeholder="搜索邮箱或昵称..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
          <span className="search-icon">🔍</span>
        </div>
        
        <div className="filter-box">
          <select 
            value={statusFilter} 
            onChange={(e) => {
              const val = e.target.value;
              setStatusFilter(val === 'All' ? 'All' : Number(val));
            }}
          >
            <option value="All">全部状态</option>
            <option value={ApprovalStatus.Approved}>已批准</option>
            <option value={ApprovalStatus.Pending}>待审批</option>
            <option value={ApprovalStatus.Rejected}>已拒绝</option>
          </select>
        </div>

        <button className="refresh-btn" onClick={loadUsers} disabled={loading}>
          🔄 刷新
        </button>
      </div>

      {/* 统计信息 */}
      <div className="stats-bar">
        <span className="stat-item">
          共 <strong>{totalCount}</strong> 个用户
        </span>
        <span className="stat-item">
          显示 <strong>{filteredUsers.length}</strong> 个
        </span>
      </div>

      {/* 错误提示 */}
      {error && (
        <div className="error-banner">
          ⚠️ {error}
          <button onClick={() => setError(null)}>×</button>
        </div>
      )}

      {/* 加载状态 */}
      {loading && (
        <div className="loading-overlay">
          <span className="spinner"></span>
          加载中...
        </div>
      )}

      {/* 用户列表 */}
      <div className="user-table-container">
        <table className="user-table">
          <thead>
            <tr>
              <th>用户</th>
              <th>角色</th>
              <th>状态</th>
              <th>注册时间</th>
              <th>最后登录</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            {filteredUsers.length === 0 && !loading && (
              <tr>
                <td colSpan={6} className="empty-row">
                  {searchQuery || statusFilter !== 'All' 
                    ? '没有匹配的用户' 
                    : '暂无用户数据'}
                </td>
              </tr>
            )}
            {filteredUsers.map(user => (
              <tr key={user.id} className={user.role === UserRoleEnum.Admin ? 'admin-row' : ''}>
                <td className="user-cell">
                  <div className="user-info">
                    <span className="user-email">{user.email}</span>
                    {user.nickname && (
                      <span className="user-nickname">{user.nickname}</span>
                    )}
                  </div>
                </td>
                <td>{getRoleBadge(user.role)}</td>
                <td>{getStatusBadge(user.approvalStatus)}</td>
                <td className="time-cell">{formatTime(user.createdAt)}</td>
                <td className="time-cell">{formatTime(user.lastLoginAt)}</td>
                <td className="action-cell">
                  {user.role !== UserRoleEnum.Admin && (
                    <div className="action-buttons">
                      {user.approvalStatus === ApprovalStatus.Approved && (
                        <button
                          className="revoke-btn"
                          onClick={() => setConfirmAction({ userId: user.id, action: 'revoke' })}
                          disabled={processingUserId === user.id}
                          title="撤销权限"
                        >
                          {processingUserId === user.id ? '处理中...' : '🚫 撤销'}
                        </button>
                      )}
                      {(user.approvalStatus === ApprovalStatus.Pending || user.approvalStatus === ApprovalStatus.Rejected) && (
                        <button
                          className="quick-approve-btn"
                          onClick={() => setConfirmAction({ userId: user.id, action: 'approve' })}
                          disabled={processingUserId === user.id}
                          title="批准用户"
                        >
                          {processingUserId === user.id ? '处理中...' : '✓ 批准'}
                        </button>
                      )}
                    </div>
                  )}
                  {user.role === UserRoleEnum.Admin && (
                    <span className="admin-label">管理员账户</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* 分页控件 */}
      {totalPages > 1 && (
        <div className="pagination">
          <button 
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1 || loading}
          >
            ← 上一页
          </button>
          <span className="page-info">
            第 {page} 页 / 共 {totalPages} 页
          </span>
          <button 
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page === totalPages || loading}
          >
            下一页 →
          </button>
        </div>
      )}

      {/* 确认弹窗 */}
      {confirmAction && (
        <div className="confirm-modal-overlay" onClick={() => setConfirmAction(null)}>
          <div className="confirm-modal" onClick={e => e.stopPropagation()}>
            <h4>
              {confirmAction.action === 'revoke' ? '确认撤销权限' : '确认批准用户'}
            </h4>
            <p>
              {confirmAction.action === 'revoke' 
                ? '撤销后该用户将无法使用AI功能，需要重新申请。确定要撤销吗？'
                : '批准后该用户将可以使用AI功能。确定要批准吗？'}
            </p>
            <div className="modal-actions">
              <button
                className="cancel-btn"
                onClick={() => setConfirmAction(null)}
              >
                取消
              </button>
              <button
                className={confirmAction.action === 'revoke' ? 'confirm-revoke-btn' : 'confirm-approve-btn'}
                onClick={() => {
                  if (confirmAction.action === 'revoke') {
                    handleRevoke(confirmAction.userId);
                  } else {
                    handleQuickApprove(confirmAction.userId);
                  }
                }}
                disabled={processingUserId === confirmAction.userId}
              >
                {processingUserId === confirmAction.userId 
                  ? '处理中...' 
                  : (confirmAction.action === 'revoke' ? '确认撤销' : '确认批准')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default UserManagement;
