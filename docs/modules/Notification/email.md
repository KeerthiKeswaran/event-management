# Email Operations Module

This module governs all outbound email communication from the platform. It handles registration OTPs, ticket booking confirmations with QR codes, event activations, support responses, and refund notices using dynamic HTML templates.

## 1. Files & Components Involved

### Controllers
* **UserAuthController.cs** -> [UserAuthController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/UserAuthController.cs)
* **DeptAuthController.cs** -> [DeptAuthController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/DeptAuthController.cs)
* **EventController.cs** -> [EventController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/EventController.cs)
* **BookingController.cs** -> [BookingController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/BookingController.cs)
* **FinanceController.cs** -> [FinanceController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/FinanceController.cs)
* **AdminController.cs** -> [AdminController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/AdminController.cs)

### Contracts & Interfaces
* **IEmailService.cs** -> [IEmailService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IEmailService.cs)
* **INotificationRepository.cs** -> [INotificationRepository.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IRepositories/INotificationRepository.cs)

### Services
* **EmailService.cs** -> [EmailService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/EmailService.cs)
* **OtpService.cs** -> [OtpService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/OtpService.cs)
* **BookingService.cs** -> [BookingService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/BookingService.cs)
* **EventService.cs** -> [EventService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/EventService.cs)
* **AdminService.cs** -> [AdminService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/AdminService.cs)
* **FinanceService.cs** -> [FinanceService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/FinanceService.cs)
* **RefundService.cs** -> [RefundService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/RefundService.cs)

### Helpers & Utilities
* **NotificationHelper.cs** -> [NotificationHelper.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Helpers/NotificationHelper.cs)

---

## 2. Trigger Points & Template Matrix

The platform maps actions to specific HTML templates and loads them from disk to replace variables dynamically:

| E-Mail Purpose | Trigger Event | Target Recipient | Template Name |
|---|---|---|---|
| **Security OTP** | Requesting verification code (Registration, reset passwords, login) | User / Admin / Finance | `OtpEmailTemplate.html` |
| **Event Live Activation** | Confirmed upfront payout from Organizer card charge | Organizer | `EventActivationEmailTemplate.html` |
| **Booking Paid / Ticket** | Confirmed ticket booking charge (with embedded base64 QR Code check-in graphics) | Attendee | `BookingConfirmationTemplate.html` |
| **Support Resolved** | Support team responds to escalated concern | User | `SupportTicketResponseTemplate.html` |
| **Refund Success Notification** | Attendee ticket cancellation or Organizer event cancellation refund | User / Organizer | `RefundSuccessTemplate.html` |
| **Event Cancel Notification** | Organizer cancels their active event (Refund notifications sent to attendees) | Attendee | `EventCancellationTemplate.html` |

---

## 3. Detailed Communication Workflows

### I. OTP Dispatch
- **Step 1:** Endpoint requests OTP generation.
- **Step 2:** `OtpService.SendEmailOtpAsync` validates credentials and generates a 6-digit random code.
- **Step 3:** The code is cached in Redis (with 10-minute expiry).
- **Step 4:** `OtpEmailTemplate.html` is compiled with dynamic variables and sent via Brevo SMTP.

### II. Event Activation (Organizers)
- **Step 1:** Organizer submits upfront Stripe payment.
- **Step 2:** `EventService.ConfirmEventAsync` validates payment success.
- **Step 3:** System generates staff allocation slots and Jitsi Room access credentials.
- **Step 4:** Compiles `EventActivationEmailTemplate.html` with host details and SMTP-dispatches to the organizer.

### III. Ticket Bookings & QR Attachment (Attendees)
- **Step 1:** Attendee completes card payment for a live event.
- **Step 2:** `BookingService.ConfirmBookingAsync` generates ticket identifiers and invokes QR Code generation.
- **Step 3:** The QR graphic is written as an embedded base64 image block.
- **Step 4:** Compiles `BookingConfirmationTemplate.html` attaching the QR Code, and dispatches the ticket to the attendee.

### IV. Support Resolutions
- **Step 1:** Administrator or Finance Operator submits response text.
- **Step 2:** `AdminService.RespondToTicketAsync` or `FinanceService.RespondToTicketAsync` resolves the ticket concern.
- **Step 3:** Compiles `SupportTicketResponseTemplate.html` containing the original concern and response copy.
- **Step 4:** Invokes `NotificationHelper.SendAndSaveNotificationAsync` to record a database history block and send SMTP.

### V. Cancellations & Refund Logs
- **Step 1:** Cancel action triggered (booking cancel or event cancel).
- **Step 2:** `RefundService` processes transaction returns via Stripe.
- **Step 3:** System creates a transaction log row marked "Success" (Refunded amount details).
- **Step 4:** Compiles `RefundSuccessTemplate.html` (or `EventCancellationTemplate.html` for event cancellations) and emails targets.

---

## 4. Operational Dispatch Flowchart

```text
                          [ START EMAIL DISPATCH ]
                                     │
                                     ▼
                     [ Construct EmailTemplateDto ]
                     - Specify HTML template name
                     - Define key-value placeholder dictionary
                                     │
                                     ▼
                      [ Load HTML Template from Disk ]
                                     │
                                     ▼
                      [ Replace template placeholders ]
                      - Replace {{placeHolder}} with real data
                                     │
                                     ▼
                      [ Call NotificationHelper ]
                      - Write notification row to DB (Status: "Sent")
                                     │
                                     ▼
                      [ Invoke SMTP / Brevo Mail Send ]
                                     │
                                     ▼
                                [ END / Sent ]
```