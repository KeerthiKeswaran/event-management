# API Endpoint Reference

> **Base URL:** `http://localhost:5106` (HTTP dev) or `https://localhost:7272` (HTTPS dev)  
> **Auth:** Protected routes require `Authorization: Bearer <token>` header.  
> **Content-Type:** `application/json` for all request bodies.

---

## 👤 User

### Authentication (`/api/auth/user`)

---

#### Send OTP
`POST http://localhost:5106/api/auth/user/send-otp`

**Purpose:** Send email OTP before registration or password reset.

```json
{
  "email": "keshwarankeerthi@gmail.com",
  "purpose": "registration"   // "registration" | "password-reset"
}
```

---

#### Register
`POST http://localhost:5106/api/auth/user/register`

**Purpose:** Create a new user account with email OTP verification.

```json
{
  "name": "KeerthiKeswaran",
  "email": "keshwarankeerthi@gmail.com",
  "mobileNumber": "8978675645",
  "password": "SecurePass123",
  "consentedTermsId": 1,
  "hasMarketingConsent": true,
  "otp": "482910"
}
```

---

#### Login
`POST http://localhost:5106/api/auth/user/login`

```json
{
  "email": "keshwarankeerthi@gmail.com",
  "password": "SecurePass123"
}
```

---

#### Forgot Password (Request OTP)
`POST http://localhost:5106/api/auth/user/send-otp`

```json
{
  "email": "keshwarankeerthi@gmail.com",
  "purpose": "password-reset"
}
```

---

#### Reset Password
`POST http://localhost:5106/api/auth/user/reset-password`

```json
{
  "email": "keshwarankeerthi@gmail.com",
  "otp": "482910",
  "newPassword": "NewSecurePass456"
}
```

---

### Profile & Preferences (`/api/user`) 🔒 Requires Auth

---

#### Get Profile
`GET http://localhost:5106/api/user/profile`

*No request body.*

---

#### Update Profile
`PUT http://localhost:5106/api/user/profile`

```json
{
  "name": "KeerthiKeswaran",
  "mobileNumber": "8978675645"
}
```

---

#### Select Interested Region
`POST http://localhost:5106/api/user/select-regions`

```json
{
  "regionId": "REG_001"
}
```

---

#### Get Created Events Overview
`GET http://localhost:5106/api/user/my-events`

**Purpose:** Retrieve a high-level overview of events created by the authenticated user. Excludes sensitive meeting credentials.

*No request body.*

**Response Example:**
```json
[
  {
    "event_Id": 10005,
    "title": "AR Rahman Live Concert",
    "event_Type": "Hybrid",
    "date_Time": "2026-07-15T09:00:00Z",
    "duration_Hours": 3.5,
    "status": "Live",
    "venue_Name": "Chennai Trade Centre",
    "tickets_Sold": 150,
    "net_Earnings": 225000.00
  }
]
```

---

#### Get Dashboard Metrics
`GET http://localhost:5106/api/user/my-dashboard`

**Purpose:** Retrieve organizer metrics (aggregated event counts, total tickets sold, net earnings) and upcoming event list.

*No request body.*

**Response Example:**
```json
{
  "totalEvents": 14,
  "ticketsSold": 8240,
  "netEarnings": 245000.00,
  "upcomingEvents": [
    {
      "event_Id": 10005,
      "title": "AR Rahman Live Concert",
      "event_Type": "Hybrid",
      "date_Time": "2026-07-15T09:00:00Z",
      "duration_Hours": 3.5,
      "status": "Live",
      "venue_Name": "Chennai Trade Centre",
      "tickets_Sold": 150,
      "net_Earnings": 225000.00
    }
  ]
}
```

---

#### View Created Event Details
`GET http://localhost:5106/api/user/my-events/{eventId}`

**Purpose:** Retrieve complete details of a specific event created by the organizer, including sensitive data such as Jitsi URLs and passcode hashes.

*No request body. Pass event ID in URL.*

**Response Example:**
```json
{
  "event_Id": 10005,
  "organizer_Id": 10000,
  "event_Type": "Hybrid",
  "title": "AR Rahman Live Concert",
  "description_Url": "https://example.com/rahman-desc",
  "image_Url": "https://example.com/rahman.jpg",
  "date_Time": "2026-07-15T09:00:00Z",
  "duration_Hours": 3.5,
  "status": "Live",
  "requires_Staff": true,
  "venue_Id": 10002,
  "venue_Name": "Chennai Trade Centre",
  "virtual_Url": "https://meet.jit.si/ar-rahman-live-concert",
  "virtual_Password_Hash": "$2b$10$xyz...",
  "ticketTiers": [
    {
      "tier_Name": "General Admission",
      "price": 1500.00,
      "tickets_Sold": 2
    }
  ]
}
```

---

### Regions (`/api/regions`) 🌐 Public

---

#### Get All Regions
`GET http://localhost:5106/api/regions`

**Purpose:** Retrieve all available regions in the system.

*No request body.*

**Response Example:**
```json
[
  {
    "region_Id": "REG_001",
    "name": "Chennai"
  },
  {
    "region_Id": "REG_002",
    "name": "Bangalore"
  }
]
```

---

### Event Browsing (`/api/event`) 🌐 Public

---

#### Browse / Search Events
`GET http://localhost:5106/api/event`

**Query Parameters:**

| Param         | Type     | Required | Description                  |
|---------------|----------|----------|------------------------------|
| `keyword`     | string   | No       | Title or description search  |
| `category`    | string   | No       | Event type filter            |
| `minDateTime` | datetime | No       | Filter events from this date |
| `regionId`    | string   | No       | Filter by region             |
| `page`        | int      | No       | Page number (default: 1)     |
| `size`        | int      | No       | Page size (default: 10)      |

Example: `GET http://localhost:5106/api/event?keyword=music&regionId=REG_001&page=1&size=10`

**Response Example:**
```json
{
  "items": [
    {
      "event_Id": 11331,
      "organizer_Name": "Rajesh Kumar",
      "venue_Name": "Chennai Trade Centre",
      "address": "Poonamallee High Rd, Nandambakkam, Chennai",
      "venue_Region_Name": "Chennai Region",
      "event_Type": "Hybrid",
      "title": "Test Hybrid",
      "description_Url": "https://example.com/tech-summit-desc",
      "image_Url": "https://example.com/images/tech-summit.jpg",
      "date_Time": "2026-07-15T09:00:00Z",
      "status": "Live",
      "duration_Hours": 8.50,
      "ticketTiers": [
        {
          "tier_Name": "VIP Access",
          "price": 500.00,
          "tickets_Sold": 0
        }
      ],
      "reports": [
        {
          "report_Id": 10001,
          "reporter_Id": 10000,
          "reason": "Misleading information",
          "created_At": "2026-06-15T12:00:00Z"
        }
      ]
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10
}
```

---

#### Get Event Details
`GET http://localhost:5106/api/event/{eventId}`

*No request body. Pass event ID in URL.*

Example: `GET http://localhost:5106/api/event/42`

---

#### Get Venues (Public)
`GET http://localhost:5106/api/event/venues`

**Purpose:** Retrieve list of all approved physical/hybrid venues in the system.

*No request body.*

**Response Example:**
```json
[
  {
    "venue_Id": 5,
    "region_Id": "REG_001",
    "name": "Chennai Trade Centre",
    "address": "Nandambakkam, Chennai",
    "hourly_Price": 5000.00,
    "is_Available": true,
    "seatTiers": [
      { "tier_Name": "General", "total_Seats": 200 }
    ]
  }
]
```

---

#### Get Recommended Events
`GET http://localhost:5106/api/event/recommended` 🔒

*No request body. Returns events in the user's interested regions.*

**Response Example:**
```json
[
  {
    "event_Id": 11331,
    "organizer_Name": "Rajesh Kumar",
    "venue_Name": "Chennai Trade Centre",
    "address": "Poonamallee High Rd, Nandambakkam, Chennai",
    "venue_Region_Name": "Chennai Region",
    "event_Type": "Hybrid",
    "title": "Test Hybrid",
    "description_Url": "https://example.com/tech-summit-desc",
    "image_Url": "https://example.com/images/tech-summit.jpg",
    "date_Time": "2026-07-15T09:00:00Z",
    "status": "Live",
    "duration_Hours": 8.50,
    "ticketTiers": [
      {
        "tier_Name": "VIP Access",
        "price": 500.00,
        "tickets_Sold": 0
      }
    ],
    "reports": [
      {
        "report_Id": 10001,
        "reporter_Id": 10000,
        "reason": "Misleading information",
        "created_At": "2026-06-15T12:00:00Z"
      }
    ]
  }
]
```

---

#### Report an Event
`POST http://localhost:5106/api/event/{eventId}/report` 🔒

```json
{
  "reason": "Misleading event description"
}
```

---

#### Submit Feedback / Review
`POST http://localhost:5106/api/event/{eventId}/feedback` 🔒

```json
{
  "rating": 4,
  "review": "Great event, very well organised!"
}
```

---

### Event Creation (Organizer) (`/api/event`) 🔒 Requires Auth

---

#### Create Event
`POST http://localhost:5106/api/event`

```json
{
  "eventType": "Physical",
  "title": "AR Rahman Live Concert Chennai",
  "descriptionUrl": "https://cdn.eventplatform.in/events/ar_rahman_chennai.html",
  "imageUrl": "https://cdn.eventplatform.in/events/ar_rahman_chennai.jpg",
  "dateTime": "2025-09-15T19:00:00Z",
  "durationHours": 3.5,
  "requiresStaff": true,
  "venueId": 5,
  "hasAcceptedPolicy": true,
  "ticketTiers": [
    { "tierName": "General", "price": 1500.00 },
    { "tierName": "VIP",     "price": 4500.00 }
  ]
}
```

> `eventType`: `"Physical"` | `"Virtual"` | `"Hybrid"`  
> `venueId`: Required for Physical/Hybrid events.  
> **Note**: For Virtual and Hybrid events, the virtual meeting URL and passcode are dynamically generated when payment is confirmed and the event goes live.

---

#### Check Staff Availability
`POST http://localhost:5106/api/event/check-staff`

```json
{
  "venueId": 5,
  "dateTime": "2025-09-15T19:00:00Z",
  "durationHours": 3.5
}
```

---

#### Confirm Event (Post Upfront Payment)
`POST http://localhost:5106/api/event/{eventId}/confirm`

```json
{
  "stripeChargeId": "ch_3Px4A2LkdIwHu7ix0abcXYZ",
  "paymentMethod": "card"
}
```

---

#### Cancel Event
`POST http://localhost:5106/api/event/{eventId}/cancel`

*No request body.*

---

#### Revert Pending Event Creation
`POST http://localhost:5106/api/event/{eventId}/revert`

*No request body.*

---

#### Verify Entry Ticket (Check-in)
`POST http://localhost:5106/api/event/verify-ticket` 🔒

```json
{
  "hash": "QR_HASH_STRING_FROM_TICKET"
}
```

---

### Ticket Booking (`/api/booking`) 🔒 Requires Auth

---

#### Initiate Booking
`POST http://localhost:5106/api/booking`

```json
{
  "eventId": 42,
  "tierQuantities": {
    "General": 2,
    "VIP": 1
  }
}
```

---

#### Confirm Booking (Post Payment)
`POST http://localhost:5106/api/booking/{bookingId}/confirm`

```json
{
  "stripeChargeId": "ch_3Px4A2LkdIwHu7ix0abcXYZ",
  "paymentMethod": "card"
}
```

---

#### Get My Bookings
`GET http://localhost:5106/api/booking`

**Query Parameters:**

| Param    | Type   | Required | Description                                                    |
|----------|--------|----------|----------------------------------------------------------------|
| `status` | string | No       | Filter bookings by status (e.g. `Confirmed` or `Cancelled`)   |

*No request body.*

**Response Example:**
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

---

#### Cancel Booking
`POST http://localhost:5106/api/booking/{bookingId}/cancel`

*No request body.*

---

#### Revert Pending Booking
`POST http://localhost:5106/api/booking/{bookingId}/revert`

*No request body.*

---

### Support Tickets (`/api/support`) 🔒 Requires Auth

---

#### Submit Support Query
`POST http://localhost:5106/api/support/tickets`

```json
{
  "subject": "Cannot access my booking",
  "message": "I paid but my booking shows as pending.",
  "requestType": "booking",
  "relatedId": 10384
}
```

> `relatedId` (int?, optional): The ID of the booking or event related to this support query.

---

---

## 🛡️ Admin

### Authentication (`/api/auth`) — Public

---

#### Admin Login
`POST http://localhost:5106/api/auth/admin/login`

```json
{
  "adminId": "ADM_1001",
  "password": "SecureAdminPass123"
}
```

---

#### Forgot Password (Request OTP)
`POST http://localhost:5106/api/auth/forgot-password`

```json
{
  "email": "admin@eventplatform.in"
}
```

---

#### Reset Password
`POST http://localhost:5106/api/auth/reset-password`

```json
{
  "email": "admin@eventplatform.in",
  "otp": "482910",
  "newPassword": "NewAdminPass456"
}
```

---

### Dashboard & Events (`/api/admin`) 🔒 Role: `admin`

---

#### Dashboard Stats
`GET http://localhost:5106/api/admin/stats`

*No request body.*

---

#### List / Filter Events
`GET http://localhost:5106/api/admin/events`

**Query Parameters:**

| Param       | Type     | Required | Description                          |
|-------------|----------|----------|--------------------------------------|
| `keyword`   | string   | No       | Title search                         |
| `eventType` | string   | No       | `"Physical"` / `"Virtual"` / `"Hybrid"` |
| `status`    | string   | No       | Event status filter                  |
| `startDate` | datetime | No       | Filter from date                     |
| `endDate`   | datetime | No       | Filter to date                       |
| `sortBy`    | string   | No       | Sort column                          |
| `page`      | int      | No       | Page number (default: 1)             |
| `size`      | int      | No       | Page size (default: 10)              |

---

### Support Ticket Management (`/api/admin`) 🔒 Role: `admin`

---

#### Get All Support Tickets
`GET http://localhost:5106/api/admin/support/tickets`

*No request body.*

**Response Example:**
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

---

#### Respond to Support Ticket
`POST http://localhost:5106/api/admin/support/tickets/{id}/respond`

```json
{
  "response": "Your booking has been confirmed. Please check your email."
}
```

---

#### Escalate Ticket to Finance
`POST http://localhost:5106/api/admin/support/tickets/{id}/escalate`

```json
{
  "actionType": "REF",
  "targetType": "ATD",
  "targetId": 15,
  "referenceId": 42
}
```

> `actionType`: `"REF"` (Refund) | `"EVT"` (Event) | `"ACC"` (Account) | `"FIN"` (Finance) | `"GEN"` (General)  
> `targetType`: `"ATD"` (Attendee) | `"ORG"` (Organizer)

---

### Event Reports (`/api/admin`) 🔒 Role: `admin`

---

#### Get Flagged Event Reports
`GET http://localhost:5106/api/admin/reports`

*No request body.*

---

#### Dismiss Report
`POST http://localhost:5106/api/admin/reports/{reportId}/dismiss`

*No request body.*

---

#### Uphold Report
`POST http://localhost:5106/api/admin/reports/{reportId}/uphold`

```json
{
  "reason": "Event description was confirmed to be misleading.",
  "organizerAction": "Restrict"
}
```

> `organizerAction`: `"No Action"` | `"Restrict"` | `"Deactivate"`

---

### Regions & Venues (`/api/admin`) 🔒 Role: `admin`

---

#### Get All Regions
`GET http://localhost:5106/api/admin/regions`

*No request body.*

---

#### Get All Venues
`GET http://localhost:5106/api/admin/venues`

*No request body.*

---

#### Register New Venue
`POST http://localhost:5106/api/admin/venues`

```json
{
  "region_Id": "REG_001",
  "name": "The Grand Hall",
  "address": "123 Main Street, Chennai",
  "hourly_Price": 5000.00,
  "is_Available": true,
  "seatTiers": [
    { "tier_Name": "General",  "total_Seats": 200 },
    { "tier_Name": "VIP",      "total_Seats": 50  },
    { "tier_Name": "Backstage","total_Seats": 10  }
  ]
}
```

---

### Staff Management (`/api/admin`) 🔒 Role: `admin`

---

#### Get Staff Directory
`GET http://localhost:5106/api/admin/staff`

*No request body.*

---

#### Allocate Staff to Event
`POST http://localhost:5106/api/admin/events/{eventId}/allocate-staff`

```json
{
  "employeeId": "EMP_0042"
}
```

---

---

## 💰 Finance

### Authentication (`/api/auth`) — Public

---

#### Finance Login — Step 1 (Trigger OTP)
`POST http://localhost:5106/api/auth/finance/login`

```json
{
  "adminId": "FIN_1001",
  "password": "SecureFinancePass123"
}
```

**Response (OTP triggered):**
```json
{
  "otpRequired": true,
  "message": "OTP has been sent to your registered email."
}
```

---

#### Finance Login — Step 2 (Verify OTP)
`POST http://localhost:5106/api/auth/finance/login/verify`

```json
{
  "adminId": "FIN_1001",
  "otp": "738291"
}
```

**Response:**
```json
{
  "token": "<signed_jwt_token>"
}
```

---

#### Forgot Password (Request OTP)
`POST http://localhost:5106/api/auth/forgot-password`

```json
{
  "email": "finance@eventplatform.in"
}
```

---

#### Reset Password
`POST http://localhost:5106/api/auth/reset-password`

```json
{
  "email": "finance@eventplatform.in",
  "otp": "482910",
  "newPassword": "NewFinancePass456"
}
```

---

### Finance Actions (`/api/finance`) 🔒 Role: `Finance`

---

#### Get Pending Actions Queue
`GET http://localhost:5106/api/finance/actions`

*No request body. Returns all pending escalated admin actions for review.*

---

#### Approve Action
`POST http://localhost:5106/api/finance/actions/{id}/approve`

```json
{
  "refundType": "FUL",
  "message": "Full refund approved due to event cancellation."
}
```

> `refundType`: `"FUL"` (Full) | `"DYN"` (Dynamic) | `"REM"` (Remaining) | `"NOR"` (No Refund)

---

#### Decline Action
`POST http://localhost:5106/api/finance/actions/{id}/decline`

```json
{
  "remarks": "Insufficient evidence provided to process a refund."
}
```

---

#### Respond to Escalated Ticket
`POST http://localhost:5106/api/finance/tickets/{id}/respond`

```json
{
  "response": "Refund of ₹499 has been processed to your original payment method."
}
```

---

#### Get Transactions (Filtered, Sorted, Paginated)
`GET http://localhost:5106/api/finance/transactions`

**Query Parameters:**

| Param             | Type     | Required | Description                                                    |
|-------------------|----------|----------|----------------------------------------------------------------|
| `keyword`         | string   | No       | Searches reference, sender/receiver ID, or remarks             |
| `transactionType` | string   | No       | Filter by type (e.g. `"BookingPayment"`, `"OrganizerPayout"`)  |
| `status`          | string   | No       | Filter by status (e.g. `"Success"`, `"Pending"`, `"Refunded"`) |
| `startDate`       | datetime | No       | Filter transactions from date                                  |
| `endDate`         | datetime | No       | Filter transactions to date                                    |
| `sortBy`          | string   | No       | Sort column (`"date_asc"`, `"amount_asc"`, `"amount_desc"`, etc.)|
| `page`            | int      | No       | Page number (default: 1)                                       |
| `size`            | int      | No       | Page size (default: 10)                                        |

*No request body.*

---

## 📜 Policies

### Policy Management (`/api/policies`) — Public

---

#### Get Active Policy by Type
`GET http://localhost:5106/api/policies/{type}`

**Purpose:** Retrieve the active terms, conditions, or rules of a specific policy type along with the full markdown content.

**Parameters:**
- `type` (route parameter): The policy type (e.g., `"General"`, `"EventCreation"`, etc.)

**Response:**
```json
{
  "termsId": 10000,
  "version": "v1.0",
  "type": "EventCreation",
  "content": "# Event Organizer Creation Agreement\n..."
}
```



