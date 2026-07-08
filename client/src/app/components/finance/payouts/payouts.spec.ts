import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PayoutsComponent } from './payouts';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach } from 'vitest';

describe('PayoutsComponent', () => {
  let component: PayoutsComponent;
  let fixture: ComponentFixture<PayoutsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PayoutsComponent],
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

    fixture = TestBed.createComponent(PayoutsComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    // Basic test case to check component creation
    expect(component).toBeTruthy();
  });
});
