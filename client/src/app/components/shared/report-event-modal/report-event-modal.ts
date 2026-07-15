import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EventService } from '../../../services/event.service';

@Component({
  selector: 'app-report-event-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="modal-backdrop" *ngIf="show()">
      <div class="modal-container">
        <div class="modal-header">
          <h2>Report Event</h2>
          <button class="close-btn" (click)="close()">×</button>
        </div>
        <div class="modal-body">
          <p>Please provide a reason for reporting the event "<strong>{{ eventTitle }}</strong>".</p>
          <textarea [(ngModel)]="reportReason" placeholder="Enter reason (e.g. Inappropriate content, scam, etc.)" rows="4"></textarea>
          
          <div *ngIf="error()" class="error-msg">{{ error() }}</div>
        </div>
        <div class="modal-footer">
          <button class="btn-cancel" (click)="close()" [disabled]="isSubmitting()">Cancel</button>
          <button class="btn-submit" (click)="submitReport()" [disabled]="!reportReason.trim() || isSubmitting()">
            {{ isSubmitting() ? 'Submitting...' : 'Submit Report' }}
          </button>
        </div>
      </div>
      
      <div *ngIf="isSubmitting()" class="processing-overlay">
        <div class="large-black-spinner-wrap">
          <div class="large-black-spinner"></div>
        </div>
        <p class="success-text" style="opacity: 0; visibility: hidden;">Event Reported</p>
      </div>

      <div *ngIf="showSuccessAnimation()" class="success-tick-overlay">
        <div class="success-checkmark-svg-wrap">
          <svg class="checkmark-svg" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 52 52">
            <circle class="checkmark-circle" cx="26" cy="26" r="25" fill="none"/>
            <path class="checkmark-check" fill="none" d="M14.1 27.2l7.1 7.2 16.7-16.8"/>
          </svg>
        </div>
        <p class="success-text">Event Reported</p>
      </div>
    </div>
  `,
  styles: [`
    .modal-backdrop { position: fixed; top: 0; left: 0; width: 100vw; height: 100vh; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal-container { background: #fff; width: 90%; max-width: 500px; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.15); overflow: hidden; }
    .modal-header { padding: 16px 20px; border-bottom: 1px solid #eee; display: flex; justify-content: space-between; align-items: center; }
    .modal-header h2 { margin: 0; font-size: 18px; color: #333; }
    .close-btn { background: transparent; border: none; font-size: 24px; cursor: pointer; color: #666; padding: 0; line-height: 1; }
    .modal-body { padding: 20px; }
    .modal-body p { margin-top: 0; margin-bottom: 16px; color: #555; }
    textarea { width: 100%; border: 1px solid #ddd; border-radius: 8px; padding: 12px; font-family: inherit; font-size: 14px; resize: vertical; box-sizing: border-box; }
    textarea:focus { outline: none; border-color: #007bff; }
    .error-msg { color: #dc3545; margin-top: 10px; font-size: 14px; }
    .modal-footer { padding: 16px 20px; border-top: 1px solid #eee; display: flex; justify-content: flex-end; gap: 12px; background: #f9f9f9; }
    .btn-cancel, .btn-submit { padding: 10px 16px; border-radius: 6px; font-weight: 500; cursor: pointer; border: none; font-size: 14px; }
    .btn-cancel { background: #e0e0e0; color: #333; }
    .btn-cancel:hover:not(:disabled) { background: #d0d0d0; }
    .btn-submit { background: #dc3545; color: white; }
    .btn-submit:hover:not(:disabled) { background: #c82333; }
    .btn-submit:disabled, .btn-cancel:disabled { opacity: 0.6; cursor: not-allowed; }
    
    .success-tick-overlay { position: absolute; inset: 0; border-radius: 12px; background: rgba(255, 255, 255, 0.75); display: flex; flex-direction: column; align-items: center; justify-content: center; z-index: 10; animation: fadeInOverlay 0.3s ease-out; }
    .success-text { margin-top: 16px; font-size: 1.25rem; font-weight: 700; color: #991b1b; opacity: 0; animation: fadeInUpCancel 0.4s cubic-bezier(0.25, 0.8, 0.25, 1) 0.8s forwards; }
    .processing-overlay { position: absolute; inset: 0; border-radius: 12px; background: rgba(255, 255, 255, 0.75); display: flex; flex-direction: column; align-items: center; justify-content: center; z-index: 10; animation: fadeInOverlay 0.3s ease-out; }
    .large-black-spinner-wrap { width: 60px; height: 60px; display: flex; align-items: center; justify-content: center; }
    .large-black-spinner { width: 40px; height: 40px; border: 4px solid rgba(0, 0, 0, 0.1); border-top-color: #000; border-radius: 50%; animation: spin 0.8s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }
    @keyframes fadeInUpCancel { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
    @keyframes fadeInOverlay { from { opacity: 0; } to { opacity: 1; } }
    .success-checkmark-svg-wrap { width: 60px; height: 60px; }
    .checkmark-svg { width: 60px; height: 60px; border-radius: 50%; display: block; stroke-width: 4; stroke: #991b1b; stroke-miterlimit: 10; box-shadow: inset 0px 0px 0px #991b1b; animation: fillCheckmarkCancel .4s ease-in-out .4s forwards, scaleCheckmarkCancel .3s ease-in-out .9s alternate both; }
    .checkmark-circle { stroke-dasharray: 166; stroke-dashoffset: 166; stroke-width: 4; stroke-miterlimit: 10; stroke: #991b1b; fill: none; animation: strokeCheckmarkCancel 0.6s cubic-bezier(0.65, 0, 0.45, 1) forwards; }
    .checkmark-check { transform-origin: 50% 50%; stroke-dasharray: 48; stroke-dashoffset: 48; stroke: #fff; animation: strokeCheckmarkCancel 0.3s cubic-bezier(0.65, 0, 0.45, 1) 0.8s forwards; }
    @keyframes strokeCheckmarkCancel { 100% { stroke-dashoffset: 0; } }
    @keyframes scaleCheckmarkCancel { 0%, 100% { transform: none; } 50% { transform: scale3d(1.1, 1.1, 1); } }
    @keyframes fillCheckmarkCancel { 100% { box-shadow: inset 0px 0px 0px 30px #991b1b; } }
  `]
})
export class ReportEventModalComponent {
  @Input() show = signal(false);
  @Input() eventId!: number;
  @Input() eventTitle: string = '';
  @Output() closeEvent = new EventEmitter<void>();

  reportReason: string = '';
  isSubmitting = signal(false);
  error = signal('');
  showSuccessAnimation = signal(false);

  constructor(private eventService: EventService) {}

  close() {
    this.reportReason = '';
    this.error.set('');
    this.closeEvent.emit();
  }

  submitReport() {
    if (!this.reportReason.trim()) return;
    this.isSubmitting.set(true);
    this.error.set('');

    this.eventService.reportEvent(this.eventId, this.reportReason.trim()).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.showSuccessAnimation.set(true);
        
        // Add to local storage for UI disable rule
        if (typeof window !== 'undefined') {
          const reported = JSON.parse(localStorage.getItem('reportedEvents') || '[]');
          if (!reported.includes(this.eventId)) {
            reported.push(this.eventId);
            localStorage.setItem('reportedEvents', JSON.stringify(reported));
          }
        }

        setTimeout(() => {
          this.showSuccessAnimation.set(false);
          this.close();
        }, 1500);
      },
      error: (err: any) => {
        this.isSubmitting.set(false);
        this.error.set(err.error?.message || 'Failed to report event.');
      }
    });
  }
}
