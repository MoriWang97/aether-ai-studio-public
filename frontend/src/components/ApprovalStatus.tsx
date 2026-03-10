import React from 'react';

interface ApprovalStatusProps {
  onStatusChange?: () => void;
}

/**
 * 审批状态组件 - 已弃用
 * 所有用户注册后自动获得已批准状态，此组件现在始终返回 null
 * 保留此组件是为了向后兼容，避免修改所有引用此组件的文件
 */
const ApprovalStatus: React.FC<ApprovalStatusProps> = () => {
  // 由于所有用户注册后自动获得已批准状态，
  // 此组件不再需要显示任何内容
  return null;
};

export default ApprovalStatus;
