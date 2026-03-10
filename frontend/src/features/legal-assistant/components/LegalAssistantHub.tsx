/**
 * 法律助手中心 - 统一入口页面
 * 整合离婚财产、劳动仲裁、租房纠纷三大功能
 */

import React, { useState, useMemo } from 'react';
import type { CaseType, LegalCase, DivorceCase, LaborCase, RentalCase } from '../types';
import { useCaseManager } from '../hooks/useCaseManager';
import DivorceAssistant from './DivorceAssistant';
import LaborAssistant from './LaborAssistant';
import RentalAssistant from './RentalAssistant';
import { useAuth } from '../../../contexts/AuthContext';
import ApprovalStatus from '../../../components/ApprovalStatus';
import './LegalAssistantHub.css';

interface AssistantOption {
  type: CaseType;
  emoji: string;
  title: string;
  subtitle: string;
  description: string;
  features: string[];
  color: string;
}

const assistantOptions: AssistantOption[] = [
  {
    type: 'divorce',
    emoji: '💔',
    title: '离婚财产分析',
    subtitle: '财产分割 · 抚养费计算',
    description: '帮您理清婚姻财产，预估分割方案，提供专业法律建议',
    features: ['共同/个人财产分类', '财产分割预估', '子女抚养费计算', '证据收集指导'],
    color: '#e91e63',
  },
  {
    type: 'labor',
    emoji: '⚖️',
    title: '劳动仲裁助手',
    subtitle: '赔偿计算 · 仲裁申请',
    description: '计算应得赔偿金额，整理证据材料，生成仲裁申请书',
    features: ['违法行为赔偿计算', '双倍工资/N+1计算', '证据清单生成', '仲裁申请书'],
    color: '#2196f3',
  },
  {
    type: 'rental',
    emoji: '🏠',
    title: '租房纠纷工具',
    subtitle: '押金追讨 · 维权指导',
    description: '分析合同漏洞，评估胜诉概率，提供维权方案',
    features: ['合同条款分析', '胜诉概率评估', '维权渠道指引', '催告函生成'],
    color: '#4caf50',
  },
];

const LegalAssistantHub: React.FC = () => {
  const { isLoggedIn, user, refreshUser } = useAuth();
  const caseManager = useCaseManager();
  const [selectedType, setSelectedType] = useState<CaseType | null>(null);
  const [viewMode, setViewMode] = useState<'select' | 'list' | 'edit'>('select');

  // 检查用户是否已被批准
  const isApproved = user?.isApproved || user?.isAdmin;

  // 按类型分组的案件
  const casesByType = useMemo(() => {
    const grouped: Record<CaseType, LegalCase[]> = {
      divorce: [],
      labor: [],
      rental: [],
    };
    caseManager.cases.forEach(c => {
      grouped[c.type].push(c);
    });
    return grouped;
  }, [caseManager.cases]);

  // 创建新案件
  const handleCreateCase = (type: CaseType) => {
    const titles: Record<CaseType, string> = {
      divorce: '离婚财产分析',
      labor: '劳动仲裁案件',
      rental: '租房纠纷案件',
    };
    caseManager.createCase(type, `${titles[type]} - ${new Date().toLocaleDateString()}`);
    setSelectedType(type);
    setViewMode('edit');
  };

  // 选择已有案件
  const handleSelectCase = (caseItem: LegalCase) => {
    caseManager.setCurrentCase(caseItem);
    setSelectedType(caseItem.type);
    setViewMode('edit');
  };

  // 返回首页
  const handleBackToHome = () => {
    setViewMode('select');
    setSelectedType(null);
    caseManager.setCurrentCase(null);
  };

  // 查看某类型的案件列表
  const handleViewList = (type: CaseType) => {
    setSelectedType(type);
    setViewMode('list');
  };

  // 渲染选择页面
  const renderSelectPage = () => (
    <div className="hub-select-page">
      <div className="hub-header">
        <h1>⚖️ 法律维权助手</h1>
        <p className="hub-subtitle">AI驱动的法律分析工具，帮您维护合法权益</p>
      </div>

      {/* 未登录或未批准提示 */}
      {(!isLoggedIn || !isApproved) && (
        <div className="legal-permission-notice">
          {!isLoggedIn ? (
            <p>🔐 请先登录后使用法律助手功能</p>
          ) : (
            <>
              <h3>🔒 需要使用权限</h3>
              <p>您需要申请并获得管理员批准后才能使用 AI 功能</p>
              <ApprovalStatus onStatusChange={refreshUser} />
            </>
          )}
        </div>
      )}

      <div className={`assistant-cards ${(!isLoggedIn || !isApproved) ? 'disabled' : ''}`}>
        {assistantOptions.map(option => (
          <div 
            key={option.type}
            className="assistant-card"
            style={{ '--accent-color': option.color } as React.CSSProperties}
          >
            <div className="card-header">
              <span className="card-emoji">{option.emoji}</span>
              <div className="card-titles">
                <h2>{option.title}</h2>
                <span className="card-subtitle">{option.subtitle}</span>
              </div>
            </div>
            
            <p className="card-description">{option.description}</p>
            
            <ul className="card-features">
              {option.features.map((feature, i) => (
                <li key={i}>✓ {feature}</li>
              ))}
            </ul>

            <div className="card-actions">
              <button 
                className="btn-primary"
                onClick={() => handleCreateCase(option.type)}
                disabled={!isLoggedIn || !isApproved}
              >
                🆕 开始新案件
              </button>
              {casesByType[option.type].length > 0 && (
                <button 
                  className="btn-secondary"
                  onClick={() => handleViewList(option.type)}
                  disabled={!isLoggedIn || !isApproved}
                >
                  📂 历史案件 ({casesByType[option.type].length})
                </button>
              )}
            </div>
          </div>
        ))}
      </div>

      {/* 统计信息 */}
      <div className="hub-stats">
        <div className="stat">
          <span className="stat-value">{caseManager.cases.length}</span>
          <span className="stat-label">总案件数</span>
        </div>
        <div className="stat">
          <span className="stat-value">
            {caseManager.cases.filter(c => c.status === 'completed').length}
          </span>
          <span className="stat-label">已分析完成</span>
        </div>
        <div className="stat">
          <span className="stat-value">
            {caseManager.cases.reduce((sum, c) => sum + c.evidences.length, 0)}
          </span>
          <span className="stat-label">已收集证据</span>
        </div>
      </div>

      {/* 免责声明 */}
      <div className="hub-disclaimer">
        <p>
          ⚠️ <strong>重要提示：</strong>本工具提供的分析结果仅供参考，不构成正式法律意见。
          涉及重大权益的案件，建议咨询专业律师获得正式法律帮助。
        </p>
      </div>
    </div>
  );

  // 渲染案件列表
  const renderCaseList = () => {
    const cases = selectedType ? casesByType[selectedType] : [];
    const option = assistantOptions.find(o => o.type === selectedType);

    return (
      <div className="hub-list-page">
        <div className="list-header">
          <button className="btn-back" onClick={handleBackToHome}>
            ← 返回
          </button>
          <h2>{option?.emoji} {option?.title} - 历史案件</h2>
        </div>

        {cases.length === 0 ? (
          <div className="empty-list">
            <p>暂无历史案件</p>
            <button 
              className="btn-primary"
              onClick={() => selectedType && handleCreateCase(selectedType)}
            >
              🆕 创建第一个案件
            </button>
          </div>
        ) : (
          <div className="case-list">
            {cases.map(caseItem => (
              <div key={caseItem.id} className="case-list-item">
                <div className="case-info">
                  <h3>{caseItem.title}</h3>
                  <div className="case-meta">
                    <span className={`status status-${caseItem.status}`}>
                      {caseItem.status === 'draft' && '📝 草稿'}
                      {caseItem.status === 'analyzing' && '⏳ 分析中'}
                      {caseItem.status === 'completed' && '✅ 已完成'}
                      {caseItem.status === 'archived' && '📦 已归档'}
                    </span>
                    <span className="date">
                      创建：{new Date(caseItem.createdAt).toLocaleDateString()}
                    </span>
                    <span className="evidence-count">
                      📎 {caseItem.evidences.length} 份证据
                    </span>
                  </div>
                </div>
                <div className="case-actions">
                  <button 
                    className="btn-edit"
                    onClick={() => handleSelectCase(caseItem)}
                  >
                    编辑
                  </button>
                  <button 
                    className="btn-delete"
                    onClick={() => {
                      if (window.confirm('确定要删除这个案件吗？')) {
                        caseManager.deleteCase(caseItem.id);
                      }
                    }}
                  >
                    删除
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}

        <button 
          className="btn-create-new"
          onClick={() => selectedType && handleCreateCase(selectedType)}
        >
          ➕ 创建新案件
        </button>
      </div>
    );
  };

  // 渲染编辑页面
  const renderEditPage = () => {
    if (!caseManager.currentCase) {
      return <div>加载中...</div>;
    }

    const handleUpdate = (updates: Partial<LegalCase>) => {
      caseManager.updateCase(caseManager.currentCase!.id, updates);
    };

    return (
      <div className="hub-edit-page">
        <div className="edit-header">
          <button className="btn-back" onClick={handleBackToHome}>
            ← 返回首页
          </button>
          <div className="case-title-edit">
            <input
              type="text"
              value={caseManager.currentCase.title}
              onChange={(e) => handleUpdate({ title: e.target.value })}
              className="title-input"
            />
          </div>
        </div>

        <div className="edit-content">
          {caseManager.currentCase.type === 'divorce' && (
            <DivorceAssistant
              caseData={caseManager.currentCase as DivorceCase}
              onUpdate={handleUpdate}
            />
          )}
          {caseManager.currentCase.type === 'labor' && (
            <LaborAssistant
              caseData={caseManager.currentCase as LaborCase}
              onUpdate={handleUpdate}
            />
          )}
          {caseManager.currentCase.type === 'rental' && (
            <RentalAssistant
              caseData={caseManager.currentCase as RentalCase}
              onUpdate={handleUpdate}
            />
          )}
        </div>
      </div>
    );
  };

  return (
    <div className="legal-assistant-hub">
      {viewMode === 'select' && renderSelectPage()}
      {viewMode === 'list' && renderCaseList()}
      {viewMode === 'edit' && renderEditPage()}
    </div>
  );
};

export default LegalAssistantHub;
