import React, { useState, useCallback, useEffect, useRef } from 'react';
import ImageGenerator from './components/ImageGenerator';
import ChatInterface from './components/ChatInterface';
import ChatHistory from './components/ChatHistory';
import LoginModal from './components/LoginModal';
import AdminPanel from './components/AdminPanel';
import UsageQuota from './components/UsageQuota';
import FeedbackForm from './components/FeedbackForm';
import { LegalAssistantHub } from './features/legal-assistant';
import { MysticHub } from './features/mystic-assistant';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { QuotaProvider } from './contexts/QuotaContext';
import './App.css';

type TabType = 'mystic' | 'chat' | 'image' | 'legal';

// 主应用内容组件
const AppContent: React.FC = () => {
  const { isLoggedIn, user, logout } = useAuth();
  const [activeTab, setActiveTab] = useState<TabType>('mystic');
  const [showLoginModal, setShowLoginModal] = useState(false);
  const [showAdminPanel, setShowAdminPanel] = useState(false);
  const [currentSessionId, setCurrentSessionId] = useState<string | null>(null);
  const [historyRefreshTrigger, setHistoryRefreshTrigger] = useState(0);
  const [showUserMenu, setShowUserMenu] = useState(false);
  const [showFeedbackModal, setShowFeedbackModal] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);

  // 点击外部关闭用户菜单
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setShowUserMenu(false);
      }
    };

    if (showUserMenu) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [showUserMenu]);

  // 选择会话
  const handleSelectSession = useCallback((sessionId: string | null) => {
    setCurrentSessionId(sessionId);
  }, []);

  // 新对话
  const handleNewChat = useCallback(() => {
    setCurrentSessionId(null);
  }, []);

  // 会话变化时刷新历史列表
  const handleSessionChange = useCallback((sessionId: string, title: string) => {
    setCurrentSessionId(sessionId);
    setHistoryRefreshTrigger(prev => prev + 1);
  }, []);

  return (
    <div className="App">
      <div className="app-layout">
        {/* 侧边栏 - 仅登录后显示 */}
        {isLoggedIn && activeTab === 'chat' && (
          <ChatHistory
            currentSessionId={currentSessionId || undefined}
            onSelectSession={handleSelectSession}
            onNewChat={handleNewChat}
            refreshTrigger={historyRefreshTrigger}
          />
        )}

        <div className="app-main">
          <div className="app-header">
            <h1 className="app-title">✨ Aether AI Studio</h1>
            <div className="header-center">
              <div className="tab-navigation">
                <button
                  className={`tab-button ${activeTab === 'mystic' ? 'active' : ''}`}
                  onClick={() => setActiveTab('mystic')}
                >
                  🔮 玄学助手
                </button>
                <button
                  className={`tab-button ${activeTab === 'chat' ? 'active' : ''}`}
                  onClick={() => setActiveTab('chat')}
                >
                  💬 AI 对话
                </button>
                <button
                  className={`tab-button ${activeTab === 'image' ? 'active' : ''}`}
                  onClick={() => setActiveTab('image')}
                >
                  🎨 图像生成
                </button>
                <button
                  className={`tab-button ${activeTab === 'legal' ? 'active' : ''}`}
                  onClick={() => setActiveTab('legal')}
                >
                  ⚖️ 法律助手
                </button>
              </div>
            </div>
            <div className="header-right">
              {isLoggedIn && !user?.isAdmin && (
                <UsageQuota className="header-quota" />
              )}
              {isLoggedIn && user?.isAdmin && (
                <button 
                  className="admin-btn"
                  onClick={() => setShowAdminPanel(true)}
                >
                  👑 管理
                </button>
              )}
              {isLoggedIn ? (
                <div 
                  ref={userMenuRef}
                  className="user-badge-wrapper"
                  onClick={() => setShowUserMenu(!showUserMenu)}
                >
                  <div className="user-badge">
                    <span className="user-greeting">👋 {user?.nickname || '用户'}</span>
                    <span className="dropdown-arrow">{showUserMenu ? '▲' : '▼'}</span>
                  </div>
                  {showUserMenu && (
                    <div className="user-dropdown-menu">
                      <div className="dropdown-user-info">
                        <span className="dropdown-nickname">{user?.nickname || '用户'}</span>
                        <span className="dropdown-email">{user?.email || ''}</span>
                      </div>
                      <div className="dropdown-divider"></div>
                      <button 
                        className="dropdown-item feedback-item" 
                        onClick={(e) => {
                          e.stopPropagation();
                          setShowUserMenu(false);
                          setShowFeedbackModal(true);
                        }}
                      >
                        💬 反馈建议
                      </button>
                      <button 
                        className="dropdown-item logout-item" 
                        onClick={(e) => {
                          e.stopPropagation();
                          setShowUserMenu(false);
                          logout();
                        }}
                      >
                        🚪 退出登录
                      </button>
                    </div>
                  )}
                </div>
              ) : (
                <button 
                  className="login-btn"
                  onClick={() => setShowLoginModal(true)}
                >
                  🔐 登录
                </button>
              )}
            </div>
          </div>
          
          <div className="app-content">
            <div className={`tab-panel ${activeTab === 'mystic' ? 'active' : ''}`}>
              <MysticHub />
            </div>
            <div className={`tab-panel ${activeTab === 'chat' ? 'active' : ''}`}>
              <ChatInterface 
                sessionId={currentSessionId}
                onSessionChange={handleSessionChange}
              />
            </div>
            <div className={`tab-panel ${activeTab === 'image' ? 'active' : ''}`}>
              <ImageGenerator />
            </div>
            <div className={`tab-panel ${activeTab === 'legal' ? 'active' : ''}`}>
              <LegalAssistantHub />
            </div>
          </div>
          
          <footer className="app-footer">
            <p>© {new Date().getFullYear()} Aether AI Studio. All rights reserved.</p>
          </footer>
        </div>
      </div>

      {/* 登录弹窗 */}
      <LoginModal 
        isOpen={showLoginModal} 
        onClose={() => setShowLoginModal(false)} 
      />

      {/* 管理员面板 */}
      <AdminPanel
        isOpen={showAdminPanel}
        onClose={() => setShowAdminPanel(false)}
      />

      {/* 用户反馈弹窗 */}
      {showFeedbackModal && (
        <div className="modal-overlay" onClick={() => setShowFeedbackModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <FeedbackForm onClose={() => setShowFeedbackModal(false)} />
          </div>
        </div>
      )}
    </div>
  );
};

// 根组件包裹AuthProvider和QuotaProvider
function App() {
  return (
    <AuthProvider>
      <QuotaProvider>
        <AppContent />
      </QuotaProvider>
    </AuthProvider>
  );
}

export default App;
