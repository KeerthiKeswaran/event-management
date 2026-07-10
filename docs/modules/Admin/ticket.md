# Quick Flow:

So okay, how do you think this entier process starts? Because again there is a case where the admin can raise a ticket from their side to cancel the event, or the organizer/attendee raise a ticket regarding the cancellation, they either mention that they already cancel it and request remaining refund, or they request for cancellation and a full refund!

So how we actually handle it? Once the admin receives a support ticket, we actually need to categorize the support ticket for treating them separately!

If it is regarding cancellation and refund, then admin have an option to proceed with the cancellation and refund process. 

So currently what i'm planing is!




1. User raises a ticket.

2. If it is type of cancellation, then admin have an option as esclate where admin raises a request for the team to approve, 

3. We can have a separate endpoint for Finance team just to approve the admin eslation. Here the support queries (must be renamed into support tickets) database will have another column as esclationstatus it can be null, requested, declined, processing, processed. The admin have chance to either response and close the ticket, or else keep it alive and click on esclate. We also have AdminAction Database to store the actions of the admins.

4. We have different possible cases here:
     1. If the finance team declines it, the db record esclationstatus attribute changes, admin's esclation button changes from requested to declined, he can reply it with a formal response and close the ticket.
     2. If the finance team accepts it, then the db record esclation status changes with processing, moved to the refund worker, and then once the refund has been done it'll be turned into processed and a mail will be sent to the attendee, now the admin can close it.

5. Here is the expanded AdminActions schema containing the execution details:

ActionId: int	
SupportTicketId: int
TransactionId: int
TargetType: string	        ("Attendee" or "Organizer")
RefundType:	string	         ("Full", "Dynamic", or "Remaining")
Ticketid: str
ActionStatus: string	    ("Pending", "Approved", "Declined", "Processing", "Processed")
RequestedBy: str	         (Support Admin ID)
ApprovedBy:	str	              (Finance Admin ID)
CreatedAt: timestamp	      (Time of creation)
Remarks: string




# Admin Support Ticket Module

This module governs all administrative support ticket operations, including support concern retrieval, responding to queries, and escalating technical/financial ticket concerns to the Finance Department.

## 1. Files & Components Involved

### Controllers
* **AdminController.cs**
  * **Path:** [AdminController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/AdminController.cs)
  * **Admin Support Endpoints:**
    * `GET api/admin/support/tickets` (Get all support tickets)
    * `POST api/admin/support/tickets/{id}/respond` (Submit response & resolve ticket)
    * `POST api/admin/support/tickets/{id}/escalate` (Escalate query & trigger Action review)

### Contracts & Interfaces
* **IAdminService.cs** -> [IAdminService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IAdminService.cs)
* **ISupportTicketRepository.cs** -> [ISupportTicketRepository.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IRepositories/ISupportTicketRepository.cs)
* **IAdminActionRepository.cs** -> [IAdminActionRepository.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IRepositories/IAdminActionRepository.cs)

### Services
* **AdminService.cs** -> [AdminService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/AdminService.cs)

---

## 2. Activity & State Flowcharts

### I. Respond to Support Ticket Flow
Allows administrators to write answers to customer/organizer queries directly into JSON-backed concern paths, automatically notifying them via Brevo.

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
                        - Populate placeholders
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

### II. Escalate Ticket to Finance Flow
Logs ticket details inside the AdminAction database queue, assigning a "Pending" status for execution/approval in the Finance team.

```text
                         [ START ESCALATE TICKET ]
                                     │
                                     ▼
                     [ Select Ticket ID & Escalation Info ]
                     - actionType, targetType, targetId, referenceId
                                     │
                                     ▼
                        [ Fetch Support Ticket by ID ]
                                     │
                                     ▼
                             { Ticket Exists? }
                              /              \
                     [No]    /                \ [Yes]
                            ▼                  ▼
                   (Throw NotFound)   [ DB Transaction Start ]
                                             │
                                             ▼
                                     [ Create AdminAction ]
                                     - ActionType: actionType
                                     - TargetType: targetType
                                     - TargetId: targetId
                                     - ReferenceId: referenceId
                                     - ActionStatus: "Pending"
                                             │
                                             ▼
                                     [ Update Support Ticket ]
                                     - Set status: "Escalated"
                                             │
                                             ▼
                                     [ Save Changes to DB ]
                                             │
                                             ▼
                                     [ DB Transaction Commit ]
                                             │
                                             ▼
                                      [ END / Success ]
```
