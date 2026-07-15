import { Component, OnInit, OnDestroy, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { AppStoreService } from '../../store/app-store.service';
import { AuthService } from '../../services/auth.service';
import { EventService } from '../../services/event.service';
import { RegionService } from '../../services/region.service';
import { LocationGeoService } from '../../services/location-geo.service';
import { BrowsedEventResponse } from '../../models/event.model';
import { RegionModel } from '../../models/region.model';
import { FooterComponent } from '../home/footer/footer';
import { NavbarComponent } from '../home/navbar/navbar';

import { ResolveDescriptionPipe } from '../../pipes/resolve-description.pipe';
import { StripHtmlPipe } from '../../pipes/strip-html.pipe';

@Component({
  selector: 'app-browse-events',
  standalone: true,
  imports: [CommonModule, FormsModule, FooterComponent, NavbarComponent, ResolveDescriptionPipe, StripHtmlPipe],
  templateUrl: './browse-events.html',
  styleUrl: './browse-events.css'
})
export class BrowseEventsComponent implements OnInit, OnDestroy {
  @HostListener('document:click', ['$event'])
  public onDocumentClick(event: MouseEvent): void {
    this.closeDropdowns();
  }
  // Filters binded to inputs
  public filterKeyword = '';
  public navSearchKeyword = '';
  public filterRegionIds: string[] = [];
  public filterCategory = '';
  public filterFormat = '';
  public filterMinPrice: number = 0;
  public filterMaxPrice: number = 25000;
  public filterSortBy = '';
  public currentPage = 1;
  public pageSize = 6;
  public totalPages = 1;
  private previousPage = 1;
  public maxAvailablePrice = 25000;

  // Local signals
  public isProfileDropdownOpen = signal(false);
  public showNationwideRegions = signal(false);

  // Modal signals
  public isLocationModalOpen = signal(false);

  // Store select observables/signals
  public currentUser = signal<any>(null);
  public isLoggedIn = signal(false);
  public events = signal<BrowsedEventResponse[]>([]);
  public regions = signal<RegionModel[]>([]);
  public eventsLoading = signal(false);
  public activeUserRegionId = signal('REG01');
  public totalEvents = signal(0);
  public categories = signal<string[]>([]);

  private subscriptions: Subscription = new Subscription();

  constructor(
    private store: AppStoreService,
    private authService: AuthService,
    private eventService: EventService,
    private regionService: RegionService,
    private locationGeoService: LocationGeoService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Subscribe to store state
    this.subscriptions.add(
      this.store.select(state => state.auth.user).subscribe(user => this.currentUser.set(user))
    );
    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(logged => this.isLoggedIn.set(logged))
    );
    this.subscriptions.add(
      this.store.select(state => state.events.items).subscribe(evs => this.events.set(evs))
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.items).subscribe(regs => this.regions.set(regs))
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.currentRegionId).subscribe(regId => {
        this.activeUserRegionId.set(regId || 'REG01');
        this.fetchEvents();
      })
    );
    this.subscriptions.add(
      this.store.select(state => state.events.loading).subscribe(loading => this.eventsLoading.set(loading))
    );

    // Initial loads
    this.regionService.loadRegions().subscribe();
    this.loadCategories();

    // Listen to query parameters
    this.subscriptions.add(
      this.route.queryParams.subscribe(params => {
        this.filterKeyword = params['keyword'] || '';
        this.navSearchKeyword = this.filterKeyword;
        const regs = params['regions'] || '';
        this.filterRegionIds = regs ? regs.split(',') : [];
        this.filterCategory = params['category'] || '';
        this.filterFormat = params['format'] || '';
        
        const maxP = params['maxPrice'];
        this.filterMaxPrice = maxP ? parseInt(maxP, 10) : 25000;

        const minP = params['minPrice'];
        this.filterMinPrice = minP ? parseInt(minP, 10) : 0;
        
        this.filterSortBy = params['sortBy'] || '';
        this.currentPage = +(params['page'] || 1);
        this.fetchEvents();
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  // Active Location Name getter helper for Navbar
  public get currentLocationName(): string {
    const activeId = this.activeUserRegionId();
    const found = this.regions().find(r => r.region_Id === activeId);
    return found ? found.name : 'Chennai';
  }

  private loadCategories(): void {
    this.eventService.getCategories().subscribe({
      next: (cats) => this.categories.set(cats),
      error: () => {
        // Fallback to defaults if API fails
        this.categories.set(['Tech', 'Conference', 'Music', 'Sports', 'Workshop', 'Education', 'Arts', 'Food', 'Wellness']);
      }
    });
  }

  public fetchEvents(): void {
    // Only filter by region if explicitly selected in the UI sidebar
    const regionIdParam = this.filterRegionIds.length > 0 ? this.filterRegionIds.join(',') : undefined;

    // Jump to top when page changes (no animation per user request)
    if (this.previousPage !== this.currentPage) {
      window.scrollTo({ top: 0, behavior: 'auto' });
      this.previousPage = this.currentPage;
    }

    this.eventService.browseEvents({
      keyword: this.filterKeyword || undefined,
      category: this.filterCategory || undefined,
      regionId: regionIdParam,
      format: this.filterFormat || undefined,
      maxPrice: this.filterMaxPrice || undefined,
      sortBy: this.filterSortBy || undefined,
      page: this.currentPage,
      size: this.pageSize
    }).subscribe(result => {
      this.totalPages = result.totalPages || 1;
      this.totalEvents.set(result.totalCount || 0);
    });
  }


  // Action Apply Filters
  public applyFilters(): void {
    this.filterKeyword = this.navSearchKeyword;
    this.currentPage = 1;
    this.updateQueryParams();
  }

  // Clear Filters
  public clearFilters(): void {
    this.filterKeyword = '';
    this.navSearchKeyword = '';
    this.filterRegionIds = [];
    this.filterCategory = '';
    this.filterFormat = '';
    this.filterMaxPrice = 25000;
    this.filterMinPrice = 0;
    this.filterSortBy = '';
    this.currentPage = 1;
    this.updateQueryParams();
  }

  // Checkbox region filters logic
  public toggleRegionFilter(regionId: string): void {
    const index = this.filterRegionIds.indexOf(regionId);
    if (index > -1) {
      this.filterRegionIds.splice(index, 1);
    } else {
      this.filterRegionIds.push(regionId);
    }
  }

  public isRegionFilterChecked(regionId: string): boolean {
    return this.filterRegionIds.includes(regionId);
  }

  // Chip removers for top filter summary
  public removeKeywordFilter(): void {
    this.filterKeyword = '';
    this.navSearchKeyword = '';
    this.applyFilters();
  }

  public removeCategoryFilter(): void {
    this.filterCategory = '';
    this.applyFilters();
  }

  public removeFormatFilter(): void {
    this.filterFormat = '';
    this.applyFilters();
  }

  public removeMaxPriceFilter(): void {
    this.filterMaxPrice = 25000;
    this.filterMinPrice = 0;
    this.applyFilters();
  }

  public removeRegionFilter(regionId: string): void {
    const index = this.filterRegionIds.indexOf(regionId);
    if (index > -1) {
      this.filterRegionIds.splice(index, 1);
      this.applyFilters();
    }
  }

  public getSelectedRegionName(regionId: string): string {
    const found = this.regions().find(r => r.region_Id === regionId);
    return found ? found.name : regionId;
  }

  private updateQueryParams(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        keyword: this.filterKeyword || null,
        regions: this.filterRegionIds.length > 0 ? this.filterRegionIds.join(',') : null,
        category: this.filterCategory || null,
        format: this.filterFormat || null,
        minPrice: (this.filterMinPrice !== 0) ? this.filterMinPrice : null,
        maxPrice: (this.filterMaxPrice !== 25000) ? this.filterMaxPrice : null,
        sortBy: this.filterSortBy || null,
        page: this.currentPage
      },
      queryParamsHandling: 'merge'
    });
  }

  // Pagination triggers
  public nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.updateQueryParams();
    }
  }

  public prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.updateQueryParams();
    }
  }

  // Navigation handlers
  public toggleProfileDropdown(event: Event): void {
    event.stopPropagation();
    this.isProfileDropdownOpen.update(v => !v);
  }

  public closeDropdowns(): void {
    this.isProfileDropdownOpen.set(false);
  }

  // Modal open/close actions
  public openLocationModal(event?: Event): void {
    if (event) event.stopPropagation();
    this.isLocationModalOpen.set(true);
  }

  public closeLocationModal(): void {
    this.isLocationModalOpen.set(false);
  }

  public selectRegion(regionId: string, event?: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.regionService.setLocalRegion(regionId);
    if (this.isLoggedIn()) {
      this.authService.selectRegion(regionId).subscribe();
    }
  }

  public onLogout(): void {
    this.authService.logout();
    this.isProfileDropdownOpen.set(false);
  }

  public triggerBookingAction(): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/bookings']);
  }

  public triggerCreateEventAction(): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/create-event']);
  }

  public navigateToBookingFlow(eventObj: any): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/booking'], { 
      queryParams: { eventId: eventObj.event_Id },
      state: { event: eventObj }
    });
  }

  public triggerManageMyEventsAction(): void {
    alert('Navigating to manage my events...');
  }

  public triggerAccountSettingsAction(): void {
    this.router.navigate(['/settings']);
    this.isProfileDropdownOpen.set(false);
  }

  public triggerGetHelpAction(): void {
    this.router.navigate(['/help']);
    this.isProfileDropdownOpen.set(false);
  }

  public onMinPriceChange(): void {
    if (this.filterMinPrice > this.filterMaxPrice) {
      this.filterMinPrice = this.filterMaxPrice;
    }
    this.applyFilters();
  }

  public onMaxPriceChange(): void {
    if (this.filterMaxPrice < this.filterMinPrice) {
      this.filterMaxPrice = this.filterMinPrice;
    }
    this.applyFilters();
  }
}
