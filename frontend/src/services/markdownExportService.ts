/**
 * Strategy Pattern: Markdown Export Strategy
 *
 * Encapsulates the logic for formatting chat messages into Markdown
 * and triggering a file download. Designed for extensibility —
 * additional export formats (PDF, HTML) can be added as new strategies.
 *
 * @see https://refactoring.guru/design-patterns/strategy
 */

export interface ExportableMessage {
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
  sources?: { title?: string; url: string; snippet?: string }[];
  usedWebSearch?: boolean;
}

/** Abstract export strategy interface */
export interface ExportStrategy {
  export(messages: ExportableMessage[], title?: string): void;
}

/**
 * Concrete Strategy: Markdown file export.
 * Converts selected assistant messages into a well-formatted .md file.
 */
export class MarkdownExportStrategy implements ExportStrategy {
  export(messages: ExportableMessage[], title?: string): void {
    const markdown = this.buildMarkdown(messages, title);
    this.downloadFile(markdown, this.buildFilename(title));
  }

  private buildMarkdown(messages: ExportableMessage[], title?: string): string {
    const lines: string[] = [];

    // Document header
    lines.push(`# ${title || '智能助手对话导出'}`);
    lines.push('');
    lines.push(`> 导出时间：${new Date().toLocaleString('zh-CN')}`);
    lines.push(`> 共 ${messages.length} 条回复`);
    lines.push('');
    lines.push('---');
    lines.push('');

    // Each selected message
    messages.forEach((msg, index) => {
      lines.push(`## 回复 ${index + 1}`);
      lines.push('');
      lines.push(`🕐 *${msg.timestamp.toLocaleString('zh-CN')}*`);
      if (msg.usedWebSearch) {
        lines.push(' 🌐 *已联网搜索*');
      }
      lines.push('');
      lines.push(msg.content);
      lines.push('');

      // Append source references if present
      if (msg.sources && msg.sources.length > 0) {
        lines.push('### 📚 参考来源');
        lines.push('');
        msg.sources.forEach((source, idx) => {
          const title = source.title || source.url;
          lines.push(`${idx + 1}. [${title}](${source.url})`);
          if (source.snippet) {
            lines.push(`   > ${source.snippet}`);
          }
        });
        lines.push('');
      }

      lines.push('---');
      lines.push('');
    });

    return lines.join('\n');
  }

  private buildFilename(title?: string): string {
    const dateStr = new Date().toISOString().slice(0, 10);
    const safeName = (title || '对话导出')
      .replace(/[<>:"/\\|?*]/g, '_')
      .slice(0, 50);
    return `${safeName}_${dateStr}.md`;
  }

  private downloadFile(content: string, filename: string): void {
    const blob = new Blob([content], { type: 'text/markdown;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }
}

/** Default singleton instance for convenience */
const markdownExportService = new MarkdownExportStrategy();
export default markdownExportService;
