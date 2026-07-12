# Azure Board Workflows

## Session: Support Ticket Module (Admin & Finance)

### Epic: Support Ticket for Refunds
**Description:** Implement the entire support ticket workflow from user submission, admin categorization and response, to financial escalation and refund processing.

#### User Story 1: Submit Support Ticket (Attendee & Organizer)
**As a** User (Attendee or Organizer),
**I want to** submit a support ticket regarding my concerns (e.g., cancellations, refunds, general queries),
**So that** the admin team can review and resolve my issue.

**Tasks:**
**Task-1: Frontend Support Ticket Form**
Implement the frontend interface allowing users (Attendees and Organizers) to submit a new concern. The form should capture the Subject, Message, and Request Type.

**Task-2: Implement Submission Endpoint**
Create the `POST api/support/tickets` endpoint in the backend controller to accept incoming ticket requests from users.

**Task-3: Generate Local JSON Concern File**
Implement logic in `SupportService.cs` to format the user's query into a JSON object and securely save it to the local filesystem (e.g., `assets/support_tickets/ticket_{Guid}.json`).

**Task-4: Update SupportTicket Database Record**
Create a new record in the `SupportTicket` table. Link it to the user, set `ConcernUrl` to the path of the generated JSON file, set the Status to "Open", and ensure `EscalationStatus` is initialized to `null`.

**Task-5: Frontend Redirection and Success Notification**
Return a successful API response and handle it in the frontend by displaying a success toast and redirecting the user to their ticket history dashboard.

---

#### User Story 2: View and Categorize Support Tickets
**As an** Admin,
**I want to** view a helpdesk page containing all support tickets and categorize them by Action (Refund, Event, Account, General) and Target Type (Attendee, Organizer),
**So that** I can effectively monitor and filter incoming concerns.

**Tasks:**
**Task-1: Create Category JSON Mapping**
Create a JSON file in the backend (`business/assets/admin`) that contains the abbreviations mapping for Actions (REF, EVT, ACC, GEN) and Target Types (ATD, ORG).

**Task-2: Serve JSON Mapping to Frontend**
Implement a backend API endpoint to serve this JSON mapping so the frontend can retrieve the dropdown options dynamically.

**Task-3: Fetch All Support Tickets Endpoint**
Implement `GET api/admin/support/tickets` in `AdminController.cs` to fetch all support tickets from the database.

**Task-4: Design Admin Helpdesk UI**
Design and implement the Helpdesk UI in the admin dashboard, ensuring it aligns with the strict "military enterprise" style UI consistency guidelines.

**Task-5: Integrate Frontend Dropdowns**
Integrate the frontend category dropdowns to display full names to the admin, while ensuring only the abbreviations are sent back to the server during updates.
