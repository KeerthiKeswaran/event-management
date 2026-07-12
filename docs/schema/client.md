# Frontend Changes

## Booking Page (`client/src/app/components/booking/`)

### booking.css
- **Column layout**: Changed `grid-template-columns: 1fr 30%` to `1fr 340px` for a fixed-width right sidebar that prevents the Book Tickets button from being hidden.

### booking.ts
- **proceedToCheckout()**: Added `state: { event }` to `router.navigate()` call so the full event object is passed to the checkout component via history state.

### checkout.ts
- **ngOnInit()**: Now checks `history.state?.event` first before falling back to `mockAllEvents.find()`. This ensures the correct event data is available when navigating from booking.

## Create Event Page (`client/src/app/components/organizer/create-event/`)

### create-event.html
- **Description field**: Replaced `<input type="url">` with `<textarea>` for multi-line text entry.
- **Image upload**: Added `<input type="file">` with `accept="image/jpeg,image/png"` and helper text "Supported formats: JPG, PNG".
- **Staff spinner**: Added `<span class="spinner">` inside a `.spinner-container` div that shows during staff calculation.
- **Ticket pricing section**: For virtual events, shows a `.virtual-pricing-box` with the price fetched from backend; hides ticket tier configuration. For physical/hybrid events, shows tier configuration options.
- **Platform Fees Summary**: Shows activation fee separately for both virtual and physical/hybrid. Virtual shows only activation fee; physical shows venue rental + activation fee + staff cost.

### create-event.ts
- **New fields**: Added `descriptionText` (string), `imageFile` (File | null), `platformSettings` signal.
- **platformSettings**: Fetched on init via `GET /api/Event/platform-settings`.
- **virtualTicketPrice**: Computed from platformSettings (`virtual_Event_Activation_Fee`).
- **activationFee**: Computed based on event type (virtual vs physical/hybrid).
- **totalFees**: Updated to include activation fee + venue rental + staff cost for physical/hybrid; only activation fee for virtual.
- **onImageSelected()**: Captures selected file from file input.
- **onSubmitDetails()**: Now async. First uploads description text via `POST /api/Event/upload-description`, then optionally uploads image via `POST /api/Event/upload-image`, then calls createEvent with the returned URLs.
- **onEventTypeChange()**: For virtual, clears ticketTiers (no tier config needed).

### create-event.css
- Added `.spinner`, `.spinner-container`, `.virtual-pricing-box`, `.loading-text`, `.helper-text` styles.

## Create Event Page (Round 2 Updates)

### create-event.ts
- **Session storage**: Added `saveDraft()` / `loadDraft()` / `clearDraft()` — all form fields saved to `sessionStorage` key `createEventDraft` on every field change. Restored in `ngOnInit()`. Cleared only after `confirmEvent` payment success.
- **Policy loading**: Added `loadPolicy()` calling `GET /api/policies/EventCreation`. Policy modal displays markdown content via `DomSanitizer`. Acceptance tracked via `acceptedPolicy` boolean.
- **Drag-drop image upload**: Added `onDragOver()`, `onDragLeave()`, `onDrop()` handlers for `.drop-zone` div. Preview shown with remove button.

### create-event.html
- **Event type selector**: Changed from 3 radio cards to `<select id="eventType">` dropdown.
- **Staff result box**: Removed green highlight (`#10b981` background), now neutral `.staff-result-box` with key-value rows.
- **Policy checkbox + modal**: Added `(change)="acceptedPolicy = $event.target.checked"` checkbox. Policy modal triggered by "View Policy" link.
- **Drop zone**: Added `.drop-zone` with `(dragover)`, `(dragleave)`, `(drop)` events. Helper text "Supported formats: JPG, PNG".
- **Payload change**: `hasAcceptedPolicy: true` → `acceptedPolicyId: termsId` (string).

### create-event.css
- Added `.drop-zone`, `.policy-modal-overlay`, `.policy-modal-content`, `.staff-result-box` styles.
- Stripe card container: background changed from dark to `var(--bg-canvas)`, text color `#121212`.

## Booking Page (Round 2 Updates)

### booking.ts
- **initializeEvent()**: Replaced `buildMockTiers()` with `this.event.ticketTiers?.map(...)` — only actual tiers from backend shown with real prices.
- **Tier prices**: Now read from `tier.tier_Price` instead of computed from `minPrice * multiplier`.

### booking.html
- **Subtotal**: Moved from left column to right column, above quantity selector. Added `min-height: 38px` container to prevent layout shift. Aligned with tier name via `justify-content: space-between`.
- **Tier list overflow**: Wrapped in scrollable container with `max-height: calc(100vh - 160px)` to keep "Book Tickets" button always visible.

### booking.css
- **`.tier-right-col`**: Added `min-height: 38px` so quantity selector position is fixed regardless of subtotal presence.
- **`.sticky-ticket-panel`**: Added `max-height: calc(100vh - 160px)` with overflow-y auto.
- **`.tier-item-price`**: `font-weight: 800` for bold prices.
- **Subtotal bold**: Wrapped in `<strong>` with `font-weight: 700`.

### checkout.ts
- **Stripe card element**: Changed CardElementOptions to light theme — `style.base.fontSize`, `iconStyle: 'solid'`, placeholders (`1234 5678 9012 3456`, `MM / YY`, `•••`).
- **404 tier name fix**: Replaced hardcoded price lookup (`tierName === 'VIP' ? minPrice * 2.5`) with `found.ticketTiers?.find(t => t.tier_Name === tierName)`.

### checkout.css
- Stripe card container: `background: var(--bg-canvas)` instead of dark, `color: #121212`, added focus-within ring.

## Event Service (`client/src/app/services/event.service.ts`)

Added three new API methods:
- **getPlatformSettings()**: `GET /api/Event/platform-settings` — returns platform fee configuration.
- **uploadDescription(text)**: `POST /api/Event/upload-description { text }` — saves description as .txt file, returns `{ url }`.
- **uploadImage(file)**: `POST /api/Event/upload-image` (multipart/form-data) — uploads image file, returns `{ url }`.

## Admin Page (`client/src/app/components/admin/`)

### auth.service.ts
- Added `adminLogin(adminId, password)` to call `POST /api/auth/admin/login`.
- Stores the admin JWT token in `localStorage` and loads profile state after login.

### admin.service.ts (Round 2 - Refined)
- Updated with new API methods:
  - `getSupportTickets(params)` now supports filters: `status`, `keyword`, `dateFrom`, `dateTo`
  - `getHelpdeskMetadata()` → `GET /api/admin/support/metadata` (fetches action types from JSON)
  - `getStaffDirectory(params)` now supports filters: `regionId`, `isAllocated`
  - `getStaffByRegion(regionId)` → `GET /api/admin/staff/by-region/{regionId}`
  - `getEventsByRegion(regionId)` → `GET /api/admin/events/by-region/{regionId}`
  - `getAdminProfile()` → `GET /api/admin/profile`
  - `updateAdminProfile(payload)` → `PUT /api/admin/profile`
  - `updateVenue(venueId, payload)` → `PUT /api/admin/venues/{venueId}`

### admin.guard.ts (Fixed)
- Now decodes JWT token and checks that the role claim is "admin" instead of just checking for token existence.

### Dashboard Component (Refined)
- Stats now properly map to backend response `{ Summary: { TotalUsers, TotalLiveEvents }, StaffMetrics: { TotalStaff, AllocationPercentage } }`
- Added filter bar: keyword search, event type dropdown, status dropdown, date range, sort
- Pagination for event list
- Azure-inspired compact table UI

### Venues Component (Refined)
- Shows list of all venues in a table on load
- "Register New Venue" button on top right opens a modal popup
- Modal has region dropdown (fetched from API), name, address, hourly price fields
- Success/error messages inline

### Moderation Component (Refined)
- **Reports**: Table layout with event ID, reporter, reason, status, date, actions (Dismiss/Uphold)
- **Uphold Modal**: Reason textarea + Organizer Action dropdown (No Action / Restrict / Deactivate)
- **Staff**: Compact table with Name, Email, Region, Status, Action columns
- **Staff Filters**: Region dropdown + allocation status filter
- **Allocation button**: Greyed out "Allocated" when allocated, black "Allocate" with white text when available
- **Allocation Modal**: Shows events in staff's region as a dropdown for selection
- Dismiss/Uphold refresh the reports list automatically

### Helpdesk Component (Refined)
- Support tickets table with ID, Subject, Status, Date columns
- Filter by keyword and status
- Helpdesk actions and target types fetched from backend JSON via `GET /api/admin/support/metadata`
- Respond section: textarea + send button
- Escalate section: Action dropdown, Target Type dropdown (both resolved from API), Remarks textarea
- Selected ticket row highlighted with left accent border

### Profile Component (Refined)
- Now uses dedicated `GET /api/admin/profile` instead of user profile endpoint
- Shows Admin ID, Email (read-only), Name (editable)
- Save changes calls `PUT /api/admin/profile`
- Password reset link redirects to `/admin/password/reset`
- Sign Out button clears session

### CSS (All Admin Pages)
- Azure-inspired enterprise design: dark sidebar (#1a1a1a), compact tables, consistent 6px border radius
- 60-30-10 color discipline: 60% canvas (white + light gray), 30% secondary (dark sidebar, borders), 10% accent (cardinal red)
- Minimal typography: Inter font, uppercase labels, 13px body, 22px page titles
- No emojis, no decorative elements - clean military-enterprise grade appearance
