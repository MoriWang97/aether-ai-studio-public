/**
 * 塔罗牌占卜组件
 * 支持多种牌阵，提供AI解读和追问功能
 */

import React, { useState, useCallback } from 'react';
import type { TarotSpreadType, TarotCard, TarotReading, ChatMessage } from '../types';
import { MAJOR_ARCANA } from '../types';
import { triggerQuotaRefresh } from '../../../contexts/QuotaContext';
import './TarotAssistant.css';

interface TarotAssistantProps {
  onAnalyze: (spreadType: TarotSpreadType, question: string) => Promise<TarotReading | null>;
  onChat: (message: string, context: TarotReading) => Promise<string>;
  onBack?: () => void;
}

// 牌阵配置
const SPREAD_CONFIG: Record<TarotSpreadType, { name: string; description: string; positions: string[] }> = {
  single: { name: '单牌占卜', description: '快速获得指引', positions: ['当前状况'] },
  three_cards: { name: '三牌阵', description: '过去、现在、未来', positions: ['过去', '现在', '未来'] },
  celtic_cross: { name: '凯尔特十字', description: '深度全面分析', positions: ['现状', '障碍', '潜意识', '过去', '可能', '未来', '自我', '环境', '希望/恐惧', '结果'] },
  relationship: { name: '关系牌阵', description: '分析两人关系', positions: ['你的状态', '对方状态', '关系现状', '障碍', '建议', '未来走向'] },
  career: { name: '事业牌阵', description: '职场发展指引', positions: ['现状', '挑战', '机遇', '行动建议', '结果'] },
  yes_no: { name: '是否牌阵', description: '快速决策', positions: ['答案'] },
};

export const TarotAssistant: React.FC<TarotAssistantProps> = ({ onAnalyze, onChat, onBack }) => {
  const [step, setStep] = useState<'select_spread' | 'input_question' | 'shuffle' | 'reveal' | 'reading' | 'chat'>('select_spread');
  const [spreadType, setSpreadType] = useState<TarotSpreadType>('three_cards');
  const [question, setQuestion] = useState('');
  const [drawnCards, setDrawnCards] = useState<TarotCard[]>([]);
  const [reading, setReading] = useState<TarotReading | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [revealedCount, setRevealedCount] = useState(0);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [chatInput, setChatInput] = useState('');
  const [isChatLoading, setIsChatLoading] = useState(false);

  // 洗牌动画状态
  const [isShuffling, setIsShuffling] = useState(false);

  // 抽牌
  const drawCards = useCallback(() => {
    const config = SPREAD_CONFIG[spreadType];
    const cardCount = config.positions.length;
    const shuffled = [...MAJOR_ARCANA].sort(() => Math.random() - 0.5);
    const drawn = shuffled.slice(0, cardCount).map(card => ({
      ...card,
      isReversed: Math.random() > 0.7, // 30%概率逆位
    }));
    setDrawnCards(drawn);
    setRevealedCount(0);
    setStep('reveal');
  }, [spreadType]);

  // 洗牌
  const handleShuffle = useCallback(() => {
    setIsShuffling(true);
    setTimeout(() => {
      setIsShuffling(false);
      drawCards();
    }, 2000);
  }, [drawCards]);

  // 翻牌
  const handleReveal = useCallback((index: number) => {
    if (index === revealedCount) {
      setRevealedCount(prev => prev + 1);
    }
  }, [revealedCount]);

  // 开始解读
  const handleStartReading = useCallback(async () => {
    setIsLoading(true);
    try {
      const result = await onAnalyze(spreadType, question);
      if (result) {
        setReading(result);
        setStep('chat');
        // 刷新使用额度显示
        triggerQuotaRefresh();
      }
    } catch (error) {
      console.error('Reading failed:', error);
    } finally {
      setIsLoading(false);
    }
  }, [onAnalyze, spreadType, question]);

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
    setStep('select_spread');
    setSpreadType('three_cards');
    setQuestion('');
    setDrawnCards([]);
    setReading(null);
    setRevealedCount(0);
    setMessages([]);
  };

  // 渲染牌阵选择
  const renderSpreadSelection = () => (
    <div className="tarot-spread-selection">
      <h2>🔮 选择牌阵</h2>
      <p className="subtitle">不同牌阵适合不同类型的问题</p>
      
      <div className="spread-grid">
        {Object.entries(SPREAD_CONFIG).map(([key, config]) => (
          <div
            key={key}
            className={`spread-card ${spreadType === key ? 'selected' : ''}`}
            onClick={() => setSpreadType(key as TarotSpreadType)}
          >
            <div className="spread-icon">{getSpreadIcon(key as TarotSpreadType)}</div>
            <h3>{config.name}</h3>
            <p>{config.description}</p>
            <span className="card-count">{config.positions.length} 张牌</span>
          </div>
        ))}
      </div>

      <button className="tarot-btn primary" onClick={() => setStep('input_question')}>
        下一步
      </button>
    </div>
  );

  // 渲染问题输入
  const renderQuestionInput = () => (
    <div className="tarot-question-input">
      <h2>💭 说出你的问题</h2>
      <p className="subtitle">心中想着你的问题，让塔罗给你指引</p>
      
      <div className="spread-preview">
        <span className="spread-tag">{SPREAD_CONFIG[spreadType].name}</span>
      </div>

      <textarea
        value={question}
        onChange={e => setQuestion(e.target.value)}
        placeholder="例如：我的感情会有什么发展？我应该接受这份工作吗？"
        rows={4}
      />

      <div className="example-questions">
        <span>常见问题：</span>
        {['最近运势如何？', 'TA对我有感觉吗？', '这个选择是否正确？'].map(q => (
          <button key={q} className="example-btn" onClick={() => setQuestion(q)}>
            {q}
          </button>
        ))}
      </div>

      <div className="button-group">
        <button className="tarot-btn secondary" onClick={() => setStep('select_spread')}>
          返回
        </button>
        <button 
          className="tarot-btn primary" 
          onClick={() => setStep('shuffle')}
          disabled={!question.trim()}
        >
          开始洗牌
        </button>
      </div>
    </div>
  );

  // 渲染洗牌
  const renderShuffle = () => (
    <div className="tarot-shuffle">
      <h2>🎴 洗牌中...</h2>
      <p className="subtitle">心中默念你的问题</p>
      
      <div className={`shuffle-animation ${isShuffling ? 'shuffling' : ''}`}>
        {Array(7).fill(0).map((_, i) => (
          <div key={i} className="shuffle-card" style={{ '--i': i } as React.CSSProperties} />
        ))}
      </div>

      {!isShuffling && (
        <button className="tarot-btn primary glow" onClick={handleShuffle}>
          点击洗牌
        </button>
      )}
    </div>
  );

  // 渲染翻牌
  const renderReveal = () => {
    const config = SPREAD_CONFIG[spreadType];
    
    return (
      <div className="tarot-reveal">
        <h2>✨ 依次点击翻开你的牌</h2>
        
        <div className={`cards-layout ${spreadType}`}>
          {drawnCards.map((card, index) => (
            <div
              key={index}
              className={`tarot-card-slot ${index < revealedCount ? 'revealed' : ''} ${index === revealedCount ? 'ready' : ''}`}
              onClick={() => handleReveal(index)}
            >
              <div className="card-position-label">{config.positions[index]}</div>
              <div className="card-inner">
                <div className="card-back">
                  <div className="card-back-design" />
                </div>
                <div className={`card-front ${card.isReversed ? 'reversed' : ''}`}>
                  <div className="card-name">{card.name}</div>
                  <div className="card-name-en">{card.nameEn}</div>
                  {card.isReversed && <div className="reversed-badge">逆位</div>}
                </div>
              </div>
            </div>
          ))}
        </div>

        {revealedCount === drawnCards.length && (
          <button 
            className="tarot-btn primary start-reading-btn" 
            onClick={handleStartReading}
            disabled={isLoading}
          >
            {isLoading ? '解读中...' : '开始解读'}
          </button>
        )}
      </div>
    );
  };

  // 渲染解读中
  const renderLoading = () => (
    <div className="tarot-loading">
      <div className="crystal-ball">
        <div className="ball-inner" />
      </div>
      <p>塔罗师正在解读你的牌...</p>
    </div>
  );

  // 渲染解读结果和对话
  const renderChat = () => (
    <div className="tarot-chat">
      <div className="reading-summary">
        <h3>📜 解读结果</h3>
        
        <div className="cards-mini">
          {drawnCards.map((card, idx) => (
            <div key={idx} className={`mini-card ${card.isReversed ? 'reversed' : ''}`}>
              <span className="position">{SPREAD_CONFIG[spreadType].positions[idx]}</span>
              <span className="name">{card.name}</span>
              {card.isReversed && <span className="rev-mark">↓</span>}
            </div>
          ))}
        </div>

        {reading && (
          <div className="reading-content">
            <p className="interpretation">{reading.interpretation}</p>
            <div className="advice-box">
              <strong>💡 建议：</strong>
              <p>{reading.advice}</p>
            </div>
            {reading.luckIndex !== undefined && (
              <div className="luck-meter">
                <span>运势指数</span>
                <div className="meter-bar">
                  <div className="meter-fill" style={{ width: `${reading.luckIndex}%` }} />
                </div>
                <span>{reading.luckIndex}%</span>
              </div>
            )}
          </div>
        )}
      </div>

      <div className="chat-section">
        <h3>💬 继续追问</h3>
        <div className="chat-messages">
          {messages.length === 0 && (
            <div className="chat-hint">
              你可以继续询问塔罗师关于这次占卜的任何问题...
            </div>
          )}
          {messages.map(msg => (
            <div key={msg.id} className={`chat-message ${msg.role}`}>
              <div className="message-content">{msg.content}</div>
            </div>
          ))}
          {isChatLoading && (
            <div className="chat-message assistant">
              <div className="message-content typing">塔罗师正在回复...</div>
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

      <button className="tarot-btn secondary restart" onClick={handleReset}>
        🔄 重新占卜
      </button>
    </div>
  );

  return (
    <div className="tarot-assistant">
      {onBack && (
        <button className="inner-back-button" onClick={onBack}>
          ← 返回
        </button>
      )}
      <div className="tarot-header">
        <h1>🔮 塔罗牌占卜</h1>
        <p>让古老的智慧指引你的方向</p>
      </div>

      <div className="tarot-content">
        {step === 'select_spread' && renderSpreadSelection()}
        {step === 'input_question' && renderQuestionInput()}
        {step === 'shuffle' && renderShuffle()}
        {step === 'reveal' && renderReveal()}
        {step === 'reading' && isLoading && renderLoading()}
        {step === 'chat' && renderChat()}
      </div>
    </div>
  );
};

// 获取牌阵图标
function getSpreadIcon(type: TarotSpreadType): string {
  const icons: Record<TarotSpreadType, string> = {
    single: '🎴',
    three_cards: '🃏🃏🃏',
    celtic_cross: '✝️',
    relationship: '💕',
    career: '💼',
    yes_no: '❓',
  };
  return icons[type];
}

export default TarotAssistant;
