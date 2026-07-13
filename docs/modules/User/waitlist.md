# Waitlist Feature Implementation

This document details the implementation of the Waitlist system, allowing users to queue for sold-out ticket tiers and get automatically notified when capacity frees up (e.g., due to booking cancellations or capacity upgrades).

---

## 1. Data Schema Changes

A new database entity `Waitlist` was introduced to track user queue positions and status.

### The `Waitlist` Table Schema
| Column | Type | Description |
| :--- | :--- | :--- |
| `Waitlist_Id` | INT (PK) | Primary identifier for the waitlist entry. |
| `Event_Id` | INT (FK) | Reference to the `Events` table. |
| `Attendee_Id` | INT (FK) | Reference to the `Users` table for the attendee. |
| `Tier_Name` | STRING | The specific ticket tier requested (e.g., "VIP"). |
| `Quantity` | INT | Number of tickets requested in this waitlist entry. |
| `Status` | STRING | Current state: `Waiting`, `Notified`, `Booked`, `Expired`, `Cancelled`. |
| `Position` | INT | The user's queue position for this specific event and tier. |
| `Joined_At` | DATETIME | Timestamp when the user joined the waitlist. |
| `Notified_At` | DATETIME? | Timestamp when the user was notified of availability. |
| `Expires_At` | DATETIME? | Timestamp when the user's booking window expires. |
| `Booking_Id` | INT? (FK) | Reference to the resulting `Bookings` table (if successfully booked). |

---

## 2. Dynamic Expiry Logic

When capacity frees up, the system must assign a window of time for the notified user to complete their booking before passing the spot to the next person. The `WaitlistService.cs` implements a dynamic expiry window based on the proximity to the event start date:

*   **> 72 Hours until event:** The user is given **24 hours** to complete the booking.
*   **24 to 72 Hours until event:** The user is given **2 hours** to complete the booking.
*   **< 24 Hours until event:** The user is given **30 minutes** to complete the booking.

---

## 3. Background Job Automation

The `BackgroundService.cs` executes periodic cron jobs to manage waitlist lifecycles automatically:

### A. Expire Stale Waitlists
When a user misses their booking window (status `Notified`, but current time > `Expires_At`), the background service `ExpireStaleWaitlistAsync()`:
1. Updates the entry status to `Expired`.
2. Shifts the `Position` of all remaining `Waiting` users in the queue up by 1.
3. Automatically calls `ProcessWaitlistForEventTierAsync()` to notify the next person in line.

### B. Close Waitlists for Starting Events
Events are no longer visible on the public browsing page 5 minutes before they go live. At this exact 5-minute mark, the `CloseWaitlistForStartingEventsAsync()` job:
1. Identifies all leftover entries stuck in `Waiting` or `Notified` status for the imminent event.
2. Updates their status to `Closed`.
3. Dispatches a casual "Waitlist Closed" notification email to these users, informing them they couldn't secure a spot before the event started.

---

## 4. State-by-State Workflow Tree

```text
[State: User Hits "Join Waitlist" on Sold-Out Tier]
 └── [Action: POST /api/waitlist/join]
      │
      └── (Service Layer: WaitlistService.cs)
           └── call: JoinWaitlistAsync(userId, req)
                │
                └── [State: Entry Created in DB]
                     └── Status: "Waiting", Position calculated via MAX(Position) + 1
                          │
                          └── [State: Idle in Queue]

[State: Capacity Opens Up (e.g., Booking Cancelled)]
 └── [Action: RefundService/BookingService triggers process]
      │
      └── (Service Layer: WaitlistService.cs)
           └── call: ProcessWaitlistForEventTierAsync(eventId, tierName, freedQuantity)
                │
                ├── [Condition: Available Quantity >= Waitlist Entry Quantity]
                │    └── call: _waitlistRepository.GetNextInLineAsync()
                │         │
                │         └── [State: Entry Status Updated]
                │              ├── Status -> "Notified"
                │              ├── Expires_At -> Calculated dynamically (24h/2h/30m)
                │              │
                │              └── [Action: Email Dispatch]
                │                   └── call: NotificationHelper.SendAndSaveNotificationAsync()
                │                        └── (Sends WaitlistNotificationTemplate.html)
                │
                └── [State: User Notified]
                     │
                     ├── [Action: User books within expiry window]
                     │    └── Status -> "Booked"
                     │
                     └── [Action: User misses window / Background cron runs]
                          └── call: ExpireStaleWaitlistAsync()
                               │
                                └── [State: Entry Status Updated]
                                     ├── Status -> "Expired"
                                     │
                                     └── [Action: Queue Shift]
                                          ├── Remaining entries Position = Position - 1
                                          └── Recursively calls ProcessWaitlistForEventTierAsync() for the next person

[State: Event Start Time is <= 5 Minutes Away]
 └── [Action: Background cron runs]
      └── call: CloseWaitlistForStartingEventsAsync()
           │
           └── [State: Entry Status Updated]
                ├── Status -> "Closed" (For all remaining Waiting/Notified)
                │
                └── [Action: Email Dispatch]
                     └── call: NotificationHelper.SendAndSaveNotificationAsync()
                          └── (Sends "Waitlist Closed, Better luck next time" email)
```

---

## 5. API Endpoints

The `WaitlistController` exposes the following endpoints to interact with the waitlist system:

### 1. Join Waitlist
*   **Endpoint:** `POST /api/waitlist`
*   **Authentication:** Requires valid JWT (Any Role)
*   **Description:** Allows a user to join the waitlist for a specific event and ticket tier. The backend calculates their `Position` based on the current queue size and assigns them a `Waiting` status.
*   **Payload:** `{ "eventId": 10001, "tierName": "VIP", "quantity": 2 }`

### 2. Get My Waitlist
*   **Endpoint:** `GET /api/waitlist/mine`
*   **Authentication:** Requires valid JWT (User)
*   **Description:** Retrieves a list of all active waitlist entries (both `Waiting` and `Notified` statuses) belonging to the authenticated user. Includes metadata like event title, queue position, and expiry windows.

### 3. Cancel Waitlist Entry
*   **Endpoint:** `DELETE /api/waitlist/{waitlistId}`
*   **Authentication:** Requires valid JWT (User)
*   **Description:** Allows a user to voluntarily leave the waitlist or forfeit their notified spot. The backend automatically shifts the queue up for remaining users. If the cancelling user had a `Notified` status, the next person in line is automatically triggered.

### 4. Get Event Waitlist (Organizer/Admin)
*   **Endpoint:** `GET /api/waitlist/event/{eventId}`
*   **Authentication:** Requires valid JWT (Roles: `Admin`, `Organizer`)
*   **Description:** Retrieves the complete waitlist queue for a specific event, allowing organizers and administrators to gauge demand for sold-out tiers.

---

## 6. UI Integration & Animation

When a user successfully joins a waitlist via the public booking page, the standard browser `alert()` is bypassed in favor of an immersive UI animation.

*   **Trigger:** Successful response from `POST /api/waitlist`.
*   **Visuals:** A full-screen, semi-transparent white overlay (`.success-tick-overlay`) fades in. A dynamic SVG checkmark stroke animation plays, giving the user immediate, satisfying feedback.
*   **Messaging:** The UI dynamically parses the backend response. If the user was placed in the queue, it renders: *"Joined Waitlist. Position: X"*. If seats were instantly available (due to concurrency), it alerts them that they were booked directly.
*   **Lifecycle:** The animation holds for exactly 2 seconds before smoothly unmounting, returning the user to their flow.
