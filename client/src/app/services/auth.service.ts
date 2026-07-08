import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, of, throwError } from 'rxjs';
import { AppStoreService } from '../store/app-store.service';
import { ActionTypes } from '../store/actions/app.actions';
import { UserModel, AuthResponse } from '../models/user.model';
import { mockUser, mockLoginResponse, mockRegisterResponse } from '../data/auth.mock';
import { ConsentDocument, mockTermsAndConditions, mockDataConsent } from '../data/consent.mock';
import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly baseUrl = environment.apiUrl;

  private getTokenKey(): string {
    if (typeof window === 'undefined') return 'user_token';
    const path = window.location.pathname;
    if (path.startsWith('/admin')) return 'admin_token';
    if (path.startsWith('/finance')) return 'finance_token';
    return 'user_token';
  }

  constructor(
    private http: HttpClient,
    private store: AppStoreService,
    private router: Router
  ) {
    // If token exists, load profile automatically on startup
    const tokenKey = this.getTokenKey();
    const token = typeof window !== 'undefined' ? localStorage.getItem(tokenKey) : null;
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload.role;
        if (role !== 'admin' && role !== 'finance') {
          this.loadProfile().subscribe();
        } else {
          // It's an admin/finance token, just restore login state without fetching profile
          this.store.dispatch({
            type: ActionTypes.LOGIN_SUCCESS,
            payload: { token: token, user: null }
          });
        }
      } catch (e) {
        this.loadProfile().subscribe();
      }
    }
  }

  public login(email: string, password: string): Observable<AuthResponse> {
    this.store.dispatch({ type: ActionTypes.LOGIN_START });
    
    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/user/login`, { email, password }).pipe(
      tap((res) => {
        if (res.token) {
          localStorage.setItem('user_token', res.token);
          this.store.dispatch({
            type: ActionTypes.LOGIN_SUCCESS,
            payload: { token: res.token, user: null }
          });
          this.loadProfile().subscribe();
        }
      }),
      catchError((err) => {
        const errMsg = err.error?.message || 'Login failed. Please check your credentials.';
        this.store.dispatch({ type: ActionTypes.LOGIN_FAIL, payload: errMsg });
        return throwError(() => err);
      })
    );
  }

  public adminLogin(adminId: string, password: string): Observable<AuthResponse> {
    this.store.dispatch({ type: ActionTypes.LOGIN_START });

    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/admin/login`, { adminId, password }).pipe(
      tap((res: any) => {
        const token = res.token || res.Token;
        if (token) {
          localStorage.setItem(this.getTokenKey(), token);
          this.store.dispatch({
            type: ActionTypes.LOGIN_SUCCESS,
            payload: { token: token, user: null }
          });
        }
      }),
      catchError((err) => {
        const errMsg = err.error?.message || 'Admin login failed. Please verify your credentials.';
        this.store.dispatch({ type: ActionTypes.LOGIN_FAIL, payload: errMsg });
        return throwError(() => err);
      })
    );
  }

  public register(payload: any): Observable<AuthResponse> {
    this.store.dispatch({ type: ActionTypes.LOGIN_START });
    
    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/user/register`, payload).pipe(
      tap((res) => {
        if (res.token) {
          localStorage.setItem('user_token', res.token);
          this.store.dispatch({
            type: ActionTypes.LOGIN_SUCCESS,
            payload: { token: res.token, user: null }
          });
          this.loadProfile().subscribe();
        }
      }),
      catchError((err) => {
        const errMsg = err.error?.message || 'Registration failed.';
        this.store.dispatch({ type: ActionTypes.LOGIN_FAIL, payload: errMsg });
        return throwError(() => err);
      })
    );
  }

  public sendOtp(email: string, purpose: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/user/send-otp`, { email, purpose });
  }

  public verifyOtp(email: string, otp: string, purpose: string = 'registration'): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/user/verify-otp`, { email, otp, purpose });
  }

  public resetUserPassword(payload: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/user/reset-password`, payload);
  }

  public checkEmail(email: string): Observable<{ exists: boolean }> {
    return this.http.get<{ exists: boolean }>(`${this.baseUrl}/auth/user/check-email?email=${encodeURIComponent(email)}`);
  }

  public updateProfile(payload: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/user/profile`, payload);
  }

  public sendAdminOtp(email: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/forgot-password`, { email });
  }

  public resetAdminPassword(payload: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/reset-password`, payload);
  }

  public resetPassword(payload: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/auth/user/reset-password`, payload);
  }

  public loadProfile(): Observable<UserModel> {
    this.store.dispatch({ type: ActionTypes.LOAD_USER_PROFILE });
    
    return this.http.get<UserModel>(`${this.baseUrl}/user/profile`).pipe(
      tap((user) => {
        this.store.dispatch({
          type: ActionTypes.LOAD_USER_PROFILE_SUCCESS,
          payload: user
        });
        if (user.interested_Region_Id) {
          this.store.dispatch({
            type: ActionTypes.SET_REGION,
            payload: user.interested_Region_Id
          });
          localStorage.setItem('currentRegionId', user.interested_Region_Id);
        }
      }),
      catchError((err) => {
        const errMsg = err.error?.message || 'Failed to load user profile.';
        this.store.dispatch({ type: ActionTypes.LOAD_USER_PROFILE_FAIL, payload: errMsg });
        
        // Only log out if it's an actual authentication error (401/403)
        if (err.status === 401 || err.status === 403) {
          this.logout();
        }
        
        // Always redirect to error page for any profile load failure, passing the status code
        const currentUrl = window.location.pathname + window.location.search;
        this.router.navigate(['/error'], { queryParams: { code: err.status || 500, returnUrl: currentUrl } });
        
        return throwError(() => err);
      })
    );
  }

  public selectRegion(regionId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/user/select-regions`, { regionId }).pipe(
      tap((res: any) => {
        this.store.dispatch({
          type: ActionTypes.SET_REGION,
          payload: regionId
        });
        if (typeof window !== 'undefined') {
          localStorage.setItem('currentRegionId', regionId);
        }
        const user = this.store.state.auth.user;
        if (user) {
          this.store.dispatch({
            type: ActionTypes.LOAD_USER_PROFILE_SUCCESS,
            payload: { ...user, interested_Region_Id: regionId }
          });
        }
      })
    );
  }

  public logout(): void {
    if (typeof window !== 'undefined') {
      localStorage.removeItem(this.getTokenKey());
    }
    this.store.dispatch({ type: ActionTypes.LOGOUT });
  }

  /**
   * Retrieves a consent or terms document by type.
   * API: GET /api/policies/{type}
   */
  public getConsentDocument(type: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/policies/${type}`);
  }
}

