# UI Wireframes & Layout Specification

This document details the complete User Interface (UI) wireframe designs, route mappings, and navigation flows for the **Event Management & Ticketing Platform**. It covers all aspects of the Attendee, Organizer, and Admin portals.

---

## 1. Page Wireframe Specifications & Local Route Connections

### Page 1: /register (Attendee & Organizer Registration)

#### Route Connections:
```
           [Register Form Submit]
/register ------------------------> /setup-location (Auto-login session initiated)
    ^
    | (Click "Login here")
    v
 /login
```

*   **URL:** `http://www.events.com/register`
*   **Title:** User Registration

```
+-------------------------------------------------------------------------------+
|  [ EVENT TICKETING PLATFORM ]                             Login | REGISTER    |
+-------------------------------------------------------------------------------+
|                                                                               |
|                             CREATE ACCOUNT                                    |
|                                                                               |
|         Full Name:       [_________________________________________]          |
|         Email:           [_________________________________________]          |
|         Mobile Number:   [_________________________________________]          |
|         Password:        [●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●]          |
|                                                                               |
|         [X] I consent to the Terms of Service. (Mandatory)                    |
|         [X] I consent to share data with organizers for booking. (Mandatory)  |
|         [ ] I consent to receive marketing/recommendations emails. (Optional) |
|                                                                               |
|                            [ REGISTER & START ]                               |
|                                                                               |
|         Already have an account? Login here.                                  |
|                                                                               |
+-------------------------------------------------------------------------------+
| SYSTEM EVENT:                                                                 |
| [!] On click 'REGISTER & START', JWT Session is automatically initialized.    |
| [!] Background service triggers: "Welcome to Event Ticketing" email.          |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Auto-Login Behavior:** Once the user inputs valid registration details and submits the form, the system generates a authentication JWT and persists it to local storage. The system navigates the user directly to `/setup-location` without forcing them to re-enter credentials on `/login`.
*   **Terms of Service Loading & Consent:** The Terms of Service document is loaded dynamically from the file path `File_Path` of the active version (e.g. `/docs/policies/terms_v1.0.md`). Registration maps the accepted `Terms_Id` directly to the user's `Consented_Terms_Id` in the database.
*   **Registration Welcome Email:** The platform immediately queues a background notification email detailing account ˚creation. A temporary UI notification box displays: *"Welcome Email sent to your registered email address!"* to notify the user. A pending record is enqueued in the `Notifications` table with `Status = 'Pending'`.
*   **Data Sharing Consent:** The mandatory data sharing checkbox consents to sharing contact details with organizers strictly for booking fulfillment. User contact details are never shared with third-party marketers unless the optional marketing/recommendation consent is explicitly enabled.
*   **Validation Rules:** Name (non-empty), Email (valid email format), Password (min 8 chars, 1 uppercase, 1 special char).

---

### Page 2: /login (Sign In)

#### Route Connections:
```
       [Login Submit]
/login --------------> / (Home)
  ^
  | (Click "Register here")
  v
/register
  ^
  | (Click "Reset here")
  v
/forgot-password
```

*   **URL:** `http://www.events.com/login`
*   **Title:** User Sign In

```
+-------------------------------------------------------------------------------+
|  [ EVENT TICKETING PLATFORM ]                             LOGIN | Register    |
+-------------------------------------------------------------------------------+
|                                                                               |
|                               USER LOGIN                                      |
|                                                                               |
|         Email:           [_________________________________________]          |
|         Password:        [●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●]          |
|                                                                               |
|                                 [ LOGIN ]                                     |
|                                                                               |
|         New to the platform? Register here.                                   |
|         Forgot your password? Reset here.                                     |
|                                                                               |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Authentication Check:** Logs in the user and redirects to `/` (Home).
*   **Error Handling:** Invalid credentials display a inline red banner warning: *"Invalid email or password. Please try again."*

---

### Page 2a: /forgot-password (Request Password Reset)

#### Route Connections:
```
                      [Submit Reset Request]
/forgot-password --------------------------> /reset-password
  ^
  | (Click "Back to Login")
  v
/login
```

*   **URL:** `http://www.events.com/forgot-password`
*   **Title:** Request Password Reset

```
+-------------------------------------------------------------------------------+
|  [ EVENT TICKETING PLATFORM ]                             Login | Register    |
+-------------------------------------------------------------------------------+
|                                                                               |
|                            REQUEST PASSWORD RESET                             |
|                                                                               |
|         Enter your registered email address to receive a reset token.         |
|                                                                               |
|         Email:           [_________________________________________]          |
|                                                                               |
|                               [ SEND RESET CODE ]                             |
|                                                                               |
|         Remembered your password? Back to Login                               |
|                                                                               |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Action:** Submitting email calls the backend service to generate a secure `Password_Reset_Token`.
*   **Feedback:** An email simulation dialog appears showing: *"A password reset token has been generated and sent to your email. Please use it on the reset page."*
*   **Redirect:** Auto-redirects to `/reset-password` on token generation success.

---

### Page 2b: /reset-password (Verify Reset Token & Set Password)

#### Route Connections:
```
                    [Reset Successful]
/reset-password -----------------------> /login
  ^
  | (Click "Request new code")
  v
/forgot-password
```

*   **URL:** `http://www.events.com/reset-password`
*   **Title:** Reset Account Password

```
+-------------------------------------------------------------------------------+
|  [ EVENT TICKETING PLATFORM ]                             Login | Register    |
+-------------------------------------------------------------------------------+
|                                                                               |
|                             RESET YOUR PASSWORD                               |
|                                                                               |
|         Please input the token from your email and select a new password.     |
|                                                                               |
|         Email:           [_________________________________________]          |
|         Reset Token:     [_________________________________________]          |
|         New Password:    [●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●]          |
|                                                                               |
|                             [ UPDATE PASSWORD ]                               |
|                                                                               |
|         Didn't receive a code? Request new code                               |
|                                                                               |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Validation:** Token must not be blank; New Password must meet criteria (min 8 characters, 1 uppercase, 1 special character).
*   **Matching Check:** If the email, token, and new password are valid, calls the backend service to verify the token, reset the password hash, clear the token, and redirect to the `/login` page with a success message banner: *"Password updated successfully. Please login with your new credentials."*
*   **Error Handling:** Displays warning if token is incorrect or expired: *"Invalid reset token or email. Please check your credentials and try again."*

---

### Page 3: /setup-location (Location & Region Selection)

#### Route Connections:
```
                 [Save & Browse]
/setup-location ----------------> / (Home)
```

*   **URL:** `http://www.events.com/setup-location`
*   **Title:** Set Up Your Region

```
+-------------------------------------------------------------------------------+
|  [ EVENT TICKETING PLATFORM ]                             Welcome, User!      |
+-------------------------------------------------------------------------------+
|                                                                               |
|                         SELECT YOUR REGION PREFERENCES                        |
|                                                                               |
|    We will use this to surface the most relevant events near your location.   |
|                                                                               |
|    [?] Allow location permission?                                             |
|                     [ ALLOW LOCATION ]    [ CHOOSE MANUALLY ]                 |
|                                                                               |
|    - OR - Select your preferred regions manually below:                       |
|                                                                               |
|    [X] Chennai (CHE_1021)                   [ ] Bangalore (BLR_3042)          |
|    [X] Mumbai (MUM_4011)                    [ ] Delhi (DEL_1102)              |
|    [ ] Hyderabad (HYD_5001)                 [ ] Pune (PUN_4110)               |
|                                                                               |
|                              [ SAVE & BROWSE ]                                |
|                                                                               |
+-------------------------------------------------------------------------------+
| SYSTEM NOTE:                                                                  |
| [!] Allowing location uses browser geolocation API to auto-select matching   |
|     region codes from database.                                               |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Location Permission Workflow:** Clicking "ALLOW LOCATION" triggers the browser's native geolocation prompt. The coordinates returned are resolved via API to select the closest matching region (e.g., Chennai or Mumbai).
*   **Fallback Mechanism:** If permission is denied, the user can tick checkbox regions manually and select "SAVE & BROWSE" to update their profile preferences.

---

### Page 4: / (Home - Event Search & Browsing)

#### Route Connections:
```
           +--> /events/{id} (View event details)
           |
/ (Home) --+--> /my-bookings (View ticket purchase history)
           |
           +--> /support (Access helpdesk center)
           |
           +--> /organizer (Access organizer console) via the "Events" button
```

*   **URL:** `http://www.events.com/`
*   **Title:** Browse Live Events

```
+-------------------------------------------------------------------------------+
| [ EVENT PLATFORM ]  [Search events...] [Region: Chennai v]  User v | [Events] | Support |
+-------------------------------------------------------------------------------+
| Filters: Category: [All Categories v]  Date: [Select Date v]      [FILTER]    |
+-------------------------------------------------------------------------------+
|                                                                               |
|  UPCOMING EVENTS NEAR Chennai:                                                |
|  +-------------------------------------------------------------------------+  |
|  | ANNUAL TECH EXPOSITION   [Physical]                   Region: Chennai   |  |
|  | Venue: City Auditorium | Date: June 15, 2026 18:00 | Hours: 4.5        |  |
|  | Ticket Prices: Elite: $150 | Gold: $100 | Silver: $50                   |  |
|  | Seats Remaining: 295 / 370                                              |  |
|  |                                                        [ VIEW DETAILS ] |  |
|  +-------------------------------------------------------------------------+  |
|  |                                                                         |  |
|  | GLOBAL INTERACTIVE STREAM [Virtual]                       Online        |  |
|  | Platform: Jitsi | Date: June 18, 2026 20:00 | Hours: 2.0                |  |
|  | Ticket Prices: General: $15                                             |  |
|  | Seats Remaining: 50 / 100                                               |  |
|  |                                                        [ VIEW DETAILS ] |  |
|  +-------------------------------------------------------------------------+  |
|                                                                               |
|  << Prev Page [ 1 ] 2 3 4 Next Page >>                                        |
|                                                                               |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Region-Specific Prioritization:** Displays events matched to the selected region in the navbar by default. Users can change the region selection via the dropdown, which re-queries the event list asynchronously.
*   **Real-time Availability Status:** Shows how many tickets are left in real time to create urgency.
*   **Pagination:** Displays up to 10 events per page.

---

### Page 4b: /home/search/{query} (Search Results Page)

#### Route Connections:
```
           [Search Submitted]
/ (Home) ---------------------> /home/search/{query}
     ^                                 |
     | (Clear search inputs)           +---> /events/{id} (View event details)
     v                                 |
  / (Home) <---------------------------+ (Go back / Click Logo)
```

*   **URL (Keyword Only):** `http://www.events.com/home/search/tech`
*   **URL (Keyword & Filters):** `http://www.events.com/home/search/tech?category=Physical&date=2026-06-15`
*   **URL (Filters Only - No Keyword):** `http://www.events.com/home/search?category=Physical&date=2026-06-15`
*   **Title:** Search & Filter Results
*   **Example API Request (Keyword & Filters):** `GET http://www.events.com/api/events?keyword=tech&category=Physical&minDateTime=2026-06-15T00:00:00Z&regionId=CHE_1021&page=1&size=10`

```
+-------------------------------------------------------------------------------+
| [ EVENT PLATFORM ]  [tech            ] [Region: Chennai v]  User v | [Events] | Support |
+-------------------------------------------------------------------------------+
| Filters: Category: [All Categories v]  Date: [Select Date v]      [FILTER]    |
+-------------------------------------------------------------------------------+
|                                                                               |
|  SEARCH RESULTS FOR "tech" (Chennai region):                                  |
|  +-------------------------------------------------------------------------+  |
|  | ANNUAL TECH EXPOSITION   [Physical]                   Region: Chennai   |  |
|  | Venue: City Auditorium | Date: June 15, 2026 18:00 | Hours: 4.5        |  |
|  | Ticket Prices: Elite: $150 | Gold: $100 | Silver: $50                   |  |
|  | Seats Remaining: 295 / 370                                              |  |
|  |                                                        [ VIEW DETAILS ] |  |
|  +-------------------------------------------------------------------------+  |
|                                                                               |
|  << Prev Page [ 1 ] Next Page >>                                              |
|                                                                               |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Search & Filter Routing**: Triggered when a user enters a search query and/or applies advanced filters. The keyword updates the URL path to `/home/search/{query}`, while advanced filters (Category, Date) are appended as query parameters (e.g. `?category={category}&date={date}`). If no keyword is entered but category/date filters are applied, the path resolves to `/home/search?category={category}&date={date}`.
*   **Backend API Query**: The client executes a call to `GET /api/events?keyword={query}&regionId={selectedRegion}&page=1&size=10`. This maps directly to the backend repository method `SearchEventsAsync`.
*   **Dynamic Refiltering**: Changing filters (dropdowns for category/date) inside this view re-submits the search request with the current query parameters and updates results asynchronously.
*   **Empty State Layout**: If no matching events exist, the page displays: *"No events found matching 'tech' in Chennai. Please adjust search parameters or regions."*

---

### Page 5: /events/{id} (Event Details & Ticket Selection)

#### Route Connections:
```
                [Book Now]
/events/{id} -------------> /booking/checkout
     ^
     | (Back to Search)
     v
  / (Home)
```

*   **URL:** `http://www.events.com/events/1042`
*   **Title:** Event Details - Annual Tech Exposition

```
+-------------------------------------------------------------------------------+
|  [ EVENT PLATFORM ]   < Back to Search                       User v | Support |
+-------------------------------------------------------------------------------+
|                                                                               |
|  ANNUAL TECH EXPOSITION  (Status: Live / Selling)                              |
|  Organized by: Developer Alliance                                             |
|                                                                               |
|  Date: June 15, 2026 at 18:00 IST | Duration: 4.5 Hours                       |
|  Location: Physical event at City Auditorium, Chennai (CHE_1021)              |
|                                                                               |
|  Description:                                                                 |
|  Join us for the ultimate technology showcase. Network with industry experts  |
|  and discover state-of-the-art coding frameworks.                             |
|                                                                               |
|  TICKET SELECTION:                                                            |
|  [X] Elite Tier  - $150.00   (Available: 15 / 50 seats)   Qty: [ 2 v ]        |
|  [ ] Gold Tier   - $100.00   (Available: 80 / 120 seats)  Qty: [ 0 v ]        |
|  [ ] Silver Tier - $50.00    (Available: 200 / 200 seats) Qty: [ 0 v ]        |
|                                                                               |
|  Total Price: $300.00                                                         |
|                                                                               |
|                                [ BOOK NOW ]                                   |
|                                                                               |
+-------------------------------------------------------------------------------+
| UX LIMITATION NOTICE:                                                         |
| [!] Maximum ticket quantity per order is restricted to 10 tickets.            |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Dynamic Ticket Pricing Calculation:** Quantities changed in dropdowns update the "Total Price" dynamically without page refresh.
*   **Ticket Limits:** Dropdown options are disabled or capped at 10 tickets per order, or limited to the remaining available seats if less than 10.
*   **Jitsi Integration for Virtual Events:** For Virtual events, the description shows a "Virtual Platform: Jitsi Meet" badge.

---

### Page 6: /booking/checkout (Checkout & Stripe Payment Simulation)

#### Route Connections:
```
                      [Pay & Confirm]
/booking/checkout ---------------------> /booking/success/{id}
       ^
       | (Cancel Checkout)
       v
    /events/{id}
```

*   **URL:** `http://www.events.com/booking/checkout`
*   **Title:** Secure Booking Checkout

```
+-------------------------------------------------------------------------------+
|  [ EVENT PLATFORM ]   < Cancel Checkout                      User v | Support |
+-------------------------------------------------------------------------------+
|                                                                               |
|  ORDER SUMMARY:                                                               |
|  Event: Annual Tech Exposition (June 15, 2026)                                |
|  Tickets Selected: 2 x Elite Tier ($150.00 each)                              |
|  Platform Convenience Fee (Included): $15.98                                  |
|  ---------------------------------------------------------------------------  |
|  Total Amount: $315.98 USD                                                    |
|                                                                               |
|  SECURE PAYMENT (Stripe Gateway Simulation):                                  |
|  Cardholder Name: [ User Name                               ]                 |
|  Card Number:     [ 4242 4242 4242 4242                     ]  Expiry: [06/28] |
|  CVC Code:        [ 123 ]                                                     |
|                                                                               |
|  * Idempotency Key will be transmitted to prevent duplicate purchases.         |
|                                                                               |
|                          [ PAY & CONFIRM BOOKING ]                            |
|                                                                               |
+-------------------------------------------------------------------------------+
| TRANSACTION STATUS LOG:                                                       |
| [?] Click payment to authorize standard Stripe simulation pipeline.            |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Idempotency Prevention:** A unique idempotency key is generated in the background when checkout loads. If the user clicks "PAY & CONFIRM" multiple times, the API checks this key to prevent double charging.
*   **Stripe Integration Mock:** Uses simulated token responses to test successful/failed payment flows.

---

### Page 7: /booking/success/{id} (Booking Confirmation & Digital Ticket)

#### Route Connections:
```
                          [Browse More Events]
/booking/success/{id} --------------------------> / (Home)
```

*   **URL:** `http://www.events.com/booking/success/9821`
*   **Title:** Booking Confirmed!

```
+-------------------------------------------------------------------------------+
|  [ EVENT PLATFORM ]                                          User v | Support |
+-------------------------------------------------------------------------------+
|                                                                               |
|     (•) YOUR BOOKING IS CONFIRMED!                                            |
|     We have sent a confirmation email to you with digital pass PDF.           |
|                                                                               |
|     Order Reference: #BKG-982104                                              |
|     Attendee: User Name                                                       |
|     Event: Annual Tech Exposition (June 15, 2026 18:00)                       |
|     Tier: 2 x Elite Tickets                                                   |
|                                                                               |
|     YOUR DIGITAL ENTRY PASS:                                                  |
|     +----------------------------+                                            |
|     |  ########################  |                                            |
|     |  #                      #  |                                            |
|     |  #   [ SECURE QR CODE ] #  |                                            |
|     |  #                      #  |                                            |
|     |  #   Scan at Entrance   #  |                                            |
|     |  ########################  |                                            |
|     |  Ref: BKG-982104-SIG78A9   |                                            |
|     +----------------------------+                                            |
|                                                                               |
|                     [ BROWSE MORE EVENTS ]   [ PRINT TICKET ]                 |
|                                                                               |
+-------------------------------------------------------------------------------+
| NOTIFICATION STATUS:                                                          |
| [!] Background Service Status: Booking Confirmation Email Sent!               |
| [!] QR Code signature payload includes secure cryptographic salt for validity. |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Booking Notification Email:** A notification indicator shows: *"Booking Confirmation Email Sent!"*. The platform creates and enqueues a notification record with status `Pending` in the `Notifications` table containing the secure signed QR ticket details and ticket receipt HTML. The background `ProcessNotificationQueueJob` worker dispatches it using SMTP/MailKit, updating status to `Sent` (setting `Sent_At` timestamp) or `Failed` (recording the error log to `ErrorMessage` and incrementing `Retry_Count`).
*   **Secure QR Code:** Displays a cryptographically signed payload (stored in `Booking.Qr_Code_Path`) to prevent spoofing during event entry.

---

### Page 8: /my-bookings (Attendee Booking History & Refunds)

#### Route Connections:
```
                  [View Ticket]
/my-bookings --------------------> /booking/success/{id}
     ^
     | (Refund / Back to Home)
     v
  / (Home)
```

*   **URL:** `http://www.events.com/my-bookings`
*   **Title:** My Booking History

```
+-------------------------------------------------------------------------------+
|  [ EVENT PLATFORM ]                                          User v | Support |
+-------------------------------------------------------------------------------+
|                                                                               |
|  MY BOOKINGS:                                                                 |
|                                                                               |
|  +-------------------------------------------------------------------------+  |
|  | Ref: #BKG-982104 | Tech Exposition | June 15, 2026 | Status: Confirmed  |  |
|  | Tier: 2 x Elite Tickets | Total: $315.98                                |  |
|  |                                  [ VIEW TICKET ]    [ CANCEL BOOKING ]  |  |
|  +-------------------------------------------------------------------------+  |
|  | Ref: #BKG-871109 | Rock Concert    | May 10, 2026  | Status: Completed  |  |
|  | Tier: 1 x Gold Ticket   | Total: $100.00                                |  |
|  |                                                     [ WRITE REVIEW ]    |  |
|  +-------------------------------------------------------------------------+  |
|  | Ref: #BKG-771239 | Digital Summit  | April 2, 2026 | Status: Refunded   |  |
|  | Tier: 1 x Elite Ticket  | Total: $150.00 (Refund complete)              |  |
|  +-------------------------------------------------------------------------+  |
|                                                                               |
+-------------------------------------------------------------------------------+
| CANCELLATION & REFUND POLICY:                                                 |
| [!] Cancel > 12h: Partial refund applies (12h-48h: 50% refund; >48h: 90%).    |
| [!] Cancel < 12h: NO REFUND (Entire ticket amount is charged).                |
| [!] Organizers cancelling events triggers a 100% full refund to attendees.     |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Cancellation Action:** Clicking "CANCEL BOOKING" prompts the user with policy terms: *"Are you sure you want to cancel? If cancelled within 12 hours of the event start, no refund will be issued. Otherwise, a partial refund will be credited."*
*   **Refund Logic & Sliding Scale:** If cancellation is > 48 hours before the event, Stripe issues a 90% refund. If between 12 and 48 hours, a 50% refund is issued. Under 12 hours, a 0% refund is processed. 100% full refunds are issued only if the event is cancelled by the organizer or overridden by an administrator.

---

### Page 9: /support (Helpdesk Ticket Support Queries)

#### Route Connections:
```
            [Back to Home]
/support -------------------> / (Home)
```

*   **URL:** `http://www.events.com/support`
*   **Title:** Help & Support Center

```
+-------------------------------------------------------------------------------+
|  [ EVENT PLATFORM ]                                          User v | Support |
+-------------------------------------------------------------------------------+
|                                                                               |
|  YOUR SUPPORT QUERIES:                                                        |
|  +-------------------------------------------------------------------------+  |
|  | Subject: Ticket refund delay                                            |  |
|  | Status: Resolved                                                        |  |
|  | Query: "I canceled ticket BKG-771239 but haven't received my refund."   |  |
|  | Reply: "Hi User, refund processed successfully on June 1st."            |  |
|  +-------------------------------------------------------------------------+  |
|  | Subject: Inability to locate streaming passcode                         |  |
|  | Status: Open                                                            |  |
|  | Query: "Can you provide the streaming access passcode hash?"            |  |
|  | Reply: "Pending support review..."                                      |  |
|  +-------------------------------------------------------------------------+  |
|                                                                               |
|  SUBMIT NEW HELP QUERY:                                                       |
|  Subject: [_______________________________________________________]          |
|  Message: [_______________________________________________________]          |
|           [_______________________________________________________]          |
|                                                                               |
|                             [ SUBMIT TICKET ]                                 |
|                                                                               |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Query Lifecycle:** Users can submit query descriptions. The page updates asynchronously displaying the new ticket with an "Open" status.
*   **Response View:** Shows administrator responses inline.

---

### Page 10: /organizer (Organizer Dashboard & Payout Claims)

#### Route Connections:
```
               +--> /organizer/create (Create Event)
               |
/organizer ----+--> /organizer/events (Event List / Management Directory)
               |
               +--> / (Back to Attendee Home)
```

*   **URL:** `http://www.events.com/organizer`
*   **Title:** Organizer Console

```
+-------------------------------------------------------------------------------+
|  [ ORGANIZER HUB ]    [Create Event]                       User v | Home      |
+-------------------------------------------------------------------------------+
|                                                                               |
|  YOUR PERFORMANCE OVERVIEW:                                                   |
|  Total Events: 12      |  Tickets Sold: 1,420    | Net Earnings: $14,200.00   |
|                                                                               |
|  YOUR MANAGED EVENTS:                                                         |
|  +-------------------------------------------------------------------------+  |
|  | ANNUAL TECH EXPOSITION [Physical]                          Status: Live |  |
|  | Sales: Elite (35/50) | Gold (40/120) | Silver (0/200)                   |  |
|  | Payout Status: Ineligible (Event in progress)                            |  |
|  |                                                        [ MANAGE EVENT ] |  |
|  +-------------------------------------------------------------------------+  |
|  | WEBINAR: MODERN DESIGN [Virtual]                       Status: Completed|  |
|  | Sales: General (100/100) | Net Earnings: $1,500.00                      |  |
|  | Payout Status: Unclaimed                               [ CLAIM PAYOUT ] |  |
|  +-------------------------------------------------------------------------+  |
|                                                                               |
+-------------------------------------------------------------------------------+
| RULES & CANCELLATION POLICIES:                                                |
| [!] Revenue claims can be initialized after event completion date.            |
| [!] Event Cancellation Upfront Fees Refund Policy:                            |
|     - Cancel > 24 hours: Partial refund of upfront fee is processed.          |
|     - Cancel < 24 hours: NO REFUND of upfront fees (entire amount is charged).|
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Claim Payout Action:** Clicking "CLAIM PAYOUT" sends a revenue collection request to the admin queue. The payout status is updated to `Pending Admin Approval`.
*   **Manage Event Action:** Clicking `[ MANAGE EVENT ]` routes the organizer to `/organizer/events/{id}/manage` to view the scanner links, handle check-ins, and view attendees.
*   **Event Cancellation Policy:** Organizers can cancel an active event. If cancelled > 24 hours before event start, a partial refund of their paid upfront fee is returned. If cancelled < 24 hours prior, no upfront fee is refunded. All booked attendees receive 100% ticket refunds.

---

### Page 11: /organizer/create (Create New Event Listing)

#### Route Connections:
```
                    [Submit & Process Payment]
/organizer/create -----------------------------> /organizer (Dashboard)
        ^
        | (Back to Console)
        v
   /organizer
```

*   **URL:** `http://www.events.com/organizer/create`
*   **Title:** List New Event

```
+-------------------------------------------------------------------------------+
|  [ ORGANIZER HUB ]    < Back to Dashboard                  User v | Home      |
+-------------------------------------------------------------------------------+
|                                                                               |
|  LIST NEW EVENT                                                               |
|                                                                               |
|  Event Title:    [________________________________________________________]   |
|  Description:    [________________________________________________________]   |
|                  [________________________________________________________]   |
|  Event Type:     ( ) Physical       ( ) Virtual       ( ) Hybrid              |
|  Date & Time:    [YYYY-MM-DD] [HH:MM]    Duration: [ 4.0 ] Hours              |
|                                                                               |
|  LOCATION CONFIGURATION (If Physical or Hybrid):                              |
|  Preferred Venue: [ City Auditorium, Chennai (CHE_1021)                   v ]  |
|  * Upfront booking rental cost will be computed from duration.                |
|                                                                               |
|  PLATFORM SUPPORT STAFF:                                                      |
|  [X] Allocate Platform Support Staff to Venue (Flat staff rates apply).      |
|                                                                               |
|  TICKET TIER PRICING:                                                         |
|  Elite Tier Price ($):  [ 150.00 ]     Total Seats: 50                        |
|  Gold Tier Price ($):   [ 100.00 ]     Total Seats: 120                       |
|  Silver Tier Price ($): [ 50.00  ]     Total Seats: 200                       |
|                                                                               |
|                      [ SUBMIT & PROCESS PAYMENT ]                             |
|                                                                               |
+-------------------------------------------------------------------------------+
| PLATFORM FEES CALCULATION:                                                    |
| [!] Venue Rental Charge: $400.00 | Staff Cost: $200.00 | Total Due: $600.00   |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Dynamic Upfront Fees Calculation:** Changes to event duration, venue selection, and support staff allocation dynamically calculate and display upfront platform costs in the info box at the bottom.
*   **Creation Rule & Timing Constraint:** Organizers must schedule the event start date/time at least 24 hours in the future to allow proper venue bookings and support staff allocations. The datetime picker blocks or flags inputs failing this constraint.
*   **Creation Action:** The organizer submits the form and completes payment for upfront charges via Stripe. Once authorized, the event is saved as `Live`.

---

### Page 11a: /organizer/events (Event Management Page)

#### Route Connections:
```
                               [Click Manage]
/organizer/events ---------------------------------------------> /organizer/events/{id}/manage
                  ---------------------------------------------> /organizer/create
                               [Click Create New Event]
                  ---------------------------------------------> /organizer
                               [Click Back to Console]
```

*   **URL:** `http://www.events.com/organizer/events`
*   **Title:** Manage Events List

```
+-------------------------------------------------------------------------------+
|  [ ORGANIZER EVENTS ]    < Back to Dashboard               User v | Home      |
|  ---------------------------------------------------------------------------  |
|  YOUR EVENTS DIRECTORY:                                                       |
|  Filters: [ All v ] [ Live ] [ Completed ] [ Cancelled ]                      |
|                                                                               |
|  +-------------------------------------------------------------------------+  |
|  | Event Title          | Date        | Type     | Tickets Sold | Actions  |  |
|  |----------------------|-------------|----------|--------------|----------|  |
|  | Annual Tech Expo     | May 10, 2026| Physical | 75 / 370     | [MANAGE] |  |
|  | Modern Design Web    | Jun 15, 2026| Virtual  | 100 / 100    | [MANAGE] |  |
|  | Rock Concert 2026    | Aug 22, 2026| Physical | 250 / 500    | [MANAGE] |  |
|  +-------------------------------------------------------------------------+  |
|                                                                               |
|                             [ CREATE NEW EVENT ]                              |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Event Filtering:** Organizers can toggle event views dynamically (All, Live/Upcoming, Completed, Cancelled).
*   **Manage Event Action:** Clicking `[MANAGE]` on any event row routes the organizer to `/organizer/events/{id}/manage`.
*   **Create Event Action:** Clicking `[ CREATE NEW EVENT ]` routes the organizer to the Event Creation page (`/organizer/create`).

---

### Page 11b: /organizer/events/{id}/manage (Event Management Console)

#### Route Connections:
```
                               [Click Back / Cancel Event]
/organizer/events/{id}/manage ---------------------------------> /organizer/events
                              ---------------------------------> /scanner/{secure_hash}
                                  [Click Open Scanner]
```

*   **URL:** `http://www.events.com/organizer/events/{id}/manage`
*   **Title:** Event Management Console

```
+-------------------------------------------------------------------------------+
|  [ EVENT MANAGEMENT ]    < Back to Events                  User v | Home      |
|  ---------------------------------------------------------------------------  |
|  MANAGE EVENT: "ANNUAL TECH EXPOSITION" (Physical)                            |
|  Date: May 10, 2026 at 10:00 AM   | Duration: 4.0 hrs   | Status: Live        |
|  Venue: Royal Hall (BLR_3042)     | Support Staff: Allocated (Rajesh K.)      |
|  ---------------------------------------------------------------------------  |
|  SECURE CAMERA SCANNER ACCESS:                                                |
|  Scanner URL: http://www.events.com/scanner/evt-871109-x92u                   |
|                   [ OPEN SCANNER ]    [ COPY SCANNER URL ]                    |
|  ---------------------------------------------------------------------------  |
|  ATTENDEE ROSTER:                                                             |
|  +-------------------------------------------------------------------------+  |
|  | Ticket ID | Attendee Name      | Seat/Ticket Tier    | Check-in Status  |  |
|  |-----------|--------------------|---------------------|------------------|  |
|  | #BKG-1021 | John Doe           | Elite               | Checked-In       |  |
|  | #BKG-1022 | Jane Smith         | Gold                | Pending          |  |
|  | #BKG-1023 | Bob Johnson        | Silver              | Pending          |  |
|  +-------------------------------------------------------------------------+  |
|  * Attendee email addresses are hidden for privacy compliance.                |
|  ---------------------------------------------------------------------------  |
|  DANGER ZONE:                                                                 |
|  [ CANCEL EVENT ] * Subject to cancellation fee policy.                       |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Attendee Roster Privacy:** Attendee email addresses are omitted from this screen, displaying only the ticket ID, name, ticket tier, and check-in status.
*   **Open Scanner:** Clicking `[ OPEN SCANNER ]` opens the camera scan tool at `/scanner/{secure_hash}` in a new tab.
*   **Copy Scanner URL:** Copies the secure event check-in scanner URL to the clipboard, allowing organizers to easily delegate scanning tasks to staff without giving them full dashboard access credentials.
*   **Scanner Link Expiration:** The secure scanner URL automatically expires and is invalidated once the event's status shifts to `Completed`.
*   **Cancel Event:** Clicking `[ CANCEL EVENT ]` triggers the cancellation and refund policy logic (e.g. processing upfront fee refunds based on the 24-hour rule and 100% full ticket refunds to attendees).
*   **[Future Update]:** Exactly 12 hours before the scheduled event start, a background system daemon automatically emails the secure scanner URL to the allocated staff member's registered email address, saving the organizer manual coordination effort.

---

### Page 11c: /scanner/{secure_hash} (Secure Ticket Scanner Tool)

#### Route Connections:
```
                              [Click Back To Console]
/scanner/{secure_hash} ----------------------------------------> /organizer/events/{id}/manage
```

*   **URL:** `http://www.events.com/scanner/{secure_hash}`
*   **Title:** Ticket Scan Verification

```
+-------------------------------------------------------------------------------+
|  [ SECURE TICKET SCANNER ]                                           User v | Home|
|  ---------------------------------------------------------------------------  |
|  EVENT: "ANNUAL TECH EXPOSITION" (BLR_3042)                                   |
|  ---------------------------------------------------------------------------  |
|  CAMERA FEED INTERFACE:                                                       |
|  +-------------------------------------------------------------------------+  |
|  |                                                                         |  |
|  |                       [ CAMERA VIEW / VIEWFINDER ]                      |  |
|  |                                                                         |  |
|  |                          [ SCANNING ACTIVE... ]                         |  |
|  |                                                                         |  |
|  +-------------------------------------------------------------------------+  |
|  Status Indicator: [ READY ]                                                  |
|  ---------------------------------------------------------------------------  |
|  SCAN RESULTS LOG:                                                            |
|  - #BKG-1021 John Doe (Elite) -> Checked-In successfully. (2 mins ago)        |
|  - #BKG-1024 Alice Green (Gold) -> ALREADY CHECKED IN ERROR! (5 mins ago)     |
|  ---------------------------------------------------------------------------  |
|                             [ BACK TO CONSOLE ]                               |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Secure Non-authenticated Access:** This URL is secured via a long event-specific cryptographic token/hash, enabling check-in workers to scan ticket codes directly on their mobile phones without logging in.
*   **Camera Integration:** Uses HTML5 camera stream constraints to open the device camera, capture QR codes, and dynamically send check-in requests to the `Verify&CheckInTicket` API endpoint.
*   **Real-time Logs:** Displays check-in confirmations and duplication warnings instantly without page reloads.

---

## 2. Frontend Event Cache & State Specifications

To optimize user experience and reduce backend load, the application implements **Client-Side Event Caching**:

### Specifications:
1. **Initial Hydration:**
   * When the user enters the home screen (`/`) or search/filter view (`/home/search`), the application requests the latest events matching their region selections from the backend.
2. **State Management Cache:**
   * Retrieved event lists are persisted in an Angular application-level service or state store (e.g., NgRx).
   * Subsequent route navigations (e.g., moving to `/events/{id}` and returning to the list view) serve the cached list from memory, providing instantaneous navigation.
3. **Invalidation & Cache Refresh Rules:**
   * **Stale-While-Revalidate / Expiry:** The cache invalidates automatically if it is older than **5 minutes**.
   * **Explicit Refresh:** A manual "Refresh" button or pull-to-refresh action on mobile triggers a fresh API call.
   * **Hard Reload:** A full browser page refresh triggers a clean backend query.

