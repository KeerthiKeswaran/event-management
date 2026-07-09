import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { EventService } from '../../../services/event.service';
import { FooterComponent } from '../../home/footer/footer';
import { NavbarComponent } from '../../home/navbar/navbar';

import { EventDetailsModalComponent } from '../event-details-modal/event-details-modal';
import { AppStoreService } from '../../../store/app-store.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-organizer-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, FooterComponent, NavbarComponent, EventDetailsModalComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class OrganizerDashboardComponent implements OnInit {
  public totalEvents = signal(0);
  public ticketsSold = signal(0);
  public netEarnings = signal(0);
  public upcomingEvents = signal<any[]>([]);
  public isLoading = signal(true);
  public selectedEvent = signal<any | null>(null);

  public liveEventsCount = signal(0);
  public completedEventsCount = signal(0);

  public isRestricted = signal(false);
  private subscriptions = new Subscription();

  constructor(
    private eventService: EventService,
    private router: Router,
    private store: AppStoreService
  ) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.store.select(state => state.auth.user).subscribe(user => {
        this.isRestricted.set(user?.status === 'Restricted');
      })
    );
    this.loadDashboardData();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private loadDashboardData(): void {
    this.isLoading.set(true);
    this.eventService.getMyDashboard().subscribe({
      next: (data) => {
        this.totalEvents.set(data.totalEvents ?? 0);
        this.ticketsSold.set(data.ticketsSold ?? 0);
        this.netEarnings.set(data.netEarnings ?? 0);
        this.upcomingEvents.set(data.upcomingEvents ?? []);

        // Derive counts from server-provided data
        const total = data.totalEvents ?? 0;
        const upcomingCount = data.upcomingEvents?.length ?? 0;

        this.liveEventsCount.set(data.liveCount ?? upcomingCount);
        this.completedEventsCount.set(data.completedCount ?? 0);

        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  public openEventModal(event: any): void {
    if (this.isRestricted()) return;
    this.selectedEvent.set(event);
  }

  public closeEventModal(): void {
    this.selectedEvent.set(null);
  }

  public refreshEventDetails(): void {
    if (this.selectedEvent()) {
      this.eventService.getMyEventDetails(this.selectedEvent()!.event_Id).subscribe({
        next: (res: any) => {
          this.selectedEvent.set(res);
          this.loadDashboardData();
        }
      });
    }
  }

  public navigateToCreate(): void {
    if (this.isRestricted()) return;
    this.router.navigate(['/myevents/create']);
  }
}
