/**
 * 劳动仲裁助手组件
 * 帮助劳动者分析权益受损情况，计算赔偿金额
 */

import React, { useState, useMemo } from 'react';
import type { LaborCase, LaborViolationType, LaborAnalysis, Evidence } from '../types';
import { createLegalAssistant } from '../services/legalAssistantService';
import { useEvidenceCollector } from '../hooks/useEvidenceCollector';
import { triggerQuotaRefresh } from '../../../contexts/QuotaContext';
import EvidencePanel from './EvidencePanel';
import AnalysisResult from './AnalysisResult';
import './LaborAssistant.css';

interface LaborAssistantProps {
  caseData: LaborCase;
  onUpdate: (updates: Partial<LaborCase>) => void;
}

const violationOptions: { value: LaborViolationType; label: string; description: string }[] = [
  { value: 'no_contract', label: '未签劳动合同', description: '入职超过1个月未签书面合同' },
  { value: 'no_social_insurance', label: '未缴社保', description: '公司未依法缴纳五险一金' },
  { value: 'overtime_unpaid', label: '加班费未付', description: '加班未支付相应的加班工资' },
  { value: 'illegal_dismissal', label: '违法解雇', description: '被违法辞退或强迫离职' },
  { value: 'salary_arrears', label: '拖欠工资', description: '工资被拖欠或克扣' },
  { value: 'forced_resignation', label: '强迫离职', description: '被迫"主动"离职' },
  { value: 'work_injury', label: '工伤未赔', description: '工伤后未获得应有赔偿' },
  { value: 'maternity_violation', label: '侵犯孕产假', description: '孕期/产假期间被违法对待' },
];

const LaborAssistant: React.FC<LaborAssistantProps> = ({ caseData, onUpdate }) => {
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [activeTab, setActiveTab] = useState<'info' | 'violations' | 'evidence' | 'result'>('info');

  const evidenceCollector = useEvidenceCollector();

  // 计算工作年限
  const calculatedYears = useMemo(() => {
    if (!caseData.entryDate) return 0;
    const entry = new Date(caseData.entryDate);
    const leave = caseData.leaveDate ? new Date(caseData.leaveDate) : new Date();
    const years = (leave.getTime() - entry.getTime()) / (1000 * 60 * 60 * 24 * 365);
    return Math.round(years * 10) / 10;
  }, [caseData.entryDate, caseData.leaveDate]);

  // 简单预估赔偿（正式分析前的参考）
  const quickEstimate = useMemo(() => {
    const salary = caseData.monthlySalary || 0;
    const years = calculatedYears || caseData.workingYears || 0;
    let total = 0;
    
    // 未签合同：最多11个月双倍工资
    if (caseData.violations?.includes('no_contract')) {
      total += Math.min(11, years * 12) * salary;
    }
    
    // 违法解雇：2N
    if (caseData.violations?.includes('illegal_dismissal')) {
      total += Math.ceil(years) * salary * 2;
    }
    
    // 拖欠工资：假设3个月
    if (caseData.violations?.includes('salary_arrears')) {
      total += salary * 3;
    }
    
    return total;
  }, [caseData, calculatedYears]);

  // 切换违法项
  const toggleViolation = (violation: LaborViolationType) => {
    const current = caseData.violations || [];
    const updated = current.includes(violation)
      ? current.filter(v => v !== violation)
      : [...current, violation];
    onUpdate({ violations: updated });
  };

  // 执行AI分析
  const handleAnalyze = async () => {
    setIsAnalyzing(true);
    try {
      const dataWithYears = {
        ...caseData,
        workingYears: calculatedYears || caseData.workingYears,
      };
      
      const service = createLegalAssistant('labor');
      const result = await service.analyze(dataWithYears);
      
      if (result.success && result.analysis) {
        onUpdate({ 
          analysis: result.analysis as LaborAnalysis,
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

  // 生成仲裁申请书
  const handleGenerateApplication = async () => {
    const service = createLegalAssistant('labor');
    const result = await service.generateDocument(caseData, 'application');
    if (result.success && result.document) {
      // 创建下载文件
      const blob = new Blob([result.document], { type: 'text/plain;charset=utf-8' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `仲裁申请书_${caseData.companyName || '未知公司'}_${new Date().toLocaleDateString()}.txt`;
      a.click();
      URL.revokeObjectURL(url);
    }
  };

  return (
    <div className="labor-assistant">
      <div className="assistant-header">
        <h2>⚖️ 劳动仲裁助手</h2>
        <p className="assistant-desc">计算赔偿金额、整理证据、生成仲裁申请书</p>
      </div>

      {/* 快速预估卡片 */}
      {quickEstimate > 0 && (
        <div className="quick-estimate-card">
          <span className="estimate-label">初步预估赔偿金额</span>
          <span className="estimate-value">¥{quickEstimate.toLocaleString()}</span>
          <span className="estimate-tip">（仅供参考，详细分析请点击下方按钮）</span>
        </div>
      )}

      {/* 标签页导航 */}
      <div className="tab-nav">
        <button 
          className={`tab-btn ${activeTab === 'info' ? 'active' : ''}`}
          onClick={() => setActiveTab('info')}
        >
          🏢 工作信息
        </button>
        <button 
          className={`tab-btn ${activeTab === 'violations' ? 'active' : ''}`}
          onClick={() => setActiveTab('violations')}
        >
          ⚠️ 违法情况
          {caseData.violations?.length > 0 && (
            <span className="badge danger">{caseData.violations.length}</span>
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

      {/* 工作信息 */}
      {activeTab === 'info' && (
        <div className="tab-content">
          <div className="form-section">
            <h3>公司及职位信息</h3>
            
            <div className="form-row">
              <div className="form-group">
                <label>公司名称 <span className="required">*</span></label>
                <input
                  type="text"
                  placeholder="公司全称"
                  value={caseData.companyName || ''}
                  onChange={(e) => onUpdate({ companyName: e.target.value })}
                />
              </div>
              <div className="form-group">
                <label>您的职位</label>
                <input
                  type="text"
                  placeholder="例如：产品经理"
                  value={caseData.position || ''}
                  onChange={(e) => onUpdate({ position: e.target.value })}
                />
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>入职日期 <span className="required">*</span></label>
                <input
                  type="date"
                  value={caseData.entryDate || ''}
                  onChange={(e) => onUpdate({ entryDate: e.target.value })}
                />
              </div>
              <div className="form-group">
                <label>离职日期（如已离职）</label>
                <input
                  type="date"
                  value={caseData.leaveDate || ''}
                  onChange={(e) => onUpdate({ leaveDate: e.target.value })}
                />
              </div>
            </div>

            {calculatedYears > 0 && (
              <div className="info-tip">
                工作年限：约 <strong>{calculatedYears}</strong> 年
              </div>
            )}

            <div className="form-row">
              <div className="form-group">
                <label>月薪（税前） <span className="required">*</span></label>
                <div className="input-with-unit">
                  <input
                    type="number"
                    placeholder="0"
                    value={caseData.monthlySalary || ''}
                    onChange={(e) => onUpdate({ monthlySalary: Number(e.target.value) })}
                  />
                  <span className="unit">元/月</span>
                </div>
              </div>
            </div>

            <div className="form-row checkbox-row">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={caseData.hasContract}
                  onChange={(e) => onUpdate({ hasContract: e.target.checked })}
                />
                已签订书面劳动合同
              </label>
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={caseData.hasSocialInsurance}
                  onChange={(e) => onUpdate({ hasSocialInsurance: e.target.checked })}
                />
                公司为我缴纳了社保
              </label>
            </div>
          </div>
        </div>
      )}

      {/* 违法情况 */}
      {activeTab === 'violations' && (
        <div className="tab-content">
          <div className="form-section">
            <h3>请勾选公司存在的违法行为</h3>
            <p className="section-desc">勾选越准确，分析结果越精确</p>
            
            <div className="violation-grid">
              {violationOptions.map(option => (
                <div 
                  key={option.value}
                  className={`violation-card ${caseData.violations?.includes(option.value) ? 'selected' : ''}`}
                  onClick={() => toggleViolation(option.value)}
                >
                  <div className="violation-checkbox">
                    <input
                      type="checkbox"
                      checked={caseData.violations?.includes(option.value) || false}
                      onChange={() => {}}
                    />
                  </div>
                  <div className="violation-content">
                    <span className="violation-label">{option.label}</span>
                    <span className="violation-desc">{option.description}</span>
                  </div>
                </div>
              ))}
            </div>

            {/* 其他说明 */}
            <div className="form-group">
              <label>其他情况说明（可选）</label>
              <textarea
                placeholder="请补充其他需要说明的情况，例如：公司口头承诺的奖金未发放、被威胁不签离职协议不给离职证明等..."
                value={caseData.description || ''}
                onChange={(e) => onUpdate({ description: e.target.value })}
                rows={4}
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
              '劳动合同（如有）',
              '工资条/银行流水',
              '考勤记录',
              '加班证据（打卡、邮件、聊天记录）',
              '工作群/钉钉聊天记录',
              '解雇通知/离职证明',
              '与HR/领导的对话录音',
              '社保缴纳记录',
              '工伤认定材料（如有）',
            ]}
            recordingTips="💡 录音提示：与HR或领导沟通时，可以录音取证。录音中尽量让对方明确说出公司名称、您的职位、拖欠工资金额等关键信息。"
          />
        </div>
      )}

      {/* 分析结果 */}
      {activeTab === 'result' && caseData.analysis && (
        <div className="tab-content">
          <AnalysisResult
            type="labor"
            analysis={caseData.analysis}
          />
        </div>
      )}

      {/* 底部操作栏 */}
      <div className="action-bar">
        <button
          className="btn-analyze"
          onClick={handleAnalyze}
          disabled={isAnalyzing || !caseData.companyName || !caseData.monthlySalary}
        >
          {isAnalyzing ? '⏳ 分析中...' : '🤖 AI 智能分析'}
        </button>
        
        {caseData.analysis && (
          <div className="doc-actions">
            <button 
              className="btn-doc primary"
              onClick={handleGenerateApplication}
            >
              📄 生成仲裁申请书
            </button>
            <button 
              className="btn-doc"
              onClick={() => {
                const service = createLegalAssistant('labor');
                service.generateDocument(caseData, 'letter');
              }}
            >
              📝 生成律师函
            </button>
          </div>
        )}
      </div>

      {/* 免责声明 */}
      <div className="disclaimer">
        ⚠️ 本工具提供的分析结果仅供参考，不构成法律意见。建议咨询专业律师获得正式法律帮助。
      </div>
    </div>
  );
};

export default LaborAssistant;
