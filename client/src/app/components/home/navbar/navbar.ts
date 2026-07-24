import { Component, OnInit, OnDestroy, Output, EventEmitter, signal, HostListener, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Subscription, Subject, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { AppStoreService } from '../../../store/app-store.service';
import { AuthService } from '../../../services/auth.service';
import { RegionService } from '../../../services/region.service';
import { EventService } from '../../../services/event.service';
import { AdminService } from '../../../services/admin.service';
import { FinanceService } from '../../../services/finance.service';
import { RegionModel } from '../../../models/region.model';
import { BrowsedEventResponse } from '../../../models/event.model';
import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { LocationModalComponent } from '../location-modal/location-modal';

@Pipe({
  name: 'highlight',
  standalone: true
})
export class HighlightPipe implements PipeTransform {
  constructor(private sanitizer: DomSanitizer) {}
  transform(value: string | number | null | undefined, args: string): SafeHtml | string {
    if (value == null) return '';
    const text = String(value);
    if (!args) return text;
    const regex = new RegExp(`(${args})`, 'gi');
    const match = text.match(regex);
    if (!match) return text;
    const replaced = text.replace(regex, `<span style="background-color: yellow; color: black; font-weight: bold;">$1</span>`);
    return this.sanitizer.bypassSecurityTrustHtml(replaced);
  }
}

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, HighlightPipe, LocationModalComponent],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css'
})
export class NavbarComponent implements OnInit, OnDestroy {
  @Input() isAdmin: boolean = false;
  @Input() isFinance: boolean = false;
  @Input() isMinimal: boolean = false;
  @Output() openLocation = new EventEmitter<void>();
  public isLocationModalOpen = signal(false);

  public searchKeyword = signal('');
  public selectedRegionId = signal('REG01');
  public isProfileDropdownOpen = signal(false);
  public showRecommendations = signal(false);
  public isSidebarOpen = signal(false);
  public recommendations = signal<BrowsedEventResponse[]>([]);
  public globalRecommendations = signal<{ events: any[], bookings: any[] }>({ events: [], bookings: [] });

  private searchSubject = new Subject<string>();

  @HostListener('document:click', ['$event'])
  public onDocumentClick(event: MouseEvent): void {
    this.closeDropdowns();
  }

  public currentUser = signal<any>(null);
  public get isLoggedIn(): boolean {
    let tokenKey = 'user_token';
    if (this.isAdmin) tokenKey = 'admin_token';
    else if (this.isFinance) tokenKey = 'finance_token';
    return typeof window !== 'undefined' ? !!localStorage.getItem(tokenKey) : false;
  }
  public regions = signal<RegionModel[]>([]);

  private subscriptions: Subscription = new Subscription();

  constructor(
    private store: AppStoreService,
    private authService: AuthService,
    private regionService: RegionService,
    private eventService: EventService,
    private adminService: AdminService,
    private financeService: FinanceService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.subscriptions.add(
      this.store.select(state => state.auth.user).subscribe(user => this.currentUser.set(user))
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.items).subscribe(regs => this.regions.set(regs))
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.currentRegionId).subscribe(regId => {
        this.selectedRegionId.set(regId || 'REG01');
      })
    );

    // Search recommendation subscription with RxJS switchMap, distinct, debounceTime querying backend
    this.subscriptions.add(
      this.searchSubject.pipe(
        debounceTime(200),
        distinctUntilChanged(),
        switchMap(keyword => {
          const kw = keyword.trim();
          if (!kw) {
            return of(this.isAdmin || this.isFinance ? { events: [], bookings: [] } : []);
          }
          if (this.isAdmin) {
            return this.adminService.searchGlobal(kw);
          } else if (this.isFinance) {
            return this.financeService.searchGlobal(kw);
          } else {
            return this.eventService.searchEventsQuick(kw);
          }
        })
      ).subscribe(matches => {
        if (this.isAdmin || this.isFinance) {
          this.globalRecommendations.set(matches);
        } else {
          this.recommendations.set(matches);
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  public get currentLocationName(): string {
    const activeId = this.selectedRegionId();
    const found = this.regions().find(r => r.region_Id === activeId);
    return found ? found.name : 'Chennai';
  }

  public get isOrganizerPortal(): boolean {
    return this.router.url.startsWith('/myevents');
  }

  public get showHomeInDropdown(): boolean {
    return this.router.url !== '/' && this.router.url !== '/home';
  }

  public get showBrowseInDropdown(): boolean {
    return !this.router.url.startsWith('/browse');
  }

  public get showManageEventsInDropdown(): boolean {
    return !this.isOrganizerPortal;
  }

  public get showHelpInDropdown(): boolean {
    return !this.isOrganizerPortal && !this.router.url.startsWith('/help');
  }

  public get showAccountSettingsInDropdown(): boolean {
    return !this.router.url.includes('/settings');
  }

  public get displayName(): string {
    if (this.currentUser()?.name) return this.currentUser().name;
    let tokenKey = 'user_token';
    if (this.isAdmin) tokenKey = 'admin_token';
    else if (this.isFinance) tokenKey = 'finance_token';
    const token = typeof window !== 'undefined' ? localStorage.getItem(tokenKey) : null;
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        // Standard 'name' claim added by JwtTokenGenerator
        const name = payload.name
          || payload.unique_name
          || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
          || payload.nameid;
        if (name) return name;
      } catch(e) {}
    }
    return this.isAdmin ? 'Admin' : (this.isFinance ? 'Finance' : 'Account');
  }

  public get displayEmail(): string {
    if (this.currentUser()?.email) return this.currentUser().email;
    let tokenKey = 'user_token';
    if (this.isAdmin) tokenKey = 'admin_token';
    else if (this.isFinance) tokenKey = 'finance_token';
    const token = typeof window !== 'undefined' ? localStorage.getItem(tokenKey) : null;
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const email = payload.email || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'];
        if (email) return email;
      } catch(e) {}
    }
    return '';
  }

  public toggleProfileDropdown(event: Event): void {
    event.stopPropagation();
    this.isProfileDropdownOpen.update(v => !v);
  }

  public closeDropdowns(): void {
    this.isProfileDropdownOpen.set(false);
    this.showRecommendations.set(false);
    this.isSidebarOpen.set(false);
  }

  public toggleSidebar(event: Event): void {
    event.stopPropagation();
    this.isSidebarOpen.update(v => !v);
  }

  public onSearchInput(val: string): void {
    this.searchKeyword.set(val);
    this.searchSubject.next(val);
  }

  public selectRecommendation(rec: any): void {
    this.searchKeyword.set(rec.title);
    this.showRecommendations.set(false);
    
    if (!this.isLoggedIn) {
      const destination = `/booking?eventId=${rec.event_Id}`;
      this.router.navigate(['/login'], { queryParams: { returnUrl: destination } });
    } else {
      this.router.navigate(['/booking'], { queryParams: { eventId: rec.event_Id } });
    }
  }

  public onLocationPickerClick(event: Event): void {
    event.stopPropagation();
    this.openLocation.emit();
    this.isLocationModalOpen.set(true);
  }

  public onSearchSubmit(event?: Event): void {
    if (event) event.preventDefault();
    this.showRecommendations.set(false);
    this.router.navigate(['/browse'], {
      queryParams: {
        keyword: this.searchKeyword() || null,
        regionId: this.selectedRegionId() || null
      }
    });
  }

  public onLogout(): void {
    this.authService.logout();
    this.isProfileDropdownOpen.set(false);
    if (this.isAdmin) {
      this.router.navigate(['/admin/login']);
    } else if (this.isFinance) {
      this.router.navigate(['/finance/login']);
    } else {
      this.router.navigate(['/login']);
    }
  }

  public triggerBookingAction(): void {
    if (!this.isLoggedIn) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/bookings']);
    this.isProfileDropdownOpen.set(false);
  }

  public triggerCreateEventAction(): void {
    if (!this.isLoggedIn) {
      this.router.navigate(['/login']);
      return;
    }
    if (this.currentUser()?.status === 'Restricted') return;
    this.router.navigate(['/myevents/create']);
  }

  public triggerManageMyEventsAction(): void {
    if (!this.isLoggedIn) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/myevents']);
    this.isProfileDropdownOpen.set(false);
  }

  public triggerHomeAction(): void {
    this.router.navigate(['/']);
    this.isProfileDropdownOpen.set(false);
  }

  public triggerAccountSettingsAction(): void {
    if (this.isAdmin) {
      this.router.navigate(['/admin/settings']);
    } else if (this.isFinance) {
      this.router.navigate(['/finance/settings']);
    } else {
      this.router.navigate(['/settings']);
    }
    this.isProfileDropdownOpen.set(false);
  }

  public triggerGetHelpAction(): void {
    this.router.navigate(['/help']);
    this.isProfileDropdownOpen.set(false);
  }
}
