import React, { useState, useEffect, useCallback } from 'react';
import {
  getAllFeedbacks,
  respondFeedback,
  updateFeedbackStatus,
  getFeedbackStatistics,
  FeedbackType,
  FeedbackTypeNames,
  FeedbackStatus,
  FeedbackStatusNames,
  FeedbackDetail,
  FeedbackStatistics,
  formatDate,
} from '../services/feedbackService';
import './FeedbackManagement.css';

/**
 * 反馈管理组件（管理员用）
 */
const FeedbackManagement: React.FC = () => {
  const [feedbacks, setFeedbacks] = useState<FeedbackDetail[]>([]);
  const [statistics, setStatistics] = useState<FeedbackStatistics | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 20;

  // 筛选条件
  const [filterStatus, setFilterStatus] = useState<FeedbackStatus | undefined>(undefined);
  const [filterType, setFilterType] = useState<FeedbackType | undefined>(undefined);

  // 响应弹窗状态
  const [showRespondModal, setShowRespondModal] = useState<FeedbackDetail | null>(null);
  const [responseText, setResponseText] = useState('');
  const [responseStatus, setResponseStatus] = useState<FeedbackStatus | undefined>(undefined);
  const [responding, setResponding] = useState(false);
  const [respondMessage, setRespondMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const loadData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [feedbackResult, statsResult] = await Promise.all([
        getAllFeedbacks(filterStatus, filterType, page, pageSize),
        getFeedbackStatistics(),
      ]);
      
      if (feedbackResult.success) {
        setFeedbacks(feedbackResult.feedbacks);
        setTotalCount(feedbackResult.totalCount);
      } else {
        setError(feedbackResult.error || '加载失败');
      }
      
      if (statsResult.success && statsResult.statistics) {
        setStatistics(statsResult.statistics);
      }
    } catch (err) {
      setError('加载失败');
    } finally {
      setLoading(false);
    }
  }, [page, filterStatus, filterType]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleRespond = async () => {
    if (!showRespondModal || !responseText.trim()) return;
    
    setResponding(true);
    setRespondMessage(null);
    try {
      const result = await respondFeedback({
        feedbackId: showRespondModal.id,
        response: responseText.trim(),
        newStatus: responseStatus,
      });
      
      if (result.success) {
        setRespondMessage({ type: 'success', text: '回复成功' });
        await loadData();
        setTimeout(() => {
          setShowRespondModal(null);
          setResponseText('');
          setResponseStatus(undefined);
          setRespondMessage(null);
        }, 1500);
      } else {
        setRespondMessage({ type: 'error', text: result.error || '回复失败' });
      }
    } catch (err) {
      setRespondMessage({ type: 'error', text: '回复失败' });
    } finally {
      setResponding(false);
    }
  };

  const handleStatusChange = async (feedbackId: string, newStatus: FeedbackStatus) => {
    try {
      const result = await updateFeedbackStatus(feedbackId, newStatus);
      if (result.success) {
        loadData();
      }
    } catch (err) {
      // ignore
    }
  };

  const getTypeBadgeClass = (type: FeedbackType) => {
    switch (type) {
      case FeedbackType.Bug: return 'type-bug';
      case FeedbackType.FeatureRequest: return 'type-feature';
      case FeedbackType.Experience: return 'type-experience';
      default: return 'type-other';
    }
  };

  const getStatusBadgeClass = (status: FeedbackStatus) => {
    switch (status) {
      case FeedbackStatus.Pending: return 'status-pending';
      case FeedbackStatus.InProgress: return 'status-in-progress';
      case FeedbackStatus.Resolved: return 'status-resolved';
      case FeedbackStatus.Closed: return 'status-closed';
      default: return '';
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="feedback-management">
      {/* 统计概览 */}
      {statistics && (
        <div className="feedback-stats">
          <div className="stat-item total">
            <span className="stat-value">{statistics.totalCount}</span>
            <span className="stat-label">总反馈</span>
          </div>
          <div className="stat-item pending">
            <span className="stat-value">{statistics.pendingCount}</span>
            <span className="stat-label">待处理</span>
          </div>
          <div className="stat-item in-progress">
            <span className="stat-value">{statistics.inProgressCount}</span>
            <span className="stat-label">处理中</span>
          </div>
          <div className="stat-item resolved">
            <span className="stat-value">{statistics.resolvedCount}</span>
            <span className="stat-label">已解决</span>
          </div>
        </div>
      )}

      {/* 筛选器 */}
      <div className="feedback-filters">
        <div className="filter-group">
          <label>状态筛选:</label>
          <select
            value={filterStatus ?? ''}
            onChange={(e) => {
              setFilterStatus(e.target.value ? Number(e.target.value) as FeedbackStatus : undefined);
              setPage(1);
            }}
          >
            <option value="">全部</option>
            {Object.entries(FeedbackStatusNames).map(([value, name]) => (
              <option key={value} value={value}>{name}</option>
            ))}
          </select>
        </div>
        <div className="filter-group">
          <label>类型筛选:</label>
          <select
            value={filterType ?? ''}
            onChange={(e) => {
              setFilterType(e.target.value ? Number(e.target.value) as FeedbackType : undefined);
              setPage(1);
            }}
          >
            <option value="">全部</option>
            {Object.entries(FeedbackTypeNames).map(([value, name]) => (
              <option key={value} value={value}>{name}</option>
            ))}
          </select>
        </div>
        <button className="refresh-btn" onClick={loadData} disabled={loading}>
          🔄 刷新
        </button>
      </div>

      {/* 列表 */}
      {loading && (
        <div className="loading-state">
          <span className="spinner"></span>
          加载中...
        </div>
      )}

      {error && (
        <div className="error-state">
          ⚠️ {error}
          <button onClick={loadData}>重试</button>
        </div>
      )}

      {!loading && !error && feedbacks.length === 0 && (
        <div className="empty-state">
          📭 没有反馈记录
        </div>
      )}

      {!loading && !error && feedbacks.length > 0 && (
        <>
          <div className="feedback-list">
            {feedbacks.map((feedback) => (
              <div key={feedback.id} className="feedback-card">
                <div className="feedback-card-header">
                  <span className={`type-badge ${getTypeBadgeClass(feedback.type)}`}>
                    {FeedbackTypeNames[feedback.type]}
                  </span>
                  <select
                    className={`status-select ${getStatusBadgeClass(feedback.status)}`}
                    value={feedback.status}
                    onChange={(e) => handleStatusChange(feedback.id, Number(e.target.value) as FeedbackStatus)}
                  >
                    {Object.entries(FeedbackStatusNames).map(([value, name]) => (
                      <option key={value} value={value}>{name}</option>
                    ))}
                  </select>
                </div>
                
                <h4 className="feedback-title">{feedback.title}</h4>
                
                <div className="feedback-user">
                  <span className="user-email">{feedback.userEmail}</span>
                  {feedback.userNickname && (
                    <span className="user-nickname">({feedback.userNickname})</span>
                  )}
                </div>
                
                <p className="feedback-content">{feedback.content}</p>
                
                <div className="feedback-meta">
                  <span>{formatDate(feedback.createdAt)}</span>
                  {feedback.relatedModuleName && (
                    <span>📂 {feedback.relatedModuleName}</span>
                  )}
                </div>

                {feedback.adminResponse && (
                  <div className="admin-response">
                    <div className="response-label">已回复:</div>
                    <p>{feedback.adminResponse}</p>
                    {feedback.respondedAt && (
                      <span className="response-time">{formatDate(feedback.respondedAt)}</span>
                    )}
                  </div>
                )}

                <div className="feedback-card-actions">
                  <button
                    className="respond-btn"
                    onClick={() => {
                      setShowRespondModal(feedback);
                      setResponseText(feedback.adminResponse || '');
                      setResponseStatus(feedback.status);
                    }}
                  >
                    💬 {feedback.adminResponse ? '修改回复' : '回复'}
                  </button>
                </div>
              </div>
            ))}
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

      {/* 回复弹窗 */}
      {showRespondModal && (
        <div className="respond-modal-overlay" onClick={() => !responding && setShowRespondModal(null)}>
          <div className="respond-modal" onClick={e => e.stopPropagation()}>
            <h4>回复反馈</h4>
            
            <div className="respond-feedback-info">
              <span className={`type-badge ${getTypeBadgeClass(showRespondModal.type)}`}>
                {FeedbackTypeNames[showRespondModal.type]}
              </span>
              <strong>{showRespondModal.title}</strong>
            </div>
            
            <div className="respond-feedback-content">
              {showRespondModal.content}
            </div>
            
            {respondMessage && (
              <div className={`respond-message ${respondMessage.type}`}>
                {respondMessage.type === 'success' ? '✅' : '❌'} {respondMessage.text}
              </div>
            )}
            
            <div className="respond-form-group">
              <label>回复内容 *</label>
              <textarea
                value={responseText}
                onChange={(e) => setResponseText(e.target.value)}
                rows={4}
                placeholder="输入您的回复..."
                disabled={responding}
              />
            </div>
            
            <div className="respond-form-group">
              <label>更新状态</label>
              <select
                value={responseStatus ?? showRespondModal.status}
                onChange={(e) => setResponseStatus(Number(e.target.value) as FeedbackStatus)}
                disabled={responding}
              >
                {Object.entries(FeedbackStatusNames).map(([value, name]) => (
                  <option key={value} value={value}>{name}</option>
                ))}
              </select>
            </div>
            
            <div className="modal-actions">
              <button
                className="cancel-btn"
                onClick={() => setShowRespondModal(null)}
                disabled={responding}
              >
                取消
              </button>
              <button
                className="confirm-respond-btn"
                onClick={handleRespond}
                disabled={responding || !responseText.trim()}
              >
                {responding ? '处理中...' : '发送回复'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default FeedbackManagement;
