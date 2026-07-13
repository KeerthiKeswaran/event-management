import { Component, signal, inject } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { filter } from 'rxjs/operators';
import { environment } from '../environments/environment';
import { ChatbotComponent } from './components/shared/chatbot/chatbot.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ChatbotComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('client');
  private router = inject(Router);
  private isInitialNavigation = true;
  private previousPathname = '';
  private http = inject(HttpClient);
  private healthCheckInterval: any;

  constructor() {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      if (typeof window === 'undefined') {
        return;
      }
      
      const currentPathname = window.location.pathname;
      const pathChanged = currentPathname !== this.previousPathname;
      this.previousPathname = currentPathname;

      // On initial navigation (first route match on page load/reload), skip manual scroll restoration
      if (this.isInitialNavigation) {
        this.isInitialNavigation = false;
        return;
      }

      // Scroll to top only if the user navigated to a different url path (a new page)
      if (pathChanged) {
        window.scrollTo(0, 0);
      }
    });

    if (typeof window !== 'undefined') {
      this.startHealthCheck();
    }
  }

  private startHealthCheck() {
    this.performHealthCheck();
    // Poll the server every 2 seconds to ensure it's still alive
    this.healthCheckInterval = setInterval(() => {
      this.performHealthCheck();
    }, 5000);
  }

  private performHealthCheck() {
    // Don't poll if we are already on the error page
    if (this.router.url.startsWith('/error')) return;
    
    this.http.get(`${environment.apiUrl}/Health`).subscribe({
      error: (err) => {
        if (err.status === 0 || err.status >= 500) {
          const currentUrl = window.location.pathname + window.location.search;
          this.router.navigate(['/error'], { queryParams: { code: err.status || 500, returnUrl: currentUrl } });
        }
      }
    });
  }

  public showChatbot(): boolean {
    // Hide chatbot on error pages
    return !this.router.url.startsWith('/error');
  }
}
