/**
 * 法律助手服务 - 策略模式实现
 * 使用 Strategy Pattern 处理不同类型的法律案件分析
 */

import { getApiBaseUrl } from '../../../services/config';
import type { 
  CaseType, 
  LegalCase, 
  DivorceCase, 
  LaborCase, 
  RentalCase,
  DivorceAnalysis,
  LaborAnalysis,
  RentalAnalysis,
  AnalysisResponse,
  Evidence
} from '../types';

// 获取API URL
const getApiUrl = () => getApiBaseUrl();

// API响应类型
interface ApiResponse<T> {
  success: boolean;
  error?: string;
  case?: T;
  cases?: T[];
  totalCount?: number;
  analysis?: DivorceAnalysis | LaborAnalysis | RentalAnalysis;
  document?: string;
  evidence?: Evidence;
}

// ===== 后端API调用函数 =====
async function getAuthToken(): Promise<string | null> {
  const { authStorage } = await import('../../../services/authService');
  return authStorage.getToken();
}

async function apiRequest<T>(
  endpoint: string, 
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const token = await getAuthToken();
  
  if (!token) {
    return { success: false, error: '请先登录' };
  }

  try {
    const response = await fetch(`${getApiUrl()}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
        ...options.headers,
      },
    });

    if (!response.ok) {
      if (response.status === 401) {
        const { authStorage } = await import('../../../services/authService');
        authStorage.clear();
        return { success: false, error: '登录已过期，请重新登录' };
      }
      const error = await response.json().catch(() => ({}));
      return { success: false, error: error.error || '请求失败' };
    }

    // Handle 204 No Content
    if (response.status === 204) {
      return { success: true };
    }

    return await response.json();
  } catch (error) {
    console.error('API request error:', error);
    return { success: false, error: '网络错误，请稍后重试' };
  }
}

// ===== 后端案件管理API =====
export const legalCaseApi = {
  /**
   * 获取用户的案件列表
   */
  async getCases(caseType?: CaseType): Promise<ApiResponse<LegalCase>> {
    const query = caseType ? `?caseType=${caseType}` : '';
    return apiRequest(`/api/legal/cases${query}`);
  },

  /**
   * 获取单个案件详情
   */
  async getCase(caseId: string): Promise<ApiResponse<LegalCase>> {
    return apiRequest(`/api/legal/cases/${caseId}`);
  },

  /**
   * 创建新案件
   */
  async createCase(data: {
    caseType: CaseType;
    title: string;
    description?: string;
    caseData?: Record<string, unknown>;
  }): Promise<ApiResponse<LegalCase>> {
    return apiRequest('/api/legal/cases', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  },

  /**
   * 更新案件
   */
  async updateCase(caseId: string, data: {
    title?: string;
    description?: string;
    status?: string;
    caseData?: Record<string, unknown>;
    analysisResult?: Record<string, unknown>;
  }): Promise<ApiResponse<LegalCase>> {
    return apiRequest(`/api/legal/cases/${caseId}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  },

  /**
   * 删除案件
   */
  async deleteCase(caseId: string): Promise<ApiResponse<void>> {
    return apiRequest(`/api/legal/cases/${caseId}`, {
      method: 'DELETE',
    });
  },

  /**
   * 添加证据
   */
  async addEvidence(caseId: string, evidence: {
    type: string;
    name: string;
    description?: string;
    mimeType?: string;
    fileData?: string; // base64
  }): Promise<ApiResponse<Evidence>> {
    return apiRequest(`/api/legal/cases/${caseId}/evidence`, {
      method: 'POST',
      body: JSON.stringify(evidence),
    });
  },

  /**
   * 删除证据
   */
  async deleteEvidence(evidenceId: string): Promise<ApiResponse<void>> {
    return apiRequest(`/api/legal/evidence/${evidenceId}`, {
      method: 'DELETE',
    });
  },

  /**
   * 转写音频证据
   */
  async transcribeEvidence(evidenceId: string): Promise<ApiResponse<Evidence>> {
    return apiRequest(`/api/legal/evidence/${evidenceId}/transcribe`, {
      method: 'POST',
    });
  },

  /**
   * AI分析案件
   */
  async analyzeCase(caseId: string): Promise<ApiResponse<LegalCase>> {
    return apiRequest('/api/legal/analyze', {
      method: 'POST',
      body: JSON.stringify({ caseId }),
    });
  },

  /**
   * 生成法律文书
   */
  async generateDocument(caseId: string, documentType: string): Promise<ApiResponse<LegalCase>> {
    return apiRequest('/api/legal/generate-document', {
      method: 'POST',
      body: JSON.stringify({ caseId, documentType }),
    });
  },
};

// ===== 策略接口 =====
interface ILegalAnalysisStrategy {
  buildPrompt(caseData: Partial<LegalCase>): string;
  parseResponse(response: string): DivorceAnalysis | LaborAnalysis | RentalAnalysis;
  getSystemPrompt(): string;
}

// ===== 离婚财产分析策略 =====
class DivorceAnalysisStrategy implements ILegalAnalysisStrategy {
  getSystemPrompt(): string {
    return `你是一位专业的婚姻家事律师，精通中国婚姻法和最新的民法典相关规定。
你的任务是帮助用户分析离婚财产分割情况，提供专业、客观的法律建议。

重要法律依据：
- 《民法典》婚姻家庭编（2021年施行）
- 夫妻共同财产：婚后所得原则上均为共同财产
- 个人财产：婚前财产、遗嘱/赠与明确归一方的财产
- 子女抚养费：根据当地生活水平和支付方计算能力确定

请始终：
1. 给出具体的法律条文引用
2. 计算要精确，说明计算依据
3. 指出潜在风险和注意事项
4. 建议用户收集的关键证据`;
  }

  buildPrompt(caseData: Partial<DivorceCase>): string {
    const { 
      marriageDate, separationDate, hasChildren, childrenAges, 
      assets = [], evidences = [] 
    } = caseData as Partial<DivorceCase>;
    
    const assetList = assets.map(a => 
      `- ${a.name}（${a.type}）：价值约${a.value}元，归属：${a.ownership}`
    ).join('\n');
    
    const evidenceList = evidences.map(e => 
      `- ${e.name}（${e.type}）${e.transcript ? '：' + e.transcript.slice(0, 100) + '...' : ''}`
    ).join('\n');

    return `请分析以下离婚财产分割情况：

## 基本信息
- 结婚日期：${marriageDate || '未提供'}
- 分居日期：${separationDate || '未提供'}
- 是否有子女：${hasChildren ? '是' : '否'}
${childrenAges?.length ? `- 子女年龄：${childrenAges.join('、')}岁` : ''}

## 财产清单
${assetList || '暂无财产信息'}

## 已收集证据
${evidenceList || '暂无证据'}

请提供：
1. 财产分类（共同财产 vs 个人财产）
2. 预估分割方案和金额
3. 子女抚养费建议（如适用）
4. 需要补充的证据
5. 法律依据和参考条文
6. 风险提示

请以JSON格式返回，结构如下：
{
  "totalAssets": 数字,
  "totalDebts": 数字,
  "jointAssets": 数字,
  "personalAssets": 数字,
  "estimatedSplit": {"self": 数字, "spouse": 数字},
  "childSupportEstimate": 数字或null,
  "recommendations": ["建议1", "建议2"],
  "legalReferences": ["法条1", "法条2"],
  "riskPoints": ["风险1", "风险2"]
}`;
  }

  parseResponse(response: string): DivorceAnalysis {
    try {
      // 尝试从response中提取JSON
      const jsonMatch = response.match(/\{[\s\S]*\}/);
      if (jsonMatch) {
        return JSON.parse(jsonMatch[0]) as DivorceAnalysis;
      }
    } catch (e) {
      console.error('Failed to parse divorce analysis:', e);
    }
    
    // 返回默认结构
    return {
      totalAssets: 0,
      totalDebts: 0,
      jointAssets: 0,
      personalAssets: 0,
      estimatedSplit: { self: 0, spouse: 0 },
      recommendations: ['解析失败，请重试或联系客服'],
      legalReferences: [],
      riskPoints: []
    };
  }
}

// ===== 劳动仲裁分析策略 =====
class LaborArbitrationStrategy implements ILegalAnalysisStrategy {
  getSystemPrompt(): string {
    return `你是一位专业的劳动法律师，精通中国劳动法、劳动合同法和相关司法解释。
你的任务是帮助劳动者分析其权益受损情况，计算应得赔偿，并提供维权指导。

重要法律依据：
- 《劳动合同法》（2012年修正）
- 《劳动争议调解仲裁法》
- 未签合同双倍工资：最多11个月
- 违法解除赔偿金：2N（N为工作年限每满一年算1个月工资）
- 经济补偿金：N 或 N+1
- 加班费：平日1.5倍、周末2倍、法定节假日3倍

请始终：
1. 精确计算各项赔偿金额
2. 列出完整的证据清单
3. 提供仲裁申请的关键时间节点
4. 指出对方可能的抗辩点`;
  }

  buildPrompt(caseData: Partial<LaborCase>): string {
    const { 
      companyName, position, entryDate, leaveDate,
      monthlySalary, hasContract, hasSocialInsurance,
      violations = [], workingYears, evidences = []
    } = caseData as Partial<LaborCase>;
    
    const violationNames: Record<string, string> = {
      'no_contract': '未签劳动合同',
      'no_social_insurance': '未缴社保',
      'overtime_unpaid': '加班费未付',
      'illegal_dismissal': '违法解雇',
      'salary_arrears': '拖欠工资',
      'forced_resignation': '强迫离职',
      'work_injury': '工伤未赔',
      'maternity_violation': '侵犯孕产假'
    };
    
    const violationList = violations.map(v => violationNames[v] || v).join('、');
    const evidenceList = evidences.map(e => 
      `- ${e.name}（${e.type}）${e.transcript ? '：' + e.transcript.slice(0, 100) + '...' : ''}`
    ).join('\n');

    return `请分析以下劳动争议案件：

## 基本信息
- 公司名称：${companyName || '未提供'}
- 职位：${position || '未提供'}
- 入职日期：${entryDate || '未提供'}
- 离职日期：${leaveDate || '在职'}
- 月薪：${monthlySalary || 0}元
- 工作年限：约${workingYears || 0}年
- 是否签订劳动合同：${hasContract ? '是' : '否'}
- 是否缴纳社保：${hasSocialInsurance ? '是' : '否'}

## 违法情况
${violationList || '未说明'}

## 已收集证据
${evidenceList || '暂无证据'}

请提供：
1. 每项违法行为的赔偿计算（含计算公式）
2. 总赔偿金额
3. 必需的证据清单（标注已收集/未收集）
4. 仲裁申请书关键内容
5. 维权时间线和关键节点
6. 法律依据

请以JSON格式返回，结构如下：
{
  "violations": [
    {
      "type": "违法类型代码",
      "description": "具体描述",
      "compensation": 赔偿金额,
      "legalBasis": "法律依据"
    }
  ],
  "totalCompensation": 总金额,
  "evidenceChecklist": [
    {"item": "证据名称", "required": true/false, "collected": true/false}
  ],
  "applicationDraft": "仲裁申请书核心内容",
  "recommendations": ["建议1", "建议2"],
  "timeline": [
    {"step": "步骤", "deadline": "期限"}
  ]
}`;
  }

  parseResponse(response: string): LaborAnalysis {
    try {
      const jsonMatch = response.match(/\{[\s\S]*\}/);
      if (jsonMatch) {
        return JSON.parse(jsonMatch[0]) as LaborAnalysis;
      }
    } catch (e) {
      console.error('Failed to parse labor analysis:', e);
    }
    
    return {
      violations: [],
      totalCompensation: 0,
      evidenceChecklist: [],
      recommendations: ['解析失败，请重试或联系客服'],
      timeline: []
    };
  }
}

// ===== 租房纠纷分析策略 =====
class RentalDisputeStrategy implements ILegalAnalysisStrategy {
  getSystemPrompt(): string {
    return `你是一位专业的房产律师，精通中国合同法、民法典租赁合同编和各地租房管理条例。
你的任务是帮助租客分析租房纠纷，评估维权可能性，并提供具体的维权方案。

重要法律依据：
- 《民法典》合同编-租赁合同章节
- 《商品房屋租赁管理办法》
- 各地住房租赁条例
- 押金退还：无特别约定时，合同到期后房东应全额退还
- 违约责任：看合同约定，无约定按实际损失

请始终：
1. 分析合同条款的合法性
2. 评估胜诉概率
3. 提供多种维权途径（协商、投诉、诉讼）
4. 草拟维权文书`;
  }

  buildPrompt(caseData: Partial<RentalCase>): string {
    const { 
      landlordName, address, monthlyRent, deposit,
      contractStartDate, contractEndDate, hasWrittenContract,
      disputeTypes = [], isRegistered, evidences = []
    } = caseData as Partial<RentalCase>;
    
    const disputeNames: Record<string, string> = {
      'deposit_refund': '押金不退',
      'illegal_eviction': '违法驱赶',
      'repair_dispute': '维修纠纷',
      'sublease_issue': '转租问题',
      'rent_increase': '违规涨租',
      'contract_breach': '合同违约',
      'living_condition': '居住条件差'
    };
    
    const disputeList = disputeTypes.map(d => disputeNames[d] || d).join('、');
    const evidenceList = evidences.map(e => 
      `- ${e.name}（${e.type}）${e.transcript ? '：' + e.transcript.slice(0, 100) + '...' : ''}`
    ).join('\n');

    return `请分析以下租房纠纷案件：

## 基本信息
- 房东姓名：${landlordName || '未提供'}
- 房屋地址：${address || '未提供'}
- 月租金：${monthlyRent || 0}元
- 押金：${deposit || 0}元
- 合同期限：${contractStartDate || '?'} 至 ${contractEndDate || '?'}
- 是否有书面合同：${hasWrittenContract ? '是' : '否'}
- 合同是否备案：${isRegistered ? '是' : '否'}

## 纠纷类型
${disputeList || '未说明'}

## 已收集证据
${evidenceList || '暂无证据'}

请提供：
1. 合同条款分析（如有合同）
2. 胜诉概率评估
3. 可主张的赔偿金额
4. 证据收集清单
5. 维权步骤（协商→投诉→诉讼）
6. 律师函/投诉信草稿

请以JSON格式返回，结构如下：
{
  "contractLoopholes": ["漏洞1", "漏洞2"],
  "disputeAssessment": [
    {
      "type": "纠纷类型代码",
      "description": "具体描述",
      "winProbability": 0-100的数字,
      "compensation": 可主张金额,
      "legalBasis": "法律依据"
    }
  ],
  "evidenceChecklist": [
    {"item": "证据名称", "importance": "critical/important/helpful", "collected": true/false}
  ],
  "actionPlan": ["步骤1", "步骤2"],
  "letterDraft": "律师函/投诉信内容",
  "recommendations": ["建议1", "建议2"]
}`;
  }

  parseResponse(response: string): RentalAnalysis {
    try {
      const jsonMatch = response.match(/\{[\s\S]*\}/);
      if (jsonMatch) {
        return JSON.parse(jsonMatch[0]) as RentalAnalysis;
      }
    } catch (e) {
      console.error('Failed to parse rental analysis:', e);
    }
    
    return {
      contractLoopholes: [],
      disputeAssessment: [],
      evidenceChecklist: [],
      actionPlan: [],
      recommendations: ['解析失败，请重试或联系客服']
    };
  }
}

// ===== 策略工厂 =====
const strategyMap: Record<CaseType, ILegalAnalysisStrategy> = {
  divorce: new DivorceAnalysisStrategy(),
  labor: new LaborArbitrationStrategy(),
  rental: new RentalDisputeStrategy()
};

// ===== 法律助手服务 =====
export class LegalAssistantService {
  private strategy: ILegalAnalysisStrategy;
  private caseType: CaseType;

  constructor(caseType: CaseType) {
    this.strategy = strategyMap[caseType];
    this.caseType = caseType;
  }

  /**
   * 执行AI分析 - 使用专用法律助手API
   */
  async analyze(caseData: Partial<LegalCase>): Promise<AnalysisResponse> {
    const { authStorage } = await import('../../../services/authService');
    const token = authStorage.getToken();
    
    if (!token) {
      return { success: false, error: '请先登录' };
    }

    try {
      // 使用专用的法律分析API，确保统计正确归类
      const response = await fetch(`${getApiUrl()}/api/legal/analyze-direct`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify({
          caseType: this.caseType,
          caseData: caseData,
          systemPrompt: this.strategy.getSystemPrompt(),
          userPrompt: this.strategy.buildPrompt(caseData)
        }),
      });

      if (!response.ok) {
        if (response.status === 401) {
          authStorage.clear();
          return { success: false, error: '登录已过期，请重新登录' };
        }
        return { success: false, error: '分析请求失败' };
      }

      const result = await response.json();
      
      if (result.success && result.message) {
        const analysis = this.strategy.parseResponse(result.message);
        return { success: true, analysis };
      }
      
      return { success: false, error: result.error || '分析失败' };
    } catch (error) {
      console.error('Legal analysis error:', error);
      return { success: false, error: '网络错误，请稍后重试' };
    }
  }

  /**
   * 证据转文字（录音/图片OCR）
   */
  async transcribeEvidence(evidence: Evidence): Promise<{ success: boolean; text?: string; error?: string }> {
    const { authStorage } = await import('../../../services/authService');
    const token = authStorage.getToken();
    
    if (!token) {
      return { success: false, error: '请先登录' };
    }

    if (evidence.type === 'audio' && evidence.data) {
      // 调用语音转文字API
      try {
        const response = await fetch(`${getApiUrl()}/api/speech/transcribe`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`,
          },
          body: JSON.stringify({
            audioBase64: evidence.data,
            audioFormat: 'wav'
          }),
        });

        if (response.ok) {
          const result = await response.json();
          return { success: true, text: result.text };
        }
      } catch (error) {
        console.error('Transcription error:', error);
      }
      return { success: false, error: '语音转文字失败' };
    }

    // 图片OCR等其他类型
    return { success: false, error: '暂不支持该类型证据的识别' };
  }

  /**
   * 生成法律文书 - 使用专用法律助手API
   */
  async generateDocument(
    caseData: Partial<LegalCase>, 
    documentType: 'application' | 'letter' | 'complaint'
  ): Promise<{ success: boolean; document?: string; error?: string }> {
    const { authStorage } = await import('../../../services/authService');
    const token = authStorage.getToken();
    
    if (!token) {
      return { success: false, error: '请先登录' };
    }

    try {
      // 使用专用的法律文书生成API，确保统计正确归类
      const response = await fetch(`${getApiUrl()}/api/legal/generate-document-direct`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify({
          caseType: this.caseType,
          caseData: caseData,
          documentType: documentType
        }),
      });

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          return { success: true, document: result.document };
        }
      }
      
      return { success: false, error: '文书生成失败' };
    } catch (error) {
      console.error('Document generation error:', error);
      return { success: false, error: '网络错误，请稍后重试' };
    }
  }
}

// 导出工厂函数
export const createLegalAssistant = (caseType: CaseType) => new LegalAssistantService(caseType);
