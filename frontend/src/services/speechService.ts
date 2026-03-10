import { getApiBaseUrl } from './config';
import { authStorage } from './authService';

// 获取 API 基础 URL
const getApiUrl = () => getApiBaseUrl();

// =====================
// 类型定义
// =====================

export interface SpeechToTextRequest {
  audioBase64: string;
  audioFormat: string;
  language?: string;
}

export interface SpeechToTextResponse {
  success: boolean;
  text?: string;
  error?: string;
  confidence?: number;
}

export interface TextToSpeechRequest {
  text: string;
  voiceName?: string;
  rate?: number;
  pitch?: number;
  outputFormat?: string;
}

export interface TextToSpeechResponse {
  success: boolean;
  audioBase64?: string;
  audioFormat?: string;
  error?: string;
}

export interface VoiceInfo {
  name: string;
  displayName: string;
  localName: string;
  gender: string;
  locale: string;
}

export interface AvailableVoicesResponse {
  success: boolean;
  voices: VoiceInfo[];
  error?: string;
}

// =====================
// API 调用
// =====================

/**
 * 语音转文字 (STT)
 */
export const transcribeSpeech = async (request: SpeechToTextRequest): Promise<SpeechToTextResponse> => {
  const token = authStorage.getToken();
  
  if (!token) {
    return {
      success: false,
      error: '请先登录',
    };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/speech/transcribe`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
      },
      body: JSON.stringify({
        audioBase64: request.audioBase64,
        audioFormat: request.audioFormat,
        language: request.language || 'zh-CN',
      }),
    });

    if (!response.ok) {
      if (response.status === 401) {
        authStorage.clear();
        return {
          success: false,
          error: '登录已过期，请重新登录',
        };
      }
      if (response.status === 403) {
        return {
          success: false,
          error: '您没有权限使用此功能',
        };
      }
      const errorData = await response.json().catch(() => ({}));
      return {
        success: false,
        error: errorData.error || `请求失败: ${response.status}`,
      };
    }

    return await response.json();
  } catch (error) {
    console.error('Speech transcription error:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : '语音识别失败',
    };
  }
};

/**
 * 文字转语音 (TTS)
 */
export const synthesizeSpeech = async (request: TextToSpeechRequest): Promise<TextToSpeechResponse> => {
  const token = authStorage.getToken();
  
  if (!token) {
    return {
      success: false,
      error: '请先登录',
    };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/speech/synthesize`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
      },
      body: JSON.stringify({
        text: request.text,
        voiceName: request.voiceName || 'zh-CN-XiaoxiaoNeural',
        rate: request.rate || 1.0,
        pitch: request.pitch || 1.0,
        outputFormat: request.outputFormat || 'mp3',
      }),
    });

    if (!response.ok) {
      if (response.status === 401) {
        authStorage.clear();
        return {
          success: false,
          error: '登录已过期，请重新登录',
        };
      }
      if (response.status === 403) {
        return {
          success: false,
          error: '您没有权限使用此功能',
        };
      }
      const errorData = await response.json().catch(() => ({}));
      return {
        success: false,
        error: errorData.error || `请求失败: ${response.status}`,
      };
    }

    return await response.json();
  } catch (error) {
    console.error('Speech synthesis error:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : '语音合成失败',
    };
  }
};

/**
 * 获取可用语音列表
 */
export const getAvailableVoices = async (locale: string = 'zh-CN'): Promise<AvailableVoicesResponse> => {
  const token = authStorage.getToken();
  
  if (!token) {
    return {
      success: false,
      voices: [],
      error: '请先登录',
    };
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/speech/voices?locale=${encodeURIComponent(locale)}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    });

    if (!response.ok) {
      return {
        success: false,
        voices: [],
        error: `请求失败: ${response.status}`,
      };
    }

    return await response.json();
  } catch (error) {
    console.error('Get voices error:', error);
    return {
      success: false,
      voices: [],
      error: error instanceof Error ? error.message : '获取语音列表失败',
    };
  }
};

// =====================
// 音频播放工具
// =====================

let currentAudio: HTMLAudioElement | null = null;

/**
 * 播放 Base64 音频
 */
export const playAudioBase64 = (audioBase64: string, format: string = 'mp3'): Promise<void> => {
  return new Promise((resolve, reject) => {
    // 停止当前播放的音频
    if (currentAudio) {
      currentAudio.pause();
      currentAudio = null;
    }

    try {
      const audio = new Audio(`data:audio/${format};base64,${audioBase64}`);
      currentAudio = audio;
      
      audio.onended = () => {
        currentAudio = null;
        resolve();
      };
      
      audio.onerror = (e) => {
        currentAudio = null;
        reject(new Error('音频播放失败'));
      };

      audio.play().catch(reject);
    } catch (error) {
      reject(error);
    }
  });
};

/**
 * 停止当前播放的音频
 */
export const stopAudio = () => {
  if (currentAudio) {
    currentAudio.pause();
    currentAudio = null;
  }
};

/**
 * 检查是否正在播放
 */
export const isAudioPlaying = (): boolean => {
  return currentAudio !== null && !currentAudio.paused;
};
