Changes in event creation page:
1. The browse anchor link is not opening, the drag and drop ot browse also have an option to remove the added image.

2. The policy must be moved to the end, it must be properly formatted by removing unnecessary things:  update all the policy formattings by removing unnecessary things like policy id, version, only date is enough in all. If the name is wrong change it to "GetMyEvents".


3. No need of description for each event format, just a small width dropdown with Physical, Virtual and Hybrid is enough. Try to align the event type, date time duration in a single line.

4. In case of physical venue choosing, the request platform event staff allocation must call the api endpoint and calculate the cost and display it properly , add a loading spinner until it. If the user changes the venue later, the cost must be re-calculated again. 

5. Remove the entire ticket pricing section in case of virtual event, calculate the cost for the virtual event upfront, physical event upfront properly, move the platform fee summary to the right side as a fixed card with a button "Checkout". And this must raise modal popup for confirmation before proceeding to a timed payment checkout page (implemented in the previous part)

6. Session storage: This is the most important thing, whatever the user is entering storage each and everything locally (add a small icon showing that it is synced, stored) - similar to google sheets saved icon. This session storage must be cleared once the user completed their checkout successfully, if not whether the payment failed or the user cancelled by themselves, this storage must remain the same for the user.

7. UI: Make the UI professional, try to minimize the hight of the session entry, make it industry level standard looking, follow proper principle from docs/design/design-principle.md. No need of huge buttons, texts, divs.

8. All the features must be properly mapped with an api call in the backend, if not create a new api call in the controller and its services.

9. Once done, mark each of the services and compoents with a tick mark beside the requirement sentence one by one, update modifications.md properly with suitable api calls and logics, append client.md and server.md

Strictly no browser preview, only build.



------------


22. Event Creation Page Refactoring (client/src/app/components/organizer/create-event/):

    ✅ 1. Browse link made functional via <label for="imageFile"> wrapping the browse text. Drag-and-drop image removal remains via the Remove button.

    ✅ 2. Policy acceptance moved to the end of the form (inside the sticky right summary card). Policy markdown formatter now strips `version:` and `policy id:` lines but retains date lines. Applied to both create-event.ts and register.ts.

    ✅ 3. Format dropdown reduced to short labels (Physical / Virtual / Hybrid). Format, Date & Time, and Duration aligned in a single row using .form-row-3 grid layout.

    ✅ 4. Staff allocation cost is recalculated on venue change AND date-time change via onVenueChange() and onDateTimeChange(). A loading spinner is shown during API call. Staff cost is included in the right-side fee summary card.

    ✅ 5. Entire Ticket Pricing section (fieldset 03) is conditionally hidden when eventType === 'Virtual'. Platform Fee Summary card moved to sticky right column (.form-right-col → .fixed-summary-card) with a "Checkout" button (Dark Cardinal Red #8A151B). Clicking Checkout triggers the Azure-style review modal before proceeding to timed payment.

    ✅ 6. Session storage draft auto-save implemented in saveDraft(). A Google Sheets-style "Saved to draft" / "Saving..." badge shown near the page title (title-with-sync + sync-status CSS). Draft is cleared on successful payment completion only; preserved on failure or cancellation.

    ✅ 7. Two-column layout (.two-column-layout grid) keeps form cards compact on the left and the fee summary sticky on the right. Form inputs have reduced padding, compact labels, and professional typography following design-principle.md.

    ✅ 8. Backend API calls verified:
        - GET api/Event/venues-with-capacity → loads venues with capacity and hourly price
        - GET api/Event/platform-settings → loads activation fee, virtual ticket price
        - POST api/Event/estimate-staff → called on venue/datetime change for staff cost
        - POST api/Event → submits event listing
        - POST api/payment/create-event-payment-intent → Stripe payment intent for organizer fee
        - DELETE api/Event/{eventId} (revert) → called on canDeactivate guard if user backs out

    selectedVenuePrice computed property updated to support both camelCase (venue_Id, hourly_Price) and PascalCase (Venue_Id, Hourly_Price) backend property names.




