import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { BookingService } from '../../services/booking.service';
import { EventService } from '../../services/event.service';
import { WaitlistService } from '../../services/waitlist.service';
import { BookingModel, BookingDetail } from '../../models/booking.model';
import { WaitlistStatusResponse } from '../../models/waitlist.model';
import { NavbarComponent } from '../home/navbar/navbar';
import { FooterComponent } from '../home/footer/footer';
import { CancelBookingModalComponent } from './cancel-booking-modal/cancel-booking-modal';
import { ReportEventModalComponent } from '../shared/report-event-modal/report-event-modal';
import { environment } from '../../../environments/environment';


type FilterStatus = 'Upcoming' | 'Completed' | 'Cancelled' | 'Waitlist';

@Component({
  selector: 'app-bookings',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, FooterComponent, CancelBookingModalComponent, ReportEventModalComponent],
  templateUrl: './bookings.html',
  styleUrl: './bookings.css'
})
export class BookingsComponent implements OnInit, OnDestroy {
  public bookings = signal<BookingModel[]>([]);
  public waitlists = signal<WaitlistStatusResponse[]>([]);
  public selectedFilter = signal<FilterStatus>('Upcoming');
  public isLoading = signal(false);

  // QR Modal state
  public activeQrBooking = signal<BookingModel | null>(null);
  public activeReportBooking = signal<BookingModel | null>(null);

  // -- Feedback State --
  private feedbackDrafts = signal<Record<number, { rating: number; review: string }>>({});
  public isSubmittingFeedback = signal<number | null>(null);
  public showFeedbackSuccess = signal<number | null>(null);
  public showQrModal = signal(false);

  // Cancellation modal state
  public showCancelModal = signal(false);
  public bookingToCancel = signal<BookingModel | null>(null);




  public virtualLinkMsgBookingId = signal<number | null>(null);
  public cancelledAccessMsgId   = signal<number | null>(null);
  public cancelledAccessMsgType = signal<'qr' | 'virtual' | null>(null);
  public cancellationMsgId = signal<number | null>(null);

  // Report modal state
  public showReportModal = signal(false);
  public activeReportEventId = signal<number>(0);
  public activeReportEventTitle = signal<string>('');

  private subscriptions = new Subscription();

  // Computed signals for filtering
  public filteredBookings = computed(() => {
    let list = this.bookings();
    const filter = this.selectedFilter();
    
    if (filter === 'Upcoming') {
      list = list.filter(b => b.booking_Status === 'Confirmed' && b.event_Status !== 'Completed');
    } else if (filter === 'Completed') {
      list = list.filter(b => b.booking_Status === 'Confirmed' && b.event_Status === 'Completed');
    } else if (filter === 'Cancelled') {
      list = list.filter(b => b.booking_Status === 'Cancelled');
    }

    // Sort newest first
    return [...list].sort((a, b) => new Date(b.created_At).getTime() - new Date(a.created_At).getTime());
  });

  public confirmedCount = computed(() =>
    this.bookings().filter(b => b.booking_Status === 'Confirmed' && b.event_Status !== 'Completed').length
  );

  public completedCount = computed(() =>
    this.bookings().filter(b => b.booking_Status === 'Confirmed' && b.event_Status === 'Completed').length
  );

  public cancelledCount = computed(() =>
    this.bookings().filter(b => b.booking_Status === 'Cancelled').length
  );

  public waitlistCount = computed(() =>
    this.waitlists().length
  );

  constructor(
    private bookingService: BookingService,
    private waitlistService: WaitlistService,
    private eventService: EventService,
    private router: Router
  ) {}

  private resolveImageUrl(url: string | null | undefined): string | undefined {
    if (!url) return undefined;
    if (url.startsWith('http://') || url.startsWith('https://') || url.startsWith('data:')) {
      return url;
    }
    const cleanUrl = url.startsWith('/') ? url : '/' + url;
    return `${environment.blobStorageUrl}${cleanUrl}`;
  }

  ngOnInit(): void {
    this.loadBookings();
  }

  public loadBookings(): void {
    this.isLoading.set(true);
    this.subscriptions.add(
      this.bookingService.getMyBookings().subscribe({
        next: (data) => {
          this.bookingService.getActiveVirtualLinks().subscribe({
            next: (activeLinks) => {
              const updated = data.map(booking => {
                if (booking.event_Type === 'Virtual' || booking.event_Type === 'Hybrid') {
                  const linkObj = activeLinks.find(al => al.booking_Id === booking.booking_Id);
                  booking.virtual_Url = (linkObj && linkObj.virtual_Url !== 'Disabled') 
                    ? linkObj.virtual_Url 
                    : 'Disabled';
                }
                booking.event_Image_Url = this.resolveImageUrl(booking.event_Image_Url);
                booking.qr_Code_Path = this.resolveImageUrl(booking.qr_Code_Path);
                return booking;
              });
              this.bookings.set(updated);
              this.isLoading.set(false);
            },
            error: () => {
              const fallbackMapped = data.map(booking => {
                booking.event_Image_Url = this.resolveImageUrl(booking.event_Image_Url);
                booking.qr_Code_Path = this.resolveImageUrl(booking.qr_Code_Path);
                return booking;
              });
              this.bookings.set(fallbackMapped);
              this.isLoading.set(false);
            }
          });
        },
        error: () => {
          this.isLoading.set(false);
        }
      })
    );

    this.subscriptions.add(
      this.waitlistService.getMyWaitlist().subscribe({
        next: (data) => {
          this.waitlists.set(data);
        },
        error: (err) => console.error(err)
      })
    );
  }

  public setFilter(filter: FilterStatus): void {
    this.selectedFilter.set(filter);
  }

  public cancelWaitlist(waitlistId: number): void {
    if (confirm('Are you sure you want to leave the waitlist?')) {
      this.waitlistService.cancelWaitlistEntry(waitlistId).subscribe({
        next: () => {
          this.waitlists.update(w => w.filter(x => x.waitlist_Id !== waitlistId));
        },
        error: (err) => alert(err.error?.message || 'Failed to cancel')
      });
    }
  }

  // ── QR Modal ────────────────────────────────────────────

  public openQrModal(booking: BookingModel): void {
    this.activeQrBooking.set(booking);
    this.showQrModal.set(true);
  }

  public closeQrModal(): void {
    this.showQrModal.set(false);
    this.activeQrBooking.set(null);
  }

  public downloadQrCode(url: string, bookingId: number | undefined): void {
    if (!url) return;
    const bid = bookingId || 'ticket';
    fetch(url)
      .then(response => response.blob())
      .then(blob => {
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = `Booking_${bid}_QR.png`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
      })
      .catch(err => {
        console.error('Error downloading QR code', err);
        window.open(url, '_blank');
      });
  }

  // ── Cancellation Modal ───────────────────────────────────

  public cancellingBookingId = signal<number | null>(null);
  public cancelledAnimationId = signal<number | null>(null);

  public openCancelModal(booking: BookingModel): void {
    this.bookingToCancel.set(booking);
    this.showCancelModal.set(true);
  }

  public closeCancelModal(): void {
    this.showCancelModal.set(false);
    this.bookingToCancel.set(null);
  }

  public canCancelBooking(booking: BookingModel): boolean {
    if (booking.checkIn_Status === 'CheckedIn' || booking.booking_Status === 'Cancelled') {
      return false;
    }
    
    let dateStr = booking.event_Date_Time;
    if (dateStr && !dateStr.endsWith('Z')) {
      dateStr += 'Z';
    }
    
    const eventTime = new Date(dateStr).getTime();
    const now = Date.now();
    
    // Fallback: if date parsing fails, allow cancellation to prevent hard-blocking the user.
    if (isNaN(eventTime)) {
      return true;
    }

    const oneHour = 60 * 60 * 1000;
    if (eventTime - now <= oneHour) {
      return false;
    }
    return true;
  }

  public showCancellationUnavailableMsg(bookingId: number): void {
    this.cancellationMsgId.set(bookingId);
    setTimeout(() => {
      if (this.cancellationMsgId() === bookingId) {
        this.cancellationMsgId.set(null);
      }
    }, 4000);
  }

  /** Called by CancelBookingModalComponent (cancelled) output after animation */
  public onBookingCancelled(booking: BookingModel): void {
    const updated = this.bookings().map(b =>
      b.booking_Id === booking.booking_Id ? { ...b, booking_Status: 'Cancelled' as const, checkIn_Status: 'Missed' as const, refunded_Amount: booking.refunded_Amount ?? b.refunded_Amount } : b
    );
    this.bookings.set(updated);
  }

  // ── Feedback (Inline) ──────────────────────────────────────────────────

  public getFeedbackDraft(bookingId: number) {
    return this.feedbackDrafts()[bookingId];
  }

  public setFeedbackRating(bookingId: number, rating: number) {
    const current = this.feedbackDrafts();
    this.feedbackDrafts.set({
      ...current,
      [bookingId]: { ...current[bookingId], rating }
    });
  }

  public setFeedbackReview(bookingId: number, review: string) {
    const current = this.feedbackDrafts();
    this.feedbackDrafts.set({
      ...current,
      [bookingId]: { ...current[bookingId], review }
    });
  }

  public submitFeedback(booking: BookingModel) {
    const draft = this.getFeedbackDraft(booking.booking_Id);
    if (!draft || !draft.rating || !draft.review.trim()) return;

    this.isSubmittingFeedback.set(booking.booking_Id);
    
    this.eventService.submitFeedback(booking.event_Id, draft.rating, draft.review).subscribe({
      next: () => {
        this.isSubmittingFeedback.set(null);
        this.showFeedbackSuccess.set(booking.booking_Id);
        
        // Update local booking state so inputs gray out
        this.bookings.update(list => list.map(b => 
          b.booking_Id === booking.booking_Id 
            ? { ...b, feedback_Rating: draft.rating, feedback_Review: draft.review }
            : b
        ));

        // Hide success animation after 2.8 seconds
        setTimeout(() => {
          if (this.showFeedbackSuccess() === booking.booking_Id) {
            this.showFeedbackSuccess.set(null);
          }
        }, 2800);
      },
      error: () => {
        this.isSubmittingFeedback.set(null);
        alert('Failed to submit feedback.');
      }
    });
  }

  public openReportModal(booking: BookingModel): void {
    if (booking.has_Reported === null) {
      this.router.navigate(['/login']);
      return;
    }
    this.activeReportEventId.set(booking.event_Id);
    this.activeReportEventTitle.set(booking.event_Title);
    this.showReportModal.set(true);
  }

  public hasReported(eventId: number): boolean {
    if (typeof window === 'undefined') return false;
    const bk = this.bookings().find(b => b.event_Id === eventId);
    if (bk && bk.has_Reported === true) return true;
    const reported = JSON.parse(localStorage.getItem('reportedEvents') || '[]');
    return reported.includes(eventId);
  }

  // ── Utils ──────────────────────────────────────────────────

  public showCancelledAccessMsg(bookingId: number, type: 'qr' | 'virtual'): void {
    this.cancelledAccessMsgId.set(bookingId);
    this.cancelledAccessMsgType.set(type);
    setTimeout(() => {
      if (this.cancelledAccessMsgId() === bookingId) {
        this.cancelledAccessMsgId.set(null);
        this.cancelledAccessMsgType.set(null);
      }
    }, 3500);
  }

  public joinMeeting(booking: BookingModel): void {
    const url = booking.virtual_Url;
    if (url && url !== 'Disabled') {
      window.open(url, '_blank');
    } else {
      this.virtualLinkMsgBookingId.set(booking.booking_Id);
      setTimeout(() => {
        if (this.virtualLinkMsgBookingId() === booking.booking_Id) {
          this.virtualLinkMsgBookingId.set(null);
        }
      }, 3000);
    }
  }

  public formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-IN', {
      weekday: 'long', year: 'numeric', month: 'long', day: 'numeric'
    });
  }

  public formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString('en-IN', {
      hour: '2-digit', minute: '2-digit', hour12: true
    });
  }

  public formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency', currency: 'INR', maximumFractionDigits: 0
    }).format(amount);
  }

  public getDetailsSummary(details: BookingDetail[]): string {
    return details.map(d => `${d.tier_Name} × ${d.quantity}`).join(', ');
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
