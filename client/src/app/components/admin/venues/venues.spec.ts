import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AdminVenuesComponent } from './venues';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach } from 'vitest';

describe('AdminVenuesComponent', () => {
  let component: AdminVenuesComponent;
  let fixture: ComponentFixture<AdminVenuesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminVenuesComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { queryParams: of({}), params: of({}) }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminVenuesComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    // Basic test case to check component creation
    expect(component).toBeTruthy();
  });
});
