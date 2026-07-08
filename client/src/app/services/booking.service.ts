import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { BookingModel, InitiateBookingRequest, ConfirmBookingRequest, InitiateBookingResponse, ConfirmBookingResponse, ActiveVirtualLinkResponse } from '../models/booking.model';
import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class BookingService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/booking`;

  /**
   * Get all bookings for the authenticated user.
   * API: GET /api/booking?status=<status>
   */
  getMyBookings(status?: string): Observable<BookingModel[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<BookingModel[]>(this.baseUrl, { params });
  }

  /**
   * Initiate a new booking for an event.
   * API: POST /api/booking
   */
  initiateBooking(request: InitiateBookingRequest): Observable<InitiateBookingResponse> {
    return this.http.post<InitiateBookingResponse>(this.baseUrl, request);
  }

  /**
   * Confirm a booking after payment.
   * API: POST /api/booking/{bookingId}/confirm
   */
  confirmBooking(bookingId: number, request: ConfirmBookingRequest): Observable<ConfirmBookingResponse> {
    return this.http.post<ConfirmBookingResponse>(`${this.baseUrl}/${bookingId}/confirm`, request);
  }

  /**
   * Create a Stripe Checkout session.
   * API: POST /api/booking/{bookingId}/create-checkout-session
   */
  createCheckoutSession(bookingId: number, successUrl: string, cancelUrl: string): Observable<{ sessionId: string, clientSecret: string, createdAtUTC: string }> {
    return this.http.post<{ sessionId: string, clientSecret: string, createdAtUTC: string }>(`${this.baseUrl}/${bookingId}/create-checkout-session`, { successUrl, cancelUrl });
  }

  /**
   * Get set of virtual urls for which the event has been started.
   * API: GET /api/booking/active-links
   */
  getActiveVirtualLinks(): Observable<ActiveVirtualLinkResponse[]> {
    return this.http.get<ActiveVirtualLinkResponse[]>(`${this.baseUrl}/active-links`);
  }

  /**
   * Calculate booking and ticket fees.
   * API: POST /api/booking/calculate-fee (calculated locally)
   */
  calculateTicketFee(eventId: number, tierQuantities: Record<string, number>): Observable<{ fee: number }> {
    let count = 0;
    Object.values(tierQuantities).forEach(q => count += q);
    // Platform fee calculation: ₹45 handling fee per ticket
    return of({ fee: count * 45 });
  }

  /**
   * Calculate refund amount.
   * API: GET /api/booking/{bookingId}/refund-estimate
   */
  getRefundEstimate(bookingId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${bookingId}/refund-estimate`);
  }

  /**
   * Cancel a booking.
   * API: POST /api/booking/{bookingId}/cancel
   */
  cancelBooking(bookingId: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${bookingId}/cancel`, {});
  }

  /**
   * Revert a pending booking.
   * API: POST /api/booking/{bookingId}/revert
   */
  revertBooking(bookingId: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${bookingId}/revert`, {});
  }

  /**
   * Submit feedback for an event.
   * API: POST /api/event/{eventId}/feedback
   */
  submitEventFeedback(eventId: number, feedback: { rating: number; review: string }): Observable<any> {
    return this.http.post<any>(`${environment.apiUrl}/event/${eventId}/feedback`, feedback);
  }

  /**
   * Submit support ticket.
   * API: POST /api/support/tickets
   */
  submitSupportTicket(payload: { subject: string; message: string; requestType: string; relatedId?: number; targetType?: string }): Observable<any> {
    return this.http.post<any>(`${environment.apiUrl}/support/tickets`, payload);
  }

  /**
   * Get my support tickets.
   * API: GET /api/support/tickets
   */
  getMySupportTickets(): Observable<any[]> {
    return this.http.get<any[]>(`${environment.apiUrl}/support/tickets`);
  }
}
