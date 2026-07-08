import { Component, OnInit, OnDestroy, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AppStoreService } from '../../../store/app-store.service';
import { RegionService } from '../../../services/region.service';
import { AuthService } from '../../../services/auth.service';
import { WikipediaImageService } from '../../../services/wikipedia-image.service';
import { RegionPopularResponse } from '../../../models/event.model';

@Component({
  selector: 'app-popular-regions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './popular-regions.html',
  styleUrl: './popular-regions.css'
})
export class PopularRegionsComponent implements OnInit, OnDestroy {
  public isLoggedIn = signal(false);
  public homeRegions = signal<RegionPopularResponse[]>([]);
  public regionImages = signal<Map<string, string | null>>(new Map());
  public imageErrors = signal<Map<string, boolean>>(new Map());
  public isLoadingRegions = signal(true);
  private subscriptions: Subscription = new Subscription();
  private currentLoadingRegionsString = '';
  private currentLimit = 4;

  constructor(
    private store: AppStoreService,
    private regionService: RegionService,
    private authService: AuthService,
    private wikiService: WikipediaImageService,
    private router: Router
  ) {}

  @HostListener('window:resize')
  public onResize(): void {
    this.updateRegionsLimit();
  }

  private calculateRegionsLimit(): number {
    if (typeof window === 'undefined') return 4;
    return window.innerWidth > 1024 ? 4 : 2;
  }

  private updateRegionsLimit(): void {
    const newLimit = this.calculateRegionsLimit();
    if (newLimit !== this.currentLimit) {
      this.currentLimit = newLimit;
      this.regionService.getPopularRegions(newLimit).subscribe();
    }
  }

  ngOnInit(): void {
    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(logged => this.isLoggedIn.set(logged))
    );

    this.currentLimit = this.calculateRegionsLimit();
    this.regionService.getPopularRegions(this.currentLimit).subscribe({
      next: () => this.isLoadingRegions.set(false),
      error: () => this.isLoadingRegions.set(false)
    });

    this.subscriptions.add(
      this.store.select(state => state.regions.popularItems).subscribe(popular => {
        if (!popular) return; // Wait until populated
        
        const regionsKey = popular.map(r => r.region_Id).join(',');
        if (regionsKey && regionsKey === this.currentLoadingRegionsString) return;
        
        this.currentLoadingRegionsString = regionsKey;
        this.homeRegions.set(popular);
        this.loadImages(popular);
      })
    );
  }

  private loadImages(regions: RegionPopularResponse[]): void {
    const inputRegions = regions.map(r => ({ id: r.region_Id, name: r.region_Name }));

    this.wikiService.preloadImages(inputRegions, (regionId, url) => {
      const current = new Map(this.regionImages());
      current.set(regionId, url);
      this.regionImages.set(current);
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  public getRegionImage(regionId: string): string | null {
    return this.regionImages().get(regionId) || null;
  }

  public onImageError(regionId: string): void {
    const errors = new Map(this.imageErrors());
    errors.set(regionId, true);
    this.imageErrors.set(errors);
  }

  public hasImageError(regionId: string): boolean {
    return !!this.imageErrors().get(regionId);
  }

  public onPopularRegionSelect(regionId: string): void {
    this.regionService.setLocalRegion(regionId);
    if (this.isLoggedIn()) {
      this.authService.selectRegion(regionId).subscribe();
    }
    setTimeout(() => {
      this.router.navigate(['/browse'], { queryParams: { regionId } });
    }, 100);
  }
}
