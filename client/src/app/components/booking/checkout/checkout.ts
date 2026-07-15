import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription, Observable } from 'rxjs';
import { NavbarComponent } from '../../home/navbar/navbar';
import { FooterComponent } from '../../home/footer/footer';
import { BookingService } from '../../../services/booking.service';
import { EventService } from '../../../services/event.service';
import { TicketTierSelection, BookingModel } from '../../../models/booking.model';
import { BrowsedEventResponse } from '../../../models/event.model';
import { mockAllEvents } from '../../../data/event.mock';
import { environment } from '../../../../environments/environment';


type CheckoutStep = 'payment' | 'confirmation';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, NavbarComponent, FooterComponent],
  templateUrl: './checkout.html',
  styleUrl: './checkout.css'
})
export class CheckoutComponent implements OnInit, OnDestroy {
  public currentStep = signal<CheckoutStep>('payment');
  public event = signal<BrowsedEventResponse | null>(null);
  public tiers = signal<TicketTierSelection[]>([]);
  public isProcessing = signal(false);
  public confirmedBooking = signal<BookingModel | null>(null);
  public showQrModal = signal(false);

  // Review Modal & Revert API states
  public showReviewModal = signal(true);
  public isInitiatingBooking = signal(false);
  public pendingBookingId: number | null = null;
  public isConfirmed = false;

  // Success animation state
  public isSuccessTickAnimating = signal(false);

  public paymentError = signal('');
  public cardNameError = signal('');

  // Ticket Fee states
  public ticketFee = signal(0);
  public isFeeLoading = signal(false);

  private subscriptions = new Subscription();

  public subtotalAmount = computed(() =>
    this.tiers().reduce((sum, t) => sum + t.price * t.quantity, 0)
  );

  public gstPercentage = signal(18);

  public gstAmount = computed(() => Math.round((this.subtotalAmount() + this.ticketFee()) * (this.gstPercentage() / 100)));

  public totalAmount = computed(() =>
    this.subtotalAmount() + this.ticketFee() + this.gstAmount()
  );

  public totalTickets = computed(() =>
    this.tiers().reduce((sum, t) => sum + t.quantity, 0)
  );

  // Invoice calculations for confirmation page
  public invoiceSubtotal = computed(() => {
    const booking = this.confirmedBooking();
    if (!booking) return 0;
    return booking.details.reduce((sum, d) => sum + (d.price * d.quantity), 0);
  });

  public invoiceFee = computed(() => {
    const booking = this.confirmedBooking();
    if (!booking) return 0;
    const subtotal = this.invoiceSubtotal();
    const total = booking.total_Amount;
    const subPlusFee = total / (1 + this.gstPercentage() / 100);
    return Math.max(0, Math.round(subPlusFee - subtotal));
  });

  public invoiceGst = computed(() => {
    const booking = this.confirmedBooking();
    if (!booking) return 0;
    return booking.total_Amount - this.invoiceSubtotal() - this.invoiceFee();
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private bookingService: BookingService,
    private eventService: EventService
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
    this.subscriptions.add(
      this.eventService.getPlatformSettings().subscribe({
        next: (res) => {
          if (res) this.gstPercentage.set(res.gsT_Percentage ?? res.GST_Percentage ?? 18);
        }
      })
    );

    this.subscriptions.add(
      this.route.queryParams.subscribe(params => {
        const eventId = Number(params['eventId']);
        const qtyStr = params['quantities'] || '{}';
        const sessionId = params['session_id'];
        const bookingId = Number(params['bookingId']);
        
        if (this.currentStep() === 'confirmation') {
          return;
        }

        if (!eventId) {
          this.router.navigate(['/browse']);
          return;
        }

        // Try history state first, then mock data
        const stateEvent = history.state?.event as BrowsedEventResponse;
        let found = stateEvent && Number(stateEvent.event_Id) === eventId ? stateEvent : null;

        if (!found) {
          found = mockAllEvents.find(e => e.event_Id === eventId) ?? null;
        }

        if (found) {
          this.handleEventFound(found, sessionId, bookingId, qtyStr, eventId);
        } else {
          // If not in state or mock, fetch from API
          this.subscriptions.add(
            this.eventService.getEventById(eventId).subscribe({
              next: (apiEvent) => {
                this.handleEventFound(apiEvent, sessionId, bookingId, qtyStr, eventId);
              },
              error: () => {
                this.router.navigate(['/browse']);
              }
            })
          );
        }
      })
    );
  }

  private handleEventFound(found: BrowsedEventResponse, sessionId: string, bookingId: number, qtyStr: string, eventId: number): void {
    this.event.set(found);

    if (sessionId && bookingId) {
      this.pendingBookingId = bookingId;
      this.confirmStripeCheckout(sessionId, found);
      return;
    }

    try {
      const quantities = JSON.parse(qtyStr);
      const selectedTiers: TicketTierSelection[] = [];
      
      Object.keys(quantities).forEach(tierName => {
        const qty = quantities[tierName];
        if (qty > 0) {
          const tierData = found.ticketTiers?.find(t => t.tier_Name === tierName);
          const price = tierData?.price ?? (found.minPrice ?? 250);
          
          selectedTiers.push({
            tierName,
            price,
            quantity: qty,
            availableSeats: 50,
            totalSeats: 50
          });
        }
      });

      this.tiers.set(selectedTiers);
      
      if (selectedTiers.length === 0) {
        this.router.navigate(['/booking'], { queryParams: { eventId } });
        return;
      }

      this.calculateFee(eventId, quantities);
    } catch {
      this.router.navigate(['/booking'], { queryParams: { eventId } });
    }
  }

  private calculateFee(eventId: number, tierQuantities: Record<string, number>): void {
    this.isFeeLoading.set(true);
    this.ticketFee.set(0);

    this.subscriptions.add(
      this.bookingService.calculateTicketFee(eventId, tierQuantities).subscribe({
        next: (res) => {
          this.ticketFee.set(res.fee);
          setTimeout(() => {
            this.isFeeLoading.set(false);
          }, 1000);
        },
        error: () => {
          this.ticketFee.set(0);
          this.isFeeLoading.set(false);
        }
      })
    );
  }


  private async confirmStripeCheckout(sessionId: string, event: BrowsedEventResponse): Promise<void> {
    this.isProcessing.set(true);
    this.currentStep.set('confirmation');
    this.isSuccessTickAnimating.set(true); // Show animation while processing
    
    try {
      const confirmed = await new Promise<BookingModel>((resolve, reject) => {
        this.bookingService.confirmBooking(this.pendingBookingId!, {
          stripeChargeId: sessionId,
          paymentMethod: 'stripe_checkout'
        }).subscribe({
          next: (res) => {
            const booking: BookingModel = {
              booking_Id: res.booking_Id,
              attendee_Id: res.attendee_Id,
              event_Id: event.event_Id,
              event_Title: event.title,
              event_Type: event.event_Type as any,
              event_Date_Time: event.date_Time,
              event_Image_Url: this.resolveImageUrl(event.image_Url),
              event_Venue: event.venue_Name,
              event_Region: event.venue_Region_Name,
              booking_Status: 'Confirmed',
              qr_Code_Path: this.resolveImageUrl(res.qr_Code_Path),
              checkIn_Status: 'Pending',
              created_At: new Date().toISOString(),
              virtual_Url: res.virtual_Url,
              total_Amount: res.total_Amount ?? 0, // Fallback since totalAmount isn't available from form
              details: (res.details ?? []).map((d: any) => ({
                tier_Name: d.tier_Name,
                quantity: d.quantity,
                price: d.price || event.ticketTiers?.find((t: any) => t.tier_Name === d.tier_Name)?.price || (event.minPrice || 250)
              }))
            };
            resolve(booking);
          },
          error: reject
        });
      });

      this.confirmedBooking.set(confirmed);

      // Clear URL params so refresh doesn't re-trigger confirmation
      this.router.navigate([], {
        queryParams: { session_id: null, bookingId: null },
        queryParamsHandling: 'merge',
        replaceUrl: true
      });
      
      // Stop animation after a brief delay
      setTimeout(() => {
        this.isSuccessTickAnimating.set(false);
      }, 1500);
      
    } catch (err: any) {
      this.isProcessing.set(false);
      this.isSuccessTickAnimating.set(false);
      this.currentStep.set('payment'); // Go back if failed
      
      const msg = err.error?.Message || err.message || '';
      if (msg.includes('already') || msg.includes('Confirmed')) {
        this.router.navigate(['/bookings']);
      } else {
        this.paymentError.set(msg || 'Payment confirmation failed.');
      }
    }
  }

  /**
   * Called when confirming the review modal.
   * Creates the pending booking reservation in the database and shows payment screen.
   */
  public onConfirmReview(): void {
    const event = this.event();
    if (!event) return;

    this.isInitiatingBooking.set(true);

    const tierQuantities: Record<string, number> = {};
    this.tiers().forEach(t => {
      tierQuantities[t.tierName] = t.quantity;
    });

    this.bookingService.initiateBooking({
      eventId: event.event_Id,
      tierQuantities
    }).subscribe({
      next: (res) => {
        this.pendingBookingId = res.booking_Id;
        // Proper timing transition to payment screen
        setTimeout(() => {
          this.isInitiatingBooking.set(false);
          this.showReviewModal.set(false);
        }, 800);
      },
      error: (err) => {
        this.isInitiatingBooking.set(false);
        alert(err?.error?.message || 'Failed to initiate booking reservation. Please try again.');
      }
    });
  }

  /**
   * Called when cancelling the review modal.
   * Redirects user back to event page.
   */
  public onCancelReview(): void {
    const event = this.event();
    const eventId = event ? event.event_Id : 0;
    this.showReviewModal.set(false);
    this.router.navigate(['/booking'], { queryParams: { eventId } });
  }

  /**
   * Guard trigger to revert pending bookings when navigating away.
   */
  public canDeactivate(): Observable<boolean> | boolean {
    if (this.pendingBookingId && !this.isConfirmed) {
      return new Observable<boolean>(observer => {
        this.bookingService.revertBooking(this.pendingBookingId!).subscribe({
          next: () => {
            observer.next(true);
            observer.complete();
          },
          error: () => {
            // Permit navigation even if API fails to revert
            observer.next(true);
            observer.complete();
          }
        });
      });
    }
    return true;
  }

  public openQrModal(): void {
    this.showQrModal.set(true);
  }

  public closeQrModal(): void {
    this.showQrModal.set(false);
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
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(amount);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
