import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RaiseTicketComponent } from './raise-ticket';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach } from 'vitest';

describe('RaiseTicketComponent', () => {
  let component: RaiseTicketComponent;
  let fixture: ComponentFixture<RaiseTicketComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RaiseTicketComponent],
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

    fixture = TestBed.createComponent(RaiseTicketComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    // Basic test case to check component creation
    expect(component).toBeTruthy();
  });
});
