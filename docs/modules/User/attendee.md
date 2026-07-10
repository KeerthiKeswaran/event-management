# Attendee Operations Module

This module governs all primary actions an attendee (user) can perform on the platform after logging in.

## 1. Files & Components Involved

### Controllers
* **UserController.cs**
  * **Path:** [UserController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/UserController.cs)
  * **Endpoints:**
    * `POST api/user/select-regions`
    * `GET api/user/profile`
    * `PUT api/user/profile`
* **EventController.cs**
  * **Path:** [EventController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/EventController.cs)
  * **Attendee Endpoints:**
    * `GET api/event` (Browse)
    * `GET api/event/{id}` (Details)
    * `GET api/event/recommended` (Region recommendations)
    * `POST api/event/{id}/report` (Report/Flag event)
    * `POST api/event/{id}/feedback` (Post-event review)
* **BookingController.cs**
  * **Path:** [BookingController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/BookingController.cs)
  * **Endpoints:**
    * `POST api/booking` (Initiate ticket request)
    * `POST api/booking/{id}/confirm` (Verify Stripe payment)
    * `GET api/booking` (List user tickets)
    * `POST api/booking/{id}/cancel` (Initiate refund policy)
* **SupportController.cs**
  * **Path:** [SupportController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/SupportController.cs)
  * **Endpoints:**
    * `POST api/support/tickets` (File support concern)

### Contracts & Interfaces
* **IUserService.cs** -> [IUserService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IUserService.cs)
* **IBookingService.cs** -> [IBookingService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IBookingService.cs)
* **IEventService.cs** -> [IEventService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IEventService.cs)
* **IRefundService.cs** -> [IRefundService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IRefundService.cs)
* **ISupportService.cs** -> [ISupportService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/ISupportService.cs)

### Services
* **UserService.cs** -> [UserService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/UserService.cs)
* **BookingService.cs** -> [BookingService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/BookingService.cs)
* **EventService.cs** -> [EventService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/EventService.cs)
* **RefundService.cs** -> [RefundService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/RefundService.cs)
* **SupportService.cs** -> [SupportService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/SupportService.cs)

---

## 2. Activity & State Flowcharts

### I. Selecting & Updating Interested Regions
This flow handles modifying user profile details and mapping region preferences to build recommendations.

```text
                         [ START PROFILE UPDATE ]
                                    │
                                    ▼
                    [ Read Name, Phone, RegionIds DTO ]
                                    │
                                    ▼
                         [ Get Authenticated ID ]
                          (GetUserId from Claims)
                                    │
                                    ▼
                         [ Fetch User Profile ]
                           (IUserRepository)
                                    │
                                    ▼
                            { User profile }
                            {   exists?    }
                             /          \
                    [No]   /              \ [Yes]
                          ▼                ▼
                 (Throw NotFound)    [ Modify Name & Mobile ]
                                            │
                                            ▼
                                     [ Persist User DB ]
                                            │
                                            ▼
                                  [ Loop Region IDs ]
                                  - Remove stale user-interested relations
                                  - Add new region mappings to join table
                                            │
                                            ▼
                                     [ Save Changes ]
                                            │
                                            ▼
                                     [ END / Success ]
```

---

### II. Browsing & Seeding Events
Allows attendees to search through physical or virtual events, either by applying search parameters or fetching personalized feeds matching their profile regions.

```text
                         [ START BROWSE EVENTS ]
                                    │
                                    ▼
                    [ Read Search Queries from Request ]
                    (keyword, category, minDate, regionId, paging)
                                    │
                                    ▼
                       { Is Recommended Only requested? }
                        /                              \
               [Yes]   /                                \ [No]
                      ▼                                  ▼
             [ Fetch User's Regions ]             [ Query Events DB directly ]
             (User interested region IDs)         (Filter by search parameters)
                      │                                  │
                      ▼                                  │
             [ Query Events matching regions ]           │
                      │                                  │
                      ▼                                  ▼
                      └───────────────┬──────────────────┘
                                      │
                                      ▼
                           [ Return Paginated List ]
                           - Event title, type, venue, date
                                      │
                                      ▼
                               [ END / Success ]
```

---

### III. Ticket Booking Lifecycle
Traces the ticket purchase flow from initiating seating validations, locking inventory, collecting payment, generating the secure entry QR, and queuing confirmation emails.

```text
                         [ START TICKET BOOKING ]
                                    │
                                    ▼
                       [ Read EventId & TierQuantities ]
                                    │
                                    ▼
                            [ Verify Event State ]
                          - Query Event details from DB
                                    │
                                    ▼
                            { Is Event Live? }
                             /            \
                    [No]   /                \ [Yes]
                          ▼                  ▼
                (Return failure)     [ Check Seat Capacity ]
                                     - Sum quantities requested
                                     - Query EventTicketTiers sold count vs
                                       VenueSeatCapacities total seats
                                                 │
                                                 ▼
                                     { Is capacity available? }
                                       /                     \
                              [No]   /                         \ [Yes]
                                    ▼                           ▼
                           (Return failure)             [ Create Pending Booking ]
                                                        - Booking_Status: "Payment Pending"
                                                        - Add BookingDetails records
                                                        - Save changes to DB
                                                                 │
                                                                 ▼
                                                        [ Send Stripe Secret ]
                                                        - Return booking entity
                                                                 │
                                                                 ▼
                                                       [ Attendee Pays Card ]
                                                       - Stripe card transaction
                                                                 │
                                                                 ▼
                                                        [ Confirm Payment API ]
                                                        - Read bookingId & charge reference
                                                                 │
                                                                 ▼
                                                        { Charge matches booking? }
                                                          /                     \
                                                 [No]   /                         \ [Yes]
                                                       ▼                           ▼
                                              (Return failure)            [ Update Booking Status ]
                                                                          - Booking_Status: "Confirmed"
                                                                                   │
                                                                                   ▼
                                                                          [ Generate Secure QR ]
                                                                          - Create check-in hash
                                                                          - Save QR image to assets
                                                                                   │
                                                                                   ▼
                                                                          [ Create Transaction Ledger ]
                                                                          - Create 'BookingPayment' log
                                                                          - Create Escrow transaction
                                                                                   │
                                                                                   ▼
                                                                          [ Queue Email Ticket ]
                                                                          - Build confirmation mail
                                                                          - Relay to SMTP sender
                                                                                   │
                                                                                   ▼
                                                                            [ END / Confirmed ]
```

---

### IV. Booking Cancellation & Refund Policy Check
Implements dynamic refund logic depending on the cancellation timeline relative to event start time (90% refund if >48 hours, 50% refund if 12–48 hours, non-refundable if <12 hours).

```text
                       [ START CANCELLATION ]
                                  │
                                  ▼
                         [ Fetch Booking Info ]
                         - Retrieve event start date & payments
                                  │
                                  ▼
                       { Check Policy / Timeline }
                         /          |          \
                       /            |            \
        [ > 48 Hours ]/      [ 12-48 Hours ]      \ [ < 12 Hours ]
                     ▼              ▼              ▼
              [ Calculate 90% ][ Calculate 50% ] [ Calculate 0% ]
                     │              │              │
                     └──────────────┼──────────────┘
                                    ▼
                           { Refund Amount > 0? }
                            /                  \
                   [No]    /                    \ [Yes]
                          ▼                      ▼
                 [ Revert Booking ]       [ Request Stripe Refund ]
                 - Revert seat count      - Send charge reference
                 - Mark status Cancelled           │
                          │                        ▼
                          │               { Refund Success? }
                          │                /               \
                          │       [No]    /                 \ [Yes]
                          │              ▼                   ▼
                          │       (Throw Error)     [ Save Transaction Ledger ]
                          │                         - Log BookingRefund transaction
                          │                         - Set booking: 'Cancelled'
                          │                         - Set payment: 'Refunded'
                          │                                  │
                          ▼                                  ▼
                          └─────────────────┬────────────────┘
                                            │
                                            ▼
                                    [ END / Success ]
```

---

### V. Event Feedback Submission
Allows attendees to leave reviews for completed events that they successfully attended.

```text
                        [ START SUBMIT FEEDBACK ]
                                    │
                                    ▼
                     [ Read EventId, Rating, Review ]
                                    │
                                    ▼
                         [ Verify Event History ]
                          - Query Event details
                                    │
                                    ▼
                         { Event Completed? }
                          /                \
                 [No]    /                  \ [Yes]
                        ▼                    ▼
               (Throw Validation)      [ Verify Booking ]
                                       - Query user's booking status
                                                │
                                                ▼
                                       { Ticket Confirmed? }
                                         /                \
                                [No]    /                  \ [Yes]
                                       ▼                    ▼
                             (Throw Validation)     [ Save EventFeedback ]
                                                    - Rating (1-5), Review
                                                    - Persist to database
                                                            │
                                                            ▼
                                                     [ END / Success ]
```

---

### VI. Support Ticket Query Submission
Creates a local text record (JSON concern file) containing query content and links it to a new database ticket row.

```text
                        [ START SUBMIT TICKET ]
                                    │
                                    ▼
                [ Read Subject, Message, RequestType ]
                                    │
                                    ▼
                         [ Generate Local File ]
                         - Create concern JSON content
                         - Save file under '/wwwroot/support/tickets/'
                                    │
                                    ▼
                        [ Create SupportTicket DB ]
                        - Set ConcernUrl pointing to file
                        - Status: "Open", Escalation: "Available"
                        - Persist to database
                                    │
                                    ▼
                             [ END / Success ]
```
