import { Component, OnInit, OnDestroy, signal, inject } from '@angular/core';
import { Subscription } from 'rxjs';
import { AppStoreService } from '../../store/app-store.service';
import { RegionService } from '../../services/region.service';
import { EventService } from '../../services/event.service';
import { LocationGeoService } from '../../services/location-geo.service';
import { NavbarComponent } from './navbar/navbar';
import { HeroCarouselComponent } from './hero-carousel/hero-carousel';
import { EventsBrowsingComponent } from './events-browsing/events-browsing';
import { PopularRegionsComponent } from './popular-regions/popular-regions';
import { AboutFaqComponent } from './about-faq/about-faq';
import { FooterComponent } from './footer/footer';
import { LocationModalComponent } from './location-modal/location-modal';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    NavbarComponent,
    HeroCarouselComponent,
    EventsBrowsingComponent,
    PopularRegionsComponent,
    AboutFaqComponent,
    FooterComponent,
    LocationModalComponent
  ],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class HomeComponent implements OnInit, OnDestroy {
  public isLocationModalOpen = signal(false);
  private subscriptions = new Subscription();

  private locationGeoService = inject(LocationGeoService);

  constructor(
    private store: AppStoreService,
    private regionService: RegionService,
    private eventService: EventService
  ) {}

  ngOnInit(): void {
    // 1. Load all regions (populates the location modal list and popular-regions section)
    this.regionService.loadRegions().subscribe();

    // 2. Load trending events (for HeroCarouselComponent)
    this.eventService.getTrendingEvents(5).subscribe();

    // 4. Listen to changes in the active region to load browse events
    this.subscriptions.add(
      this.store.select(state => state.regions.currentRegionId).subscribe(regId => {
        const activeRegionId = regId || 'REG01';
        this.eventService.browseEvents({
          regionId: activeRegionId,
          page: 1,
          size: 24
        }).subscribe();
      })
    );

    // 5. Load recommended events if user is logged in (auth-only endpoint)
    const token = this.store.state.auth.token;
    if (token) {
      this.eventService.getRecommendedEvents().subscribe();
    }

    // 6. Auto-trigger location modal & permission prompt if user just registered
    if (typeof window !== 'undefined' && localStorage.getItem('justRegistered') === 'true') {
      localStorage.removeItem('justRegistered');
      this.isLocationModalOpen.set(true);

      // Prompt for Geolocation permission in the browser
      this.locationGeoService.requestAndSyncLocation()
        .then(() => {
          // If user grants permission & location is resolved, close the modal
          this.isLocationModalOpen.set(false);
        })
        .catch((err) => {
          console.warn('Auto-geolocation permission request failed/denied:', err);
          // Keep the modal open so the user can choose a location manually
        });
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  public openLocationModal(): void {
    this.isLocationModalOpen.set(true);
  }

  public closeLocationModal(): void {
    this.isLocationModalOpen.set(false);
  }
}
