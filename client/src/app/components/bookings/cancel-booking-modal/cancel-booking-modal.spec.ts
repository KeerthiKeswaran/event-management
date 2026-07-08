import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CancelBookingModalComponent } from './cancel-booking-modal';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach } from 'vitest';

describe('CancelBookingModalComponent', () => {
  let component: CancelBookingModalComponent;
  let fixture: ComponentFixture<CancelBookingModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CancelBookingModalComponent],
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

    fixture = TestBed.createComponent(CancelBookingModalComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    // Basic test case to check component creation
    expect(component).toBeTruthy();
  });
});
