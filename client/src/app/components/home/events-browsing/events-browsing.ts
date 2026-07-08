import { Component, OnInit, OnDestroy, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AppStoreService } from '../../../store/app-store.service';
import { EventService } from '../../../services/event.service';
import { BrowsedEventResponse } from '../../../models/event.model';
import { RegionModel } from '../../../models/region.model';

@Component({
  selector: 'app-events-browsing',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './events-browsing.html',
  styleUrl: './events-browsing.css'
})
export class EventsBrowsingComponent implements OnInit, OnDestroy {
  public searchKeyword = signal('');
  public selectedRegionId = signal('REG01');
  public isLoggedIn = signal(false);
  public events = signal<BrowsedEventResponse[]>([]);
  public localEvents = signal<BrowsedEventResponse[]>([]);
  public otherRegionEvents = signal<BrowsedEventResponse[]>([]);
  public regions = signal<RegionModel[]>([]);
  public eventsLoading = signal(false);

  private subscriptions: Subscription = new Subscription();
  private allPopularEvents: BrowsedEventResponse[] = [];
  private currentLimit = 3;

  constructor(
    private store: AppStoreService,
    private eventService: EventService,
    private router: Router
  ) {}

  @HostListener('window:resize')
  public onResize(): void {
    this.updateEventsLimit();
  }

  private calculateEventsLimit(): number {
    if (typeof window === 'undefined') return 3;
    const w = window.innerWidth;
    if (w >= 1600) return 4;
    if (w >= 1200) return 3;
    return 2; // minimum is 2 for sure
  }

  private updateEventsLimit(): void {
    const newLimit = this.calculateEventsLimit();
    if (newLimit !== this.currentLimit) {
      this.currentLimit = newLimit;
      this.loadPopularEvents();
    }
  }

  ngOnInit(): void {
    this.currentLimit = this.calculateEventsLimit();

    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(logged => this.isLoggedIn.set(logged))
    );
    this.subscriptions.add(
      this.store.select(state => state.events.items).subscribe(evs => {
        this.events.set(evs);
        this.updateSplitEvents(evs, this.selectedRegionId());
      })
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.items).subscribe(regs => {
        this.regions.set(regs);
        // Load popular events once regions are resolved so activeRegion name matching is accurate
        this.loadPopularEvents();
      })
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.currentRegionId).subscribe(regId => {
        const activeRegionId = regId || 'REG01';
        this.selectedRegionId.set(activeRegionId);
        this.updateSplitEvents(this.events(), activeRegionId);
      })
    );
    this.subscriptions.add(
      this.store.select(state => state.events.loading).subscribe(loading => this.eventsLoading.set(loading))
    );
  }

  private loadPopularEvents(): void {
    this.eventService.getPopularEvents(this.currentLimit).subscribe(evs => {
      this.allPopularEvents = evs;
      this.updateSplitEvents(this.events(), this.selectedRegionId());
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private updateSplitEvents(evs: BrowsedEventResponse[], activeRegionId: string): void {
    if (!evs) return;

    // Resolve the active region's name from the store for name-based comparison
    const activeRegion = this.regions().find(r => r.region_Id === activeRegionId);
    const activeRegionName = activeRegion?.name?.toLowerCase() ?? '';

    // Split events: local = venue region name matches active region, other = doesn't
    const local = evs.filter(e =>
      activeRegionName && (e.venue_Region_Name?.toLowerCase() === activeRegionName)
    );
    this.localEvents.set(local.slice(0, 3));

    // For other regions, filter the popular events to exclude current active region
    const other = this.allPopularEvents.filter(e =>
      !activeRegionName || (e.venue_Region_Name?.toLowerCase() !== activeRegionName)
    );
    this.otherRegionEvents.set(other.slice(0, this.currentLimit));
  }

  public get currentLocationName(): string {
    const activeId = this.selectedRegionId();
    const found = this.regions().find(r => r.region_Id === activeId);
    return found ? found.name : 'Chennai';
  }

  public triggerBookingAction(): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/bookings']);
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

  public onSearchSubmit(event?: Event): void {
    if (event) event.preventDefault();
    this.router.navigate(['/browse'], {
      queryParams: {
        keyword: this.searchKeyword(),
        regionId: this.selectedRegionId()
      }
    });
  }

  public clearFilters(): void {
    this.searchKeyword.set('');
    this.onSearchSubmit();
  }
}
