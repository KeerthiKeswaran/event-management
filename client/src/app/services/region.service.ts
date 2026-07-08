import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { map } from 'rxjs/operators';
import { AppStoreService } from '../store/app-store.service';
import { ActionTypes } from '../store/actions/app.actions';
import { RegionModel } from '../models/region.model';
import { RegionPopularResponse } from '../models/event.model';
import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class RegionService {
  private readonly baseUrl = environment.apiUrl;

  constructor(
    private http: HttpClient,
    private store: AppStoreService
  ) {}

  private extractArray(data: any): any[] {
    if (!data) return [];
    if (Array.isArray(data)) return data;
    return data.$values || data.items || data.Items || [];
  }

  // GET /api/regions
  // Returns: RegionResponse[] → { Region_Id, Region_Name, No_Of_Staffs }
  // Maps to client RegionModel: { region_Id, name }
  public loadRegions(): Observable<RegionModel[]> {
    this.store.dispatch({ type: ActionTypes.LOAD_REGIONS_START });

    return this.http.get<any[]>(`${this.baseUrl}/regions`).pipe(
      map((regions) =>
        this.extractArray(regions).map(r => ({
          region_Id: r.region_Id,
          name: r.region_Name
        }))
      ),
      tap((mapped) => {
        this.store.dispatch({
          type: ActionTypes.LOAD_REGIONS_SUCCESS,
          payload: mapped
        });
      }),
      catchError((err) => {
        this.store.dispatch({
          type: ActionTypes.LOAD_REGIONS_FAIL,
          payload: err.message || 'Failed to load regions'
        });
        return throwError(() => err);
      })
    );
  }

  // GET /api/regions/popular?rankNumber={n}
  // Returns: RegionResponse[] → { Region_Id, Region_Name, No_Of_Staffs }
  public getPopularRegions(rankNumber?: number): Observable<RegionPopularResponse[]> {
    this.store.dispatch({ type: ActionTypes.LOAD_POPULAR_REGIONS_START });

    const params = rankNumber ? `?rankNumber=${rankNumber}` : '';
    return this.http.get<any[]>(`${this.baseUrl}/regions/popular${params}`).pipe(
      map((regions) =>
        this.extractArray(regions).map(r => ({
          region_Id: r.region_Id,
          region_Name: r.region_Name,
          no_Of_Staffs: r.no_Of_Staffs
        }))
      ),
      tap((mapped) => {
        this.store.dispatch({
          type: ActionTypes.LOAD_POPULAR_REGIONS_SUCCESS,
          payload: mapped
        });
      }),
      catchError((err) => {
        this.store.dispatch({
          type: ActionTypes.LOAD_POPULAR_REGIONS_FAIL,
          payload: err.message || 'Failed to load popular regions'
        });
        return throwError(() => err);
      })
    );
  }

  public setLocalRegion(regionId: string): void {
    this.store.dispatch({
      type: ActionTypes.SET_REGION,
      payload: regionId
    });
    if (typeof window !== 'undefined') {
      localStorage.setItem('currentRegionId', regionId);
    }
  }
}
