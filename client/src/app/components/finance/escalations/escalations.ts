import { Component, OnInit, signal, computed, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { FinanceService } from '../../../services/finance.service';
import { NavbarComponent } from '../../home/navbar/navbar';
import { SidebarComponent } from '../sidebar/sidebar';
import { FooterComponent } from '../../home/footer/footer';
import { environment } from '../../../../environments/environment';


@Component({
  selector: 'app-finance-escalations',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, SidebarComponent, FooterComponent],
  templateUrl: './escalations.html',
  styleUrl: './escalations.css'
})
export class EscalationsComponent implements OnInit {
  // Data
  actions = signal<any[]>([]);
  loading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  // Sort
  sortColumn = signal('createdAt');
  sortDirection = signal<'asc' | 'desc'>('desc');

  sortedActions = computed(() => {
    const col = this.sortColumn();
    const dir = this.sortDirection();
    return [...this.actions()].sort((a, b) => {
      const aVal = a[col] ?? a[col.charAt(0).toUpperCase() + col.slice(1)];
      const bVal = b[col] ?? b[col.charAt(0).toUpperCase() + col.slice(1)];
      if (aVal == null) return 1;
      if (bVal == null) return -1;
      if (col === 'createdAt') {
        return dir === 'asc'
          ? new Date(aVal).getTime() - new Date(bVal).getTime()
          : new Date(bVal).getTime() - new Date(aVal).getTime();
      }
      const cmp = String(aVal).localeCompare(String(bVal));
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  // Modal state
  isModalOpen = false;
  selectedAction: any = null;

  // Approve form
  showApproveForm = false;
  refundType = '';
  approveMessage = '';
  approving = false;
  approveSuccess = false;
  processedActionId: number | null = null;
  processedStatusText: string = 'Processed';
  actionProcessed = false;

  // Decline form
  showDeclineConfirm = false;
  declineRemarks = '';
  declining = false;

  // Related entity popover
  relatedEntityDetails: any = null;
  relatedEntityLoading = false;
  relatedEntityError = false;
  showRelatedDetails = false;

  refundTypes = [
    { code: 'FUL', label: 'Full Refund' },
    { code: 'DYN', label: 'Dynamic (Time-based) Refund' },
    { code: 'REM', label: 'Remaining Balance Refund' },
    { code: 'NOR', label: 'No Refund' }
  ];

  constructor(
    private financeService: FinanceService,
    private http: HttpClient,
    private cdr: ChangeDetectorRef,
    private route: ActivatedRoute,
    private router: Router
  ) { }

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.sortColumn.set(params['sortColumn'] || 'createdAt');
      this.sortDirection.set((params['sortDirection'] as 'asc' | 'desc') || 'desc');

      this.loadActions();
    });
  }

  updateUrlParams() {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        sortColumn: this.sortColumn() || null,
        sortDirection: this.sortDirection() || null
      },
      queryParamsHandling: 'merge'
    });
  }

  loadActions() {
    this.loading.set(true);
    this.errorMessage.set('');
    this.financeService.getAdminActions().subscribe({
      next: (res: any[]) => {
        this.actions.set(res || []);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load escalations.');
        this.loading.set(false);
      }
    });
  }

  toggleSort(column: string) {
    if (this.sortColumn() === column) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColumn.set(column);
      this.sortDirection.set('asc');
    }
    this.updateUrlParams();
  }

  getTicketId(action: any): number | null {
    return action?.ticketId ?? action?.TicketId ?? null;
  }

  getActionId(action: any): number {
    return action?.actionId ?? action?.ActionId ?? action?.id ?? action?.Id;
  }

  getSubject(action: any): string {
    return action?.fetchedSubject || action?.details?.Subject || action?.details?.subject || '—';
  }

  getSenderEmail(action: any): string {
    return action?.senderEmail || action?.SenderEmail || action?.fetchedEmail || '—';
  }

  getEscalationType(action: any): string {
    const type = action?.actionType ?? action?.ActionType ?? '—';
    const map: any = {
      'REF': 'Refund',
      'GEN': 'General',
      'REQ': 'Request',
      'CMP': 'Complaint',
      'INQ': 'Inquiry'
    };
    return map[type] || type;
  }

  getTargetType(action: any): string {
    const type = action?.targetType ?? action?.TargetType ?? '—';
    const map: any = {
      'ATD': 'Attendee',
      'ORG': 'Organizer',
      'TKT': 'Ticket',
      'EVT': 'Event',
      'USR': 'User'
    };
    return map[type] || type;
  }

  getActionStatus(action: any): string {
    return action?.actionStatus ?? action?.ActionStatus ?? action?.status ?? '—';
  }

  getStatusClass(status: string): string {
    const s = (status || '').toLowerCase();
    if (s === 'pending') return 'pending';
    if (s === 'processing') return 'processing';
    if (s === 'processed') return 'completed';
    if (s === 'declined') return 'failed';
    return s;
  }

  openModal(action: any) {
    this.selectedAction = action;
    this.showApproveForm = false;
    this.showDeclineConfirm = false;
    this.refundType = '';
    this.approveMessage = '';
    this.declineRemarks = '';
    this.approving = false;
    this.actionProcessed = false;
    this.processedActionId = null;
    this.relatedEntityDetails = null;
    this.relatedEntityError = false;
    this.relatedEntityLoading = false;
    this.showRelatedDetails = false;
    this.errorMessage.set('');
    this.successMessage.set('');
    this.isModalOpen = true;

    // Pre-fetch subject if needed
    if (!action.fetchedSubject) {
      const ticketId = this.getTicketId(action);
      const details = action?.details;
      if (details?.Subject) {
        action.fetchedSubject = details.Subject;
        action.fetchedMessage = details.Message;
        action.fetchedResponse = details.Response;
      }
    }
  }

  closeModal() {
    if (this.actionProcessed) {
      this.loadActions();
    }
    this.isModalOpen = false;
    this.selectedAction = null;
    this.showRelatedDetails = false;
  }

  handleModalClick(event: Event) {
    event.stopPropagation();
    if (this.showRelatedDetails) {
      this.showRelatedDetails = false;
      this.cdr.detectChanges();
    }
  }

  toggleRelatedEntity(event: Event) {
    event.preventDefault();
    event.stopPropagation();
    this.showRelatedDetails = !this.showRelatedDetails;
    this.cdr.detectChanges();

    if (this.showRelatedDetails && !this.relatedEntityDetails) {
      const relatedId = this.selectedAction?.relatedId ?? this.selectedAction?.RelatedId;
      const targetType = this.selectedAction?.targetType ?? this.selectedAction?.TargetType ?? 'ANY';

      if (relatedId) {
        this.relatedEntityLoading = true;
        this.relatedEntityError = false;
        this.cdr.detectChanges();

        this.http.get(`${environment.apiUrl}/finance/related-entity/${targetType}/${relatedId}`).subscribe({
          next: (ev: any) => {
            this.relatedEntityDetails = ev;
            this.relatedEntityLoading = false;
            this.cdr.detectChanges();
          },
          error: () => {
            this.relatedEntityError = true;
            this.relatedEntityLoading = false;
            this.cdr.detectChanges();
          }
        });
      }
    }
  }

  copiedState = '';
  copyText(text: any, type: string = 'text') {
    if (text) {
      navigator.clipboard.writeText(String(text)).then(() => {
        this.copiedState = type;
        setTimeout(() => {
          if (this.copiedState === type) {
            this.copiedState = '';
            this.cdr.detectChanges();
          }
        }, 2000);
      });
    }
  }

  startApprove() {
    this.showApproveForm = true;
    this.showDeclineConfirm = false;
  }

  startDecline() {
    this.showDeclineConfirm = true;
    this.showApproveForm = false;
  }

  cancelApprove() {
    this.showApproveForm = false;
    this.refundType = '';
    this.approveMessage = '';
  }

  cancelDecline() {
    this.showDeclineConfirm = false;
    this.declineRemarks = '';
  }

  proceedApprove() {
    if (!this.refundType) {
      this.errorMessage.set('Please select a refund type.');
      return;
    }
    const id = this.getActionId(this.selectedAction);
    this.approving = true;
    this.errorMessage.set('');

    this.financeService.approveAction(id, this.refundType, this.approveMessage).subscribe({
      next: () => {
        this.approving = false;
        this.actionProcessed = true;
        this.processedActionId = id;
        this.processedStatusText = 'Approved';
        this.showApproveForm = false;
        this.cdr.detectChanges();

        // Auto-close after 2.5s
        setTimeout(() => this.closeModal(), 2500);
      },
      error: (err: any) => {
        this.approving = false;
        this.errorMessage.set(err.error?.Message || 'Failed to approve action.');
      }
    });
  }

  proceedDecline() {
    if (!this.declineRemarks.trim()) {
      this.errorMessage.set('Please provide a reason for declining.');
      return;
    }
    const id = this.getActionId(this.selectedAction);
    this.declining = true;
    this.errorMessage.set('');

    this.financeService.declineAction(id, this.declineRemarks).subscribe({
      next: () => {
        this.declining = false;
        this.actionProcessed = true;
        this.processedActionId = id;
        this.processedStatusText = 'Declined';
        this.showDeclineConfirm = false;
        this.cdr.detectChanges();

        // Auto-close after 2.5s
        setTimeout(() => this.closeModal(), 2500);
      },
      error: (err: any) => {
        this.declining = false;
        this.errorMessage.set(err.error?.Message || 'Failed to decline action.');
      }
    });
  }
}
