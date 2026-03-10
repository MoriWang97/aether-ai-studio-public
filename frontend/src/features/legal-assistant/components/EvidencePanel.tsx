/**
 * 证据收集面板组件
 * 支持录音、拍照、上传文件
 */

import React, { useRef, useState } from 'react';
import type { Evidence, EvidenceType } from '../types';
import type { UseEvidenceCollectorReturn } from '../hooks/useEvidenceCollector';
import './EvidencePanel.css';

interface EvidencePanelProps {
  evidences: Evidence[];
  collector: UseEvidenceCollectorReturn;
  onAddEvidence: (evidence: Evidence) => void;
  onRemoveEvidence: (evidenceId: string) => void;
  suggestedEvidences?: string[];
  recordingTips?: string;
}

const evidenceTypeLabels: Record<EvidenceType, string> = {
  audio: '🎤 录音',
  image: '📷 图片',
  document: '📄 文档',
  contract: '📋 合同',
  chat: '💬 聊天记录',
  bank: '🏦 银行流水',
  property: '🏠 房产证明',
  other: '📎 其他',
};

const EvidencePanel: React.FC<EvidencePanelProps> = ({
  evidences,
  collector,
  onAddEvidence,
  onRemoveEvidence,
  suggestedEvidences = [],
  recordingTips,
}) => {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isTranscribing, setIsTranscribing] = useState<string | null>(null);

  // 处理录音
  const handleStartRecording = async () => {
    await collector.startRecording();
  };

  const handleStopRecording = async () => {
    const evidence = await collector.stopRecording();
    if (evidence) {
      onAddEvidence(evidence);
    }
  };

  // 处理文件上传
  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files?.length) return;

    for (let i = 0; i < files.length; i++) {
      const file = files[i];
      let type: EvidenceType = 'other';
      
      if (file.type.startsWith('image/')) {
        type = 'image';
      } else if (file.type.startsWith('audio/')) {
        type = 'audio';
      } else if (file.type.includes('pdf') || file.type.includes('document')) {
        type = 'document';
      }

      const evidence = await collector.uploadFile(file, type);
      if (evidence) {
        onAddEvidence(evidence);
      }
    }

    // 清空input以允许重复选择同一文件
    e.target.value = '';
  };

  // 转写录音
  const handleTranscribe = async (evidence: Evidence) => {
    if (evidence.type !== 'audio' || !evidence.data) return;
    
    setIsTranscribing(evidence.id);
    const text = await collector.transcribeEvidence(evidence);
    setIsTranscribing(null);
    
    if (text) {
      // 需要通过父组件更新证据
      const updatedEvidence = { ...evidence, transcript: text };
      onRemoveEvidence(evidence.id);
      onAddEvidence(updatedEvidence);
    }
  };

  // 格式化时间
  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  return (
    <div className="evidence-panel">
      {/* 录音提示 */}
      {recordingTips && (
        <div className="recording-tips">
          {recordingTips}
        </div>
      )}

      {/* 操作按钮区 */}
      <div className="evidence-actions">
        {/* 录音按钮 */}
        <div className="action-group">
          {collector.isRecording ? (
            <div className="recording-controls">
              <span className="recording-indicator">
                🔴 录音中 {formatTime(collector.recordingTime)}
              </span>
              <button 
                className="btn-stop-recording"
                onClick={handleStopRecording}
              >
                ⏹️ 停止录音
              </button>
              <button 
                className="btn-cancel-recording"
                onClick={collector.cancelRecording}
              >
                ❌ 取消
              </button>
            </div>
          ) : (
            <button 
              className="btn-action btn-record"
              onClick={handleStartRecording}
              disabled={collector.isProcessing}
            >
              🎤 开始录音
            </button>
          )}
        </div>

        {/* 上传按钮 */}
        <button 
          className="btn-action btn-upload"
          onClick={() => fileInputRef.current?.click()}
          disabled={collector.isRecording || collector.isProcessing}
        >
          📤 上传文件/图片
        </button>
        <input
          ref={fileInputRef}
          type="file"
          multiple
          accept="image/*,audio/*,.pdf,.doc,.docx"
          onChange={handleFileSelect}
          style={{ display: 'none' }}
        />
      </div>

      {/* 错误提示 */}
      {collector.error && (
        <div className="error-message">
          ❌ {collector.error}
        </div>
      )}

      {/* 已收集证据列表 */}
      <div className="evidence-list">
        <h4>已收集证据 ({evidences.length})</h4>
        
        {evidences.length === 0 ? (
          <div className="empty-evidence">
            <p>暂无证据，请通过上方按钮添加</p>
          </div>
        ) : (
          <div className="evidence-items">
            {evidences.map(evidence => (
              <div key={evidence.id} className="evidence-item">
                <div className="evidence-icon">
                  {evidence.type === 'audio' && '🎤'}
                  {evidence.type === 'image' && '📷'}
                  {evidence.type === 'document' && '📄'}
                  {evidence.type === 'contract' && '📋'}
                  {evidence.type === 'chat' && '💬'}
                  {evidence.type === 'bank' && '🏦'}
                  {evidence.type === 'property' && '🏠'}
                  {evidence.type === 'other' && '📎'}
                </div>
                <div className="evidence-info">
                  <span className="evidence-name">{evidence.name}</span>
                  <span className="evidence-type">
                    {evidenceTypeLabels[evidence.type]}
                  </span>
                  {evidence.transcript && (
                    <div className="evidence-transcript">
                      <strong>转写内容：</strong>
                      <p>{evidence.transcript}</p>
                    </div>
                  )}
                </div>
                <div className="evidence-actions-inline">
                  {/* 录音转文字按钮 */}
                  {evidence.type === 'audio' && !evidence.transcript && (
                    <button
                      className="btn-transcribe"
                      onClick={() => handleTranscribe(evidence)}
                      disabled={isTranscribing === evidence.id}
                    >
                      {isTranscribing === evidence.id ? '⏳ 转写中...' : '📝 转文字'}
                    </button>
                  )}
                  {/* 预览按钮（图片） */}
                  {evidence.type === 'image' && evidence.data && (
                    <button
                      className="btn-preview"
                      onClick={() => {
                        const img = new Image();
                        img.src = `data:image/jpeg;base64,${evidence.data}`;
                        const win = window.open('');
                        win?.document.write(img.outerHTML);
                      }}
                    >
                      👁️ 预览
                    </button>
                  )}
                  {/* 删除按钮 */}
                  <button
                    className="btn-remove"
                    onClick={() => onRemoveEvidence(evidence.id)}
                  >
                    🗑️
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* 建议收集的证据 */}
      {suggestedEvidences.length > 0 && (
        <div className="suggested-evidence">
          <h4>📋 建议收集的证据</h4>
          <ul>
            {suggestedEvidences.map((item, index) => {
              const collected = evidences.some(e => 
                e.name.includes(item) || item.includes(e.name.split('_')[0])
              );
              return (
                <li key={index} className={collected ? 'collected' : ''}>
                  {collected ? '✅' : '⬜'} {item}
                </li>
              );
            })}
          </ul>
        </div>
      )}
    </div>
  );
};

export default EvidencePanel;
