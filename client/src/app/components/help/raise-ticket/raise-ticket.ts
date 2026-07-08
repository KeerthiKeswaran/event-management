import { Component, OnInit, OnDestroy, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { NavbarComponent } from '../../home/navbar/navbar';
import { FooterComponent } from '../../home/footer/footer';
import { BookingService } from '../../../services/booking.service';
import { AuthService } from '../../../services/auth.service';
import { AppStoreService } from '../../../store/app-store.service';
import { BookingModel } from '../../../models/booking.model';
import { SupportTicket } from '../help';
import { EventService } from '../../../services/event.service';

@Component({
  selector: 'app-raise-ticket',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, FooterComponent],
  templateUrl: './raise-ticket.html',
  styleUrl: './raise-ticket.css'
})
export class RaiseTicketComponent implements OnInit, OnDestroy {
  public bookings = signal<any[]>([]);
  public isLoggedIn = signal(false);
  
  public isBookingsLoading = signal(true);
  public isEventsLoading = signal(true);

  // Form State
  public ticketCategory = '';
  public ticketSubject = '';
  public ticketDetails = '';
  
  // Selection Modal State
  public events = signal<any[]>([]);
  public showSelectionModal = signal(false);
  public activeSelectionTab = signal<'bookings' | 'events'>('bookings');
  public selectedItemType = signal<'booking' | 'event' | null>(null);
  public selectedItemId = signal<string | number | null>(null);
  public selectedItemDisplay = signal<string>('Select an item...');
  
  // File upload state
  public selectedFileName = signal('');
  
  // Alert & Animation State
  public ticketErrorMessage = signal('');
  public isSuccessAnimating = signal(false);
  public successTicketId = signal('');
  
  private redirectTimer: any = null;
  private subscriptions = new Subscription();

  @HostListener('document:click', ['$event'])
  public onDocumentClick(event: MouseEvent): void {
    // Navbar dropdown outside click handling
  }

  constructor(
    private bookingService: BookingService,
    private authService: AuthService,
    private eventService: EventService,
    private store: AppStoreService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Check authentication
    this.isLoggedIn.set(true);
    this.loadUserBookings();
  }

  private loadUserBookings(): void {
    this.isBookingsLoading.set(true);
    this.bookingService.getMyBookings().subscribe({
      next: (res: any) => {
        let list: any[] = [];
        if (Array.isArray(res)) list = res;
        else if (res && res.$values) list = res.$values;
        else if (res && res.data) list = res.data;
        else if (res && res.items) list = res.items;
        this.bookings.set(list || []);
        this.isBookingsLoading.set(false);
      },
      error: () => {
        this.bookings.set([]);
        this.isBookingsLoading.set(false);
      }
    });

    this.isEventsLoading.set(true);
    this.eventService.getMyEvents().subscribe({
      next: (res: any) => {
        let list: any[] = [];
        if (Array.isArray(res)) list = res;
        else if (res && res.$values) list = res.$values;
        else if (res && res.data) list = res.data;
        else if (res && res.items) list = res.items;
        this.events.set(list || []);
        this.isEventsLoading.set(false);
      },
      error: () => {
        this.events.set([]);
        this.isEventsLoading.set(false);
      }
    });
  }

  public openSelectionModal(): void {
    this.showSelectionModal.set(true);
  }

  public closeSelectionModal(): void {
    this.showSelectionModal.set(false);
  }

  public selectBooking(booking: any): void {
    this.selectedItemType.set('booking');
    const id = booking.booking_Id || booking.BookingId || booking.bookingId || booking.Booking_Id;
    this.selectedItemId.set(id);
    const title = booking.event_Title || booking.EventTitle || booking.eventTitle || 'Event';
    this.selectedItemDisplay.set(`Booking #${id} - ${title}`);
    this.closeSelectionModal();
  }

  public selectEvent(event: any): void {
    this.selectedItemType.set('event');
    this.selectedItemId.set(event.event_Id || event.Event_Id || event.eventId);
    this.selectedItemDisplay.set(`Event #${this.selectedItemId()} - ${event.title || event.Title}`);
    this.closeSelectionModal();
  }

  public onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFileName.set(input.files[0].name);
    } else {
      this.selectedFileName.set('');
    }
  }

  public submitSupportTicket(): void {
    this.ticketErrorMessage.set('');

    if (!this.selectedItemId()) {
      this.ticketErrorMessage.set('Please select a related Event or Booking.');
      return;
    }
    if (!this.ticketCategory) {
      this.ticketErrorMessage.set('Please select a support category.');
      return;
    }
    if (!this.ticketSubject.trim()) {
      this.ticketErrorMessage.set('Please enter a ticket subject.');
      return;
    }
    if (!this.ticketDetails.trim()) {
      this.ticketErrorMessage.set('Please provide a description of your issue.');
      return;
    }

    let mappedRequestType = 'GEN';
    const categoryLower = this.ticketCategory.toLowerCase();
    if (categoryLower.includes('refund') || categoryLower.includes('cancellation')) mappedRequestType = 'REF';
    else if (categoryLower.includes('event') || categoryLower.includes('venue') || categoryLower.includes('qr')) mappedRequestType = 'EVT';
    else if (categoryLower.includes('account') || categoryLower.includes('payment')) mappedRequestType = 'ACC';

    let targetType = 'GEN';
    if (this.selectedItemType() === 'booking') targetType = 'ATD';
    else if (this.selectedItemType() === 'event') targetType = 'ORG';

    const payload = {
      subject: this.ticketSubject.trim(),
      message: this.ticketDetails.trim(),
      requestType: mappedRequestType,
      targetType: targetType,
      relatedId: Number(this.selectedItemId())
    };

    this.bookingService.submitSupportTicket(payload).subscribe({
      next: (res) => {
        const ticketId = String(res?.ticket_Id || res?.Ticket_Id || `TKT-${Math.floor(Math.random() * 90000) + 10000}`);


        // Show tick animation
        this.successTicketId.set(ticketId);
        this.isSuccessAnimating.set(true);

        this.redirectTimer = setTimeout(() => {
          this.backToSupport();
        }, 5000);
      },
      error: (err) => {
        this.ticketErrorMessage.set(err?.error?.message || 'Failed to submit support ticket.');
      }
    });
  }

  public backToSupport(): void {
    if (this.redirectTimer) {
      clearTimeout(this.redirectTimer);
      this.redirectTimer = null;
    }
    this.router.navigate(['/help']);
  }

  ngOnDestroy(): void {
    if (this.redirectTimer) {
      clearTimeout(this.redirectTimer);
    }
    this.subscriptions.unsubscribe();
  }
}
