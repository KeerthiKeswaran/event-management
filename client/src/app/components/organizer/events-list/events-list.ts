import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { EventService } from '../../../services/event.service';
import { FooterComponent } from '../../home/footer/footer';
import { NavbarComponent } from '../../home/navbar/navbar';

import { EventDetailsModalComponent } from '../event-details-modal/event-details-modal';

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

  constructor(
    private eventService: EventService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadEvents();
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
    this.router.navigate(['/myevents/create']);
  }

  public openEventModal(event: any): void {
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
}
