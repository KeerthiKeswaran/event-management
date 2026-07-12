# Admin Tabs Operations Module

This module governs all core dashboard capabilities for the Administrator dashboard, excluding support ticket management. It includes stats querying, events listing/filtering, flagged reports handling, venues management, and staff allocation.

## 1. Files & Components Involved

### Controllers
* **AdminController.cs**
  * **Path:** [AdminController.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.API/Controllers/AdminController.cs)
  * **Admin Operations Endpoints (excluding Support):**
    * `GET api/admin/stats` (Get overall platform stats)
    * `GET api/admin/events` (Get filtered, sorted, paginated events)
    * `GET api/admin/reports` (Get flagged event reports)
    * `POST api/admin/reports/{reportId}/dismiss` (Dismiss a flagged event report)
    * `POST api/admin/reports/{reportId}/uphold` (Uphold a report & apply actions to event/organizer)
    * `GET api/admin/regions` (Retrieve all active regions)
    * `GET api/admin/venues` (Retrieve all registered venues)
    * `POST api/admin/venues` (Register a new venue with seat capacities)
    * `GET api/admin/staff` (Get staff directory list)
    * `POST api/admin/events/{eventId}/allocate-staff` (Allocate an available staff member to a physical/hybrid event)

### Contracts & Interfaces
* **IAdminService.cs** -> [IAdminService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IServices/IAdminService.cs)
* **IEventRepository.cs** -> [IEventRepository.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IRepositories/IEventRepository.cs)
* **IVenueRepository.cs** -> [IVenueRepository.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IRepositories/IVenueRepository.cs)
* **IStaffRepository.cs** -> [IStaffRepository.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Contracts/IRepositories/IStaffRepository.cs)

### Services
* **AdminService.cs** -> [AdminService.cs](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/server/Event.Business/Services/AdminService.cs)

---

## 2. Activity & State Flowcharts

### I. Flagged Event Reports Flow (Dismiss / Uphold)
Allows administrators to review complaints against events and dynamically apply sanctions (e.g. warning, restrict, or deactivate) on events/organizers.

```text
                         [ START FLAG REPORT ]
                                   │
                                   ▼
                      [ Select Report ID & Action ]
                                   │
                                   ▼
                            { Action Type? }
                             /            \
                     [Dismiss]            [Uphold]
                       /                        \
                      ▼                          ▼
             [ Fetch Event Report ]      [ Fetch Event Report ]
                      │                          │
                      ▼                          ▼
             [ Set Report: "Dismissed" ] [ Set Report: "Upheld" ]
                      │                  - Apply Organizer action:
                      │                    No Action / Restrict / Deactivate
                      │                  - Send Warning / Block Email
                      │                  - Deactivate Event / Cancel Bookings
                      │                          │
                      ▼                          ▼
             [ Save changes to DB ]      [ Save changes to DB ]
                      │                          │
                      ▼                          ▼
             [ END / Dismissed ]         [ END / Upheld & Action Taken ]
```

---

### II. Register Venue Flow
Registers physical venues with different seating capacities under predefined regions.

```text
                        [ START REGISTER VENUE ]
                                   │
                                   ▼
                    [ Read Region_Id, Name, Address, ]
                    [ HourlyPrice, SeatTiers         ]
                                   │
                                   ▼
                         [ Verify Region Exists ]
                                   │
                                   ▼
                         { Region Exists? }
                          /              \
                 [No]    /                \ [Yes]
                        ▼                  ▼
               (Throw Validation)   [ DB Transaction Start ]
                                           │
                                           ▼
                                    [ Create Venue ]
                                           │
                                           ▼
                                    [ Create SeatCapacities ]
                                    - Save details for each tier
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

---

### III. Allocate Staff to Event Flow
Verifies regional eligibility and checks date schedules to allocate support staff members for active events.

```text
                       [ START STAFF ALLOCATION ]
                                   │
                                   ▼
                     [ Read EventId & EmployeeId ]
                                   │
                                   ▼
                     [ Fetch Event & Staff Details ]
                                   │
                                   ▼
                     { Does Event Require Staff? }
                      /                         \
             [No]    /                           \ [Yes]
                    ▼                             ▼
            (Throw Validation)          { Is Staff in Venue Region? }
                                         /                          \
                                [No]    /                            \ [Yes]
                                       ▼                              ▼
                              (Throw Valid)                  { Staff Available? }
                                                              /                 \
                                                     [No]    /                   \ [Yes]
                                                            ▼                     ▼
                                                   (Throw Valid)       [ DB Transaction Start ]
                                                                                  │
                                                                                  ▼
                                                                        [ Allocate Staff ]
                                                                        - Save allocation row
                                                                                  │
                                                                                  ▼
                                                                        [ Update Event status ]
                                                                        - If all staff filled
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
