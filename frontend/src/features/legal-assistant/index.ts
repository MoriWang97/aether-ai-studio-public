// 法律助手模块导出

// Types
export * from './types';

// Hooks
export { useCaseManager } from './hooks/useCaseManager';
export { useEvidenceCollector } from './hooks/useEvidenceCollector';

// Services
export { LegalAssistantService, createLegalAssistant } from './services/legalAssistantService';

// Components
export { default as LegalAssistantHub } from './components/LegalAssistantHub';
export { default as DivorceAssistant } from './components/DivorceAssistant';
export { default as LaborAssistant } from './components/LaborAssistant';
export { default as RentalAssistant } from './components/RentalAssistant';
export { default as EvidencePanel } from './components/EvidencePanel';
export { default as AnalysisResult } from './components/AnalysisResult';
