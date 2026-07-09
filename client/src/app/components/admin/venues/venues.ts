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
  public venueNameError = signal<string | null>(null);
  public similarVenues = signal<any[]>([]);
  public showSuccessAnimation = false;

  public isEditing = false;
  public editingVenueId: number | null = null;
  public isAvailable = true;

  public filterRegion = signal<string>('');
  public filterStatus = signal<string>('');
  public filterMinPrice = signal<number | null>(null);
  public filterMaxPrice = signal<number | null>(null);

  public sortColumn = signal('status');
  public sortDirection = signal<'asc' | 'desc'>('asc');

  public currentPage = signal(1);
  public pageSize = signal(10);

  public clearFilters() {
    this.filterRegion.set('');
    this.filterStatus.set('');
    this.filterStatus.set('');
    this.filterMinPrice.set(null);
    this.filterMaxPrice.set(null);
    this.currentPage.set(1);
  }

  public getRegionName(id: string): string {
    const region = this.regions().find(r => (r.region_Id || r.Region_Id) === id);
    return region ? (region.region_Name || region.Region_Name || region.name || region.Name) : id;
  }

  public filteredVenues = computed(() => {
    let arr = [...this.venues()];
    
    // Filters
    const region = this.filterRegion();
    if (region) {
      arr = arr.filter(v => (v.region_Id || v.Region_Id) === region);
    }
    
    const status = this.filterStatus();
    if (status === 'active') {
      arr = arr.filter(v => (v.is_Available ?? v.Is_Available));
    } else if (status === 'inactive') {
      arr = arr.filter(v => !(v.is_Available ?? v.Is_Available));
    }

    const min = this.filterMinPrice();
    if (min !== null) {
      arr = arr.filter(v => (v.hourly_Price || v.Hourly_Price) >= min);
    }

    const max = this.filterMaxPrice();
    if (max !== null) {
      arr = arr.filter(v => (v.hourly_Price || v.Hourly_Price) <= max);
    }

    return arr.sort((a, b) => {
      const col = this.sortColumn();
      const dir = this.sortDirection();
      let aVal = a[col] ?? a[col.charAt(0).toUpperCase() + col.slice(1)];
      let bVal = b[col] ?? b[col.charAt(0).toUpperCase() + col.slice(1)];
      
      // Handle dates correctly
      if (col === 'createdAt') {
        aVal = aVal ? new Date(aVal).getTime() : 0;
        bVal = bVal ? new Date(bVal).getTime() : 0;
      }
      
      if (aVal == null) return 1;
      if (bVal == null) return -1;
      
      const cmp = typeof aVal === 'number' ? aVal - bVal : String(aVal).localeCompare(String(bVal));
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  public pagedVenues = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize();
    return this.filteredVenues().slice(start, start + this.pageSize());
  });

  public totalPages = computed(() => {
    return Math.max(1, Math.ceil(this.filteredVenues().length / this.pageSize()));
  });

  public prevPage(): void {
    if (this.currentPage() > 1) {
      this.currentPage.set(this.currentPage() - 1);
      this.updateUrlParams();
    }
  }

  public nextPage(): void {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.set(this.currentPage() + 1);
      this.updateUrlParams();
    }
  }

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
      this.currentPage.set(params['page'] ? +params['page'] : 1);
      this.loadData();
    });
  }

  updateUrlParams(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        sortColumn: this.sortColumn() || null,
        sortDirection: this.sortDirection() || null,
        page: this.currentPage()
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
    this.currentPage.set(1);
    this.updateUrlParams();
  }

  public openModal(): void {
    this.showModal = true;
    this.showSuccessAnimation = false;
    this.isEditing = false;
    this.editingVenueId = null;
    this.isAvailable = true;
    this.venueName = '';
    this.address = '';
    this.hourlyPrice = null;
    this.selectedRegionId = '';
    this.seatTiers = [{ tier_Name: '', total_Seats: null }];
    this.successMessage.set(null);
    this.modalErrorMessage.set(null);
    this.venueNameError.set(null);
    this.similarVenues.set([]);
  }

  public openEditModal(venue: any): void {
    const isAv = venue.is_Available ?? venue.Is_Available ?? true;
    if (!isAv) {
      this.errorMessage.set('Cannot update details for an Allocated Venue.');
      window.scrollTo(0, 0);
      return;
    }
    this.showModal = true;
    this.showSuccessAnimation = false;
    this.isEditing = true;
    this.editingVenueId = venue.venue_Id || venue.Venue_Id;
    this.isAvailable = isAv;
    this.venueName = venue.name || venue.Name;
    this.address = venue.address || venue.Address;
    this.hourlyPrice = venue.hourly_Price || venue.Hourly_Price;
    this.selectedRegionId = venue.region_Id || venue.Region_Id;
    
    if (venue.seatTiers && venue.seatTiers.length > 0) {
      this.seatTiers = venue.seatTiers.map((t: any) => ({
        tier_Name: t.tier_Name || t.Tier_Name,
        total_Seats: t.total_Seats || t.Total_Seats
      }));
    } else if (venue.SeatTiers && venue.SeatTiers.length > 0) {
       this.seatTiers = venue.SeatTiers.map((t: any) => ({
        tier_Name: t.tier_Name || t.Tier_Name,
        total_Seats: t.total_Seats || t.Total_Seats
      }));
    } else {
      this.seatTiers = [{ tier_Name: '', total_Seats: null }];
    }

    this.successMessage.set(null);
    this.modalErrorMessage.set(null);
    this.venueNameError.set(null);
    this.similarVenues.set([]);
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
    this.cdr.detectChanges();
  }

  public onVenueNameBlur(): void {
    setTimeout(() => {
      this.similarVenues.set([]);
    }, 200);
  }

  public onVenueNameChange(): void {
    const val = this.venueName.trim();
    this.modalErrorMessage.set(null);

    if (!val) {
      this.similarVenues.set([]);
      this.venueNameError.set(null);
      return;
    }

    this.adminService.searchVenues(val).subscribe({
      next: (venues: any[]) => {
        const exactMatch = venues.find(v => (v.name || v.Name || '').toLowerCase() === val.toLowerCase() && (this.isEditing ? (v.venue_Id || v.Venue_Id) !== this.editingVenueId : true));
        
        if (exactMatch) {
          this.venueNameError.set("This venue is already exists, try different name if you pointing out to different region.");
        } else {
          this.venueNameError.set(null);
        }

        const similar = venues.filter(v => (v.name || v.Name || '').toLowerCase() !== val.toLowerCase());
        this.similarVenues.set(similar);
      },
      error: () => {}
    });
  }

  public createVenue(): void {
    if (this.venueNameError()) {
      return;
    }
    
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
    const payload = {
      region_Id: this.selectedRegionId,
      name: this.venueName,
      address: this.address,
      hourly_Price: this.hourlyPrice,
      is_Available: this.isAvailable,
      seatTiers: validTiers
    };

    if (this.isEditing && this.editingVenueId) {
      this.adminService.updateVenue(this.editingVenueId, payload).subscribe({
        next: () => {
          this.showSuccessAnimation = true;
          this.cdr.detectChanges();
          this.loadData();
          setTimeout(() => {
            this.closeModal();
          }, 2500);
        },
        error: (err) => this.modalErrorMessage.set(err.error?.message || 'Failed to update venue.')
      });
    } else {
      this.adminService.createVenue(payload).subscribe({
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
}
