import { Component, Input, Output, EventEmitter, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { CancellationPolicyDocComponent } from '../../bookings/cancellation-policy-doc/cancellation-policy-doc';

@Component({
  selector: 'app-cancel-event-modal',
  standalone: true,
  imports: [CommonModule, CancellationPolicyDocComponent],
  templateUrl: './cancel-event-modal.html',
  styleUrl: './cancel-event-modal.css'
})
export class CancelEventModalComponent implements OnInit {
  @Input({ required: true }) event!: any;
  @Output() closed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<any>();

  public isCancelling = signal(false);
  public cancelError = signal('');
  public showSuccessAnimation = signal(false);
  public showPolicyDoc = signal(false);
  
  public upfrontPaid = 0;
  public estimatedRefund = 0;

  private http = inject(HttpClient);

  ngOnInit() {
    this.calculateRefund();
  }

  private calculateRefund() {
    // If backend doesn't provide upfront fee, assume standard ₹5000 for demonstration
    this.upfrontPaid = this.event.upfront_Fee || 5000;
    
    const eventDate = new Date(this.event.date_Time || this.event.Date_Time);
    const now = new Date();
    
    const diffHours = (eventDate.getTime() - now.getTime()) / (1000 * 60 * 60);
    
    if (diffHours > 48) {
      this.estimatedRefund = this.upfrontPaid * 0.90;
    } else if (diffHours > 24) {
      this.estimatedRefund = this.upfrontPaid * 0.50;
    } else {
      this.estimatedRefund = 0;
    }
  }

  public formatDate(dateString: string): string {
    if (!dateString) return '—';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  public formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2
    }).format(amount);
  }

  public close(): void {
    if (this.isCancelling()) return;
    this.closed.emit();
  }

  public openPolicyDoc(event: Event) {
    event.preventDefault();
    this.showPolicyDoc.set(true);
  }

  public closePolicyDoc() {
    this.showPolicyDoc.set(false);
  }

  public confirmCancellation(): void {
    if (this.isCancelling()) return;

    this.isCancelling.set(true);
    this.cancelError.set('');

    const url = `${environment.serverUrl}/api/Event/${this.event.event_Id}/cancel`;
    
    this.http.post(url, {}).subscribe({
      next: () => {
        this.isCancelling.set(false);
        this.showSuccessAnimation.set(true);
        setTimeout(() => {
          this.cancelled.emit(this.event);
        }, 2800);
      },
      error: (err) => {
        this.isCancelling.set(false);
        const errMsg = err.error?.message || err.error?.Message || 'Failed to cancel the event. Please try again.';
        this.cancelError.set(errMsg);
      }
    });
  }
}
