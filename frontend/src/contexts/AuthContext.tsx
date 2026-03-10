import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { 
  UserInfo, 
  authStorage, 
  emailLogin,
  emailRegister,
  logout as authLogout,
  getCurrentUser 
} from '../services/authService';

interface AuthContextType {
  user: UserInfo | null;
  isLoggedIn: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<boolean>;
  register: (email: string, password: string, verificationCode: string, nickname?: string) => Promise<boolean>;
  logout: () => void;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

interface AuthProviderProps {
  children: React.ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<UserInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // 刷新用户信息（从服务器获取最新状态）
  const refreshUser = useCallback(async () => {
    if (authStorage.isLoggedIn()) {
      const result = await getCurrentUser();
      if (result.success && result.user) {
        setUser(result.user);
        authStorage.setUser(result.user);
      }
    }
  }, []);

  // 初始化时检查登录状态
  useEffect(() => {
    const initAuth = async () => {
      if (authStorage.isLoggedIn()) {
        const savedUser = authStorage.getUser();
        if (savedUser) {
          setUser(savedUser);
        } else {
          // 尝试从服务器获取用户信息
          const result = await getCurrentUser();
          if (result.success && result.user) {
            setUser(result.user);
            authStorage.setUser(result.user);
          } else {
            // token无效，清除登录状态
            authStorage.clear();
          }
        }
      }
      setIsLoading(false);
    };

    initAuth();
  }, []);

  // 监听其他标签页的登录状态变化
  useEffect(() => {
    const handleStorageChange = (event: StorageEvent) => {
      // 只关注用户信息的变化
      if (event.key === 'user_info') {
        const newUserStr = event.newValue;
        const currentUserId = user?.id;
        
        if (newUserStr) {
          try {
            const newUser = JSON.parse(newUserStr) as UserInfo;
            // 检测到不同用户登录
            if (newUser.id !== currentUserId) {
              // 另一个标签页登录了不同账号，同步当前页面
              setUser(newUser);
            }
          } catch {
            // JSON解析失败，忽略
          }
        } else {
          // 用户信息被清除（登出），当前页面也登出
          setUser(null);
        }
      }
      
      // 监听 token 清除（登出）
      if (event.key === 'auth_token' && !event.newValue) {
        setUser(null);
      }
    };

    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
  }, [user?.id]);

  // 邮箱登录
  const login = useCallback(async (email: string, password: string): Promise<boolean> => {
    setIsLoading(true);
    try {
      const result = await emailLogin(email, password);
      if (result.success && result.user) {
        setUser(result.user);
        return true;
      }
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  // 邮箱注册（带验证码）
  const register = useCallback(async (email: string, password: string, verificationCode: string, nickname?: string): Promise<boolean> => {
    setIsLoading(true);
    try {
      const result = await emailRegister(email, password, verificationCode, nickname);
      if (result.success && result.user) {
        setUser(result.user);
        return true;
      }
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  // 登出
  const logout = useCallback(() => {
    authLogout();
    setUser(null);
  }, []);

  const value: AuthContextType = {
    user,
    isLoggedIn: !!user,
    isLoading,
    login,
    register,
    logout,
    refreshUser
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};
