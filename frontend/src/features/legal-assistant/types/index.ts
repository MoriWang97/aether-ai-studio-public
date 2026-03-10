/**
 * 法律助手类型定义
 * 统一三个场景的核心数据结构
 */

// 案件类型
export type CaseType = 'divorce' | 'labor' | 'rental';

// 案件状态
export type CaseStatus = 'draft' | 'analyzing' | 'completed' | 'archived';

// 证据类型
export type EvidenceType = 
  | 'audio'      // 录音
  | 'image'      // 图片
  | 'document'   // 文档
  | 'contract'   // 合同
  | 'chat'       // 聊天记录
  | 'bank'       // 银行流水
  | 'property'   // 房产证明
  | 'other';     // 其他

// 证据项
export interface Evidence {
  id: string;
  type: EvidenceType;
  name: string;
  description?: string;
  data?: string;          // base64 数据
  url?: string;           // 文件URL
  transcript?: string;    // 录音转文字
  extractedInfo?: string; // AI提取的关键信息
  createdAt: Date;
}

// 基础案件信息
export interface BaseCase {
  id: string;
  type: CaseType;
  status: CaseStatus;
  title: string;
  description?: string;
  evidences: Evidence[];
  createdAt: Date;
  updatedAt: Date;
}

// ===== 离婚财产分析 =====
export interface DivorceAsset {
  id: string;
  name: string;
  type: 'house' | 'car' | 'savings' | 'stock' | 'debt' | 'other';
  value: number;
  ownership: 'joint' | 'personal_self' | 'personal_spouse' | 'unknown';
  description?: string;
  evidence?: string[]; // 关联的证据ID
}

export interface DivorceCase extends BaseCase {
  type: 'divorce';
  marriageDate?: string;        // 结婚日期
  separationDate?: string;      // 分居日期
  hasChildren: boolean;         // 是否有子女
  childrenAges?: number[];      // 子女年龄
  childrenCustody?: 'self' | 'spouse' | 'shared' | 'undecided';
  assets: DivorceAsset[];       // 财产清单
  analysis?: DivorceAnalysis;   // AI分析结果
}

export interface DivorceAnalysis {
  totalAssets: number;          // 总资产
  totalDebts: number;           // 总负债
  jointAssets: number;          // 共同财产
  personalAssets: number;       // 个人财产
  estimatedSplit: {
    self: number;
    spouse: number;
  };
  childSupportEstimate?: number; // 预估抚养费（月）
  recommendations: string[];     // 建议
  legalReferences: string[];     // 法律依据
  riskPoints: string[];          // 风险点
}

// ===== 劳动仲裁 =====
export type LaborViolationType = 
  | 'no_contract'           // 未签劳动合同
  | 'no_social_insurance'   // 未缴社保
  | 'overtime_unpaid'       // 加班费未付
  | 'illegal_dismissal'     // 违法解雇
  | 'salary_arrears'        // 拖欠工资
  | 'forced_resignation'    // 强迫离职
  | 'work_injury'           // 工伤未赔
  | 'maternity_violation'   // 侵犯孕产假
  | 'other';

export interface LaborCase extends BaseCase {
  type: 'labor';
  companyName: string;          // 公司名称
  position: string;             // 职位
  entryDate: string;            // 入职日期
  leaveDate?: string;           // 离职日期
  monthlySalary: number;        // 月薪
  hasContract: boolean;         // 是否签合同
  hasSocialInsurance: boolean;  // 是否缴社保
  violations: LaborViolationType[];
  workingYears: number;         // 工作年限
  analysis?: LaborAnalysis;     // AI分析结果
}

export interface LaborAnalysis {
  violations: {
    type: LaborViolationType;
    description: string;
    compensation: number;
    legalBasis: string;
  }[];
  totalCompensation: number;    // 总赔偿金额
  evidenceChecklist: {
    item: string;
    required: boolean;
    collected: boolean;
  }[];
  applicationDraft?: string;    // 仲裁申请书草稿
  recommendations: string[];    // 建议
  timeline: {
    step: string;
    deadline?: string;
  }[];
}

// ===== 租房纠纷 =====
export type RentalDisputeType =
  | 'deposit_refund'        // 押金不退
  | 'illegal_eviction'      // 违法驱赶
  | 'repair_dispute'        // 维修纠纷
  | 'sublease_issue'        // 转租问题
  | 'rent_increase'         // 违规涨租
  | 'contract_breach'       // 合同违约
  | 'living_condition'      // 居住条件差
  | 'other';

export interface RentalCase extends BaseCase {
  type: 'rental';
  landlordName?: string;        // 房东姓名
  address: string;              // 房屋地址
  monthlyRent: number;          // 月租金
  deposit: number;              // 押金
  contractStartDate: string;    // 合同开始日期
  contractEndDate: string;      // 合同结束日期
  hasWrittenContract: boolean;  // 是否有书面合同
  disputeTypes: RentalDisputeType[];
  isRegistered: boolean;        // 是否备案
  analysis?: RentalAnalysis;    // AI分析结果
}

export interface RentalAnalysis {
  contractLoopholes: string[];  // 合同漏洞
  disputeAssessment: {
    type: RentalDisputeType;
    description: string;
    winProbability: number;     // 胜诉概率
    compensation: number;       // 可主张金额
    legalBasis: string;
  }[];
  evidenceChecklist: {
    item: string;
    importance: 'critical' | 'important' | 'helpful';
    collected: boolean;
  }[];
  actionPlan: string[];         // 维权步骤
  letterDraft?: string;         // 律师函/投诉信草稿
  recommendations: string[];
}

// 统一的案件类型
export type LegalCase = DivorceCase | LaborCase | RentalCase;

// AI分析请求
export interface AnalysisRequest {
  caseType: CaseType;
  caseData: Partial<LegalCase>;
  analysisType: 'full' | 'quick' | 'evidence' | 'document';
}

// AI分析响应
export interface AnalysisResponse {
  success: boolean;
  analysis?: DivorceAnalysis | LaborAnalysis | RentalAnalysis;
  error?: string;
}
