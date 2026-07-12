import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { JoinWaitlistRequest, WaitlistStatusResponse } from '../models/waitlist.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class WaitlistService {
  private apiUrl = `${environment.apiUrl}/waitlist`;

  constructor(private http: HttpClient) {}

  joinWaitlist(request: JoinWaitlistRequest): Observable<WaitlistStatusResponse> {
    return this.http.post<WaitlistStatusResponse>(this.apiUrl, request);
  }

  getMyWaitlist(): Observable<WaitlistStatusResponse[]> {
    return this.http.get<WaitlistStatusResponse[]>(`${this.apiUrl}/mine`);
  }

  cancelWaitlistEntry(waitlistId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${waitlistId}`);
  }
}
