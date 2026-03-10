/**
 * 租房纠纷助手组件
 * 帮助租客分析租房纠纷，提供维权指导
 */

import React, { useState } from 'react';
import type { RentalCase, RentalDisputeType, RentalAnalysis, Evidence } from '../types';
import { createLegalAssistant } from '../services/legalAssistantService';
import { useEvidenceCollector } from '../hooks/useEvidenceCollector';
import { triggerQuotaRefresh } from '../../../contexts/QuotaContext';
import EvidencePanel from './EvidencePanel';
import AnalysisResult from './AnalysisResult';
import './RentalAssistant.css';

interface RentalAssistantProps {
  caseData: RentalCase;
  onUpdate: (updates: Partial<RentalCase>) => void;
}

const disputeOptions: { value: RentalDisputeType; label: string; emoji: string; description: string }[] = [
  { value: 'deposit_refund', label: '押金不退', emoji: '💰', description: '房东拒绝退还押金或无理由扣押金' },
  { value: 'illegal_eviction', label: '违法驱赶', emoji: '🚪', description: '被房东或二房东强行赶出' },
  { value: 'repair_dispute', label: '维修纠纷', emoji: '🔧', description: '房屋设施损坏，维修责任不清' },
  { value: 'sublease_issue', label: '转租问题', emoji: '🔄', description: '二房东跑路、转租纠纷等' },
  { value: 'rent_increase', label: '违规涨租', emoji: '📈', description: '租期内擅自涨租' },
  { value: 'contract_breach', label: '合同违约', emoji: '📋', description: '房东违反合同约定' },
  { value: 'living_condition', label: '居住条件差', emoji: '🏚️', description: '甲醛超标、隔断房、消防隐患等' },
];

const RentalAssistant: React.FC<RentalAssistantProps> = ({ caseData, onUpdate }) => {
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [activeTab, setActiveTab] = useState<'info' | 'dispute' | 'evidence' | 'result'>('info');

  const evidenceCollector = useEvidenceCollector();

  // 切换纠纷类型
  const toggleDispute = (dispute: RentalDisputeType) => {
    const current = caseData.disputeTypes || [];
    const updated = current.includes(dispute)
      ? current.filter(d => d !== dispute)
      : [...current, dispute];
    onUpdate({ disputeTypes: updated });
  };

  // 执行AI分析
  const handleAnalyze = async () => {
    setIsAnalyzing(true);
    try {
      const service = createLegalAssistant('rental');
      const result = await service.analyze(caseData);
      
      if (result.success && result.analysis) {
        onUpdate({ 
          analysis: result.analysis as RentalAnalysis,
          status: 'completed' 
        });
        setActiveTab('result');
        // 刷新使用额度显示
        triggerQuotaRefresh();
      }
    } finally {
      setIsAnalyzing(false);
    }
  };

  // 生成投诉信
  const handleGenerateComplaint = async () => {
    const service = createLegalAssistant('rental');
    const result = await service.generateDocument(caseData, 'complaint');
    if (result.success && result.document) {
      const blob = new Blob([result.document], { type: 'text/plain;charset=utf-8' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `投诉信_${caseData.address || '租房纠纷'}_${new Date().toLocaleDateString()}.txt`;
      a.click();
      URL.revokeObjectURL(url);
    }
  };

  // 生成律师函
  const handleGenerateLetter = async () => {
    const service = createLegalAssistant('rental');
    const result = await service.generateDocument(caseData, 'letter');
    if (result.success && result.document) {
      const blob = new Blob([result.document], { type: 'text/plain;charset=utf-8' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `律师函_${caseData.landlordName || '房东'}_${new Date().toLocaleDateString()}.txt`;
      a.click();
      URL.revokeObjectURL(url);
    }
  };

  // 计算可主张金额
  const claimableAmount = () => {
    let amount = 0;
    if (caseData.disputeTypes?.includes('deposit_refund')) {
      amount += caseData.deposit || 0;
    }
    if (caseData.disputeTypes?.includes('rent_increase')) {
      amount += (caseData.monthlyRent || 0) * 3; // 假设3个月差价
    }
    return amount;
  };

  return (
    <div className="rental-assistant">
      <div className="assistant-header">
        <h2>🏠 租房纠纷助手</h2>
        <p className="assistant-desc">分析合同漏洞、评估胜诉概率、指导维权步骤</p>
      </div>

      {/* 快速统计 */}
      {(caseData.deposit || caseData.monthlyRent) && (
        <div className="quick-stats">
          <div className="stat-item">
            <span className="stat-label">押金</span>
            <span className="stat-value">¥{(caseData.deposit || 0).toLocaleString()}</span>
          </div>
          <div className="stat-item">
            <span className="stat-label">月租</span>
            <span className="stat-value">¥{(caseData.monthlyRent || 0).toLocaleString()}</span>
          </div>
          {claimableAmount() > 0 && (
            <div className="stat-item highlight">
              <span className="stat-label">预估可主张</span>
              <span className="stat-value">¥{claimableAmount().toLocaleString()}</span>
            </div>
          )}
        </div>
      )}

      {/* 标签页导航 */}
      <div className="tab-nav">
        <button 
          className={`tab-btn ${activeTab === 'info' ? 'active' : ''}`}
          onClick={() => setActiveTab('info')}
        >
          📍 租房信息
        </button>
        <button 
          className={`tab-btn ${activeTab === 'dispute' ? 'active' : ''}`}
          onClick={() => setActiveTab('dispute')}
        >
          ⚠️ 纠纷类型
          {caseData.disputeTypes?.length > 0 && (
            <span className="badge danger">{caseData.disputeTypes.length}</span>
          )}
        </button>
        <button 
          className={`tab-btn ${activeTab === 'evidence' ? 'active' : ''}`}
          onClick={() => setActiveTab('evidence')}
        >
          📂 证据材料
          {caseData.evidences?.length > 0 && (
            <span className="badge">{caseData.evidences.length}</span>
          )}
        </button>
        <button 
          className={`tab-btn ${activeTab === 'result' ? 'active' : ''}`}
          onClick={() => setActiveTab('result')}
          disabled={!caseData.analysis}
        >
          📊 分析结果
        </button>
      </div>

      {/* 租房信息 */}
      {activeTab === 'info' && (
        <div className="tab-content">
          <div className="form-section">
            <h3>房屋及房东信息</h3>
            
            <div className="form-group">
              <label>房屋地址 <span className="required">*</span></label>
              <input
                type="text"
                placeholder="例如：北京市朝阳区XX小区XX号楼XX室"
                value={caseData.address || ''}
                onChange={(e) => onUpdate({ address: e.target.value })}
              />
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>房东姓名</label>
                <input
                  type="text"
                  placeholder="房东或二房东姓名"
                  value={caseData.landlordName || ''}
                  onChange={(e) => onUpdate({ landlordName: e.target.value })}
                />
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>月租金 <span className="required">*</span></label>
                <div className="input-with-unit">
                  <input
                    type="number"
                    placeholder="0"
                    value={caseData.monthlyRent || ''}
                    onChange={(e) => onUpdate({ monthlyRent: Number(e.target.value) })}
                  />
                  <span className="unit">元/月</span>
                </div>
              </div>
              <div className="form-group">
                <label>押金</label>
                <div className="input-with-unit">
                  <input
                    type="number"
                    placeholder="0"
                    value={caseData.deposit || ''}
                    onChange={(e) => onUpdate({ deposit: Number(e.target.value) })}
                  />
                  <span className="unit">元</span>
                </div>
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>合同开始日期</label>
                <input
                  type="date"
                  value={caseData.contractStartDate || ''}
                  onChange={(e) => onUpdate({ contractStartDate: e.target.value })}
                />
              </div>
              <div className="form-group">
                <label>合同结束日期</label>
                <input
                  type="date"
                  value={caseData.contractEndDate || ''}
                  onChange={(e) => onUpdate({ contractEndDate: e.target.value })}
                />
              </div>
            </div>

            <div className="form-row checkbox-row">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={caseData.hasWrittenContract}
                  onChange={(e) => onUpdate({ hasWrittenContract: e.target.checked })}
                />
                有书面租房合同
              </label>
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={caseData.isRegistered}
                  onChange={(e) => onUpdate({ isRegistered: e.target.checked })}
                />
                租房合同已备案
              </label>
            </div>

            {!caseData.hasWrittenContract && (
              <div className="warning-tip">
                ⚠️ 没有书面合同会增加维权难度，但并不代表无法维权。口头约定也受法律保护，关键是收集其他证据。
              </div>
            )}
          </div>
        </div>
      )}

      {/* 纠纷类型 */}
      {activeTab === 'dispute' && (
        <div className="tab-content">
          <div className="form-section">
            <h3>请选择您遇到的纠纷类型</h3>
            <p className="section-desc">可多选，选择越准确分析越精确</p>
            
            <div className="dispute-grid">
              {disputeOptions.map(option => (
                <div 
                  key={option.value}
                  className={`dispute-card ${caseData.disputeTypes?.includes(option.value) ? 'selected' : ''}`}
                  onClick={() => toggleDispute(option.value)}
                >
                  <div className="dispute-emoji">{option.emoji}</div>
                  <div className="dispute-content">
                    <span className="dispute-label">{option.label}</span>
                    <span className="dispute-desc">{option.description}</span>
                  </div>
                  <div className="dispute-checkbox">
                    <input
                      type="checkbox"
                      checked={caseData.disputeTypes?.includes(option.value) || false}
                      onChange={() => {}}
                    />
                  </div>
                </div>
              ))}
            </div>

            {/* 详细描述 */}
            <div className="form-group">
              <label>详细描述纠纷经过</label>
              <textarea
                placeholder="请详细描述发生了什么，例如：合同到期后房东以各种理由拒绝退还押金3000元，包括说墙面有划痕（实际是入住前就有的）..."
                value={caseData.description || ''}
                onChange={(e) => onUpdate({ description: e.target.value })}
                rows={5}
              />
            </div>
          </div>
        </div>
      )}

      {/* 证据材料 */}
      {activeTab === 'evidence' && (
        <div className="tab-content">
          <EvidencePanel
            evidences={caseData.evidences}
            collector={evidenceCollector}
            onAddEvidence={(evidence: Evidence) => {
              onUpdate({
                evidences: [...caseData.evidences, evidence],
              });
            }}
            onRemoveEvidence={(evidenceId: string) => {
              onUpdate({
                evidences: caseData.evidences.filter(e => e.id !== evidenceId),
              });
            }}
            suggestedEvidences={[
              '租房合同（如有）',
              '押金收据/转账记录',
              '租金支付记录',
              '房屋照片（入住时/现在）',
              '与房东的聊天记录',
              '与房东的通话录音',
              '物业费/水电费缴纳记录',
              '房屋问题照片（损坏、甲醛检测等）',
              '中介合同/收据',
            ]}
            recordingTips="💡 录音提示：与房东沟通退押金时，可以录音。尽量让房东说出拒绝退押金的理由，以及押金金额等关键信息。"
          />
        </div>
      )}

      {/* 分析结果 */}
      {activeTab === 'result' && caseData.analysis && (
        <div className="tab-content">
          <AnalysisResult
            type="rental"
            analysis={caseData.analysis}
          />
        </div>
      )}

      {/* 底部操作栏 */}
      <div className="action-bar">
        <button
          className="btn-analyze"
          onClick={handleAnalyze}
          disabled={isAnalyzing || !caseData.address || caseData.disputeTypes?.length === 0}
        >
          {isAnalyzing ? '⏳ 分析中...' : '🤖 AI 智能分析'}
        </button>
        
        {caseData.analysis && (
          <div className="doc-actions">
            <button 
              className="btn-doc primary"
              onClick={handleGenerateComplaint}
            >
              📄 生成投诉信
            </button>
            <button 
              className="btn-doc"
              onClick={handleGenerateLetter}
            >
              📝 生成催告函
            </button>
          </div>
        )}
      </div>

      {/* 维权渠道提示 */}
      <div className="channel-tips">
        <h4>🛡️ 常用维权渠道</h4>
        <ul>
          <li>📞 <strong>12345</strong> - 市民服务热线</li>
          <li>📞 <strong>12315</strong> - 消费者投诉（适用于中介问题）</li>
          <li>📞 <strong>住建委投诉热线</strong> - 各地住建部门</li>
          <li>⚖️ <strong>人民调解委员会</strong> - 免费调解</li>
          <li>⚖️ <strong>小额诉讼</strong> - 争议金额5万以下可简易程序</li>
        </ul>
      </div>

      {/* 免责声明 */}
      <div className="disclaimer">
        ⚠️ 本工具提供的分析结果仅供参考，不构成法律意见。建议咨询专业律师获得正式法律帮助。
      </div>
    </div>
  );
};

export default RentalAssistant;
