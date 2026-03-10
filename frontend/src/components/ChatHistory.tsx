import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { 
  ChatSessionDto, 
  getChatSessions, 
  deleteChatSession,
  updateSessionTitle
} from '../services/authService';
import './ChatHistory.css';

interface ChatHistoryProps {
  currentSessionId?: string;
  onSelectSession: (sessionId: string | null) => void;
  onNewChat: () => void;
  refreshTrigger?: number;
}

const ChatHistory: React.FC<ChatHistoryProps> = ({ 
  currentSessionId, 
  onSelectSession, 
  onNewChat,
  refreshTrigger 
}) => {
  const { isLoggedIn, user } = useAuth();
  const [sessions, setSessions] = useState<ChatSessionDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editTitle, setEditTitle] = useState('');

  // 加载会话列表
  const loadSessions = useCallback(async () => {
    if (!isLoggedIn) return;
    
    setIsLoading(true);
    try {
      const result = await getChatSessions();
      if (result.success) {
        setSessions(result.sessions);
      }
    } finally {
      setIsLoading(false);
    }
  }, [isLoggedIn]);

  useEffect(() => {
    loadSessions();
  }, [loadSessions, refreshTrigger]);

  // 删除会话
  const handleDelete = async (e: React.MouseEvent, sessionId: string) => {
    e.stopPropagation();
    if (window.confirm('确定要删除这个会话吗？')) {
      const result = await deleteChatSession(sessionId);
      if (result.success) {
        setSessions(prev => prev.filter(s => s.id !== sessionId));
        if (currentSessionId === sessionId) {
          onSelectSession(null);
        }
      }
    }
  };

  // 开始编辑标题
  const handleStartEdit = (e: React.MouseEvent, session: ChatSessionDto) => {
    e.stopPropagation();
    setEditingId(session.id);
    setEditTitle(session.title);
  };

  // 保存标题
  const handleSaveTitle = async (sessionId: string) => {
    if (editTitle.trim()) {
      const result = await updateSessionTitle(sessionId, editTitle.trim());
      if (result.success) {
        setSessions(prev => 
          prev.map(s => s.id === sessionId ? { ...s, title: editTitle.trim() } : s)
        );
      }
    }
    setEditingId(null);
    setEditTitle('');
  };

  // 格式化日期
  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    
    if (days === 0) {
      return '今天';
    } else if (days === 1) {
      return '昨天';
    } else if (days < 7) {
      return `${days}天前`;
    } else {
      return date.toLocaleDateString('zh-CN', { month: 'short', day: 'numeric' });
    }
  };

  if (!isLoggedIn) {
    return null;
  }

  return (
    <div className={`chat-history-sidebar ${isCollapsed ? 'collapsed' : ''}`}>
      {isCollapsed && (
        <button 
          className="expand-toggle"
          onClick={() => setIsCollapsed(false)}
          title="展开侧边栏"
        >
          ▶
        </button>
      )}

      {!isCollapsed && (
        <>
          <div className="sidebar-header">
            <div className="user-info">
              <div className="avatar-wrapper">
                {user?.avatarUrl ? (
                  <img src={user.avatarUrl} alt="avatar" className="user-avatar" />
                ) : (
                  <div className="user-avatar-placeholder">
                    {user?.nickname?.[0] || '👤'}
                  </div>
                )}
              </div>
              <span className="user-nickname">{user?.nickname || '用户'}</span>
            </div>
            <button 
              className="collapse-btn" 
              onClick={() => setIsCollapsed(true)} 
              title="收起侧边栏"
            >
              ◀
            </button>
          </div>

          <button className="new-chat-button" onClick={onNewChat}>
            ✨ 新对话
          </button>

          <div className="sessions-list">
            <div className="sessions-header">
              <span>历史会话</span>
              <button 
                className="refresh-button" 
                onClick={loadSessions}
                disabled={isLoading}
                title="刷新列表"
              >
                🔄
              </button>
            </div>

            {isLoading ? (
              <div className="loading-sessions">加载中...</div>
            ) : sessions.length === 0 ? (
              <div className="no-sessions">暂无历史会话</div>
            ) : (
              <ul className="session-items">
                {sessions.map(session => (
                  <li 
                    key={session.id}
                    className={`session-item ${currentSessionId === session.id ? 'active' : ''}`}
                    onClick={() => onSelectSession(session.id)}
                  >
                    {editingId === session.id ? (
                      <input
                        type="text"
                        className="edit-title-input"
                        value={editTitle}
                        onChange={e => setEditTitle(e.target.value)}
                        onBlur={() => handleSaveTitle(session.id)}
                        onKeyPress={e => e.key === 'Enter' && handleSaveTitle(session.id)}
                        onClick={e => e.stopPropagation()}
                        autoFocus
                      />
                    ) : (
                      <>
                        <div className="session-content">
                          <span className="session-title">{session.title}</span>
                          <span className="session-meta">
                            {formatDate(session.updatedAt)} · {session.messageCount}条
                          </span>
                        </div>
                        <div className="session-actions">
                          <button 
                            className="action-btn edit-btn"
                            onClick={e => handleStartEdit(e, session)}
                            title="编辑标题"
                          >
                            ✏️
                          </button>
                          <button 
                            className="action-btn delete-btn"
                            onClick={e => handleDelete(e, session.id)}
                            title="删除会话"
                          >
                            🗑️
                          </button>
                        </div>
                      </>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </div>

          <div className="sidebar-footer">
            <p className="retention-note">💡 历史记录保留30天</p>
          </div>
        </>
      )}
    </div>
  );
};

export default ChatHistory;
