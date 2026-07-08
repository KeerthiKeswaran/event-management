import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { StripeCheckoutComponent } from './stripe-checkout';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { StripeService } from 'ngx-stripe';
import { BookingService } from '../../../services/booking.service';
import { EventService } from '../../../services/event.service';
import { of, Subject } from 'rxjs';
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';

describe('StripeCheckoutComponent', () => {
  let component: StripeCheckoutComponent;
  let fixture: ComponentFixture<StripeCheckoutComponent>;
  let stripeService: StripeService;
  let bookingService: BookingService;
  let eventService: EventService;
  let router: Router;

  beforeEach(async () => {
    // Generate a simulated creation time 1 minute ago so timer logic processes accurately
    const createdTimeStr = new Date(Date.now() - 60000).toISOString(); 

    await TestBed.configureTestingModule({
      imports: [StripeCheckoutComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { 
            queryParams: of({ 
              clientSecret: 'secret_123', 
              createdAt: createdTimeStr, 
              type: 'booking', 
              id: '99' 
            }) 
          }
        },
        {
          provide: StripeService,
          useValue: { 
            initEmbeddedCheckout: vi.fn().mockReturnValue(of({ mount: vi.fn(), destroy: vi.fn() })) 
          }
        },
        {
          provide: BookingService,
          useValue: { revertBooking: vi.fn().mockReturnValue(of({})) }
        },
        {
          provide: EventService,
          useValue: { revertEvent: vi.fn().mockReturnValue(of({})) }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(StripeCheckoutComponent);
    component = fixture.componentInstance;
    stripeService = TestBed.inject(StripeService);
    bookingService = TestBed.inject(BookingService);
    eventService = TestBed.inject(EventService);
    router = TestBed.inject(Router);
    fixture.detectChanges(); // Triggers ngOnInit and ngAfterViewInit
  });

  afterEach(() => {
    vi.clearAllTimers();
  });

  it('should create and initialize embedded checkout', () => {
    expect(component).toBeTruthy();
    expect(component.clientSecret()).toBe('secret_123');
    expect(stripeService.initEmbeddedCheckout).toHaveBeenCalledWith({ clientSecret: 'secret_123' });
  });

  it('should process the countdown timer correctly', async () => {
    // Since we passed a date 60 seconds ago in beforeEach, the remaining time should be roughly 240 seconds (300 - 60)
    // Account for slight delays
    const remaining = component.remainingSeconds();
    expect(remaining).toBeLessThanOrEqual(241);
    expect(remaining).toBeGreaterThanOrEqual(239);
    
    // Check formatted time (e.g. "04:00")
    const formatted = component.formattedTime();
    expect(formatted.startsWith('04:') || formatted.startsWith('03:')).toBeTruthy();
  });

  it('should call revert booking API when leave is confirmed', () => {
    const revertSpy = vi.spyOn(bookingService, 'revertBooking');
    
    // Simulate confirming cancel modal
    component.confirmLeave();
    
    expect(component.isCancelled()).toBeTruthy();
    expect(component.showCancelModal()).toBeFalsy();
    expect(revertSpy).toHaveBeenCalledWith(99);
  });

  it('canDeactivate should return true if already cancelled or navigating to checkout', async () => {
    component.isCancelled.set(true);
    const result = await component.canDeactivate({ url: '/browse' });
    expect(result).toBeTruthy();
    
    component.isCancelled.set(false);
    const result2 = await component.canDeactivate({ url: '/checkout' });
    expect(result2).toBeTruthy();
  });

  it('canDeactivate should return Promise and show modal if attempting to leave normally', () => {
    const deactivatePromise = component.canDeactivate({ url: '/browse' }) as Promise<boolean>;
    
    expect(component.showCancelModal()).toBeTruthy();
    
    // Complete promise by calling cancelLeave (user decides to stay)
    component.cancelLeave();
    
    return deactivatePromise.then((canLeave: boolean) => {
      expect(canLeave).toBeFalsy();
      expect(component.showCancelModal()).toBeFalsy();
    });
  });
});
