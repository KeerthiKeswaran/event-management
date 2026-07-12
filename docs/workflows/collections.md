### Understanding the Postman Variable Security Model (Best Practice)
Before looking at the endpoints, it is critical to understand how Postman handles variables securely:
1. **Initial Value**: Synced to Postman’s servers. If you are using a shared workspace, team members will see this value. **Never put real passwords, private keys, or API tokens here.**
2. **Current Value**: Stored **locally** on your machine and never synced to Postman Cloud. **Always paste active JWT tokens and passwords here.**
3. **Environment vs. Global**: Use an **Environment** (e.g., `Event Management - Dev`) so you can easily toggle between `Development`, `Staging`, and `Production` setups.

---

# Postman API Collection Structure

```text
Event Management API/
├── User (Attendee/Organizer)
│   ├── Authentication
│   ├── Profile
│   ├── Events
│   ├── Bookings
│   ├── Support
│   └── Policies
├── Admin
│   ├── Authentication
│   ├── Dashboard & Stats
│   ├── Events & Allocation
│   ├── Venues & Regions
│   ├── Staff
│   ├── Support & Escalation
│   └── Event Reports
└── Finance
    ├── Authentication
    ├── Dashboard & Approvals
    ├── Support Tickets
    └── Transactions
```

---

## Folder 1: User (Attendee & Organizer)

### Authentication
All endpoints in this folder are **public** and do **not** require authorization headers.

#### 1. Send OTP (For Registration)
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/auth/user/send-otp`
*   **Authorization Required**: **No**
*   **Request Body (JSON)**:
```json
{
  "email": "keshwarankeerthi@gmail.com",
  "purpose": "registration"
}
```

#### 2. Register
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/auth/user/register`
*   **Authorization Required**: **No**
*   **Request Body (JSON)**:
```json
{
  "name": "Keerthi Keswaran",
  "email": "keshwarankeerthi@gmail.com",
  "mobileNumber": "9876543210",
  "password": "SecurePassword123!",
  "consentedTermsId": 10000,
  "hasMarketingConsent": true,
  "otp": "123456"
}
```

#### 3. Login
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/auth/user/login`
*   **Authorization Required**: **No**
*   **Request Body (JSON)**:
```json
{
  "email": "keshwarankeerthi@gmail.com",
  "password": "SecurePassword123!"
}
```

#### 4. Forgot Password (Send OTP)
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/auth/user/send-otp`
*   **Authorization Required**: **No**
*   **Request Body (JSON)**:
```json
{
  "email": "keshwarankeerthi@gmail.com",
  "purpose": "password-reset"
}
```

#### 5. Reset Password
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/auth/user/reset-password`
*   **Authorization Required**: **No**
*   **Request Body (JSON)**:
```json
{
  "email": "keshwarankeerthi@gmail.com",
  "otp": "654321",
  "newPassword": "NewSecurePassword123!"
}
```

---

### Profile
These endpoints require user authentication.

#### 6. Select Interested Region
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/user/select-regions`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body (JSON)**:
```json
{
  "regionId": "REG01"
}
```

#### 7. Get User Profile
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/user/profile`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body**: *None*

#### 8. Update User Profile
*   **Method**: `PUT`
*   **Endpoint**: `{{baseUrl}}/api/user/profile`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body (JSON)**:
```json
{
  "name": "Keerthi Keswaran (Updated)",
  "mobileNumber": "9876543211"
}
```

#### 8b. Get Created Events Overview (GetMyEvents)
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/user/my-events`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body**: *None*

#### 8c. View Created Event Details
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/user/my-events/10005`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body**: *None*

---

### Events
A mix of public browsing and authorized actions.

#### 9. Browse Events (Query Params)
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/event`
*   **Authorization Required**: **No**
*   **Query Parameters**:
    *   `keyword`: `Tech` (Optional)
    *   `category`: `Conference` (Optional)
    *   `minDateTime`: `2026-06-15T00:00:00Z` (Optional)
    *   `regionId`: `REG01` (Optional)
    *   `page`: `1`
    *   `size`: `10`
*   **Request Body**: *None*

#### 10. Get Event Details
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/event/54321` *(replace 54321 with your Event ID)*
*   **Authorization Required**: **No**
*   **Request Body**: *None*

#### 11. Get Recommended Events
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/event/recommended`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body**: *None*

#### 12. Report Event
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/event/54321/report`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body (JSON)**:
```json
{
  "reason": "Event displays misleading details and violates terms of service."
}
```

#### 13. Submit Event Feedback
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/event/54321/feedback`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body (JSON)**:
```json
{
  "rating": 5,
  "review": "Excellent coordination and amazing speakers!"
}
```

#### 14. Verify Ticket & Check-in
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/event/verify-ticket`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body (JSON)**:
```json
{
  "hash": "a4f89d012435bcdef937102ad0273c5b364e10b1"
}
```

#### 15. Create Event (Organizer)
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/event`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body (JSON)**:
```json
{
  "eventType": "Physical",
  "title": "Agentic AI Hackathon 2026",
  "descriptionUrl": "https://example.com/hackathon-desc",
  "imageUrl": "https://example.com/images/hackathon.jpg",
  "dateTime": "2026-07-15T09:00:00Z",
  "durationHours": 8.5,
  "requiresStaff": true,
  "venueId": 10001,
  "hasAcceptedPolicy": true,
  "ticketTiers": [
    {
      "tierName": "General Admission",
      "price": 10.00
    },
    {
      "tierName": "VIP Access",
      "price": 50.00
    }
  ]
}
```

#### 16. Check Staff Availability
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/event/check-staff`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body (JSON)**:
```json
{
  "venueId": 10001,
  "dateTime": "2026-07-15T09:00:00Z"
}
```

#### 17. Confirm Event Upfront Payment (Stripe)
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/event/54321/confirm`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body (JSON)**:
```json
{
  "stripeChargeId": "ch_3NtgK2LkdIwHu7ix2aB7J8w3",
  "paymentMethod": "Stripe"
}
```

#### 18. Cancel Event
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/event/54321/cancel`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body**: *None*

#### 19. Revert Pending Event Creation
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/event/54321/revert`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body**: *None*

---

### Bookings

#### 20. Book Tickets
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/booking`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body (JSON)**:
```json
{
  "eventId": 54321,
  "tierQuantities": {
    "General Admission": 2,
    "VIP Access": 1
  }
}
```

#### 21. Confirm Booking Payment
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/booking/11223/confirm` *(replace 11223 with Booking ID)*
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body (JSON)**:
```json
{
  "stripeChargeId": "ch_3MtgK2LkdIwHu7ix2aB7J8w3",
  "paymentMethod": "Stripe"
}
```

#### 22. Get My Bookings
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/booking` or `{{baseUrl}}/api/booking?status=Confirmed` *(status values: Confirmed or Cancelled)*
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body**: *None*
*   **Response (JSON)**:
```json
[
  {
    "booking_Id": 10000,
    "attendee_Id": 10000,
    "event_Id": 10005,
    "event_Title": "AR Rahman Live Concert",
    "event_Type": "Hybrid",
    "event_Date_Time": "2026-07-15T09:00:00Z",
    "booking_Status": "Confirmed",
    "qr_Code_Path": "/assets/10000/bookings/qr_10000.png",
    "checkIn_Status": "Pending",
    "created_At": "2026-06-15T12:00:00Z",
    "virtual_Url": "https://meet.jit.si/ar-rahman-live-concert",
    "virtual_Password_Hash": "$2b$10$xyz...",
    "details": [
      {
        "tier_Name": "General Admission",
        "quantity": 2
      }
    ]
  }
]
```

#### 23. Cancel Booking
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/booking/11223/cancel`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body**: *None*

#### 24. Revert Pending Booking
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/booking/11223/revert`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body**: *None*

---

### Support

#### 25. Submit Support Ticket
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/support/tickets`
*   **Authorization Required**: **Yes** (Bearer Token)
*   **Request Body (JSON)**:
```json
{
  "subject": "Payment processed but booking pending",
  "message": "My card was charged $70 but the ticket status shows Pending Payment.",
  "requestType": "Refund Request",
  "relatedId": 10384
}
```

---

### Policies

#### 26. Get Policy by Type
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/policies/Refund`
*   **Authorization Required**: **No**
*   **Request Body**: *None*

---

## Folder 2: Admin (Admin Access Only)

### Authentication

#### 27. Admin Login
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/auth/admin/login`
*   **Authorization Required**: **No**
*   **Request Body (JSON)**:
```json
{
  "adminId": "ADM01",
  "password": "AdminPassword123!"
}
```

#### 28. Forgot Password
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/auth/forgot-password`
*   **Authorization Required**: **No**
*   **Request Body (JSON)**:
```json
{
  "email": "admin@example.com"
}
```

#### 29. Reset Password
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/auth/reset-password`
*   **Authorization Required**: **No**
*   **Request Body (JSON)**:
```json
{
  "email": "admin@example.com",
  "otp": "123456",
  "newPassword": "NewAdminPassword123!"
}
```

---

### Dashboard & Stats

#### 30. Get Dashboard Stats
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/admin/stats`
*   **Authorization Required**: **Yes** (Bearer Token - Admin Role)
*   **Request Body**: *None*

---

### Events & Allocation

#### 31. Get Paged Events (Admin view)
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/admin/events`
*   **Authorization Required**: **Yes** (Bearer Token - Admin Role)
*   **Query Parameters**:
    *   `keyword`: `Tech` (Optional)
    *   `status`: `Live` (Optional)
    *   `page`: `1`
    *   `size`: `10`

#### 32. Allocate Staff to Event
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/admin/events/54321/allocate-staff`
*   **Authorization Required**: **Yes** (Bearer Token - Admin Role)
*   **Request Body (JSON)**:
```json
{
  "employeeId": "STF01"
}
```

---

### Venues & Regions

#### 33. Get Regions (Public)
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/regions`
*   **Authorization Required**: **No**
*   **Request Body**: *None*

#### 33b. Get Regions (Admin)
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/admin/regions`
*   **Authorization Required**: **Yes** (Bearer Token - Admin Role)
*   **Request Body**: *None*

#### 34. Get Venues
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/admin/venues`
*   **Authorization Required**: **Yes** (Bearer Token - Admin Role)
*   **Request Body**: *None*

#### 35. Create Venue
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/admin/venues`
*   **Authorization Required**: **Yes** (Bearer Token - Admin Role)
*   **Request Body (JSON)**:
```json
{
  "region_Id": "REG01",
  "name": "Grand Ballroom A",
  "address": "456 Silicon Valley Blvd",
  "hourly_Price": 250.00,
  "is_Available": true,
  "seatTiers": [
    {
      "tier_Name": "Floor Seats",
      "total_Seats": 300
    },
    {
      "tier_Name": "Balcony",
      "total_Seats": 100
    }
  ]
}
```

---

### Staff

#### 36. Get Staff Directory
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/admin/staff`
*   **Authorization Required**: **Yes** (Bearer Token - Admin Role)
*   **Request Body**: *None*

---

### Support & Escalation

#### 37. Get All Support Tickets
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/admin/support/tickets`
*   **Authorization Required**: **Yes** (Bearer Token - Admin Role)
*   **Request Body**: *None*
*   **Response (JSON)**:
```json
[
  {
    "ticket_Id": 10000,
    "user_Id": 11992,
    "concernUrl": "/assets/support_tickets/ticket_abc-123.json",
    "requestType": "booking",
    "status": "Open",
    "esclationStatus": "Unavailable",
    "relatedId": 10384
  }
]
```

#### 38. Respond to Support Ticket
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/admin/support/tickets/10001/respond`
*   **Authorization Required**: **Yes** (Bearer Token - Admin Role)
*   **Request Body (JSON)**:
```json
{
  "response": "Hello, we have confirmed your payment error and will escalate this to Finance."
}
```

#### 39. Escalate Support Ticket to Finance
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/admin/support/tickets/10001/escalate`
*   **Authorization Required**: **Yes** (Bearer Token - Admin Role)
*   **Request Body (JSON)**:
```json
{
  "actionType": "REF",
  "targetType": "ATD",
  "targetId": 12345,
  "referenceId": 98765
}
```

---

### Event Reports

#### 40. Get Flagged Event Reports
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/admin/reports`
*   **Authorization Required**: **Yes** (Bearer Token - Admin Role)
*   **Request Body**: *None*

#### 41. Dismiss Event Report
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/admin/reports/20001/dismiss`
*   **Authorization Required**: **Yes** (Bearer Token - Admin Role)
*   **Request Body**: *None*

#### 42. Uphold Event Report (Block Organizer)
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/admin/reports/20001/uphold`
*   **Authorization Required**: **Yes** (Bearer Token - Admin Role)
*   **Request Body (JSON)**:
```json
{
  "reason": "Confirmed report that the organizer is conducting a scam.",
  "organizerAction": "Restrict"
}
```

---

## Folder 3: Finance (Finance Access Only)

### Authentication

#### 43. Finance Login (Sends OTP)
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/auth/finance/login`
*   **Authorization Required**: **No**
*   **Request Body (JSON)**:
```json
{
  "adminId": "FIN01",
  "password": "FinancePassword123!"
}
```

#### 44. Verify Finance Login OTP
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/auth/finance/login/verify`
*   **Authorization Required**: **No**
*   **Request Body (JSON)**:
```json
{
  "adminId": "FIN01",
  "otp": "123456"
}
```

---

### Dashboard & Approvals

#### 45. Get Escalated Finance Actions
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/finance/actions`
*   **Authorization Required**: **Yes** (Bearer Token - Finance Role)
*   **Request Body**: *None*

#### 46. Decline Escalated Action
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/finance/actions/30001/decline`
*   **Authorization Required**: **Yes** (Bearer Token - Finance Role)
*   **Request Body (JSON)**:
```json
{
  "remarks": "Invalid charge verification payload. Stripe references do not match."
}
```

#### 47. Approve Escalated Action & Process Refund
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/finance/actions/30001/approve`
*   **Authorization Required**: **Yes** (Bearer Token - Finance Role)
*   **Request Body (JSON)**:
```json
{
  "refundType": "FUL",
  "message": "Full refund has been approved and successfully reversed on your card."
}
```

---

### Support Tickets

#### 48. Finance Respond to Ticket
*   **Method**: `POST`
*   **Endpoint**: `{{baseUrl}}/api/finance/tickets/10001/respond`
*   **Authorization Required**: **Yes** (Bearer Token - Finance Role)
*   **Request Body (JSON)**:
```json
{
  "response": "Refund successful. Bank reference number: TXN829410."
}
```

---

### Transactions

#### 49. Get Paged Transactions
*   **Method**: `GET`
*   **Endpoint**: `{{baseUrl}}/api/finance/transactions`
*   **Authorization Required**: **Yes** (Bearer Token - Finance Role)
*   **Query Parameters**:
    *   `transactionType`: `Refund` (Optional)
    *   `status`: `Succeeded` (Optional)
    *   `page`: `1`
    *   `size`: `10`

---

# Step-by-Step Guide: Safely Managing JWT Tokens in Postman Environments

Follow these steps to prevent security issues when managing environments and tokens:

### Step 1: Create a Postman Environment
1. In the left panel of Postman, click **Environments**.
2. Click the **`+` (Create Environment)** button at the top.
3. Name your environment: `Event Management - Local`.
4. Add the following variables:
   *   `baseUrl` | Type: `default` | Initial: `http://localhost:5000` | Current: `http://localhost:5000`
   *   `token` | Type: `secret` | **Initial: (Leave Blank)** | **Current: (Leave Blank)**
5. Click **Save** in the top right.
6. In the top right dropdown of Postman, change the environment from `No Environment` to `Event Management - Local`.

---

### Step 2: Automate Token Extractions via the "Tests" Tab
Instead of copy-pasting the token manually, you can automate token updates. 

Go to the **Tests** tab of the following four Login/Register endpoints in your Postman collection:
1. `User Login`
2. `User Register`
3. `Admin Login`
4. `Verify Finance Login OTP`

Paste the following script:

```javascript
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    if (jsonData.token) {
        // Set the token ONLY to the current value (local storage) for security
        pm.environment.set("token", jsonData.token);
        console.log("JWT Token extracted and saved safely to environment current value.");
    }
}
```
*Now, whenever you invoke a login or registration request, Postman will automatically update the `token` variable in your active environment.*

---

### Step 3: Configure Root Collection Authorization
Rather than setting the token in every request's headers manually:
1. Click on the top-level collection folder (**`Event Management API`**).
2. Go to the **Authorization** tab.
3. Choose Type: **Bearer Token**.
4. In the Token field, type: `{{token}}`.
5. Under each folder (and request) within the collection, ensure the **Authorization** tab has Type set to: **Inherit auth from parent**.

Now, every authorized endpoint will automatically use the active JWT token from your environment securely and cleanly!