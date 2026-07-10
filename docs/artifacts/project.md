# Event Management Project Knowledge Base

Last updated: 2026-07-03
Scope source: verified from code under server and client plus docs artifacts.

## 1) Project Snapshot

Event Management is a full-stack platform for:
- User registration/login with OTP flows.
- Event creation and publishing (Physical, Virtual, Hybrid).
- Ticket booking and payment confirmation.
- QR-based check-in.
- Support tickets and escalation.
- Admin moderation and operations.
- Finance actions and refund decisions.
- Automated lifecycle jobs (expiry handling, completion processing, payout release).

## 2) Tech Stack (Current)

Backend:
- ASP.NET Core Web API on .NET target net10.0.
- Layered projects: Event.API, Event.Business, Event.Contracts, Event.Data, Event.Models.
- EF Core + Npgsql (PostgreSQL).
- JWT auth and role-based authorization.
- Redis cache for OTP and verification markers.
- Stripe.net for payments/refunds/payout plumbing.
- QRCoder for ticket QR generation.

Frontend:
- Angular 21 (standalone routes/components style).
- RxJS, ngx-stripe, @stripe/stripe-js.

## 3) Runtime Architecture

Request flow:
- Angular client -> Event.API controllers -> Business services -> Repository layer -> PostgreSQL.

Background workers:
- BackgroundService (every 1 minute):
	- Release expired pending bookings.
	- Release expired pending event activations.
	- Complete ended live events and perform completion actions.
- PayoutBackgroundService (every 1 minute):
	- Re-check cancelled payouts and release them if all event reports become Dismissed.

Static assets:
- API serves Event.Business/assets on route /assets.
- Asset root resolution is handled dynamically for run/test environments.

## 4) Folder Intent

Server:
- Event.API: Controllers, middleware, startup wiring.
- Event.Business: Domain logic, integrations, workers, templates, assets.
- Event.Data: DbContext, repositories, seeding.
- Event.Contracts: Service and repository interfaces.
- Event.Models: Entities and DTOs.

Client:
- src/app/components: User, organizer, admin, finance, help, booking UIs.
- src/app/services: auth, event, booking, finance, admin, region, location, pixabay services.
- src/app/guards: route protection and canDeactivate safeguards.

## 5) Authentication and Authorization

User auth:
- Routes under api/auth/user.
- OTP send/verify + register/login/reset-password.

Department auth:
- Routes under api/auth.
- Admin login requires ADM prefix IDs.
- Finance login requires FIN prefix IDs and OTP verification.

JWT/roles:
- Admin APIs guarded with [Authorize(Roles = "admin")].
- Finance APIs guarded with [Authorize(Roles = "finance")].
- General authenticated APIs guarded with [Authorize].

## 6) Primary API Surface (Controller-Level)

UserAuthController (api/auth/user):
- send-otp, verify-otp, register, login, reset-password.

DeptAuthController (api/auth):
- admin/login.
- finance/login and finance/login/verify OTP.
- forgot-password, reset-password.

UserController (api/user, authorized):
- select-regions.
- profile get/update.
- my-events, my-events/{eventId}, my-dashboard.
- close-account.

EventController (api/event):
- Public: categories, age-categories, browse, details, seats, venues, trending, popular, platform-settings.
- Authorized: recommended, report, feedback, verify-ticket, create-event, check-staff, checkout-session, confirm, upload-description, upload-image, cancel, revert.

BookingController (api/booking, authorized):
- book tickets.
- confirm payment.
- my bookings (optional status filter).
- checkout session.
- cancel booking.
- revert pending booking.
- refund estimate.
- active virtual links.

SupportController (api/support, authorized):
- submit ticket.

AdminController (api/admin, role admin):
- dashboard stats/events.
- support ticket list/respond/escalate/escalation-status.
- reports list/dismiss/uphold.
- regions, venues CRUD/update scope.
- staff directory/by-region and event-region mapping.
- manual staff allocation.
- profile get/update.
- support metadata.

FinanceController (api/finance, role finance):
- actions list.
- approve/decline action.
- respond to tickets.
- transactions list/filter/sort/paged.

RegionController:
- api/regions (public region list).
- api/regions/popular.

PoliciesController:
- api/policies/{type}.

## 7) Domain Workflow (Verified Rules)

Event creation:
- Event starts as Activation Pending.
- Upfront transaction is created with Pending status.
- Confirmation success sets event to Live.
- On payment failure/interruption, event can be reverted to Failed via revert flow.
- Policy acceptance is enforced for EventCreation terms.

Booking:
- Booking starts as Payment Pending.
- Seat counts are reserved at booking creation.
- Confirm payment sets booking to Confirmed and creates QR secret/hash/path.
- On failed confirmation or user leave flow, revert marks booking Payment Failed and releases seats.

Virtual/hybrid links:
- Booking confirm response intentionally masks Virtual_Url as Disabled.
- Active links are unlocked only during event time window through active-links endpoint.

Check-in:
- QR check-in validates secret hash, booking status Confirmed, and prevents double check-in.

Refunds:
- RefundService is cancellation/refund engine for booking and event-level scenarios.
- Attendee dynamic refund currently:
	- > 48h: 90%
	- 12h to 48h: 50%
	- < 12h: 0%
- Organizer dynamic refund currently:
	- > 48h: 90%
	- 24h to 48h: 50%
	- < 24h: 0%
- Finance/admin decisions support Full, Dynamic, Remaining, NoRefund patterns.

Event completion:
- Live events move to Completed after end time.
- Virtual links are disabled.
- Allocated staff is released.
- Feedback request notifications are sent to confirmed attendees.
- Organizer payout records are generated; payout may be cancelled if reports are active/upheld.

Dismissed-report payout release:
- If a cancelled payout event later has all reports Dismissed, payout status and related transaction are released to Success by background worker.

## 8) Asset and File Storage Conventions (Important)

Static root served by API:
- /assets -> Event.Business/assets.

Per-user asset conventions:
- QR ticket files: /assets/users/{attendeeId}/bookings/qr_{bookingId}.png
- Support ticket JSON files: /assets/users/{userId}/support/ticket_{ticketId}.json
- Event reports JSON files: /assets/users/{reporterId}/reports/report_{reportId}.json

Event content uploads:
- Description text temp files: assets/events/temp/{tempId}/description.txt
- Event image temp files: assets/events/temp/{tempId}/image.{ext}

Note:
- UploadDescription and UploadImage endpoints return relative paths beginning with assets/... and the API static-file middleware makes them publicly retrievable through /assets.

## 9) Data Model Highlights

Core entities present:
- Users, Admins, Regions, UserInterestedRegions.
- Staffs, Venues, VenueSeatCapacities.
- Events, EventTicketTiers.
- Bookings, BookingDetails, BookingPayments.
- OrganizerUpfrontPayments, OrganizerPayouts, Transactions.
- SupportTickets, AdminActions.
- EventFeedbacks, EventReports.
- PlatformSettings, TermsAndConditions, Notifications.

ID strategy:
- Most numeric IDs start at 10000 range.
- Transaction_Id uses 16-digit sequence start 1000000000000000.

## 10) Seeding and Environment Facts

Seeding behavior:
- Seed is explicit only: run API with seed argument; normal startup does not auto-seed.
- Seed script truncates public tables (except migration history) then repopulates core data.

Seed includes:
- Regions, terms, admins, settings, users, interested regions, staffs, venues, capacities, events, tiers, allocations, and more.

## 11) Client Application Map

Key routed experiences:
- Public/home/login/register/browse.
- Booking and checkout.
- User bookings/help/settings.
- Organizer portal: dashboard, events list, create flow.
- Admin portal: login/dashboard/moderation/venues/helpdesk/settings.
- Finance portal: login/dashboard/transactions/escalations/settings.

Client guard usage:
- canDeactivate guard is used for checkout and organizer create event flows.
- admin guard and finance guard protect role-specific dashboards.

## 12) Build and Run Commands

Backend:
- Restore/build:
	- dotnet restore
	- dotnet build
- Run API:
	- dotnet run --project server/Event.API
- Seed:
	- dotnet run --project server/Event.API seed

Frontend:
- Install and run:
	- npm install
	- npm start

EF migration/update pattern:
- dotnet ef migrations add <Name> --project Event.Data --startup-project Event.API
- dotnet ef database update --project Event.Data --startup-project Event.API

## 13) Known Important Status Values

Event statuses seen in code:
- Activation Pending, Live, Completed, Cancelled, Failed.

Booking statuses seen in code:
- Payment Pending, Confirmed, Cancelled, Payment Failed.

Check-in statuses:
- Pending, Checked-In.

## 14) Reliability and Safety Behaviors

- Most critical flows wrap transactional operations with begin/commit/rollback.
- Revert APIs exist for interrupted checkout flows both for booking and event activation.
- Background jobs clean stale pending records automatically.
- OTP data and verification markers are TTL-based in Redis cache.

## 15) What To Trust as Source of Truth

When docs and behavior differ:
- Prefer service/controller code in server over older markdown notes.
- Treat this file as the current working knowledge base for ongoing implementation.

## 16) Current Readiness

Project understanding status: Ready to work.

I can now implement features/fixes with context on:
- API surface and auth boundaries.
- Booking/event/payment/refund lifecycle.
- Asset storage conventions.
- Frontend routing and module ownership.
