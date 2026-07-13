import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { TicketTierSelection } from '../../models/booking.model';
import { BrowsedEventResponse } from '../../models/event.model';
import { mockAllEvents } from '../../data/event.mock';
import { NavbarComponent } from '../home/navbar/navbar';
import { FooterComponent } from '../home/footer/footer';
import { ReportEventModalComponent } from '../shared/report-event-modal/report-event-modal';

import { ResolveDescriptionPipe } from '../../pipes/resolve-description.pipe';
import { EventService } from '../../services/event.service';
import { BookingService } from '../../services/booking.service';
import { WaitlistService } from '../../services/waitlist.service';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, FooterComponent, ResolveDescriptionPipe, ReportEventModalComponent],
  templateUrl: './booking.html',
  styleUrl: './booking.css'
})
export class BookingComponent implements OnInit, OnDestroy {
  public event = signal<BrowsedEventResponse | null>(null);
  public tiers = signal<TicketTierSelection[]>([]);
  public relatedEvents = signal<BrowsedEventResponse[]>([]);

  public isLoading = signal(false);

  // Report modal state
  public showReportModal = signal(false);
  public activeReportEventId = signal<number>(0);
  public activeReportEventTitle = signal<string>('');

  private subscriptions = new Subscription();

  public subtotalAmount = computed(() =>
    this.tiers().reduce((sum, t) => sum + t.price * t.quantity, 0)
  );

  public totalTickets = computed(() =>
    this.tiers().reduce((sum, t) => sum + t.quantity, 0)
  );

  public gstPercentage = signal(18);
  public ticketFixedFee = signal(0);

  public fixedFeeTotal = computed(() => this.totalTickets() * this.ticketFixedFee());
  public gstAmount = computed(() => Math.round(this.subtotalAmount() * (this.gstPercentage() / 100)));
  public totalAmount = computed(() => this.subtotalAmount() + this.fixedFeeTotal() + this.gstAmount());

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private eventService: EventService,
    private bookingService: BookingService,
    private waitlistService: WaitlistService,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    // Fetch platform settings first
    this.subscriptions.add(
      this.eventService.getPlatformSettings().subscribe({
        next: (settings) => {
          if (settings.ticket_Fixed_Fee) this.ticketFixedFee.set(settings.ticket_Fixed_Fee);
          if (settings.gsT_Percentage) this.gstPercentage.set(settings.gsT_Percentage);
        }
      })
    );

    this.subscriptions.add(
      this.route.queryParams.subscribe(params => {
        const eventId = Number(params['eventId']);
        if (!eventId) {
          this.router.navigate(['/browse']);
          return;
        }

        this.isLoading.set(true);

        this.subscriptions.add(
          this.eventService.getPlatformSettings().subscribe({
            next: (res) => {
              if (res) {
                this.gstPercentage.set(res.gsT_Percentage ?? res.GST_Percentage ?? 18);
              }
            }
          })
        );

        // 1. Try loading from history navigation state (all existing data from clicked card)
        const stateEvent = history.state?.event as BrowsedEventResponse;
        if (stateEvent && Number(stateEvent.event_Id) === eventId) {
          this.initializeEvent(stateEvent);
          this.isLoading.set(false);
          return;
        }

        // 2. Try loading from local mock events list
        const found = mockAllEvents.find(e => e.event_Id === eventId);
        if (found) {
          this.initializeEvent(found);
          this.isLoading.set(false);
          return;
        }

        // 3. Fall back to loading from the backend API directly using EventService
        this.eventService.getEventById(eventId).subscribe({
          next: (ev) => {
            this.isLoading.set(false);
            if (ev) {
              this.initializeEvent(ev);
            } else {
              this.router.navigate(['/browse']);
            }
          },
          error: () => {
            this.isLoading.set(false);
            this.router.navigate(['/browse']);
          }
        });
      })
    );
  }

  private initializeEvent(found: BrowsedEventResponse): void {
    this.event.set(found);

    // Build tier selection list from the event's actual ticketTiers initially
    let tierData: TicketTierSelection[] = (found.ticketTiers ?? []).map(t => {
      console.log('Tier:', t.tier_Name, 'has_Active_Waitlist:', t.has_Active_Waitlist);
      return {
        tierName: t.tier_Name,
        price: t.price,
        quantity: 0,
        totalSeats: t.capacity ?? 99999,
        availableSeats: Math.max(0, (t.capacity ?? 99999) - t.tickets_Sold),
        waitlistQuantity: 1, // default waitlist req
        has_Active_Waitlist: t.has_Active_Waitlist
      };
    });
    this.tiers.set(tierData);

    // Fetch live capacities from backend to update available seats accurately
    this.subscriptions.add(
      this.eventService.getEventSeats(found.event_Id).subscribe({
        next: (seats) => {
          if (seats && seats.length > 0) {
            tierData = tierData.map(tier => {
              const liveSeat = seats.find((s: any) => s.tier_Name === tier.tierName);
              if (liveSeat) {
                return {
                  ...tier,
                  totalSeats: liveSeat.total_Seats,
                  availableSeats: liveSeat.available_Seats
                };
              }
              return tier;
            });
            this.tiers.set(tierData);
          }
        }
      })
    );

    // Load related events (matching similar categories via browseEvents API)
    const category = found.category || 'General';
    this.eventService.browseEvents({ category, page: 1, size: 4 }).subscribe({
      next: (result) => {
        let list = (result.items || []).filter(e => e.event_Id !== found.event_Id);
        
        // Ensure at least 2 events by combining with all events if same-category count is too low
        if (list.length < 2) {
          this.eventService.browseEvents({ page: 1, size: 4 }).subscribe({
            next: (allResult) => {
              const extra = (allResult.items || []).filter(
                e => e.event_Id !== found.event_Id && !list.some(l => l.event_Id === e.event_Id)
              );
              list = [...list, ...extra].slice(0, 4);
              this.relatedEvents.set(list);
            },
            error: () => {
              this.relatedEvents.set(this.getMockRelatedEvents(found));
            }
          });
        } else {
          this.relatedEvents.set(list.slice(0, 4));
        }
      },
      error: () => {
        this.relatedEvents.set(this.getMockRelatedEvents(found));
      }
    });
  }

  private getMockRelatedEvents(found: BrowsedEventResponse): BrowsedEventResponse[] {
    return mockAllEvents
      .filter(e => e.event_Id !== found.event_Id && (e.event_Type === found.event_Type || e.region_Id === found.region_Id))
      .slice(0, 4);
  }

  public increaseQty(tierName: string): void {
    const tiers = this.tiers();
    const tier = tiers.find(t => t.tierName === tierName);
    if (!tier) return;
    if (tier.quantity >= Math.min(tier.availableSeats, 10)) return;
    tier.quantity++;
    this.tiers.set([...tiers]);
  }

  public decreaseQty(tierName: string): void {
    const tiers = this.tiers();
    const tier = tiers.find(t => t.tierName === tierName);
    if (!tier || tier.quantity === 0) return;
    tier.quantity--;
    this.tiers.set([...tiers]);
  }

  public increaseWaitlistQty(tierName: string): void {
    const tiers = this.tiers();
    const tier = tiers.find(t => t.tierName === tierName);
    if (tier && tier.waitlistQuantity) {
      tier.waitlistQuantity++;
      this.tiers.set([...tiers]);
    }
  }

  public decreaseWaitlistQty(tierName: string): void {
    const tiers = this.tiers();
    const tier = tiers.find(t => t.tierName === tierName);
    if (tier && tier.waitlistQuantity && tier.waitlistQuantity > 1) {
      tier.waitlistQuantity--;
      this.tiers.set([...tiers]);
    }
  }

  public joinWaitlist(tierName: string): void {
    const ev = this.event();
    if (!ev) return;
    
    const tier = this.tiers().find(t => t.tierName === tierName);
    if (!tier) return;

    this.waitlistService.joinWaitlist({
      eventId: ev.event_Id,
      tierName: tier.tierName,
      quantity: tier.waitlistQuantity || 1
    }).subscribe({
      next: (res) => {
        if (res.status === 'Booked') {
          this.waitlistSuccessMessage.set('Seats became available and you were booked!');
          this.showWaitlistSuccessAnimation.set(true);
          setTimeout(() => {
            this.showWaitlistSuccessAnimation.set(false);
            window.location.reload(); 
          }, 2000);
        } else {
          this.waitlistSuccessMessage.set(`Joined Waitlist. Position: ${res.position}`);
          this.showWaitlistSuccessAnimation.set(true);
          setTimeout(() => {
            this.showWaitlistSuccessAnimation.set(false);
          }, 2000);
        }
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to join waitlist. Are you logged in?');
      }
    });
  }

  public getSeatFillPercent(tier: TicketTierSelection): number {
    return Math.round(((tier.totalSeats - tier.availableSeats) / tier.totalSeats) * 100);
  }

  public showReviewModal = signal(false);
  public isInitiatingBooking = signal(false);
  public showWaitlistSuccessAnimation = signal(false);
  public waitlistSuccessMessage = signal('');

  public isCheckoutDisabled(): boolean {
    return this.totalTickets() === 0 || this.isInitiatingBooking();
  }

  public openReportModal(): void {
    const ev = this.event();
    if (ev && !this.hasReported()) {
      if (ev.has_Reported === null) {
        // Not logged in -> redirect to login
        this.router.navigate(['/login']);
        return;
      }
      this.activeReportEventId.set(ev.event_Id);
      this.activeReportEventTitle.set(ev.title);
      this.showReportModal.set(true);
    }
  }

  public hasReported(): boolean {
    if (typeof window === 'undefined') return false;
    const ev = this.event();
    if (!ev) return false;
    if (ev.has_Reported === true) return true;
    const reported = JSON.parse(localStorage.getItem('reportedEvents') || '[]');
    return reported.includes(ev.event_Id);
  }

  public routeToEvent(eventId: number): void {
    this.showReviewModal.set(false);
  }

  public proceedToCheckout(): void {
    if (this.totalTickets() === 0) return;
    this.showReviewModal.set(true);
  }

  public onCancelReview(): void {
    this.showReviewModal.set(false);
  }

  public onConfirmReview(): void {
    const tierQuantities: Record<string, number> = {};
    this.tiers().filter(t => t.quantity > 0).forEach(t => {
      tierQuantities[t.tierName] = t.quantity;
    });

    const event = this.event();
    const eventId = event ? event.event_Id : 0;

    this.isInitiatingBooking.set(true);

    this.subscriptions.add(
      this.bookingService.initiateBooking({
        eventId: eventId,
        tierQuantities: tierQuantities
      }).subscribe({
        next: (res) => {
          const pendingBookingId = res.booking_Id;
          const successUrl = `${environment.clientUrl}/checkout?eventId=${eventId}&session_id={CHECKOUT_SESSION_ID}&bookingId=${pendingBookingId}`;
          const cancelUrl = `${environment.clientUrl}/booking?eventId=${eventId}`;

          this.bookingService.createCheckoutSession(pendingBookingId, successUrl, cancelUrl).subscribe({
            next: (stripeRes) => {
              this.router.navigate(['/stripe-checkout'], {
                queryParams: {
                  clientSecret: stripeRes.clientSecret,
                  createdAt: stripeRes.createdAtUTC,
                  type: 'booking',
                  id: pendingBookingId
                }
              });
            },
            error: (err) => {
              this.isInitiatingBooking.set(false);
              console.error('Failed to create checkout session', err);
            }
          });
        },
        error: (err) => {
          this.isInitiatingBooking.set(false);
          console.error('Failed to initiate booking', err);
        }
      })
    );
  }

  public navigateToRelatedEvent(eventObj: any): void {
    this.router.navigate(['/booking'], { 
      queryParams: { eventId: eventObj.event_Id },
      state: { event: eventObj }
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
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

  public mapEmbedUrl = computed<SafeResourceUrl | null>(() => {
    const evt = this.event();
    if (!evt || evt.event_Type === 'Virtual') return null;
    
    const parts = [evt.venue_Name, evt.address, evt.venue_Region_Name].filter(x => !!x);
    if (parts.length === 0) return null;
    
    const query = encodeURIComponent(parts.join(', '));
    return this.sanitizer.bypassSecurityTrustResourceUrl(`https://maps.google.com/maps?q=${query}&t=&z=14&ie=UTF8&iwloc=&output=embed`);
  });

  public openGoogleMaps(): void {
    const evt = this.event();
    if (!evt || evt.event_Type === 'Virtual') return;
    const parts = [evt.venue_Name, evt.address, evt.venue_Region_Name].filter(x => !!x);
    if (parts.length === 0) return;
    const query = encodeURIComponent(parts.join(', '));
    window.open(`https://www.google.com/maps/search/?api=1&query=${query}`, '_blank');
  }

  public onContactOrg(): void {
    const evt = this.event();
    const orgEmail = evt?.organizer_Email;
    if (orgEmail) {
      const subject = encodeURIComponent(`Inquiry regarding event: ${evt?.title || ''}`);
      window.location.href = `mailto:${orgEmail}?subject=${subject}`;
    } else {
      alert('Organizer email is not available.');
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}

