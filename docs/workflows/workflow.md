# Event Management & Ticketing Platform
## System Blueprint & End-to-End Workflow Walkthrough

---

## 1. Project Overview & Architecture
The **Event Management & Ticketing Platform** is an enterprise-grade web application designed for listing, hosting, booking, and managing public and hybrid events. 

### Technology Stack
* **Frontend:** Angular 17+ (featuring modern styling, responsive layouts, and Jitsi Meet IFrame SDK integration).
* **Backend:** ASP.NET Core Web API (layered architecture separating API Controllers, Business Logic Services, Contracts/DTOs, Data Repository mappings, and domain Entities).
* **Database:** PostgreSQL (structured with relational tables, foreign key constraints, and transactional safety).
* **Payment Gateway:** Stripe API (handling attendee ticket checkouts, organizer upfront activation/staff fees, and admin-triggered Connected Account payouts).
* **Video Streaming:** Jitsi Meet API (providing free, secure, passcode-protected video streams for virtual and hybrid events).

---

## 2. Core System Requirements
* **Unified User Accounts:** A single user login acts as both an **Attendee** (browses events, books tickets, and cancels bookings) and an **Organizer** (creates events, books support staff, manages virtual links, and requests payouts).
* **Dedicated Administrator Panel:** Admins log in using pre-seeded alphanumeric IDs (e.g. `'ADM_1001'`) and passwords. They control platform-wide settings, manage regions and venues, assign support staff, and trigger payouts.
* **Hybrid and Virtual Events:** Organizers can publish:
  * *Virtual events:* Hosted online via an auto-generated Jitsi room. Free or paid.
  * *Physical events:* Hosted at a registered physical venue. Linked to hourly rental costs and seat tier capacities (Elite, Gold, Silver).
  * *Hybrid events:* Physical events that are also broadcasted online to remote attendees.
* **Organizer Upfront Fees:** Every event requires an upfront payment to go live. This payment covers a platform activation fee, plus venue hourly rental fees and platform support staff costs (if applicable).
* **Ticket Reservation & Checkout:** When booking physical seats, the platform locks the requested capacity and starts a transaction timer. Stripe handles checkout session limits, and once paid, the platform updates the booking to `Confirmed` and logs a secure QR ticket code path.
* **Escrow & Admin-Verified Payouts:** Attendee ticket collections are held in the platform's Stripe balance. After the event concludes, the admin re-authenticates with secure credentials and initiates a bank transfer to the organizer, minus the platform commission.
* **Central Transactions Ledger:** All cash inflows and outflows are recorded in a central `Transactions` table to ensure audit trail integrity.

---

## 3. Database Schema Overview
For full column details, refer to the [schema.md](file:///path/to/schema.md) file. The platform holds 20 core tables:

1. **`Users`**: Standard customer profiles.
2. **`Admins`**: Platform administrator accounts.
3. **`UserInterestedRegions`**: Junction table mapping users to their preferred regions.
4. **`Management` (Regions)**: Holds geographic regions and support staff counts.
5. **`Staffs`**: Platform employees available for event assignment.
6. **`Venues`**: Physical event locations with hourly prices and availability.
7. **`VenueSeatCapacities`**: Bounded physical seats per tier (Elite, Gold, Silver).
8. **`Events`**: Metadata, type (Virtual/Physical/Hybrid), duration, and Jitsi links.
9. **`EventTicketTiers`**: Ticket prices and quantities sold per tier.
10. **`Bookings`**: Reservation statuses and secure QR image paths.
11. **`BookingDetails`**: Seat tier and quantity purchased per booking.
12. **`BookingPayments`**: attendee payments for tickets.
13. **`OrganizerUpfrontPayments`**: Organizer payments for activation/venues/staff.
14. **`OrganizerPayouts`**: Net platform payout transfers back to organizers.
15. **`Transactions`**: Central ledger storing Stripe references and audit trails.
16. **`PlatformSettings`**: Global rates (staff fees, commission percentage, ticket limits).
17. **`EventStaffAllocations`**: Support staff assigned to physical/hybrid events.
18. **`SupportQueries`**: Support ticket messages and resolution replies.
19. **`EventFeedback`**: Event ratings and comments submitted by attendees.
20. **`EventReports`**: Policy violations flagged by users for admin moderation.

---

## 4. End-to-End Workflow Diagrams (Visual Text Layout)

### User Workflow (Attendee & Organizer Lifecycle)
```text
               +-------------------------------------------+
               |          User Registration & Login        |
               +-------------------------------------------+
                                     |
                                     v
               +-------------------------------------------+
               |               User Dashboard              |
               +-------------------------------------------+
                /                                         \
               / (Organizer Action)             (Attendee) \
              v                                             v
     +-------------------+                         +------------------+
     |   Create Event    |                         |  Browse Events   |
     | (Virtual/Physical)|                         |  (Starts After   |
     +-------------------+                         |    30 Minutes)   |
              |                                    +------------------+
              v                                             |
     +-------------------+                                  v
     | Calculate Upfront |                         +------------------+
     |   Activation Fee  |                         |   Book Tickets   |
     +-------------------+                         |  (Lock Capacity) |
              |                                    +------------------+
              v                                             |
     +-------------------+                                  v
     |  Stripe Checkout  |                         +------------------+
     |   (Upfront Fee)   |                         | Stripe Checkout  |
     +-------------------+                         |   (Ticket Fee)   |
        /             \                            +------------------+
  (Paid)       (Failed)                              /             \
      v                 v                      (Paid)       (Failed)
+------------+   +------------+                  v                 v
| Event Live |   |Transaction |            +-----------+     +------------+
| (Jitsi Set)|   |  Log: Fail |            |  Booking  |     | Release    |
+------------+   +------------+            | Confirmed |     | Capacity & |
      |                                    | (Gen QR)  |     | Cancel Book|
      v                                    +-----------+     +------------+
+------------+                                   |
| Event      |                                   v
| Completed  |                             +-----------+
| (Jitsi End)|                             |   Event   |
+------------+                             | Check-In  |
      |                                    +-----------+
      v
+------------+
|Admin Payout|
| (Stripe)   |
+------------+
      |
      v
+------------+
|Transaction |
| Log: Payout|
+------------+
```

### Admin Workflow (Management, Settings, & Payouts)
```text
               +-------------------------------------------+
               |               Admin Login                 |
               +-------------------------------------------+
                                     |
                                     v
               +-------------------------------------------+
               |             Admin Dashboard               |
               +-------------------------------------------+
                /                    |                    \
               /                     |                     \
              v                      v                      v
     +------------------+   +------------------+   +------------------+
     | Seed Management  |   | Seed Venues and  |   | View Completed   |
     | & Regional Staff |   | Seat capacities  |   |    Financials    |
     +------------------+   +------------------+   +------------------+
                                                            |
                                                            v
                                                   +------------------+
                                                   | Trigger Payout   |
                                                   |  (Password Auth) |
                                                   +------------------+
                                                            |
                                                            v
                                                   +------------------+
                                                   | Stripe Connect   |
                                                   |  Host Payout     |
                                                   +------------------+
                                                            |
                                                            v
                                                   +------------------+
                                                   | Log Transaction  |
                                                   | (Success/Failed) |
                                                   +------------------+
```

---

## 5. End-to-End Workflow (Step-by-Step: User vs. Admin)
Below is the chronological timeline tracking what the **User (Attendee/Organizer)** and **Admin** perform side-by-side throughout the lifecycle of the platform:

| Step | User (Attendee / Organizer) Workflow | Admin Workflow |
| :--- | :--- | :--- |
| **1. System Setup** | *Organizer/Attendee:* Navigates to the platform web page. Registers an account. Accepts terms and data-share consents (mandatory) and marketing opt-ins (optional). Selects geographic regions of interest. | *Admin:* Database is pre-seeded with admin profiles (e.g. `'ADM_1001'`). Admin logs in, updates **`PlatformSettings`** (rates, commissions, ticket limits), and seeds regions (**`Management`**), support staff (**`Staffs`**), and physical **`Venues`** with seat tiers and hourly prices. |
| **2. Event Creation** | *Organizer:* Decides to host an event. Fills the form choosing the event type:<br>• **Virtual:** Enters title, description, time. Bypasses venue configuration.<br>• **Physical/Hybrid:** Selects a venue. The system checks venue availability and calculates cost (`Duration_Hours * Hourly_Price`). Sets seat tier prices.<br>• *Optional:* Requests support staff. System calculates staff needed and checks availability in that region. | *Platform Backend:* Validates that the venue is available and that unallocated support staff exist in the region. Calculates the total upfront fee based on the selected venue, duration, requested staff count, and global settings. |
| **3. Publishing & Activation** | *Organizer:* Redirects to Stripe Checkout to pay the upfront fee (activation + venue + staff).<br>• *On success:* Backend sets the event to `'Live'`, registers the transaction in `Transactions` (Success) and `OrganizerUpfrontPayments`, and generates a secure Jitsi room and passcode.<br>• *Broadcast:* Platform notifies all users who listed the event's region as an interested region. | *Platform Backend:* If the organizer requested support staff, the platform automatically allocates available regional employees to the event in `EventStaffAllocations` and locks their schedule for that timeslot. |
| **4. Ticket Booking** | *Attendee:* Browses the dashboard (filtered by interested regions). Selects a `'Live'` event (must start at least 30 minutes in the future). Selects seat tiers and quantities (limited by `Max_Tickets_Per_Booking` and venue capacity). Clicks book. | *Platform Backend:* Executes a thread-safe check. Decrements capacity in `EventTicketTiers.Tickets_Sold`. Creates a `Booking` record (Status: `'Payment Pending'`) and logs a `'Pending'` transaction in `Transactions`. |
| **5. Booking Payment** | *Attendee:* Redirected to Stripe Checkout to pay for tickets.<br>• *On success:* Stripe Webhook fires. Backend creates `BookingPayments` & `BookingDetails` records, updates Booking to `'Confirmed'`, generates a unique digital QR ticket path (`Qr_Code_Path`), and marks the ledger transaction as `'Success'`.<br>• *On failure/timeout:* Seats are released (re-incremented) and Booking becomes `'Payment Failed'`. | *Platform Backend:* Monitors payment callbacks. If a checkout session expires, the backend automatically cancels the booking and frees up the seat capacity back to the event pool. |
| **6. Day of Event & Entry** | *Attendee:* Arrives at the physical venue and presents their QR ticket code.<br><br>*Virtual/Hybrid Attendee:* Clicks the Jitsi link in their dashboard. The platform validates their booking, launches the Jitsi frame, and automatically injects the room passcode via the SDK.<br><br>*Organizer:* Scans attendee QR codes at the door using their dashboard camera tool. | *Organizer/Staff:* The platform decrypts the scanned QR code, verifies the digital signature, checks if the ticket is valid for this event, and marks the attendee as checked-in (`Booking_Status` verified) to prevent double entry. |
| **7. Completion & Payout** | *Organizer:* Host event. Once the event date/time passes, the platform changes the event status to `'Completed'`. The virtual meeting room is deleted, and Jitsi links are greyed out on the dashboard. The organizer requests a payout. | *Admin:* Log into the admin portal. Views completed events and total sales. Clicks "Payout". Re-enters credentials for re-authentication. The platform calls Stripe Connect to transfer the net sales (`Total_Ticket_Sales - Commission`) to the host's bank. Logs a `'Success'` or `'Failed'` payout transaction. |
| **8. Cancellations & Refunds** | *Attendee:* Cancels booking (subject to timeline rules).<br>• *On success:* Receives a Stripe refund. Booking is marked `'Cancelled'`, capacity is released, and a `'BookingRefund'` transaction is logged as `'Success'`.<br><br>*Organizer:* Cancels their event.<br>• *On success:* Event becomes `'Cancelled'`. Staff allocations are deleted, and all attendees are automatically refunded. | *Platform Backend:* Manages refund execution. Reverts allocated support staff schedule states to unallocated (`IsAllocated = false`). Processes mass Stripe refunds for all ticket holders of the cancelled event. |

---

## 6. Security & Edge Case Handling
* **Concurrency Protection (Double Booking):** Ticket purchases execute thread-safe operations in the DB context (e.g. using optimistic concurrency or locks) to prevent selling more tickets than the physical venue capacity allows.
* **Secure Meetings:** Jitsi links and passcodes are never exposed to public GET endpoints. Only the organizer and attendees with a confirmed booking (`Booking_Status == 'Confirmed'`) can retrieve the passcode.
* **Audit Trail Integrity:** The `Transactions` table serves as an immutable ledger. Once a transaction record is created, it is updated (Success/Failed) but never deleted.
* **Payout Gatekeeping:** To prevent unauthorized money transfers, the payout API enforces a two-factor validation: it checks that the event status is `'Completed'` and requires the active admin to submit their password to complete the API request.
