import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { AppStoreService } from '../../../store/app-store.service';
import { BrowsedEventResponse } from '../../../models/event.model';
import { ResolveDescriptionPipe } from '../../../pipes/resolve-description.pipe';

@Component({
  selector: 'app-hero-carousel',
  standalone: true,
  imports: [CommonModule, ResolveDescriptionPipe],
  templateUrl: './hero-carousel.html',
  styleUrl: './hero-carousel.css'
})
export class HeroCarouselComponent implements OnInit, OnDestroy {
  public currentSlideIndex = signal(0);
  public trendingEvents = signal<BrowsedEventResponse[]>([]);
  public isLoggedIn = signal(false);

  private subscriptions: Subscription = new Subscription();
  private carouselIntervalSub?: Subscription;

  constructor(
    private store: AppStoreService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.store.select(state => state.events.trending).subscribe(trend => {
        this.trendingEvents.set(trend);
      })
    );
    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(logged => this.isLoggedIn.set(logged))
    );

    this.startCarouselTimer();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    this.stopCarouselTimer();
  }

  public nextSlide(): void {
    const total = this.trendingEvents().length;
    if (total > 0) {
      this.currentSlideIndex.update(idx => (idx + 1) % total);
    }
  }

  public prevSlide(): void {
    const total = this.trendingEvents().length;
    if (total > 0) {
      this.currentSlideIndex.update(idx => (idx - 1 + total) % total);
    }
  }

  public setSlide(idx: number): void {
    this.currentSlideIndex.set(idx);
    this.resetCarouselTimer();
  }

  private startCarouselTimer(): void {
    this.carouselIntervalSub = interval(5000).subscribe(() => {
      this.nextSlide();
    });
  }

  private stopCarouselTimer(): void {
    if (this.carouselIntervalSub) {
      this.carouselIntervalSub.unsubscribe();
    }
  }

  private resetCarouselTimer(): void {
    this.stopCarouselTimer();
    this.startCarouselTimer();
  }

  public triggerBookingAction(): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/bookings']);
  }

  public navigateToBookingFlow(eventObj: any): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/booking'], { 
      queryParams: { eventId: eventObj.event_Id },
      state: { event: eventObj }
    });
  }
}
