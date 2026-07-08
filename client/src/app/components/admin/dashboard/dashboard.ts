import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { AdminService } from '../../../services/admin.service';
import { NavbarComponent } from '../../home/navbar/navbar';
import { FooterComponent } from '../../home/footer/footer';
import { AdminSidebarComponent } from '../sidebar/sidebar';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, FooterComponent, AdminSidebarComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class AdminDashboardComponent implements OnInit {
  public stats = signal<any>(null);
  public events = signal<any[]>([]);
  public totalCount = signal(0);
  public loading = signal(true);
  public errorMessage = signal<string | null>(null);

  public filters = {
    eventType: '',
    status: '',
    page: 1,
    size: 10
  };
  public dateFrom = '';
  public dateTo = '';

  public sortColumn = '';
  public sortDirection: 'asc' | 'desc' = 'asc';
  public readonly Math = Math;

  public showEventModal = false;
  public selectedEvent: any = null;
  public allVenues = signal<any[]>([]);
  public selectedVenueId: number | null = null;
  public venueUpdateMessage = signal<string | null>(null);

  constructor(private adminService: AdminService, private router: Router, private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.filters.eventType = params['eventType'] || '';
      this.filters.status = params['status'] || '';
      this.dateFrom = params['dateFrom'] || '';
      this.dateTo = params['dateTo'] || '';
      this.sortColumn = params['sortColumn'] || 'event_Date_Time';
      this.sortDirection = (params['sortDirection'] as 'asc' | 'desc') || 'desc';
      this.filters.page = params['page'] ? +params['page'] : 1;
      
      this.loadDashboard();
    });
  }

  updateUrlParams(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        eventType: this.filters.eventType || null,
        status: this.filters.status || null,
        dateFrom: this.dateFrom || null,
        dateTo: this.dateTo || null,
        sortColumn: this.sortColumn || null,
        sortDirection: this.sortDirection || null,
        page: this.filters.page > 1 ? this.filters.page : null
      },
      queryParamsHandling: 'merge'
    });
  }

  public loadDashboard(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.adminService.getDashboardStats().subscribe({
      next: (stats) => this.stats.set(stats),
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Failed to load dashboard stats.');
        const currentUrl = window.location.pathname + window.location.search;
        this.router.navigate(['/error'], { queryParams: { code: err.status || 500, returnUrl: currentUrl } });
      }
    });

    this.loadEvents();
  }

  private loadEvents(): void {
    const params: any = { page: this.filters.page, size: this.filters.size };
    if (this.filters.eventType) params.eventType = this.filters.eventType;
    if (this.filters.status) params.status = this.filters.status;
    if (this.sortColumn) params.sortBy = this.sortColumn + '_' + this.sortDirection;
    if (this.dateFrom) params.startDate = this.dateFrom;
    if (this.dateTo) params.endDate = this.dateTo;

    this.adminService.getAdminEvents(params).subscribe({
      next: (result) => {
        this.events.set(result.items || []);
        this.totalCount.set(result.totalCount || 0);
      },
      error: (err) => this.errorMessage.set(err.error?.message || 'Failed to load event list.'),
      complete: () => this.loading.set(false)
    });
  }

  public onFilterChange(): void {
    this.filters.page = 1;
    this.updateUrlParams();
  }

  public clearFilters(): void {
    this.filters = { eventType: '', status: '', page: 1, size: 10 };
    this.dateFrom = '';
    this.dateTo = '';
    this.sortColumn = 'event_Date_Time';
    this.sortDirection = 'desc';
    this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  public toggleSort(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
    this.updateUrlParams();
  }

  public changePage(delta: number): void {
    const newPage = this.filters.page + delta;
    if (newPage < 1) return;
    this.filters.page = newPage;
    this.updateUrlParams();
  }

  public openEventModal(ev: any): void {
    this.selectedEvent = ev;
    this.selectedVenueId = null;
    this.venueUpdateMessage.set(null);
    this.adminService.getAllVenuesIncludingInactive().subscribe({
      next: (venues) => this.allVenues.set(venues || []),
      error: () => this.allVenues.set([])
    });
    this.showEventModal = true;
  }

  public closeEventModal(): void {
    this.showEventModal = false;
    this.selectedEvent = null;
  }

  public updateEventVenue(): void {
    if (!this.selectedEvent || !this.selectedVenueId) return;
    this.adminService.updateEventVenue(this.selectedEvent.eventId || this.selectedEvent.EventId, this.selectedVenueId).subscribe({
      next: () => {
        this.venueUpdateMessage.set('Venue updated successfully.');
        this.loadEvents();
        setTimeout(() => this.closeEventModal(), 1200);
      },
      error: (err) => this.venueUpdateMessage.set(err.error?.message || 'Failed to update venue.')
    });
  }

  public formatEventDate(dateTime: string | Date | null | undefined): string {
    if (!dateTime) return '—';
    const d = new Date(dateTime);
    if (isNaN(d.getTime())) return '—';
    return d.toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' });
  }

  public formatEventTime(dateTime: string | Date | null | undefined): string {
    if (!dateTime) return '—';
    const d = new Date(dateTime);
    if (isNaN(d.getTime())) return '—';
    return d.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: true });
  }
}
