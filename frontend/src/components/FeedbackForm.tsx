import React, { useState } from 'react';
import {
  createFeedback,
  getMyFeedbacks,
  FeedbackType,
  FeedbackTypeNames,
  FeedbackStatusNames,
  FeatureModule,
  FeatureModuleNames,
  FeedbackDetail,
  formatDate,
} from '../services/feedbackService';
import './FeedbackForm.css';

interface FeedbackFormProps {
  onClose?: () => void;
  onSubmitSuccess?: () => void;
}

/**
 * 反馈表单组件
 * 用户提交Bug报告、功能建议或使用体验
 */
export const FeedbackForm: React.FC<FeedbackFormProps> = ({
  onClose,
  onSubmitSuccess,
}) => {
  const [activeTab, setActiveTab] = useState<'submit' | 'history'>('submit');
  const [type, setType] = useState<FeedbackType>(FeedbackType.Experience);
  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [relatedModule, setRelatedModule] = useState<FeatureModule | undefined>(undefined);
  const [submitting, setSubmitting] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  // 历史记录状态
  const [feedbacks, setFeedbacks] = useState<FeedbackDetail[]>([]);
  const [loadingHistory, setLoadingHistory] = useState(false);
  const [historyLoaded, setHistoryLoaded] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!title.trim()) {
      setMessage({ type: 'error', text: '请输入标题' });
      return;
    }
    if (!content.trim()) {
      setMessage({ type: 'error', text: '请输入详细内容' });
      return;
    }

    setSubmitting(true);
    setMessage(null);

    try {
      const result = await createFeedback({
        type,
        title: title.trim(),
        content: content.trim(),
        relatedModule,
      });

      if (result.success) {
        setMessage({ type: 'success', text: result.message || '反馈提交成功！感谢您的反馈。' });
        setTitle('');
        setContent('');
        setRelatedModule(undefined);
        onSubmitSuccess?.();
        // 刷新历史记录
        setHistoryLoaded(false);
      } else {
        setMessage({ type: 'error', text: result.error || '提交失败，请稍后重试' });
      }
    } catch {
      setMessage({ type: 'error', text: '提交失败，请稍后重试' });
    } finally {
      setSubmitting(false);
    }
  };

  const loadHistory = async () => {
    if (historyLoaded) return;
    
    setLoadingHistory(true);
    try {
      const result = await getMyFeedbacks(1, 50);
      if (result.success) {
        setFeedbacks(result.feedbacks);
      }
    } catch {
      // ignore
    } finally {
      setLoadingHistory(false);
      setHistoryLoaded(true);
    }
  };

  const handleTabChange = (tab: 'submit' | 'history') => {
    setActiveTab(tab);
    if (tab === 'history' && !historyLoaded) {
      loadHistory();
    }
  };

  const getStatusBadgeClass = (status: number) => {
    switch (status) {
      case 0: return 'status-pending';
      case 1: return 'status-in-progress';
      case 2: return 'status-resolved';
      case 3: return 'status-closed';
      default: return '';
    }
  };

  const getTypeBadgeClass = (feedbackType: FeedbackType) => {
    switch (feedbackType) {
      case FeedbackType.Bug: return 'type-bug';
      case FeedbackType.FeatureRequest: return 'type-feature';
      case FeedbackType.Experience: return 'type-experience';
      default: return 'type-other';
    }
  };

  return (
    <div className="feedback-form-container">
      <div className="feedback-header">
        <h2>💬 用户反馈</h2>
        {onClose && (
          <button className="close-btn" onClick={onClose}>
            ✕
          </button>
        )}
      </div>

      <div className="feedback-tabs">
        <button
          className={`tab-btn ${activeTab === 'submit' ? 'active' : ''}`}
          onClick={() => handleTabChange('submit')}
        >
          📝 提交反馈
        </button>
        <button
          className={`tab-btn ${activeTab === 'history' ? 'active' : ''}`}
          onClick={() => handleTabChange('history')}
        >
          📋 我的反馈
        </button>
      </div>

      {activeTab === 'submit' && (
        <form onSubmit={handleSubmit} className="feedback-form">
          {message && (
            <div className={`feedback-message ${message.type}`}>
              {message.type === 'success' ? '✅' : '❌'} {message.text}
            </div>
          )}

          <div className="form-group">
            <label>反馈类型</label>
            <div className="type-selector">
              {Object.entries(FeedbackTypeNames).map(([value, name]) => (
                <button
                  key={value}
                  type="button"
                  className={`type-btn ${type === Number(value) ? 'selected' : ''} ${getTypeBadgeClass(Number(value) as FeedbackType)}`}
                  onClick={() => setType(Number(value) as FeedbackType)}
                >
                  {name}
                </button>
              ))}
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="feedback-title">标题 *</label>
            <input
              id="feedback-title"
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="简要描述您的反馈"
              maxLength={200}
              disabled={submitting}
            />
          </div>

          <div className="form-group">
            <label htmlFor="feedback-content">详细内容 *</label>
            <textarea
              id="feedback-content"
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder={type === FeedbackType.Bug 
                ? "请详细描述问题，包括：\n1. 问题现象\n2. 复现步骤\n3. 预期结果"
                : type === FeedbackType.FeatureRequest
                ? "请描述您期望的功能：\n1. 功能描述\n2. 使用场景\n3. 期望效果"
                : "请分享您的使用体验或心得..."
              }
              rows={6}
              maxLength={5000}
              disabled={submitting}
            />
            <div className="char-count">{content.length} / 5000</div>
          </div>

          <div className="form-group">
            <label htmlFor="feedback-module">相关功能（可选）</label>
            <select
              id="feedback-module"
              value={relatedModule ?? ''}
              onChange={(e) => setRelatedModule(e.target.value ? Number(e.target.value) as FeatureModule : undefined)}
              disabled={submitting}
            >
              <option value="">-- 请选择 --</option>
              {Object.entries(FeatureModuleNames)
                .filter(([value]) => Number(value) !== FeatureModule.Admin && Number(value) !== FeatureModule.Other)
                .map(([value, name]) => (
                  <option key={value} value={value}>{name}</option>
                ))}
            </select>
          </div>

          <div className="form-actions">
            <button
              type="submit"
              className="submit-btn"
              disabled={submitting}
            >
              {submitting ? '提交中...' : '提交反馈'}
            </button>
          </div>
        </form>
      )}

      {activeTab === 'history' && (
        <div className="feedback-history">
          {loadingHistory ? (
            <div className="loading">加载中...</div>
          ) : feedbacks.length === 0 ? (
            <div className="empty">
              <span className="empty-icon">📭</span>
              <p>您还没有提交过反馈</p>
            </div>
          ) : (
            <div className="feedback-list">
              {feedbacks.map((feedback) => (
                <div key={feedback.id} className="feedback-item">
                  <div className="feedback-item-header">
                    <span className={`type-badge ${getTypeBadgeClass(feedback.type)}`}>
                      {FeedbackTypeNames[feedback.type]}
                    </span>
                    <span className={`status-badge ${getStatusBadgeClass(feedback.status)}`}>
                      {FeedbackStatusNames[feedback.status]}
                    </span>
                  </div>
                  <h4 className="feedback-item-title">{feedback.title}</h4>
                  <p className="feedback-item-content">{feedback.content}</p>
                  <div className="feedback-item-meta">
                    <span className="meta-date">{formatDate(feedback.createdAt)}</span>
                    {feedback.relatedModuleName && (
                      <span className="meta-module">📂 {feedback.relatedModuleName}</span>
                    )}
                  </div>
                  {feedback.adminResponse && (
                    <div className="admin-response">
                      <div className="response-label">📬 管理员回复：</div>
                      <p>{feedback.adminResponse}</p>
                      {feedback.respondedAt && (
                        <span className="response-date">{formatDate(feedback.respondedAt)}</span>
                      )}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default FeedbackForm;
