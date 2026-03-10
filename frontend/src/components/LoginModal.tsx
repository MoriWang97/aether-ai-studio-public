import React, { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { sendVerificationCode } from '../services/authService';
import './LoginModal.css';

interface LoginModalProps {
  isOpen: boolean;
  onClose: () => void;
}

const LoginModal: React.FC<LoginModalProps> = ({ isOpen, onClose }) => {
  const { login, register, isLoading } = useAuth();
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [nickname, setNickname] = useState('');
  const [verificationCode, setVerificationCode] = useState('');
  const [error, setError] = useState<string>('');
  const [countdown, setCountdown] = useState(0);
  const [isSendingCode, setIsSendingCode] = useState(false);

  const validateEmail = (email: string): boolean => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  };

  // 倒计时效果
  useEffect(() => {
    if (countdown > 0) {
      const timer = setTimeout(() => setCountdown(countdown - 1), 1000);
      return () => clearTimeout(timer);
    }
  }, [countdown]);

  // 发送验证码
  const handleSendCode = async () => {
    if (!validateEmail(email)) {
      setError('请输入有效的邮箱地址');
      return;
    }

    setIsSendingCode(true);
    setError('');

    try {
      const result = await sendVerificationCode(email);
      if (result.success) {
        setCountdown(result.cooldownSeconds || 60);
      } else {
        setError(result.error || '发送验证码失败');
        if (result.cooldownSeconds) {
          setCountdown(result.cooldownSeconds);
        }
      }
    } finally {
      setIsSendingCode(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    // 验证邮箱格式
    if (!validateEmail(email)) {
      setError('请输入有效的邮箱地址');
      return;
    }

    // 验证密码长度
    if (password.length < 6) {
      setError('密码至少需要6个字符');
      return;
    }

    if (mode === 'register') {
      // 验证确认密码
      if (password !== confirmPassword) {
        setError('两次输入的密码不一致');
        return;
      }

      // 验证验证码
      if (!verificationCode || verificationCode.length !== 6) {
        setError('请输入6位验证码');
        return;
      }

      const success = await register(email, password, verificationCode, nickname || undefined);
      if (success) {
        onClose();
        resetForm();
      } else {
        setError('注册失败，验证码可能无效或已过期');
      }
    } else {
      const success = await login(email, password);
      if (success) {
        onClose();
        resetForm();
      } else {
        setError('邮箱或密码错误');
      }
    }
  };

  const resetForm = () => {
    setEmail('');
    setPassword('');
    setConfirmPassword('');
    setNickname('');
    setVerificationCode('');
    setError('');
    setCountdown(0);
  };

  const switchMode = () => {
    setMode(mode === 'login' ? 'register' : 'login');
    setError('');
  };

  if (!isOpen) return null;

  return (
    <div className="login-modal-overlay" onClick={onClose}>
      <div className="login-modal" onClick={e => e.stopPropagation()}>
        <button className="close-button" onClick={onClose}>✕</button>
        
        <div className="login-header">
          <h2>🔐 {mode === 'login' ? '登录' : '注册'}</h2>
          <p className="login-subtitle">
            {mode === 'login' 
              ? '登录后可保存聊天历史记录' 
              : '创建账号以保存您的聊天记录'}
          </p>
        </div>

        <form className="login-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="email">邮箱</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              placeholder="请输入邮箱地址"
              disabled={isLoading}
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">密码</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="请输入密码（至少6位）"
              disabled={isLoading}
              required
              minLength={6}
            />
          </div>

          {mode === 'register' && (
            <>
              <div className="form-group">
                <label htmlFor="confirmPassword">确认密码</label>
                <input
                  id="confirmPassword"
                  type="password"
                  value={confirmPassword}
                  onChange={e => setConfirmPassword(e.target.value)}
                  placeholder="请再次输入密码"
                  disabled={isLoading}
                  required
                  minLength={6}
                />
              </div>

              <div className="form-group">
                <label htmlFor="verificationCode">验证码</label>
                <div className="verification-code-row">
                  <input
                    id="verificationCode"
                    type="text"
                    value={verificationCode}
                    onChange={e => setVerificationCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                    placeholder="请输入6位验证码"
                    disabled={isLoading}
                    required
                    maxLength={6}
                  />
                  <button
                    type="button"
                    className="send-code-button"
                    onClick={handleSendCode}
                    disabled={isLoading || isSendingCode || countdown > 0 || !validateEmail(email)}
                  >
                    {isSendingCode ? '发送中...' : countdown > 0 ? `${countdown}秒后重发` : '发送验证码'}
                  </button>
                </div>
              </div>

              <div className="form-group">
                <label htmlFor="nickname">昵称（可选）</label>
                <input
                  id="nickname"
                  type="text"
                  value={nickname}
                  onChange={e => setNickname(e.target.value)}
                  placeholder="输入昵称或留空使用默认"
                  disabled={isLoading}
                />
              </div>
            </>
          )}

          {error && <div className="error-message">{error}</div>}

          <button 
            type="submit"
            className="login-button"
            disabled={isLoading}
          >
            {isLoading 
              ? (mode === 'login' ? '登录中...' : '注册中...') 
              : (mode === 'login' ? '登录' : '注册')}
          </button>
        </form>

        <div className="login-switch">
          <span>
            {mode === 'login' ? '还没有账号？' : '已有账号？'}
          </span>
          <button 
            type="button"
            className="switch-button"
            onClick={switchMode}
            disabled={isLoading}
          >
            {mode === 'login' ? '立即注册' : '去登录'}
          </button>
        </div>

        <div className="login-footer">
          <p className="privacy-note">
            登录即表示您同意我们的服务条款和隐私政策
          </p>
        </div>
      </div>
    </div>
  );
};

export default LoginModal;
