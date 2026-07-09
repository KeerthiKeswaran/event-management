import { Component, signal, computed, OnInit, Pipe, PipeTransform, ChangeDetectorRef } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive, ActivatedRoute, Router } from '@angular/router';
import { AdminService } from '../../../services/admin.service';
import { NavbarComponent } from '../../home/navbar/navbar';
import { FooterComponent } from '../../home/footer/footer';
import { AdminSidebarComponent } from '../sidebar/sidebar';
import { environment } from '../../../../environments/environment';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';


@Pipe({
  name: 'highlight',
  standalone: true
})
export class HighlightPipe implements PipeTransform {
  constructor(private sanitizer: DomSanitizer) { }

  transform(value: string | number | null | undefined, keyword: string): SafeHtml | string {
    if (value == null) return '';
    const text = String(value);
    if (!keyword) return text;

    const escapedKeyword = keyword.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const regex = new RegExp(`(${escapedKeyword})`, 'gi');
    const highlighted = text.replace(regex, '<span class="highlight-match">$1</span>');
    return this.sanitizer.bypassSecurityTrustHtml(highlighted);
  }
}

@Component({
  selector: 'app-admin-helpdesk',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, FooterComponent, AdminSidebarComponent, HighlightPipe],
  templateUrl: './helpdesk.html',
  styleUrl: './helpdesk.css'
})
export class AdminHelpdeskComponent implements OnInit {
  public tickets = signal<any[]>([]);
  public actions: any[] = [];
  public targetTypes: any[] = [];
  public loading = signal(false);

  public selectedTicket: any = null;
  public selectedTicketId: number | null = null;
  public isModalOpen = false;
  public showEscalateForm = false;
  public escalationStatusData: any = null;
  public respondText = '';

  public action = 'REF';
  public targetType = 'ATD';
  public remarks = '';

  public errorMessage = signal<string | null>(null);
  public successMessage = signal<string | null>(null);

  public filterStatus = '';
  public filterKeyword = '';
  public filterType = '';
  public filterDateFrom = '';
  public filterDateTo = '';
  public filterEscalation = '';

  private searchSubject = new Subject<string>();

  public sortColumn = signal('createdAt');
  public sortDirection = signal<'asc' | 'desc'>('desc');

  public currentPage = signal(1);
  public pageSize = signal(10);

  public sortedTickets = computed(() => {
    let arr = [...this.tickets()];

    return arr.filter(t => {
      const kw = this.filterKeyword.toLowerCase();
      const matchKw = !kw ||
        (t.ticket_Id?.toString().includes(kw) || t.ticketId?.toString().includes(kw)) ||
        ((t.senderEmail || t.SenderEmail || '')?.toLowerCase().includes(kw)) ||
        ((t.fetchedSubject || '')?.toLowerCase().includes(kw)) ||
        (t.user_Id?.toString().includes(kw) || t.User_Id?.toString().includes(kw));

      const matchType = !this.filterType || (t.requestType || t.RequestType || '') === this.filterType;
      const matchStatus = !this.filterStatus || (t.status || t.Status || '') === this.filterStatus;

      let matchDate = true;
      if (this.filterDateFrom || this.filterDateTo) {
        const tDate = new Date(t.created_At || t.CreatedAt);
        tDate.setHours(0, 0, 0, 0);
        if (this.filterDateFrom) {
          const from = new Date(this.filterDateFrom);
          from.setHours(0, 0, 0, 0);
          matchDate = matchDate && tDate >= from;
        }
        if (this.filterDateTo) {
          const to = new Date(this.filterDateTo);
          to.setHours(0, 0, 0, 0);
          matchDate = matchDate && tDate <= to;
        }
      }

      let matchEsc = true;
      if (this.filterEscalation) {
        const escStatus = t.esclationStatus || t.EsclationStatus || t.escalationStatus || 'Unavailable';
        matchEsc = escStatus === this.filterEscalation;
      }

      return matchKw && matchType && matchStatus && matchDate && matchEsc;
    }).sort((a, b) => {
      const col = this.sortColumn();
      const dir = this.sortDirection();
      const aVal = a[col] ?? a[col.charAt(0).toUpperCase() + col.slice(1)];
      const bVal = b[col] ?? b[col.charAt(0).toUpperCase() + col.slice(1)];
      if (aVal == null) return 1;
      if (bVal == null) return -1;
      if (col === 'created_At' || col === 'CreatedAt') {
        const d1 = new Date(aVal).getTime();
        const d2 = new Date(bVal).getTime();
        return dir === 'asc' ? d1 - d2 : d2 - d1;
      }
      const cmp = typeof aVal === 'number' ? aVal - bVal : String(aVal).localeCompare(String(bVal));
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  public totalPages = computed(() => {
    return Math.max(1, Math.ceil(this.sortedTickets().length / this.pageSize()));
  });

  public pagedTickets = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize();
    return this.sortedTickets().slice(start, start + this.pageSize());
  });

  public nextPage(): void {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
    }
  }

  public prevPage(): void {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
    }
  }

  constructor(
    private adminService: AdminService,
    private http: HttpClient,
    private cdr: ChangeDetectorRef,
    private route: ActivatedRoute,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(searchTerm => {
      this.filterKeyword = searchTerm;
      this.onFilterChange();
    });

    this.route.queryParams.subscribe(params => {
      this.filterStatus = params['status'] || '';
      this.filterKeyword = params['keyword'] || '';
      this.filterType = params['type'] || '';
      this.filterDateFrom = params['dateFrom'] || '';
      this.filterDateTo = params['dateTo'] || '';
      this.filterEscalation = params['escalation'] || '';
      this.sortColumn.set(params['sortColumn'] || 'created_At');
      this.sortDirection.set((params['sortDirection'] as 'asc' | 'desc') || 'desc');

      this.loadMetadata();
      this.loadTickets();
    });
  }

  public onSearchInput(term: string) {
    this.searchSubject.next(term);
  }

  public updateUrlParams(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        status: this.filterStatus || null,
        keyword: this.filterKeyword || null,
        type: this.filterType || null,
        dateFrom: this.filterDateFrom || null,
        dateTo: this.filterDateTo || null,
        escalation: this.filterEscalation || null,
        sortColumn: this.sortColumn() || null,
        sortDirection: this.sortDirection() || null
      },
      queryParamsHandling: 'merge'
    });
  }

  private loadMetadata(): void {
    this.adminService.getHelpdeskMetadata().subscribe({
      next: (meta) => {
        this.actions = meta.actions || [];
        this.targetTypes = meta.targetTypes || [];
      },
      error: () => {
        this.actions = [
          { key: 'REF', label: 'Refund' },
          { key: 'EVT', label: 'Event' },
          { key: 'ACC', label: 'Account' },
          { key: 'GEN', label: 'General' }
        ];
        this.targetTypes = [
          { key: 'ATD', label: 'Attendee' },
          { key: 'ORG', label: 'Organizer' }
        ];
      }
    });
  }

  public loadTickets(): void {
    this.loading.set(true);
    const params: any = {};
    if (this.filterStatus) params.status = this.filterStatus;
    if (this.filterKeyword) params.keyword = this.filterKeyword;
    if (this.filterDateFrom) params.dateFrom = this.filterDateFrom;
    if (this.filterDateTo) params.dateTo = this.filterDateTo;

    this.adminService.getSupportTickets(params).subscribe({
      next: (tickets: any) => {
        let list: any[] = [];
        if (Array.isArray(tickets)) list = tickets;
        else if (tickets && tickets.$values) list = tickets.$values;
        else if (tickets && tickets.data) list = tickets.data;
        else if (tickets && tickets.items) list = tickets.items;

        list.forEach((t: any) => {
          t.fetchedSubject = t.subject || t.Subject || t.fetchedSubject;
          t.fetchedMessage = t.message || t.Message || t.fetchedMessage;

          if (!t.fetchedSubject && !t.fetchedMessage) {
            let url = t.concernUrl || t.ConcernUrl || t.concern_Url;
            if (url) {
              if (url.startsWith('/')) {
                url = environment.serverUrl + url;
              }
              this.http.get<any>(url).subscribe({
                next: (data) => {
                  t.fetchedSubject = data.subject || data.Subject || 'No Subject';
                  t.fetchedMessage = data.message || data.Message || 'No Message';
                  this.tickets.update(v => [...v]);
                },
                error: () => {
                  t.fetchedSubject = 'Failed to load';
                  t.fetchedMessage = 'Failed to load';
                  this.tickets.update(v => [...v]);
                }
              });
            }
          }
        });
        this.tickets.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load support tickets.');
        this.loading.set(false);
      }
    });
  }

  public onFilterChange(): void {
    this.currentPage.set(1);
    this.updateUrlParams();
  }



  public getCleanEmail(email: string | null | undefined): string {
    if (!email) return 'no-email@provided.com';
    return email.replace(/[<>]/g, '').trim();
  }

  public clearFilters(): void {
    this.filterStatus = '';
    this.filterKeyword = '';
    this.filterType = '';
    this.filterDateFrom = '';
    this.filterDateTo = '';
    this.filterEscalation = '';
    this.currentPage.set(1);
    this.sortColumn.set('created_At');
    this.sortDirection.set('desc');
    this.updateUrlParams();
    this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  public toggleSort(column: string): void {
    if (this.sortColumn() === column) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColumn.set(column);
      this.sortDirection.set('asc');
    }
    this.currentPage.set(1);
    this.updateUrlParams();
  }

  public selectTicket(ticket: any): void {
    const id = ticket.ticket_Id || ticket.ticketId || ticket.Ticket_Id || ticket.id;
    this.selectedTicket = ticket;
    this.selectedTicketId = id;
    this.respondText = '';
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.showEscalateForm = false;
    this.escalationStatusData = null;
    this.relatedEventDetails = null;
    this.showRelatedDetails = false;
    this.isModalOpen = true;

    if (ticket.esclationStatus === 'Escalated' || ticket.EsclationStatus === 'Escalated' || ticket.escalationStatus === 'Escalated') {
      this.fetchEscalationStatus(id);
    }
  }

  public closeModal(): void {
    this.isModalOpen = false;
    this.selectedTicket = null;
    this.selectedTicketId = null;
    this.showRelatedDetails = false;
  }

  public handleModalClick(event: Event): void {
    event.stopPropagation();
    if (this.showRelatedDetails) {
      this.showRelatedDetails = false;
      this.cdr.detectChanges();
    }
  }

  public copiedState = '';
  public copyText(text: string | number, type: string = 'text'): void {
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

  public relatedEventDetails: any = null;
  public relatedEventLoading = false;
  public relatedEventError = false;
  public showRelatedDetails = false;

  public toggleRelatedEntityDetails(event: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.showRelatedDetails = !this.showRelatedDetails;
    this.cdr.detectChanges();
    
    if (this.showRelatedDetails && !this.relatedEventDetails) {
      const id = this.selectedTicket?.relatedId || this.selectedTicket?.RelatedId;
      const type = this.selectedTicket?.targetType || this.selectedTicket?.TargetType || 'ANY';
      
      if (id) {
        this.relatedEventLoading = true;
        this.relatedEventError = false;
        this.cdr.detectChanges();
        
        this.adminService.getRelatedEntity(type, id).subscribe({
          next: (ev) => {
            this.relatedEventDetails = ev;
            this.relatedEventLoading = false;
            this.cdr.detectChanges();
          },
          error: () => {
            this.relatedEventError = true;
            this.relatedEventLoading = false;
            this.cdr.detectChanges();
          }
        });
      }
    }
  }

  public fetchEscalationStatus(ticketId: number | null): void {
    if (!ticketId) return;
    this.adminService.getEscalationStatus(ticketId).subscribe({
      next: (data) => {
        this.escalationStatusData = data;
      },
      error: (err) => console.warn('Failed to fetch escalation status', err)
    });
  }

  public respondToTicket(): void {
    if (!this.selectedTicketId || !this.respondText) return;
    this.adminService.respondToTicket(this.selectedTicketId, this.respondText).subscribe({
      next: () => {
        this.successAnimText = `Ticket Closed successfully`;
        this.showSuccessAnim = true;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.showSuccessAnim = false;
          this.successMessage.set('Response sent successfully.');
          this.respondText = '';
          this.closeModal();
          this.loadTickets();
          this.cdr.detectChanges();
        }, 1800);
      },
      error: (err) => this.errorMessage.set(err.error?.message || 'Failed to respond.')
    });
  }

  public showSuccessAnim = false;
  public successAnimText = '';

  public escalateSelectedTicket(): void {
    if (!this.selectedTicketId) {
      this.errorMessage.set('Select a ticket before escalation.');
      return;
    }

    this.adminService.escalateTicket(this.selectedTicketId, {
      actionType: this.action,
      targetType: this.targetType,
      targetId: this.selectedTicket?.user_Id || this.selectedTicket?.User_Id || 0,
      referenceId: this.selectedTicket?.relatedId || this.selectedTicket?.RelatedId || this.selectedTicketId
    }).subscribe({
      next: () => {
        if (this.selectedTicket) {
          this.selectedTicket.esclationStatus = 'Escalated';
          this.selectedTicket.EsclationStatus = 'Escalated';
          this.selectedTicket.escalationStatus = 'Escalated';
        }
        this.fetchEscalationStatus(this.selectedTicketId!);

        // Show success animation
        this.showEscalateForm = false;
        this.successAnimText = `Ticket #${this.selectedTicketId} Escalated`;
        this.showSuccessAnim = true;
        this.cdr.detectChanges();

        setTimeout(() => {
          this.showSuccessAnim = false;
          this.remarks = '';
          this.closeModal();
          this.loadTickets();
          this.cdr.detectChanges();
        }, 1800);
      },
      error: (err) => this.errorMessage.set(err.error?.message || 'Failed to escalate ticket.')
    });
  }
}
