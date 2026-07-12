# Backend Changes

## New DTOs (`server/Event.Models/DTOs/`)

### PlatformSettingsResponse.cs
- **Fields**: `Staff_Flat_Rate`, `Virtual_Event_Activation_Fee`, `Physical_Event_Activation_Fee`, `Ticket_Commission_Percentage`, `Ticket_Fixed_Fee`, `Max_Tickets_Per_Booking`.
- Maps from `PlatformSettings` model for safe API exposure.

### UploadDescriptionRequest.cs
- **Fields**: `Text` (string, required).
- Used by `POST /api/Event/upload-description`.

## Interface (`server/Event.Contracts/IServices/IEventService.cs`)

Added three new method signatures:
- `Task<PlatformSettingsResponse?> GetPlatformSettingsAsync()`
- `Task<string> SaveDescriptionFileAsync(string text)`
- `Task<string> SaveImageFileAsync(string fileName, byte[] fileBytes)`

## Service Implementation (`server/Event.Business/Services/EventService.cs`)

### GetPlatformSettingsAsync()
- Calls `_settingsRepository.GetSettingsAsync()`, maps to `PlatformSettingsResponse`.

### SaveDescriptionFileAsync(string text)
- Generates a temp GUID folder under `assets/events/temp/{guid}/`.
- Writes text to `description.txt`.
- Returns relative path (`assets/events/temp/{guid}/description.txt`).

### SaveImageFileAsync(string fileName, byte[] fileBytes)
- Generates a temp GUID folder under `assets/events/temp/{guid}/`.
- Writes bytes as `image.{ext}` preserving original extension.
- Returns relative path (`assets/events/temp/{guid}/image.{ext}`).

## Controller (`server/Event.API/Controllers/EventController.cs`)

### GET /api/Event/platform-settings
- **Auth**: AllowAnonymous
- **Returns**: `PlatformSettingsResponse` or 404.
- Maps to `EventService.GetPlatformSettingsAsync()`.

### POST /api/Event/upload-description
- **Auth**: Authenticated (no attribute = Authorize from controller level)
- **Body**: `UploadDescriptionRequest` (`{ text: string }`)
- **Returns**: `{ url: "assets/events/temp/{guid}/description.txt" }`
- Maps to `EventService.SaveDescriptionFileAsync()`.

### POST /api/Event/upload-image
- **Auth**: Authenticated
- **Body**: multipart/form-data with `IFormFile file`
- **Returns**: `{ url: "assets/events/temp/{guid}/image.{ext}" }`
- Maps to `EventService.SaveImageFileAsync()`.

## File Storage Convention

Files are stored relative to the app base directory under:
- `assets/events/temp/{tempId}/description.txt` — description text files
- `assets/events/temp/{tempId}/image.{jpg|png}` — event images

Temp IDs are GUIDs generated on each upload. The frontend includes the returned URL in the `CreateEventRequest.DescriptionUrl` / `CreateEventRequest.ImageUrl` fields.

## Venue Repository (`server/Event.Data/Repositories/VenueRepository.cs`)

### GetByIdAsync Override
- Overridden to `.Include(v => v.SeatCapacities).Include(v => v.Region)` — ensures `SeatCapacities` is eagerly loaded for staff calculation.
- Fixes: 550 total seats → 6 staff (was returning 1 due to lazy loading not populating capacities).

## Admin Service (`server/Event.Business/Services/AdminService.cs`)

### GetAllVenuesAsync
- Added `.Where(v => v.Is_Available)` filter at line 528 — only available venues returned in dropdown.

## Venue Model (`server/Event.Models/Entities/Venue.cs`)

### Staff Calculation
- `CalculateRequiredStaffCount()` sums `SeatCapacities.Sum(c => c.Total_Seats)` → `max(1, (int)Math.Ceiling(total / 100.0))`.
- Staff count now correctly computed from total venue seat capacity.

## Event Policy Endpoint

### GET /api/policies/EventCreation
- Returns `{ termsId, filePath }` with the event creation policy content in markdown format.

## Admin Page (`server/Event.API/Controllers/`)

### DeptAuthController
- `POST /api/auth/admin/login` — admin credential login route for the admin page.

### AdminController
- `GET /api/admin/stats` — returns dashboard statistics for the admin.
- `GET /api/admin/events` — returns paged event listings with filter support.
- `GET /api/admin/reports` — returns flagged event reports.
- `POST /api/admin/reports/{reportId}/dismiss` — dismisses a reported event.
- `POST /api/admin/reports/{reportId}/uphold` — upholds a report with `Reason` and `OrganizerAction`.
- `GET /api/admin/support/tickets` — returns admin support tickets.
- `POST /api/admin/support/tickets/{ticketId}/respond` — submits an admin response to a ticket.
- `POST /api/admin/support/tickets/{ticketId}/escalate` — escalates a ticket with a payload that includes short-form action and target type.
- `GET /api/admin/regions` — returns available region options for venue registration.
- `GET /api/admin/venues` — returns available venues.
- `POST /api/admin/venues` — creates a new venue record.
- `GET /api/admin/staff` — returns staff directory data for allocation.
- `POST /api/admin/events/{eventId}/allocate-staff` — assigns an employee to an event.

### Notes
- Admin controller is protected with `[Authorize(Roles = "admin")]`.
- Uphold report and escalate ticket flows require structured request payloads and carry backend-side validation.

## Admin Page (Round 2 - Refinements)

### New DTOs (`server/Event.Models/DTOs/`)

#### AdminProfileResponse.cs
- **Fields**: `Admin_Id`, `Name`, `Email`.
- Used by `GET /api/admin/profile`.

#### UpdateAdminProfileRequest.cs
- **Fields**: `Name` (string, required).
- Used by `PUT /api/admin/profile`.

#### HelpdeskMetadataResponse.cs
- Contains `Actions` (list of `HelpdeskAction` with `Key`, `Label`) and `TargetTypes` (list of `HelpdeskTargetType` with `Key`, `Label`).
- Deserialized from `assets/admin/helpdesk-types.json`.

### New Assets File
- **`server/Event.Business/assets/admin/helpdesk-types.json`**: JSON file containing action types (REF, EVT, ACC, GEN) and target types (ATD, ORG) with their full-form labels. Fetched via `GET /api/admin/support/metadata`.

### New Interface Methods (`IAdminService.cs`)
- `GetSupportTicketsAsync(status, keyword, dateFrom, dateTo)` — added filter parameters.
- `GetStaffDirectoryAsync(regionId, isAllocated)` — added filter parameters.
- `GetStaffByRegionAsync(regionId)` — returns available staff by working region.
- `GetEventsByRegionAsync(regionId)` — returns events by venue region.
- `GetAdminProfileAsync(adminId)` — returns admin profile.
- `UpdateAdminProfileAsync(adminId, request)` — updates admin name.
- `GetHelpdeskMetadataAsync()` — reads helpdesk types from JSON file.
- `UpdateVenueAsync(venueId, request)` — updates venue details.

### Updated/New Controller Endpoints (`AdminController.cs`)

#### Updated Endpoints:
- `GET /api/admin/support/tickets` — now accepts query params: `status`, `keyword`, `dateFrom`, `dateTo`.
- `GET /api/admin/staff` — now accepts query params: `regionId`, `isAllocated`.

#### New Endpoints:
- `GET /api/admin/profile` — returns admin profile from JWT identity.
- `PUT /api/admin/profile` — updates admin profile name.
- `GET /api/admin/support/metadata` — returns helpdesk action types and target types from JSON.
- `GET /api/admin/staff/by-region/{regionId}` — returns available (unallocated) staff for a region.
- `GET /api/admin/events/by-region/{regionId}` — returns events with venues in the given region.
- `PUT /api/admin/venues/{venueId}` — updates venue name, address, price, region.

### Service Implementation Updates (`AdminService.cs`)

#### GetSupportTicketsAsync
- Now filters by `status` (case-insensitive), `keyword` (searches subject + description), `dateFrom`/`dateTo` (by Created_At).
- Results ordered by `Created_At` descending.

#### GetStaffDirectoryAsync
- Now filters by `regionId` and `isAllocated` status.

#### GetStaffByRegionAsync
- Returns only unallocated staff (`IsAllocated == false`) for the given region.

#### GetEventsByRegionAsync
- Filters events by venue's `Region_Id` matching the parameter.

#### GetAdminProfileAsync / UpdateAdminProfileAsync
- Uses `AdminRepository.GetByAdminIdAsync()` to fetch/admin profile.
- Update saves the name and returns updated profile.

#### GetHelpdeskMetadataAsync
- Reads and deserializes `Event.Business/assets/admin/helpdesk-types.json`.
- Falls back to empty response if file not found.

#### UpdateVenueAsync
- Fetches venue by ID, updates fields, saves and returns refreshed venue with seat tiers.
