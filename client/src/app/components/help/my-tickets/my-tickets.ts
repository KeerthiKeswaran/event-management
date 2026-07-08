import { Component, OnInit, OnDestroy, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { NavbarComponent } from '../../home/navbar/navbar';
import { FooterComponent } from '../../home/footer/footer';
import { AppStoreService } from '../../../store/app-store.service';
import { BookingService } from '../../../services/booking.service';
import { SupportTicket } from '../help';

type StatusFilter = 'All' | 'Open' | 'In Progress' | 'Resolved';

@Component({
  selector: 'app-my-tickets',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, NavbarComponent, FooterComponent],
  templateUrl: './my-tickets.html',
  styleUrl: './my-tickets.css'
})
export class MyTicketsComponent implements OnInit, OnDestroy {
  public currentTickets = signal<SupportTicket[]>([]);
  public selectedStatusFilter = signal<StatusFilter>('All');
  public isLoggedIn = signal(false);
  public isLoading = signal(true);

  private subscriptions = new Subscription();

  @HostListener('document:click', ['$event'])
  public onDocumentClick(event: MouseEvent): void {
    // Navbar dropdown close handler
  }

  // Computed signal for filtered tickets
  public filteredTickets = computed(() => {
    const list = this.currentTickets();
    const filter = this.selectedStatusFilter();
    if (filter === 'All') return list;
    return list.filter(ticket => ticket.status === filter);
  });

  constructor(
    private store: AppStoreService,
    private router: Router,
    private bookingService: BookingService
  ) {}

  ngOnInit(): void {
    // Check authentication
    this.isLoggedIn.set(true);
    this.loadTickets();
  }

  public loadTickets(): void {
    this.isLoading.set(true);
    this.bookingService.getMySupportTickets().subscribe({
      next: (res: any) => {
        let list: any[] = [];
        if (Array.isArray(res)) list = res;
        else if (res && res.$values) list = res.$values;
        else if (res && res.data) list = res.data;
        else if (res && res.items) list = res.items;
        this.currentTickets.set(list || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load tickets', err);
        this.currentTickets.set([]);
        this.isLoading.set(false);
      }
    });
  }

  public setFilter(filter: StatusFilter): void {
    this.selectedStatusFilter.set(filter);
  }

  public backToSupport(): void {
    this.router.navigate(['/help']);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
