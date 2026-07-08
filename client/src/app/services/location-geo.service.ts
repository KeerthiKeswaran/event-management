import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { AppStoreService } from '../store/app-store.service';
import { AuthService } from './auth.service';
import { RegionService } from './region.service';

interface RegionCoords {
  regionId: string;
  name: string;
  lat: number;
  lng: number;
}

@Injectable({
  providedIn: 'root'
})
export class LocationGeoService {
  // Pre-configured coordinates for Tamil Nadu and popular Indian cities
  private readonly regionCoordinates: RegionCoords[] = [
    { regionId: 'REG01', name: 'Chennai', lat: 13.0827, lng: 80.2707 },
    { regionId: 'REG02', name: 'Coimbatore', lat: 11.0168, lng: 76.9558 },
    { regionId: 'REG03', name: 'Madurai', lat: 9.9252, lng: 78.1198 },
    { regionId: 'REG04', name: 'Trichy', lat: 10.7905, lng: 78.7047 },
    { regionId: 'REG05', name: 'Mumbai', lat: 18.9750, lng: 72.8258 },
    { regionId: 'REG06', name: 'Delhi-NCR', lat: 28.6139, lng: 77.2090 },
    { regionId: 'REG07', name: 'Bengaluru', lat: 12.9716, lng: 77.5946 },
    { regionId: 'REG08', name: 'Hyderabad', lat: 17.3850, lng: 78.4867 },
    { regionId: 'REG09', name: 'Kochi', lat: 9.9312, lng: 76.2673 },
    { regionId: 'REG10', name: 'Kolkata', lat: 22.5726, lng: 88.3639 },
    { regionId: 'REG11', name: 'Pune', lat: 18.5204, lng: 73.8567 },
    { regionId: 'REG12', name: 'Ahmedabad', lat: 23.0225, lng: 72.5714 },
    { regionId: 'REG13', name: 'Chandigarh', lat: 30.7333, lng: 76.7794 },
    { regionId: 'REG14', name: 'Salem', lat: 11.6643, lng: 78.1460 },
    { regionId: 'REG15', name: 'Tirunelveli', lat: 8.7139, lng: 77.7567 }
  ];

  constructor(
    private store: AppStoreService,
    private authService: AuthService,
    private regionService: RegionService
  ) {}

  public getRegionCoordinates(): RegionCoords[] {
    return this.regionCoordinates;
  }

  public requestAndSyncLocation(): Promise<string> {
    return new Promise((resolve, reject) => {
      if (typeof window === 'undefined' || !navigator.geolocation) {
        reject('Geolocation is not supported by this browser.');
        return;
      }

      navigator.geolocation.getCurrentPosition(
        (position) => {
          const lat = position.coords.latitude;
          const lng = position.coords.longitude;
          const nearestRegionId = this.findNearestRegion(lat, lng);
          
          this.regionService.setLocalRegion(nearestRegionId);

          let tokenKey = 'user_token';
          if (typeof window !== 'undefined') {
            if (window.location.pathname.startsWith('/admin')) tokenKey = 'admin_token';
            else if (window.location.pathname.startsWith('/finance')) tokenKey = 'finance_token';
          }
          const token = typeof window !== 'undefined' ? localStorage.getItem(tokenKey) : null;
          if (token) {
            this.authService.selectRegion(nearestRegionId).subscribe({
              next: () => resolve(nearestRegionId),
              error: () => resolve(nearestRegionId)
            });
          } else {
            resolve(nearestRegionId);
          }
        },
        (error) => {
          reject(error.message);
        },
        { enableHighAccuracy: true, timeout: 5000, maximumAge: 0 }
      );
    });
  }

  public findNearestRegion(lat: number, lng: number): string {
    let minDistance = Infinity;
    let closestRegionId = 'REG01'; // Default to Chennai

    for (const region of this.regionCoordinates) {
      const distance = this.calculateDistance(lat, lng, region.lat, region.lng);
      if (distance < minDistance) {
        minDistance = distance;
        closestRegionId = region.regionId;
      }
    }
    return closestRegionId;
  }

  private calculateDistance(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const R = 6371;
    const dLat = this.deg2rad(lat2 - lat1);
    const dLon = this.deg2rad(lon2 - lon1);
    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos(this.deg2rad(lat1)) * Math.cos(this.deg2rad(lat2)) *
      Math.sin(dLon / 2) * Math.sin(dLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c;
  }

  private deg2rad(deg: number): number {
    return deg * (Math.PI / 180);
  }
}
