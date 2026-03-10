/**
 * 玄学AI助手服务
 * 处理塔罗牌、星座、八字的AI分析请求
 */

import { getApiBaseUrl } from '../../../services/config';
import type {
  TarotSpreadType,
  TarotReading,
  ZodiacSign,
  AstrologyPeriod,
  AstrologyReading,
  BaziRequest,
  BaziReading,
  WuXing,
} from '../types';
import { ZODIAC_DATA } from '../types';

const getApiUrl = () => getApiBaseUrl();

// 获取认证Token
async function getAuthToken(): Promise<string | null> {
  const { authStorage } = await import('../../../services/authService');
  return authStorage.getToken();
}

// 玄学 API 请求基础函数 - 调用 /api/mystic/* 端点
async function mysticApiRequest<T>(endpoint: string, body: object): Promise<T | null> {
  const token = await getAuthToken();
  if (!token) {
    console.error('Mystic API request failed: No auth token');
    return null;
  }

  try {
    const response = await fetch(`${getApiUrl()}/api/mystic${endpoint}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
      },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      console.error('Mystic API request failed: HTTP', response.status, response.statusText);
      return null;
    }
    const result = await response.json();
    if (!result.success) {
      console.error('Mystic API request failed:', result.error || 'Unknown error');
      return null;
    }
    return result as T;
  } catch (error) {
    console.error('Mystic API request error:', error);
    return null;
  }
}

// ===== 塔罗牌服务 =====
// 从后端 API 获取的塔罗响应类型
interface TarotApiResponse {
  success: boolean;
  error?: string;
  sessionId?: string;
  reading?: {
    spreadType: string;
    question: string;
    positions: Array<{
      name: string;
      card?: {
        id: number;
        name: string;
        nameEn: string;
        arcana: string;
        isReversed: boolean;
        keywords: string[];
      };
    }>;
    interpretation: string;
    advice: string;
    luckIndex?: number;
    createdAt: string;
  };
}

// 存储会话ID用于后续对话
let currentTarotSessionId: string | null = null;
let currentAstrologySessionId: string | null = null;
let currentBaziSessionId: string | null = null;

async function analyzeTarot(spreadType: TarotSpreadType, question: string): Promise<TarotReading | null> {
  const response = await mysticApiRequest<TarotApiResponse>('/tarot/analyze', {
    spreadType,
    question,
    focusArea: 'general'
  });

  if (!response || !response.reading) {
    // fallback: 返回基础错误结果
    return {
      spreadType,
      question,
      positions: [],
      interpretation: '解读失败，请稍后重试',
      advice: '',
      createdAt: new Date(),
    };
  }

  // 保存会话ID用于后续对话
  currentTarotSessionId = response.sessionId || null;

  // 转换后端响应为前端数据结构
  const reading = response.reading;
  return {
    spreadType,
    question,
    positions: reading.positions.map(pos => ({
      name: pos.name,
      description: '',
      card: pos.card ? {
        id: pos.card.id,
        name: pos.card.name,
        nameEn: pos.card.nameEn,
        arcana: pos.card.arcana as 'major' | 'minor',
        keywords: pos.card.keywords || [],
        isReversed: pos.card.isReversed
      } : undefined
    })),
    interpretation: reading.interpretation,
    advice: reading.advice,
    luckIndex: reading.luckIndex,
    createdAt: new Date(reading.createdAt),
  };
}

// 塔罗对话 - 调用后端 API
interface ChatApiResponse {
  success: boolean;
  error?: string;
  response?: string;
}

async function chatWithTarot(message: string, context: TarotReading): Promise<string> {
  if (!currentTarotSessionId) {
    return '抱歉，无法找到对话上下文，请重新占卜。';
  }
  
  const response = await mysticApiRequest<ChatApiResponse>('/tarot/chat', {
    sessionId: currentTarotSessionId,
    message
  });

  return response?.response || '抱歉，无法回答您的问题，请稍后再试。';
}

// ===== 星座服务 =====
// 星座 API 响应类型
interface AstrologyApiResponse {
  success: boolean;
  error?: string;
  sessionId?: string;
  reading?: {
    sign: string;
    period: string;
    date: string;
    overall: { score: number; summary: string; advice: string };
    love: { score: number; summary: string; advice: string };
    career: { score: number; summary: string; advice: string };
    wealth: { score: number; summary: string; advice: string };
    health: { score: number; summary: string; advice: string };
    luckyColor: string;
    luckyNumber: number;
    luckyDirection: string;
    compatibility: string[];
  };
}

async function analyzeAstrology(
  sign: ZodiacSign, 
  period: AstrologyPeriod, 
  birthInfo?: { date?: string; time?: string }
): Promise<AstrologyReading | null> {
  const response = await mysticApiRequest<AstrologyApiResponse>('/astrology/analyze', {
    sign,
    period,
    birthDate: birthInfo?.date,
    birthTime: birthInfo?.time
  });

  if (!response || !response.reading) {
    console.error('Astrology analysis failed: No response');
    return null;
  }

  // 保存会话ID用于后续对话
  currentAstrologySessionId = response.sessionId || null;

  const reading = response.reading;
  
  // 处理 compatibility 数组
  let compatibility: ZodiacSign[] = ['taurus', 'virgo'] as ZodiacSign[];
  if (reading.compatibility && Array.isArray(reading.compatibility)) {
    compatibility = reading.compatibility.map((c: string) => {
      const found = ZODIAC_DATA.find(z => 
        z.name === c || z.nameEn.toLowerCase() === c.toLowerCase() || z.sign === c
      );
      return found?.sign || 'taurus';
    }) as ZodiacSign[];
  }

  return {
    sign,
    period,
    date: reading.date || new Date().toISOString().split('T')[0],
    overall: reading.overall,
    love: reading.love,
    career: reading.career,
    wealth: reading.wealth,
    health: reading.health,
    luckyColor: reading.luckyColor,
    luckyNumber: reading.luckyNumber,
    luckyDirection: reading.luckyDirection,
    compatibility,
  };
}

async function chatWithAstrology(message: string, context: AstrologyReading): Promise<string> {
  if (!currentAstrologySessionId) {
    return '抱歉，无法找到对话上下文，请重新查看运势。';
  }

  const response = await mysticApiRequest<ChatApiResponse>('/astrology/chat', {
    sessionId: currentAstrologySessionId,
    message
  });

  return response?.response || '抱歉，无法回答您的问题，请稍后再试。';
}

// ===== 八字服务 =====
// 八字 API 响应类型
interface BaziApiResponse {
  success: boolean;
  error?: string;
  sessionId?: string;
  reading?: {
    chart: {
      yearPillar: { stem: string; branch: string; element: string };
      monthPillar: { stem: string; branch: string; element: string };
      dayPillar: { stem: string; branch: string; element: string };
      hourPillar: { stem: string; branch: string; element: string };
      dayMaster: string;
      dayMasterElement: string;
      wuxingCount: Record<string, number>;
      wuxingBalance?: string;
    };
    personality: {
      traits: string[];
      strengths: string[];
      weaknesses: string[];
      advice: string;
    };
    career: {
      suitableFields: string[];
      luckyDirections: string[];
      advice: string;
    };
    relationship: {
      idealPartner: string;
      marriageAge: string;
      advice: string;
    };
    wealth: {
      wealthType: string;
      luckyYears: string[];
      advice: string;
    };
    health: {
      weakOrgans: string[];
      advice: string;
    };
    annualFortune?: {
      year: number;
      summary: string;
      luckyMonths: number[];
      challenges: string[];
    };
    luckyElements: string[];
    luckyColors: string[];
    luckyNumbers: number[];
  };
}

async function analyzeBazi(request: BaziRequest): Promise<BaziReading | null> {
  const response = await mysticApiRequest<BaziApiResponse>('/bazi/analyze', {
    birthDate: request.birthDate,
    birthTime: request.birthTime,
    birthPlace: request.birthPlace,
    gender: request.gender,
    name: request.name,
    analysisYear: request.analysisYear || new Date().getFullYear()
  });

  if (!response || !response.reading) {
    console.error('Bazi analysis failed: No response');
    return null;
  }

  // 保存会话ID用于后续对话
  currentBaziSessionId = response.sessionId || null;

  const reading = response.reading;
  
  // 类型转换辅助函数
  const convertPillar = (pillar: { stem: string; branch: string; element: string }) => ({
    stem: pillar.stem as any,
    branch: pillar.branch as any,
    element: pillar.element as WuXing
  });

  return {
    chart: {
      yearPillar: convertPillar(reading.chart.yearPillar),
      monthPillar: convertPillar(reading.chart.monthPillar),
      dayPillar: convertPillar(reading.chart.dayPillar),
      hourPillar: convertPillar(reading.chart.hourPillar),
      dayMaster: reading.chart.dayMaster as any,
      dayMasterElement: reading.chart.dayMasterElement as WuXing,
      wuxingCount: reading.chart.wuxingCount as Record<WuXing, number>,
      wuxingBalance: reading.chart.wuxingBalance || '五行分析中'
    },
    personality: reading.personality,
    career: reading.career,
    relationship: reading.relationship,
    wealth: reading.wealth,
    health: reading.health,
    annualFortune: reading.annualFortune,
    luckyElements: reading.luckyElements as WuXing[],
    luckyColors: reading.luckyColors,
    luckyNumbers: reading.luckyNumbers,
  };
}

async function chatWithBazi(message: string, context: BaziReading): Promise<string> {
  if (!currentBaziSessionId) {
    return '抱歉，无法找到对话上下文，请重新分析八字。';
  }

  const response = await mysticApiRequest<ChatApiResponse>('/bazi/chat', {
    sessionId: currentBaziSessionId,
    message
  });

  return response?.response || '抱歉，无法回答您的问题，请稍后再试。';
}

// 导出服务对象
export const mysticService = {
  analyzeTarot,
  chatWithTarot,
  analyzeAstrology,
  chatWithAstrology,
  analyzeBazi,
  chatWithBazi,
};
