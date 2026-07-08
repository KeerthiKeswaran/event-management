import { Component, OnInit, OnDestroy, Input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { AppStoreService } from '../../../store/app-store.service';
import { RegionService } from '../../../services/region.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './footer.html',
  styleUrl: './footer.css'
})
export class FooterComponent implements OnInit, OnDestroy {
  @Input() isAdmin = false;
  @Input() isFinance = false;
  @Input() isRaw = false;
  public isLoggedIn = signal(false);
  private subscriptions: Subscription = new Subscription();

  constructor(
    private store: AppStoreService,
    private regionService: RegionService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(logged => this.isLoggedIn.set(logged))
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  public onSearchSubmit(): void {
    this.router.navigate(['/browse']);
  }

  public onPopularRegionSelect(regionId: string): void {
    this.regionService.setLocalRegion(regionId);
    if (this.isLoggedIn()) {
      this.authService.selectRegion(regionId).subscribe();
    }
    setTimeout(() => {
      this.router.navigate(['/browse'], {
        queryParams: {
          regionId: regionId
        }
      });
    }, 100);
  }

  public triggerCreateEventAction(): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/create-event']);
  }

  public triggerManageMyEventsAction(): void {
    alert('Navigating to manage my events...');
  }

  public triggerGetHelpAction(): void {
    this.router.navigate(['/help']);
  }
}
