/**
 * 证据收集 Hook
 * 集成录音、拍照、文件上传等功能
 * 复用现有的 useAudioRecorder
 */

import { useState, useCallback, useRef } from 'react';
import { useAudioRecorder } from '../../../hooks/useAudioRecorder';
import type { Evidence, EvidenceType } from '../types';
import { createLegalAssistant } from '../services/legalAssistantService';

export interface UseEvidenceCollectorReturn {
  // 录音相关 (复用 useAudioRecorder)
  isRecording: boolean;
  recordingTime: number;
  startRecording: () => Promise<void>;
  stopRecording: () => Promise<Evidence | null>;
  cancelRecording: () => void;
  
  // 文件上传
  uploadFile: (file: File, type: EvidenceType) => Promise<Evidence | null>;
  uploadImage: (file: File) => Promise<Evidence | null>;
  
  // 文字转换
  transcribeEvidence: (evidence: Evidence) => Promise<string | null>;
  
  // 状态
  isProcessing: boolean;
  error: string | null;
}

export const useEvidenceCollector = (): UseEvidenceCollectorReturn => {
  const audioRecorder = useAudioRecorder();
  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const pendingEvidenceRef = useRef<Partial<Evidence>>({});

  // 开始录音
  const startRecording = useCallback(async () => {
    setError(null);
    pendingEvidenceRef.current = {
      type: 'audio',
      name: `录音_${new Date().toLocaleString('zh-CN')}`,
    };
    await audioRecorder.startRecording();
  }, [audioRecorder]);

  // 停止录音并生成证据
  const stopRecording = useCallback(async (): Promise<Evidence | null> => {
    const result = await audioRecorder.stopRecording();
    
    if (!result) {
      setError('录音失败');
      return null;
    }

    const evidence: Evidence = {
      id: `evidence_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      type: 'audio',
      name: pendingEvidenceRef.current.name || `录音_${new Date().toLocaleString('zh-CN')}`,
      data: result.audioBase64,
      createdAt: new Date(),
    };

    return evidence;
  }, [audioRecorder]);

  // 取消录音
  const cancelRecording = useCallback(() => {
    audioRecorder.cancelRecording();
    pendingEvidenceRef.current = {};
  }, [audioRecorder]);

  // 上传文件
  const uploadFile = useCallback(async (file: File, type: EvidenceType): Promise<Evidence | null> => {
    setIsProcessing(true);
    setError(null);

    try {
      return new Promise((resolve, reject) => {
        const reader = new FileReader();
        
        reader.onload = () => {
          const base64 = (reader.result as string).split(',')[1];
          const evidence: Evidence = {
            id: `evidence_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
            type,
            name: file.name,
            data: base64,
            createdAt: new Date(),
          };
          setIsProcessing(false);
          resolve(evidence);
        };

        reader.onerror = () => {
          setError('文件读取失败');
          setIsProcessing(false);
          reject(new Error('File read failed'));
        };

        reader.readAsDataURL(file);
      });
    } catch (e) {
      setError('文件上传失败');
      setIsProcessing(false);
      return null;
    }
  }, []);

  // 上传图片（便捷方法）
  const uploadImage = useCallback(async (file: File): Promise<Evidence | null> => {
    if (!file.type.startsWith('image/')) {
      setError('请选择图片文件');
      return null;
    }
    return uploadFile(file, 'image');
  }, [uploadFile]);

  // 证据转文字（录音转文字、图片OCR）
  const transcribeEvidence = useCallback(async (evidence: Evidence): Promise<string | null> => {
    setIsProcessing(true);
    setError(null);

    try {
      // 使用法律助手服务进行转写
      const service = createLegalAssistant('labor'); // 任意类型，只用转写功能
      const result = await service.transcribeEvidence(evidence);
      
      setIsProcessing(false);
      
      if (result.success && result.text) {
        return result.text;
      } else {
        setError(result.error || '转写失败');
        return null;
      }
    } catch (e) {
      setError('转写请求失败');
      setIsProcessing(false);
      return null;
    }
  }, []);

  return {
    // 录音
    isRecording: audioRecorder.state.isRecording,
    recordingTime: audioRecorder.state.recordingTime,
    startRecording,
    stopRecording,
    cancelRecording,
    
    // 文件
    uploadFile,
    uploadImage,
    
    // 转写
    transcribeEvidence,
    
    // 状态
    isProcessing,
    error: error || audioRecorder.state.error,
  };
};

export default useEvidenceCollector;
