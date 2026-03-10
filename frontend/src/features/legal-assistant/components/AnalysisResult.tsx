/**
 * 分析结果展示组件
 * 展示AI分析的法律建议和计算结果
 */

import React from 'react';
import type { DivorceAnalysis, LaborAnalysis, RentalAnalysis, CaseType } from '../types';
import './AnalysisResult.css';

interface AnalysisResultProps {
  type: CaseType;
  analysis: DivorceAnalysis | LaborAnalysis | RentalAnalysis;
}

const AnalysisResult: React.FC<AnalysisResultProps> = ({ type, analysis }) => {
  // 离婚财产分析结果
  const renderDivorceResult = (data: DivorceAnalysis) => (
    <div className="analysis-result divorce-result">
      {/* 财产概览 */}
      <div className="result-section">
        <h3>💰 财产概览</h3>
        <div className="financial-summary">
          <div className="summary-row">
            <span className="label">总资产</span>
            <span className="value positive">¥{data.totalAssets?.toLocaleString() || 0}</span>
          </div>
          <div className="summary-row">
            <span className="label">总负债</span>
            <span className="value negative">¥{data.totalDebts?.toLocaleString() || 0}</span>
          </div>
          <div className="summary-row">
            <span className="label">共同财产</span>
            <span className="value">¥{data.jointAssets?.toLocaleString() || 0}</span>
          </div>
          <div className="summary-row">
            <span className="label">个人财产</span>
            <span className="value">¥{data.personalAssets?.toLocaleString() || 0}</span>
          </div>
        </div>
      </div>

      {/* 预估分割 */}
      <div className="result-section highlight-section">
        <h3>⚖️ 预估分割方案</h3>
        <div className="split-comparison">
          <div className="split-side self">
            <span className="split-label">您可获得</span>
            <span className="split-value">¥{data.estimatedSplit?.self?.toLocaleString() || 0}</span>
          </div>
          <div className="split-divider">VS</div>
          <div className="split-side spouse">
            <span className="split-label">对方获得</span>
            <span className="split-value">¥{data.estimatedSplit?.spouse?.toLocaleString() || 0}</span>
          </div>
        </div>
        {data.childSupportEstimate && (
          <div className="child-support">
            <span className="label">预估抚养费</span>
            <span className="value">¥{data.childSupportEstimate.toLocaleString()}/月</span>
          </div>
        )}
      </div>

      {/* 法律依据 */}
      {data.legalReferences && data.legalReferences.length > 0 && (
        <div className="result-section">
          <h3>📚 法律依据</h3>
          <ul className="legal-references">
            {data.legalReferences.map((ref, index) => (
              <li key={index}>{ref}</li>
            ))}
          </ul>
        </div>
      )}

      {/* 风险提示 */}
      {data.riskPoints && data.riskPoints.length > 0 && (
        <div className="result-section warning-section">
          <h3>⚠️ 风险提示</h3>
          <ul className="risk-points">
            {data.riskPoints.map((risk, index) => (
              <li key={index}>{risk}</li>
            ))}
          </ul>
        </div>
      )}

      {/* 建议 */}
      {data.recommendations && data.recommendations.length > 0 && (
        <div className="result-section">
          <h3>💡 专业建议</h3>
          <ul className="recommendations">
            {data.recommendations.map((rec, index) => (
              <li key={index}>{rec}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );

  // 劳动仲裁分析结果
  const renderLaborResult = (data: LaborAnalysis) => (
    <div className="analysis-result labor-result">
      {/* 赔偿金额汇总 */}
      <div className="result-section highlight-section">
        <h3>💰 预估赔偿金额</h3>
        <div className="total-compensation">
          <span className="total-label">总计可主张</span>
          <span className="total-value">¥{data.totalCompensation?.toLocaleString() || 0}</span>
        </div>
      </div>

      {/* 违法行为明细 */}
      {data.violations && data.violations.length > 0 && (
        <div className="result-section">
          <h3>⚖️ 违法行为赔偿明细</h3>
          <div className="violation-breakdown">
            {data.violations.map((v, index) => (
              <div key={index} className="violation-item">
                <div className="violation-header">
                  <span className="violation-type">{v.description}</span>
                  <span className="violation-amount">¥{v.compensation?.toLocaleString() || 0}</span>
                </div>
                <div className="violation-basis">
                  <span className="basis-label">法律依据：</span>
                  <span className="basis-text">{v.legalBasis}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* 证据清单 */}
      {data.evidenceChecklist && data.evidenceChecklist.length > 0 && (
        <div className="result-section">
          <h3>📋 证据收集清单</h3>
          <div className="evidence-checklist">
            {data.evidenceChecklist.map((item, index) => (
              <div key={index} className={`checklist-item ${item.collected ? 'collected' : ''} ${item.required ? 'required' : ''}`}>
                <span className="check-icon">
                  {item.collected ? '✅' : item.required ? '❗' : '⬜'}
                </span>
                <span className="check-label">{item.item}</span>
                {item.required && !item.collected && (
                  <span className="required-badge">必需</span>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* 维权时间线 */}
      {data.timeline && data.timeline.length > 0 && (
        <div className="result-section">
          <h3>📅 维权时间线</h3>
          <div className="timeline">
            {data.timeline.map((step, index) => (
              <div key={index} className="timeline-item">
                <div className="timeline-marker">{index + 1}</div>
                <div className="timeline-content">
                  <span className="timeline-step">{step.step}</span>
                  {step.deadline && (
                    <span className="timeline-deadline">期限：{step.deadline}</span>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* 仲裁申请书草稿 */}
      {data.applicationDraft && (
        <div className="result-section">
          <h3>📝 仲裁申请书要点</h3>
          <div className="application-draft">
            <pre>{data.applicationDraft}</pre>
          </div>
        </div>
      )}

      {/* 建议 */}
      {data.recommendations && data.recommendations.length > 0 && (
        <div className="result-section">
          <h3>💡 专业建议</h3>
          <ul className="recommendations">
            {data.recommendations.map((rec, index) => (
              <li key={index}>{rec}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );

  // 租房纠纷分析结果
  const renderRentalResult = (data: RentalAnalysis) => (
    <div className="analysis-result rental-result">
      {/* 合同漏洞 */}
      {data.contractLoopholes && data.contractLoopholes.length > 0 && (
        <div className="result-section warning-section">
          <h3>🔍 合同问题分析</h3>
          <ul className="contract-loopholes">
            {data.contractLoopholes.map((loophole, index) => (
              <li key={index}>{loophole}</li>
            ))}
          </ul>
        </div>
      )}

      {/* 纠纷评估 */}
      {data.disputeAssessment && data.disputeAssessment.length > 0 && (
        <div className="result-section">
          <h3>⚖️ 纠纷分析与胜诉概率</h3>
          <div className="dispute-assessment">
            {data.disputeAssessment.map((dispute, index) => (
              <div key={index} className="dispute-item">
                <div className="dispute-header">
                  <span className="dispute-type">{dispute.description}</span>
                </div>
                <div className="dispute-metrics">
                  <div className="metric">
                    <span className="metric-label">胜诉概率</span>
                    <div className="probability-bar">
                      <div 
                        className={`probability-fill ${dispute.winProbability >= 70 ? 'high' : dispute.winProbability >= 40 ? 'medium' : 'low'}`}
                        style={{ width: `${dispute.winProbability}%` }}
                      />
                    </div>
                    <span className="metric-value">{dispute.winProbability}%</span>
                  </div>
                  <div className="metric">
                    <span className="metric-label">可主张金额</span>
                    <span className="metric-value highlight">¥{dispute.compensation?.toLocaleString() || 0}</span>
                  </div>
                </div>
                <div className="dispute-basis">
                  <span className="basis-label">法律依据：</span>
                  <span className="basis-text">{dispute.legalBasis}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* 证据清单 */}
      {data.evidenceChecklist && data.evidenceChecklist.length > 0 && (
        <div className="result-section">
          <h3>📋 证据收集清单</h3>
          <div className="evidence-checklist">
            {data.evidenceChecklist.map((item, index) => (
              <div key={index} className={`checklist-item ${item.collected ? 'collected' : ''} importance-${item.importance}`}>
                <span className="check-icon">
                  {item.collected ? '✅' : item.importance === 'critical' ? '❗' : '⬜'}
                </span>
                <span className="check-label">{item.item}</span>
                <span className={`importance-badge ${item.importance}`}>
                  {item.importance === 'critical' ? '关键' : item.importance === 'important' ? '重要' : '有帮助'}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* 维权步骤 */}
      {data.actionPlan && data.actionPlan.length > 0 && (
        <div className="result-section">
          <h3>🛡️ 维权行动计划</h3>
          <div className="action-plan">
            {data.actionPlan.map((step, index) => (
              <div key={index} className="action-step">
                <div className="step-number">{index + 1}</div>
                <div className="step-content">{step}</div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* 文书草稿 */}
      {data.letterDraft && (
        <div className="result-section">
          <h3>📝 催告函/投诉信草稿</h3>
          <div className="letter-draft">
            <pre>{data.letterDraft}</pre>
          </div>
        </div>
      )}

      {/* 建议 */}
      {data.recommendations && data.recommendations.length > 0 && (
        <div className="result-section">
          <h3>💡 专业建议</h3>
          <ul className="recommendations">
            {data.recommendations.map((rec, index) => (
              <li key={index}>{rec}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );

  return (
    <div className="analysis-result-container">
      <div className="result-header">
        <h2>📊 AI 分析结果</h2>
        <p className="result-timestamp">
          分析时间：{new Date().toLocaleString('zh-CN')}
        </p>
      </div>

      {type === 'divorce' && renderDivorceResult(analysis as DivorceAnalysis)}
      {type === 'labor' && renderLaborResult(analysis as LaborAnalysis)}
      {type === 'rental' && renderRentalResult(analysis as RentalAnalysis)}

      <div className="result-footer">
        <p className="disclaimer">
          ⚠️ 以上分析结果仅供参考，不构成正式法律意见。具体情况请咨询专业律师。
        </p>
      </div>
    </div>
  );
};

export default AnalysisResult;
