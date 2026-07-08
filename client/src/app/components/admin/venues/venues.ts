import { Component, signal, computed, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AdminService } from '../../../services/admin.service';
import { NavbarComponent } from '../../home/navbar/navbar';
import { FooterComponent } from '../../home/footer/footer';
import { AdminSidebarComponent } from '../sidebar/sidebar';

@Component({
  selector: 'app-admin-venues',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, FooterComponent, AdminSidebarComponent],
  templateUrl: './venues.html',
  styleUrl: './venues.css'
})
export class AdminVenuesComponent implements OnInit {
  public venues = signal<any[]>([]);
  public regions = signal<any[]>([]);
  public loading = signal(true);
  public errorMessage = signal<string | null>(null);

  public showModal = false;
  public venueName = '';
  public address = '';
  public hourlyPrice: number | null = null;
  public selectedRegionId = '';
  public seatTiers: Array<{ tier_Name: string; total_Seats: number | null }> = [{ tier_Name: '', total_Seats: null }];
  public successMessage = signal<string | null>(null);
  public modalErrorMessage = signal<string | null>(null);
  public showSuccessAnimation = false;

  public sortColumn = signal('status');
  public sortDirection = signal<'asc' | 'desc'>('asc');

  public sortedVenues = computed(() => {
    const col = this.sortColumn();
    let arr = [...this.venues()];
    return arr.sort((a, b) => {
      const col = this.sortColumn();
      const dir = this.sortDirection();
      const aVal = a[col] ?? a[col.charAt(0).toUpperCase() + col.slice(1)];
      const bVal = b[col] ?? b[col.charAt(0).toUpperCase() + col.slice(1)];
      
      if (aVal == null) return 1;
      if (bVal == null) return -1;
      
      const cmp = typeof aVal === 'number' ? aVal - bVal : String(aVal).localeCompare(String(bVal));
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  constructor(
    private adminService: AdminService,
    private route: ActivatedRoute,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.sortColumn.set(params['sortColumn'] || 'status');
      this.sortDirection.set((params['sortDirection'] as 'asc' | 'desc') || 'asc');
      this.loadData();
    });
  }

  updateUrlParams(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        sortColumn: this.sortColumn() || null,
        sortDirection: this.sortDirection() || null
      },
      queryParamsHandling: 'merge'
    });
  }

  public loadVenues(): void {
    this.loadData();
  }

  private loadData(): void {
    this.loading.set(true);
    this.adminService.getVenues().subscribe({
      next: (venues) => {
        this.venues.set(venues || []);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load venues.');
        this.loading.set(false);
      }
    });
    this.adminService.getRegions().subscribe({
      next: (regions) => this.regions.set(regions || []),
      error: () => {}
    });
  }

  public toggleSort(column: string): void {
    if (this.sortColumn() === column) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColumn.set(column);
      this.sortDirection.set('asc');
    }
    this.updateUrlParams();
  }

  public openModal(): void {
    this.showModal = true;
    this.showSuccessAnimation = false;
    this.venueName = '';
    this.address = '';
    this.hourlyPrice = null;
    this.selectedRegionId = '';
    this.seatTiers = [{ tier_Name: '', total_Seats: null }];
    this.successMessage.set(null);
    this.modalErrorMessage.set(null);
  }

  public addSeatTier(): void {
    this.seatTiers.push({ tier_Name: '', total_Seats: null });
  }

  public removeSeatTier(index: number): void {
    if (this.seatTiers.length > 1) {
      this.seatTiers.splice(index, 1);
    }
  }

  public closeModal(): void {
    this.showModal = false;
    this.showSuccessAnimation = false;
  }

  public createVenue(): void {
    if (!this.selectedRegionId || !this.venueName || !this.address || !this.hourlyPrice) {
      this.modalErrorMessage.set('All venue fields are required.');
      return;
    }

    const validTiers = this.seatTiers.filter(t => t.tier_Name && t.total_Seats);
    if (validTiers.length === 0) {
      this.modalErrorMessage.set('At least one seat tier with name and capacity is required.');
      return;
    }

    this.modalErrorMessage.set(null);
    this.adminService.createVenue({
      region_Id: this.selectedRegionId,
      name: this.venueName,
      address: this.address,
      hourly_Price: this.hourlyPrice,
      seatTiers: validTiers
    }).subscribe({
      next: () => {
        this.showSuccessAnimation = true;
        this.cdr.detectChanges();
        this.loadData();
        setTimeout(() => {
          this.closeModal();
        }, 2500);
      },
      error: (err) => this.modalErrorMessage.set(err.error?.message || 'Failed to register venue.')
    });
  }
}
