import { Component, OnInit, signal, computed, ViewChild, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { StripeCardComponent, StripeService } from 'ngx-stripe';
import { StripeCardElementOptions, StripeElementsOptions } from '@stripe/stripe-js';
import { firstValueFrom, Observable, Subscription } from 'rxjs';
import { AppStoreService } from '../../../store/app-store.service';
import { EventService } from '../../../services/event.service';
import { AuthService } from '../../../services/auth.service';
import { FooterComponent } from '../../home/footer/footer';
import { NavbarComponent } from '../../home/navbar/navbar';
import { environment } from '../../../../environments/environment';


@Component({
  selector: 'app-create-event',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, FormsModule, FooterComponent, NavbarComponent],
  templateUrl: './create-event.html',
  styleUrl: './create-event.css'
})
export class CreateEventComponent implements OnInit {
  private readonly STORAGE_KEY = 'createEventDraft';

  // Step 1: Form details, Step 2: Payment
  public currentStep = signal<'details' | 'payment'>('details');

  // Venues, Categories & Age Categories list
  public venuesList = signal<any[]>([]);
  public categoriesList = signal<string[]>([]);
  public ageCategoriesList = signal<{ key: string; display: string }[]>([]);

  // Form Model
  public title = '';
  public descriptionText = '';
  public imageFile: File | null = null;
  public imagePreviewUrl = signal<string | null>(null);
  public eventType = 'Physical';
  public category = 'Tech';
  public ageCategory = '';
  public dateTime = '';
  public durationHours = 2;
  public venueId: number | null = null;

  // Inline validation errors
  public formErrors = signal<Record<string, string>>({});

  private clearFormErrors(): void { this.formErrors.set({}); }
  private setFieldError(field: string, msg: string): void {
    this.formErrors.update(e => ({ ...e, [field]: msg }));
  }
  private clearFieldError(field: string): void {
    const errs = { ...this.formErrors() };
    delete errs[field];
    this.formErrors.set(errs);
  }
  public fieldError(field: string): string { return this.formErrors()[field] ?? ''; }
  
  // Venues enriched with total capacity from SeatTiers
  public venuesWithCapacity = computed(() =>
    this.venuesList().map(v => ({
      ...v,
      capacity: (v.seatTiers ?? v.SeatTiers ?? [])
        .reduce((sum: number, t: any) => sum + (t.total_Seats ?? t.Total_Seats ?? t.totalSeats ?? 0), 0)
    }))
  );

  public venueSearchKeyword = signal('');
  public isVenueDropdownOpen = signal(false);
  public isVenuesLoading = signal(false);

  public filteredVenues = computed(() => {
    const keyword = this.venueSearchKeyword().toLowerCase().trim();
    const allVenues = this.venuesWithCapacity();
    
    if (!keyword) {
      return allVenues;
    }
    
    return allVenues.filter(v => 
      (v.name && v.name.toLowerCase().includes(keyword)) ||
      (v.address && v.address.toLowerCase().includes(keyword)) ||
      (v.region_Id && v.region_Id.toLowerCase().includes(keyword)) ||
      (v.capacity && v.capacity.toString().includes(keyword)) ||
      (v.city && v.city.toLowerCase().includes(keyword))
    );
  });

  public selectVenue(venue: any): void {
    this.venueId = venue.venue_Id;
    this.isVenueDropdownOpen.set(false);
    this.venueSearchKeyword.set('');
    this.onVenueChange();
  }

  public getSelectedVenueName(): string {
     if (!this.venueId) return 'Select venue...';
     const v = this.venuesWithCapacity().find(x => x.venue_Id === this.venueId);
     return v ? `${v.name} (Capacity: ${v.capacity})` : 'Select venue...';
  }

  public toggleVenueDropdown(): void {
    this.isVenueDropdownOpen.update(open => !open);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
     const target = event.target as HTMLElement;
     if (!target.closest('.custom-venue-dropdown')) {
       this.isVenueDropdownOpen.set(false);
     }
  }

  // Staff allocation details
  public requiresStaff = false;
  public estimatedStaffCount = signal(0);
  public estimatedStaffCost = signal(0);
  public isStaffCalculationLoading = signal(false);
  public staffErrorMsg = signal('');

  // Ticket Tiers configured dynamically based on selection
  public ticketTiers: any[] = [];

  // Capacity validation getters
  get currentVenueCapacity(): number {
    if (!this.venueId) return 0;
    const v = this.venuesWithCapacity().find(x => x.venue_Id === this.venueId);
    return v ? v.capacity : 0;
  }

  get totalAllocatedCapacity(): number {
    return this.ticketTiers.reduce((sum, tier) => sum + (tier.capacity || 0), 0);
  }

  get isCapacityExceeded(): boolean {
    if (this.eventType === 'Virtual' || !this.venueId) return false;
    return this.totalAllocatedCapacity > this.currentVenueCapacity;
  }

  get isCapacityUnderutilized(): boolean {
    if (this.eventType === 'Virtual' || !this.venueId) return false;
    return this.totalAllocatedCapacity > 0 && this.totalAllocatedCapacity < this.currentVenueCapacity;
  }

  // Platform settings (fees, rates)
  public platformSettings = signal<any | null>(null);

  // Event policy
  public policyDocument = signal<any | null>(null);
  public acceptPolicy = false;
  public acceptedPolicyTermsId = signal('');
  public showPolicyModal = signal(false);
  public policyModalContent = signal('');
  public isLoadingPolicy = signal(false);

  // Response after Step 1 create API
  public createdEventId = signal<number | null>(null);

  // Review Modal & Revert API states
  public showReviewModal = signal(false);
  public isInitiatingEvent = signal(false);
  public isPaymentConfirmed = false;
  public pendingEventPayload: any = null;

  // Stripe Cancellation states
  public pendingCancelEventId = signal<number | null>(null);
  public showStripeCancelModal = signal(false);
  public isRevertingEvent = signal(false);

  // Stripe Payment Form Model
  @ViewChild(StripeCardComponent) card!: StripeCardComponent;
  
  public cardholderName = '';
  public isSubmittingPayment = signal(false);
  public showConfirmBackModal = signal(false);

  public cardOptions: StripeCardElementOptions = {
    style: {
      base: {
        color: '#121212',
        fontFamily: 'Outfit, sans-serif',
        fontSize: '16px',
        '::placeholder': {
          color: '#9ca3af'
        }
      }
    }
  };

  public elementsOptions: StripeElementsOptions = {
    locale: 'en'
  };

  // Virtual ticket price fetched from platform settings
  public virtualTicketPrice = computed(() => {
    const settings = this.platformSettings();
    return settings ? settings.virtual_Event_Activation_Fee ?? settings.Virtual_Event_Activation_Fee ?? null : null;
  });

  // Activation fees — plain getters so they react to plain property changes (eventType, durationHours)
  get activationFee(): number {
    const settings = this.platformSettings();
    if (!settings) return 0;
    const virtualFee  = settings.virtual_Event_Activation_Fee  ?? settings.Virtual_Event_Activation_Fee  ?? 0;
    const physicalFee = settings.physical_Event_Activation_Fee ?? settings.Physical_Event_Activation_Fee ?? 0;
    if (this.eventType === 'Virtual') return virtualFee;          // ₹500
    if (this.eventType === 'Hybrid')  return virtualFee + physicalFee; // ₹500 + ₹2000 = ₹2500
    return physicalFee;                                           // ₹2000
  }

  get selectedVenuePrice(): number {
    if (!this.venueId) return 0;
    const venue = this.venuesList().find(v => (v.venue_Id ?? v.Venue_Id) === Number(this.venueId));
    return venue ? (venue.hourly_Price ?? venue.Hourly_Price ?? 0) : 0;
  }

  get selectedVenueName(): string {
    if (!this.venueId) return '';
    const venue = this.venuesList().find(v => (v.venue_Id ?? v.Venue_Id) === Number(this.venueId));
    return venue ? (venue.name ?? venue.Name ?? '') : '';
  }

  get venueRentalCost(): number {
    return this.selectedVenuePrice * this.durationHours;
  }

  get baseTotalFees(): number {
    if (this.eventType === 'Virtual') return this.activationFee;
    return this.activationFee + this.venueRentalCost + this.estimatedStaffCost();
  }

  get gstAmount(): number {
    const settings = this.platformSettings();
    const gstRate = settings ? (settings.gsT_Percentage ?? settings.GST_Percentage ?? 18) / 100 : 0.18;
    return this.baseTotalFees * gstRate;
  }

  get totalFees(): number {
    return this.baseTotalFees + this.gstAmount;
  }

  constructor(
    private eventService: EventService,
    private authService: AuthService,
    private http: HttpClient,
    private stripeService: StripeService,
    private router: Router,
    private route: ActivatedRoute,
    private store: AppStoreService
  ) {}

  private subscriptions = new Subscription();
  public isSuccessTickAnimating = signal(false);
  public isRestricted = signal(false);

  ngOnInit(): void {
    this.subscriptions.add(
      this.route.queryParams.subscribe(params => {
        const sessionId = params['session_id'];
        const eventId = Number(params['eventId']);
        
        if (sessionId && eventId) {
          this.confirmStripeEvent(sessionId, eventId);
          return;
        }
        
        const canceled = params['canceled'];
        if (canceled === 'true' && eventId) {
          this.pendingCancelEventId.set(eventId);
          this.showStripeCancelModal.set(true);
          // Remove from URL so refresh doesn't trigger modal again
          this.router.navigate([], {
            queryParams: { canceled: null, eventId: null },
            queryParamsHandling: 'merge'
          });
          return;
        }
      })
    );

    this.subscriptions.add(
      this.store.select((state: any) => state.auth.user).subscribe((user: any) => {
        const restricted = user?.status === 'Restricted';
        this.isRestricted.set(restricted);
        if (restricted) {
          this.router.navigate(['/myevents']);
        }
      })
    );

    this.loadDraft();
    this.loadVenues();
    this.loadCategories();
    this.loadAgeCategories();
    this.loadPlatformSettings();
    this.loadPolicy();
  }

  private confirmStripeEvent(sessionId: string, eventId: number): void {
    this.isInitiatingEvent.set(true);
    this.currentStep.set('payment'); // Go to confirmation page
    this.isSuccessTickAnimating.set(true); // Show animation while verifying

    this.eventService.confirmEvent(eventId, sessionId, 'stripe_checkout').subscribe({
      next: (res) => {
        this.isPaymentConfirmed = true;
        this.clearDraft();
        this.isInitiatingEvent.set(false);
        
        // Remove params from URL
        this.router.navigate([], {
          queryParams: { session_id: null, eventId: null },
          queryParamsHandling: 'merge',
          replaceUrl: true
        });

        // After a small delay to let animation play a bit if API was too fast
        setTimeout(() => {
          this.isSuccessTickAnimating.set(false);
        }, 1500);
      },
      error: (err) => {
        this.isInitiatingEvent.set(false);
        this.isSuccessTickAnimating.set(false);
        this.currentStep.set('details');
        const msg = err?.error?.Message || err?.error?.message || err.message || '';
        if (msg.includes('already') || msg.includes('Live')) {
          this.router.navigate(['/myevents']);
        } else {
          alert(msg || 'Payment confirmation failed.');
        }
      }
    });
  }

  public draftStatus = signal('Saved to draft');
  private saveTimeout: any = null;

  public saveDraft(): void {
    this.draftStatus.set('Saving...');
    const draft = {
      title: this.title,
      descriptionText: this.descriptionText,
      eventType: this.eventType,
      category: this.category,
      ageCategory: this.ageCategory,
      dateTime: this.dateTime,
      durationHours: this.durationHours,
      venueId: this.venueId,
      requiresStaff: this.requiresStaff,
      acceptPolicy: this.acceptPolicy,
      ticketTiers: this.ticketTiers.map(t => ({ ...t })),
      imagePreviewUrl: this.imagePreviewUrl(),
      estimatedStaffCount: this.estimatedStaffCount(),
      estimatedStaffCost: this.estimatedStaffCost()
    };
    sessionStorage.setItem(this.STORAGE_KEY, JSON.stringify(draft));

    if (this.saveTimeout) {
      clearTimeout(this.saveTimeout);
    }
    this.saveTimeout = setTimeout(() => {
      this.draftStatus.set('Saved to draft');
    }, 500);
  }

  private loadDraft(): void {
    try {
      const saved = sessionStorage.getItem(this.STORAGE_KEY);
      if (saved) {
        const draft = JSON.parse(saved);
        this.title = draft.title ?? '';
        this.descriptionText = draft.descriptionText ?? '';
        this.eventType = draft.eventType ?? 'Physical';
        this.category = draft.category ?? 'Tech';
        this.ageCategory = draft.ageCategory ?? '';
        this.dateTime = draft.dateTime ?? '';
        this.durationHours = draft.durationHours ?? 2;
        this.venueId = draft.venueId ?? null;
        this.requiresStaff = draft.requiresStaff ?? false;
        this.acceptPolicy = draft.acceptPolicy ?? false;
        this.ticketTiers = draft.ticketTiers ?? [];
        if (draft.estimatedStaffCount) this.estimatedStaffCount.set(draft.estimatedStaffCount);
        if (draft.estimatedStaffCost) this.estimatedStaffCost.set(draft.estimatedStaffCost);
        if (draft.imagePreviewUrl) {
          this.imagePreviewUrl.set(draft.imagePreviewUrl);
          fetch(draft.imagePreviewUrl)
            .then(res => res.blob())
            .then(blob => {
              this.imageFile = new File([blob], 'draft_image.png', { type: blob.type });
            })
            .catch(err => console.error('Failed to reconstruct image file from draft', err));
        }

        // Always recalculate staff estimation when recreating/loading draft to ensure accuracy
        if (this.requiresStaff) {
          setTimeout(() => this.calculateStaffEstimation(), 100);
        }
      }
    } catch {
      sessionStorage.removeItem(this.STORAGE_KEY);
    }
  }

  private clearDraft(): void {
    sessionStorage.removeItem(this.STORAGE_KEY);
  }

  private loadPlatformSettings(): void {
    this.eventService.getPlatformSettings().subscribe({
      next: (settings) => {
        this.platformSettings.set(settings);
      },
      error: () => {
        console.error('Failed to load platform settings');
      }
    });
  }

  private loadPolicy(): void {
    this.authService.getConsentDocument('EventCreation').subscribe({
      next: (doc) => {
        this.policyDocument.set(doc);
        if (doc?.termsId) {
          this.acceptedPolicyTermsId.set(doc.termsId);
        }
      },
      error: (err) => {
        console.error('Failed to load event creation policy', err);
      }
    });
  }

  public openPolicyModal(event: Event): void {
    event.preventDefault();
    const doc = this.policyDocument();
    if (!doc?.filePath) {
      this.policyModalContent.set('<p>No policy document available.</p>');
      this.showPolicyModal.set(true);
      return;
    }

    this.isLoadingPolicy.set(true);
    this.showPolicyModal.set(true);

    const fileUrl = doc.filePath.startsWith('http') ? doc.filePath : `${environment.serverUrl}${doc.filePath}`;
    this.http.get(fileUrl, { responseType: 'text' }).subscribe({
      next: (content) => {
        let lines = content.split('\n');
        lines = lines.filter(line => {
          const trimmed = line.trim().toLowerCase();
          return !trimmed.startsWith('**version:**') &&
                 !trimmed.startsWith('**policy id:**') &&
                 !trimmed.startsWith('version:') &&
                 !trimmed.startsWith('policy id:');
        });
        const filteredContent = lines.join('\n');
        const formatted = filteredContent
          .replace(/^### (.+)$/gm, '<h3>$1</h3>')
          .replace(/^## (.+)$/gm, '<h2>$1</h2>')
          .replace(/^# (.+)$/gm, '<h1>$1</h1>')
          .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
          .replace(/^- (.+)$/gm, '<li>$1</li>')
          .replace(/(<li>.*<\/li>\n?)+/g, '<ul>$&</ul>')
          .replace(/\n\n/g, '</p><p>')
          .replace(/\n/g, '<br/>');
        this.policyModalContent.set(`<p>${formatted}</p>`);
        this.isLoadingPolicy.set(false);
      },
      error: () => {
        this.policyModalContent.set('<p>Unable to load the policy document contents.</p>');
        this.isLoadingPolicy.set(false);
      }
    });
  }

  public closePolicyModal(): void {
    this.showPolicyModal.set(false);
    this.policyModalContent.set('');
  }

  private loadVenues(): void {
    this.isVenuesLoading.set(true);
    this.eventService.getVenues().subscribe({
      next: (venues) => {
        this.venuesList.set(venues);
        this.isVenuesLoading.set(false);
      },
      error: () => this.isVenuesLoading.set(false)
    });
  }

  private loadCategories(): void {
    this.eventService.getCategories().subscribe({
      next: (cats) => {
        this.categoriesList.set(cats);
        if (cats.length > 0 && !this.category) {
          this.category = cats[0];
        }
      },
      error: () => {
        const fallbacks = ['Tech', 'Conference', 'Music', 'Sports', 'Workshop', 'Education', 'Arts', 'Food', 'Wellness'];
        this.categoriesList.set(fallbacks);
        this.category = fallbacks[0];
      }
    });
  }

  private loadAgeCategories(): void {
    this.eventService.getAgeCategories().subscribe({
      next: (list) => {
        this.ageCategoriesList.set(list);
        if (list.length > 0) {
          this.ageCategory = list[0].key;
        }
      },
      error: () => {
        const fallbacks = [
          { key: 'ALL', display: 'Unrestricted' },
          { key: 'KID', display: '5 years +' },
          { key: 'ADL', display: '18+' }
        ];
        this.ageCategoriesList.set(fallbacks);
        this.ageCategory = fallbacks[0].key;
      }
    });
  }

  public onEventTypeChange(): void {
    if (this.eventType === 'Virtual') {
      this.venueId = null;
      this.requiresStaff = false;
      this.ticketTiers = [];
    } else {
      this.onVenueChange();
    }
    this.saveDraft();
  }

  public onVenueChange(): void {
    // 1. Calculate staff if toggled on
    if (this.requiresStaff) {
      if (this.venueId) {
        this.calculateStaffEstimation();
      } else {
        this.requiresStaff = false;
      }
    }

    // 2. Set dynamic ticket tiers from the selected venue
    const selectedVenue = this.venuesList().find(
      (v: any) => (v.venue_Id ?? v.Venue_Id) === Number(this.venueId)
    );

    if (selectedVenue) {
      const seatTiers = selectedVenue.seatTiers ?? selectedVenue.SeatTiers ?? [];
      this.ticketTiers = seatTiers.map((t: any) => ({
        tierName: t.tier_Name ?? t.Tier_Name ?? t.tierName ?? '',
        price: 0, // Default price in INR
        capacity: t.total_Seats ?? t.Total_Seats ?? t.totalSeats ?? 0
      }));
    } else {
      this.ticketTiers = [];
    }
    this.saveDraft();
  }

  public onDateTimeChange(): void {
    if (this.dateTime) {
      const selected = new Date(this.dateTime);
      const now = new Date();
      const diffHrs = (selected.getTime() - now.getTime()) / (1000 * 60 * 60);
      
      if (diffHrs < 0) {
        this.setFieldError('dateTime', 'Event date cannot be in the past.');
      } else if (diffHrs < 24) {
        this.setFieldError('dateTime', 'Event date must be at least 24 hours from now.');
      } else {
        this.clearFieldError('dateTime');
      }
    } else {
      this.clearFieldError('dateTime');
    }

    if (this.requiresStaff) {
      this.calculateStaffEstimation();
    }
    this.saveDraft();
  }

  public onStaffToggleChange(): void {
    if (!this.venueId || !this.dateTime || !this.durationHours) {
      this.requiresStaff = false;
      alert('Please specify Date & Time, Duration, and a Physical Venue before requesting staff.');
      return;
    }

    if (this.requiresStaff) {
      this.calculateStaffEstimation();
    } else {
      this.estimatedStaffCount.set(0);
      this.estimatedStaffCost.set(0);
      this.staffErrorMsg.set('');
    }
    this.saveDraft();
  }

  public calculateStaffEstimation(): void {
    if (this.isStaffCalculationLoading()) return;

    if (!this.venueId || !this.dateTime || !this.durationHours) {
      this.staffErrorMsg.set('Please select Venue, Date/Time, and Duration first before estimating staff.');
      this.requiresStaff = false;
      return;
    }

    this.staffErrorMsg.set('');
    this.isStaffCalculationLoading.set(true);

    const dateStr = new Date(this.dateTime).toISOString();
    
    const startTime = Date.now();

    this.eventService.checkStaffAvailability(Number(this.venueId), dateStr, this.durationHours).subscribe({
      next: (res) => {
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 600 - elapsedTime);
        
        setTimeout(() => {
          this.estimatedStaffCount.set(res.requiredStaffCount ?? res.RequiredStaffCount ?? 0);
          this.estimatedStaffCost.set(res.staffingCost ?? res.StaffingCost ?? 0);
          this.isStaffCalculationLoading.set(false);
          this.saveDraft();
        }, delay);
      },
      error: (err) => {
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 600 - elapsedTime);

        setTimeout(() => {
          console.error('checkStaff error:', err);
          this.staffErrorMsg.set(err?.error?.message || err?.message || 'Staff availability calculation failed.');
          this.isStaffCalculationLoading.set(false);
          this.requiresStaff = false;
          this.saveDraft();
        }, delay);
      }
    });
  }

  public isDragOver = signal(false);
  public isImageProcessing = signal(false);
  public imageErrorMsg = signal<string | null>(null);

  public onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(true);
  }

  public onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
  }

  public onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
    this.imageErrorMsg.set(null);

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.processFile(event.dataTransfer.files[0]);
    } else if (event.dataTransfer?.items) {
      let found = false;
      for (let i = 0; i < event.dataTransfer.items.length; i++) {
        const item = event.dataTransfer.items[i];
        if (item.type.indexOf('image/') === 0) {
          const file = item.getAsFile();
          if (file) {
            this.processFile(file);
            found = true;
            break;
          }
        }
      }
      if (!found) {
        const html = event.dataTransfer.getData('text/html');
        if (html) {
          const div = document.createElement('div');
          div.innerHTML = html;
          const img = div.querySelector('img');
          if (img && img.src) {
             this.fetchAndProcessImageUrl(img.src);
             found = true;
          }
        }
      }
      if (!found) {
        this.imageErrorMsg.set('No valid image found in the dropped content.');
      }
    }
  }

  public onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.imageErrorMsg.set(null);
    if (input.files && input.files.length > 0) {
      this.processFile(input.files[0]);
      input.value = ''; // clear so same file can be selected again
    }
  }

  private async processFile(file: File): Promise<void> {
    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/bmp', 'image/gif'];
    if (!allowedTypes.includes(file.type)) {
      this.imageErrorMsg.set(`Unsupported format: ${file.type || 'Unknown'}. Please upload a JPEG, PNG, WebP, GIF, or BMP.`);
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      this.imageErrorMsg.set('Image is too large. Maximum size is 10MB.');
      return;
    }

    try {
      this.isImageProcessing.set(true);
      const webpFile = await this.convertToWebP(file);
      this.imageFile = webpFile;
      const reader = new FileReader();
      reader.onload = (e) => this.imagePreviewUrl.set(e.target?.result as string);
      reader.readAsDataURL(webpFile);
      this.saveDraft();
    } catch (err) {
      this.imageErrorMsg.set('Failed to process image. It might be corrupted.');
      console.error('Image processing error', err);
    } finally {
      this.isImageProcessing.set(false);
    }
  }

  private async fetchAndProcessImageUrl(url: string): Promise<void> {
     try {
        this.isImageProcessing.set(true);
        const response = await fetch(url);
        const blob = await response.blob();
        if (blob.type.indexOf('image/') !== 0) {
           this.imageErrorMsg.set('Dragged URL is not a valid image.');
           this.isImageProcessing.set(false);
           return;
        }
        const file = new File([blob], 'dragged-image.jpg', { type: blob.type });
        await this.processFile(file);
     } catch (err) {
        this.imageErrorMsg.set('Could not fetch the dragged image due to cross-origin restrictions or network error.');
        this.isImageProcessing.set(false);
     }
  }

  private convertToWebP(file: File): Promise<File> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = (e) => {
        const img = new Image();
        img.onload = () => {
          const canvas = document.createElement('canvas');
          canvas.width = img.width;
          canvas.height = img.height;
          const ctx = canvas.getContext('2d');
          if (!ctx) {
            return reject(new Error('Canvas 2d context not available'));
          }
          ctx.drawImage(img, 0, 0);
          canvas.toBlob((blob) => {
            if (blob) {
              const nameWithoutExt = file.name ? file.name.replace(/\.[^/.]+$/, "") : "image";
              const newFile = new File([blob], nameWithoutExt + ".webp", {
                type: 'image/webp'
              });
              resolve(newFile);
            } else {
              reject(new Error('Canvas toBlob failed'));
            }
          }, 'image/webp', 0.85);
        };
        img.onerror = () => reject(new Error('Image load error'));
        img.src = e.target?.result as string;
      };
      reader.onerror = () => reject(new Error('FileReader error'));
      reader.readAsDataURL(file);
    });
  }


  public async onSubmitDetails(): Promise<void> {
    this.clearFormErrors();
    let hasError = false;

    if (!this.title?.trim()) {
      this.setFieldError('title', 'Event title is required.');
      hasError = true;
    }
    if (!this.category) {
      this.setFieldError('category', 'Please select a category.');
      hasError = true;
    }
    if (!this.ageCategory) {
      this.setFieldError('ageCategory', 'Please select an age category.');
      hasError = true;
    }
    if (!this.descriptionText?.trim()) {
      this.setFieldError('description', 'Event description cannot be empty.');
      hasError = true;
    }
    if (!this.dateTime) {
      this.setFieldError('dateTime', 'Please select an event date & time.');
      hasError = true;
    } else {
      const selected = new Date(this.dateTime);
      const now = new Date();
      const diffHrs = (selected.getTime() - now.getTime()) / (1000 * 60 * 60);
      if (diffHrs < 0) {
        this.setFieldError('dateTime', 'Event date cannot be in the past.');
        hasError = true;
      } else if (diffHrs < 24) {
        this.setFieldError('dateTime', 'Event date must be at least 24 hours from now.');
        hasError = true;
      }
    }
    if (this.eventType !== 'Virtual' && !this.venueId) {
      this.setFieldError('venue', 'Please select a venue for physical/hybrid events.');
      hasError = true;
    }
    if (this.requiresStaff && this.estimatedStaffCount() === 0) {
      this.setFieldError('staff', 'Please wait for staff estimation to complete.');
      hasError = true;
    }
    if (this.eventType !== 'Virtual') {
      const activeTiers = this.ticketTiers;
      if (activeTiers.length === 0) {
        this.setFieldError('tiers', 'Please select a venue with ticket tiers.');
        hasError = true;
      } else if (activeTiers.some(t => !t.price || t.price <= 0)) {
        this.setFieldError('tiers', 'All ticket tiers must have a price greater than ₹0.');
        hasError = true;
      } else if (activeTiers.some(t => !t.capacity || t.capacity <= 0)) {
        this.setFieldError('tiers', 'All ticket tiers must have a seat capacity greater than 0.');
        hasError = true;
      }
    }
    if (!this.acceptPolicy) {
      this.setFieldError('policy', 'You must accept the Event Creation Policy to proceed.');
      hasError = true;
    }

    if (hasError) return;

    // Step 1: Upload description text as .txt file
    let descriptionUrl = '';
    try {
      const descResult = await firstValueFrom(this.eventService.uploadDescription(this.descriptionText));
      descriptionUrl = descResult.url;
    } catch {
      this.setFieldError('description', 'Failed to save description file.');
      return;
    }

    // Step 2: Upload image if selected
    let imageUrl = '';
    if (this.imageFile) {
      try {
        const imgResult = await firstValueFrom(this.eventService.uploadImage(this.imageFile));
        imageUrl = imgResult.url;
      } catch {
        this.imageErrorMsg.set('Failed to upload event image.');
        return;
      }
    }

    // Filter enabled tiers (only for physical/hybrid)
    const tiersPayload = this.eventType === 'Virtual'
      ? []
      : this.ticketTiers
          .map(t => ({
            tierName: t.tierName,
            price: t.price
          }));

    if (this.eventType !== 'Virtual' && tiersPayload.length === 0) {
      this.setFieldError('tiers', 'Please configure at least one ticket tier.');
      return;
    }

    const payload = {
      eventType: this.eventType,
      title: this.title,
      category: this.category,
      ageCategory: this.ageCategory,
      descriptionUrl,
      imageUrl,
      dateTime: new Date(this.dateTime).toISOString(),
      durationHours: this.durationHours,
      requiresStaff: this.requiresStaff,
      venueId: this.eventType === 'Virtual' ? null : Number(this.venueId),
      acceptedPolicyId: this.acceptedPolicyTermsId(),
      ticketTiers: tiersPayload
    };

    this.pendingEventPayload = payload;
    this.showReviewModal.set(true);
  }

  public onConfirmReview(): void {
    if (!this.pendingEventPayload) return;

    this.isInitiatingEvent.set(true);
    this.eventService.createEvent(this.pendingEventPayload).subscribe({
      next: (res) => {
        const pendingEventId = res.event_Id;
        this.createdEventId.set(pendingEventId);
        
        const successUrl = `http://localhost:4200/myevents/create?session_id={CHECKOUT_SESSION_ID}&eventId=${pendingEventId}`;
        const cancelUrl = `http://localhost:4200/myevents/create?canceled=true&eventId=${pendingEventId}`;
        
        this.eventService.createCheckoutSession(pendingEventId, successUrl, cancelUrl).subscribe({
          next: (stripeRes) => {
            this.router.navigate(['/stripe-checkout'], {
              queryParams: {
                clientSecret: stripeRes.clientSecret,
                createdAt: stripeRes.createdAtUTC,
                type: 'event',
                id: pendingEventId
              }
            });
          },
          error: (err) => {
            this.isInitiatingEvent.set(false);
            console.error('Failed to create checkout session', err);
            alert('Failed to initialize payment gateway.');
          }
        });
      },
      error: (err) => {
        this.isInitiatingEvent.set(false);
        alert(err?.error?.message || 'Failed to initialize event listing.');
      }
    });
  }

  public onCancelReview(): void {
    this.showReviewModal.set(false);
  }

  public onConfirmStripeCancel(): void {
    const eventId = this.pendingCancelEventId();
    if (!eventId) {
      this.showStripeCancelModal.set(false);
      return;
    }

    this.isRevertingEvent.set(true);
    this.eventService.revertEvent(eventId).subscribe({
      next: () => {
        this.isRevertingEvent.set(false);
        this.showStripeCancelModal.set(false);
        this.clearDraft();
        this.router.navigate(['/myevents']);
      },
      error: (err) => {
        this.isRevertingEvent.set(false);
        alert('Failed to completely revert event. It may require manual cleanup.');
        this.showStripeCancelModal.set(false);
        this.router.navigate(['/myevents']);
      }
    });
  }

  public onKeepStripeDraft(): void {
    const eventId = this.pendingCancelEventId();
    if (!eventId) {
      this.showStripeCancelModal.set(false);
      return;
    }

    this.isRevertingEvent.set(true);
    this.eventService.revertEvent(eventId).subscribe({
      next: () => {
        this.isRevertingEvent.set(false);
        this.showStripeCancelModal.set(false);
        // Form stays filled from local storage draft
      },
      error: (err) => {
        this.isRevertingEvent.set(false);
        this.showStripeCancelModal.set(false);
        console.error('Failed to revert pending event.', err);
      }
    });
  }

  public canDeactivate(): Observable<boolean> | boolean {
    const id = this.createdEventId();
    if (id && this.currentStep() === 'payment' && !this.isPaymentConfirmed) {
      return new Observable<boolean>(observer => {
        this.eventService.revertEvent(id).subscribe({
          next: () => {
            observer.next(true);
            observer.complete();
          },
          error: () => {
            // Permit navigation even if API fails to revert
            observer.next(true);
            observer.complete();
          }
        });
      });
    }
    return true;
  }

  public onBackToDetails(): void {
    this.showConfirmBackModal.set(true);
  }

  public confirmRevertAndBack(): void {
    const id = this.createdEventId();
    if (id) {
      this.eventService.revertEvent(id).subscribe({
        next: () => {
          this.currentStep.set('details');
          this.showConfirmBackModal.set(false);
        },
        error: () => {
          alert('Could not revert the pending event registration.');
          this.showConfirmBackModal.set(false);
        }
      });
    } else {
      this.currentStep.set('details');
      this.showConfirmBackModal.set(false);
    }
  }

  public onSubmitPayment(): void {
    if (!this.cardholderName) {
      alert('Please enter the cardholder name.');
      return;
    }

    this.isSubmittingPayment.set(true);

    this.stripeService.createToken(this.card.element, { name: this.cardholderName }).subscribe({
      next: (result) => {
        if (result.token) {
          const id = this.createdEventId();
          if (id) {
            this.eventService.confirmEvent(id, result.token.id, 'card').subscribe({
              next: () => {
                this.isPaymentConfirmed = true;
                this.clearDraft();
                this.isSubmittingPayment.set(false);
                alert('Payment Successful! Your event is now live.');
                this.router.navigate(['/myevents']);
              },
              error: (err) => {
                this.isSubmittingPayment.set(false);
                alert(err?.error?.message || 'Payment confirmation failed.');
              }
            });
          }
        } else if (result.error) {
          this.isSubmittingPayment.set(false);
          alert(result.error.message || 'Stripe card tokenization failed.');
        }
      },
      error: (err) => {
        this.isSubmittingPayment.set(false);
        alert(err?.message || 'Payment token generation failed.');
      }
    });
  }
}
