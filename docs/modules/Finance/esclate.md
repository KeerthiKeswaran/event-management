# Quick Flow:

1. Get Admin Actions.

2.Decline it with remarks:
     This changes the Admin Action status as declined along with updating its remarks.

3. Approve it: The finance team will approve it, providing the type of requested refund (FUL, DYN for dynamic, REM (For remaining)).

4. When the finance team approved it, it'll first look at the action type, currently we're gonna handle if type == REF, and then it looks for the type of refund, then if the target type is ADT (Attendee, it'll go for that particular refund from RefundService), if ORG (Organizer), then it'll move for it from RefundService, the reference id here will be the booking id or event id.

5. Respond: 
if the finance team wants to send any reply for a particular ticket id, they can do it.


# Finance Operations Module

This module governs all actions the Finance Team can perform to verify departments login, approve/decline escalated admin refund actions, respond to support tickets, and view paginated/filtered transaction logs.

## 1. Files & Components Involved

### Controllers
* **DeptAuthController.cs**
  * **Path:** [DeptAuthController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/DeptAuthController.cs)
  * **Finance Endpoints:**
    * `POST api/auth/finance/login` (Finance login - step 1)
    * `POST api/auth/finance/login/verify` (Finance login - step 2 verify OTP)
    * `POST api/auth/forgot-password` (Send password reset OTP)
    * `POST api/auth/reset-password` (Perform password reset)
* **FinanceController.cs**
  * **Path:** [FinanceController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/FinanceController.cs)
  * **Finance Endpoints:**
    * `GET api/finance/actions` (Get pending admin action requests queue)
    * `POST api/finance/actions/{id}/approve` (Approve escalated refund action)
    * `POST api/finance/actions/{id}/decline` (Decline escalated action)
    * `POST api/finance/tickets/{id}/respond` (Respond to escalated support ticket)
    * `GET api/finance/transactions` (Retrieve filtered, sorted, paginated transaction history)

### Contracts & Interfaces
* **IFinanceService.cs** -> [IFinanceService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IFinanceService.cs)
* **IDeptAuthService.cs** -> [IDeptAuthService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IDeptAuthService.cs)
* **ICacheService.cs** -> [ICacheService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/ICacheService.cs)
* **IRefundService.cs** -> [IRefundService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IRefundService.cs)
* **ITransactionRepository.cs** -> [ITransactionRepository.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IRepositories/ITransactionRepository.cs)

### Services
* **FinanceService.cs** -> [FinanceService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/FinanceService.cs)
* **DeptAuthService.cs** -> [DeptAuthService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/DeptAuthService.cs)
* **OtpService.cs** -> [OtpService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/OtpService.cs)
* **CacheService.cs** -> [CacheService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/CacheService.cs)
* **RefundService.cs** -> [RefundService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/RefundService.cs)

---

## 2. Activity & State Flowcharts

### I. Finance OTP Authentication (Login Flow)
Secures the finance operations dashboard using a dynamic Redis-backed OTP verification flow.

```text
                         [ START FINANCE LOGIN ]
                                    │
                                    ▼
                     [ Enter AdminId & Password ]
                                    │
                                    ▼
                     [ Validate Password in Database ]
                                    │
                                    ▼
                     { Is Password Valid? }
                      /                  \
             [No]    /                    \ [Yes]
                    ▼                      ▼
            (Throw Unauthorized)   [ Generate 6-digit OTP ]
                                           │
                                           ▼
                                   [ Store OTP in Redis ]
                                   - Key: otp:finance-login:{email}
                                   - Expiration: 10 minutes
                                           │
                                           ▼
                                   [ Send OTP Email via Brevo ]
                                           │
                                           ▼
                                   [ Enter OTP Code ]
                                           │
                                           ▼
                                   [ Verify via Redis Cache ]
                                   - Compare with cached OTP
                                           │
                                           ▼
                                   { OTP Matches & Not Expired? }
                                    /                         \
                           [No]    /                           \ [Yes]
                                  ▼                             ▼
                         (Throw Unauthorized)          [ Delete Key from Redis ]
                                                                │
                                                                ▼
                                                       [ Generate JwtToken ]
                                                                │
                                                                ▼
                                                        [ END / Authenticated ]
```

---

### II. Review & Approve/Decline Admin Action (Refund Escalation)
Handles escalated admin decisions regarding refunds, resolving them with transaction operations in Stripe.

```text
                         [ START REVIEW ESCALATION ]
                                      │
                                      ▼
                      [ Fetch Escalated Actions Queue ]
                                      │
                                      ▼
                             { Decision? }
                             /           \
                     [Decline]           [Approve]
                        /                     \
                       ▼                       ▼
             [ Read Decline Remarks ]    [ Read RefundType & Message ]
                       │                       - FUL / DYN / REM / NOR
                       │                       │
                       ▼                       ▼
             [ Set Action Status ]       [ Set Action Status ]
             - "Declined"                - "Processing"
                       │                       │
                       ▼                       ▼
             [ Update Database ]         { Target Type? }
                       │                  /            \
                       │          [Attendee]       [Organizer]
                       │              /                  \
                       │             ▼                    ▼
                       │      [ Attendee Refund ]  [ Organizer Refund ]
                       │      - RefundAttendeeAsync - RefundOrganizerAsync
                       │      - Dynamic time-decay  - Dynamic time-decay
                       │             │                    │
                       │             ▼                    ▼
                       │             └─────────┬──────────┘
                       │                       │
                       ▼                       ▼
                       │               [ Update Action ]
                       │               - Remarks: Refund amount details
                       │               - Status: "Processed"
                       │                       │
                       └───────────┬───────────┘
                                   │
                                   ▼
                           [ Update Database ]
                                   │
                                   ▼
                           [ END / Completed ]
```

---

### III. Respond to Escalated Support Tickets
Allows the Finance team to answer user queries escalated from admin support tickets, saving responses to file concerns and triggering email responses.

```text
                         [ START RESPOND TO TICKET ]
                                      │
                                      ▼
                        [ Fetch Support Ticket by ID ]
                                      │
                                      ▼
                        [ Fetch Associated User Info ]
                                      │
                                      ▼
                        [ Read Escalation Concern File ]
                        - Path from ticket.ConcernUrl
                                      │
                                      ▼
                        [ Write Response to File (JSON) ]
                        - Add response text and preserve query
                                      │
                                      ▼
                        [ Update Ticket Status ]
                        - Set status: "Resolved"
                                      │
                                      ▼
                        [ Build Notification Email ]
                        - Use template: SupportTicketResponseTemplate.html
                        - Populate placeholders (name, ticketId, response)
                                      │
                                      ▼
                        [ Send & Save Notification ]
                        - Call NotificationHelper
                        - Save notification log in Database
                        - Send SMTP email via Brevo
                                      │
                                      ▼
                              [ END / Resolved ]
```

---

### IV. Get Transactions (Filtered, Sorted, Paginated)
Retrieves a paginated list of transactions from the database with flexible filtering (by keyword, status, date range, or transaction type) and custom sorting.

```text
                         [ START GET TRANSACTIONS ]
                                      │
                                      ▼
                     [ Read Query Parameters ]
                     - keyword, transactionType, status,
                       startDate, endDate, sortBy, page, size
                                      │
                                      ▼
                     [ Build Queryable Base ]
                     - query = _dbSet.AsQueryable()
                                      │
                                      ▼
                     { Keyword Provided? }
                      /                 \
             [Yes]   /                   \ [No]
                    ▼                     │
             [ Apply Search Filter ]      │
             - Reference, Sender/Receiver │
               ID, or Remarks             │
                    │                     │
                    ▼                     ▼
             { Type / Status / Dates? }
             - Apply where-clauses for non-null criteria
                                      │
                                      ▼
                     [ Apply Sort Order ]
                     - date_desc / date_asc
                     - amount_desc / amount_asc
                     - status_desc / status_asc
                                      │
                                      ▼
                     [ Fetch Page Items ]
                     - Count matching totals
                     - Skip (page-1)*size
                     - Take size
                                      │
                                      ▼
                     [ Return PagedResult ]
                                      │
                                      ▼
                               [ END / Success ]
```