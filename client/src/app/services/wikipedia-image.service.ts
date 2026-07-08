import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, from } from 'rxjs';
import { map, catchError, switchMap } from 'rxjs/operators';

/**
 * WikipediaImageService
 * ---------------------
 * Fetches the primary landmark thumbnail for any city/region using the
 * Wikipedia REST API. Results (just the URL string) are persisted in
 * localStorage so each city is fetched at most ONCE per browser, forever.
 *
 * No API key. No rate limits. Wikipedia CDN handles all load.
 */
@Injectable({
  providedIn: 'root'
})
export class WikipediaImageService {
  private http = inject(HttpClient);

  /** localStorage single key */
  private readonly CACHE_KEY = 'cachedImages';
  /** Sentinel stored when Wikipedia returned no image, to avoid re-querying */
  private readonly NOT_FOUND = '__NOT_FOUND__';

  // ─── Cache helpers ────────────────────────────────────────────────────────

  private readCacheMap(): Record<string, { id: number; dataUrl: string | null }> {
    if (typeof window === 'undefined') return {};
    const val = localStorage.getItem(this.CACHE_KEY);
    if (!val) return {};
    try {
      return JSON.parse(val);
    } catch {
      return {};
    }
  }

  private writeCacheMap(map: Record<string, { id: number; dataUrl: string | null }>): void {
    if (typeof window === 'undefined') return;
    try {
      localStorage.setItem(this.CACHE_KEY, JSON.stringify(map));
    } catch {
      // localStorage full
    }
  }

  private readFromCache(regionId: string): { id: number; dataUrl: string | null } | undefined {
    const map = this.readCacheMap();
    return map[regionId];
  }

  private writeToCache(regionId: string, pageId: number, url: string | null): void {
    const map = this.readCacheMap();
    map[regionId] = { id: pageId, dataUrl: url };
    this.writeCacheMap(map);
  }

  // ─── Public API ───────────────────────────────────────────────────────────

  getRegionImage(regionId: string, cityName: string): Observable<string | null> {
    if (typeof window === 'undefined') return of(null);

    const cached = this.readFromCache(regionId);
    if (cached !== undefined) return of(cached.dataUrl);   // instant cache hit

    const endpoint = `https://en.wikipedia.org/w/api.php`;
    const params = {
      action: 'query',
      generator: 'search',
      gsrsearch: cityName,
      gsrlimit: '5',
      prop: 'pageimages',
      pithumbsize: '400',
      format: 'json',
      origin: '*'          // required for CORS
    };

    return this.http.get<any>(endpoint, { params }).pipe(
      map(response => {
        const pages = response?.query?.pages;
        if (!pages) {
          this.writeToCache(regionId, 0, null);
          return null;
        }

        const pagesArray = Object.values(pages) as any[];
        pagesArray.sort((a, b) => (a.index || 99) - (b.index || 99));
        
        const bestPage = pagesArray.find(p => p.thumbnail?.source);
        const url = bestPage?.thumbnail?.source ?? null;
        const pageId = bestPage?.pageid ?? 0;
        
        this.writeToCache(regionId, pageId, url);
        return url as string | null;
      }),
      catchError(() => {
        this.writeToCache(regionId, 0, null);
        return of(null);
      })
    );
  }

  async preloadImages(
    regions: { id: string; name: string }[],
    onUpdate: (regionId: string, url: string | null) => void
  ): Promise<void> {
    for (const region of regions) {
      const cached = this.readFromCache(region.id);
      if (cached !== undefined) {
        onUpdate(region.id, cached.dataUrl);
        continue;
      }

      try {
        const { firstValueFrom } = await import('rxjs');
        const url = await firstValueFrom(this.getRegionImage(region.id, region.name));
        onUpdate(region.id, url);
      } catch {
        onUpdate(region.id, null);
      }

      await new Promise(r => setTimeout(r, 100));
    }
  }
}
