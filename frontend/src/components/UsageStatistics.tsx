import React, { useState, useEffect, useCallback } from 'react';
import {
  getStatisticsOverview,
  getUsageLogs,
  getTodayStatistics,
  getThisWeekStatistics,
  getThisMonthStatistics,
  UsageStatisticsOverview,
  UsageLogListResponse,
  StatisticsQuery,
  StatisticsGroupBy,
  FeatureModule,
  ModuleNames,
  ModuleUsageStats,
  TimeTrendStats,
  UserActivityStats,
  UsageLogItem
} from '../services/adminService';
import './UsageStatistics.css';

type QuickRange = 'today' | 'week' | 'month' | 'custom';
type ViewMode = 'overview' | 'logs';

const UsageStatistics: React.FC = () => {
  // 状态
  const [viewMode, setViewMode] = useState<ViewMode>('overview');
  const [quickRange, setQuickRange] = useState<QuickRange>('today');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // 筛选条件
  const [startDate, setStartDate] = useState<string>('');
  const [endDate, setEndDate] = useState<string>('');
  const [selectedModule, setSelectedModule] = useState<FeatureModule | undefined>(undefined);
  const [groupBy, setGroupBy] = useState<StatisticsGroupBy>(StatisticsGroupBy.Day);
  
  // 数据
  const [overview, setOverview] = useState<UsageStatisticsOverview | null>(null);
  const [logsData, setLogsData] = useState<UsageLogListResponse | null>(null);
  const [logsPage, setLogsPage] = useState(1);

  // 加载概览数据
  const loadOverview = useCallback(async () => {
    setLoading(true);
    setError(null);
    
    try {
      let result: UsageStatisticsOverview;
      
      if (quickRange === 'today') {
        result = await getTodayStatistics();
      } else if (quickRange === 'week') {
        result = await getThisWeekStatistics();
      } else if (quickRange === 'month') {
        result = await getThisMonthStatistics();
      } else {
        const query: StatisticsQuery = {
          startDate: startDate || undefined,
          endDate: endDate || undefined,
          module: selectedModule,
          groupBy
        };
        result = await getStatisticsOverview(query);
      }
      
      if (result.success) {
        setOverview(result);
      } else {
        setError(result.error || '加载统计数据失败');
      }
    } catch (err) {
      setError('加载统计数据失败');
    } finally {
      setLoading(false);
    }
  }, [quickRange, startDate, endDate, selectedModule, groupBy]);

  // 加载日志数据
  const loadLogs = useCallback(async (page: number = 1) => {
    setLoading(true);
    setError(null);
    
    try {
      const query: StatisticsQuery = {
        startDate: startDate || undefined,
        endDate: endDate || undefined,
        module: selectedModule,
        page,
        pageSize: 20
      };
      
      const result = await getUsageLogs(query);
      
      if (result.success) {
        setLogsData(result);
        setLogsPage(page);
      } else {
        setError(result.error || '加载日志失败');
      }
    } catch (err) {
      setError('加载日志失败');
    } finally {
      setLoading(false);
    }
  }, [startDate, endDate, selectedModule]);

  // 初始加载
  useEffect(() => {
    if (viewMode === 'overview') {
      loadOverview();
    } else {
      loadLogs(1);
    }
  }, [viewMode]); // eslint-disable-line react-hooks/exhaustive-deps

  // 快捷时间范围变化时重新加载
  useEffect(() => {
    if (viewMode === 'overview' && quickRange !== 'custom') {
      loadOverview();
    }
  }, [quickRange]); // eslint-disable-line react-hooks/exhaustive-deps

  // 格式化数字
  const formatNumber = (num: number): string => {
    if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M';
    if (num >= 1000) return (num / 1000).toFixed(1) + 'K';
    return num.toString();
  };

  // 格式化时间
  const formatTime = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleString('zh-CN', {
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  // 获取模块图标
  const getModuleIcon = (module: FeatureModule): string => {
    const icons: Record<FeatureModule, string> = {
      [FeatureModule.Chat]: '💬',
      [FeatureModule.Image]: '🎨',
      [FeatureModule.Speech]: '🎤',
      [FeatureModule.Legal]: '⚖️',
      [FeatureModule.Mystic]: '🔮',
      [FeatureModule.RagChat]: '📚',
      [FeatureModule.Admin]: '🔧',
      [FeatureModule.Other]: '📦'
    };
    return icons[module] || '📦';
  };

  // 获取模块颜色
  const getModuleColor = (module: FeatureModule): string => {
    const colors: Record<FeatureModule, string> = {
      [FeatureModule.Chat]: '#667eea',      // 蓝紫色
      [FeatureModule.Image]: '#f59e0b',     // 橙色
      [FeatureModule.Speech]: '#10b981',    // 绿色
      [FeatureModule.Legal]: '#3b82f6',     // 蓝色
      [FeatureModule.Mystic]: '#8b5cf6',    // 紫色
      [FeatureModule.RagChat]: '#06b6d4',   // 青色
      [FeatureModule.Admin]: '#6b7280',     // 灰色
      [FeatureModule.Other]: '#9ca3af'      // 浅灰
    };
    return colors[module] || '#9ca3af';
  };

  // 主要功能模块（按优先级排序）
  const mainModules: FeatureModule[] = [
    FeatureModule.Chat,
    FeatureModule.Image,
    FeatureModule.Mystic,
    FeatureModule.Legal,
    FeatureModule.Speech,
    FeatureModule.RagChat
  ];

  // 渲染统计卡片
  const renderStatCard = (title: string, value: string | number, subtitle?: string, trend?: 'up' | 'down' | 'neutral') => (
    <div className="stat-card">
      <div className="stat-value">{typeof value === 'number' ? formatNumber(value) : value}</div>
      <div className="stat-title">{title}</div>
      {subtitle && <div className={`stat-subtitle ${trend || ''}`}>{subtitle}</div>}
    </div>
  );

  // 渲染模块使用统计 - Tab卡片形式
  const renderModuleStats = (stats: ModuleUsageStats[]) => {
    // 按主要模块排序，并补全没有数据的模块
    const sortedStats = mainModules.map(module => {
      const existing = stats.find(s => s.module === module);
      if (existing) return existing;
      // 补全没有数据的模块
      return {
        module,
        moduleName: ModuleNames[module] || module.toString(),
        requestCount: 0,
        successCount: 0,
        uniqueUsers: 0,
        averageResponseTimeMs: 0,
        percentage: 0
      };
    });

    // 找出最大请求数（用于计算相对高度）
    const maxCount = Math.max(...sortedStats.map(s => s.requestCount), 1);

    return (
      <div className="module-stats">
        <h4>📊 功能模块使用分布</h4>
        <div className="module-tabs">
          {sortedStats.map(stat => {
            const color = getModuleColor(stat.module);
            const heightPercent = (stat.requestCount / maxCount) * 100;
            
            return (
              <div 
                key={stat.module} 
                className={`module-tab ${stat.requestCount === 0 ? 'empty' : ''}`}
                style={{ '--module-color': color } as React.CSSProperties}
              >
                <div className="module-tab-header">
                  <span className="module-tab-icon">{getModuleIcon(stat.module)}</span>
                  <span className="module-tab-name">{stat.moduleName}</span>
                </div>
                <div className="module-tab-bar-container">
                  <div 
                    className="module-tab-bar" 
                    style={{ 
                      height: `${Math.max(heightPercent, 4)}%`,
                      background: `linear-gradient(180deg, ${color} 0%, ${color}88 100%)`
                    }}
                  />
                </div>
                <div className="module-tab-stats">
                  <div className="module-tab-count">{formatNumber(stat.requestCount)}</div>
                  <div className="module-tab-label">次调用</div>
                </div>
                {stat.requestCount > 0 && (
                  <div className="module-tab-details">
                    <span>{stat.percentage.toFixed(1)}%</span>
                    <span>{stat.uniqueUsers} 用户</span>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>
    );
  };

  // 渲染趋势图表（简单的柱状图）
  const renderTrendChart = (trends: TimeTrendStats[]) => {
    if (trends.length === 0) return <div className="empty-hint">暂无趋势数据</div>;
    
    const maxCount = Math.max(...trends.map(t => t.requestCount), 1);
    
    return (
      <div className="trend-chart">
        <h4>📈 使用趋势</h4>
        <div className="chart-container">
          {trends.map((trend, index) => (
            <div key={index} className="chart-bar-container">
              <div 
                className="chart-bar" 
                style={{ height: `${(trend.requestCount / maxCount) * 100}%` }}
                title={`${trend.periodLabel}: ${trend.requestCount} 次请求, ${trend.activeUsers} 活跃用户`}
              >
                <span className="chart-value">{formatNumber(trend.requestCount)}</span>
              </div>
              <div className="chart-label">{trend.periodLabel.split(' ').pop()}</div>
            </div>
          ))}
        </div>
      </div>
    );
  };

  // 渲染活跃用户排行
  const renderTopUsers = (users: UserActivityStats[]) => (
    <div className="top-users">
      <h4>🏆 活跃用户排行</h4>
      {users.length === 0 ? (
        <div className="empty-hint">暂无数据</div>
      ) : (
        <div className="users-list">
          {users.map((user, index) => (
            <div key={user.userId} className="user-item">
              <div className="user-rank">#{index + 1}</div>
              <div className="user-info">
                <div className="user-email">{user.email}</div>
                {user.nickname && <div className="user-nickname">{user.nickname}</div>}
              </div>
              <div className="user-stats">
                <span className="user-requests">{formatNumber(user.totalRequests)} 次</span>
                <span className="user-module">{getModuleIcon(user.mostUsedModule)}</span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );

  // 渲染日志列表
  const renderLogs = (logs: UsageLogItem[]) => (
    <div className="logs-table-container">
      <table className="logs-table">
        <thead>
          <tr>
            <th>时间</th>
            <th>用户</th>
            <th>模块</th>
            <th>操作</th>
            <th>状态</th>
            <th>耗时</th>
          </tr>
        </thead>
        <tbody>
          {logs.map(log => (
            <tr key={log.id} className={log.isSuccess ? 'success' : 'failed'}>
              <td>{formatTime(log.timestamp)}</td>
              <td>
                <div className="log-user">
                  <span>{log.userEmail || log.userId}</span>
                  {log.userNickname && <small>{log.userNickname}</small>}
                </div>
              </td>
              <td>
                <span className="log-module">
                  {getModuleIcon(log.module)} {log.moduleName}
                </span>
              </td>
              <td className="log-action">{log.action}</td>
              <td>
                <span className={`status-badge ${log.isSuccess ? 'success' : 'failed'}`}>
                  {log.isSuccess ? '✓' : '✗'} {log.statusCode || '-'}
                </span>
              </td>
              <td>{log.responseTimeMs ? `${log.responseTimeMs}ms` : '-'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );

  // 渲染分页
  const renderPagination = () => {
    if (!logsData || logsData.totalPages <= 1) return null;
    
    return (
      <div className="pagination">
        <button 
          onClick={() => loadLogs(logsPage - 1)} 
          disabled={logsPage <= 1 || loading}
        >
          上一页
        </button>
        <span className="page-info">
          第 {logsPage} / {logsData.totalPages} 页 (共 {logsData.totalCount} 条)
        </span>
        <button 
          onClick={() => loadLogs(logsPage + 1)} 
          disabled={logsPage >= logsData.totalPages || loading}
        >
          下一页
        </button>
      </div>
    );
  };

  return (
    <div className="usage-statistics">
      {/* 顶部控制栏 */}
      <div className="stats-controls">
        {/* 视图切换 */}
        <div className="view-toggle">
          <button 
            className={viewMode === 'overview' ? 'active' : ''} 
            onClick={() => setViewMode('overview')}
          >
            📊 概览
          </button>
          <button 
            className={viewMode === 'logs' ? 'active' : ''} 
            onClick={() => setViewMode('logs')}
          >
            📋 日志
          </button>
        </div>

        {/* 快捷时间选择（仅概览模式） */}
        {viewMode === 'overview' && (
          <div className="quick-range">
            <button 
              className={quickRange === 'today' ? 'active' : ''} 
              onClick={() => setQuickRange('today')}
            >
              今日
            </button>
            <button 
              className={quickRange === 'week' ? 'active' : ''} 
              onClick={() => setQuickRange('week')}
            >
              本周
            </button>
            <button 
              className={quickRange === 'month' ? 'active' : ''} 
              onClick={() => setQuickRange('month')}
            >
              本月
            </button>
            <button 
              className={quickRange === 'custom' ? 'active' : ''} 
              onClick={() => setQuickRange('custom')}
            >
              自定义
            </button>
          </div>
        )}

        {/* 自定义筛选（自定义模式或日志模式） */}
        {(quickRange === 'custom' || viewMode === 'logs') && (
          <div className="custom-filters">
            <div className="filter-row">
              <label>
                开始:
                <input 
                  type="date" 
                  value={startDate} 
                  onChange={e => setStartDate(e.target.value)}
                />
              </label>
              <label>
                结束:
                <input 
                  type="date" 
                  value={endDate} 
                  onChange={e => setEndDate(e.target.value)}
                />
              </label>
              <label>
                模块:
                <select 
                  value={selectedModule ?? ''} 
                  onChange={e => setSelectedModule(e.target.value ? Number(e.target.value) as FeatureModule : undefined)}
                >
                  <option value="">全部</option>
                  {Object.entries(ModuleNames).map(([key, name]) => (
                    <option key={key} value={key}>{name}</option>
                  ))}
                </select>
              </label>
              {viewMode === 'overview' && (
                <label>
                  分组:
                  <select 
                    value={groupBy} 
                    onChange={e => setGroupBy(e.target.value as StatisticsGroupBy)}
                  >
                    <option value={StatisticsGroupBy.Hour}>按小时</option>
                    <option value={StatisticsGroupBy.Day}>按天</option>
                    <option value={StatisticsGroupBy.Week}>按周</option>
                    <option value={StatisticsGroupBy.Month}>按月</option>
                  </select>
                </label>
              )}
              <button 
                className="apply-filter-btn" 
                onClick={() => viewMode === 'overview' ? loadOverview() : loadLogs(1)}
                disabled={loading}
              >
                🔍 查询
              </button>
            </div>
          </div>
        )}
      </div>

      {/* 加载/错误状态 */}
      {loading && (
        <div className="loading-state">
          <span className="spinner"></span>
          加载中...
        </div>
      )}

      {error && (
        <div className="error-state">
          ⚠️ {error}
          <button onClick={() => viewMode === 'overview' ? loadOverview() : loadLogs(logsPage)}>
            重试
          </button>
        </div>
      )}

      {/* 概览视图 */}
      {viewMode === 'overview' && !loading && overview && (
        <div className="overview-content">
          {/* 核心指标卡片 */}
          <div className="stat-cards">
            {renderStatCard('总请求数', overview.totalRequests)}
            {renderStatCard('成功请求', overview.successfulRequests, 
              overview.totalRequests > 0 
                ? `成功率 ${((overview.successfulRequests / overview.totalRequests) * 100).toFixed(1)}%` 
                : undefined
            )}
            {renderStatCard('活跃用户', overview.activeUsers)}
            {renderStatCard('平均响应', `${overview.averageResponseTimeMs.toFixed(0)}ms`)}
          </div>

          {/* 功能模块分布 - 独占一行 */}
          <div className="stats-section module-section">
            {renderModuleStats(overview.moduleStats)}
          </div>

          {/* 趋势和用户排行 */}
          <div className="stats-grid">
            <div className="stats-section">
              {renderTrendChart(overview.trendStats)}
            </div>
            <div className="stats-section">
              {renderTopUsers(overview.topActiveUsers)}
            </div>
          </div>
        </div>
      )}

      {/* 日志视图 */}
      {viewMode === 'logs' && !loading && logsData && (
        <div className="logs-content">
          {logsData.logs.length === 0 ? (
            <div className="empty-state">暂无日志数据</div>
          ) : (
            <>
              {renderLogs(logsData.logs)}
              {renderPagination()}
            </>
          )}
        </div>
      )}
    </div>
  );
};

export default UsageStatistics;
