import { Component, OnInit, OnDestroy, Input, Output, EventEmitter, signal, computed, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { AppStoreService } from '../../../store/app-store.service';
import { AuthService } from '../../../services/auth.service';
import { RegionService } from '../../../services/region.service';
import { LocationGeoService } from '../../../services/location-geo.service';
import { WikipediaImageService } from '../../../services/wikipedia-image.service';
import { RegionModel } from '../../../models/region.model';

@Component({
  selector: 'app-location-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './location-modal.html'
})
export class LocationModalComponent implements OnInit, OnDestroy, OnChanges {
  @Input() isOpen: boolean = false;
  @Output() isOpenChange = new EventEmitter<boolean>();

  public locationSearchQuery = '';
  public searchLocationResults = signal<any[]>([]);
  public isSearchingLocation = signal(false);
  public showAllCities = signal(false);
  public isDetectingLocation = signal(false);

  public selectedRegionId = signal('REG01');
  public isLoggedIn = signal(false);
  public regions = signal<RegionModel[]>([]);
  public regionImages = signal<Map<string, string | null>>(new Map());
  
  public popularRegionIds = ['REG05', 'REG06', 'REG07', 'REG08', 'REG01'];
  
  public popularRegions = computed(() => {
    return this.popularRegionIds
      .map(id => this.regions().find(r => r.region_Id === id))
      .filter(r => !!r) as RegionModel[];
  });

  public nonPopularRegions = computed(() => {
    return this.regions().filter(r => !this.popularRegionIds.includes(r.region_Id));
  });

  private subscriptions: Subscription = new Subscription();
  private searchTimeout: any;

  constructor(
    private store: AppStoreService,
    private authService: AuthService,
    private regionService: RegionService,
    private locationGeoService: LocationGeoService,
    private wikiService: WikipediaImageService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && changes['isOpen'].currentValue) {
      this.showAllCities.set(false);
    }
  }

  ngOnInit(): void {
    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(logged => this.isLoggedIn.set(logged))
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.items).subscribe(regs => {
        this.regions.set(regs);
        // Pre-warm Wikipedia image cache for all regions as soon as we have them
        if (regs && regs.length > 0) {
          this.wikiService.preloadImages(
            regs.map(r => ({ id: r.region_Id, name: r.name })),
            (regionId, url) => {
              const current = new Map(this.regionImages());
              current.set(regionId, url);
              this.regionImages.set(current);
            }
          );
        }
      })
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.currentRegionId).subscribe(regId => {
        this.selectedRegionId.set(regId || 'REG01');
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }
  }

  public closeLocationModal(): void {
    this.isOpen = false;
    this.isOpenChange.emit(false);
    this.locationSearchQuery = '';
    this.searchLocationResults.set([]);
    this.isSearchingLocation.set(false);
  }

  public selectRegionFromModal(regionId: string): void {
    this.regionService.setLocalRegion(regionId);
    if (this.isLoggedIn()) {
      this.authService.selectRegion(regionId).subscribe();
    }
    this.closeLocationModal();
  }

  public detectLocationFromModal(): void {
    this.isDetectingLocation.set(true);
    this.locationGeoService.requestAndSyncLocation()
      .then((regionId) => {
        this.isDetectingLocation.set(false);
        this.closeLocationModal();
      })
      .catch((err) => {
        this.isDetectingLocation.set(false);
        console.warn('Geolocation failed:', err);
        alert('Could not auto-detect location. Please choose manually.');
      });
  }

  public onLocationSearch(): void {
    const query = this.locationSearchQuery.trim();
    if (!query) {
      this.searchLocationResults.set([]);
      this.isSearchingLocation.set(false);
      return;
    }
    
    // 1. Fetch local matching regions immediately
    const queryLower = query.toLowerCase();
    const localMatches = this.regions()
      .filter(r => r.name.toLowerCase().includes(queryLower))
      .map(r => {
        const coords = this.locationGeoService.getRegionCoordinates().find(c => c.regionId === r.region_Id);
        return {
          display_name: `${r.name}, India`,
          lat: coords ? coords.lat.toString() : '0',
          lon: coords ? coords.lng.toString() : '0',
          place_id: r.region_Id,
          isLocalRegion: true
        };
      });

    this.searchLocationResults.set(localMatches);

    // Clear previous timeout
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }

    this.isSearchingLocation.set(true);
    
    // 2. Fetch Nominatim results (restricted to India) debounced
    this.searchTimeout = setTimeout(() => {
      const url = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&countrycodes=in&featuretype=city&limit=5`;
      
      fetch(url, {
        headers: {
          'Accept': 'application/json'
        }
      })
        .then(res => {
          if (!res.ok) {
            throw new Error(`HTTP error! status: ${res.status}`);
          }
          return res.json();
        })
        .then(data => {
          this.isSearchingLocation.set(false);
          const externalResults = (data || []).map((item: any) => ({
            display_name: item.display_name,
            lat: item.lat,
            lon: item.lon,
            place_id: item.place_id,
            isLocalRegion: false
          }));

          // Merge local matches with external ones (avoiding duplicates by name)
          const combined = [...localMatches];
          externalResults.forEach((ext: any) => {
            const extName = ext.display_name.split(',')[0].trim().toLowerCase();
            const alreadyAdded = combined.some(loc => loc.display_name.split(',')[0].trim().toLowerCase() === extName);
            if (!alreadyAdded) {
              combined.push(ext);
            }
          });

          this.searchLocationResults.set(combined);
        })
        .catch(err => {
          this.isSearchingLocation.set(false);
          console.warn('Location search API error (CORS/Network):', err);
          // Fall back gracefully to our local matches (which are already in searchLocationResults)
        });
    }, 450);
  }

  public selectSearchedLocation(item: any): void {
    if (item.isLocalRegion) {
      this.selectRegionFromModal(item.place_id);
      return;
    }

    const lat = parseFloat(item.lat);
    const lon = parseFloat(item.lon);
    if (isNaN(lat) || isNaN(lon)) {
      this.closeLocationModal();
      return;
    }
    const nearestId = this.locationGeoService.findNearestRegion(lat, lon);
    this.regionService.setLocalRegion(nearestId);
    if (this.isLoggedIn()) {
      this.authService.selectRegion(nearestId).subscribe();
    }
    this.closeLocationModal();
  }
  public getRegionImage(regionId: string): string | null {
    return this.regionImages().get(regionId) ?? null;
  }

  public getRegionName(regionId: string): string {
    return this.regions().find(r => r.region_Id === regionId)?.name ?? regionId;
  }
}
