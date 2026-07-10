# Organizer Operations Module

This module governs all actions an organizer can perform to publish, manage, and cancel events.

## 1. Files & Components Involved

### Controllers
* **EventController.cs**
  * **Path:** [EventController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/EventController.cs)
  * **Organizer Endpoints:**
    * `POST api/event` (Create Event)
    * `POST api/event/check-staff` (Verify staff availability)
    * `POST api/event/{eventId}/confirm` (Confirm upfront payment & activate)
    * `POST api/event/{eventId}/cancel` (Cancel event & trigger refund chain)
    * `POST api/event/{eventId}/revert` (Revert pending activation event)

### Contracts & Interfaces
* **IEventService.cs** -> [IEventService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IEventService.cs)
* **IRefundService.cs** -> [IRefundService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IRefundService.cs)
* **IPaymentService.cs** -> [IPaymentService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IPaymentService.cs)
* **IVirtualMeetingService.cs** -> [IVirtualMeetingService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IVirtualMeetingService.cs)

### Services
* **EventService.cs** -> [EventService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/EventService.cs)
* **RefundService.cs** -> [RefundService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/RefundService.cs)
* **StripePaymentService.cs** -> [StripePaymentService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/StripePaymentService.cs)
* **VirtualMeetingService.cs** -> [VirtualMeetingService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/VirtualMeetingService.cs)

---

## 2. Activity & State Flowcharts

### I. Check Staff Availability
Verifies if there is adequate platform support staff in the target region before scheduling.

```text
                        [ START CHECK STAFF ]
                                  │
                                  ▼
                    [ Read VenueId & Target DateTime ]
                                  │
                                  ▼
                         [ Fetch Venue Info ]
                                  │
                                  ▼
                         { Venue exists? }
                          /             \
                 [No]    /               \ [Yes]
                        ▼                 ▼
               (Throw NotFound)    [ Fetch Region ID ]
                                            │
                                            ▼
                                   [ Calculate Seats ]
                                   - Elite + Gold + Silver totals
                                            │
                                            ▼
                                   [ Calculate Needs ]
                                   - 1 staff per 100 seats (min 1)
                                            │
                                            ▼
                                   [ Query Available Staffs ]
                                   - Find Staffs in Region who are
                                     not allocated on this DateTime
                                            │
                                            ▼
                                   { Count >= Required? }
                                    /                 \
                           [No]    /                   \ [Yes]
                                  ▼                     ▼
                        (Available = false)     (Available = true)
                                  │                     │
                                  ▼                     ▼
                                  └──────────┬──────────┘
                                             │
                                             ▼
                                      [ Return DTO ]
                                      - Cost and Staffing availability
                                             │
                                             ▼
                                      [ END / Success ]
```

---

### II. Event Creation & Upfront Fee Calculation
Initializes event setup, calculates appropriate licensing/rental fees, checks double bookings, and flags the event as `Activation Pending`.

```text
                         [ START EVENT CREATION ]
                                    │
                                    ▼
                [ Read Type, DateTime, Duration, VenueId, Tiers ]
                                    │
                                    ▼
                        { Is DateTime >= 24h away? }
                           /                      \
                  [No]    /                        \ [Yes]
                         ▼                          ▼
                (Throw Validation)         [ Fetch Platform Settings ]
                                                    │
                                                    ▼
                                             { Event Type? }
                                             /             \
                                  [Physical/Hybrid]       [Virtual]
                                        │                     │
                             { VenueId Provided? }            │
                               /               \              │
                      [No]   /                   \ [Yes]      │
                            ▼                     ▼           │
                    (Throw Valid)        [ Fetch Venue details ]      │
                                         - Verify availability        │
                                         - Check double bookings      │
                                                    │                 │
                                                    ▼                 ▼
                                           [ Calculate Fees ]  [ Calculate Fees ]
                                           - Base Activation   - Base Activation
                                           - Venue rental fee    only
                                           - Staff fee if req         │
                                                    │                 │
                                                    ▼                 ▼
                                                    └────────┬────────┘
                                                             │
                                                             ▼
                                                    [ DB Transaction ]
                                                    - Save Event: "Activation Pending"
                                                    - Save Ticket Tiers
                                                    - Create Transaction: "Pending"
                                                             │
                                                             ▼
                                                      [ Return Event ]
                                                             │
                                                             ▼
                                                     [ END / Pending ]
```

---

### III. Upfront Payment Confirmation & Event Activation
Charges the organizer's card using Stripe, configures staff assignments (for physical/hybrid events), and creates secure virtual meeting channels (for virtual/hybrid events) to publish the event as `Live`.

```text
                        [ START UPFRONT PAYMENT ]
                                    │
                                    ▼
                    [ Read EventId, StripeToken, PayMethod ]
                                    │
                                    ▼
                          [ DB Transaction Start ]
                                    │
                                    ▼
                          [ Fetch Event Details ]
                                    │
                                    ▼
                          { Status == "Pending"? }
                           /                    \
                  [No]    /                      \ [Yes]
                         ▼                        ▼
                (Throw Validation)       [ Query Pending Transaction ]
                                                    │
                                                    ▼
                                         [ Charge Card via Stripe ]
                                                    │
                                                    ▼
                                            { Charge Success? }
                                             /               \
                                    [No]    /                 \ [Yes]
                                           ▼                   ▼
                                    - Set Tx: "Failed"   - Set Tx: "Success"
                                    - Rollback DB transaction  - Log UpfrontPayment mapping
                                    - Throw Validation         - Set Event Status: "Live"
                                                                        │
                                                                        ▼
                                                             { Physical / Hybrid? }
                                                              /                  \
                                                      [Yes]  /                    \ [No]
                                                            ▼                      ▼
                                                   [ Allocate Staffs ]      [ Generate Meeting ]
                                                   - Query available staffs - Query Jitsi Service
                                                   - Add assignments to DB  - Save room url
                                                   - Mark staff: Allocated  - Hash raw password
                                                            │                      │
                                                            ▼                      ▼
                                                            └──────────┬───────────┘
                                                                       │
                                                                       ▼
                                                             [ Save Event Changes ]
                                                                       │
                                                                       ▼
                                                             [ Send Activation Email ]
                                                             - Send credentials & Jitsi password
                                                                       │
                                                                       ▼
                                                             [ DB Transaction Commit ]
                                                                       │
                                                                       ▼
                                                              [ END / Activated ]
```

---

### IV. Event Cancellation & Attendee Refund Chains
Allows organizers to cancel an active event, triggers a dynamic cancellation refund calculation for the organizer's upfront fees, and automatically processes full ticket refunds for all registered attendees.

```text
                        [ START EVENT CANCELLATION ]
                                     │
                                     ▼
                           [ Fetch Event Details ]
                                     │
                                     ▼
                            { Status == "Live"? }
                             /                 \
                    [No]    /                   \ [Yes]
                           ▼                     ▼
                  (Throw Validation)    [ Set Event: "Cancelled" ]
                                                 │
                                                 ▼
                                        [ DB Commit Status ]
                                                 │
                                                 ▼
                                        [ Trigger Refund Chain ]
                                        - Invoke RefundService.RefundOrganizerAsync
                                                 │
                                                 ▼
                                    [ Step 1: Organizer Upfront Refund ]
                                    - Check time-decay rules:
                                      * > 48h prior: 90% refund
                                      * 24-48h prior: 50% refund
                                      * < 24h prior: Non-refundable
                                    - Request Stripe refund
                                    - Create refund transaction log
                                                 │
                                                 ▼
                                    [ Step 2: Attendee Booking Refunds ]
                                    - Loop through all confirmed bookings
                                    - Query successes
                                    - Revert attendee seats count
                                    - Execute Stripe refund to each booking
                                    - Log transaction history
                                    - Update booking: "Cancelled"
                                                 │
                                                 ▼
                                        [ Step 3: Deallocate Staff ]
                                        - Remove EventStaffAllocations
                                        - Reset staff.IsAllocated to false
                                                 │
                                                 ▼
                                         [ END / Cancelled ]
```
