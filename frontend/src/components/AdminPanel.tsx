import React, { useState } from 'react';
import UserManagement from './UserManagement';
import UsageStatistics from './UsageStatistics';
import QuotaManagement from './QuotaManagement';
import FeedbackManagement from './FeedbackManagement';
import './AdminPanel.css';

type TabType = 'users' | 'quota' | 'feedback' | 'statistics';

interface AdminPanelProps {
  isOpen: boolean;
  onClose: () => void;
}

const AdminPanel: React.FC<AdminPanelProps> = ({ isOpen, onClose }) => {
  const [activeTab, setActiveTab] = useState<TabType>('users');

  if (!isOpen) return null;

  return (
    <div className="admin-panel-overlay" onClick={onClose}>
      <div className="admin-panel admin-panel-wide" onClick={e => e.stopPropagation()}>
        <div className="admin-panel-header">
          <h3>🔧 管理面板</h3>
          <button className="close-btn" onClick={onClose}>×</button>
        </div>

        {/* 选项卡导航 */}
        <div className="admin-tabs">
          <button 
            className={`tab-btn ${activeTab === 'users' ? 'active' : ''}`}
            onClick={() => setActiveTab('users')}
          >
            👥 用户管理
          </button>
          <button 
            className={`tab-btn ${activeTab === 'quota' ? 'active' : ''}`}
            onClick={() => setActiveTab('quota')}
          >
            🎫 额度管理
          </button>
          <button 
            className={`tab-btn ${activeTab === 'feedback' ? 'active' : ''}`}
            onClick={() => setActiveTab('feedback')}
          >
            💬 用户反馈
          </button>
          <button 
            className={`tab-btn ${activeTab === 'statistics' ? 'active' : ''}`}
            onClick={() => setActiveTab('statistics')}
          >
            📊 使用统计
          </button>
        </div>

        <div className="admin-panel-content">
          {/* 用户管理选项卡内容 */}
          {activeTab === 'users' && (
            <UserManagement />
          )}

          {/* 额度管理选项卡内容 */}
          {activeTab === 'quota' && (
            <QuotaManagement />
          )}

          {/* 用户反馈选项卡内容 */}
          {activeTab === 'feedback' && (
            <FeedbackManagement />
          )}

          {/* 使用统计选项卡内容 */}
          {activeTab === 'statistics' && (
            <UsageStatistics />
          )}
        </div>
      </div>
    </div>
  );
};

export default AdminPanel;
