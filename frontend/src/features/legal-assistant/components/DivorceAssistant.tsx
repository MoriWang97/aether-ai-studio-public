/**
 * 离婚财产分析助手组件
 * 帮助用户分析财产分割方案
 */

import React, { useState, useMemo } from 'react';
import type { DivorceCase, DivorceAsset, DivorceAnalysis, Evidence } from '../types';
import { createLegalAssistant } from '../services/legalAssistantService';
import { useEvidenceCollector } from '../hooks/useEvidenceCollector';
import { triggerQuotaRefresh } from '../../../contexts/QuotaContext';
import EvidencePanel from './EvidencePanel';
import AnalysisResult from './AnalysisResult';
import './DivorceAssistant.css';

interface DivorceAssistantProps {
  caseData: DivorceCase;
  onUpdate: (updates: Partial<DivorceCase>) => void;
}

const assetTypes = [
  { value: 'house', label: '房产' },
  { value: 'car', label: '车辆' },
  { value: 'savings', label: '存款' },
  { value: 'stock', label: '股票/基金' },
  { value: 'debt', label: '债务' },
  { value: 'other', label: '其他' },
];

const ownershipTypes = [
  { value: 'joint', label: '夫妻共同' },
  { value: 'personal_self', label: '我的婚前/个人' },
  { value: 'personal_spouse', label: '对方婚前/个人' },
  { value: 'unknown', label: '待确认' },
];

const DivorceAssistant: React.FC<DivorceAssistantProps> = ({ caseData, onUpdate }) => {
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [activeTab, setActiveTab] = useState<'info' | 'assets' | 'evidence' | 'result'>('info');
  const [newAsset, setNewAsset] = useState<Partial<DivorceAsset>>({
    type: 'house',
    ownership: 'joint',
  });

  const evidenceCollector = useEvidenceCollector();

  // 计算财产总计
  const assetSummary = useMemo(() => {
    const assets = caseData.assets || [];
    const totalAssets = assets
      .filter(a => a.type !== 'debt')
      .reduce((sum, a) => sum + (a.value || 0), 0);
    const totalDebts = assets
      .filter(a => a.type === 'debt')
      .reduce((sum, a) => sum + (a.value || 0), 0);
    return { totalAssets, totalDebts, netAssets: totalAssets - totalDebts };
  }, [caseData.assets]);

  // 添加财产
  const handleAddAsset = () => {
    if (!newAsset.name || !newAsset.value) return;
    
    const asset: DivorceAsset = {
      id: `asset_${Date.now()}`,
      name: newAsset.name,
      type: newAsset.type as DivorceAsset['type'],
      value: Number(newAsset.value),
      ownership: newAsset.ownership as DivorceAsset['ownership'],
      description: newAsset.description,
    };
    
    onUpdate({
      assets: [...(caseData.assets || []), asset],
    });
    
    setNewAsset({ type: 'house', ownership: 'joint' });
  };

  // 删除财产
  const handleRemoveAsset = (assetId: string) => {
    onUpdate({
      assets: caseData.assets.filter(a => a.id !== assetId),
    });
  };

  // 执行AI分析
  const handleAnalyze = async () => {
    setIsAnalyzing(true);
    try {
      const service = createLegalAssistant('divorce');
      const result = await service.analyze(caseData);
      
      if (result.success && result.analysis) {
        onUpdate({ 
          analysis: result.analysis as DivorceAnalysis,
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

  // 生成法律文书
  const handleGenerateDocument = async (type: 'application' | 'letter') => {
    const service = createLegalAssistant('divorce');
    const result = await service.generateDocument(caseData, type);
    if (result.success && result.document) {
      // 可以弹窗显示或下载
      alert('文书已生成，请查看控制台');
      console.log(result.document);
    }
  };

  return (
    <div className="divorce-assistant">
      <div className="assistant-header">
        <h2>💔 离婚财产分析助手</h2>
        <p className="assistant-desc">帮您理清财产、预估分割方案、提供法律建议</p>
      </div>

      {/* 标签页导航 */}
      <div className="tab-nav">
        <button 
          className={`tab-btn ${activeTab === 'info' ? 'active' : ''}`}
          onClick={() => setActiveTab('info')}
        >
          📋 基本信息
        </button>
        <button 
          className={`tab-btn ${activeTab === 'assets' ? 'active' : ''}`}
          onClick={() => setActiveTab('assets')}
        >
          💰 财产清单
          {caseData.assets?.length > 0 && (
            <span className="badge">{caseData.assets.length}</span>
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

      {/* 基本信息 */}
      {activeTab === 'info' && (
        <div className="tab-content">
          <div className="form-section">
            <h3>婚姻基本情况</h3>
            
            <div className="form-row">
              <div className="form-group">
                <label>结婚日期</label>
                <input
                  type="date"
                  value={caseData.marriageDate || ''}
                  onChange={(e) => onUpdate({ marriageDate: e.target.value })}
                />
              </div>
              <div className="form-group">
                <label>分居日期（如有）</label>
                <input
                  type="date"
                  value={caseData.separationDate || ''}
                  onChange={(e) => onUpdate({ separationDate: e.target.value })}
                />
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>
                  <input
                    type="checkbox"
                    checked={caseData.hasChildren}
                    onChange={(e) => onUpdate({ hasChildren: e.target.checked })}
                  />
                  有未成年子女
                </label>
              </div>
            </div>

            {caseData.hasChildren && (
              <div className="form-group">
                <label>子女年龄（用逗号分隔多个）</label>
                <input
                  type="text"
                  placeholder="例如：5, 8"
                  value={caseData.childrenAges?.join(', ') || ''}
                  onChange={(e) => {
                    const ages = e.target.value.split(',').map(s => parseInt(s.trim())).filter(n => !isNaN(n));
                    onUpdate({ childrenAges: ages });
                  }}
                />
              </div>
            )}

            {caseData.hasChildren && (
              <div className="form-group">
                <label>抚养权意向</label>
                <select
                  value={caseData.childrenCustody || 'undecided'}
                  onChange={(e) => onUpdate({ childrenCustody: e.target.value as DivorceCase['childrenCustody'] })}
                >
                  <option value="undecided">待定</option>
                  <option value="self">我争取抚养权</option>
                  <option value="spouse">对方抚养</option>
                  <option value="shared">共同抚养</option>
                </select>
              </div>
            )}
          </div>

          <div className="form-section">
            <h3>案件描述（可选）</h3>
            <textarea
              placeholder="简要描述您的情况，例如：婚后感情不和、存在家暴、对方出轨等..."
              value={caseData.description || ''}
              onChange={(e) => onUpdate({ description: e.target.value })}
              rows={4}
            />
          </div>
        </div>
      )}

      {/* 财产清单 */}
      {activeTab === 'assets' && (
        <div className="tab-content">
          {/* 财产汇总 */}
          <div className="asset-summary">
            <div className="summary-item">
              <span className="label">总资产</span>
              <span className="value positive">¥{assetSummary.totalAssets.toLocaleString()}</span>
            </div>
            <div className="summary-item">
              <span className="label">总负债</span>
              <span className="value negative">¥{assetSummary.totalDebts.toLocaleString()}</span>
            </div>
            <div className="summary-item">
              <span className="label">净资产</span>
              <span className={`value ${assetSummary.netAssets >= 0 ? 'positive' : 'negative'}`}>
                ¥{assetSummary.netAssets.toLocaleString()}
              </span>
            </div>
          </div>

          {/* 添加财产 */}
          <div className="add-asset-form">
            <h3>添加财产/负债</h3>
            <div className="form-row">
              <div className="form-group">
                <label>名称</label>
                <input
                  type="text"
                  placeholder="例如：海淀区XX小区房产"
                  value={newAsset.name || ''}
                  onChange={(e) => setNewAsset(prev => ({ ...prev, name: e.target.value }))}
                />
              </div>
              <div className="form-group">
                <label>类型</label>
                <select
                  value={newAsset.type}
                  onChange={(e) => setNewAsset(prev => ({ ...prev, type: e.target.value as DivorceAsset['type'] }))}
                >
                  {assetTypes.map(t => (
                    <option key={t.value} value={t.value}>{t.label}</option>
                  ))}
                </select>
              </div>
            </div>
            <div className="form-row">
              <div className="form-group">
                <label>估值（元）</label>
                <input
                  type="number"
                  placeholder="0"
                  value={newAsset.value || ''}
                  onChange={(e) => setNewAsset(prev => ({ ...prev, value: Number(e.target.value) }))}
                />
              </div>
              <div className="form-group">
                <label>归属</label>
                <select
                  value={newAsset.ownership}
                  onChange={(e) => setNewAsset(prev => ({ ...prev, ownership: e.target.value as DivorceAsset['ownership'] }))}
                >
                  {ownershipTypes.map(t => (
                    <option key={t.value} value={t.value}>{t.label}</option>
                  ))}
                </select>
              </div>
            </div>
            <button className="btn-add" onClick={handleAddAsset}>
              ➕ 添加
            </button>
          </div>

          {/* 财产列表 */}
          <div className="asset-list">
            <h3>已添加财产</h3>
            {caseData.assets?.length === 0 ? (
              <p className="empty-tip">暂无财产信息，点击上方添加</p>
            ) : (
              <table className="asset-table">
                <thead>
                  <tr>
                    <th>名称</th>
                    <th>类型</th>
                    <th>估值</th>
                    <th>归属</th>
                    <th>操作</th>
                  </tr>
                </thead>
                <tbody>
                  {caseData.assets?.map(asset => (
                    <tr key={asset.id}>
                      <td>{asset.name}</td>
                      <td>{assetTypes.find(t => t.value === asset.type)?.label}</td>
                      <td className={asset.type === 'debt' ? 'negative' : 'positive'}>
                        {asset.type === 'debt' ? '-' : ''}¥{asset.value.toLocaleString()}
                      </td>
                      <td>{ownershipTypes.find(t => t.value === asset.ownership)?.label}</td>
                      <td>
                        <button 
                          className="btn-remove"
                          onClick={() => handleRemoveAsset(asset.id)}
                        >
                          删除
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
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
              '结婚证',
              '房产证/购房合同',
              '车辆行驶证',
              '银行流水',
              '工资流水',
              '股票/基金账户截图',
              '债务凭证（借条、贷款合同）',
              '共同债务/投资记录',
            ]}
          />
        </div>
      )}

      {/* 分析结果 */}
      {activeTab === 'result' && caseData.analysis && (
        <div className="tab-content">
          <AnalysisResult
            type="divorce"
            analysis={caseData.analysis}
          />
        </div>
      )}

      {/* 底部操作栏 */}
      <div className="action-bar">
        <button
          className="btn-analyze"
          onClick={handleAnalyze}
          disabled={isAnalyzing || caseData.assets?.length === 0}
        >
          {isAnalyzing ? '⏳ 分析中...' : '🤖 AI 智能分析'}
        </button>
        
        {caseData.analysis && (
          <div className="doc-actions">
            <button 
              className="btn-doc"
              onClick={() => handleGenerateDocument('letter')}
            >
              📝 生成协议草稿
            </button>
          </div>
        )}
      </div>
    </div>
  );
};

export default DivorceAssistant;
