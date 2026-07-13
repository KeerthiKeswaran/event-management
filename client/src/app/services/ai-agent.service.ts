import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ChatMessage {
  role: 'user' | 'assistant' | 'system' | 'tool';
  content: string;
}

export interface ChatRequest {
  messages: ChatMessage[];
}

export interface ChatResponse {
  response?: string;
  status?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AiAgentService {
  private apiUrl = `${environment.apiUrl}/Ai/chat`;

  constructor(private http: HttpClient) {}

  public sendMessage(messages: ChatMessage[]): Observable<ChatResponse> {
    return new Observable<ChatResponse>(observer => {
      const token = localStorage.getItem('user_token') || '';
      // Note: if the token is somewhere else, you can adjust. Assuming it's standard or passed in.
      // Wait, AppStoreService handles token. I will inject it.
      
      const doFetch = async () => {
        try {
          const response = await fetch(this.apiUrl, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'Authorization': `Bearer ${token}` // The interceptor doesn't apply to fetch!
            },
            body: JSON.stringify({ messages })
          });

          if (!response.ok) {
            observer.error('HTTP Error ' + response.status);
            return;
          }

          const reader = response.body?.getReader();
          const decoder = new TextDecoder();
          if (!reader) {
            observer.error('No reader');
            return;
          }

          let buffer = '';
          while (true) {
            const { done, value } = await reader.read();
            if (done) {
              if (buffer.length > 0) {
                // process remaining buffer if any
                if (buffer.startsWith('data: ')) {
                  try {
                    const data = JSON.parse(buffer.substring(6));
                    observer.next(data);
                  } catch (e) {}
                }
              }
              break;
            }
            buffer += decoder.decode(value, { stream: true });
            const lines = buffer.split('\n');
            buffer = lines.pop() || '';
            for (const line of lines) {
              if (line.startsWith('data: ')) {
                try {
                  const data = JSON.parse(line.substring(6));
                  observer.next(data);
                  if (data.response !== undefined) {
                    observer.complete();
                    return;
                  }
                } catch (e) {}
              }
            }
          }
          observer.complete();
        } catch (err) {
          observer.error(err);
        }
      };

      doFetch();
    });
  }

  // --- Session Management (Redis Backend) ---

  public getSessions(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/sessions`);
  }

  public getSession(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/sessions`);
  }

  public saveSession(title: string, messages: any[]): Observable<any> {
    return this.http.post(`${this.apiUrl}/sessions`, { title, messages });
  }

  public deleteSession(): Observable<any> {
    return this.http.delete(`${this.apiUrl}/sessions`);
  }
}
