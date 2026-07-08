import { Pipe, PipeTransform, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, shareReplay } from 'rxjs/operators';
import { environment } from '../../environments/environment';


@Pipe({
  name: 'resolveDescription',
  standalone: true
})
export class ResolveDescriptionPipe implements PipeTransform {
  private http = inject(HttpClient);
  private static cache = new Map<string, Observable<string>>();

  transform(value: string | undefined): Observable<string> {
    if (!value) {
      return of('');
    }

    const trimmed = value.trim();
    const isRelative = trimmed.startsWith('/assets/') || trimmed.startsWith('assets/');
    if (!isRelative && !trimmed.startsWith('http://') && !trimmed.startsWith('https://')) {
      return of(value);
    }

    const cleanUrl = trimmed.startsWith('/') ? trimmed : '/' + trimmed;
    const url = isRelative ? `${environment.serverUrl}${cleanUrl}` : trimmed;

    if (ResolveDescriptionPipe.cache.has(url)) {
      return ResolveDescriptionPipe.cache.get(url)!;
    }

    const obs = this.http.get(url, { responseType: 'text' }).pipe(
      catchError(err => {
        console.error(`Failed to fetch description from ${url}`, err);
        return of('Failed to load description.');
      }),
      shareReplay(1)
    );

    ResolveDescriptionPipe.cache.set(url, obs);
    return obs;
  }
}
