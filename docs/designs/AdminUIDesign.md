# Admin UI Wireframes & Layout Specification

This document details the Administrator Portal wireframe designs, tabbed page states, routing paths, and back-office management interfaces for the **Event Management & Ticketing Platform**.

---

## 1. Page Routing Architecture

```
        +-------------------+
        |   /admin-login    | (ADM_XXXX ID credentials authentication)
        +-------------------+
                  |
                  v (On Authentication Success)
        +-------------------+
        |      /admin       | (Unified Tabbed Control Panel)
        +-------------------+
```

---

## 2. Page Wireframe Specifications & Local Route Connections

### Page 1: /admin-login (Administrator Portal Authentication)

#### Route Connections:
```
                      [Authenticate Submit]
/admin-login ----------------------------> /admin (Unified Dashboard)
  ^
  | (Click "Reset here")
  v
/admin-forgot-password
```

*   **URL:** `http://www.events.com/admin-login`
*   **Title:** Administrator Security Portal

```
+-------------------------------------------------------------------------------+
|  [ SECURITY CONTROL PANEL ]                                                   |
+-------------------------------------------------------------------------------+
|                                                                               |
|                          SECURE ADMINISTRATOR SIGN IN                         |
|                                                                               |
|         Admin ID:        [ADM_1001                                 ]          |
|         Password:        [●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●]          |
|                                                                               |
|                              [ AUTHENTICATE ]                                 |
|                                                                               |
|         Forgot your password? Reset here.                                     |
|         * Authorization logs are transmitted directly to audit ledgers.      |
|                                                                               |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Admin Separation of Concerns:** Administrators authenticate using a distinct corporate alphanumeric ID string starting with an `ADM_` prefix instead of a standard email.
*   **Audit Logging:** Failsafe lockout triggers after 5 failed authentication attempts, logging the originating IP to security registers.

---

### Page 1a: /admin-forgot-password (Admin Request Password Reset)

#### Route Connections:
```
                            [Submit Reset Request]
/admin-forgot-password --------------------------> /admin-reset-password
  ^
  | (Click "Back to Login")
  v
/admin-login
```

*   **URL:** `http://www.events.com/admin-forgot-password`
*   **Title:** Admin Request Password Reset

```
+-------------------------------------------------------------------------------+
|  [ SECURITY CONTROL PANEL ]                                                   |
+-------------------------------------------------------------------------------+
|                                                                               |
|                       ADMINISTRATOR PASSWORD RESET REQUEST                    |
|                                                                               |
|         Enter your registered email address to receive a reset token.         |
|                                                                               |
|         Email:           [admin@events.com                         ]          |
|                                                                               |
|                              [ GENERATE RESET CODE ]                          |
|                                                                               |
|         Remembered your password? Back to Login                               |
|                                                                               |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Action:** Submitting email calls the backend admin authentication service to generate a secure `Password_Reset_Token`.
*   **Feedback:** Displays verification dialog: *"A password reset token has been successfully generated for this Administrator account. Please retrieve it from your registered email address and input on the next page."*
*   **Redirect:** Auto-redirects to `/admin-reset-password` on token generation success.

---

### Page 1b: /admin-reset-password (Admin Verify Reset Token & Set Password)

#### Route Connections:
```
                          [Reset Successful]
/admin-reset-password -----------------------> /admin-login
  ^
  | (Click "Request new code")
  v
/admin-forgot-password
```

*   **URL:** `http://www.events.com/admin-reset-password`
*   **Title:** Admin Reset Password

```
+-------------------------------------------------------------------------------+
|  [ SECURITY CONTROL PANEL ]                                                   |
+-------------------------------------------------------------------------------+
|                                                                               |
|                           RESET ADMINISTRATOR PASSWORD                        |
|                                                                               |
|         Please input your reset token and enter your new password.            |
|                                                                               |
|         Email:           [admin@events.com                         ]          |
|         Reset Token:     [_________________________________________]          |
|         New Password:    [●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●●]          |
|                                                                               |
|                              [ RESET PASSWORD ]                               |
|                                                                               |
|         Didn't receive a token? Request new code                              |
|                                                                               |
+-------------------------------------------------------------------------------+
```

#### Detailed UX Specifications:
*   **Validation:** Token must not be empty. New Password must comply with secure administrative policy constraints.
*   **Verification:** Calls admin authentication service to verify reset token for the specified email, update the hashed password, clear the token, and redirect back to `/admin-login` with a green notification banner: *"Admin password reset completed successfully. Please sign in with your new credentials."*
*   **Error Handling:** Displays error indicator if the token or email is mismatching/expired: *"Invalid password reset token or email address. Audit log recorded."*

---

### Page 2: /admin (Unified Administrator Dashboard)

#### Route Connections:
```
              [Logout Button Click]
/admin --------------------------> /admin-login
```

*   **URL:** `http://www.events.com/admin`
*   **Title:** Platform Admin Control Center

This page utilizes a unified, tabbed control panel layout. Selecting a tab updates the active sub-view asynchronously.

---

#### View State 2a: [Stats & Diagnostics Tab]

```
+-------------------------------------------------------------------------------+
|  [ ADMIN PANEL ]  (Tab: Stats)                              ADM_1001 | Logout |
+-------------------------------------------------------------------------------+
| TABS: [*Stats*] [Moderation] [Regions/Venues] [Staff] [Payouts] [Settings] [Help]
+-------------------------------------------------------------------------------+
|                                                                               |
|  GLOBAL PLATFORM STATS:                                                       |
|  Total Registered Users: 8,421        |  Active Live Events: 142              |
|  Gross Transactions: $242,500.00      |  Platform Commission Earned: $24,250  |
|                                                                               |
|  SYSTEM LEDGER AUDIT LOGS:                                                    |
|  +-------------------------------------------------------------------------+  |
|  | Txn ID | Type              | Sender         | Receiver       | Amount   |  |
|  |--------|-------------------|----------------|----------------|----------|  |
|  | #84910 | BookingPayment    | User_42        | Escrow_Pool    | $315.98  |  |
|  | #84909 | OrganizerPayout   | Escrow_Pool    | Organizer_8    | $1350.00 |  |
|  | #84908 | UpfrontPayment    | Organizer_8    | Platform_Fee   | $600.00  |  |
|  +-------------------------------------------------------------------------+  |
|                                                                               |
|                      [ EXPORT FINANCIAL DATA (CSV) ]                          |
|                                                                               |
+-------------------------------------------------------------------------------+
```

##### Detailed UX Specifications:
*   **Financial Auditing:** The ledger audit table pulls transaction history directly from the `Transactions` ledger table in real time.
*   **CSV Export:** Clicking the export button initiates a server-side build of a CSV spreadsheet containing raw audit ledger columns, pushing it as a browser download.

---

#### View State 2b: [Moderation & Event Flagging Tab]

```
+-------------------------------------------------------------------------------+
|  [ ADMIN PANEL ]  (Tab: Moderation)                         ADM_1001 | Logout |
+-------------------------------------------------------------------------------+
| TABS: [Stats] [*Moderation*] [Regions/Venues] [Staff] [Payouts] [Settings] [Help]
+-------------------------------------------------------------------------------+
|                                                                               |
|  PENDING FLAG REPORTS / MODERATION CHECKS:                                    |
|                                                                               |
|  +-------------------------------------------------------------------------+  |
|  | ID  | Target Event      | Reporter  | Reason                  | Actions |  |
|  |-----|-------------------|-----------|-------------------------|---------|  |
|  | #18 | Spam Link Listing | User_90   | Scam offering fake links| [S] [D] |  |
|  | #19 | Concert Bootleg   | User_12   | Copyright infringement  | [S] [D] |  |
|  +-------------------------------------------------------------------------+  |
|                                                                               |
|  * Action Keys: [S] = SUSPEND EVENT (Red)      [D] = DISMISS REPORT (Grey)    |
|                                                                               |
|  ---------------------------------------------------------------------------  |
|                                                                               |
|  VENUE CONFLICTS & OVERRIDES:                                                 |
|                                                                               |
|  +-------------------------------------------------------------------------+  |
|  | Organizer| Target Event| Venue Name (ID)      | Conflict Detail| Actions|  |
|  |----------|-------------|----------------------|----------------|--------|  |
|  | User_8   | Rock Show   | Royal Hall (BLR_3042)| Double-Booked  | [O] [P]|  |
|  | User_15  | Jazz Night  | City Aud (CHE_1021)  | Schedule Clash | [O] [P]|  |
|  +-------------------------------------------------------------------------+  |
|                                                                               |
|  * Override Keys: [O] = OVERRIDE & FULL REFUND   [P] = PAY REMAINING BALANCE  |
|                                                                               |
+-------------------------------------------------------------------------------+
```

##### Detailed UX Specifications:
*   **Suspend Event Action:** Clicking `[S]` prompts: *"Are you sure you want to suspend this event? This will cancel all bookings and trigger full refunds."* On confirmation, status updates to `Cancelled`, staff allocations revert, Stripe automatic rollbacks process for booked attendees, and notification queue emails are sent.
*   **Dismiss Action:** Clicking `[D]` deletes the moderation report record without affecting event lifecycle status.
*   **Override & Full Refund Action:** Clicking `[O]` facilitates an override for events with venue conflicts. It prompts: *"Override cancellation constraints to execute a 100% full refund?"* On confirmation, it bypasses standard cancellation time-window cutoffs to issue a 100% refund of the organizer's upfront fees and all attendee ticket sales via the Stripe API.
*   **Pay Remaining Refund Action:** Clicking `[P]` handles conflicts where the organizer already cancelled the event and received partial/no refund under the 24-hour cutoff rule. It prompts: *"Process Stripe transaction to pay the remaining balance to the organizer?"* On confirmation, it processes a Stripe payment to refund the remaining balance of the upfront license fee to the organizer's connected Stripe account.

---

#### View State 2c: [Regions & Venues Setup Tab]

```
+-------------------------------------------------------------------------------+
|  [ ADMIN PANEL ]  (Tab: Regions/Venues)                     ADM_1001 | Logout |
+-------------------------------------------------------------------------------+
| TABS: [Stats] [Moderation] [*Regions/Venues*] [Staff] [Payouts] [Settings] [Help]
+-------------------------------------------------------------------------------+
|                                                                               |
|  REGISTERED REGIONS:                                                          |
|  - Chennai (CHE_1021)   - Bangalore (BLR_3042)   - Mumbai (MUM_4011)          |
|                                                                               |
|  ACTIVE PHYSICAL VENUES LIST:                                                 |
|  +-------------------------------------------------------------------------+  |
|  | Venue Name      | RegionCode | Hourly Rate | Max Seating Capacities     |  |
|  |-----------------|------------|-------------|----------------------------|  |
|  | City Auditorium | CHE_1021   | $100.00/hr  | Elite:50, Gold:120, Sil:200|  |
|  | Royal Hall      | BLR_3042   | $150.00/hr  | Elite:80, Gold:200, Sil:400|  |
|  +-------------------------------------------------------------------------+  |
|                                                                               |
|  ADD NEW VENUE:                                                               |
|  Name: [__________________]  Region: [CHE_1021 v]  Hourly Price: [$______]    |
|  Seating Capacities:  Elite: [50 ]  Gold: [120]  Silver: [200]                 |
|                                                                               |
|                              [ CREATE VENUE ]                                 |
|                                                                               |
+-------------------------------------------------------------------------------+
```

##### Detailed UX Specifications:
*   **Venue Management:** Administrators configure physical venues, hourly rental costs, and tier seating bounds. These limits restrict seat ticket availability checks in `/events/{id}` for attendees.
*   **Input Validation:** Form fields block non-numeric values for pricing and ensure at least one ticket tier has a capacity greater than zero.

---

#### View State 2d: [Staff Management & Allocation Tab]

```
+-------------------------------------------------------------------------------+
|  [ ADMIN PANEL ]  (Tab: Staff)                              ADM_1001 | Logout |
+-------------------------------------------------------------------------------+
| TABS: [Stats] [Moderation] [Regions/Venues] [*Staff*] [Payouts] [Settings] [Help]
+-------------------------------------------------------------------------------+
|                                                                               |
|  SUPPORT STAFF REGISTRATION DIRECTORY:                                        |
|  +-------------------------------------------------------------------------+  |
|  | Emp ID | Staff Name      | RegionCode | Active Allocation Status        |  |
|  |--------|-----------------|------------|---------------------------------|  |
|  | #1001  | Rajesh Kumar    | CHE_1021   | Allocated (Event #1042)         |  |
|  | #1002  | Sarah Smith     | CHE_1021   | Unallocated (Available)         |  |
|  | #1003  | Amit Sharma     | BLR_3042   | Unallocated (Available)         |  |
|  +-------------------------------------------------------------------------+  |
|                                                                               |
|  REGISTER NEW STAFF MEMBER:                                                   |
|  Full Name: [_______________________]  Region Workspace: [CHE_1021 v]         |
|                                                                               |
|                              [ ADD TO DIRECTORY ]                             |
|                                                                               |
+-------------------------------------------------------------------------------+
```

##### Detailed UX Specifications:
*   **Staff Allocation Rules:** Organizers pay flat platform fees to request support staff. The admin allocation service ensures that only staff registered in the event's region are assigned, preventing cross-region coordination issues.
*   **Automatic De-allocation:** Upon event completion, NUnit/Quartz background jobs restore staff allocation statuses to `Unallocated` (Available).

---

#### View State 2e: [Payouts & Escrow Transfers Tab]

```
+-------------------------------------------------------------------------------+
|  [ ADMIN PANEL ]  (Tab: Payouts)                            ADM_1001 | Logout |
+-------------------------------------------------------------------------------+
| TABS: [Stats] [Moderation] [Regions/Venues] [Staff] [*Payouts*] [Settings] [Help]
+-------------------------------------------------------------------------------+
|                                                                               |
|  PENDING PAYOUT CLAIMS (Concluded Events):                                    |
|                                                                               |
|  Event: Webinar: Modern Design                                                |
|  Organizer: User_8 (Alice Miller)                                             |
|  Gross Sales: $1,500.00                                                       |
|  Platform Commission Fee (10% + fixed fee): $150.00                           |
|  Net Escrow Payout Transfer: $1,350.00                                        |
|                                                                               |
|  RE-AUTHENTICATION FOR PAYOUT COMPLIANCE:                                     |
|  Admin Password: [●●●●●●●●●●●●●●●●]                                           |
|                                                                               |
|                          [ APPROVE & TRANSFER PAYOUT ]                        |
|                                                                               |
|  ---------------------------------------------------------------------------  |
|  * Payout transfers are directly wired to the organizer's connected Stripe ID. |
|                                                                               |
+-------------------------------------------------------------------------------+
```

##### Detailed UX Specifications:
*   **Compliance Payouts:** Re-authentication is required to authorize the Stripe Connected Account payout. If password check succeeds, the platform executes a Stripe escrow transfer, marks `OrganizerPayouts.Payout_Status` to `Success`, and logs the audit trail.

---

#### View State 2f: [Platform Settings Tab]

```
+-------------------------------------------------------------------------------+
|  [ ADMIN PANEL ]  (Tab: Settings)                           ADM_1001 | Logout |
+-------------------------------------------------------------------------------+
| TABS: [Stats] [Moderation] [Regions/Venues] [Staff] [Payouts] [*Settings*] [Help]
+-------------------------------------------------------------------------------+
|                                                                               |
|  GLOBAL PLATFORM CONFIGURATION SETTINGS:                                      |
|                                                                               |
|  Staff Flat Booking Rate ($/hr):      [ 50.00 ]                               |
|  Base Physical Event License Fee ($): [ 200.00]                               |
|  Base Virtual Event License Fee ($):  [ 50.00 ]                               |
|  Ticket Commission Fee (%):          [ 10.0  ]                               |
|  Ticket Fixed Convenience Fee ($):    [ 0.99  ]                               |
|  Maximum Tickets Per Booking Order:   [ 10    ]                               |
|                                                                               |
|                              [ SAVE SETTINGS ]                                |
|                                                                               |
+-------------------------------------------------------------------------------+
```

##### Detailed UX Specifications:
*   **Global Variables Setup:** These constants dynamically control pricing formulas across the platform. Saving settings updates the `PlatformSettings` row and caches variables for checkout transactions.

---

#### View State 2g: [Helpdesk Support Resolution Tab]

```
+-------------------------------------------------------------------------------+
|  [ ADMIN PANEL ]  (Tab: Support Queries)                    ADM_1001 | Logout |
+-------------------------------------------------------------------------------+
| TABS: [Stats] [Moderation] [Regions/Venues] [Staff] [Payouts] [Settings] [*Help*]
+-------------------------------------------------------------------------------+
|                                                                               |
|  PENDING USER HELP TICKETS:                                                   |
|  +-------------------------------------------------------------------------+  |
|  | Ticket ID | User Name | Subject             | Msg Content        | Status   |  |
|  |-----------|-----------|---------------------|--------------------|----------|  |
|  | #1042     | User_12   | Refund Delay BKG-12 | "No refund received"| [RESOLVE]|  |
|  +-------------------------------------------------------------------------+  |
|                                                                               |
|  TICKET RESPONSE INPUT (#1042):                                               |
|  Admin Reply: [Refund processed successfully on June 1st. Please check bank. ] |
|                                                                               |
|                              [ SUBMIT REPLY & RESOLVE ]                       |
|                                                                               |
+-------------------------------------------------------------------------------+
```

##### Detailed UX Specifications:
*   **Ticket Support Resolution:** Admins review incoming inquiries, input answers, and submit. The status shifts from `Open` to `Resolved` and enqueues an asynchronous email response to the user via the `Notifications` queue.
