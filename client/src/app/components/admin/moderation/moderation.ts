import { Component, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive, ActivatedRoute, Router } from '@angular/router';
import { AdminService } from '../../../services/admin.service';
import { NavbarComponent } from '../../home/navbar/navbar';
import { FooterComponent } from '../../home/footer/footer';
import { AdminSidebarComponent } from '../sidebar/sidebar';

@Component({
  selector: 'app-admin-moderation',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, FooterComponent, AdminSidebarComponent],
  templateUrl: './moderation.html',
  styleUrl: './moderation.css'
})
export class AdminModerationComponent implements OnInit {
  public reports = signal<any[]>([]);
  public staffDirectory = signal<any[]>([]);
  public regions = signal<any[]>([]);
  public errorMessage = signal<string | null>(null);
  public loadingReports = signal(false);
  public loadingStaff = signal(false);

  public staffFilterRegion = '';
  public staffFilterStatus = '';
  public staffFilterKeyword = '';

  public flagsExpanded = true;
  public staffExpanded = true;

  public reportPage = signal(1);
  public reportPageSize = 5;
  public staffPage = signal(1);
  public staffPageSize = 10;
  public staffTotalCount = signal(0);
  public staffTotalPages = signal(1);

  public reportSortColumn = signal('created_At');
  public reportSortDirection = signal<'asc' | 'desc'>('desc');
  public staffSortColumn = signal('status');
  public staffSortDirection = signal<'asc' | 'desc'>('asc');

  public showUpholdModal = false;
  public selectedReport: any = null;
  public upholdReason = '';
  public organizerAction = 'No Action';

  public showDismissModal = false;
  public dismissReportId: number | null = null;
  public dismissEventId: number | null = null;

  public showAllocateModal = false;
  public selectedStaff: any = null;
  public regionEvents = signal<any[]>([]);
  public selectedEventId: number | null = null;
  public allocateMessage = signal<string | null>(null);

  public showSuccessModal = false;
  public successModalMessage = '';

  // Event Detail Modal (for flagged reports & staff allocation)
  public showEventDetailModal = false;
  public selectedEventDetail = signal<any>(null);
  public eventDetailLoading = signal(false);

  public readonly Math = Math;

  public showSuccessAnimation = false;

  public pagedReports = computed(() => {
    const sorted = this.sortData(this.reports(), this.reportSortColumn(), this.reportSortDirection());
    const start = (this.reportPage() - 1) * this.reportPageSize;
    return sorted.slice(start, start + this.reportPageSize);
  });

  public pagedStaff = computed(() => {
    return this.staffDirectory();
  });

  constructor(private adminService: AdminService, private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.staffFilterRegion = params['region'] || '';
      this.staffFilterStatus = params['status'] || '';
      this.staffFilterKeyword = params['keyword'] || '';
      
      this.reportPage.set(params['rPage'] ? +params['rPage'] : 1);
      this.staffPage.set(params['sPage'] ? +params['sPage'] : 1);
      
      this.reportSortColumn.set(params['rSort'] || 'created_At');
      this.reportSortDirection.set((params['rDir'] as 'asc' | 'desc') || 'desc');
      
      this.staffSortColumn.set(params['sSort'] || 'status');
      this.staffSortDirection.set((params['sDir'] as 'asc' | 'desc') || 'asc');
      
      this.loadData();
    });
  }

  updateUrlParams(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        region: this.staffFilterRegion || null,
        status: this.staffFilterStatus || null,
        keyword: this.staffFilterKeyword || null,
        rPage: this.reportPage() > 1 ? this.reportPage() : null,
        sPage: this.staffPage() > 1 ? this.staffPage() : null,
        rSort: this.reportSortColumn() || null,
        rDir: this.reportSortDirection() || null,
        sSort: this.staffSortColumn() || null,
        sDir: this.staffSortDirection() || null
      },
      queryParamsHandling: 'merge'
    });
  }

  public loadReports(): void {
    this.loadingReports.set(true);
    this.adminService.getEventReports().subscribe({
      next: (result) => {
        const flat: any[] = [];
        if (result && typeof result === 'object') {
          Object.keys(result).forEach((eventId) => {
            const group = result[eventId];
            if (group && group.reports) {
              group.reports.forEach((r: any) => {
                flat.push({ ...r, eventId: Number(eventId), countOfReports: group.countOfReports });
              });
            }
          });
        }
        this.reports.set(flat);
        this.loadingReports.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load moderation reports.');
        this.loadingReports.set(false);
      }
    });
  }

  private loadData(): void {
    this.loadReports();
    this.loadStaff();
    this.adminService.getRegions().subscribe({
      next: (regions) => this.regions.set(regions || []),
      error: () => {}
    });
  }

  public loadStaff(): void {
    this.loadingStaff.set(true);
    const params: any = { page: this.staffPage(), size: this.staffPageSize };
    if (this.staffFilterRegion) params.regionId = this.staffFilterRegion;
    if (this.staffFilterStatus === 'allocated') params.isAllocated = true;
    else if (this.staffFilterStatus === 'available') params.isAllocated = false;

    if (this.staffSortColumn()) {
      params.sortBy = `${this.staffSortColumn()}_${this.staffSortDirection()}`;
    }

    this.adminService.getStaffDirectory(params).subscribe({
      next: (res) => {
        if (res && res.items) {
          this.staffDirectory.set(res.items);
          this.staffTotalCount.set(res.totalCount);
          this.staffTotalPages.set(res.totalPages);
        } else {
          this.staffDirectory.set(res || []);
          this.staffTotalCount.set((res || []).length);
          this.staffTotalPages.set(1);
        }
        this.loadingStaff.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load staff directory.');
        this.loadingStaff.set(false);
      }
    });
  }

  public onStaffFilterChange(): void {
    this.staffPage.set(1);
    this.updateUrlParams();
  }

  public clearStaffFilters(): void {
    this.staffFilterRegion = '';
    this.staffFilterStatus = '';
    this.staffFilterKeyword = '';
    this.staffSortColumn.set('status');
    this.staffSortDirection.set('asc');
    this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  public dismissReport(reportId: number): void {
    this.adminService.dismissEventReport(reportId).subscribe({
      next: () => {
        this.showDismissModal = false;
        this.dismissReportId = null;
        this.dismissEventId = null;
        this.showSuccessWithMessage('Report dismissed successfully', () => this.loadData());
      },
      error: (err) => this.errorMessage.set(err.error?.message || 'Failed to dismiss report.')
    });
  }

  public openDismissModal(report: any): void {
    this.dismissReportId = report.reportId;
    this.dismissEventId = report.eventId;
    this.showDismissModal = true;
  }

  public cancelDismiss(): void {
    this.showDismissModal = false;
    this.dismissReportId = null;
    this.dismissEventId = null;
  }

  public openUpholdModal(report: any): void {
    this.selectedReport = report;
    this.upholdReason = '';
    this.organizerAction = 'No Action';
    this.showUpholdModal = true;
  }

  public closeUpholdModal(): void {
    this.showUpholdModal = false;
    this.selectedReport = null;
  }

  public upholdReport(): void {
    if (!this.selectedReport || !this.upholdReason) return;
    this.adminService.upholdEventReport(this.selectedReport.reportId, {
      reason: this.upholdReason,
      organizerAction: this.organizerAction
    }).subscribe({
      next: () => {
        this.closeUpholdModal();
        this.showSuccessWithMessage('Report upheld successfully', () => this.loadData());
      },
      error: (err) => this.errorMessage.set(err.error?.message || 'Failed to uphold report.')
    });
  }

  public openAllocateModal(staff: any): void {
    this.selectedStaff = staff;
    this.selectedEventId = null;
    this.allocateMessage.set(null);
    this.regionEvents.set([]);

    this.adminService.getEventsByRegion(staff.regionId || staff.region_Id || staff.Region_Id).subscribe({
      next: (events) => this.regionEvents.set(events || []),
      error: () => this.allocateMessage.set('Unable to load events for this region.')
    });

    this.showAllocateModal = true;
  }

  public closeAllocateModal(): void {
    this.showAllocateModal = false;
    this.selectedStaff = null;
  }

  private showSuccessWithMessage(message: string, onClose: () => void): void {
    this.successModalMessage = message;
    this.showSuccessModal = true;
    setTimeout(() => {
      this.showSuccessModal = false;
      onClose();
    }, 2000);
  }

  public openEventDetailModal(eventId: number): void {
    this.selectedEventDetail.set(null);
    this.eventDetailLoading.set(true);
    this.showEventDetailModal = true;
    this.adminService.getAdminEventById(eventId).subscribe({
      next: (ev) => {
        this.selectedEventDetail.set(ev);
        this.eventDetailLoading.set(false);
      },
      error: () => {
        this.eventDetailLoading.set(false);
      }
    });
  }

  public closeEventDetailModal(): void {
    this.showEventDetailModal = false;
    this.selectedEventDetail.set(null);
  }

  public copiedState = '';
  public copyText(text: string, type: string = 'text'): void {
    if (text) {
      navigator.clipboard.writeText(text).then(() => {
        this.copiedState = type;
        setTimeout(() => {
          if (this.copiedState === type) {
            this.copiedState = '';
          }
        }, 2000);
      });
    }
  }

  public openStaffEventDetailModal(staff: any): void {
    const allocated = staff.isAllocated ?? staff.IsAllocated;
    if (!allocated) return; // only clickable for allocated staff
    const eventId = staff.allocatedEventId || staff.AllocatedEventId || staff.currentEventId || staff.event_Id || staff.Event_Id;
    if (eventId) {
      this.openEventDetailModal(eventId);
    }
  }

  public allocateStaff(): void {
    if (!this.selectedEventId || !this.selectedStaff) return;
    this.adminService.allocateStaff(
      this.selectedEventId,
      this.selectedStaff.employee_ID || this.selectedStaff.Employee_ID
    ).subscribe({
      next: () => {
        this.showSuccessAnimation = true;
        setTimeout(() => {
          this.showAllocateModal = false;
          this.showSuccessAnimation = false;
          this.loadStaff();
          this.selectedStaff = null;
        }, 1500);
      },
      error: (err) => this.allocateMessage.set(err.error?.message || 'Failed to allocate staff.')
    });
  }

  public changeReportPage(delta: number): void {
    const newPage = this.reportPage() + delta;
    if (newPage > 0 && newPage <= this.Math.ceil(this.reports().length / this.reportPageSize)) {
      this.reportPage.set(newPage);
      this.updateUrlParams();
    }
  }

  public changeStaffPage(delta: number): void {
    const newPage = this.staffPage() + delta;
    if (newPage > 0 && newPage <= this.staffTotalPages()) {
      this.staffPage.set(newPage);
      this.updateUrlParams();
    }
  }

  public toggleReportSort(column: string): void {
    if (this.reportSortColumn() === column) {
      this.reportSortDirection.set(this.reportSortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.reportSortColumn.set(column);
      this.reportSortDirection.set('asc');
    }
    this.updateUrlParams();
  }

  public toggleStaffSort(column: string): void {
    if (this.staffSortColumn() === column) {
      this.staffSortDirection.set(this.staffSortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.staffSortColumn.set(column);
      this.staffSortDirection.set('asc');
    }
    this.updateUrlParams();
  }

  public toggleFlagsSection(): void {
    this.flagsExpanded = !this.flagsExpanded;
  }

  public toggleStaffSection(): void {
    this.staffExpanded = !this.staffExpanded;
  }

  private sortData(arr: any[], column: string, dir: 'asc' | 'desc'): any[] {
    if (!column) return [...arr];
    return [...arr].sort((a, b) => {
      let aVal = this.resolveValue(a, column);
      let bVal = this.resolveValue(b, column);
      
      if (column === 'responseAction') {
        aVal = aVal || 'Pending';
        bVal = bVal || 'Pending';
      }

      if (aVal == null) return 1;
      if (bVal == null) return -1;
      const cmp = typeof aVal === 'number' ? aVal - bVal : String(aVal).localeCompare(String(bVal));
      return dir === 'asc' ? cmp : -cmp;
    });
  }

  private resolveValue(obj: any, key: string): any {
    const val = obj[key] ?? obj[key.charAt(0).toUpperCase() + key.slice(1)];
    if (val !== undefined && val !== null) return val;
    const snakeKey = key.replace(/([A-Z])/g, '_$1').toLowerCase();
    if (snakeKey !== key) {
      const snakeVal = obj[snakeKey] ?? obj[snakeKey.charAt(0).toUpperCase() + snakeKey.slice(1)];
      if (snakeVal !== undefined && snakeVal !== null) return snakeVal;
    }
    return undefined;
  }
}
