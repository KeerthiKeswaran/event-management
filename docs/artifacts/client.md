# Client-Side Development History: EventSphere Frontend (A-Z)

This document provides a chronological timeline and technical log of all frontend features, layout configurations, state management integrations, and API integration paths built for the EventSphere client application.

---

## Phase 1: Project Scaffolding & Folder Setup
* **Request:** Create a modular, clean Angular workspace directory structure to accommodate state management, controllers, components, models, and helper services.
* **Discussion:** Discussed the use of empty folder structures and git rules. Temporary `.gitkeep` files were placed inside subfolders and subsequently removed to satisfy repository requirements.
* **Files Created:**
  * `client/src/app/components/`
  * `client/src/app/services/`
  * `client/src/app/interceptors/`
  * `client/src/app/guards/`
  * `client/src/app/models/`
  * `client/src/app/store/actions/`
  * `client/src/app/store/reducers/`
  * `client/src/app/store/state/`
  * `client/src/app/store/selectors/`

---

## Phase 2: Navigation Bar & Authentication Guards
* **Request:** Build a minimal-height, premium top navigation bar containing:
  1. The branding logo on the left.
  2. A search bar mapping the user's selected location.
  3. Action controls on the right (Settings, Bookings history, Create Event, and Profile Dropdown).
* **Discussion:** Configured layout behavior based on user sessions: dropdown features are hidden for anonymous guests and replaced with a prominent "Sign In" button, whereas logged-in sessions activate all profile options in a clean, single-line alignment.
* **Files Modified/Created:**
  * `client/src/app/components/home/navbar/navbar.ts`
  * `client/src/app/components/home/navbar/navbar.html`
  * `client/src/app/components/home/navbar/navbar.css`

---

## Phase 3: Homepage, Hero Slider & Localized Mock Data
* **Request:** Implement an interactive homepage featuring a Hero Carousel slider, a popular regions selector, and a scrollable About & FAQ section.
* **Discussion:** Decided to localize the application context to India and Tamil Nadu. Updated default coordinates from San Francisco to Chennai (`REG01`) and Coimbatore, Madurai, and Trichy.
* **Files Modified/Created:**
  * `client/src/app/components/home/home.ts`
  * `client/src/app/components/home/hero-carousel/`
  * `client/src/app/components/home/about-faq/`
  * `client/src/app/services/location-geo.service.ts` (Geolocation syncing)
  * `client/src/app/data/event.mock.ts` (Added localized events like A.R. Rahman Symphony Concert, Tech Expo, Madurai Chithirai Art Festival)

---

## Phase 4: Event Browsing, Searching & Filters
* **Request:** Build a `/browse` page acting as the primary landing page for searches, equipped with:
  1. Category select grids.
  2. Date filters (minimum/maximum event date).
  3. Sorting controls (sort by date ascending/descending, price ascending/descending).
* **Discussion:** Decided to remove standard "View Details" buttons from the cards, making the entire event card a clickable container navigating to the event detail view.
* **Files Modified/Created:**
  * `client/src/app/components/browse-events/browse-events.ts`
  * `client/src/app/components/browse-events/browse-events.html`
  * `client/src/app/components/browse-events/browse-events.css`

---

## Phase 5: Booking Wizard & Stripe Checkout
* **Request:** Build the ticket reservation flow, including:
  1. Quantity selector wizard.
  2. Platform convenience handling fees calculations (₹45 per ticket).
  3. stripe charge simulation.
  4. Success screen redirection.
  5. Back-to-browse revert mechanisms if checkout is interrupted.
* **Discussion:** Created mock endpoints for initiate-booking, convenience fee calculations, and transaction references mapping to prevent local runtime failures.
* **Files Modified/Created:**
  * `client/src/app/components/booking/booking.ts`
  * `client/src/app/components/booking/checkout/`
  * `client/src/app/services/booking.service.ts`

---

## Phase 6: Bookings History Dashboard & Cancellations
* **Request:** Create a user bookings log page showing active and past reservations, including check-in QR codes, virtual joining URLs, and cancellation buttons.
* **Discussion:** Designed a cancellation refund schedule:
  * Cancel > 48 hours prior: **90% refund**.
  * Cancel between 12 and 48 hours prior: **50% refund**.
  * Cancel < 12 hours prior: **Non-Refundable**.
* **Files Modified/Created:**
  * `client/src/app/components/bookings/bookings.ts`
  * `client/src/app/components/bookings/bookings.html`
  * `client/src/app/components/bookings/cancel-booking-modal/`

---

## Phase 7: Profile Edit & Account Deactivation Settings
* **Request:** Build a sidebar account page `/settings` with three views:
  1. **Profile Info:** Renders user information in read-only mode until a pencil Edit button is clicked, turning fields into text inputs and revealing a "Save Changes" action.
  2. **Password Reset:** Changing password prompts a 6-digit OTP verification code, followed by new and confirm password modal entries.
  3. **Close Account:** Prompts reasons survey (with other-specify textbox), confirmation by typing profile name, and an email OTP verification. Upon success, clears all localStorage, dispatches logout state, and redirects with a professional thank you splash.
* **Files Modified/Created:**
  * `client/src/app/components/account-settings/account-settings.ts`
  * `client/src/app/components/account-settings/account-settings.html`
  * `client/src/app/components/account-settings/account-settings.css`

---

## Phase 8: Location Auto-Trigger & Geolocation Permission
* **Request:** When a new user completes registration, automatically trigger the location selection modal and browser location permission prompt on the homepage.
* **Discussion:** Set up flags in localStorage (`'justRegistered'`) to identify new signups. If permission is granted, coordinates are translated to regions; otherwise, the user selects manually from the grid.
* **Files Modified/Created:**
  * `client/src/app/components/register/register.ts`
  * `client/src/app/components/home/home.ts`
  * `client/src/app/components/home/location-modal/`

---

## Phase 9: Registration Consent & Times New Roman PDF Viewer
* **Request:** Add mandatory registration checkboxes for *Terms and Conditions \** and *Data Storage Consent \**, along with an optional *Marketing Consent*. Clicked terms must open a PDF-like visual document container styled in Times New Roman font.
* **Discussion:** Designed a unified backend policy retrieval endpoint. Implemented the layout styled with Times New Roman text (`font-family: 'Times New Roman', Times, serif`) embedded in a dark reader frame overlay in the register modal and the booking cancellation policy viewer.
* **Files Modified/Created:**
  * `client/src/app/data/consent.mock.ts` (Industry legal policy content)
  * `client/src/app/components/register/register.html` (Checkboxes, required validations, PDF modal layout)
  * `client/src/app/components/register/register.css` (PDF dark reader style sheets)
  * `client/src/app/components/bookings/cancellation-policy-doc/cancellation-policy-doc.css` (Font update to Times New Roman)
  * `client/src/app/services/auth.service.ts` (Unified `getConsentDocument('terms' | 'data_consent')` call targeting `api/policies/{type}`)
  * `client/src/app/components/bookings/cancellation-policy-doc/cancellation-policy-doc.ts` (Switched fetch endpoint to `GET /api/policies/cancellation`)

---

## Phase 10: Registration OTP Countdown Timer & Auto-Verification
* **Request:** Modify the OTP delivery and confirmation steps to include:
  1. A 30-second cooldown timer before "Resend" OTP is enabled/visible.
  2. The "OTP Sent!" message moved right next to the OTP field label.
  3. Disabling the OTP field until Send OTP is triggered.
  4. Auto-verification of the OTP code (mock value '123456') as soon as entered, displaying a loader or success/error icon directly inside the input container.
  5. The registration button remains enabled, but validation of inputs (email structure, 10-digit mobile number, verified OTP status, and policy consent) is triggered when clicked.
  6. Clubbing the Terms & Conditions and Data Storage consents into a single checkbox to improve user registration UX.
  7. Renaming site brand references inside mock text to the actual project name "GetMyEvents" and formatting policy layout structures to use plain prose points rather than markdown bullet points.
* **Files Modified/Created:**
  * `client/src/app/components/register/register.ts` (Added signal states, lifecycle timers, onOtpChange logic, validation functions on submission)
  * `client/src/app/components/register/register.html` (Combined checkbox inputs, relocated indicators, styled input wrapper, added verification SVGs)
  * `client/src/app/components/register/register.css` (Increased policy modal width to 720px, added spinner, indicators & inline label layouts)
  * `client/src/app/data/consent.mock.ts` (Renamed "EventSphere" to "GetMyEvents", removed bullet points in favor of paragraphs)
  * `client/src/app/services/auth.service.ts` (Added commented/mocked verifyOtp API client method)

---

## Phase 11: Booking Responses Sanitization & Dynamic Virtual Link Activation
* **Request:** Differentiate initiate booking response from confirm booking response. Prevent returning real virtual URL, password hash, checkin status, and booking status in initial confirmation. Activate virtual URL dynamically at the event timing only when the event starts and before it ends via a new active links endpoint.
* **Discussion:** Updated booking service client models, API method signatures (with mock fallbacks & commented real URLs), and checkout handling to process distinct DTOs. Updated My Bookings component to request active links and conditionally grey-out or enable "Join Meeting" action with proper CSS visual cues.
* **Files Modified/Created:**
  * `client/src/app/models/booking.model.ts` (Added `InitiateBookingResponse`, `ConfirmBookingResponse`, and `ActiveVirtualLinkResponse` interfaces)
  * `client/src/app/services/booking.service.ts` (Updated initiateBooking, confirmBooking methods signatures and added getActiveVirtualLinks method)
  * `client/src/app/components/booking/checkout/checkout.ts` (Correctly mapped the payment confirmation promise to safely reconstruct local BookingModel without compile errors)
  * `client/src/app/components/bookings/bookings.ts` (Modified loadBookings lifecycle to load active links and map/enable URLs dynamically; updated joinMeeting alert messaging)
  * `client/src/app/components/bookings/bookings.html` (Applied CSS conditional disabled styles to virtual meeting links button)
  * `client/src/app/components/bookings/bookings.css` (Added visual styles for grayed-out `.action-btn.join-meeting.disabled-link`)

---

## Phase 12: Backend Assets Integration Alignment
* **Request:** Align user asset paths to use a uniform directory structure.
* **Discussion:** The backend team migrated all user-specific assets (QR codes, support ticket receipts, reports) from `/assets/user/...` to `/assets/users/...`. Because the client application retrieves all files and resource links dynamically from database model properties (such as `Qr_Code_Path`, `ConcernUrl`, and `ReportUrl`), this change resolves without client code modifications, maintaining high resilience.
* **Outcome:** The frontend dynamically fetches and renders the QR code images and ticket JSON metadata from `/assets/users/...` endpoints natively.

---

## Phase 13: Dynamic URL Content Fetching & Pixabay CORS Fixes
* **Request:** Resolve the CORS policy error when loading Pixabay images, fetch the text contents of the event descriptions dynamically from their assets URLs, and correct policy endpoints and fetches for registration consent and bookings cancellation.
* **Discussion:**
  * Bypassed fetching Pixabay images via HttpClient/Ajax (which triggered CORS blocks due to CDN rules) in `PixabayService` and instead stored/served remote URLs directly, allowing the browser to render them via `<img>` tags natively.
  * Implemented an automatic background fetch in `EventService.mapEvent` that detects `/assets/...` URLs in the event `description_Url` field, downloads the text content via `HttpClient`, and updates the field reactively to display the description instead of the URL string.
  * Corrected registration consent (`register.ts`) and cancellation policy (`cancellation-policy-doc.ts`) components to map types (`'terms'` and `'data_consent'` map to `'General'`), fetch metadata, download MD files via HTTP, parse simple markdown tags, and bind the parsed HTML to the UI dynamically via `[innerHTML]`.
* **Files Modified:**
  * `client/src/app/services/pixabay.service.ts` (Modified search region caching to store/return direct URLs)
  * `client/src/app/services/event.service.ts` (Added background description URL content retrieval)
  * `client/src/app/components/register/register.ts` (Mapped policy types, injected HttpClient, and fetched/formatted file contents)
  * `client/src/app/components/bookings/cancellation-policy-doc/cancellation-policy-doc.ts` (Fetched and formatted C10001.md policy contents dynamically)
  * `client/src/app/components/bookings/cancellation-policy-doc/cancellation-policy-doc.html` (Added dynamic [innerHTML] binding and spinner template)

---

## Phase 14: Organizer Dashboard & Upfront Payment Flow
* **Request:** Build a comprehensive `/myevents` organizer portal featuring:
  1. A main **Dashboard View** showing total event count, tickets sold, net earnings, and upcoming event listings preview.
  2. An **All Hosted Events Directory** listing all organizer events with interactive status tabs (All, Live, Pending, Completed, Cancelled) and keyword searches.
  3. A **Create Event Wizard** collecting event details, dynamic venue pricing summaries, automated platform staff estimation calculators, ticket tier prices/capacities configurations, and Stripe checkout flow.
  4. Interrupted payment reversion mechanisms to cancel pending listings.
  5. Rerouting navbar triggers to direct users cleanly to these new interfaces.
* **Discussion:** Set up robust client forms, computed fee variables, loading animations, error banners, and modal confirmations to ensure premium responsive workflows.
* **Files Modified/Created:**
  * `client/src/app/components/organizer/dashboard/` (Created component TS, HTML, CSS files)
  * `client/src/app/components/organizer/events-list/` (Created directory TS, HTML, CSS files)
  * `client/src/app/components/organizer/create-event/` (Created wizard TS, HTML, CSS files)
  * `client/src/app/app.routes.ts` (Integrated new route mappings)
  * `client/src/app/components/home/navbar/navbar.ts` (Updated menu navigation bindings)
  * `client/src/app/services/event.service.ts` (Appended organizer API methods)

---

## Phase 15: Stripe Elements Integration
* **Request:** Integrate custom Stripe Elements (Option A - Embedded inputs) in place of the raw dummy payment cards text boxes inside bookings checkout and event creation upfront deposit forms.
* **Discussion:**
  - Configured global Stripe publishable token initialization provider `provideNgxStripe` in application configuration metadata.
  - Replaced manual input forms in bookings checkout template and create event wizard template with `<ngx-stripe-card>`.
  - Wired submit events to request tokenization from Stripe servers, sending token IDs (`tok_xxxx`) to C# controllers to confirm bookings.
* **Files Modified/Created:**
  - `client/src/app/app.config.ts` (Configured ngx-stripe provider)
  - `client/src/app/components/booking/checkout/checkout.ts` (Embedded Stripe Elements logic)
  - `client/src/app/components/booking/checkout/checkout.html` (Replaced manual input elements with ngx-stripe-card)
  - `client/src/app/components/organizer/create-event/create-event.ts` (Integrated token validation trigger)
  - `client/src/app/components/organizer/create-event/create-event.html` (Replaced inputs with ngx-stripe-card)

---

## Phase 16: Organizer Sidebar Slider Navigation & INR Currency Localization
* **Request:** Convert organizer portal layout to use a premium left sidebar slider navigation containing "Dashboard" and "Events" links, and localize the portal to display all currency amounts in INR (₹).
* **Discussion:**
  - Designed a side-by-side flex layout with a fixed, sticky white left navigation panel containing clean vector icons for Dashboard and Events views.
  - Rewrote the CSS stylesheets of the organizer Dashboard, Events Directory, and Create Event Wizard to adjust layout wrapping, margins, and sticky positions below the global top navbar.
  - Localized all metrics, summaries, pricing tiers configurations, and checkout badges to display currency in INR (`₹`) and labels in `INR`.
  - Replaced the input number `ageRestriction` in the wizard forms with a validated drop-down select for `AgeCategory` matching the `"ALL"`, `"KID"`, or `"ADL"` backend model validation rules.
* **Files Modified:**
  - `client/src/app/components/organizer/dashboard/dashboard.html` / `dashboard.css` / `dashboard.ts`
  - `client/src/app/components/organizer/events-list/events-list.html` / `events-list.css` / `events-list.ts`
  - `client/src/app/components/organizer/create-event/create-event.html` / `create-event.css` / `create-event.ts`

---

## Phase 17: Dynamic Venue Tiers, Switch Toggle Staff & simplified Back Button
* **Request:** Convert staff requirements option to a slider switch toggle disabled if no venue is selected, retrieve dynamic ticket tiers and category data from backend, retrieve age limits dynamically, and simplify the back-to-dashboard button design.
* **Discussion:**
  - Redesigned the staff requirements checkbox into a sleek switch toggle slider with explicit disabled visual state when no venue is assigned.
  - Linked the format radios and physical venue dropdowns to set up dynamic ticket tiers and capacities sourced directly from the chosen venue's metadata, making ticket capacity read-only for physical locations.
  - Fetched the age limit options dynamically via the new `GET /api/Event/age-categories` endpoint with error fallbacks.
  - Simplified the back button layout to a clean standard border layout button.
* **Files Modified:**
  - `client/src/app/services/event.service.ts` (Added getAgeCategories method)
  - `client/src/app/components/organizer/create-event/create-event.html` (Added switch toggle, simple back button, dynamic selectors, read-only capacities)
  - `client/src/app/components/organizer/create-event/create-event.css` (Added switch CSS, warning text helpers, simple button classes)
  - `client/src/app/components/organizer/create-event/create-event.ts` (Integrated state change events, category fallbacks, dynamic mapping logic)

---

## Phase 18: Attendee Bookings Details & Relative Media Path Resolvers
* **Request:** Modify attendee bookings calls to return actual amount paid. Retrieve event images and entry QR codes dynamically from static asset links, resolving their absolute URLs. Remove deprecated local base64 QR generation fields.
* **Discussion:**
  - Removed `qr_Code_Data` string from `BookingModel` and modified bookings view template and checkout confirmation dialog to display clean styled content layout without displaying internal raw hash strings.
  - Added base-server URL checks (`http://localhost:5106`) to resolve any relative `/assets/...` URLs in both dashboard and checkout flows, ensuring images load correctly.
  - Linked `amount_paid` field to show the exact purchase amount paid by the attendee.
* **Files Modified/Created:**
  - `client/src/app/models/booking.model.ts` (Updated models schema, removed `qr_Code_Data`, added optional `event_Image_Url` to confirm response)
  - `client/src/app/components/bookings/bookings.ts` (Mapped relative QR/image paths to absolute C# backend addresses)
  - `client/src/app/components/bookings/bookings.html` (Removed raw QR text hash from modal)
  - `client/src/app/components/booking/checkout/checkout.ts` (Resolved image and QR paths inside confirmation callbacks)
  - `client/src/app/components/booking/checkout/checkout.html` (Removed raw hash text from success popup)
  - `client/src/app/data/booking.mock.ts` (Replaced `qr_Code_Data` with `qr_Code_Path` mock references to align with strict interface types)

---

## Phase 19: Policies Integration & Case-insensitive Path Resolution
* **Request:** Fix the 404 Not Found error when calling cancellation policy.
* **Discussion:**
  - Standardized policy retrieval for `/api/policies/cancellation` and related policy types.
  - Aligned client components to fetch and parse markdown content of the policy using the updated absolute asset paths.
* **Files Modified:**
  - `client/src/app/components/bookings/cancellation-policy-doc/cancellation-policy-doc.ts` (Ensured retrieval handles absolute URL mapping)

---

## Phase 20: Ticket Subtotals Stability, Image Paths Resolution, and Bookings UI Tweaks
* **Request:** Align subtotal to the top of card, stabilize ticket qty selector button spacing, resolve relative event image URLs, improve bookings list badge layout, and update CTA checkout button style.
* **Discussion:**
  - Changed ticket subtotal rendering in `booking.html` to always remain in the DOM via `[style.visibility]`, ensuring layout stability.
  - Added `resolveImageUrl` helper inside `event.service.ts`, `bookings.ts`, and `checkout.ts` to correctly handle both relative and absolute image and QR code paths.
  - Relocated event type badges under the event title inside the details list on the Bookings page.
  - Updated confirmed booking status color to a professional steel blue and adjusted checkout confirm button to a dark cardinal red with reduced width.
* **Files Modified:**
  - `client/src/app/components/booking/booking.html` / `.css` (Layout stability, subtotal styling)
  - `client/src/app/services/event.service.ts` (Added URL resolver and mapped event images)
  - `client/src/app/components/bookings/bookings.ts` / `.html` / `.css` (Updated badge layout, resolved booking image paths, set status colors)
  - `client/src/app/components/booking/checkout/checkout.ts` / `.css` (Resolved paths on success, updated checkout button styling)
  - `client/src/app/pipes/resolve-description.pipe.ts` (Expanded assets folder path checks)

---

## Phase 21: Azure-style Review Modals & Navigation Guards for Pending Reservations
* **Request:** Add confirmation modals for booking and event creation showing details as review (Azure-like UI), transition to payment checkout smoothly, and integrate automatic revert API calls when navigating away before payment completion.
* **Discussion:**
  - Designed and built Azure-style Review Modals for both ticket booking checkout and event creation steps using clean, structured tables, solid headers, and clear CTA actions.
  - Split the payment process: booking initiation (POST `api/booking`) and event initiation (POST `api/event`) are triggered upon confirming the review modal, storing the pending IDs.
  - Implemented a modular Angular functional router `canDeactivateGuard` to capture in-app navigation (such as clicking the back link, browser back actions, or keyboard shortcuts).
  - Configured the guard to invoke the respective revert APIs (`revertBooking` / `revertEvent`) in the background before allowing the route transition to complete.
* **Files Created/Modified:**
  - `client/src/app/guards/can-deactivate.guard.ts` (Created functional guard interface)
  - `client/src/app/app.routes.ts` (Registered guard on checkout and create-event routes)
  - `client/src/app/components/booking/checkout/checkout.ts` / `.html` / `.css` (Integrated review modal, deferred booking initiation, applied deactivation guard)
  - `client/src/app/components/organizer/create-event/create-event.ts` / `.html` / `.css` (Integrated review modal, deferred event listing creation, applied deactivation guard)



## Phase 22: Finance Module & Administrative Pages
* **Request:** Create a secure `/finance` portal matching the Admin's military enterprise UI design.
* **Discussion:**
  - Implemented `FinanceService` fetching from `/api/finance` and `/api/auth/finance` endpoints.
  - Added a two-step `/finance/login` flow requiring an `adminId` and OTP verification.
  - Built a comprehensive transactions dashboard, an escalations modal to map `FUL`, `DYN`, `REM`, `NOR` refund types, and a standard profile settings view.
* **Files Modified/Created:**
  - `client/src/app/services/finance.service.ts` (API methods)
  - `client/src/app/guards/finance.guard.ts` (Restricts unauthorized access)
  - `client/src/app/components/finance/login/...` (Login forms and logic)
  - `client/src/app/components/finance/dashboard/...` (Stats and transaction overview)
  - `client/src/app/components/finance/transactions/...` (Filtered transactions)
  - `client/src/app/components/finance/escalations/...` (Admin action approvals)

---

## Phase 23: View All Cities Expansion Behavior
* **Request:** Redesign the city modal expansion interaction.
* **Discussion:** Removed the secondary expandable container. Clicking "View All Cities" now extends the existing list of city logos naturally. The state automatically returns to collapsed when reopened.
* **Files Modified/Created:**
  - `client/src/app/components/home/location-modal/location-modal.ts`
  - `client/src/app/components/home/location-modal/location-modal.html`
  - `client/src/app/components/home/location-modal/location-modal.css`

---

## Phase 24: Event Overview Map and Organizer Contact Experience
* **Request:** Enhance the Event Overview page map and organizer contact interactions.
* **Discussion:** Updated map embed to redirect to Google Maps with prefilled coordinates. Redesigned "Contact Organizer" to open a pre-populated `mailto:` link.
* **Files Modified/Created:**
  - `client/src/app/components/event-details/event-details.ts`
  - `client/src/app/components/event-details/event-details.html`

---

## Phase 25: Browse Events Retrieval, Pagination and Search Consistency
* **Request:** Synchronize search logic and pagination for events.
* **Discussion:** Corrected frontend query parameters and state synchronization to guarantee Browse Events pagination matches recommendation counts without clipping results.
* **Files Modified/Created:**
  - `client/src/app/components/browse-events/browse-events.ts`

---

## Phase 26: Browse Events Pagination User Experience
* **Request:** Add automatic scroll-to-top after page transitions.
* **Discussion:** Integrated a viewport scrolling mechanism `window.scrollTo` immediately following a successful pagination API response.
* **Files Modified/Created:**
  - `client/src/app/components/browse-events/browse-events.ts`

---

## Phase 27: Event Type Filtering (Physical, Virtual and Hybrid)
* **Request:** Correct event type filters to accurately reflect selected combinations.
* **Discussion:** Fixed frontend payload structures mapping Virtual and Hybrid checkbox states down to API requests.
* **Files Modified/Created:**
  - `client/src/app/components/browse-events/browse-events.ts`

---

## Phase 28: Dynamic Range Slider Price Filter
* **Request:** Replace price input with a dynamic range slider calculating max prices.
* **Discussion:** Implemented an HTML range slider that automatically calculates `max` based on the highest ticket price in the returned event list.
* **Files Modified/Created:**
  - `client/src/app/components/browse-events/browse-events.ts`
  - `client/src/app/components/browse-events/browse-events.html`
  - `client/src/app/components/browse-events/browse-events.css`

---

## Phase 29: Browse Events Sorting Functionality
* **Request:** Correct event sorting logic.
* **Discussion:** Repaired frontend state management that prevented sort criteria (Price, Date) from correctly appending to API requests.
* **Files Modified/Created:**
  - `client/src/app/components/browse-events/browse-events.ts`

---

## Phase 30: Drag-and-Drop Image Upload with WebP Conversion
* **Request:** Add drag-and-drop support with WebP format conversion.
* **Discussion:** Built a comprehensive drag-and-drop file upload zone. Intercepts files, validates formats, and dynamically converts images via Canvas API to WebP before transmission.
* **Files Modified/Created:**
  - `client/src/app/components/organizer/create-event/create-event.ts`
  - `client/src/app/components/organizer/create-event/create-event.html`
  - `client/src/app/components/organizer/create-event/create-event.css`

---

## Phase 31: Venue Selection Dropdown with Intelligent Search
* **Request:** Make venue dropdown searchable by text and capacity.
* **Discussion:** Replaced the native `<select>` element with a custom searchable dropdown component rendering cards for each venue.
* **Files Modified/Created:**
  - `client/src/app/components/organizer/create-event/create-event.ts`
  - `client/src/app/components/organizer/create-event/create-event.html`
  - `client/src/app/components/organizer/create-event/create-event.css`

---

## Phase 32: Staff Allocation Refresh Experience
* **Request:** Add skeleton loaders and spinner to staff allocation.
* **Discussion:** Introduced a boolean loading state binding skeleton CSS classes while the backend recalculates required staff for a venue.
* **Files Modified/Created:**
  - `client/src/app/components/organizer/create-event/create-event.ts`
  - `client/src/app/components/organizer/create-event/create-event.html`
  - `client/src/app/components/organizer/create-event/create-event.css`

---

## Phase 33: Ticket Tier Capacity Validation
* **Request:** Implement real-time capacity validations against the venue limit.
* **Discussion:** Built real-time form `valueChanges` subscriptions that aggregate total ticket allocations and disable checkout if the sum exceeds the venue's max capacity.
* **Files Modified/Created:**
  - `client/src/app/components/organizer/create-event/create-event.ts`
  - `client/src/app/components/organizer/create-event/create-event.html`

---

## Phase 34: Stripe Payment Cancellation and Event Rollback
* **Request:** Handle Stripe Payment Cancellation and Event Rollback.
* **Discussion:** Leveraged Angular routing guards to detect browser back button navigation. Invokes `api/Event/{eventId}/revert` transparently if checkout isn't completed.
* **Files Modified/Created:**
  - `client/src/app/guards/can-deactivate.guard.ts`
  - `client/src/app/components/organizer/create-event/create-event.ts`

---

## Phase 35: Stripe Payment Amount Mismatch Fix
* **Request:** Synchronize checkout pricing calculation.
* **Discussion:** Removed redundant client-side pricing calculators. The client now strictly binds total amounts returned by the initiate API.
* **Files Modified/Created:**
  - `client/src/app/components/booking/checkout/checkout.ts`
  - `client/src/app/components/organizer/create-event/create-event.ts`

---

## Phase 36: Organizer Dashboard Statistics Binding
* **Request:** Correctly bind tickets sold and earnings from dashboard API.
* **Discussion:** Fixed property mapping to accurately consume `ticketsSold` and `netEarnings` from the backend payload.
* **Files Modified/Created:**
  - `client/src/app/components/organizer/dashboard/dashboard.ts`
  - `client/src/app/components/organizer/dashboard/dashboard.html`

---

## Phase 37: Organizer Event Details Modal Redesign
* **Request:** Refactor the modal for better layout and constrained title editing.
* **Discussion:** Redesigned the component to show virtual URLs securely (with masking toggle). Enabled `PUT` calls to update details while restricting title edit counts.
* **Files Modified/Created:**
  - `client/src/app/components/organizer/events-list/events-list.ts`
  - `client/src/app/components/organizer/events-list/events-list.html`
  - `client/src/app/components/organizer/events-list/events-list.css`

---

## Phase 38: Booking Cancellation Animation
* **Request:** Enhance booking cancellation opacity fade and animation overlays.
* **Discussion:** Replaced abrupt DOM removal with CSS transition fades and Lottie/CSS animated overlays rendering on top of the original booking card.
* **Files Modified/Created:**
  - `client/src/app/components/bookings/bookings.ts`
  - `client/src/app/components/bookings/bookings.html`
  - `client/src/app/components/bookings/bookings.css`

---

## Phase 39: Secure User Profile Update with Email Verification
* **Request:** Handle asynchronous email uniqueness and real-time validation.
* **Discussion:** Attached `valueChanges` debounce listeners to the profile form. Hits `api/auth/user/check-email` before enabling OTP resend flows.
* **Files Modified/Created:**
  - `client/src/app/components/account-settings/account-settings.ts`
  - `client/src/app/components/account-settings/account-settings.html`
  - `client/src/app/components/account-settings/account-settings.css`

---

## Phase 40: Support Ticket Entity Selection Workflow
* **Request:** Introduce a modal to select events/bookings when creating tickets.
* **Discussion:** Built a tabbed modal (Events vs. Bookings) presenting cards for the user's historical records. Automatically binds the selected `TargetType` and `TicketId` (or event `TargetId`).
* **Files Modified/Created:**
  - `client/src/app/components/support/support.ts`
  - `client/src/app/components/support/support.html`
  - `client/src/app/components/support/support.css`

---

## Phase 41: Persistent Backend Integration for Support Tickets
* **Request:** Remove local storage dependency and integrate backend endpoints.
* **Discussion:** Completely ripped out the `localStorage` ticket queue. Re-wired the UI to fetch real tickets via `GET /api/Support/tickets` and submit via `POST`.
* **Files Modified/Created:**
  - `client/src/app/components/support/support.ts`


---

## Phase 42: Stripe Embedded Checkout & 5-Minute Payment Timeout
* **Request:** Implement a Secure Stripe Checkout Timeout and Automatic Transaction Rollback.
* **Discussion:** Migrated away from Stripe's hosted redirect checkout to an embedded checkout rendered inside the platform. A dedicated `/stripe-checkout` route was created as a standalone component that receives the `clientSecret` and session `createdAtUTC` timestamp via query parameters. A live countdown timer is computed from the original session creation time, ensuring it stays accurate even after page refreshes. When the timer reaches zero, the embedded checkout form is destroyed immediately, the appropriate rollback API is called, and the user sees a professional payment cancellation screen. The timer uses a `setInterval` tick that recalculates remaining seconds each second by comparing `Date.now()` against the backend-provided timestamp — no frontend drift.
* **Files Modified/Created:**
  - `client/src/app/components/booking/stripe-checkout/stripe-checkout.ts` *(created)*
  - `client/src/app/components/booking/stripe-checkout/stripe-checkout.html` *(created)*
  - `client/src/app/components/booking/stripe-checkout/stripe-checkout.css` *(created)*
  - `client/src/app/app.routes.ts` — Added lazy-loaded `/stripe-checkout` route
  - `client/src/app/components/booking/booking.ts` — Replaced `window.location.href = sessionUrl` with `router.navigate(['/stripe-checkout'], { queryParams })`
  - `client/src/app/components/organizer/create-event/create-event.ts` — Same redirect replacement for event creation payment
  - `client/src/app/services/booking.service.ts` — Updated `createCheckoutSession` return type to `{ sessionId, clientSecret, createdAtUTC }`
  - `client/src/app/services/event.service.ts` — Updated `createCheckoutSession` return type to `{ sessionId, clientSecret, createdAtUTC }`

---

## Phase 43: Admin Support Ticket Management Redesign & Escalation Tracking
* **Request:** Redesign the Admin Support Ticket management module and implement live escalation status tracking.
* **Discussion:** Reviewed the frontend integration for support tickets. Resolved missing data bindings that caused empty fields. Converted the static Escalate button into a dynamic tracking board. Implemented a robust frontend fetching strategy that continuously reflects the true status of escalations.
* **Files Modified/Created:**
  - `client/src/app/components/admin/helpdesk/helpdesk.ts`
  - `client/src/app/components/admin/helpdesk/helpdesk.html`
  - `client/src/app/components/admin/helpdesk/helpdesk.css`

---

## Phase 44: Staff Management, Pagination, and Search
* **Request:** Enhance the Staff Allocation interface with robust search, backend-driven pagination, and sorting capabilities.
* **Discussion:** Replaced client-side sorting and pagination with definitive backend-driven queries (`api/admin/staff`). Added a search functionality allowing lookup by Staff ID, Name, or Email. Replaced raw Region IDs with readable Region Names. Ensure duplicate records are prevented during transitions.
* **Files Modified/Created:**
  - `client/src/app/components/admin/staff/staff.ts`
  - `client/src/app/components/admin/staff/staff.html`

---

## Phase 45: Admin Navigation Sidebar Redesign
* **Request:** Transition the Admin portal to a collapsible left navigation sidebar with a dedicated color scheme.
* **Discussion:** Abstracted the administrative navigation into a standalone `<app-admin-sidebar>` component leveraging signals for a collapsible state (persisted in localStorage). Transitioned the active styling to a soft Cardinal Red and simplified the Admin Footer into a streamlined horizontal layout.
* **Files Modified/Created:**
  - `client/src/app/components/admin/sidebar/` (created ts, html, css)
  - `client/src/app/components/admin/dashboard/` (integrated sidebar)
  - `client/src/app/components/admin/moderation/` (integrated sidebar)
  - `client/src/app/components/admin/venues/` (integrated sidebar)
  - `client/src/app/components/admin/helpdesk/` (integrated sidebar)
  - `client/src/app/components/home/footer/footer.html` & `.css` (adjusted layout for admin context)

---

## Phase 46: Secure Admin Profile Management & OTP Password Reset
* **Request:** Make the admin profile view read-only except for passwords, and rebuild the OTP workflow to hit backend targets specific to administrators.
* **Discussion:** Modified the `AccountSettingsComponent`. Swapped the generic user profile extraction for a direct call to `AdminService.getAdminProfile()`. Enforced view-only mode for admins on the Name/Email fields. Rewired the OTP requests in the password change workflow to utilize `sendAdminOtp` and `resetAdminPassword`, mapping correctly to the backend administrative authentication endpoints.
* **Files Modified/Created:**
  - `client/src/app/components/account-settings/account-settings.ts`
  - `client/src/app/components/account-settings/account-settings.html`

---

## Phase 47: Finance Dashboard Stats Update
* **Request:** Add "Total Intake" metric to the Finance Dashboard and ensure both Total Intake and Total Revenue are properly fetched from backend and formatted in INR (₹).
* **Discussion:** Updated `dashboard.ts` and `dashboard.html` to integrate with the new `dashboard-stats` API. Removed mock logic and updated bindings to use the returned `TotalRevenue` and `TotalIntake` values correctly.
* **Files Modified:**
  - `client/src/app/services/finance.service.ts`
  - `client/src/app/components/finance/dashboard/dashboard.ts`
  - `client/src/app/components/finance/dashboard/dashboard.html`

---

## Phase 48: Finance Organizer Payout Page
* **Request:** Add a dedicated "Organizer Payout" page to the Finance module, accessible from the sidebar below Transactions. The page should display event-level payout records with columns: Event ID, Organizer ID, Organizer Email, Amount (net of commission), Date, Time, and Status (Upcoming/Completed). No filters — only a sort option on the Status column.
* **Discussion:** 
  - Created a standalone `PayoutsComponent` with a dedicated CSS file (`payouts.css`).
  - The status sort is handled **client-side** using Angular's `computed()` signal to re-sort the current page's data immediately without re-fetching. Date-based sorting triggers a new API call for proper server-side ordering.
  - Three sort controls are displayed in the panel header: "Upcoming First", "Completed First", and "Latest First". The active control is highlighted in cardinal red.
  - Clicking the Status column header also toggles the sort direction.
  - Amount and date column headers are sortable with directional arrows.
  - Status badges use "Upcoming" → blue (`#1565C0`) and "Completed" → green (`#2E7D32`).
  - The `getPayouts()` method in `FinanceService` passes `sortBy`, `page`, and `size` to the backend.
  - Added route `/finance/payouts` in `app.routes.ts`.
  - Registered `PayoutsComponent` in all Finance module sidebars (Dashboard, Transactions, Escalations).
* **Files Created:**
  - `client/src/app/components/finance/payouts/payouts.ts`
  - `client/src/app/components/finance/payouts/payouts.html`
  - `client/src/app/components/finance/payouts/payouts.css`
* **Files Modified:**
  - `client/src/app/app.routes.ts`
  - `client/src/app/services/finance.service.ts`

---

## Phase 49: Finance Escalations Page — Full Rebuild
* **Request:** The Escalations page must show a rich table with Ticket ID, Attendee Email, Subject, Escalation Type, Target Type, Created At, and Status columns. Clicking a row opens a modal with full action details, a Related Entity popover, and Approve/Decline buttons.
* **Discussion:**
  - **Table**: Clickable rows highlight on hover. Columns are sortable by Escalation Type, Target Type, and Created At. Default sort is Created At descending.
  - **Modal**: Shows Action ID, Admin ID, Ticket ID, Escalation/Target type badges, Created At, and Target Entity ID (as a clickable anchor). Clicking the Target ID fetches and shows a popover with event details (title, status, organizer, date).
  - **Approve Flow**: Clicking "Approve" slides down an inline form with a Refund Type selector (FUL/DYN/REM/NOR) with contextual hints per selection, an optional message field, and a "Proceed" button. Calls `POST /api/finance/actions/{id}/approve`.
  - **Success Animation**: After approval, a green animated SVG checkmark is shown with the text "Action #{id} Approved". The modal auto-closes after 2.5 seconds.
  - **Decline Flow**: Clicking "Decline" slides down an inline form requiring a reason field. Calls `POST /api/finance/actions/{id}/decline`. Closes after 1.5 seconds on success.
  - Status badges: `Pending` → amber, `Processing` → blue, `Processed/Completed` → green, `Declined/Failed` → red.
  - Approve/Decline buttons only appear when status is `Pending`.
* **Files Created:**
  - `client/src/app/components/finance/escalations/escalations.css`
* **Files Modified:**
  - `client/src/app/components/finance/escalations/escalations.ts`
  - `client/src/app/components/finance/escalations/escalations.html`

---

## Phase 50: Booking Card — Report-Upheld Cancellation UX
* **Request:** When an admin upholds a report against an event and the event is cancelled by the platform:
  1. The event must not appear in any discovery/browse pages for attendees.
  2. Booking cards for that event must display a clear cancellation notice banner.
  3. The QR and Virtual Meeting buttons must be greyed out with appropriate popup messages when clicked.
* **Discussion:**
  - The backend `SearchEventsAsync` already filters by `Status == "Live"` — so cancelled events are automatically excluded from all public discovery endpoints (`/api/event`, `/api/event/trending`, `/api/event/recommended`). No additional frontend filter is needed.
  - A new `.event-cancelled-banner` component (inline HTML + CSS) was added to each booking card. It is only rendered when `booking.event_Status === 'Cancelled' && booking.booking_Status === 'Cancelled'`. It includes an info icon and the message: *"This event was cancelled by the platform. A full refund has been processed..."*
  - The **"View Ticket (QR)"** button now has `[class.disabled-link]="booking.event_Status === 'Cancelled'"`. When clicked in cancelled state, it fires `showCancelledAccessMsg(id, 'qr')` which displays a floating tooltip: *"Ticket access is unavailable — this event has been cancelled."*
  - The **"Join Meeting"** button similarly becomes disabled with `[class.disabled-link]` and shows: *"Virtual meeting link is unavailable — this event has been cancelled."*
  - Two new signals (`cancelledAccessMsgId`, `cancelledAccessMsgType`) and one new method (`showCancelledAccessMsg`) were added to the `BookingsComponent`.
  - All popup messages auto-dismiss after 3.5 seconds.
* **Files Modified:**
  - `client/src/app/components/bookings/bookings.ts`
  - `client/src/app/components/bookings/bookings.html`
  - `client/src/app/components/bookings/bookings.css`


---

## Phase 51: Helpdesk, Finance Filters, and Moderation UI
* **Request:** Prevent redundant clicking in Moderation UI, hide response functionality for Admin-created helpdesk tickets, fix auto-close in venue registration, and add a status filter for finance escalations.
* **Discussion:**
  - Added loading flags (`isUpholding`, `isDismissing`) to moderation action buttons along with CSS spinners, preventing rapid multi-clicks. Synced success text with checkmark animation delay.
  - Added `cdr.detectChanges()` to the Venue register modal closing sequence to force Angular context synchronization after RxJS subscriptions finish.
  - Added logic in Helpdesk to read `isRaisedByAdmin` boolean from ticket models and conditionally hide the textarea and "Send Response" action, preventing admins from responding to internal tickets.
  - Inserted a `select` status filter in `finance/escalations/escalations.html` wrapping the client-side sorting function to filter by "Pending", "Processed", or "Declined".
* **Files Modified:**
  - `client/src/app/components/admin/moderation/moderation.ts`, `moderation.html`, `moderation.css`
  - `client/src/app/components/admin/venues/venues.ts`
  - `client/src/app/components/admin/helpdesk/helpdesk.html`, `helpdesk.ts`
  - `client/src/app/components/finance/escalations/escalations.html`, `escalations.ts`
