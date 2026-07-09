import { Routes } from '@angular/router';
import { HomeComponent } from './components/home/home';
import { LoginComponent } from './components/login/login';
import { RegisterComponent } from './components/register/register';
import { BrowseEventsComponent } from './components/browse-events/browse-events';
import { BookingComponent } from './components/booking/booking';
import { BookingsComponent } from './components/bookings/bookings';
import { CheckoutComponent } from './components/booking/checkout/checkout';
import { HelpComponent } from './components/help/help';
import { RaiseTicketComponent } from './components/help/raise-ticket/raise-ticket';
import { MyTicketsComponent } from './components/help/my-tickets/my-tickets';
import { AccountSettingsComponent } from './components/account-settings/account-settings';
import { OrganizerDashboardComponent } from './components/organizer/dashboard/dashboard';
import { OrganizerEventsComponent } from './components/organizer/events-list/events-list';
import { CreateEventComponent } from './components/organizer/create-event/create-event';
import { AdminLoginComponent } from './components/admin/login/login';
import { AdminDashboardComponent } from './components/admin/dashboard/dashboard';
import { AdminModerationComponent } from './components/admin/moderation/moderation';
import { AdminVenuesComponent } from './components/admin/venues/venues';
import { AdminHelpdeskComponent } from './components/admin/helpdesk/helpdesk';
import { AdminUsersComponent } from './components/admin/users/users';

import { canDeactivateGuard } from './guards/can-deactivate.guard';
import { adminGuard } from './guards/admin.guard';
import { PasswordResetComponent } from './components/password-reset/password-reset';
import { AdminPasswordResetComponent } from './components/admin/password-reset/password-reset';
import { LoginComponent as FinanceLoginComponent } from './components/finance/login/login';
import { DashboardComponent as FinanceDashboardComponent } from './components/finance/dashboard/dashboard';
import { TransactionsComponent as FinanceTransactionsComponent } from './components/finance/transactions/transactions';
import { EscalationsComponent as FinanceEscalationsComponent } from './components/finance/escalations/escalations';
import { PayoutsComponent as FinancePayoutsComponent } from './components/finance/payouts/payouts';

import { financeGuard } from './guards/finance.guard';

export const routes: Routes = [
  { path: '', component: HomeComponent, title: ' Home' },
  { path: 'login', component: LoginComponent, title: ' Login' },
  { path: 'user/password/reset', component: PasswordResetComponent, title: ' Reset Password' },
  { path: 'admin/login', component: AdminLoginComponent, title: ' Admin Login' },
  { path: 'admin/password/reset', component: AdminPasswordResetComponent, title: ' Admin Reset Password' },
  { path: 'register', component: RegisterComponent, title: '  Register' },
  { path: 'browse', component: BrowseEventsComponent, title: ' Browse Events' },
  { path: 'booking', component: BookingComponent, title: ' Booking' },
  { path: 'booking/checkin', loadComponent: () => import('./components/bookings/checkin/checkin').then(m => m.CheckinComponent), title: ' CheckIn' },
  { path: 'bookings', component: BookingsComponent, title: ' My Bookings' },
  { path: 'checkout', component: CheckoutComponent, canDeactivate: [canDeactivateGuard], title: ' Checkout' },
  { path: 'help', component: HelpComponent, title: ' Help' },
  { path: 'raise-ticket', component: RaiseTicketComponent, title: ' Raise Ticket' },
  { path: 'my-tickets', component: MyTicketsComponent, title: ' My Tickets' },
  { path: 'settings', component: AccountSettingsComponent, title: ' Settings' },
  { path: 'admin/dashboard', component: AdminDashboardComponent, canActivate: [adminGuard], title: ' Admin Dashboard' },
  { path: 'admin/moderation', component: AdminModerationComponent, canActivate: [adminGuard], title: ' Moderation' },
  { path: 'admin/venues', component: AdminVenuesComponent, canActivate: [adminGuard], title: ' Venues' },
  { path: 'admin/users', component: AdminUsersComponent, canActivate: [adminGuard], title: ' Users' },
  { path: 'admin/helpdesk', component: AdminHelpdeskComponent, canActivate: [adminGuard], title: ' Admin Helpdesk' },
  { path: 'admin/settings', component: AccountSettingsComponent, canActivate: [adminGuard], title: ' Admin Settings' },
  { path: 'myevents', component: OrganizerDashboardComponent, title: ' My Events' },
  { path: 'myevents/all', component: OrganizerEventsComponent, title: ' All My Events' },
  { path: 'myevents/create', component: CreateEventComponent, canDeactivate: [canDeactivateGuard], title: ' Create Event' },
  { path: 'finance/login', component: FinanceLoginComponent, title: ' Finance Login' },
  { path: 'finance/dashboard', component: FinanceDashboardComponent, canActivate: [financeGuard], title: ' Finance Dashboard' },
  { path: 'finance/transactions', component: FinanceTransactionsComponent, canActivate: [financeGuard], title: ' Transactions' },
  { path: 'finance/payouts', component: FinancePayoutsComponent, canActivate: [financeGuard], title: ' Payouts' },
  { path: 'finance/escalations', component: FinanceEscalationsComponent, canActivate: [financeGuard], title: ' Escalations' },
  { path: 'finance/settings', component: AccountSettingsComponent, canActivate: [financeGuard], title: ' Finance Settings' },
  { path: 'stripe-checkout', loadComponent: () => import('./components/booking/stripe-checkout/stripe-checkout').then(m => m.StripeCheckoutComponent), canDeactivate: [canDeactivateGuard], title: ' Payment' },
  { path: 'error', loadComponent: () => import('./components/error-page/error-page').then(m => m.ErrorPageComponent), title: ' Error' },
  { path: '**', redirectTo: '' }
];

