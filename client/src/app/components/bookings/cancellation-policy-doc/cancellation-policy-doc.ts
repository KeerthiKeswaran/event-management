import { Component, Output, EventEmitter, OnInit, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';


@Component({
  selector: 'app-cancellation-policy-doc',
  standalone: true,
  imports: [],
  templateUrl: './cancellation-policy-doc.html',
  styleUrl: './cancellation-policy-doc.css'
})
export class CancellationPolicyDocComponent implements OnInit {
  /** Emitted when the user closes the policy viewer */
  @Output() closed = new EventEmitter<void>();

  private http = inject(HttpClient);
  public policyContent = signal<string | null>(null);
  public isLoadingPolicy = signal(false);
  public policyError = signal<string>('');

  ngOnInit(): void {
    this.fetchCancellationPolicy();
  }

  private parseMarkdown(content: string): string {
    if (!content) return '';
    
    // Split into lines to filter out policy metadata
    let lines = content.split('\n');
    lines = lines.filter(line => {
      const trimmed = line.trim().toLowerCase();
      return !trimmed.startsWith('**last updated:**') &&
             !trimmed.startsWith('**version:**') &&
             !trimmed.startsWith('**policy id:**') &&
             !trimmed.startsWith('last updated:') &&
             !trimmed.startsWith('version:') &&
             !trimmed.startsWith('policy id:');
    });
    
    let parsedText = lines.join('\n');
    
    // Parse Headers
    parsedText = parsedText
      .replace(/^# (.*?)$/gm, '<h1>$1</h1>')
      .replace(/^## (.*?)$/gm, '<h2>$1</h2>')
      .replace(/^### (.*?)$/gm, '<h3>$1</h3>')
      .replace(/^#### (.*?)$/gm, '<h4>$1</h4>');
      
    // Parse Horizontal Rules (---)
    parsedText = parsedText.replace(/^---$/gm, '<hr class="markdown-hr"/>');
    
    // Parse Bold text (**text**)
    parsedText = parsedText.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    
    // Parse Bullet Lists (* item or - item)
    parsedText = parsedText.replace(/^[\*-]\s+(.*?)$/gm, '<li>$1</li>');
    
    // Wrap adjacent <li> tags in <ul>
    parsedText = parsedText.replace(/(<li>.*?<\/li>[\r\n]*)+/g, '<ul>$&</ul>\n');
    
    // Convert double line breaks into paragraphs
    parsedText = parsedText.split('\n\n').map(p => {
      p = p.trim();
      if (!p) return '';
      if (p.startsWith('<h') || p.startsWith('<hr') || p.startsWith('<ul') || p.startsWith('<li')) {
        return p;
      }
      return `<p>${p}</p>`;
    }).join('\n');
    
    // Convert remaining single line breaks to <br/>
    parsedText = parsedText.replace(/\n/g, '<br/>');
    
    return parsedText;
  }

  private fetchCancellationPolicy(): void {
    this.isLoadingPolicy.set(true);
    this.policyError.set('');

    this.http.get<any>(`${environment.apiUrl}/policies/cancellation`).subscribe({
      next: (res) => {
        const fileUrl = res.filePath.startsWith('http') ? res.filePath : `${environment.serverUrl}${res.filePath}`;
        this.http.get(fileUrl, { responseType: 'text' }).subscribe({
          next: (content) => {
            const formatted = this.parseMarkdown(content);
            this.policyContent.set(formatted);
            this.isLoadingPolicy.set(false);
          },
          error: (err) => {
            console.error('Failed to fetch cancellation policy content', err);
            this.policyError.set('Failed to load policy content.');
            this.isLoadingPolicy.set(false);
          }
        });
      },
      error: (err) => {
        console.error('Failed to fetch cancellation policy metadata', err);
        this.policyError.set('Failed to fetch policy from server.');
        this.isLoadingPolicy.set(false);
      }
    });
  }

  public close(event?: Event): void {
    event?.stopPropagation();
    this.closed.emit();
  }
}
