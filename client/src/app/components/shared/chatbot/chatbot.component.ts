import { Component, OnInit, ViewChild, ElementRef, OnDestroy, ChangeDetectorRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AiAgentService, ChatMessage } from '../../../services/ai-agent.service';
import { AppStoreService } from '../../../store/app-store.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chatbot.html',
  styleUrl: './chatbot.css'
})
export class ChatbotComponent implements OnInit, OnDestroy {
  public isOpen = false;
  public userMessage = '';
  public messages: ChatMessage[] = [];
  public isProcessing = false;
  public hasUser = false;
  public suggestions = ['Find music events', 'Cancel my booking', 'Check my tickets', 'Raise a support ticket'];
  public isPeeking = true;
  public loadingStatus = '';

  // Redis Session states
  private userMessageSub: Subscription | undefined;
  @ViewChild('chatScrollContainer') private scrollContainer!: ElementRef;
  public isFetchingHistory = true;

  constructor(
    private aiAgent: AiAgentService,
    private store: AppStoreService,
    private router: Router,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit() {
    setTimeout(() => {
      this.isPeeking = false;
      this.cd.detectChanges();
    }, 10000);

    // Removed eager loadSessions() to prevent 401 errors for unauthenticated users on page load

    this.authSub = this.store.select((state: any) => state.auth.user).subscribe((user: any) => {
      this.hasUser = !!user;
      if (this.hasUser && this.messages.length === 0 && !this.isFetchingHistory) {
        this.messages.push({
          role: 'assistant',
          content: 'Hi there! I am your Event Platform Assistant. How can I help you today?'
        });
      }
    });
  }

  public loadSessions() {
    this.isFetchingHistory = true;
    this.aiAgent.getSession().subscribe({
      next: (sessionData) => {
        this.isFetchingHistory = false;
        if (sessionData && sessionData.messages && sessionData.messages.length > 0) {
          this.messages = sessionData.messages;
          this.scrollToBottom();
        } else if (this.hasUser && this.messages.length === 0) {
          // Initialize empty chat
          this.messages.push({
            role: 'assistant',
            content: 'Hi there! I am your Event Platform Assistant. How can I help you today?'
          });
        }
        this.cd.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load chat session', err);
        this.isFetchingHistory = false;
        this.cd.detectChanges();
      }
    });
  }

  private saveChat(): void {
    this.aiAgent.saveSession('Event Assistant', this.messages).subscribe({
      error: (err) => console.error('Failed to save chat', err)
    });
  }

  private authSub!: Subscription;

  ngOnDestroy() {
    if (this.authSub) this.authSub.unsubscribe();
  }

  public toggleChat() {
    if (!this.hasUser) {
      this.router.navigate(['/login']);
      return;
    }
    this.isOpen = !this.isOpen;
    this.isPeeking = false;

    if (this.isOpen && this.messages.length === 0) {
      this.loadSessions();
    }
    if (this.isOpen) {
      setTimeout(() => this.scrollToBottom(), 100);
    }
  }

  public sendMessage() {
    const text = this.userMessage.trim();
    if (!text) return;
    this.send(text);
  }

  public sendSuggestion(text: string) {
    if (this.isProcessing) return;
    this.send(text);
  }

  private send(text: string) {
    this.messages.push({ role: 'user', content: text });
    this.saveChat();
    this.userMessage = '';
    this.resetTextareaHeight();
    this.isProcessing = true;
    this.cd.detectChanges();
    this.scrollToBottom();

    // Send context to the backend
    const context = this.messages.filter(m => m.role !== 'system');
    
    this.aiAgent.sendMessage(context).subscribe({
      next: (res) => {
        if (res.status) {
          this.loadingStatus = res.status;
          this.cd.detectChanges();
          this.scrollToBottom();
        }
        if (res.error) {
          this.messages.push({ role: 'assistant', content: `Error: ${res.error}` });
          this.saveChat();
          this.isProcessing = false;
          this.loadingStatus = '';
          this.cd.detectChanges();
          this.scrollToLatestResponse();
          return;
        }
        if (res.response !== undefined) {
          if (res.response.trim().length > 0) {
            const formatted = this.formatMessage(res.response);
            this.messages.push({ role: 'assistant', content: formatted });
          }
          this.saveChat();
          this.isProcessing = false;
          this.loadingStatus = '';
          this.cd.detectChanges();
          this.scrollToLatestResponse();
        }
      },
      error: (err) => {
        console.error('Chat error', err);
        this.messages.push({ role: 'assistant', content: 'Sorry, I am having trouble connecting right now.' });
        this.saveChat();
        this.isProcessing = false;
        this.loadingStatus = '';
        this.cd.detectChanges();
        this.scrollToLatestResponse();
      }
    });
  }

  public handleKeyPress(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      if (this.userMessage.trim()) {
        this.sendMessage();
      }
    }
  }

  public adjustTextareaHeight(event: Event) {
    const textarea = event.target as HTMLTextAreaElement;
    textarea.style.height = 'auto'; // Reset height to calculate new scrollHeight
    // Max 4 lines (approx 20px per line + padding -> ~96px)
    textarea.style.height = Math.min(textarea.scrollHeight, 96) + 'px';
  }

  private resetTextareaHeight() {
    // Need to use query selector or ViewChild. Let's rely on standard binding or query.
    const textarea = document.querySelector('.chatbot-input textarea') as HTMLTextAreaElement;
    if (textarea) {
      textarea.style.height = 'auto';
    }
  }

  private formatMessage(text: string): string {
    if (!text) return '';
    let formatted = text;
    // Replace plain text asterisks (* Item) with HTML bullets, ignoring markdown bold (**Bold**)
    formatted = formatted.replace(/(^|[^\*])\*(?!\*)([^\*\n]+)/g, (match, p1, p2) => {
      return `${p1}<br>&bull; ${p2.trim()} `;
    });
    
    // Clean up if the message starts with an unnecessary <br>
    if (formatted.trim().startsWith('<br>')) {
      formatted = formatted.replace(/^(?:\s*<br>\s*)+/, '');
    }
    
    return formatted;
  }


  @HostListener('click', ['$event'])
  public onClick(event: Event) {
    const target = event.target as HTMLElement;
    
    // Intercept SELECT button clicks
    if (target.tagName === 'A' && target.classList.contains('event-select-btn')) {
      const bookingName = target.getAttribute('title');
      if (bookingName) {
        // Disable all the buttons in that response list
        const messageBubble = target.closest('.message-bubble');
        if (messageBubble) {
          const buttons = messageBubble.querySelectorAll('.event-select-btn');
          buttons.forEach((btn: any) => {
            btn.classList.add('disabled-btn');
          });
        }
        
        this.send(bookingName);
      }
      return;
    }

    if (target.tagName === 'A' && target.hasAttribute('href')) {
      const href = target.getAttribute('href');
      if (href && href.startsWith('/')) {
        event.preventDefault();
        const urlTree = this.router.parseUrl(href);
        this.router.navigateByUrl(urlTree);
        this.isOpen = false; // Close the chatbot when navigating away
      }
    }
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      try {
        if (this.scrollContainer) {
          const el = this.scrollContainer.nativeElement;
          el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' });
        }
      } catch (err) {}
    }, 100);
  }

  private scrollToLatestResponse(): void {
    setTimeout(() => {
      try {
        if (this.scrollContainer) {
          const el = this.scrollContainer.nativeElement;
          const wrappers = el.querySelectorAll('.message-wrapper');
          if (wrappers.length > 0) {
            const lastMessage = wrappers[wrappers.length - 1] as HTMLElement;
            el.scrollTo({ top: lastMessage.offsetTop - 16, behavior: 'smooth' });
          }
        }
      } catch (err) {}
    }, 100);
  }

  public handleLinkClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    if (target.tagName === 'A') {
      const href = target.getAttribute('href');
      if (href && href.startsWith('/')) {
        event.preventDefault();
        this.router.navigateByUrl(href);
        this.isOpen = false;
      }
    }
  }
}
