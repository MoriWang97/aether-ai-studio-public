import React, { useState, useRef } from 'react';
import { generateImage, ImageGenerationResponse } from '../services/api';
import { useAuth } from '../contexts/AuthContext';
import { triggerQuotaRefresh } from '../contexts/QuotaContext';
import ApprovalStatus from './ApprovalStatus';
import './ImageGenerator.css';

const ImageGenerator: React.FC = () => {
  const { isLoggedIn, user, refreshUser } = useAuth();
  const [prompt, setPrompt] = useState('');
  const [uploadedImages, setUploadedImages] = useState<string[]>([]);
  const [result, setResult] = useState<ImageGenerationResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // 检查用户是否已被批准
  const isApproved = user?.isApproved || user?.isAdmin;

  const handleImageUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = event.target.files;
    if (files && files.length > 0) {
      const newImages: string[] = [];
      const maxImages = 5;
      const remainingSlots = maxImages - uploadedImages.length;
      const filesToProcess = Array.from(files).slice(0, remainingSlots);

      let processed = 0;
      filesToProcess.forEach((file) => {
        const reader = new FileReader();
        reader.onloadend = () => {
          const base64 = reader.result as string;
          newImages.push(base64);
          processed++;
          if (processed === filesToProcess.length) {
            setUploadedImages((prev) => [...prev, ...newImages].slice(0, maxImages));
          }
        };
        reader.readAsDataURL(file);
      });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!prompt.trim()) {
      alert('请输入提示词');
      return;
    }

    // 检查是否已登录
    if (!isLoggedIn) {
      setResult({
        success: false,
        error: '请先登录后再使用图像生成功能',
      });
      return;
    }

    // 检查是否已被批准
    if (!isApproved) {
      setResult({
        success: false,
        error: '您的账户尚未获得使用权限，请先申请并等待管理员审批',
      });
      return;
    }

    setLoading(true);
    setResult(null);

    try {
      const response = await generateImage({
        prompt: prompt.trim(),
        images: uploadedImages.length > 0 ? uploadedImages : undefined,
      });
      setResult(response);
      // 刷新使用额度显示
      if (response.success) {
        triggerQuotaRefresh();
      }
    } catch (error) {
      setResult({
        success: false,
        error: '请求失败，请稍后重试',
      });
    } finally {
      setLoading(false);
    }
  };

  const clearUploadedImages = () => {
    setUploadedImages([]);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const removeImage = (index: number) => {
    setUploadedImages((prev) => prev.filter((_, i) => i !== index));
  };

  return (
    <div className="image-generator">
      <div className="generator-header">
        <div className="generator-title">
          <h2>图像生成</h2>
          <span className="generator-badge">AI 创作</span>
        </div>
        <div className="generator-actions">
          <button 
            className="clear-button"
            onClick={() => {
              setPrompt('');
              clearUploadedImages();
              setResult(null);
            }}
            disabled={!prompt && uploadedImages.length === 0 && !result}
            title="清空内容"
          >
            🗑️
          </button>
        </div>
      </div>

      <div className="generator-content">
        {/* 未登录或未批准提示 */}
        {(!isLoggedIn || !isApproved) && (
          <div className="permission-notice">
            {!isLoggedIn ? (
              <p>🔐 请先登录后使用图像生成功能</p>
            ) : (
              <>
                <h3>🔒 需要使用权限</h3>
                <p>您需要申请并获得管理员批准后才能使用 AI 功能</p>
                <ApprovalStatus onStatusChange={refreshUser} />
              </>
            )}
          </div>
        )}

        <form onSubmit={handleSubmit} className="generator-form">
          <div className="input-group">
            <label htmlFor="prompt">描述你想要的图像：</label>
            <textarea
              id="prompt"
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
              placeholder="例如：一只可爱的猫咪在阳光下睡觉..."
              rows={4}
              disabled={loading || !isApproved}
            />
          </div>

          <div className="input-group">
            <label>上传参考图片（可选，最多5张）：</label>
            <div className="file-upload-area" onClick={() => !loading && uploadedImages.length < 5 && fileInputRef.current?.click()}>
              <input
                type="file"
                accept="image/*"
                multiple
                onChange={handleImageUpload}
                ref={fileInputRef}
                disabled={loading || uploadedImages.length >= 5}
                style={{ display: 'none' }}
              />
              <span className="upload-icon">📁</span>
              <span className="upload-text">点击选择图片或拖拽到此处</span>
            </div>
            {uploadedImages.length > 0 && (
              <div className="uploaded-preview-container">
                <div className="uploaded-preview-grid">
                  {uploadedImages.map((img, index) => (
                    <div key={index} className="uploaded-preview-item">
                      <img src={img} alt={`Uploaded ${index + 1}`} />
                      <button
                        type="button"
                        onClick={() => removeImage(index)}
                        className="remove-btn"
                      >
                        ✕
                      </button>
                    </div>
                  ))}
                </div>
                <button type="button" onClick={clearUploadedImages} className="clear-all-btn">
                  清除所有图片
                </button>
              </div>
            )}
          </div>

          <button type="submit" disabled={loading || !prompt.trim()} className="submit-btn">
            {loading ? '生成中...' : '🚀 生成图像'}
          </button>
        </form>

        {loading && (
          <div className="loading">
            <div className="spinner"></div>
            <p>AI 正在创作中，请稍候...</p>
          </div>
        )}

        {result && (
          <div className={`result ${result.success ? 'success' : 'error'}`}>
            {result.success ? (
              <div className="result-image">
                <h3>✨ 生成结果</h3>
                {result.imageUrl && (
                  <img src={result.imageUrl} alt="Generated" />
                )}
                {result.imageBase64 && (
                  <img src={result.imageBase64} alt="Generated" />
                )}
              </div>
            ) : (
              <div className="error-message">
                <h3>❌ 生成失败</h3>
                <p>{result.error}</p>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default ImageGenerator;
