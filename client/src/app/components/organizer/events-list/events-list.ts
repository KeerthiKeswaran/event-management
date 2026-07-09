import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { EventService } from '../../../services/event.service';
import { FooterComponent } from '../../home/footer/footer';
import { NavbarComponent } from '../../home/navbar/navbar';

import { EventDetailsModalComponent } from '../event-details-modal/event-details-modal';
import { AppStoreService } from '../../../store/app-store.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-organizer-events',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, FormsModule, FooterComponent, NavbarComponent, EventDetailsModalComponent],
  templateUrl: './events-list.html',
  styleUrl: './events-list.css'
})
export class OrganizerEventsComponent implements OnInit {
  public allEvents = signal<any[]>([]);
  public isLoading = signal(true);

  // Filters
  public filterStatus = signal<string>('all');
  public filterType = signal<string>('all');
  public sortBy = signal<string>('date');
  public sortDirection = signal<'asc' | 'desc'>('desc');

  public selectedEvent = signal<any | null>(null);

  public toggleSort(column: string): void {
    if (this.sortBy() === column) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(column);
      this.sortDirection.set('desc');
    }
  }

  // Computed filtered list
  public filteredEvents = computed(() => {
    let list = [...this.allEvents()];
    
    // Status filter
    const status = this.filterStatus();
    if (status !== 'all') {
      list = list.filter(e => e.status.toLowerCase() === status.toLowerCase());
    }

    // Type filter
    const type = this.filterType();
    if (type !== 'all') {
      list = list.filter(e => (e.event_Type || e.eventType || e.type || '').toLowerCase() === type.toLowerCase());
    }

    // Sort
    const sort = this.sortBy();
    const dir = this.sortDirection() === 'asc' ? 1 : -1;
    list.sort((a, b) => {
      if (sort === 'date') {
        const timeA = new Date(a.date_Time).getTime();
        const timeB = new Date(b.date_Time).getTime();
        return (timeA - timeB) * dir;
      }
      if (sort === 'earnings') return ((a.net_Earnings || 0) - (b.net_Earnings || 0)) * dir;
      if (sort === 'sold') return ((a.tickets_Sold || 0) - (b.tickets_Sold || 0)) * dir;
      return 0;
    });

    return list;
  });

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
    this.loadEvents();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  public loadEvents(): void {
    this.isLoading.set(true);
    this.eventService.getMyEvents().subscribe({
      next: (events) => {
        this.allEvents.set(events);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  public navigateToCreate(): void {
    if (this.isRestricted()) return;
    this.router.navigate(['/myevents/create']);
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
          this.loadEvents();
        }
      });
    }
  }

  public recreateEvent(eventObj: any, mouseEvent?: Event): void {
    if (mouseEvent) {
      mouseEvent.stopPropagation();
    }
    this.eventService.getMyEventDetails(eventObj.event_Id).subscribe({
      next: (detailedEvent: any) => {
        let draftTiers: any[] = [];
        if (detailedEvent.ticketTiers && detailedEvent.ticketTiers.length > 0) {
          draftTiers = detailedEvent.ticketTiers.map((t: any) => ({
             tierName: t.tier_Name || t.Tier_Name,
             price: t.price || t.Price,
             capacity: t.capacity || t.Capacity || t.tickets_Sold || 0 // Re-use the existing values, event was failed so tickets_Sold is essentially the capacity they tried to allocate
          }));
        } else if (detailedEvent.tiers && detailedEvent.tiers.length > 0) {
          draftTiers = detailedEvent.tiers.map((t: any) => ({
             tierName: t.tier_Name || t.Tier_Name,
             price: t.price || t.Price,
             capacity: t.capacity || t.Capacity || 0
          }));
        }

        let localDateTime = '';
        const rawDate = detailedEvent.date_Time || detailedEvent.Date_Time;
        if (rawDate) {
          const dateObj = new Date(rawDate);
          // format as YYYY-MM-DDTHH:mm in local time
          const year = dateObj.getFullYear();
          const month = String(dateObj.getMonth() + 1).padStart(2, '0');
          const day = String(dateObj.getDate()).padStart(2, '0');
          const hours = String(dateObj.getHours()).padStart(2, '0');
          const minutes = String(dateObj.getMinutes()).padStart(2, '0');
          localDateTime = `${year}-${month}-${day}T${hours}:${minutes}`;
        }

        const draft = {
          title: detailedEvent.title || detailedEvent.Title || '',
          descriptionText: detailedEvent.description || detailedEvent.Description_Url || '',
          eventType: detailedEvent.event_Type || detailedEvent.Event_Type || 'Physical',
          category: detailedEvent.category || detailedEvent.Category || 'Tech',
          ageCategory: detailedEvent.ageCategory || detailedEvent.AgeCategory || '',
          dateTime: localDateTime,
          durationHours: detailedEvent.duration_Hours || detailedEvent.Duration_Hours || 2,
          venueId: detailedEvent.venue_Id || detailedEvent.Venue_Id || null,
          requiresStaff: detailedEvent.requires_Staff || detailedEvent.Requires_Staff || false,
          acceptPolicy: false,
          ticketTiers: draftTiers
        };
        sessionStorage.setItem('createEventDraft', JSON.stringify(draft));
        this.router.navigate(['/myevents/create']);
      }
    });
  }
}
