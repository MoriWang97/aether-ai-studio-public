import React, { useState, useCallback, useRef, useEffect } from 'react';
import { useAudioRecorder } from '../hooks/useAudioRecorder';
import { transcribeSpeech } from '../services/speechService';
import './VoiceRecordButton.css';

interface VoiceRecordButtonProps {
  onTranscribed: (text: string) => void;
  disabled?: boolean;
  className?: string;
  language?: 'zh-CN' | 'en-US' | 'ja-JP' | 'auto';  // 支持的语言
}

/**
 * 语音录制按钮组件
 * 支持点击录音和长按录音两种模式
 */
const VoiceRecordButton: React.FC<VoiceRecordButtonProps> = ({
  onTranscribed,
  disabled = false,
  className = '',
  language = 'auto',  // 默认自动检测
}) => {
  const { state, startRecording, stopRecording, cancelRecording, isSupported } = useAudioRecorder();
  const [isTranscribing, setIsTranscribing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const longPressTimer = useRef<NodeJS.Timeout | null>(null);
  const isLongPress = useRef(false);

  // 格式化录音时间
  const formatTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  // 处理录音完成
  const handleStopAndTranscribe = useCallback(async () => {
    const audioData = await stopRecording();
    
    if (!audioData) {
      setError('录音失败，请重试');
      return;
    }

    // 调用语音转文字 API
    setIsTranscribing(true);
    setError(null);

    try {
      // 获取语言设置（auto 时使用浏览器语言）
      const detectLanguage = (): string => {
        if (language !== 'auto') return language;
        const browserLang = navigator.language.toLowerCase();
        if (browserLang.startsWith('zh')) return 'zh-CN';
        if (browserLang.startsWith('ja')) return 'ja-JP';
        if (browserLang.startsWith('en')) return 'en-US';
        return 'zh-CN';  // 默认中文
      };

      const result = await transcribeSpeech({
        audioBase64: audioData.audioBase64,
        audioFormat: audioData.audioFormat,
        language: detectLanguage(),
      });

      if (result.success && result.text) {
        onTranscribed(result.text);
      } else {
        setError(result.error || '语音识别失败');
      }
    } catch (err) {
      setError('语音识别失败，请重试');
    } finally {
      setIsTranscribing(false);
    }
  }, [stopRecording, onTranscribed, language]);

  // 点击/长按开始
  const handlePointerDown = useCallback(() => {
    if (disabled || !isSupported) return;

    isLongPress.current = false;
    
    // 如果正在录音，则停止并转换
    if (state.isRecording) {
      handleStopAndTranscribe();
      return;
    }

    // 设置长按计时器
    longPressTimer.current = setTimeout(() => {
      isLongPress.current = true;
    }, 200);

    // 立即开始录音
    setError(null);
    startRecording();
  }, [disabled, isSupported, state.isRecording, handleStopAndTranscribe, startRecording]);

  // 松开
  const handlePointerUp = useCallback(() => {
    if (longPressTimer.current) {
      clearTimeout(longPressTimer.current);
      longPressTimer.current = null;
    }

    // 如果是长按模式，松开时停止录音
    if (isLongPress.current && state.isRecording) {
      handleStopAndTranscribe();
    }
    // 点击模式下，需要再次点击才停止（在 handlePointerDown 中处理）
  }, [state.isRecording, handleStopAndTranscribe]);

  // 取消（移出按钮区域）
  const handlePointerLeave = useCallback(() => {
    if (longPressTimer.current) {
      clearTimeout(longPressTimer.current);
      longPressTimer.current = null;
    }

    // 长按模式下，移出取消录音
    if (isLongPress.current && state.isRecording) {
      cancelRecording();
      setError('已取消录音');
    }
  }, [state.isRecording, cancelRecording]);

  // 清理定时器
  useEffect(() => {
    return () => {
      if (longPressTimer.current) {
        clearTimeout(longPressTimer.current);
      }
    };
  }, []);

  // 自动清除错误
  useEffect(() => {
    if (error) {
      const timer = setTimeout(() => setError(null), 3000);
      return () => clearTimeout(timer);
    }
  }, [error]);

  // 不支持录音
  if (!isSupported) {
    return (
      <button
        className={`voice-record-button unsupported ${className}`}
        disabled
        title="您的浏览器不支持录音"
      >
        🎤
      </button>
    );
  }

  const isActive = state.isRecording || isTranscribing;
  const displayError = error || state.error;

  return (
    <div className="voice-record-wrapper">
      <button
        className={`voice-record-button ${isActive ? 'active' : ''} ${isTranscribing ? 'transcribing' : ''} ${className}`}
        onPointerDown={handlePointerDown}
        onPointerUp={handlePointerUp}
        onPointerLeave={handlePointerLeave}
        disabled={disabled || isTranscribing}
        title={state.isRecording ? '点击停止录音' : '点击开始录音 / 长按录音'}
      >
        {isTranscribing ? (
          <span className="transcribing-icon">⏳</span>
        ) : state.isRecording ? (
          <span className="recording-icon">⏹️</span>
        ) : (
          <span className="mic-icon">🎤</span>
        )}
      </button>
      
      {/* 录音状态指示 */}
      {state.isRecording && (
        <div className="recording-indicator">
          <span className="recording-dot"></span>
          <span className="recording-time">{formatTime(state.recordingTime)}</span>
        </div>
      )}

      {/* 转写中状态 */}
      {isTranscribing && (
        <div className="transcribing-indicator">
          <span>识别中...</span>
        </div>
      )}

      {/* 错误提示 */}
      {displayError && (
        <div className="voice-error-tooltip">
          {displayError}
        </div>
      )}
    </div>
  );
};

export default VoiceRecordButton;
