import React, { useState, useRef, useEffect, useCallback } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { sendChatMessageWithRag, ChatMessage, MessageContent, SourceReference } from '../services/api';
import { getChatSessionDetail, ChatMessageDto } from '../services/authService';
import { useAuth } from '../contexts/AuthContext';
import { triggerQuotaRefresh } from '../contexts/QuotaContext';
import ApprovalStatus from './ApprovalStatus';
import markdownExportService, { ExportableMessage } from '../services/markdownExportService';
import VoiceRecordButton from './VoiceRecordButton';
import VoicePlayButton from './VoicePlayButton';
import './ChatInterface.css';

interface DisplayMessage {
  role: 'user' | 'assistant';
  content: string;
  images?: string[];
  timestamp: Date;
  sources?: SourceReference[]; // RAG 来源引用
  usedWebSearch?: boolean;     // 是否使用了 Web 搜索
}

interface ChatInterfaceProps {
  sessionId?: string | null;
  onSessionChange?: (sessionId: string, title: string) => void;
}

const ChatInterface: React.FC<ChatInterfaceProps> = ({ sessionId, onSessionChange }) => {
  const { isLoggedIn, user, refreshUser } = useAuth();
  const [messages, setMessages] = useState<DisplayMessage[]>([]);
  const [inputText, setInputText] = useState('');
  const [selectedImages, setSelectedImages] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [temperature, setTemperature] = useState(0.7);
  const [enableWebSearch, setEnableWebSearch] = useState(true); // RAG Web搜索开关
  const [showSettings, setShowSettings] = useState(false);
  const [currentSessionId, setCurrentSessionId] = useState<string | null>(sessionId || null);
  const [exportMode, setExportMode] = useState(false);
  const [selectedMsgIndices, setSelectedMsgIndices] = useState<Set<number>>(new Set());
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // 检查用户是否已被批准
  const isApproved = user?.isApproved || user?.isAdmin;

  // 加载会话详情
  const loadSession = useCallback(async (sid: string) => {
    if (!isLoggedIn) return;
    
    setIsLoading(true);
    try {
      const result = await getChatSessionDetail(sid);
      if (result.success && result.session) {
        const loadedMessages: DisplayMessage[] = result.session.messages.map((msg: ChatMessageDto) => ({
          role: msg.role as 'user' | 'assistant',
          content: msg.textContent || '',
          images: msg.imageUrls,
          timestamp: new Date(msg.createdAt)
        }));
        setMessages(loadedMessages);
      }
    } finally {
      setIsLoading(false);
    }
  }, [isLoggedIn]);

  // 当外部sessionId变化时加载会话
  useEffect(() => {
    if (sessionId !== undefined) {
      setCurrentSessionId(sessionId);
      if (sessionId) {
        loadSession(sessionId);
      } else {
        // 新对话
        setMessages([]);
      }
    }
  }, [sessionId, loadSession]);

// ...existing code...
  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  // 处理粘贴事件（支持直接粘贴截图）
  const handlePaste = (e: React.ClipboardEvent) => {
    const items = e.clipboardData?.items;
    if (!items) return;

    const imageItems = Array.from(items).filter(item => item.type.startsWith('image/'));
    if (imageItems.length === 0) return;

    e.preventDefault(); // 阻止默认粘贴行为

    const imagePromises = imageItems.map(item => {
      return new Promise<string>((resolve, reject) => {
        const blob = item.getAsFile();
        if (!blob) {
          reject(new Error('无法获取图片'));
          return;
        }
        const reader = new FileReader();
        reader.onload = (event) => {
          resolve(event.target?.result as string);
        };
        reader.onerror = reject;
        reader.readAsDataURL(blob);
      });
    });

    Promise.all(imagePromises).then(images => {
      setSelectedImages(prev => [...prev, ...images].slice(0, 5)); // 最多5张图片
    });
  };

  // 处理图片选择
  const handleImageSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files) return;

    const imagePromises = Array.from(files).map(file => {
      return new Promise<string>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = (event) => {
          resolve(event.target?.result as string);
        };
        reader.onerror = reject;
        reader.readAsDataURL(file);
      });
    });

    Promise.all(imagePromises).then(images => {
      setSelectedImages(prev => [...prev, ...images].slice(0, 5)); // 最多5张图片
    });
  };

  // 移除选中的图片
  const removeImage = (index: number) => {
    setSelectedImages(prev => prev.filter((_, i) => i !== index));
  };

  // 构建API消息格式
  const buildApiMessages = (): ChatMessage[] => {
    return messages.map(msg => {
      const content: MessageContent[] = [];
      
      // 添加图片内容
      if (msg.images && msg.images.length > 0) {
        msg.images.forEach(img => {
          content.push({
            type: 'image_url',
            imageUrl: { url: img }
          });
        });
      }
      
      // 添加文本内容
      if (msg.content) {
        content.push({
          type: 'text',
          text: msg.content
        });
      }

      return {
        role: msg.role,
        content: content
      };
    });
  };

  // 发送消息
  const handleSend = async () => {
    if (!inputText.trim() && selectedImages.length === 0) return;

    // 检查是否已登录
    if (!isLoggedIn) {
      const errorMessage: DisplayMessage = {
        role: 'assistant',
        content: '请先登录后再使用聊天功能',
        timestamp: new Date()
      };
      setMessages(prev => [...prev, errorMessage]);
      return;
    }

    // 检查是否已被批准
    if (!isApproved) {
      const errorMessage: DisplayMessage = {
        role: 'assistant',
        content: '⚠️ 您的账户尚未获得使用权限。请点击上方的「申请使用权限」按钮提交申请，等待管理员审批后即可使用。',
        timestamp: new Date()
      };
      setMessages(prev => [...prev, errorMessage]);
      return;
    }

    const userMessage: DisplayMessage = {
      role: 'user',
      content: inputText,
      images: selectedImages.length > 0 ? [...selectedImages] : undefined,
      timestamp: new Date()
    };

    const currentInput = inputText;
    const currentImages = [...selectedImages];
    
    setMessages(prev => [...prev, userMessage]);
    setInputText('');
    setSelectedImages([]);
    setIsLoading(true);

    try {
      // 构建完整的消息历史
      const apiMessages = [...buildApiMessages(), {
        role: 'user' as const,
        content: [
          ...currentImages.map(img => ({
            type: 'image_url' as const,
            imageUrl: { url: img }
          })),
          ...(currentInput ? [{
            type: 'text' as const,
            text: currentInput
          }] : [])
        ]
      }];

      // 使用 RAG API（支持 Web 搜索）
      const response = await sendChatMessageWithRag({
        messages: apiMessages,
        temperature: temperature,
        maxTokens: 2000,
        sessionId: currentSessionId || undefined,
        enableWebSearch: enableWebSearch
      });

      if (response.success && response.message) {
        const assistantMessage: DisplayMessage = {
          role: 'assistant',
          content: response.message,
          timestamp: new Date(),
          sources: response.sources,
          usedWebSearch: response.usedWebSearch
        };
        setMessages(prev => [...prev, assistantMessage]);

        // 更新会话ID
        if (response.sessionId && response.sessionId !== currentSessionId) {
          setCurrentSessionId(response.sessionId);
          if (onSessionChange) {
            onSessionChange(response.sessionId, response.sessionTitle || '新对话');
          }
        }

        // 刷新使用额度显示
        triggerQuotaRefresh();
      } else {
        const errorMessage: DisplayMessage = {
          role: 'assistant',
          content: `错误: ${response.error || '未知错误'}`,
          timestamp: new Date()
        };
        setMessages(prev => [...prev, errorMessage]);
      }
    } catch (error) {
      const errorMessage: DisplayMessage = {
        role: 'assistant',
        content: `错误: ${error instanceof Error ? error.message : '未知错误'}`,
        timestamp: new Date()
      };
      setMessages(prev => [...prev, errorMessage]);
    } finally {
      setIsLoading(false);
    }
  };

  // 清空对话（开始新会话）
  const handleClear = () => {
    setMessages([]);
    setInputText('');
    setSelectedImages([]);
    setCurrentSessionId(null);
  };

  // 处理回车发送
  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  // === 导出 Markdown 功能 ===

  // 获取所有 assistant 消息的索引
  const assistantIndices = messages
    .map((msg, idx) => (msg.role === 'assistant' ? idx : -1))
    .filter(idx => idx !== -1);

  // 切换导出模式
  const toggleExportMode = () => {
    setExportMode(prev => {
      if (prev) setSelectedMsgIndices(new Set()); // 退出时清除选择
      return !prev;
    });
  };

  // 切换单条消息选择
  const toggleMessageSelection = (index: number) => {
    setSelectedMsgIndices(prev => {
      const next = new Set(prev);
      if (next.has(index)) {
        next.delete(index);
      } else {
        next.add(index);
      }
      return next;
    });
  };

  // 全选 / 取消全选
  const toggleSelectAll = () => {
    if (selectedMsgIndices.size === assistantIndices.length) {
      setSelectedMsgIndices(new Set());
    } else {
      setSelectedMsgIndices(new Set(assistantIndices));
    }
  };

  // 执行导出
  const handleExportMarkdown = () => {
    const selected: ExportableMessage[] = Array.from(selectedMsgIndices)
      .sort((a, b) => a - b)
      .map(idx => messages[idx]);
    if (selected.length === 0) return;
    markdownExportService.export(selected, '智能助手对话');
    // 导出后退出导出模式
    setExportMode(false);
    setSelectedMsgIndices(new Set());
  };

  return (
    <div className="chat-interface">
      <div className="chat-header">
        <div className="chat-title">
          <h2>智能助手</h2>
          <span className="model-badge">GPT-5.4</span>
          <span className="model-badge">支持图片识别</span>
        </div>
        <div className="chat-actions">
          <button
            className={`export-mode-button ${exportMode ? 'active' : ''}`}
            onClick={toggleExportMode}
            disabled={assistantIndices.length === 0}
            title={exportMode ? '退出导出模式' : '导出 Markdown'}
          >
            📥
          </button>
          <button 
            className="settings-button"
            onClick={() => setShowSettings(!showSettings)}
            title="设置"
          >
            ⚙️
          </button>
          <button 
            className="clear-button"
            onClick={handleClear}
            disabled={messages.length === 0}
            title="清空对话"
          >
            🗑️
          </button>
        </div>
      </div>

      {showSettings && (
        <div className="settings-panel">
          <div className="setting-item">
            <label>温度 (Temperature): {temperature}</label>
            <input
              type="range"
              min="0"
              max="1"
              step="0.1"
              value={temperature}
              onChange={(e) => setTemperature(parseFloat(e.target.value))}
            />
          </div>
          <div className="setting-item">
            <label className="toggle-label">
              <input
                type="checkbox"
                checked={enableWebSearch}
                onChange={(e) => setEnableWebSearch(e.target.checked)}
              />
              <span className="toggle-text">🌐 启用 Web 搜索 (RAG)</span>
            </label>
            <p className="setting-description">
              启用后，AI 可自动搜索网络获取实时信息（如天气、新闻等）
            </p>
          </div>
        </div>
      )}

      {exportMode && (
        <div className="export-toolbar">
          <div className="export-toolbar-left">
            <label className="select-all-label">
              <input
                type="checkbox"
                checked={selectedMsgIndices.size === assistantIndices.length && assistantIndices.length > 0}
                onChange={toggleSelectAll}
              />
              全选
            </label>
            <span className="export-count">已选择 {selectedMsgIndices.size} / {assistantIndices.length} 条回复</span>
          </div>
          <div className="export-toolbar-right">
            <button
              className="export-cancel-button"
              onClick={toggleExportMode}
            >
              取消
            </button>
            <button
              className="export-confirm-button"
              onClick={handleExportMarkdown}
              disabled={selectedMsgIndices.size === 0}
            >
              📄 导出 Markdown ({selectedMsgIndices.size})
            </button>
          </div>
        </div>
      )}

      <div className="chat-messages">
        {messages.length === 0 && (
          <div className="welcome-message">
            <h3>👋 欢迎使用智能助手</h3>
            <p>支持文本对话和图片识别功能</p>
            <div className="feature-list">
              <div className="feature-item">💬 多轮对话</div>
              <div className="feature-item">🖼️ 图片识别</div>
              <div className="feature-item">🧠 上下文理解</div>
              <div className="feature-item">📋 粘贴截图</div>
            </div>
            
            {/* 未登录提示 */}
            {!isLoggedIn && (
              <div className="chat-approval-notice">
                <p>🔐 请先登录后使用聊天功能</p>
              </div>
            )}
            
            {/* 已登录但未批准提示 */}
            {isLoggedIn && !isApproved && (
              <div className="chat-approval-notice">
                <h3>🔒 需要使用权限</h3>
                <p>您需要申请并获得管理员批准后才能使用 AI 功能</p>
                <ApprovalStatus onStatusChange={refreshUser} />
              </div>
            )}
          </div>
        )}

        {messages.map((msg, index) => (
          <div key={index} className={`message ${msg.role} ${exportMode && msg.role === 'assistant' ? 'export-selectable' : ''} ${selectedMsgIndices.has(index) ? 'export-selected' : ''}`}>
            {exportMode && msg.role === 'assistant' && (
              <div className="export-checkbox-wrapper">
                <input
                  type="checkbox"
                  className="export-checkbox"
                  checked={selectedMsgIndices.has(index)}
                  onChange={() => toggleMessageSelection(index)}
                />
              </div>
            )}
            <div className="message-avatar">
              {msg.role === 'user' ? '👤' : '🤖'}
            </div>
            <div className="message-content">
              {msg.images && msg.images.length > 0 && (
                <div className="message-images">
                  {msg.images.map((img, imgIndex) => (
                    <img 
                      key={imgIndex} 
                      src={img} 
                      alt={`上传的图片 ${imgIndex + 1}`}
                      className="message-image"
                    />
                  ))}
                </div>
              )}
              <div className="message-text">
                {msg.role === 'assistant' ? (
                  <ReactMarkdown 
                    remarkPlugins={[remarkGfm]}
                    components={{
                      // 自定义链接渲染 - 在新窗口打开
                      a: ({node, children, ...props}) => (
                        <a {...props} target="_blank" rel="noopener noreferrer">
                          {children || props.href || '链接'}
                        </a>
                      ),
                      // 简化段落渲染
                      p: ({node, ...props}) => <p {...props} />,
                    }}
                  >
                    {msg.content}
                  </ReactMarkdown>
                ) : (
                  msg.content
                )}
              </div>
              {/* RAG 来源引用 - 美化显示 */}
              {msg.role === 'assistant' && msg.sources && msg.sources.length > 0 && (
                <div className="message-sources">
                  <div className="sources-header">
                    <span className="sources-icon">📚</span>
                    <span>参考来源 ({msg.sources.length})</span>
                  </div>
                  <div className="sources-list">
                    {msg.sources.map((source, idx) => (
                      <div key={idx} className="source-item">
                        <div className="source-number">{idx + 1}</div>
                        <div className="source-content">
                          <a 
                            href={source.url} 
                            target="_blank" 
                            rel="noopener noreferrer"
                            title={source.url}
                          >
                            {source.title || '查看来源'}
                          </a>
                          {source.snippet && (
                            <p className="source-snippet">{source.snippet}</p>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
              <div className="message-timestamp">
                {msg.timestamp.toLocaleTimeString()}
                {msg.role === 'assistant' && msg.usedWebSearch && (
                  <span className="web-search-badge" title="已通过网络搜索获取实时信息">🌐 已联网</span>
                )}
                {msg.role === 'assistant' && msg.content && (
                  <VoicePlayButton text={msg.content} size="small" />
                )}
              </div>
            </div>
          </div>
        ))}

        {isLoading && (
          <div className="message assistant">
            <div className="message-avatar">🤖</div>
            <div className="message-content">
              <div className="typing-indicator">
                <span></span>
                <span></span>
                <span></span>
              </div>
            </div>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      <div className="chat-input-container">
        {selectedImages.length > 0 && (
          <div className="selected-images">
            {selectedImages.map((img, index) => (
              <div key={index} className="selected-image">
                <img src={img} alt={`选择的图片 ${index + 1}`} />
                <button 
                  className="remove-image"
                  onClick={() => removeImage(index)}
                >
                  ×
                </button>
              </div>
            ))}
          </div>
        )}

        <div className="chat-input">
          <button
            className="image-button"
            onClick={() => fileInputRef.current?.click()}
            disabled={isLoading || selectedImages.length >= 5}
            title="上传图片 (最多5张)"
          >
            🖼️
          </button>
          <input
            ref={fileInputRef}
            type="file"
            accept="image/*"
            multiple
            onChange={handleImageSelect}
            style={{ display: 'none' }}
          />
          <VoiceRecordButton
            onTranscribed={(text) => setInputText(prev => prev + text)}
            disabled={isLoading}
          />
          <textarea
            ref={textareaRef}
            value={inputText}
            onChange={(e) => setInputText(e.target.value)}
            onKeyPress={handleKeyPress}
            onPaste={handlePaste}
            placeholder="输入消息... (Ctrl+V 粘贴截图，Shift+Enter 换行，Enter 发送)"
            disabled={isLoading}
            rows={1}
          />
          <button
            className="send-button"
            onClick={handleSend}
            disabled={isLoading || (!inputText.trim() && selectedImages.length === 0)}
          >
            {isLoading ? '发送中...' : '发送'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default ChatInterface;
