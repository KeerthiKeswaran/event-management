import {
  Component,
  Input,
  Output,
  EventEmitter,
  signal,
  OnInit,
  OnDestroy,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { of, delay } from 'rxjs';
import { Subscription } from 'rxjs';
import { BookingService } from '../../../services/booking.service';
import { BookingModel, BookingDetail } from '../../../models/booking.model';
import { CancellationPolicyDocComponent } from '../cancellation-policy-doc/cancellation-policy-doc';

@Component({
  selector: 'app-cancel-booking-modal',
  standalone: true,
  imports: [CommonModule, CancellationPolicyDocComponent],
  templateUrl: './cancel-booking-modal.html',
  styleUrl: './cancel-booking-modal.css'
})
export class CancelBookingModalComponent implements OnInit, OnDestroy {
  @Input({ required: true }) booking!: BookingModel;

  /** Emitted when modal requests close */
  @Output() closed = new EventEmitter<void>();

  /** Emitted after successful cancellation with the updated booking */
  @Output() cancelled = new EventEmitter<BookingModel>();

  // Refund estimate state
  public isLoadingRefund = signal(true);
  public estimatedRefundAmount = signal<number | null>(null);

  // Policy document dialog
  public showPolicyDoc = signal(false);

  // Cancellation action state
  public isCancelling = signal(false);
  public cancelError = signal('');
  public showSuccessAnimation = signal(false);

  private bookingService = inject(BookingService);
  private subscriptions = new Subscription();

  ngOnInit(): void {
    this.loadRefundEstimate();
  }

  private loadRefundEstimate(): void {
    this.isLoadingRefund.set(true);
    this.subscriptions.add(
      this.bookingService.getRefundEstimate(this.booking.booking_Id).subscribe({
        next: (res) => {
          this.estimatedRefundAmount.set(res.estimatedRefund || res.EstimatedRefund || 0);
          this.isLoadingRefund.set(false);
        },
        error: () => {
          this.estimatedRefundAmount.set(0);
          this.isLoadingRefund.set(false);
        }
      })
    );
  }

  /** Returns the colour associated with the refund amount */
  public getRefundColor(): string {
    const amount = this.estimatedRefundAmount();
    if (amount === null) return '#9ca3af';
    if (amount > 0) {
      const totalPaid = (this.booking.amount_Paid !== undefined ? this.booking.amount_Paid : this.booking.total_Amount) ?? 1;
      const pct = (amount / totalPaid) * 100;
      return pct >= 80 ? '#10b981' : '#f59e0b';
    }
    return '#ef4444';
  }

  /** Returns a human-readable status label */
  public getRefundStatusLabel(): string {
    const amount = this.estimatedRefundAmount();
    if (amount === null) return '—';
    if (amount <= 0) return 'Non-Refundable';
    const totalPaid = (this.booking.amount_Paid !== undefined ? this.booking.amount_Paid : this.booking.total_Amount) ?? 1;
    const pct = Math.round((amount / totalPaid) * 100);
    return `${pct}% Refund Eligible`;
  }

  public openPolicyDoc(event: Event): void {
    event.preventDefault();
    this.showPolicyDoc.set(true);
  }

  public closePolicyDoc(event?: Event): void {
    event?.stopPropagation();
    this.showPolicyDoc.set(false);
  }

  public close(): void {
    this.closed.emit();
  }

  public confirmCancellation(): void {
    if (this.isCancelling()) return;
    this.isCancelling.set(true);
    this.cancelError.set('');

    this.subscriptions.add(
      this.bookingService.cancelBooking(this.booking.booking_Id).subscribe({
        next: () => {
          this.isCancelling.set(false);
          this.showSuccessAnimation.set(true);

          setTimeout(() => {
            this.showSuccessAnimation.set(false);
            const updatedBooking = { ...this.booking, refunded_Amount: this.estimatedRefundAmount() || 0 };
            this.cancelled.emit(updatedBooking);
            this.close();
          }, 2800); // Wait for the animation to play
        },
        error: () => {
          this.isCancelling.set(false);
          this.cancelError.set('Cancellation failed. Please try again or contact support.');
        }
      })
    );
  }

  public formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-IN', {
      weekday: 'long', year: 'numeric', month: 'long', day: 'numeric'
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
