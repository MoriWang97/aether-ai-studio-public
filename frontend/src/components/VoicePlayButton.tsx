import React, { useState, useCallback, useRef, useEffect } from 'react';
import { synthesizeSpeech, playAudioBase64, stopAudio, isAudioPlaying } from '../services/speechService';
import './VoicePlayButton.css';

interface VoicePlayButtonProps {
  text: string;
  voiceName?: string;
  rate?: number;
  className?: string;
  size?: 'small' | 'medium' | 'large';
}

// 音频缓存
const audioCache = new Map<string, { audioBase64: string; audioFormat: string }>();

/**
 * TTS 播放按钮组件
 * 点击后将文本转换为语音并播放
 */
const VoicePlayButton: React.FC<VoicePlayButtonProps> = ({
  text,
  voiceName = 'zh-CN-XiaoxiaoNeural',
  rate = 1.0,
  className = '',
  size = 'small',
}) => {
  const [isLoading, setIsLoading] = useState(false);
  const [isPlaying, setIsPlaying] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const playingRef = useRef(false);

  // 生成缓存键
  const getCacheKey = (text: string, voice: string, rate: number): string => {
    return `${voice}_${rate}_${text.slice(0, 100)}`;
  };

  // 停止播放
  const handleStop = useCallback(() => {
    stopAudio();
    setIsPlaying(false);
    playingRef.current = false;
  }, []);

  // 播放语音
  const handlePlay = useCallback(async () => {
    if (!text.trim()) {
      setError('没有可播放的内容');
      return;
    }

    // 如果正在播放，则停止
    if (isPlaying || playingRef.current) {
      handleStop();
      return;
    }

    setError(null);
    setIsLoading(true);
    playingRef.current = true;

    try {
      const cacheKey = getCacheKey(text, voiceName, rate);
      let audioData = audioCache.get(cacheKey);

      // 如果缓存中没有，则调用 API
      if (!audioData) {
        const result = await synthesizeSpeech({
          text: text,
          voiceName: voiceName,
          rate: rate,
        });

        if (!result.success || !result.audioBase64) {
          throw new Error(result.error || '语音合成失败');
        }

        audioData = {
          audioBase64: result.audioBase64,
          audioFormat: result.audioFormat || 'mp3',
        };

        // 存入缓存（限制大小）
        if (audioCache.size > 50) {
          const firstKey = audioCache.keys().next().value;
          if (firstKey) {
            audioCache.delete(firstKey);
          }
        }
        audioCache.set(cacheKey, audioData);
      }

      setIsLoading(false);
      setIsPlaying(true);

      // 播放音频
      await playAudioBase64(audioData.audioBase64, audioData.audioFormat);
      
    } catch (err) {
      setError(err instanceof Error ? err.message : '播放失败');
    } finally {
      setIsLoading(false);
      setIsPlaying(false);
      playingRef.current = false;
    }
  }, [text, voiceName, rate, isPlaying, handleStop]);

  // 自动清除错误
  useEffect(() => {
    if (error) {
      const timer = setTimeout(() => setError(null), 3000);
      return () => clearTimeout(timer);
    }
  }, [error]);

  // 组件卸载时停止播放
  useEffect(() => {
    return () => {
      if (playingRef.current) {
        stopAudio();
      }
    };
  }, []);

  // 检查音频播放状态
  useEffect(() => {
    if (isPlaying) {
      const checkInterval = setInterval(() => {
        if (!isAudioPlaying() && playingRef.current) {
          setIsPlaying(false);
          playingRef.current = false;
        }
      }, 200);
      return () => clearInterval(checkInterval);
    }
  }, [isPlaying]);

  const sizeClass = `voice-play-button--${size}`;

  return (
    <div className="voice-play-wrapper">
      <button
        className={`voice-play-button ${sizeClass} ${isPlaying ? 'playing' : ''} ${isLoading ? 'loading' : ''} ${className}`}
        onClick={handlePlay}
        disabled={isLoading}
        title={isPlaying ? '点击停止' : '朗读此消息'}
      >
        {isLoading ? (
          <span className="loading-icon">⏳</span>
        ) : isPlaying ? (
          <span className="stop-icon">⏹️</span>
        ) : (
          <span className="play-icon">🔊</span>
        )}
      </button>

      {/* 错误提示 */}
      {error && (
        <div className="voice-play-error">
          {error}
        </div>
      )}
    </div>
  );
};

export default VoicePlayButton;
