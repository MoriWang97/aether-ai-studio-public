// 应用配置
export interface AppConfig {
  apiBaseUrl: string;
}

// 默认配置
const defaultConfig: AppConfig = {
  apiBaseUrl: 'http://localhost:5205',
};

// 缓存配置
let cachedConfig: AppConfig | null = null;

// 加载配置（从 public/config.json）
export const loadConfig = async (): Promise<AppConfig> => {
  if (cachedConfig) {
    return cachedConfig;
  }

  try {
    const response = await fetch('/config.json');
    if (response.ok) {
      const config = await response.json();
      const merged: AppConfig = {
        ...defaultConfig,
        ...config,
      };
      // 移除末尾斜杠
      merged.apiBaseUrl = merged.apiBaseUrl.replace(/\/$/, '');
      cachedConfig = merged;
      return cachedConfig;
    }
  } catch (error) {
    console.warn('无法加载配置文件，使用默认配置:', error);
  }

  cachedConfig = defaultConfig;
  return cachedConfig;
};

// 同步获取配置（需要先调用 loadConfig）
export const getConfig = (): AppConfig => {
  if (!cachedConfig) {
    console.warn('配置尚未加载，返回默认配置');
    return defaultConfig;
  }
  return cachedConfig;
};

// 获取 API 基础 URL
export const getApiBaseUrl = (): string => {
  return getConfig().apiBaseUrl;
};
