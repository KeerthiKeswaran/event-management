import { Component, OnInit, OnDestroy, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { NavbarComponent } from '../home/navbar/navbar';
import { FooterComponent } from '../home/footer/footer';
import { BookingService } from '../../services/booking.service';
import { AuthService } from '../../services/auth.service';
import { AppStoreService } from '../../store/app-store.service';
import { BookingModel } from '../../models/booking.model';

export interface SupportTicket {
  ticketId: string;
  bookingId: string;
  category: string;
  subject: string;
  details: string;
  status: 'Open' | 'In Progress' | 'Resolved';
  createdAt: string;
  response?: string;
}

export interface HelpTopic {
  id: string;
  title: string;
  summary: string;
  content: string;
}

@Component({
  selector: 'app-help',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, NavbarComponent, FooterComponent],
  templateUrl: './help.html',
  styleUrl: './help.css'
})
export class HelpComponent implements OnInit, OnDestroy {
  public searchKeyword = signal('');
  public selectedTopicId = signal<string | null>(null);
  
  // Bookings list for dropdown mapping
  public bookings = signal<BookingModel[]>([]);
  public isLoggedIn = signal(false);

  // Support Ticket Form State
  public ticketBookingId = '';
  public ticketCategory = '';
  public ticketSubject = '';
  public ticketDetails = '';
  public ticketSuccessMessage = signal('');
  public ticketErrorMessage = signal('');

  // 5 Main Important helping topics
  public helpTopics = signal<HelpTopic[]>([
    {
      id: 'refunds',
      title: 'Cancellation & Refund Guide',
      summary: 'Learn how to cancel a reservation and retrieve your money.',
      content: `
        <h3>How to Cancel a Booking</h3>
        <p>You can cancel your booking directly from your account page. Follow these simple steps:</p>
        <ol>
          <li>Navigate to the <strong>My Bookings</strong> page from the top navigation bar.</li>
          <li>Find the confirmed booking you wish to cancel in the bookings list.</li>
          <li>Click the <strong>Cancel Booking</strong> button on the bottom of the card.</li>
          <li>Review the cancellation confirmation popup and click confirm.</li>
        </ol>
        <h3>Refund Processing Timelines</h3>
        <p>Refunds are automatically approved and dispatched back to the original card used during payment. The time it takes for funds to appear depends on your bank provider:</p>
        <ul>
          <li><strong>Credit Cards / Debit Cards</strong>: 5 to 10 business days.</li>
          <li><strong>NetBanking / UPI</strong>: 2 to 5 business days.</li>
        </ul>
        <p>If you experience any issues or do not receive your refund within 10 business days, please open a support ticket mapping the affected Booking ID.</p>
      `
    },
    {
      id: 'qr-codes',
      title: 'QR Codes & Gate Entry Check-in',
      summary: 'Find out where to display your QR codes and how gate check-ins work.',
      content: `
        <h3>Locating your entry QR code</h3>
        <p>A unique, encrypted entry QR code is generated for every successfully confirmed ticket booking. You can access it by following these steps:</p>
        <ol>
          <li>Go to the <strong>My Bookings</strong> page.</li>
          <li>Ensure your filter is set to <strong>Confirmed Bookings</strong>.</li>
          <li>Click the <strong>View Ticket (QR)</strong> button.</li>
          <li>A modal containing your ticket details and a visual check-in QR code will pop up.</li>
        </ol>
        <h3>How venue check-in works</h3>
        <p>Present the QR code on your mobile device at the venue entrance. The gate staff will scan the code using the EventManagement organizer check-in portal to confirm your entry. You do not need to print the ticket; a digital copy is fully acceptable and recommended.</p>
      `
    },
    {
      id: 'ticket-tiers',
      title: 'Ticket Tier Comparison (General, VIP, Backstage)',
      summary: 'Understand the features, benefits, and price differences of each tier.',
      content: `
        <h3>General Admission</h3>
        <p>General Admission tickets grant standard entry to the physical, virtual, or hybrid event space. Seating is on a first-come, first-served basis unless explicitly stated otherwise by the organizer.</p>
        <h3>VIP Experience</h3>
        <p>VIP tickets include premium benefits, which typically feature:</p>
        <ul>
          <li>Priority fast-track entry lines at the gates.</li>
          <li>Dedicated front-row or preferred seating zones.</li>
          <li>Welcome refreshments and VIP lounge access.</li>
        </ul>
        <h3>Backstage Passes</h3>
        <p>Backstage passes offer the ultimate event experience, including:</p>
        <ul>
          <li>All benefits included in the VIP tier.</li>
          <li>Exclusive meet-and-greet sessions with speakers, performers, or coordinators.</li>
          <li>Premium food and catering access.</li>
          <li>Behind-the-scenes guided tours before the event starts.</li>
        </ul>
      `
    },
    {
      id: 'pending-payment',
      title: 'Charged Payments showing as "Pending"',
      summary: 'What to do if your bank account was debited but the ticket remains pending.',
      content: `
        <h3>Why is my booking pending?</h3>
        <p>In very rare cases of high-traffic ticketing or merchant bank delays, the payment confirmation webhook from Stripe might take a few minutes to reach our servers. During this time, the booking is held in a "Pending" state.</p>
        <h3>What you should do</h3>
        <ul>
          <li><strong>Do not re-book or pay again</strong>: This avoids duplicate charges on your card.</li>
          <li><strong>Refresh the dashboard</strong>: Wait 5 minutes and reload your bookings dashboard to see if the payment status has updated to Confirmed.</li>
          <li><strong>Contact Support</strong>: If your booking remains pending for more than 30 minutes and you have received a charge notification from your bank, please raise a support ticket below. Be sure to specify the Cardholder Name and transaction time.</li>
        </ul>
      `
    },
    {
      id: 'virtual-sessions',
      title: 'Accessing Virtual & Hybrid Event Links',
      summary: 'How to join online live streams, video meetings, or virtual halls.',
      content: `
        <h3>Joining the Live Event</h3>
        <p>If you booked a Virtual or Hybrid event tier, a direct join link is integrated right into your ticket details. Here is how to access it:</p>
        <ol>
          <li>Open your <strong>My Bookings</strong> dashboard.</li>
          <li>Locate the virtual/hybrid event card under Confirmed Bookings.</li>
          <li>Click the <strong>Join Meeting</strong> button.</li>
          <li>This will launch the live-stream or video conference room (e.g. Zoom, Google Meet, or MS Teams) in a new browser tab.</li>
        </ol>
        <p>Note: The "Join Meeting" button is unlocked and clickable starting 15 minutes before the event start time. Please make sure your browser permits pop-ups to open meeting links successfully.</p>
      `
    }
  ]);

  // Selected help topic look up
  public selectedTopic = computed(() => {
    const activeId = this.selectedTopicId();
    if (!activeId) return null;
    return this.helpTopics().find(topic => topic.id === activeId) || null;
  });

  // Filtered help topics based on search input keywords
  public filteredHelpTopics = computed(() => {
    const kw = this.searchKeyword().toLowerCase().trim();
    if (!kw) return this.helpTopics();
    return this.helpTopics().filter(topic => 
      topic.title.toLowerCase().includes(kw) || 
      topic.summary.toLowerCase().includes(kw)
    );
  });

  // Tickets raised list
  public currentTickets = signal<SupportTicket[]>([]);

  private subscriptions = new Subscription();

  constructor(
    private bookingService: BookingService,
    private authService: AuthService,
    private store: AppStoreService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Check authentication status
    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(logged => {
        this.isLoggedIn.set(logged);
        if (logged) {
          this.loadUserBookings();
        }
      })
    );

    // Initialize support tickets list from LocalStorage if exists
    const storedTickets = localStorage.getItem('raisedSupportTickets');
    if (storedTickets) {
      try {
        this.currentTickets.set(JSON.parse(storedTickets));
      } catch {
        this.currentTickets.set([]);
      }
    }
  }

  private loadUserBookings(): void {
    this.bookingService.getMyBookings().subscribe(res => {
      this.bookings.set(res || []);
    });
  }

  public selectTopic(topicId: string): void {
    this.selectedTopicId.set(topicId);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  public clearFilters(): void {
    this.searchKeyword.set('');
  }

  public submitSupportTicket(): void {
    this.ticketSuccessMessage.set('');
    this.ticketErrorMessage.set('');

    if (!this.ticketCategory) {
      this.ticketErrorMessage.set('Please select a support category.');
      return;
    }
    if (!this.ticketSubject.trim()) {
      this.ticketErrorMessage.set('Please enter a ticket subject.');
      return;
    }
    if (!this.ticketDetails.trim()) {
      this.ticketErrorMessage.set('Please provide the details/description of the issue.');
      return;
    }

    const newTicket: SupportTicket = {
      ticketId: `TKT-${Math.floor(Math.random() * 90000) + 10000}`,
      bookingId: this.ticketBookingId || 'General',
      category: this.ticketCategory,
      subject: this.ticketSubject.trim(),
      details: this.ticketDetails.trim(),
      status: 'Open',
      createdAt: new Date().toLocaleString('en-IN')
    };

    const updated = [newTicket, ...this.currentTickets()];
    this.currentTickets.set(updated);
    localStorage.setItem('raisedSupportTickets', JSON.stringify(updated));

    this.ticketSuccessMessage.set(`Success! Support ticket #${newTicket.ticketId} has been successfully raised. Our team will review it shortly.`);

    // Clear form
    this.ticketBookingId = '';
    this.ticketCategory = '';
    this.ticketSubject = '';
    this.ticketDetails = '';
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
