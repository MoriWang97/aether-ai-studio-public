/**
 * 案件管理 Hook
 * 提供案件的创建、保存、加载功能
 * 使用 localStorage 持久化（后续可升级为服务端存储）
 */

import { useState, useCallback, useEffect } from 'react';
import type { LegalCase, CaseType, CaseStatus, Evidence } from '../types';

const STORAGE_KEY = 'legal_assistant_cases';

// 生成唯一ID
const generateId = () => `case_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

// 从本地存储加载案件列表
const loadCasesFromStorage = (): LegalCase[] => {
  try {
    const data = localStorage.getItem(STORAGE_KEY);
    return data ? JSON.parse(data) : [];
  } catch {
    return [];
  }
};

// 保存案件列表到本地存储
const saveCasesToStorage = (cases: LegalCase[]) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(cases));
  } catch (e) {
    console.error('Failed to save cases:', e);
  }
};

export interface UseCaseManagerReturn {
  cases: LegalCase[];
  currentCase: LegalCase | null;
  isLoading: boolean;
  
  // 案件操作
  createCase: (type: CaseType, title: string) => LegalCase;
  loadCase: (id: string) => LegalCase | null;
  updateCase: (id: string, updates: Partial<LegalCase>) => void;
  deleteCase: (id: string) => void;
  setCurrentCase: (caseItem: LegalCase | null) => void;
  
  // 证据操作
  addEvidence: (caseId: string, evidence: Omit<Evidence, 'id' | 'createdAt'>) => Evidence;
  removeEvidence: (caseId: string, evidenceId: string) => void;
  updateEvidence: (caseId: string, evidenceId: string, updates: Partial<Evidence>) => void;
  
  // 状态操作
  updateCaseStatus: (id: string, status: CaseStatus) => void;
}

export const useCaseManager = (): UseCaseManagerReturn => {
  const [cases, setCases] = useState<LegalCase[]>([]);
  const [currentCase, setCurrentCase] = useState<LegalCase | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // 初始化加载
  useEffect(() => {
    const loaded = loadCasesFromStorage();
    setCases(loaded);
    setIsLoading(false);
  }, []);

  // cases 变化时自动保存
  useEffect(() => {
    if (!isLoading) {
      saveCasesToStorage(cases);
    }
  }, [cases, isLoading]);

  // 创建新案件
  const createCase = useCallback((type: CaseType, title: string): LegalCase => {
    const now = new Date();
    const baseCase = {
      id: generateId(),
      type,
      status: 'draft' as CaseStatus,
      title,
      evidences: [],
      createdAt: now,
      updatedAt: now,
    };

    let newCase: LegalCase;
    
    switch (type) {
      case 'divorce':
        newCase = {
          ...baseCase,
          type: 'divorce',
          hasChildren: false,
          assets: [],
        };
        break;
      case 'labor':
        newCase = {
          ...baseCase,
          type: 'labor',
          companyName: '',
          position: '',
          entryDate: '',
          monthlySalary: 0,
          hasContract: false,
          hasSocialInsurance: false,
          violations: [],
          workingYears: 0,
        };
        break;
      case 'rental':
        newCase = {
          ...baseCase,
          type: 'rental',
          address: '',
          monthlyRent: 0,
          deposit: 0,
          contractStartDate: '',
          contractEndDate: '',
          hasWrittenContract: false,
          disputeTypes: [],
          isRegistered: false,
        };
        break;
      default:
        throw new Error(`Unknown case type: ${type}`);
    }

    setCases(prev => [...prev, newCase]);
    setCurrentCase(newCase);
    return newCase;
  }, []);

  // 加载案件
  const loadCase = useCallback((id: string): LegalCase | null => {
    const found = cases.find(c => c.id === id);
    if (found) {
      setCurrentCase(found);
    }
    return found || null;
  }, [cases]);

  // 更新案件
  const updateCase = useCallback((id: string, updates: Partial<LegalCase>) => {
    setCases(prev => prev.map(c => {
      if (c.id === id) {
        const updated = { ...c, ...updates, updatedAt: new Date() } as LegalCase;
        if (currentCase?.id === id) {
          setCurrentCase(updated);
        }
        return updated;
      }
      return c;
    }));
  }, [currentCase]);

  // 删除案件
  const deleteCase = useCallback((id: string) => {
    setCases(prev => prev.filter(c => c.id !== id));
    if (currentCase?.id === id) {
      setCurrentCase(null);
    }
  }, [currentCase]);

  // 添加证据
  const addEvidence = useCallback((caseId: string, evidence: Omit<Evidence, 'id' | 'createdAt'>): Evidence => {
    const newEvidence: Evidence = {
      ...evidence,
      id: `evidence_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      createdAt: new Date(),
    };

    setCases(prev => prev.map(c => {
      if (c.id === caseId) {
        const updated = {
          ...c,
          evidences: [...c.evidences, newEvidence],
          updatedAt: new Date(),
        } as LegalCase;
        if (currentCase?.id === caseId) {
          setCurrentCase(updated);
        }
        return updated;
      }
      return c;
    }));

    return newEvidence;
  }, [currentCase]);

  // 删除证据
  const removeEvidence = useCallback((caseId: string, evidenceId: string) => {
    setCases(prev => prev.map(c => {
      if (c.id === caseId) {
        const updated = {
          ...c,
          evidences: c.evidences.filter(e => e.id !== evidenceId),
          updatedAt: new Date(),
        } as LegalCase;
        if (currentCase?.id === caseId) {
          setCurrentCase(updated);
        }
        return updated;
      }
      return c;
    }));
  }, [currentCase]);

  // 更新证据
  const updateEvidence = useCallback((caseId: string, evidenceId: string, updates: Partial<Evidence>) => {
    setCases(prev => prev.map(c => {
      if (c.id === caseId) {
        const updated = {
          ...c,
          evidences: c.evidences.map(e => 
            e.id === evidenceId ? { ...e, ...updates } : e
          ),
          updatedAt: new Date(),
        } as LegalCase;
        if (currentCase?.id === caseId) {
          setCurrentCase(updated);
        }
        return updated;
      }
      return c;
    }));
  }, [currentCase]);

  // 更新案件状态
  const updateCaseStatus = useCallback((id: string, status: CaseStatus) => {
    updateCase(id, { status });
  }, [updateCase]);

  return {
    cases,
    currentCase,
    isLoading,
    createCase,
    loadCase,
    updateCase,
    deleteCase,
    setCurrentCase,
    addEvidence,
    removeEvidence,
    updateEvidence,
    updateCaseStatus,
  };
};

export default useCaseManager;
