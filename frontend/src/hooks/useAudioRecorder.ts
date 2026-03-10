import { useState, useRef, useCallback, useEffect } from 'react';

export interface AudioRecorderState {
  isRecording: boolean;
  isPaused: boolean;
  recordingTime: number; // 录音时长（秒）
  error: string | null;
}

export interface UseAudioRecorderReturn {
  state: AudioRecorderState;
  startRecording: () => Promise<void>;
  stopRecording: () => Promise<{ audioBase64: string; audioFormat: string } | null>;
  cancelRecording: () => void;
  isSupported: boolean;
}

/**
 * 将 AudioBuffer 编码为 WAV 格式
 * WAV 是 Azure Speech 原生支持的格式，无需服务端转换
 */
const encodeWav = (audioBuffer: AudioBuffer, targetSampleRate: number = 16000): ArrayBuffer => {
  // 重采样到目标采样率
  const originalSampleRate = audioBuffer.sampleRate;
  const originalLength = audioBuffer.length;
  const targetLength = Math.round(originalLength * targetSampleRate / originalSampleRate);
  
  // 获取单声道数据（取第一个声道或混合多声道）
  let channelData: Float32Array;
  if (audioBuffer.numberOfChannels === 1) {
    channelData = audioBuffer.getChannelData(0);
  } else {
    // 混合多声道为单声道
    channelData = new Float32Array(originalLength);
    for (let i = 0; i < audioBuffer.numberOfChannels; i++) {
      const channel = audioBuffer.getChannelData(i);
      for (let j = 0; j < originalLength; j++) {
        channelData[j] += channel[j] / audioBuffer.numberOfChannels;
      }
    }
  }
  
  // 简单线性插值重采样
  const resampledData = new Float32Array(targetLength);
  const ratio = originalLength / targetLength;
  for (let i = 0; i < targetLength; i++) {
    const srcIndex = i * ratio;
    const srcIndexFloor = Math.floor(srcIndex);
    const srcIndexCeil = Math.min(srcIndexFloor + 1, originalLength - 1);
    const t = srcIndex - srcIndexFloor;
    resampledData[i] = channelData[srcIndexFloor] * (1 - t) + channelData[srcIndexCeil] * t;
  }
  
  // 转换为 16-bit PCM
  const pcmData = new Int16Array(targetLength);
  for (let i = 0; i < targetLength; i++) {
    const s = Math.max(-1, Math.min(1, resampledData[i]));
    pcmData[i] = s < 0 ? s * 0x8000 : s * 0x7FFF;
  }
  
  // 构建 WAV 文件
  const wavBuffer = new ArrayBuffer(44 + pcmData.length * 2);
  const view = new DataView(wavBuffer);
  
  // RIFF header
  writeString(view, 0, 'RIFF');
  view.setUint32(4, 36 + pcmData.length * 2, true);
  writeString(view, 8, 'WAVE');
  
  // fmt chunk
  writeString(view, 12, 'fmt ');
  view.setUint32(16, 16, true); // chunk size
  view.setUint16(20, 1, true); // PCM format
  view.setUint16(22, 1, true); // mono
  view.setUint32(24, targetSampleRate, true); // sample rate
  view.setUint32(28, targetSampleRate * 2, true); // byte rate
  view.setUint16(32, 2, true); // block align
  view.setUint16(34, 16, true); // bits per sample
  
  // data chunk
  writeString(view, 36, 'data');
  view.setUint32(40, pcmData.length * 2, true);
  
  // PCM data
  const pcmView = new Int16Array(wavBuffer, 44);
  pcmView.set(pcmData);
  
  return wavBuffer;
};

const writeString = (view: DataView, offset: number, str: string) => {
  for (let i = 0; i < str.length; i++) {
    view.setUint8(offset + i, str.charCodeAt(i));
  }
};

/**
 * 跨平台音频录制 Hook
 * 支持所有现代浏览器（Chrome, Safari, Firefox, Edge）以及移动端
 * 自动将录制的音频转换为 WAV 格式（Azure Speech 原生支持）
 */
export const useAudioRecorder = (): UseAudioRecorderReturn => {
  const [state, setState] = useState<AudioRecorderState>({
    isRecording: false,
    isPaused: false,
    recordingTime: 0,
    error: null,
  });

  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);
  const streamRef = useRef<MediaStream | null>(null);
  const timerRef = useRef<NodeJS.Timeout | null>(null);

  // 检查浏览器是否支持录音
  const isSupported = typeof navigator !== 'undefined' && 
    !!navigator.mediaDevices && 
    !!navigator.mediaDevices.getUserMedia &&
    typeof MediaRecorder !== 'undefined';

  // 获取支持的 MIME 类型（录制用，后续会转换为 WAV）
  const getSupportedMimeType = (): string => {
    const types = [
      'audio/webm;codecs=opus',
      'audio/webm',
      'audio/ogg;codecs=opus',
      'audio/ogg',
      'audio/mp4',
    ];
    
    for (const type of types) {
      if (MediaRecorder.isTypeSupported(type)) {
        console.log('Recording MIME type:', type, '(will convert to WAV)');
        return type;
      }
    }
    
    return 'audio/webm';
  };

  // 清理资源
  const cleanup = useCallback(() => {
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }
    
    if (mediaRecorderRef.current && mediaRecorderRef.current.state !== 'inactive') {
      try {
        mediaRecorderRef.current.stop();
      } catch (e) {
        // 忽略错误
      }
    }
    mediaRecorderRef.current = null;
    
    if (streamRef.current) {
      streamRef.current.getTracks().forEach(track => track.stop());
      streamRef.current = null;
    }
    
    audioChunksRef.current = [];
  }, []);

  // 开始录音
  const startRecording = useCallback(async () => {
    if (!isSupported) {
      setState(prev => ({ ...prev, error: '您的浏览器不支持录音功能' }));
      return;
    }

    // 先清理之前的录音
    cleanup();

    try {
      // 请求麦克风权限
      const stream = await navigator.mediaDevices.getUserMedia({ 
        audio: {
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true,
        } 
      });
      streamRef.current = stream;

      const mimeType = getSupportedMimeType();
      const mediaRecorder = new MediaRecorder(stream, { mimeType });
      mediaRecorderRef.current = mediaRecorder;
      audioChunksRef.current = [];

      mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          audioChunksRef.current.push(event.data);
        }
      };

      mediaRecorder.onerror = () => {
        setState(prev => ({ ...prev, error: '录音出错，请重试', isRecording: false }));
        cleanup();
      };

      // 每 100ms 收集一次数据
      mediaRecorder.start(100);

      // 开始计时
      setState({
        isRecording: true,
        isPaused: false,
        recordingTime: 0,
        error: null,
      });

      timerRef.current = setInterval(() => {
        setState(prev => ({ ...prev, recordingTime: prev.recordingTime + 1 }));
      }, 1000);

    } catch (error) {
      let errorMessage = '无法访问麦克风';
      
      if (error instanceof Error) {
        if (error.name === 'NotAllowedError' || error.name === 'PermissionDeniedError') {
          errorMessage = '请允许访问麦克风';
        } else if (error.name === 'NotFoundError' || error.name === 'DevicesNotFoundError') {
          errorMessage = '未检测到麦克风设备';
        } else if (error.name === 'NotReadableError' || error.name === 'TrackStartError') {
          errorMessage = '麦克风被其他应用占用';
        }
      }
      
      setState(prev => ({ ...prev, error: errorMessage, isRecording: false }));
      cleanup();
    }
  }, [isSupported, cleanup]);

  // 停止录音并返回音频数据（自动转换为 WAV 格式）
  const stopRecording = useCallback(async (): Promise<{ audioBase64: string; audioFormat: string } | null> => {
    return new Promise((resolve) => {
      if (!mediaRecorderRef.current || mediaRecorderRef.current.state === 'inactive') {
        cleanup();
        setState(prev => ({ ...prev, isRecording: false, recordingTime: 0 }));
        resolve(null);
        return;
      }

      const mediaRecorder = mediaRecorderRef.current;
      const mimeType = mediaRecorder.mimeType;

      mediaRecorder.onstop = async () => {
        try {
          if (audioChunksRef.current.length === 0) {
            resolve(null);
            return;
          }

          const audioBlob = new Blob(audioChunksRef.current, { type: mimeType });
          
          // 使用 Web Audio API 解码并转换为 WAV
          console.log('Converting audio to WAV format...');
          const arrayBuffer = await audioBlob.arrayBuffer();
          const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
          
          try {
            const audioBuffer = await audioContext.decodeAudioData(arrayBuffer);
            console.log('Audio decoded:', audioBuffer.duration, 'seconds,', audioBuffer.sampleRate, 'Hz');
            
            // 编码为 WAV (16kHz, 单声道, 16-bit PCM)
            const wavBuffer = encodeWav(audioBuffer, 16000);
            console.log('WAV encoded:', wavBuffer.byteLength, 'bytes');
            
            // 转换为 Base64
            const wavArray = new Uint8Array(wavBuffer);
            let binary = '';
            for (let i = 0; i < wavArray.length; i++) {
              binary += String.fromCharCode(wavArray[i]);
            }
            const base64Data = btoa(binary);
            
            setState(prev => ({ ...prev, isRecording: false, recordingTime: 0 }));
            cleanup();
            audioContext.close();
            
            resolve({
              audioBase64: base64Data,
              audioFormat: 'wav',
            });
          } catch (decodeError) {
            console.error('Failed to decode audio:', decodeError);
            // 解码失败时回退到原始格式
            const reader = new FileReader();
            reader.onloadend = () => {
              const base64String = reader.result as string;
              const base64Data = base64String.split(',')[1];
              
              setState(prev => ({ ...prev, isRecording: false, recordingTime: 0 }));
              cleanup();
              audioContext.close();
              
              const format = mimeType.includes('webm') ? 'webm' : 
                           mimeType.includes('ogg') ? 'ogg' : 
                           mimeType.includes('mp4') ? 'm4a' : 'webm';
              
              resolve({
                audioBase64: base64Data,
                audioFormat: format,
              });
            };
            reader.readAsDataURL(audioBlob);
          }
        } catch (error) {
          console.error('Audio processing error:', error);
          setState(prev => ({ ...prev, error: '音频处理失败' }));
          cleanup();
          resolve(null);
        }
      };

      mediaRecorder.stop();
    });
  }, [cleanup]);

  // 取消录音
  const cancelRecording = useCallback(() => {
    cleanup();
    setState({
      isRecording: false,
      isPaused: false,
      recordingTime: 0,
      error: null,
    });
  }, [cleanup]);

  // 组件卸载时清理
  useEffect(() => {
    return () => {
      cleanup();
    };
  }, [cleanup]);

  return {
    state,
    startRecording,
    stopRecording,
    cancelRecording,
    isSupported,
  };
};

export default useAudioRecorder;
