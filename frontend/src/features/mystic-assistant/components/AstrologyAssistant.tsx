/**
 * 星座运势组件
 * 支持每日/周/月/年运势查询，提供详细分析和追问
 */

import React, { useState, useCallback } from 'react';
import type { ZodiacSign, AstrologyPeriod, AstrologyReading, ChatMessage } from '../types';
import { ZODIAC_DATA } from '../types';
import { triggerQuotaRefresh } from '../../../contexts/QuotaContext';
import './AstrologyAssistant.css';

interface AstrologyAssistantProps {
  onAnalyze: (sign: ZodiacSign, period: AstrologyPeriod, birthInfo?: { date?: string; time?: string }) => Promise<AstrologyReading | null>;
  onChat: (message: string, context: AstrologyReading) => Promise<string>;
  onBack?: () => void;
}

const PERIOD_CONFIG: Record<AstrologyPeriod, { name: string; icon: string }> = {
  daily: { name: '今日运势', icon: '☀️' },
  weekly: { name: '本周运势', icon: '📅' },
  monthly: { name: '本月运势', icon: '🌙' },
  yearly: { name: '年度运势', icon: '🌟' },
};

export const AstrologyAssistant: React.FC<AstrologyAssistantProps> = ({ onAnalyze, onChat, onBack }) => {
  const [step, setStep] = useState<'select_sign' | 'select_period' | 'loading' | 'result' | 'error'>('select_sign');
  const [selectedSign, setSelectedSign] = useState<ZodiacSign | null>(null);
  const [selectedPeriod, setSelectedPeriod] = useState<AstrologyPeriod>('daily');
  const [birthDate, setBirthDate] = useState('');
  const [birthTime, setBirthTime] = useState('');
  const [reading, setReading] = useState<AstrologyReading | null>(null);
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [chatInput, setChatInput] = useState('');
  const [isChatLoading, setIsChatLoading] = useState(false);
  const [showAdvanced, setShowAdvanced] = useState(false);

  // 获取星座信息
  const getSignInfo = (sign: ZodiacSign) => ZODIAC_DATA.find(z => z.sign === sign);

  // 根据出生日期自动判断星座
  const detectSignFromDate = (dateStr: string): ZodiacSign | null => {
    if (!dateStr) return null;
    const date = new Date(dateStr);
    const month = date.getMonth() + 1;
    const day = date.getDate();
    
    const signRanges: [ZodiacSign, number, number, number, number][] = [
      ['capricorn', 12, 22, 1, 19],
      ['aquarius', 1, 20, 2, 18],
      ['pisces', 2, 19, 3, 20],
      ['aries', 3, 21, 4, 19],
      ['taurus', 4, 20, 5, 20],
      ['gemini', 5, 21, 6, 21],
      ['cancer', 6, 22, 7, 22],
      ['leo', 7, 23, 8, 22],
      ['virgo', 8, 23, 9, 22],
      ['libra', 9, 23, 10, 23],
      ['scorpio', 10, 24, 11, 22],
      ['sagittarius', 11, 23, 12, 21],
    ];

    for (const [sign, m1, d1, m2, d2] of signRanges) {
      if ((month === m1 && day >= d1) || (month === m2 && day <= d2)) {
        return sign;
      }
    }
    return 'capricorn';
  };

  // 开始分析
  const handleAnalyze = useCallback(async () => {
    if (!selectedSign) return;
    
    setIsLoading(true);
    setStep('loading');
    setErrorMessage('');
    
    try {
      const birthInfo = birthDate ? { date: birthDate, time: birthTime || undefined } : undefined;
      const result = await onAnalyze(selectedSign, selectedPeriod, birthInfo);
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
  }, [selectedSign, selectedPeriod, birthDate, birthTime, onAnalyze]);

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
    setStep('select_sign');
    setSelectedSign(null);
    setSelectedPeriod('daily');
    setBirthDate('');
    setBirthTime('');
    setReading(null);
    setMessages([]);
    setErrorMessage('');
  };

  // 渲染错误页面
  const renderError = () => (
    <div className="astrology-error">
      <div className="error-icon">⚠️</div>
      <h2>分析失败</h2>
      <p>{errorMessage}</p>
      <div className="button-group">
        <button className="astrology-btn primary" onClick={handleReset}>
          重新开始
        </button>
        <button className="astrology-btn secondary" onClick={() => setStep('select_period')}>
          返回重试
        </button>
      </div>
    </div>
  );

  // 处理出生日期变化
  const handleBirthDateChange = (value: string) => {
    setBirthDate(value);
    const detected = detectSignFromDate(value);
    if (detected) {
      setSelectedSign(detected);
    }
  };

  // 渲染运势分数
  const renderScore = (score: number, label: string) => (
    <div className="score-item">
      <div className="score-label">{label}</div>
      <div className="score-bar-container">
        <div className="score-bar" style={{ width: `${score}%` }}>
          <span className="score-value">{score}</span>
        </div>
      </div>
      <div className="score-stars">
        {Array(5).fill(0).map((_, i) => (
          <span key={i} className={i < Math.round(score / 20) ? 'star filled' : 'star'}>★</span>
        ))}
      </div>
    </div>
  );

  // 渲染星座选择
  const renderSignSelection = () => (
    <div className="astrology-sign-selection">
      <h2>✨ 选择你的星座</h2>
      
      <div className="birth-date-input">
        <label>
          <span>或输入出生日期自动识别：</span>
          <input
            type="date"
            value={birthDate}
            onChange={e => handleBirthDateChange(e.target.value)}
          />
        </label>
        {birthDate && selectedSign && (
          <span className="detected-sign">
            检测到：{getSignInfo(selectedSign)?.symbol} {getSignInfo(selectedSign)?.name}
          </span>
        )}
      </div>

      <div className="zodiac-grid">
        {ZODIAC_DATA.map(zodiac => (
          <div
            key={zodiac.sign}
            className={`zodiac-card ${selectedSign === zodiac.sign ? 'selected' : ''} ${zodiac.element}`}
            onClick={() => setSelectedSign(zodiac.sign)}
          >
            <div className="zodiac-symbol">{zodiac.symbol}</div>
            <div className="zodiac-name">{zodiac.name}</div>
            <div className="zodiac-date">{zodiac.dateRange}</div>
          </div>
        ))}
      </div>

      <button 
        className="astrology-btn primary"
        onClick={() => setStep('select_period')}
        disabled={!selectedSign}
      >
        下一步
      </button>
    </div>
  );

  // 渲染周期选择
  const renderPeriodSelection = () => {
    const signInfo = selectedSign ? getSignInfo(selectedSign) : null;
    
    return (
      <div className="astrology-period-selection">
        <div className="selected-sign-display">
          <span className="big-symbol">{signInfo?.symbol}</span>
          <span className="sign-name">{signInfo?.name}</span>
        </div>

        <h2>📆 选择查询周期</h2>

        <div className="period-options">
          {Object.entries(PERIOD_CONFIG).map(([key, config]) => (
            <div
              key={key}
              className={`period-card ${selectedPeriod === key ? 'selected' : ''}`}
              onClick={() => setSelectedPeriod(key as AstrologyPeriod)}
            >
              <span className="period-icon">{config.icon}</span>
              <span className="period-name">{config.name}</span>
            </div>
          ))}
        </div>

        <div className="advanced-toggle">
          <button onClick={() => setShowAdvanced(!showAdvanced)}>
            {showAdvanced ? '收起' : '展开'}高级选项（更精准分析）
          </button>
        </div>

        {showAdvanced && (
          <div className="advanced-options">
            <div className="option-row">
              <label>
                <span>出生日期</span>
                <input
                  type="date"
                  value={birthDate}
                  onChange={e => setBirthDate(e.target.value)}
                />
              </label>
              <label>
                <span>出生时间</span>
                <input
                  type="time"
                  value={birthTime}
                  onChange={e => setBirthTime(e.target.value)}
                  placeholder="可选"
                />
              </label>
            </div>
            <p className="hint">提供更详细的出生信息可获得更精准的分析</p>
          </div>
        )}

        <div className="button-group">
          <button className="astrology-btn secondary" onClick={() => setStep('select_sign')}>
            返回
          </button>
          <button className="astrology-btn primary" onClick={handleAnalyze}>
            开始分析
          </button>
        </div>
      </div>
    );
  };

  // 渲染加载
  const renderLoading = () => {
    const signInfo = selectedSign ? getSignInfo(selectedSign) : null;
    
    return (
      <div className="astrology-loading">
        <div className="constellation-animation">
          <div className="star-field">
            {Array(20).fill(0).map((_, i) => (
              <div key={i} className="twinkle-star" style={{
                left: `${Math.random() * 100}%`,
                top: `${Math.random() * 100}%`,
                animationDelay: `${Math.random() * 2}s`,
              }} />
            ))}
          </div>
          <div className="center-symbol">{signInfo?.symbol}</div>
        </div>
        <p>正在解读 {signInfo?.name} 的星象...</p>
      </div>
    );
  };

  // 渲染结果
  const renderResult = () => {
    if (!reading) return null;
    
    const signInfo = getSignInfo(reading.sign);
    
    return (
      <div className="astrology-result">
        <div className="result-header">
          <div className="sign-badge">
            <span className="symbol">{signInfo?.symbol}</span>
            <span className="name">{signInfo?.name}</span>
          </div>
          <div className="period-badge">
            {PERIOD_CONFIG[reading.period].icon} {PERIOD_CONFIG[reading.period].name}
          </div>
        </div>

        <div className="result-content">
          <div className="main-fortune">
            <div className="overall-section">
              <h3>🌟 综合运势</h3>
              <div className="overall-score">
                <div className="score-circle" style={{ '--score': reading.overall.score } as React.CSSProperties}>
                  <span className="score-number">{reading.overall.score}</span>
                </div>
              </div>
              <p className="summary">{reading.overall.summary}</p>
              <p className="advice">{reading.overall.advice}</p>
            </div>

            <div className="fortune-details">
              <h3>📊 各方面运势</h3>
              {renderScore(reading.love.score, '❤️ 爱情')}
              {renderScore(reading.career.score, '💼 事业')}
              {renderScore(reading.wealth.score, '💰 财运')}
              {renderScore(reading.health.score, '💪 健康')}
            </div>
          </div>

          <div className="details-grid">
            <div className="detail-card love">
              <h4>❤️ 爱情运势</h4>
              <p>{reading.love.summary}</p>
              <p className="advice">{reading.love.advice}</p>
            </div>
            <div className="detail-card career">
              <h4>💼 事业运势</h4>
              <p>{reading.career.summary}</p>
              <p className="advice">{reading.career.advice}</p>
            </div>
            <div className="detail-card wealth">
              <h4>💰 财运分析</h4>
              <p>{reading.wealth.summary}</p>
              <p className="advice">{reading.wealth.advice}</p>
            </div>
            <div className="detail-card health">
              <h4>💪 健康提醒</h4>
              <p>{reading.health.summary}</p>
              <p className="advice">{reading.health.advice}</p>
            </div>
          </div>

          <div className="lucky-items">
            <h3>🍀 幸运指南</h3>
            <div className="lucky-grid">
              <div className="lucky-item">
                <span className="lucky-label">幸运颜色</span>
                <span className="lucky-value" style={{ color: reading.luckyColor }}>{reading.luckyColor}</span>
              </div>
              <div className="lucky-item">
                <span className="lucky-label">幸运数字</span>
                <span className="lucky-value">{reading.luckyNumber}</span>
              </div>
              <div className="lucky-item">
                <span className="lucky-label">幸运方位</span>
                <span className="lucky-value">{reading.luckyDirection}</span>
              </div>
              <div className="lucky-item">
                <span className="lucky-label">最配星座</span>
                <span className="lucky-value">
                  {reading.compatibility.map(s => getSignInfo(s)?.symbol).join(' ')}
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* 追问区域 */}
        <div className="chat-section">
          <h3>💬 有问题？继续问我</h3>
          <div className="chat-messages">
            {messages.length === 0 && (
              <div className="chat-hint">
                可以问我关于运势的任何细节，比如"感情方面要注意什么？"
              </div>
            )}
            {messages.map(msg => (
              <div key={msg.id} className={`chat-message ${msg.role}`}>
                <div className="message-content">{msg.content}</div>
              </div>
            ))}
            {isChatLoading && (
              <div className="chat-message assistant">
                <div className="message-content typing">占星师正在回复...</div>
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

        <button className="astrology-btn secondary restart" onClick={handleReset}>
          🔄 重新查询
        </button>
      </div>
    );
  };

  return (
    <div className="astrology-assistant">
      {onBack && (
        <button className="inner-back-button" onClick={onBack}>
          ← 返回
        </button>
      )}
      <div className="astrology-header">
        <h1>⭐ 星座运势</h1>
        <p>探索星象的奥秘，把握命运的轨迹</p>
      </div>

      <div className="astrology-content">
        {step === 'select_sign' && renderSignSelection()}
        {step === 'select_period' && renderPeriodSelection()}
        {step === 'loading' && renderLoading()}
        {step === 'result' && renderResult()}
        {step === 'error' && renderError()}
      </div>
    </div>
  );
};

export default AstrologyAssistant;
