import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';



export interface AdminAction {
  id: number;
  // Add other properties based on your DTO
  [key: string]: any;
}

export interface Transaction {
  // Add properties based on your DTO
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class FinanceService {
  private http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;
  private apiUrl = `${this.baseUrl}/finance`;
  private authUrl = `${this.baseUrl}/auth/finance`;

  financeLogin(adminId: string, password: string): Observable<any> {
    return this.http.post(`${this.authUrl}/login`, { adminId, password });
  }

  verifyFinanceLoginOtp(adminId: string, otp: string): Observable<any> {
    return this.http.post(`${this.authUrl}/login/verify`, { adminId, otp });
  }

  getAdminActions(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/actions`);
  }

  declineAction(id: number, remarks: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/actions/${id}/decline`, { remarks });
  }

  approveAction(id: number, refundType: string, message: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/actions/${id}/approve`, { refundType, message });
  }

  respondToTicket(id: number, response: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/tickets/${id}/respond`, { response });
  }

  getTransactions(params?: any): Observable<any> {
    let httpParams = new HttpParams();
    if (params) {
      Object.keys(params).forEach(key => {
        if (params[key] !== null && params[key] !== undefined && params[key] !== '') {
          httpParams = httpParams.set(key, params[key]);
        }
      });
    }
    return this.http.get(`${this.apiUrl}/transactions`, { params: httpParams });
  }

  getDashboardStats(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/dashboard-stats`);
  }

  getPayouts(filters: any): Observable<any> {
    let httpParams = new HttpParams();
    if (filters.status && filters.status !== 'All') httpParams = httpParams.set('status', filters.status);
    if (filters.sortBy) httpParams = httpParams.set('sortBy', filters.sortBy);
    if (filters.page) httpParams = httpParams.set('page', filters.page.toString());
    if (filters.size) httpParams = httpParams.set('size', filters.size.toString());
    return this.http.get(`${this.apiUrl}/payouts`, { params: httpParams });
  }

  searchGlobal(keyword: string): Observable<any> {
    let httpParams = new HttpParams().set('keyword', keyword);
    return this.http.get(`${this.apiUrl}/search`, { params: httpParams });
  }
}
