### Modifications and Debugs;
1. Change the broken policy viewer display in the registration page (it must be like a pdf viewer ui).✅
2. Send OTP must not immdiately trigger resend (allocate time).✅
3. OTP sent message must be moved beside the Email Verification OTP input label (the input box must only enabled after that).✅
4. Add a common mock otp for now, verify it, automatically verified as soon as entered (add commented api call). The result of verficiation must be displayed as icon inside the input box itself, only after sucessful verification the Regiteration button must be enabled.✅
5. Try clubbing both Data Concent and terms and condition (discuss and brainstorm).✅
6. Implement pending services in the server, update db (know reason why still we have the concent attributes and discuss about removing it from user).✅

----------------------

7. Continue: Add proper test cases and perform dotnet build (previous reponse got terminated due to token exhaustion), add event completion template, trigger feedback email to attendee.✅

8. Add another background service for releasing payout to the organizer for the dismissed reports.✅

9. Review and perform DB Seed.✅

10. Uncomment out api calls one by one and test with servers.✅

--------------------------------

11. OTP-Verification Failed, Check all the existing api calls once. ✅

12. Look into filter part. Try integrating stripe UI for payment pages. ✅

13. Add Organizer Page. ✅

14. Uncomment all api calls. ✅


15. Modify the booking payment and event creation payment, first they must confirm with a modal popup showing the ticket details of booking, event details for event creation as review (azure like ui) and then proceed ahead with payment checkout page with proper timing, integrate the revert api calls which will be called on modal confirmation when the user tried to move to the previous page either with back button or by using keyboard shortcut (simply when the page tries navigates to some other page) ✅

    => In case of booking and event creation, the confirmation modal UI is not subtle like azure ui confirmation with standard design principles, also the page it redirected already in the backend, also in the booking confirmation page, if the user refresh the page, it is again showing the confirmation modal popup! It must refresh the user to the bookings page instead or try to refresh them in the same page.

    => The booking confirmation modal must show the necessary amounts and prices.

    => Make the checkout page proper, similar to the ticket booking checkout page.

    => Confirmation when the user gives back on the payment page.


16. Modify event creation page (virtual event fee calculated wrongly, dropdown of event type is not perfect): event-creation.md ✅


---------------------------------

17. my Bookings page: ✅ Test
    1. align the type of the event and ui properly place the event type without any extra addons along with the event details as regular texts. Make the confirmation button simple, dark cardinal red color without any glow in minimal width complying with the `docs/design/design-principles.md`, add booking cancellation once done animation, this must be a contrast one to the ticket booking animation.
    2. calculate refund properly with an api call, currently showing as non-refundable by default.
    3. Cancellation policy must be properly formatted.
    4. Ticket booking: Add GST for booking payment 18% (Or whatever is standard), update platform settingsdb with that GST%, use the data from there.
    5. Event Dashboard: Add a modal popup for getting event details by clicking on the event in the dashboard or event page, the status must show only the count of live events and completed events.

18. Filters: ✅ Test 
Browsing Page: Remove the apply button, don't wait for apply button to trigger.
Organizer Dashboard and Events: Add sorting, remove the search bar, add necessary filers like an enterprice level application.
The event type is not properly shown on the event cards and event details, by defaul all events are physical.


19. Update the user profile edit height (by default a space has been allocated for Save button, no need of it, only add space if the button is enabled ), update all the policy formattings by removing unnecessary things like policy id, version, only date is enough in all. If the name is wrong change it to "GetMyEvents". ✅ Test


20. Corrections require in finance service: ✅ Test
    1. remove the alerts (otp resent or sent alert), instead project it in the UI itself!
    2. The login page and otp verification page must have the same split image style like admin, but here with an image related to financial management team.
    3. The Dashboard must show some payments (last 10 payments).
    4. The transaction page is not showing up with any payments, IDs properly, display it perfectly, note: the currency must be INR, not USD, the ID column must not have sort option, status must not have sort option. Remove the search option from it.


-----------------------------------------------


22. User Side Changes:
    1. Remove view all citie's showing another div, instead make it to extend the current logo's to more cities, and no option must be there to collapse it again. It must get collapsed automatically once the modal has been closed. ✅

    2. Event Overview and booking Page: The location map, show the map properly, redirect to google map on click.
    Contact organizer button must move the user to the gmail. ✅

    3. Browse Events: The search bar in the browse events page must also show recommendations, the browsed results is still not retriving the events properly, for example: when searched for 'tech', the search recommendation is 4, but browsing result is only 2.  Event when clicking on browse events without any filters, only 8 events out of 52 is showing up. ✅
    
    4. The search bar recommendations and browse events must be powerful to search, it must match the keywords with event title, venue names, region names, organizer name. ✅
    
    5. The next button (moves to next page of browse results) must be scrolling the user to the top to the original state. ✅

    6. The physical, virtual, hybrid type is not working, whatever the type of event filter made, only physical events are getting shown. ✅

    7. Make this price limit as a slider that goes upto maximum fo the maximum price amoung all the events that got retrived, and make it functional. ✅

    8. The sorting is also not working in the browse events page. ✅

    9. No need to show failed status and activation pending status events in the hosted events directory of the organizer. ✅

    10. Drag and Drop of image is not working in event creation part, it must accept most common image formats even when I dragged and dropped from other sites or google image search results. The images must be then converted into webp while storing.

    11. The venue dropdown must comes with a search option along with it, user can search by region name, venue name, capacity number. The venue must show the address along with the title and capacity in the dropdown, increase the height of the dropdown and modify the dropdown lists into a white div for showing it properly.

    12. The refresh button in the staff allocation card, must have a small delay and have both skeleton loading as well as the refresh button spinning before showing the results while refreshing.

    13. In the ticket pricing section of event creation, the total seat capacity of all the tier inputs must not be exceeded the original venue capacity, if the user entered high numbers, grey out the checkout button and add a red color message in that ticket pricing section div in suitable place.

    14. If the user is getting back from the stripe payment page, to the event creation page, immediately raise a modal popup confirming the user to cancel the payment, if yes, call the revert event creation api call.

    15. The original event creation amount and the stripe payment amount is getting mismatched sometimes, original amount is 9480, but stripe amount in its checkout page is 8000.

    16. In the organizer dashboard, the total earnings and the tickets sold are not getting shown it must get retrived from { "ticketsSold": 0, "netEarnings": 0.00 } of the dashboard api. ✅

    17. The venue status is not getting changed as available in the backend, check the issue, this might be a bug caused by the background service. The background service before changing the event status as 'Failed' from 'Activation Pending', it must check whether the venue has been taken over by come other event in between, if yes then don't change the venue availabilty status as true. 

    18. Make the event viewing modal card of the organizer as huge, for the virtual events show the URL and password, along with a note that "it'll be enabled for the attendee during the time of event, ask them to join the meeting url before 10 mins of event schedule", of the meeting which must be hidden with astrics and a button eye icon must be there to view it and there must be a copy button. Make that created event viewing card to be detailed, follow the same UI and color palette and sizes used for the organier dashboard page, the organier must be able to edit the description how much ever time they need, title must be edited only 2 times and add a recommendation for the organizer not to edit the title often, rest of the things cannot be edited.

    19. The cancelled booking animation in the my bookings page must have the background as the same cancellation card details with quite low opacity. It must be like, once the user clicked on confirm cancellation button, the entire card's opacity must go low, and there the cancelled booking animation must come, even make the cancelled booking animation box opacity quite low.

    20. The user profile update is not making any api call to the backend, when the user is changing their email, they must have a otp verfication there to the new email, also there must be a check while they entering their email as soon as they entered (block otp trigger button) whether that email is already existing or not, this check must be done even during registration as soon as they entered (block otp trigger button).

    21. The password reset option in user profile is also not making an send otp api call, also it must have some resend option enabled with some timing same like registration.
    Note: All the resend otp option must have limit, user can resend their otp only for 3 times. Then ask them to try again later after 10 minutes, blocking the resend option for them for the next 10 mins using a redis cache in the backend. The logic is simply for the count the same user email is request for OTP, and if the count exceeds the limit then return back with try again later option.

    22. In the support ticket raising page, Change the The select booking dropdown to a button, it must open up a modal box with the list of events user created and the bookings they made. It must be convenient for the user to select them. There must be a toggle Events/Bookings, where by choosing one the user will see the list of events or bookings, selecting it will map its ID with ticket to the related id. Note: Its mandatory for the user to choose it.

    23. The raised support ticket are getting stored in the local storage, it must not be! Add an api call to retrive the user support tickets in the /my-tickets page. Map the ticket cateogries with their abbreavation (short forms) properly and then send the request.
    These are the categories and their abbrevation:
    1. REF - Refund
    2. EVT - Event
    3. ACC - Account
    4. GEN - General
    (Target types: ATD - Attendee (If booking related concerns), ORG - Organizer (If event related concern))

    24. Critical update: The platform must never show the cancelled, failed events at any cause, it must only show the events which are live.

    25. Integrating time limit in the current stripe checkout page:
    Integrate time limit in the stripe checkout page which is 5 minutes the stripe checkout page must project the time running, once the time limit is expired, the respective operation must be cancelled (call revert api of the event or booking) and it must get back to cancellation page of that event or the booking and this page must have a proper industry level payment cancellation animation.

---------------------------------------------------


24. Admin Side Changes:
    1. Rectify the support ticket page structure: The support ticket page is not showing the data properly, all the fields are just simply blank. It must show the data with a button of esclation, once this button has been clicked, there must be a form appeared for filling this following details: Action (REF - refund, EVT - Event, ACC - Account, GEN - General), Target Type (ATD - Attendee /ORG - Organizer). And the request to the backend must sent properly with its required ID's and details. Instead of showing the url, retrive the content from the URL, show the subject properly, and show the detailed message in a modal popup(like a mail ui structure, with user id, name, and email) once the admin has been clicked on that particular ticket div, and the same esclation button must be there in that modal. And while sending the request to the backend, map the ticket id properly and send it. Check whether the action, target type are properly mapped and sent.
    
    2. The button status: it must be turned from button to a status projection board and this esclation status must call an endpoint that retrives the status from AdminAction on every refresh to update.

    3. Add staff id in the staff section, Pagination is not working in the staffs page, move the page next and previous to the center, for each and every page, the data must be retrived properly from the backend. Add a search option for the staff allocation section where the user can search with staff name, id or email. Add a collapse and dropdown option for the entier sections for both the flagged events section and staff allocation section separately keeping only the section title. 

    4. Sorting issues: Remove staffs sorting by ID, status. Instead of showing region ID, show the region name. The sorting option must send a new api request to the backend, if no sorting applied make the field null, if sorting applied, the backend must return the response properly in sorted format. 
    For example: 
    Data = {A, B, C, F, E, D, I , H}
    Pagination : 3
    Sorting = 'Ascending'
    First page response = {A, B, C}
    Second Page response = {D, E, F}
    Last Page response = {H, I}

    Also sligthly modify the Previous, next button for switching pages, position them properly, make them convenient and modern. 

    5. Make the left slider to collapse and expand, Change its color to comething else like white with cardinal red blurred pastel gradient flow. (This is just a suggestion, you can change it to something else which must be matched with color palette and differ from footer color). Reduce the height of the Footer of the admin, instead of showing everything vertically show them Horizontally, a minimal height is more enough. 

    6. The Admin Profile Information is not getting shown properly! The admin have no access to edit their profile, only the password can be changed upon a OTP verification, but which is also have a bug in it.
    The password reset option in user profile is also not making an send otp api call, also it must have some resend option enabled with some timing and the limits of resend count which is 3 times. 

25. In the My bookings page, few things need to be added.

    1. Add another filter dropdow option for completed bookings, and inside that booking card, it must show The star ratings and A review input. Once the user given the feedback there must be a small feeback submitted tick animation for a second, and that start rating and review input box must get greyed out with the ratings and the review given by the user.
    Note: This feedback must get saved in a json file inside assets/users/{user_id}/feedback folder and it must be handled exactly like how support tickets are stored and handled.

    2. Add a Report event button in all the booking cards and the event booking and overview page in suitable place. It must open a quick modal with suitable options to fill by the user with a submit button that makes an api call to properly update the EventReports table.

------------------------------------


26. Admin Side Changes:
    1. The Dashboard page must also another column as event date in format as 'July 20, 2026', and another column as time.

    2. IN the dashboard page, when sorting the staff count getting error as: The LINQ expression 'DbSet<Event>() .Where(e => e.Status == "Live" || e.Status == "Completed" || e.Status == "Cancelled") .OrderBy(e => MaterializeCollectionNavigation( Navigation: Event.StaffAllocations, Subquery: DbSet<EventStaffAllocation>() .Where(e0 => EF.Property<int?>(e, "Event_Id") != null && object.Equals( objA: (object)EF.Property<int?>(e, "Event_Id"), objB: (object)EF.Property<int?>(e0, "Event_Id")))) != null ? DbSet<EventStaffAllocation>() .Where(e1 => EF.Property<int?>(e, "Event_Id") != null && object.Equals( objA: (object)EF.Property<int?>(e, "Event_Id"), objB: (object)EF.Property<int?>(e1, "Event_Id"))) .Count() : 0)' could not be translated. Either rewrite the query in a form that can be translated, or switch to client evaluation explicitly by inserting a call to 'AsEnumerable', 'AsAsyncEnumerable', 'ToList', or 'ToListAsync'. See https://go.microsoft.com/fwlink/?linkid=2101038 for more information.


    3. Moderation Page, flagged events section: The reason column must not have sorting capability. 
    Make event every ID as an anchor link, clicking on which a new modal opens up showing up the event details along with its image. 
    
    4. Moderation Page, staff allocation section: All the allocated staff row must be clickable, which opens a modal popup showing the details of the event the staff has been allocated and the venue details and timings. Remove the search bar from the filters.

    5. The Register venue option is not showing up with the seat capacity and tier details entry option, the admin must be able to name the seat tier and straight to it he must be able to add the seat capacity in that tier. By default one ticket tier+capacity filling pair will be enabled, the add icon must make the admin to add another pair, and this must be properly updated in the VenueSeatCapacities Table.

    6. The Search option in the helpdesk is showing the results, ad I said already, it'll be more good it is highlights the exact keyword match on every records it fetches, say by entering a keyword "canc" and the search result rows events with the subject column as {cancel request, cancellation}, it must highlight the exact keyword {'canc'el request, 'canc'ellation} that matches in yelloe (quite standard highlight color), and same with the text based search.


---------------------------------

27. Finance team Changes:
    1. Dashboard Page: Add another metric called "Total Intake", this is the total revenue the management earns which is the sum of all the fee, upfrontds and ticket commision from the organizer payout (the ticket commission percentage can be fectched from platform settings attribute named "Ticket_Commision_Percentage), The total revenue is not getting calculated properly, display both the metrics properly.
    Note: The currency must be in INR not dollars, show the indian ruppee symbol.

    2. Dashboard and Transactions Page: Add another column for showing time of the transaction, reduce te width of the 'Type' column. Make the status column text to get centered properly and each status have spefici color, 'Blue' for cancelled, 'Red' for failed, 'Yellow' for pending and green for success. I also need remarks to be displayed at the last as a separate column, it must display only some fixed portion of the remarks, and by clicking on the remarks colun the user can expand that column width to show the full remarks. 
    Properly align all the columns.

    3. Organizer Payout Page: There must be an another page which mustbe placed right below the transaction page in the slide bar. This is the Organizer Payout page, It must show all the transaction page columns, But with some changes: The ID here must be event ID, it must not contain the type column, instead it'll contain organizer ID, and their email id column, and then the rest like amount (Total amount the event collected so far - Platform Ticket Commision percentage amount), date, time, status (Upcoming - If event is live, Completed - If Event is completed). No need of any filters, instead add a sort option alone in the status column, implement it perfectly.

    4. All the pages table must contain a refresh option to refresh the records.

    5. Add the collapse and open feature to the slidebar, similar to how it has been implemented for admin. Use the same cardinal red gradient color and same width and text size of slide bar texts used for admin page in the slide bar, use the same footer about section style in all the pages of finance team that has been used for admin page instead of the current one.

    6. Esclation Page: This page must show the Ticket ID, Attendee Email ID, Subject, Esclation Type, Target Type, Created At and the Status (This shows the Action Status). Similar to admin helpdesk, by clicking on the Ticket, will open a modal which shows even more details like Action ID, Admin Details, same dropdown opens up from the ID Anchor of booking or event showing up the details of the booking or the event. Same like how Admin esclated with a button, here finance team have Approve or Decline button, approving it will open another dropdown extension (like esclation form) it asks for the related details and there wll be a button named "Proceed" that calls the approve api call, once it is approved same tick animation will get appear with a text of "Action {ActionID} Approved" below it. And then it closes the modal.


28. Add validations.

29. Global Update: No single filter and sorting must get resetted upon refreshing the page, the filters must get reset only explicitly cleared on "clear all" anchor or page navigation. For all the sorting in columns, by default sort it by date if it is present, if date is not present, sort it with the respective status like: Allocated/Available or the corresponding ones where the status of available must be first, the logic with status sorting is new live records must come first instead of old records like (responded event flag, responded support ticket etc.,)

30. Create a Search bar tool for Admin and Finance Team Nav Bar: Powerful tool that searches events and bookings by ID, name, venue any keyword. The results in recommendations must shown properly with booking id, and it must be properly highlighting the keywords in the result,  it'll be more good it is highlights the exact keyword match on every records it fetches, say by entering a keyword with event id as "1001" and the search result rows events with the subject column as {100012, 100013}, it must highlight the exact keyword {'10001'2, '10001'3} that matches in yellow (quite standard highlight color), and same with the text based search.

31. No text copied reflection on copied in event id of helpdesk, flagged events organizer email icon. Also check whether there is one for organizer my events, event detail modal. Use the same one for all.

32. Admin Helpdesk:
    1. Show the tick animation properly once the admin has been esclated the ticket exactly same like booking cancellation with black color and "Ticket #{Ticket Id} Esclated" text below it. And it must close the modal after the animation completes.

    2. Add Esclation Column for Admin helpdesk, if the esclation is unavailable, show "Unavailable", if Available, but not proceeded show only Available, If available and esclated, then show the respective esclation status "Like processing" of whatever the status it have on the ActionStatus attribute of the AdminAction table. Add filter for this column. No need of shoowing the esclation status inside the modal popup for support ticket details and esclation actions.

33. Create Checkin Page: Add a new checkin page component, it must have a separate route as booking/checkin which must have an image upload option, where I can drag and drop the QR image from wherever I can, and it must first scan the QR image (scanning animation must be there), and then for that particular QR hash in the booking table, it must change the status of that particular booking as checkedin, a quick tick animation must be there saying Checked in and it must be ready for the next QR to get uploaded.

Important Note: There must be a critical validation that this must not happen before 1 hour of the event start time. Currently for testing purpose add that particular validation in the backend and comment it out.

34. Change all the Email Templates, Sender name must be GetMyEvents, and the logo must replaced in place of the "Event Platform" Title of the email message.


35. Removed ReferenceId from AdminActions table, as TicketId is sufficient.

------------------------------------------


36. Event Reporting Module:
    1. The event must be cancelled if it is still live and not started (current time < event time), organizer account will be updated (Either Restricted or Deactivated) mail will be sent to the organizer regarding their account status as well as event cancellation (specify that the event has been cancelled based on reports - in a professional way).
    2. Full Refund must be processed for all the attendees of the event.
    3. The status of the EventReport must be changed as Upholded. 


    Main thing here:
    1. This cancelled event must not be retrived and shown in any page of the attendee.

    2. If the attendee have a bookings for that event, it must be cancelled and there must be a note on the booking card that "This event has been cancelled, your refund has been processed, you'll receive it... bla bla bla" in short and professional way.

    3. The attendee cannot access the event QR and Virtual Link after it is marked as cancelled or completed both the buttons must be greyed out, when they try to click it must show respective small popup (similar to how it shows  "Virtual link will be enabled only during the event"). 

37. The Deactivated User cannot login or register with that email. Restricted User cannot create an event, their create event button must be greyed out, when they entier into manage my events page, the entire page must be overlayed with black and white color with a red note of "You're restricted from creating further events". 

39. Validations in frotend: All the validations in the platform must be handled properly, no alerts must be made instead validate immediately after they entered and the mouse has been moved into other input. 
Say for example: Event creation validation where the user can create event only before 24 hours and they cannot create an event in the past date other than the present date. The current system waits until the user is clicking the checkour button and then raising an alert. 
But, validate it in the frontend immediately as soon as they completed entering it, instead of showing alert add a red message right below that respective input , say for example if it is a date time div, add that red invalidation error right below that div or inside the div in suitable space if available.

40. Modificaiton in Admin Event Report Upholding flow:
    
    Instead of having a Remark input in the admin uphold modal, change it to Message input (actionReason -> adminUpheldMessage), once the admin gave it:

    1. Event Must be cancelled if it is live.

    2. Organizer user status must be changed with either Restricted or Deactivated.

    3. A support ticket must be raised with,
    userId: AdminID, 

    Request Type: REF,

    ConcernUrl: Stored Ticket file URL which must be created under Event.Business/assets/admins/{AdminId}/support/ticket_{ticketid}.json,

    Status: Open

    EsclationStatus: Esclated, 

    RelatedId: EventID, 

    TargetType: ORG, 

    CreatedAt: Respective Timing.

    4. Admin Action must be created with,

    AdminId: Admin ID, 
    Action Type: REF,
    Target Type: ORG, 
    Target Id: Organizer Id, 
    Remarks: A default remarks that "Event {EventId} has been Upheld".
    CreatedAt: Respective Time
    ActionStatus: "Pending" Initially
    TicketId: TicketId.

    5. Refund attendees.

    6. OrganzizerAccountActionTemplte Email Must be sent.



41. Venue Creation:
    Whenever the admin has been entering the venue name in its input, retrive the similar venues for each and every keyword, it must act as a search box where the admin can see all the existing venues in the similar name. Admin cannot select it, its just only for his reference sake.

    There must an another validation happen over there, even if the admin tries to enter the same venue name, the frontend validation check must trigger immediately that "This venue is already exists, try different name if you pointing out to different region."


42. Admin Page Flickering:
    I recently noted that, As soon as I swiched from user tab (opened and logged in) to the admin tab (opened and logged in), it keeps on flickering between some error stating 4.. something, I don't know what the issue is.


43. Issue with brevo:
    My brevo email service is getting stopped often and I  getting an email as "verify your new IP", I don't know how many times I should've do that and why it requires it. Give me a proper solution for it.

44. In the Organizer dashboard, for the failed status event, add a button named recreate, that get backs to the event creation page filling the exact details that filled already.

its completely fine to retrive the failed status event, add a category for it in status dropdown, if some code is restricting from showing it, then allow to show the failed status events. Add a proper label background for failed event as Red.

----------------------------

45. Clear Filter button on venue page, Similar venue suggestion as smooth dropdown.

46. Venue Created Animation.

47. Check Charukeerthi, user event creation dashboard.

48. Venue Page Corrections:
    1. Only the available venues are getting retrived in the venues page, apply proper pagination for it efficiently like other pages.
    
    2. The Region column is showing the region ID, instead it must show the Region Name.

    3. As we're applying pagination, not all the venues will be readily available so in the Admin controller and an endpoint and service specifically for searching the venues which will get utilized in the name input while registering as well as updating.

    4. Reduce the Height of the Update Venue Modal, and make the internal details as scrollable.