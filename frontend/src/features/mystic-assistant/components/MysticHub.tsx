/**
 * 玄学AI助手中心
 * 整合塔罗牌、星座运势、八字命理三大功能
 */

import React, { useState } from 'react';
import type { MysticType } from '../types';
import { TarotAssistant } from './TarotAssistant';
import { AstrologyAssistant } from './AstrologyAssistant';
import { BaziAssistant } from './BaziAssistant';
import { mysticService } from '../services/mysticService';
import { useAuth } from '../../../contexts/AuthContext';
import ApprovalStatus from '../../../components/ApprovalStatus';
import './MysticHub.css';

interface ToolCard {
  type: MysticType;
  title: string;
  subtitle: string;
  icon: string;
  description: string;
  features: string[];
  color: string;
}

const TOOLS: ToolCard[] = [
  {
    type: 'tarot',
    title: '塔罗牌占卜',
    subtitle: 'Tarot Reading',
    icon: '🔮',
    description: '78张神秘牌卡，揭示命运的指引',
    features: ['多种牌阵选择', '专业解读', '支持追问'],
    color: 'purple',
  },
  {
    type: 'astrology',
    title: '星座运势',
    subtitle: 'Horoscope',
    icon: '⭐',
    description: '十二星座运势分析，把握人生节奏',
    features: ['每日/周/月/年运势', '各方面详细分析', '幸运指南'],
    color: 'blue',
  },
  {
    type: 'bazi',
    title: '八字命理',
    subtitle: 'Four Pillars',
    icon: '☯️',
    description: '根据生辰八字，解读命运密码',
    features: ['命盘排算', '五行分析', '流年运势'],
    color: 'red',
  },
];

export const MysticHub: React.FC = () => {
  const { isLoggedIn, user, refreshUser } = useAuth();
  const [activeTool, setActiveTool] = useState<MysticType | null>(null);

  // 检查用户是否已被批准
  const isApproved = user?.isApproved || user?.isAdmin;

  // 返回首页
  const handleBack = () => {
    setActiveTool(null);
  };

  // 渲染工具选择首页
  const renderHome = () => (
    <div className="mystic-hub-home">
      <div className="hub-header">
        <h1>🌙 玄学AI助手</h1>
        <p>探索未知，聆听内心的声音</p>
      </div>

      {/* 未登录或未批准提示 */}
      {(!isLoggedIn || !isApproved) && (
        <div className="mystic-permission-notice">
          {!isLoggedIn ? (
            <p>🔐 请先登录后使用玄学助手功能</p>
          ) : (
            <>
              <h3>🔒 需要使用权限</h3>
              <p>您需要申请并获得管理员批准后才能使用 AI 功能</p>
              <ApprovalStatus onStatusChange={refreshUser} />
            </>
          )}
        </div>
      )}

      <div className={`tools-showcase ${(!isLoggedIn || !isApproved) ? 'disabled' : ''}`}>
        {TOOLS.map(tool => (
          <div
            key={tool.type}
            className={`tool-card ${tool.color}`}
            onClick={() => isLoggedIn && isApproved && setActiveTool(tool.type)}
          >
            <div className="tool-icon">{tool.icon}</div>
            <div className="tool-info">
              <h2>{tool.title}</h2>
              <span className="subtitle">{tool.subtitle}</span>
              <p className="description">{tool.description}</p>
              <div className="features">
                {tool.features.map((f, i) => (
                  <span key={i} className="feature-tag">{f}</span>
                ))}
              </div>
            </div>
            <div className="tool-arrow">→</div>
          </div>
        ))}
      </div>

      <div className="disclaimer">
        <p>
          ⚠️ 本服务仅供娱乐参考，不构成任何决策建议。
          命运掌握在自己手中，请理性看待。
        </p>
      </div>
    </div>
  );

  // 渲染工具内容
  const renderTool = () => {
    switch (activeTool) {
      case 'tarot':
        return (
          <TarotAssistant
            onAnalyze={mysticService.analyzeTarot}
            onChat={mysticService.chatWithTarot}
            onBack={handleBack}
          />
        );
      case 'astrology':
        return (
          <AstrologyAssistant
            onAnalyze={mysticService.analyzeAstrology}
            onChat={mysticService.chatWithAstrology}
            onBack={handleBack}
          />
        );
      case 'bazi':
        return (
          <BaziAssistant
            onAnalyze={mysticService.analyzeBazi}
            onChat={mysticService.chatWithBazi}
            onBack={handleBack}
          />
        );
      default:
        return null;
    }
  };

  return (
    <div className="mystic-hub">
      {activeTool ? renderTool() : renderHome()}
    </div>
  );
};

export default MysticHub;
