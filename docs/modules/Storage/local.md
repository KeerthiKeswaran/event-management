# Local Storage Configuration

This document outlines the local storage architecture used within the Event Management application. It specifies what files are stored locally, their configurations, paths, and the respective service classes responsible for managing them.

---

## 1. Email Notification Payloads (JSON)

### Overview
Every email notification sent by the platform is archived locally on disk in JSON format. This allows for historical auditing and retrying of notification messages if needed.

*   **Content stored:** JSON object containing `Subject` and `Body` (HTML format).
*   **Target Directory:** `Event.Business/assets/notifications/`
*   **Filename Format:** `{Notification_Id}.json`
*   **Database Reference:** The `Notification.MessageUrl` property points to the relative path of the file (`Event.Business/assets/notifications/{Notification_Id}.json`).
*   **Implementation Source:** [NotificationHelper.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Helpers/NotificationHelper.cs#L32-L62)

### Configuration / Path Resolution
The storage location is dynamically resolved depending on where the execution context is initiated (API hosting environment, test runner, or external project directory):
```csharp
string rootPath = Directory.GetCurrentDirectory();
if (rootPath.Contains("bin") || rootPath.EndsWith("Tests"))
{
    rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
}
else if (rootPath.EndsWith("Event.API"))
{
    rootPath = Path.GetFullPath(Path.Combine(rootPath, ".."));
}
string notificationDir = Path.Combine(rootPath, "Event.Business", "assets", "notifications");
```

---

## 2. Booking QR Codes (PNG)

### Overview
When an attendee registers/books tickets for an event, a QR code containing a secure validation hash is generated. This image is stored locally to be served on tickets or verified at physical checkpoints.

*   **Content stored:** PNG image containing the encoded raw secret validation hash (`Qr_Secret_Hash`).
*   **Target Directory:** `assets/{Attendee_Id}/bookings/` relative to the workspace root.
*   **Filename Format:** `qr_{bookingId}.png`
*   **Database Reference:** The `Booking.Qr_Code_Path` property stores the public relative path `/assets/{Attendee_Id}/bookings/qr_{bookingId}.png`.
*   **Implementation Source:** [BookingService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/BookingService.cs#L224-L238)

### Configuration / Path Resolution
To ensure QR codes are saved to the workspace asset folder rather than the build output folder (`bin/`), the root path is resolved back to the source directory:
```csharp
string rootPath = Directory.GetCurrentDirectory();
if (rootPath.Contains("bin"))
{
    rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
}
var ticketsDir = Path.Combine(rootPath, "assets", booking.Attendee_Id.ToString(), "bookings");
```

---

## 3. Support Tickets & Escalation Reports (JSON)

### Overview
Support concern details and administrative actions are saved to local JSON files. Relative file paths to these concerns are linked directly to support ticket entities in the database.

*   **Content stored:** JSON object containing `Subject`, `Message`, and `Response`.
*   **Implementation Sources:**
    *   [SupportService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/SupportService.cs#L46-L75) (Submission of new tickets)
    *   [AdminService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/AdminService.cs#L377-L407) (Admin resolution and escalation of tickets)
    *   [FinanceService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/FinanceService.cs#L165-L200) (Finance user resolution of tickets)

### Target Directories & Filename Formats:
1.  **Standard Tickets:**
    *   **Directory:** `assets/support_tickets/` relative to the execution current directory.
    *   **Filename:** `ticket_{Guid}.json`
    *   **Database Reference:** `SupportTicket.ConcernUrl` is stored as `/assets/support_tickets/ticket_{Guid}.json`.
    *   **Implementation Code:** [SupportService.cs:L47](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/SupportService.cs#L47)
2.  **Escalated Reports (from flags/policy violations):**
    *   **Directory:** `assets/esclation/` relative to the execution current directory.
    *   **Filename:** `escalation_report_{reportId}.json`
    *   **Database Reference:** `SupportTicket.ConcernUrl` is stored as `/assets/esclation/escalation_report_{reportId}.json`.
    *   **Escalated Report Creation Code:** [AdminService.cs:L377-407](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/AdminService.cs#L377-L407)

---

## 4. Policy Terms & Conditions (Markdown)

### Overview
Active terms of service, platform agreements, and policy descriptions are written in markdown files on disk. The system resolves these files based on database metadata to return policy content dynamically.

*   **Content stored:** Markdown text representing legal agreements.
*   **Target Directory:** `assets/policies/` relative to the application base folder.
*   **Filename Format:** `{Terms_Id}.md` (mapped from active database records).
*   **Implementation Source:** [PolicyService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/PolicyService.cs#L28-L36)

### Configuration / Path Resolution
```csharp
var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "policies", $"{policy.Terms_Id}.md");
```

---

## 5. Email HTML Templates (HTML)

### Overview
The system relies on HTML template files located in the source code to build formatted emails with dynamic placeholder replacement.

*   **Content stored:** HTML markup templates (e.g., `SupportTicketResponseTemplate.html`).
*   **Target Directory:** `Event.Business/Templates/` relative to the source project directory.
*   **Filename Format:** `{TemplateName}`
*   **Implementation Source:** [EmailService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/EmailService.cs#L81-L93)

### Configuration / Path Resolution
The template path is resolved dynamically relative to the executing project directory:
```csharp
string rootPath = Directory.GetCurrentDirectory();
if (rootPath.Contains("bin"))
{
    rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
}
else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
{
    rootPath = Path.GetFullPath(Path.Combine(rootPath, ".."));
}

string templatePath = Path.Combine(rootPath, "Event.Business", "Templates", dto.TemplateName);
```
