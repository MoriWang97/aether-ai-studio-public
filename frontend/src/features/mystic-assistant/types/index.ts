/**
 * 玄学AI助手类型定义
 * 包含塔罗牌、星座运势、八字命理
 */

// ===== 通用类型 =====
export type MysticType = 'tarot' | 'astrology' | 'bazi';

export interface MysticSession {
  id: string;
  type: MysticType;
  title: string;
  question?: string;
  createdAt: Date;
  updatedAt: Date;
  status: 'draft' | 'reading' | 'completed';
  result?: TarotReading | AstrologyReading | BaziReading;
  messages: ChatMessage[];
}

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
}

// ===== 塔罗牌类型 =====
export type TarotSpreadType = 
  | 'single'        // 单牌 - 快速解答
  | 'three_cards'   // 三牌阵 - 过去/现在/未来
  | 'celtic_cross'  // 凯尔特十字 - 深度分析
  | 'relationship'  // 关系牌阵 - 感情分析
  | 'career'        // 事业牌阵
  | 'yes_no';       // 是否牌阵

export interface TarotCard {
  id: number;           // 0-77
  name: string;         // 如 "愚者"、"魔术师"
  nameEn: string;       // "The Fool", "The Magician"
  arcana: 'major' | 'minor';  // 大阿尔卡纳/小阿尔卡纳
  suit?: 'wands' | 'cups' | 'swords' | 'pentacles'; // 权杖/圣杯/宝剑/金币
  number?: number;      // 牌面数字
  isReversed: boolean;  // 是否逆位
  keywords: string[];   // 关键词
  imageUrl?: string;    // 牌面图片
}

export interface TarotPosition {
  name: string;         // 位置名称，如"过去"、"现在"
  description: string;  // 位置含义
  card?: TarotCard;     // 抽到的牌
}

export interface TarotReading {
  spreadType: TarotSpreadType;
  question: string;
  positions: TarotPosition[];
  interpretation: string;     // AI解读
  advice: string;             // 建议
  luckIndex?: number;         // 运势指数 0-100
  createdAt: Date;
}

export interface TarotRequest {
  spreadType: TarotSpreadType;
  question: string;
  focusArea?: 'love' | 'career' | 'wealth' | 'health' | 'general';
}

// ===== 星座类型 =====
export type ZodiacSign = 
  | 'aries' | 'taurus' | 'gemini' | 'cancer' 
  | 'leo' | 'virgo' | 'libra' | 'scorpio'
  | 'sagittarius' | 'capricorn' | 'aquarius' | 'pisces';

export type AstrologyPeriod = 'daily' | 'weekly' | 'monthly' | 'yearly';

export interface ZodiacInfo {
  sign: ZodiacSign;
  name: string;         // 中文名
  nameEn: string;       // 英文名
  element: 'fire' | 'earth' | 'air' | 'water';  // 元素
  quality: 'cardinal' | 'fixed' | 'mutable';     // 模式
  ruler: string;        // 守护星
  dateRange: string;    // 日期范围
  symbol: string;       // 符号
}

export interface AstrologyReading {
  sign: ZodiacSign;
  period: AstrologyPeriod;
  date: string;
  
  // 各方面运势
  overall: {
    score: number;      // 0-100
    summary: string;
    advice: string;
  };
  love: {
    score: number;
    summary: string;
    advice: string;
  };
  career: {
    score: number;
    summary: string;
    advice: string;
  };
  wealth: {
    score: number;
    summary: string;
    advice: string;
  };
  health: {
    score: number;
    summary: string;
    advice: string;
  };
  
  luckyColor: string;
  luckyNumber: number;
  luckyDirection: string;
  compatibility: ZodiacSign[];  // 今日最配星座
}

export interface AstrologyRequest {
  sign: ZodiacSign;
  period: AstrologyPeriod;
  birthDate?: string;   // 用于更精准的分析
  birthTime?: string;   // 出生时间
  birthPlace?: string;  // 出生地点
}

// ===== 八字命理类型 =====
export type HeavenlyStem = '甲' | '乙' | '丙' | '丁' | '戊' | '己' | '庚' | '辛' | '壬' | '癸';
export type EarthlyBranch = '子' | '丑' | '寅' | '卯' | '辰' | '巳' | '午' | '未' | '申' | '酉' | '戌' | '亥';
export type WuXing = '金' | '木' | '水' | '火' | '土';

export interface Pillar {
  stem: HeavenlyStem;     // 天干
  branch: EarthlyBranch;  // 地支
  element: WuXing;        // 五行
  hiddenStems?: HeavenlyStem[];  // 藏干
}

export interface BaziChart {
  yearPillar: Pillar;     // 年柱
  monthPillar: Pillar;    // 月柱
  dayPillar: Pillar;      // 日柱
  hourPillar: Pillar;     // 时柱
  
  dayMaster: HeavenlyStem; // 日主
  dayMasterElement: WuXing;
  
  wuxingCount: Record<WuXing, number>;  // 五行统计
  wuxingBalance: string;  // 五行强弱分析
}

export interface BaziReading {
  chart: BaziChart;
  
  // 五行格局分析
  wuxingAnalysis?: string;
  
  // 命盘分析
  personality: {
    traits: string[];           // 性格特点
    strengths: string[];        // 优势
    weaknesses: string[];       // 弱点
    advice: string;
  };
  
  // 事业分析
  career: {
    suitableFields: string[];   // 适合行业
    luckyDirections: string[];  // 吉利方位
    advice: string;
  };
  
  // 感情分析
  relationship: {
    idealPartner: string;       // 理想伴侣特征
    marriageAge?: string;       // 适婚年龄
    advice: string;
  };
  
  // 财运分析
  wealth: {
    wealthType: string;         // 财富类型
    luckyYears: string[];       // 财运年份
    advice: string;
  };
  
  // 健康分析
  health: {
    weakOrgans: string[];       // 需注意器官
    advice: string;
  };
  
  // 流年运势
  annualFortune?: {
    year: number;
    summary: string;
    luckyMonths: number[];
    challenges: string[];
  };
  
  luckyElements: WuXing[];
  luckyColors: string[];
  luckyNumbers: number[];
}

export interface BaziRequest {
  birthDate: string;      // 出生日期 YYYY-MM-DD
  birthTime: string;      // 出生时间 HH:mm
  birthPlace?: string;    // 出生地点（用于真太阳时校正）
  gender: 'male' | 'female';
  name?: string;          // 姓名（可选）
  analysisYear?: number;  // 分析流年（默认当年）
}

// ===== API响应类型 =====
export interface MysticResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  sessionId?: string;
}

// ===== 星座数据 =====
export const ZODIAC_DATA: ZodiacInfo[] = [
  { sign: 'aries', name: '白羊座', nameEn: 'Aries', element: 'fire', quality: 'cardinal', ruler: '火星', dateRange: '3/21-4/19', symbol: '♈' },
  { sign: 'taurus', name: '金牛座', nameEn: 'Taurus', element: 'earth', quality: 'fixed', ruler: '金星', dateRange: '4/20-5/20', symbol: '♉' },
  { sign: 'gemini', name: '双子座', nameEn: 'Gemini', element: 'air', quality: 'mutable', ruler: '水星', dateRange: '5/21-6/21', symbol: '♊' },
  { sign: 'cancer', name: '巨蟹座', nameEn: 'Cancer', element: 'water', quality: 'cardinal', ruler: '月亮', dateRange: '6/22-7/22', symbol: '♋' },
  { sign: 'leo', name: '狮子座', nameEn: 'Leo', element: 'fire', quality: 'fixed', ruler: '太阳', dateRange: '7/23-8/22', symbol: '♌' },
  { sign: 'virgo', name: '处女座', nameEn: 'Virgo', element: 'earth', quality: 'mutable', ruler: '水星', dateRange: '8/23-9/22', symbol: '♍' },
  { sign: 'libra', name: '天秤座', nameEn: 'Libra', element: 'air', quality: 'cardinal', ruler: '金星', dateRange: '9/23-10/23', symbol: '♎' },
  { sign: 'scorpio', name: '天蝎座', nameEn: 'Scorpio', element: 'water', quality: 'fixed', ruler: '冥王星', dateRange: '10/24-11/22', symbol: '♏' },
  { sign: 'sagittarius', name: '射手座', nameEn: 'Sagittarius', element: 'fire', quality: 'mutable', ruler: '木星', dateRange: '11/23-12/21', symbol: '♐' },
  { sign: 'capricorn', name: '摩羯座', nameEn: 'Capricorn', element: 'earth', quality: 'cardinal', ruler: '土星', dateRange: '12/22-1/19', symbol: '♑' },
  { sign: 'aquarius', name: '水瓶座', nameEn: 'Aquarius', element: 'air', quality: 'fixed', ruler: '天王星', dateRange: '1/20-2/18', symbol: '♒' },
  { sign: 'pisces', name: '双鱼座', nameEn: 'Pisces', element: 'water', quality: 'mutable', ruler: '海王星', dateRange: '2/19-3/20', symbol: '♓' },
];

// ===== 塔罗牌数据 =====
export const MAJOR_ARCANA: Omit<TarotCard, 'isReversed'>[] = [
  { id: 0, name: '愚者', nameEn: 'The Fool', arcana: 'major', keywords: ['新开始', '冒险', '纯真', '自由'] },
  { id: 1, name: '魔术师', nameEn: 'The Magician', arcana: 'major', keywords: ['创造力', '意志力', '技能', '自信'] },
  { id: 2, name: '女祭司', nameEn: 'The High Priestess', arcana: 'major', keywords: ['直觉', '神秘', '智慧', '内在声音'] },
  { id: 3, name: '皇后', nameEn: 'The Empress', arcana: 'major', keywords: ['丰饶', '母性', '自然', '感官享受'] },
  { id: 4, name: '皇帝', nameEn: 'The Emperor', arcana: 'major', keywords: ['权威', '结构', '控制', '父性'] },
  { id: 5, name: '教皇', nameEn: 'The Hierophant', arcana: 'major', keywords: ['传统', '信仰', '教育', '指导'] },
  { id: 6, name: '恋人', nameEn: 'The Lovers', arcana: 'major', keywords: ['爱情', '选择', '和谐', '价值观'] },
  { id: 7, name: '战车', nameEn: 'The Chariot', arcana: 'major', keywords: ['胜利', '决心', '意志', '方向'] },
  { id: 8, name: '力量', nameEn: 'Strength', arcana: 'major', keywords: ['勇气', '耐心', '内在力量', '同情'] },
  { id: 9, name: '隐者', nameEn: 'The Hermit', arcana: 'major', keywords: ['内省', '寻找', '指引', '独处'] },
  { id: 10, name: '命运之轮', nameEn: 'Wheel of Fortune', arcana: 'major', keywords: ['命运', '变化', '周期', '机遇'] },
  { id: 11, name: '正义', nameEn: 'Justice', arcana: 'major', keywords: ['公正', '真相', '法律', '因果'] },
  { id: 12, name: '倒吊人', nameEn: 'The Hanged Man', arcana: 'major', keywords: ['牺牲', '新视角', '等待', '放手'] },
  { id: 13, name: '死神', nameEn: 'Death', arcana: 'major', keywords: ['结束', '转变', '过渡', '重生'] },
  { id: 14, name: '节制', nameEn: 'Temperance', arcana: 'major', keywords: ['平衡', '耐心', '适度', '融合'] },
  { id: 15, name: '恶魔', nameEn: 'The Devil', arcana: 'major', keywords: ['束缚', '物质', '欲望', '影子'] },
  { id: 16, name: '塔', nameEn: 'The Tower', arcana: 'major', keywords: ['突变', '崩塌', '觉醒', '解放'] },
  { id: 17, name: '星星', nameEn: 'The Star', arcana: 'major', keywords: ['希望', '灵感', '宁静', '更新'] },
  { id: 18, name: '月亮', nameEn: 'The Moon', arcana: 'major', keywords: ['幻象', '恐惧', '潜意识', '直觉'] },
  { id: 19, name: '太阳', nameEn: 'The Sun', arcana: 'major', keywords: ['成功', '喜悦', '活力', '正能量'] },
  { id: 20, name: '审判', nameEn: 'Judgement', arcana: 'major', keywords: ['觉醒', '重生', '召唤', '赦免'] },
  { id: 21, name: '世界', nameEn: 'The World', arcana: 'major', keywords: ['完成', '圆满', '成就', '旅程终点'] },
];
