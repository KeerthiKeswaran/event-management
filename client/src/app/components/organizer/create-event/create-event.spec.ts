import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { CreateEventComponent } from './create-event';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { EventService } from '../../../services/event.service';
import { AuthService } from '../../../services/auth.service';
import { StripeService } from 'ngx-stripe';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { By } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';

describe('CreateEventComponent', () => {
  let component: CreateEventComponent;
  let fixture: ComponentFixture<CreateEventComponent>;
  let eventService: EventService;
  let authService: AuthService;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateEventComponent, FormsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { queryParams: of({}) }
        },
        {
          provide: EventService,
          useValue: {
            getVenues: vi.fn().mockReturnValue(of([
              { venue_Id: 1, name: 'Main Hall', hourly_Price: 1000, capacity: 500, seatTiers: [{ total_Seats: 500, tier_Name: 'General' }] }
            ])),
            getCategories: vi.fn().mockReturnValue(of(['Tech', 'Music'])),
            getAgeCategories: vi.fn().mockReturnValue(of([{ key: 'ALL', display: 'Unrestricted' }])),
            getPlatformSettings: vi.fn().mockReturnValue(of({ virtual_Event_Activation_Fee: 500, physical_Event_Activation_Fee: 2000, gsT_Percentage: 18 })),
            checkStaffAvailability: vi.fn().mockReturnValue(of({ requiredStaffCount: 5, staffingCost: 5000 })),
            initiateCreateEvent: vi.fn(),
            uploadDescription: vi.fn().mockReturnValue(of({ url: 'desc_url' })),
            uploadImage: vi.fn().mockReturnValue(of({ url: 'img_url' })),
            createEvent: vi.fn().mockReturnValue(of({ event_Id: 99 })),
            createCheckoutSession: vi.fn().mockReturnValue(of({ clientSecret: 'sec', createdAtUTC: 'time' }))
          }
        },
        {
          provide: AuthService,
          useValue: {
            getConsentDocument: vi.fn().mockReturnValue(of({ termsId: 'term-123', filePath: '/dummy' }))
          }
        },
        { provide: StripeService, useValue: { initEmbeddedCheckout: vi.fn() } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CreateEventComponent);
    component = fixture.componentInstance;
    eventService = TestBed.inject(EventService);
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create and load initial lists', () => {
    expect(component).toBeTruthy();
    expect(eventService.getVenues).toHaveBeenCalled();
    expect(eventService.getCategories).toHaveBeenCalled();
    expect(eventService.getAgeCategories).toHaveBeenCalled();
    
    expect(component.venuesList().length).toBe(1);
    expect(component.categoriesList().length).toBe(2);
    expect(component.category).toBe('Tech'); // Auto-selected first
    expect(component.ageCategory).toBe('ALL');
  });

  it('should validate empty form correctly on submit details', async () => {
    // Clear auto-selected to ensure pure invalid state
    component.title = '';
    component.descriptionText = '';
    component.dateTime = '';
    component.eventType = 'Physical';
    component.venueId = null;

    await component.onSubmitDetails();

    expect(component.fieldError('title')).toContain('Event title is required');
    expect(component.fieldError('description')).toContain('Event description cannot be empty');
    expect(component.fieldError('dateTime')).toContain('Please select an event date');
    expect(component.fieldError('venue')).toContain('Please select a venue');
  });

  it('should reset venue and staff when changing to Virtual event type', () => {
    component.venueId = 1;
    component.requiresStaff = true;
    component.ticketTiers = [{ tierName: 'VIP', price: 1000, capacity: 50 }];

    component.eventType = 'Virtual';
    component.onEventTypeChange();

    expect(component.venueId).toBeNull();
    expect(component.requiresStaff).toBeFalsy();
    expect(component.ticketTiers.length).toBe(0);
  });

  it('should correctly calculate total fees for physical events', () => {
    component.eventType = 'Physical';
    component.venueId = 1; // 1000 hourly price
    component.durationHours = 2; // 2000 total venue rental
    component.requiresStaff = true;
    component.estimatedStaffCost.set(5000);
    // Platform physical fee: 2000
    // Total Base: 2000 (activation) + 2000 (venue) + 5000 (staff) = 9000
    // GST: 18% of 9000 = 1620
    // Total: 10620

    expect(component.baseTotalFees).toBe(9000);
    expect(component.gstAmount).toBe(1620);
    expect(component.totalFees).toBe(10620);
  });

  it('should proceed to review phase if form is valid', async () => {
    component.title = 'My Valid Event';
    component.descriptionText = 'Description';
    component.category = 'Tech';
    component.ageCategory = 'ALL';
    component.dateTime = '2026-10-10T10:00';
    component.eventType = 'Virtual';
    component.acceptPolicy = true;

    await component.onSubmitDetails();
    
    // Check if review modal is opened instead of throwing errors
    expect(component.formErrors()).toEqual({});
    expect(component.showReviewModal()).toBeTruthy();
  });
});
