import { Component, OnInit, OnDestroy, signal, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { StripeService } from 'ngx-stripe';
import { Subscription } from 'rxjs';
import { NavbarComponent } from '../../home/navbar/navbar';
import { FooterComponent } from '../../home/footer/footer';
import { BookingService } from '../../../services/booking.service';
import { EventService } from '../../../services/event.service';

@Component({
  selector: 'app-stripe-checkout',
  standalone: true,
  imports: [CommonModule, NavbarComponent, FooterComponent],
  templateUrl: './stripe-checkout.html',
  styleUrl: './stripe-checkout.css'
})
export class StripeCheckoutComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('checkoutContainer') checkoutContainer!: ElementRef;

  public clientSecret = signal<string>('');
  public remainingSeconds = signal<number>(300);
  public formattedTime = signal<string>('05:00');
  public isCancelled = signal<boolean>(false);
  public showCancelModal = signal<boolean>(false);
  private resolveDeactivate: ((value: boolean) => void) | null = null;

  private type: 'booking' | 'event' | null = null;
  private targetId: number | null = null;
  private createdAtUTC: string = '';
  private timerInterval: any;
  private subscriptions = new Subscription();
  private stripeEmbeddedCheckout: any = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private stripeService: StripeService,
    private bookingService: BookingService,
    private eventService: EventService
  ) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.route.queryParams.subscribe(params => {
        if (!params['clientSecret'] || !params['createdAt'] || !params['type'] || !params['id']) {
          this.router.navigate(['/']);
          return;
        }

        this.clientSecret.set(params['clientSecret']);
        this.createdAtUTC = params['createdAt'];
        this.type = params['type'] as 'booking' | 'event';
        this.targetId = Number(params['id']);

        this.startTimer();
      })
    );
  }

  ngAfterViewInit(): void {
    if (this.clientSecret() && !this.isCancelled()) {
      this.initializeEmbeddedCheckout();
    }
  }

  private initializeEmbeddedCheckout(): void {
    this.stripeService.initEmbeddedCheckout({
      clientSecret: this.clientSecret()
    }).subscribe({
      next: (checkout: any) => {
        this.stripeEmbeddedCheckout = checkout;
        if (this.checkoutContainer) {
          checkout.mount(this.checkoutContainer.nativeElement);
        }
      },
      error: (err) => {
        console.error('Failed to initialize embedded checkout', err);
      }
    });
  }

  private startTimer(): void {
    this.updateTimer(); // Initial call
    this.timerInterval = setInterval(() => {
      this.updateTimer();
    }, 1000);
  }

  private updateTimer(): void {
    if (this.isCancelled()) return;

    // createdAtUTC is an ISO string from C# DateTime.UtcNow
    const createdTime = new Date(this.createdAtUTC).getTime();
    const now = Date.now();
    
    // 5 minutes = 300 seconds
    const elapsed = Math.floor((now - createdTime) / 1000);
    const remaining = Math.max(0, 300 - elapsed);
    
    this.remainingSeconds.set(remaining);

    const minutes = Math.floor(remaining / 60);
    const seconds = remaining % 60;
    this.formattedTime.set(`${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`);

    if (remaining <= 0) {
      this.handleTimeout();
    }
  }

  private handleTimeout(): void {
    clearInterval(this.timerInterval);
    this.isCancelled.set(true);
    
    // Unmount Stripe Checkout securely
    if (this.stripeEmbeddedCheckout) {
      try {
        this.stripeEmbeddedCheckout.destroy();
      } catch (e) {
        console.error('Error destroying stripe checkout', e);
      }
    }

    // Call backend revert APIs safely
    if (this.type === 'booking' && this.targetId) {
      this.bookingService.revertBooking(this.targetId).subscribe({
        next: () => console.log('Booking reverted successfully after timeout.'),
        error: (err) => console.error('Failed to revert booking', err)
      });
    } else if (this.type === 'event' && this.targetId) {
      this.eventService.revertEvent(this.targetId).subscribe({
        next: () => console.log('Event reverted successfully after timeout.'),
        error: (err) => console.error('Failed to revert event', err)
      });
    }
  }

  public goBack(): void {
    if (this.type === 'booking') {
      this.router.navigate(['/browse']);
    } else {
      this.router.navigate(['/myevents']);
    }
  }

  canDeactivate(nextState?: any): Promise<boolean> | boolean {
    if (this.isCancelled() || (nextState && nextState.url.includes('/checkout'))) {
      return true;
    }
    this.showCancelModal.set(true);
    return new Promise((resolve) => {
      this.resolveDeactivate = resolve;
    });
  }

  public confirmLeave(): void {
    this.showCancelModal.set(false);
    this.handleTimeout(); // Cleans up and calls revert API
    if (this.resolveDeactivate) this.resolveDeactivate(true);
  }

  public cancelLeave(): void {
    this.showCancelModal.set(false);
    if (this.resolveDeactivate) this.resolveDeactivate(false);
  }

  ngOnDestroy(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
    }
    if (this.stripeEmbeddedCheckout) {
      try {
        this.stripeEmbeddedCheckout.destroy();
      } catch (e) {}
    }
    this.subscriptions.unsubscribe();
  }
}
