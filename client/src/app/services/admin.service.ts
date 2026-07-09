import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  public getDashboardStats(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/stats`);
  }

  public getAdminEvents(params?: {
    keyword?: string;
    eventType?: string;
    status?: string;
    startDate?: string;
    endDate?: string;
    sortBy?: string;
    page?: number;
    size?: number;
  }): Observable<any> {
    let requestParams = new HttpParams();
    if (params?.keyword) requestParams = requestParams.set('keyword', params.keyword);
    if (params?.eventType) requestParams = requestParams.set('eventType', params.eventType);
    if (params?.status) requestParams = requestParams.set('status', params.status);
    if (params?.startDate) requestParams = requestParams.set('startDate', params.startDate);
    if (params?.endDate) requestParams = requestParams.set('endDate', params.endDate);
    if (params?.sortBy) requestParams = requestParams.set('sortBy', params.sortBy);
    if (params?.page) requestParams = requestParams.set('page', String(params.page));
    if (params?.size) requestParams = requestParams.set('size', String(params.size));
    return this.http.get<any>(`${this.baseUrl}/admin/events`, { params: requestParams });
  }

  public getEventReports(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/reports`);
  }

  public upholdEventReport(reportId: number, payload: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/admin/reports/${reportId}/uphold`, payload);
  }

  public dismissEventReport(reportId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/admin/reports/${reportId}/dismiss`, {});
  }

  public getSupportTickets(params?: {
    status?: string;
    keyword?: string;
    dateFrom?: string;
    dateTo?: string;
  }): Observable<any> {
    let requestParams = new HttpParams();
    if (params?.status) requestParams = requestParams.set('status', params.status);
    if (params?.keyword) requestParams = requestParams.set('keyword', params.keyword);
    if (params?.dateFrom) requestParams = requestParams.set('dateFrom', params.dateFrom);
    if (params?.dateTo) requestParams = requestParams.set('dateTo', params.dateTo);
    return this.http.get<any>(`${this.baseUrl}/admin/support/tickets`, { params: requestParams });
  }

  public getHelpdeskMetadata(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/support/metadata`);
  }

  public respondToTicket(ticketId: number, response: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/admin/support/tickets/${ticketId}/respond`, { response });
  }

  public escalateTicket(ticketId: number, payload: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/admin/support/tickets/${ticketId}/escalate`, payload);
  }

  public getEscalationStatus(ticketId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/support/tickets/${ticketId}/escalation-status`);
  }

  public getRegions(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/regions`);
  }

  public getVenues(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/venues`);
  }

  public searchVenues(name: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/venues/search?name=${encodeURIComponent(name)}`);
  }

  public createVenue(payload: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/admin/venues`, payload);
  }

  public updateVenue(venueId: number, payload: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/admin/venues/${venueId}`, payload);
  }

  public getStaffDirectory(params?: {
    regionId?: string;
    isAllocated?: boolean;
    page?: number;
    size?: number;
  }): Observable<any> {
    let requestParams = new HttpParams();
    if (params?.regionId) requestParams = requestParams.set('regionId', params.regionId);
    if (params?.isAllocated !== undefined) requestParams = requestParams.set('isAllocated', String(params.isAllocated));
    if (params?.page) requestParams = requestParams.set('page', String(params.page));
    if (params?.size) requestParams = requestParams.set('size', String(params.size));
    return this.http.get<any>(`${this.baseUrl}/admin/staff`, { params: requestParams });
  }

  public getStaffByRegion(regionId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/staff/by-region/${regionId}`);
  }

  public getEventsByRegion(regionId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/events/by-region/${regionId}`);
  }

  public allocateStaff(eventId: number, employeeId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/admin/events/${eventId}/allocate-staff`, { employeeId });
  }

  public getAdminProfile(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/profile`);
  }

  public updateAdminProfile(payload: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/admin/profile`, payload);
  }

  public getAllVenuesIncludingInactive(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/venues/all`);
  }

  public updateEventVenue(eventId: number, venueId: number): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/admin/events/${eventId}/venue`, { venueId });
  }

  public getAdminEventById(eventId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/events/${eventId}`);
  }

  public getRelatedEntity(type: string, id: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/related-entity/${type}/${id}`);
  }

  public searchGlobal(keyword: string): Observable<any> {
    let requestParams = new HttpParams().set('keyword', keyword);
    return this.http.get<any>(`${this.baseUrl}/admin/search`, { params: requestParams });
  }

  public getUsers(
    keyword?: string,
    status?: string,
    startDate?: string,
    endDate?: string,
    sortBy?: string,
    page: number = 1,
    size: number = 10
  ): Observable<any> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('size', size.toString());

    if (keyword) params = params.set('keyword', keyword);
    if (status && status !== 'All Status') params = params.set('status', status);
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    if (sortBy) params = params.set('sortBy', sortBy);

    return this.http.get<any>(`${this.baseUrl}/admin/users`, { params });
  }

  public updateUserStatus(userId: number, status: string): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/admin/users/${userId}/status`, { status });
  }
}
