/**
 * 八字命理组件
 * 根据出生时间分析命盘，包含性格、事业、感情、财运等
 */

import React, { useState, useCallback } from 'react';
import type { BaziReading, BaziRequest, WuXing, ChatMessage } from '../types';
import { triggerQuotaRefresh } from '../../../contexts/QuotaContext';
import './BaziAssistant.css';

interface BaziAssistantProps {
  onAnalyze: (request: BaziRequest) => Promise<BaziReading | null>;
  onChat: (message: string, context: BaziReading) => Promise<string>;
  onBack?: () => void;
}

// 五行配色
const WUXING_COLORS: Record<WuXing, string> = {
  '金': '#c0c0c0',
  '木': '#2e8b57',
  '水': '#4169e1',
  '火': '#dc143c',
  '土': '#daa520',
};

// 五行图标
const WUXING_ICONS: Record<WuXing, string> = {
  '金': '🪙',
  '木': '🌳',
  '水': '💧',
  '火': '🔥',
  '土': '🏔️',
};

export const BaziAssistant: React.FC<BaziAssistantProps> = ({ onAnalyze, onChat, onBack }) => {
  const [step, setStep] = useState<'input' | 'loading' | 'result' | 'error'>('input');
  const [birthDate, setBirthDate] = useState('');
  const [birthTime, setBirthTime] = useState('');
  const [birthPlace, setBirthPlace] = useState('');
  const [gender, setGender] = useState<'male' | 'female'>('male');
  const [name, setName] = useState('');
  const [reading, setReading] = useState<BaziReading | null>(null);
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [activeTab, setActiveTab] = useState<'chart' | 'personality' | 'career' | 'love' | 'wealth' | 'health'>('chart');
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [chatInput, setChatInput] = useState('');
  const [isChatLoading, setIsChatLoading] = useState(false);

  // 开始分析
  const handleAnalyze = useCallback(async () => {
    if (!birthDate || !birthTime) return;
    
    setIsLoading(true);
    setStep('loading');
    setErrorMessage('');
    
    try {
      const request: BaziRequest = {
        birthDate,
        birthTime,
        birthPlace: birthPlace || undefined,
        gender,
        name: name || undefined,
        analysisYear: new Date().getFullYear(),
      };
      
      const result = await onAnalyze(request);
      if (result) {
        setReading(result);
        setStep('result');
        // 刷新使用额度显示
        triggerQuotaRefresh();
      } else {
        setErrorMessage('AI 分析请求失败，请检查网络连接或稍后重试。如果问题持续，请确认您的账户已获得使用权限。');
        setStep('error');
      }
    } catch (error) {
      console.error('Analysis failed:', error);
      setErrorMessage('发生错误，请稍后重试');
      setStep('error');
    } finally {
      setIsLoading(false);
    }
  }, [birthDate, birthTime, birthPlace, gender, name, onAnalyze]);

  // 发送追问
  const handleSendChat = useCallback(async () => {
    if (!chatInput.trim() || !reading) return;
    
    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      role: 'user',
      content: chatInput,
      timestamp: new Date(),
    };
    setMessages(prev => [...prev, userMessage]);
    setChatInput('');
    setIsChatLoading(true);

    try {
      const response = await onChat(chatInput, reading);
      const assistantMessage: ChatMessage = {
        id: (Date.now() + 1).toString(),
        role: 'assistant',
        content: response,
        timestamp: new Date(),
      };
      setMessages(prev => [...prev, assistantMessage]);
    } catch (error) {
      console.error('Chat failed:', error);
    } finally {
      setIsChatLoading(false);
    }
  }, [chatInput, reading, onChat]);

  // 重新开始
  const handleReset = () => {
    setStep('input');
    setBirthDate('');
    setBirthTime('');
    setBirthPlace('');
    setGender('male');
    setName('');
    setReading(null);
    setActiveTab('chart');
    setMessages([]);
    setErrorMessage('');
  };

  // 渲染错误页面
  const renderError = () => (
    <div className="bazi-error">
      <div className="error-icon">⚠️</div>
      <h2>分析失败</h2>
      <p>{errorMessage}</p>
      <div className="button-group">
        <button className="bazi-btn primary" onClick={handleReset}>
          重新开始
        </button>
        <button className="bazi-btn secondary" onClick={() => setStep('input')}>
          返回重试
        </button>
      </div>
    </div>
  );

  // 渲染输入表单
  const renderInput = () => (
    <div className="bazi-input-form">
      <h2>📅 请输入出生信息</h2>
      <p className="subtitle">精准的出生时间是命理分析的基础</p>

      <div className="form-grid">
        <div className="form-group">
          <label>
            <span className="required">*</span> 出生日期
          </label>
          <input
            type="date"
            value={birthDate}
            onChange={e => setBirthDate(e.target.value)}
            max={new Date().toISOString().split('T')[0]}
          />
        </div>

        <div className="form-group">
          <label>
            <span className="required">*</span> 出生时间
          </label>
          <input
            type="time"
            value={birthTime}
            onChange={e => setBirthTime(e.target.value)}
          />
          <span className="hint">尽量精确到小时</span>
        </div>

        <div className="form-group">
          <label>性别</label>
          <div className="gender-select">
            <button
              className={gender === 'male' ? 'active' : ''}
              onClick={() => setGender('male')}
            >
              👨 男
            </button>
            <button
              className={gender === 'female' ? 'active' : ''}
              onClick={() => setGender('female')}
            >
              👩 女
            </button>
          </div>
        </div>

        <div className="form-group">
          <label>出生地点 <span className="optional">(可选)</span></label>
          <input
            type="text"
            value={birthPlace}
            onChange={e => setBirthPlace(e.target.value)}
            placeholder="如：北京、上海"
          />
          <span className="hint">用于真太阳时校正</span>
        </div>

        <div className="form-group full-width">
          <label>姓名 <span className="optional">(可选)</span></label>
          <input
            type="text"
            value={name}
            onChange={e => setName(e.target.value)}
            placeholder="输入姓名可获得更个性化的分析"
          />
        </div>
      </div>

      <div className="time-helper">
        <h4>不知道具体时辰？</h4>
        <div className="shichen-grid">
          {[
            { name: '子时', range: '23:00-01:00' },
            { name: '丑时', range: '01:00-03:00' },
            { name: '寅时', range: '03:00-05:00' },
            { name: '卯时', range: '05:00-07:00' },
            { name: '辰时', range: '07:00-09:00' },
            { name: '巳时', range: '09:00-11:00' },
            { name: '午时', range: '11:00-13:00' },
            { name: '未时', range: '13:00-15:00' },
            { name: '申时', range: '15:00-17:00' },
            { name: '酉时', range: '17:00-19:00' },
            { name: '戌时', range: '19:00-21:00' },
            { name: '亥时', range: '21:00-23:00' },
          ].map(sc => (
            <button
              key={sc.name}
              className="shichen-btn"
              onClick={() => {
                const [start] = sc.range.split('-');
                setBirthTime(start);
              }}
            >
              {sc.name}<br /><small>{sc.range}</small>
            </button>
          ))}
        </div>
      </div>

      <button
        className="bazi-btn primary"
        onClick={handleAnalyze}
        disabled={!birthDate || !birthTime}
      >
        开始排盘
      </button>
    </div>
  );

  // 渲染加载
  const renderLoading = () => (
    <div className="bazi-loading">
      <div className="bagua-animation">
        <div className="yin-yang">
          <div className="yang"></div>
          <div className="yin"></div>
        </div>
      </div>
      <p>正在排算八字命盘...</p>
      <p className="sub">综合分析五行、十神、大运流年</p>
    </div>
  );

  // 渲染五行分布
  const renderWuxingChart = () => {
    if (!reading) return null;
    const maxCount = Math.max(...Object.values(reading.chart.wuxingCount));
    
    return (
      <div className="wuxing-chart">
        <h4>五行分布</h4>
        <div className="wuxing-bars">
          {(Object.entries(reading.chart.wuxingCount) as [WuXing, number][]).map(([element, count]) => (
            <div key={element} className="wuxing-bar-item">
              <div className="wuxing-label">
                <span className="icon">{WUXING_ICONS[element]}</span>
                <span>{element}</span>
              </div>
              <div className="bar-container">
                <div
                  className="bar-fill"
                  style={{
                    width: `${(count / maxCount) * 100}%`,
                    backgroundColor: WUXING_COLORS[element],
                  }}
                />
              </div>
              <span className="count">{count}</span>
            </div>
          ))}
        </div>
        <p className="balance-text">{reading.chart.wuxingBalance}</p>
        {reading.wuxingAnalysis && (
          <p className="wuxing-analysis">{reading.wuxingAnalysis}</p>
        )}
      </div>
    );
  };

  // 渲染八字命盘
  const renderBaziChart = () => {
    if (!reading) return null;
    
    const { chart } = reading;
    const pillars = [
      { name: '年柱', pillar: chart.yearPillar },
      { name: '月柱', pillar: chart.monthPillar },
      { name: '日柱', pillar: chart.dayPillar },
      { name: '时柱', pillar: chart.hourPillar },
    ];

    return (
      <div className="bazi-chart">
        <h4>八字命盘</h4>
        <div className="pillars-container">
          {pillars.map(({ name, pillar }) => (
            <div key={name} className="pillar">
              <div className="pillar-name">{name}</div>
              <div
                className="pillar-stem"
                style={{ backgroundColor: WUXING_COLORS[pillar.element] }}
              >
                {pillar.stem}
              </div>
              <div
                className="pillar-branch"
                style={{ borderColor: WUXING_COLORS[pillar.element] }}
              >
                {pillar.branch}
              </div>
              <div className="pillar-element">{WUXING_ICONS[pillar.element]} {pillar.element}</div>
            </div>
          ))}
        </div>
        <div className="day-master">
          <span>日主：</span>
          <strong style={{ color: WUXING_COLORS[chart.dayMasterElement] }}>
            {chart.dayMaster} ({chart.dayMasterElement})
          </strong>
        </div>
      </div>
    );
  };

  // 渲染结果
  const renderResult = () => {
    if (!reading) return null;

    const tabs = [
      { key: 'chart', label: '命盘', icon: '☯️' },
      { key: 'personality', label: '性格', icon: '🧠' },
      { key: 'career', label: '事业', icon: '💼' },
      { key: 'love', label: '感情', icon: '❤️' },
      { key: 'wealth', label: '财运', icon: '💰' },
      { key: 'health', label: '健康', icon: '🏥' },
    ];

    return (
      <div className="bazi-result">
        <div className="result-header">
          {name && <h2>{name}的命盘分析</h2>}
          <p className="birth-info">
            {birthDate} {birthTime} ({gender === 'male' ? '男' : '女'})
          </p>
        </div>

        <div className="tabs">
          {tabs.map(tab => (
            <button
              key={tab.key}
              className={`tab ${activeTab === tab.key ? 'active' : ''}`}
              onClick={() => setActiveTab(tab.key as typeof activeTab)}
            >
              {tab.icon} {tab.label}
            </button>
          ))}
        </div>

        <div className="tab-content">
          {activeTab === 'chart' && (
            <div className="chart-section">
              {renderBaziChart()}
              {renderWuxingChart()}
              
              <div className="lucky-elements">
                <h4>🍀 命理喜用</h4>
                <div className="lucky-items">
                  <div className="lucky-item">
                    <span className="label">喜用五行</span>
                    <span className="value">
                      {reading.luckyElements.map(e => (
                        <span key={e} style={{ color: WUXING_COLORS[e] }}>
                          {WUXING_ICONS[e]} {e}
                        </span>
                      ))}
                    </span>
                  </div>
                  <div className="lucky-item">
                    <span className="label">幸运颜色</span>
                    <span className="value">{reading.luckyColors.join('、')}</span>
                  </div>
                  <div className="lucky-item">
                    <span className="label">幸运数字</span>
                    <span className="value">{reading.luckyNumbers.join('、')}</span>
                  </div>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'personality' && (
            <div className="personality-section">
              <div className="traits-box">
                <h4>🎭 性格特质</h4>
                <div className="trait-tags">
                  {reading.personality.traits.map((trait, i) => (
                    <span key={i} className="trait-tag">{trait}</span>
                  ))}
                </div>
              </div>
              <div className="pros-cons">
                <div className="pros">
                  <h4>✨ 优势</h4>
                  <ul>
                    {reading.personality.strengths.map((s, i) => (
                      <li key={i}>{s}</li>
                    ))}
                  </ul>
                </div>
                <div className="cons">
                  <h4>⚠️ 需注意</h4>
                  <ul>
                    {reading.personality.weaknesses.map((w, i) => (
                      <li key={i}>{w}</li>
                    ))}
                  </ul>
                </div>
              </div>
              <div className="advice-box">
                <strong>建议：</strong>
                <p>{reading.personality.advice}</p>
              </div>
            </div>
          )}

          {activeTab === 'career' && (
            <div className="career-section">
              <div className="suitable-fields">
                <h4>💼 适合行业</h4>
                <div className="field-tags">
                  {reading.career.suitableFields.map((field, i) => (
                    <span key={i} className="field-tag">{field}</span>
                  ))}
                </div>
              </div>
              <div className="lucky-directions">
                <h4>🧭 吉利方位</h4>
                <div className="direction-tags">
                  {reading.career.luckyDirections.map((dir, i) => (
                    <span key={i} className="direction-tag">{dir}</span>
                  ))}
                </div>
              </div>
              <div className="advice-box">
                <strong>事业建议：</strong>
                <p>{reading.career.advice}</p>
              </div>
            </div>
          )}

          {activeTab === 'love' && (
            <div className="love-section">
              <div className="ideal-partner">
                <h4>💕 理想伴侣特征</h4>
                <p>{reading.relationship.idealPartner}</p>
              </div>
              {reading.relationship.marriageAge && (
                <div className="marriage-age">
                  <h4>💍 适婚年龄</h4>
                  <p>{reading.relationship.marriageAge}</p>
                </div>
              )}
              <div className="advice-box">
                <strong>感情建议：</strong>
                <p>{reading.relationship.advice}</p>
              </div>
            </div>
          )}

          {activeTab === 'wealth' && (
            <div className="wealth-section">
              <div className="wealth-type">
                <h4>💰 财富类型</h4>
                <p>{reading.wealth.wealthType}</p>
              </div>
              <div className="lucky-years">
                <h4>📈 财运旺盛年份</h4>
                <div className="year-tags">
                  {reading.wealth.luckyYears.map((year, i) => (
                    <span key={i} className="year-tag">{year}</span>
                  ))}
                </div>
              </div>
              <div className="advice-box">
                <strong>理财建议：</strong>
                <p>{reading.wealth.advice}</p>
              </div>
            </div>
          )}

          {activeTab === 'health' && (
            <div className="health-section">
              <div className="weak-organs">
                <h4>⚕️ 需关注的身体部位</h4>
                <div className="organ-tags">
                  {reading.health.weakOrgans.map((organ, i) => (
                    <span key={i} className="organ-tag">{organ}</span>
                  ))}
                </div>
              </div>
              <div className="advice-box">
                <strong>健康建议：</strong>
                <p>{reading.health.advice}</p>
              </div>
            </div>
          )}
        </div>

        {/* 流年运势 */}
        {reading.annualFortune && (
          <div className="annual-fortune">
            <h3>🗓️ {reading.annualFortune.year}年流年运势</h3>
            <p className="summary">{reading.annualFortune.summary}</p>
            <div className="fortune-details">
              <div className="lucky-months">
                <strong>旺运月份：</strong>
                {reading.annualFortune.luckyMonths.map(m => `${m}月`).join('、')}
              </div>
              <div className="challenges">
                <strong>注意事项：</strong>
                <ul>
                  {reading.annualFortune.challenges.map((c, i) => (
                    <li key={i}>{c}</li>
                  ))}
                </ul>
              </div>
            </div>
          </div>
        )}

        {/* 对话区域 */}
        <div className="chat-section">
          <h3>💬 继续追问命理师</h3>
          <div className="chat-messages">
            {messages.length === 0 && (
              <div className="chat-hint">
                有任何关于你命盘的问题都可以问我，比如"今年适合创业吗？"
              </div>
            )}
            {messages.map(msg => (
              <div key={msg.id} className={`chat-message ${msg.role}`}>
                <div className="message-content">{msg.content}</div>
              </div>
            ))}
            {isChatLoading && (
              <div className="chat-message assistant">
                <div className="message-content typing">命理师正在分析...</div>
              </div>
            )}
          </div>
          
          <div className="chat-input-area">
            <input
              type="text"
              value={chatInput}
              onChange={e => setChatInput(e.target.value)}
              placeholder="输入你的问题..."
              onKeyDown={e => e.key === 'Enter' && handleSendChat()}
            />
            <button onClick={handleSendChat} disabled={!chatInput.trim() || isChatLoading}>
              发送
            </button>
          </div>
        </div>

        <button className="bazi-btn secondary restart" onClick={handleReset}>
          🔄 重新排盘
        </button>
      </div>
    );
  };

  return (
    <div className="bazi-assistant">
      {onBack && (
        <button className="inner-back-button" onClick={onBack}>
          ← 返回
        </button>
      )}
      <div className="bazi-header">
        <h1>☯️ 八字命理</h1>
        <p>探索生辰密码，了解命运轨迹</p>
      </div>

      <div className="bazi-content">
        {step === 'input' && renderInput()}
        {step === 'loading' && renderLoading()}
        {step === 'result' && renderResult()}
        {step === 'error' && renderError()}
      </div>
    </div>
  );
};

export default BaziAssistant;
