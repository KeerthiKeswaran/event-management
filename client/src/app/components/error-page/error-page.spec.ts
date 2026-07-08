import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ErrorPageComponent } from './error-page';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { environment } from '../../../environments/environment';

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
describe('ErrorPageComponent', () => {
  let component: ErrorPageComponent;
  let fixture: ComponentFixture<ErrorPageComponent>;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ErrorPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'home', component: ErrorPageComponent }]),
        {
          provide: ActivatedRoute,
          useValue: {
            queryParams: of({ code: '404', returnUrl: '/home' })
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ErrorPageComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create and set error code from query params', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(component.errorCode()).toBe(404);
    
    // the component fires a checkServer() on init
    const req = httpMock.expectOne(`${environment.apiUrl}/Health`);
    expect(req.request.method).toBe('GET');
    req.flush('Ok'); // flush to avoid pending requests
  });

  it('should auto-redirect when server is online', async () => {
    const navigateSpy = vi.spyOn(router, 'navigateByUrl');
    fixture.detectChanges();
    
    // Initial check
    const req = httpMock.expectOne(`${environment.apiUrl}/Health`);
    req.flush('Ok');
    
    expect(navigateSpy).toHaveBeenCalledWith('/home');
  });

  it('should not redirect if server is offline during polling', async () => {
    const navigateSpy = vi.spyOn(router, 'navigateByUrl');
    fixture.detectChanges();
    
    // Initial check
    let req = httpMock.expectOne(`${environment.apiUrl}/Health`);
    req.error(new ProgressEvent('error'));
    
    expect(navigateSpy).not.toHaveBeenCalled();
    
    // Wait for next polling interval (2000ms)
    await new Promise(r => setTimeout(r, 2100));
    req = httpMock.expectOne(`${environment.apiUrl}/Health`);
    req.error(new ProgressEvent('error'));
    
    expect(navigateSpy).not.toHaveBeenCalled();
  });
});
