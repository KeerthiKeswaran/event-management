### Endpoints Implemented:

------------ Populates (Triggers Automatically by Rendering) --------

1. Popular Regions:
   api/regions/popular: This endpoint retrieves the list of popular regions to show on the homepage. It requests optional { rankNumber } and returns popular region objects.
   service status: implemented in RegionController.cs
   client status: implemented in region.service.ts

2. List Cities / Regions:
   api/regions: This endpoint retrieves all active cities on the platform. It requests nothing and returns a list of region objects.
   service status: implemented in RegionController.cs
   client status: implemented in region.service.ts

3. Browse & Search Events:
   api/Event: This endpoint queries paginated lists of events. It requests query parameters { keyword, category, minDateTime, regionId, page, size } and returns filtered page results.
   service status: implemented in EventController.cs
   client status: implemented in event.service.ts

4. Trending Events:
   api/Event/trending: This endpoint retrieves the list of trending events to show on the homepage hero carousel. It requests nothing and returns a list of browsed event objects.
   service status: implemented in EventController.cs
   client status: implemented in event.service.ts

5. Event Detail Info:
   api/Event/{eventId}: This endpoint retrieves details for a single event. It requests { eventId } and returns complete event configurations.
   service status: implemented in EventController.cs
   client status: implemented in event.service.ts

6. Retrieve User Profile:
   api/user/profile: This endpoint retrieves the details of the active user. It requests nothing (authenticated via JWT header) and returns profile details.
   service status: implemented in UserController.cs
   client status: implemented in auth.service.ts

7. Fetch Policy / Consent Document by Type:
   api/policies/{type}: This endpoint retrieves terms and conditions, data consent, or cancellation policies by type (e.g. cancellation, terms, data_consent). It requests { type } in route and returns { termsId, version, type, content }.
   service status: implemented in PoliciesController.cs
   client status: implemented in auth.service.ts

8. Event Ticket Tier Seat Availability:
   api/Event/{eventId}/seats: This endpoint retrieves the total and available seat capacity for each ticket tier of an event. It requests { eventId } and returns a list of ticket tier capacities.
   service status: implemented in EventController.cs
   client status: implemented in event.service.ts

9. Recommended Events:
   api/Event/recommended: This endpoint retrieves list of events matching user's interested regions. It requests nothing (authenticated) and returns list of events.
   service status: implemented in EventController.cs
   client status: implemented in event.service.ts

10. Get My Organized Events:
    api/user/my-events: This endpoint lists events organized by the authenticated user. It requests nothing and returns a list of events.
    service status: implemented in UserController.cs
    client status: implemented in event.service.ts

11. View My Organized Event Detail:
    api/user/my-events/{eventId}: This endpoint returns details of a single organized event. It requests event id and returns details.
    service status: implemented in UserController.cs
    client status: implemented in event.service.ts

12. Categories Listing:
    api/Event/categories: This endpoint retrieves the categories JSON array. It requests nothing and returns the category list.
    service status: implemented in EventController.cs
    client status: implemented in event.service.ts

13. Get Active Virtual Links:
    api/booking/active-links: This endpoint retrieves active virtual meeting links for events that have started and not ended.
    service status: implemented in BookingController.cs
    client status: implemented in booking.service.ts

13b. Get User Bookings:
     api/booking: This endpoint retrieves the authenticated user's active and past booking history.
     service status: implemented in BookingController.cs
     client status: implemented in booking.service.ts

14. Get Organizer Dashboard Metrics:
    api/user/my-dashboard: This endpoint retrieves organizer stats and upcoming events preview.
    service status: implemented in UserController.cs
    client status: implemented in event.service.ts

15. Get Venues (Public):
    api/event/venues: This endpoint retrieves all approved venues.
    service status: implemented in EventController.cs
    client status: implemented in event.service.ts

15b. Get Popular Events:
     api/Event/popular: This endpoint retrieves lists of popular events based on tickets sold.
     service status: implemented in EventController.cs
     client status: implemented in event.service.ts


-------------- Services (Triggers by an event) -------

1. User Login:
   api/auth/user/login: This endpoint authenticates user credentials. It requests { email, password } and returns { token } containing user session data.
   service status: implemented in UserAuthController.cs
   client status: implemented in auth.service.ts

2. User Registration:
   api/auth/user/register: This endpoint creates a new user account. It requests { name, email, mobileNumber, password, otp } and returns { token, message }.
   service status: implemented in UserAuthController.cs
   client status: implemented in auth.service.ts

3. OTP Verification Sender:
   api/auth/user/send-otp: This endpoint generates and sends a 6-digit confirmation code. It requests { email, purpose } and returns { message }.
   service status: implemented in UserAuthController.cs
   client status: implemented in auth.service.ts

4. OTP Verification Validator:
   api/auth/user/verify-otp: This endpoint validates a 6-digit confirmation code. It requests { email, otp, purpose } and returns { message }.
   service status: implemented in UserAuthController.cs
   client status: implemented in auth.service.ts

5. Password Resetting:
   api/auth/user/reset-password: This endpoint resets a user password. It requests { email, otp, newPassword } and returns { message }.
   service status: implemented in UserAuthController.cs
   client status: implemented in auth.service.ts

6. Region Preferences Selection:
   api/user/select-regions: This endpoint saves the default preferred city/region. It requests { regionId } and returns { message }.
   service status: implemented in UserController.cs
   client status: implemented in auth.service.ts

7. Event Booking Ticket Reservation:
   api/booking: This endpoint initiates a new ticket booking reservation. It requests { eventId, attendeeId, ticketTierQuantities } and returns InitiateBookingResponse containing basic event details and settings-calculated prices (total price, fee rate, commission percentage).
   service status: implemented in BookingController.cs
   client status: implemented in booking.service.ts

8. Confirm Booking Payment:
   api/booking/{bookingId}/confirm: This endpoint confirms payment completion. It requests { stripeChargeId, paymentMethod } and returns ConfirmBookingResponse containing full absolute QR code path, virtual URL as "Disabled", and ticket details (excluding password hash, check-in status, and booking status).
   service status: implemented in BookingController.cs
   client status: implemented in booking.service.ts

9. Revert Booking:
   api/booking/{bookingId}/revert: This endpoint cancels a pending booking. It requests { bookingId } and returns void.
   service status: implemented in BookingController.cs
   client status: implemented in booking.service.ts

10. Cancel Booking:
    api/booking/{bookingId}/cancel: This endpoint cancels an existing active booking. It requests { bookingId } and returns void.
    service status: implemented in BookingController.cs
    client status: implemented in booking.service.ts

11. Refund Estimation:
    api/booking/{bookingId}/refund-estimate: This endpoint calculates the estimated refund amount. It requests { bookingId } and returns { estimatedRefund }.
    service status: implemented in BookingController.cs
    client status: Not Implemented (Calculated locally on client side, direct API call not included)

12. Update User Profile:
    api/user/profile (PUT): This endpoint updates the authenticated user's profile details (Name, Mobile Number). It requests { name, mobileNumber } and returns { message }.
    service status: implemented in UserController.cs
    client status: implemented in auth.service.ts

13. Close Account:
    api/user/close-account: This endpoint deactivates and permanently deletes the user account. It requests { reason, explanation, confirmName, otp } and returns { message }.
    service status: implemented in UserController.cs
    client status: implemented in auth.service.ts

14. Create Event:
    api/Event (POST): This endpoint registers a new event setup. It requests event configuration payload.
    service status: implemented in EventController.cs
    client status: implemented in event.service.ts

14b. Get Platform Settings:
     api/Event/platform-settings (GET): This endpoint retrieves platform fee configuration (activation fees, staff rate, ticket fees).
     service status: implemented in EventController.cs
     client status: implemented in event.service.ts

14c. Upload Description File:
     api/Event/upload-description (POST): This endpoint saves event description text as a .txt file and returns the URL. It requests { text }.
     service status: implemented in EventController.cs
     client status: implemented in event.service.ts

14d. Upload Event Image:
     api/Event/upload-image (POST): This endpoint saves event image file (JPG/PNG) and returns the URL. It accepts multipart form file upload.
     service status: implemented in EventController.cs
     client status: implemented in event.service.ts

15. Confirm Event Upfront Payment:
    api/Event/{eventId}/confirm (POST): This endpoint registers the organizer's upfront deposit. It requests payment details.
    service status: implemented in EventController.cs
    client status: implemented in event.service.ts

16. Cancel Event:
    api/Event/{eventId}/cancel (POST): This endpoint cancels a scheduled event.
    service status: implemented in EventController.cs
    client status: implemented in event.service.ts

17. Revert Pending Event:
    api/Event/{eventId}/revert (POST): This endpoint cancels a pending event registration.
    service status: implemented in EventController.cs
    client status: implemented in event.service.ts

18. Submit Event Feedback:
    api/Event/{eventId}/feedback (POST): This endpoint registers ratings and reviews. It requests { rating, review } in body.
    service status: implemented in EventController.cs
    client status: implemented in event.service.ts

19. Submit Support Ticket:
    api/Support/tickets (POST): This endpoint registers a support query ticket. It requests { subject, message, requestType, relatedId }.
    service status: implemented in SupportController.cs
    client status: implemented in admin.service.ts

20. Platform Convenience Fee Calculation:
    api/booking/calculate-fee: This endpoint estimates convenience fees for checkout. It requests { eventId, tierQuantities } and returns { fee }.
    service status: implemented locally on client
    client status: Implemented (Calculated locally on client side)

21. Get Age Categories:
    api/Event/age-categories (GET): This endpoint retrieves dynamic age category restrictions for event setups.
    service status: implemented in EventController.cs
    client status: implemented in event.service.ts

22. Check Platform Event Staff Availability:
    api/Event/check-staff (POST): This endpoint checks for platform event staff availability. It requests { venueId, dateTime } and returns { available, count, estimatedFee }.
    service status: implemented in EventController.cs
    client status: implemented in event.service.ts

23. Admin Login:
    api/auth/admin/login (POST): Authenticates admin credentials.
    service status: implemented in DeptAuthController.cs
    client status: implemented in admin.service.ts

24. Admin Stats Dashboard:
    api/admin/stats (GET): Retrieves general metrics and stats for the admin dashboard.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

25. Admin Events List:
    api/admin/events (GET): Retrieves a paged view of events for admin oversight.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

26. Admin Flagged Event Reports:
    api/admin/reports (GET): Retrieves flagged event reports for moderation.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

27. Admin Dismiss Report:
    api/admin/reports/{reportId}/dismiss (POST): Dismisses moderation flags against an event.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

28. Admin Uphold Report:
    api/admin/reports/{reportId}/uphold (POST): Upholds moderation flags and suspends organizer account.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

29. Admin Support Ticket List:
    api/admin/support/tickets (GET): Retrieves all active support tickets.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

30. Admin Respond to Support Ticket:
    api/admin/support/tickets/{ticketId}/respond (POST): Answers tickets from admin role.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

31. Admin Support Ticket Escalation:
    api/admin/support/tickets/{ticketId}/escalate (POST): Escalates support tickets to higher levels.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

32. Admin Regions List:
    api/admin/regions (GET): Retrieves all region options for admin operations.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

33. Admin Venues List:
    api/admin/venues (GET): Retrieves all venue details for admin oversight.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

34. Admin Register New Venue:
    api/admin/venues (POST): Creates and configures a new approved physical venue.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

35. Admin Get Staff Directory:
    api/admin/staff (GET): Retrieves list of available staff and allocations.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

36. Admin Allocate Staff to Event:
    api/admin/events/{eventId}/allocate-staff (POST): Manually assigns a staff member to a physical event.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

37. Admin Get Profile:
    api/admin/profile (GET): Retrieves admin profile details.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

38. Admin Update Profile:
    api/admin/profile (PUT): Updates admin profile name.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

39. Admin Helpdesk Metadata:
    api/admin/support/metadata (GET): Retrieves action types and target types from JSON file for helpdesk escalation.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

40. Admin Get Staff by Region:
    api/admin/staff/by-region/{regionId} (GET): Retrieves available staff members filtered by working region.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

41. Admin Get Events by Region:
    api/admin/events/by-region/{regionId} (GET): Retrieves events filtered by venue region for staff allocation modal.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

42. Admin Update Venue:
    api/admin/venues/{venueId} (PUT): Updates venue details (name, address, price, region).
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

43. Finance Pending Actions List:
    api/finance/actions (GET): Retrieves pending refund actions for completed or cancelled bookings.
    service status: implemented in FinanceController.cs
    client status: implemented in finance.service.ts

44. Finance Payout Action Approval:
    api/finance/actions/{id}/approve (POST): Confirms refund payout execution.
    service status: implemented in FinanceController.cs
    client status: implemented in finance.service.ts

45. Finance Payout Action Decline:
    api/finance/actions/{id}/decline (POST): Rejects refund payout request.
    service status: implemented in FinanceController.cs
    client status: implemented in finance.service.ts

46. Finance Respond to Support Ticket:
    api/finance/tickets/{id}/respond (POST): Sends direct message to attendee/organizer support ticket.
    service status: implemented in FinanceController.cs
    client status: implemented in finance.service.ts

47. Finance Portal Login:
    api/auth/finance/login (POST): Authenticates finance credentials.
    service status: implemented in DeptAuthController.cs
    client status: implemented in finance.service.ts

48. Finance Portal OTP Validation:
    api/auth/finance/login/verify (POST): Verifies two-factor OTP code for finance login.
    service status: implemented in DeptAuthController.cs
    client status: implemented in finance.service.ts

49. Admin/Finance Forgot Password:
    api/auth/forgot-password (POST): Requests password reset OTP for administrators.
    service status: implemented in DeptAuthController.cs
    client status: implemented in admin.service.ts

50. Admin/Finance Reset Password:
    api/auth/reset-password (POST): Confirms password updates using reset OTP tokens.
    service status: implemented in DeptAuthController.cs
    client status: implemented in admin.service.ts

51. Report Event:
    api/Event/{eventId}/report (POST): Files a code of conduct report from attendee.
    service status: implemented in EventController.cs
    client status: implemented in event.service.ts

52. Verify Ticket check-in:
    api/Event/verify-ticket (POST): Verifies entrance ticket QR hash.
    service status: implemented in EventController.cs
    client status: implemented in event.service.ts

53. Finance Transactions List:
    api/finance/transactions (GET): Retrieves paged list of transaction records for finance audit.
    service status: implemented in FinanceController.cs
    client status: implemented in finance.service.ts

54. Check Email Availability:
    api/auth/user/check-email (GET): Checks if an email is available for registration or profile update.
    service status: implemented in UserAuthController.cs
    client status: implemented in auth.service.ts

55. Update Event Details:
    api/Event/{eventId}/details (PUT): Updates the event details (title and description) for an organizer.
    service status: implemented in EventController.cs
    client status: implemented in event.service.ts

56. Retrieve Support Tickets:
    api/Support/tickets (GET): Retrieves the list of support tickets created by the authenticated user.
    service status: implemented in SupportController.cs
    client status: implemented in admin.service.ts

57. Admin Support Ticket Escalation Status:
    api/admin/support/tickets/{id}/escalation-status (GET): Retrieves the current escalation progress/status of a support ticket.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

58. Admin Get All Venues:
    api/admin/venues/all (GET): Retrieves a complete list of all venues for dropdowns and allocations.
    service status: implemented in AdminController.cs
    client status: implemented in admin.service.ts

59. Global Health Check:
    api/Health (GET): Retrieves a simple 200 OK status to confirm the backend server is running and accessible.
    service status: implemented in HealthController.cs
    client status: implemented in app.routes.ts (Global App Component)

60. Finance Dashboard Stats:
    api/finance/dashboard-stats (GET): Retrieves dashboard statistics for finance (total transactions, revenue, intake, pending approvals).
    service status: implemented in FinanceController.cs
    client status: implemented in finance.service.ts

61. Finance Organizer Payouts:
    api/finance/payouts (GET): Retrieves paginated organizer payout records. Shows events with status "Live" or "Completed". For each event, returns the total booking revenue collected minus the platform Ticket_Commission_Percentage. Status is "Upcoming" if event is Live, "Completed" if event is Completed. Supports sortBy (e.g. date_desc, date_asc), page, and size query parameters.
    service status: implemented in FinanceController.cs → GetOrganizerPayouts
    client status: implemented in finance.service.ts → getPayouts()

### DB Schema:
1. Age restriction to Events table. ✅
2. Category to the events table. ✅



### Files:
1. Policies (Data Consent, Cancellation Policy md files) ✅
2. Email templates (user password reset otp) ✅
3. Email template: OrganizerAccountActionTemplate.html ✅ — Sent to organizer when a report against their event is upheld. Contains event name, action reason, new account status (Restricted/Deactivated/No Action), and refund assurance for attendees.


### Behaviour Changes (Report Module):

62. Uphold Event Report — Enhanced Full Flow:
    api/admin/reports/{reportId}/uphold (POST): Upholds a flagged event report.
    Changes in this version:
      a. Event is cancelled if status is Live or Pending (current time < event time).
      b. IRefundService.RefundOrganizerAsync is now called with refundType="Full" — automatically processes 100% Stripe refunds for all confirmed attendees of the event.
      c. Organizer receives a professional OrganizerAccountActionTemplate.html email detailing event cancellation, the reason, and their new account status (Restricted / Deactivated / No Action).
      d. Report ResponseAction is now correctly set to "Upheld" (previously was "Upholds").
      e. AdminAction.ActionStatus is set to "Completed" immediately (previously was "Pending").
    service status: implemented in AdminService.cs → UpholdEventReportAsync
    client status: No API change — existing POST /admin/reports/{id}/uphold

63. Admin User Management Notifications:
    AdminService.cs -> UpdateUserStatusAsync: Now automatically triggers an email notification to the user communicating the exact account status change (Activated, Restricted, Deactivated).
64. Refund Cancellation Messages:
    RefundService.cs -> ProcessEventCancellationRefundsAsync / RefundAttendeeAsync: Fallback reasoning dynamically handles general cancellations vs moderation-based cancellations.
65. Helpdesk Internal Tickets:
    AdminService.cs -> GetSupportTicketsAsync: Automatically flags tickets raised internally by an admin (identified by checking JSON file metadata for `userId` starting with 'ADM').

### Files Updates:
4. Email Templates (General) ✅ — All 11 HTML email templates converted to use CID inline attachment for `logo.png` rather than text-based headers or localhost URLs, enabling stable and secure rendering on modern mail clients.
