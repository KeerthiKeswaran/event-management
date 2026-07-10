
#### Admin Page:
1. ✅ The page must contain a deparate dedicated login page 'admin/login'.
2. ✅ Components:
    => ✅ Dashboard (Stats) page which also lists the events(refer to the Admin Controller api) along with the necessary filters. The admin can also update the venue for an event by clicking on the update button on an event to update its venues, status and other required things that are under admin clearance level (think for this properly as per industry level application, implement required api endpoints for this).

    => ✅ Moderation Page: This page is responsible for moderating the events and it also displays the reports made for the event by the attendee, the admin can dismiss or upload it, by upholding it the admin suppose to provide the reason and the action take on organizer from the dropdown ("No Action", "Restrict", "Deactivate"). 
    
    => ✅ Moderation Page: In case of moderation, the admin can manually allocate staffs, these must be a separate section in this page that shows the available staffs and their allocation status, if they allocated the status must show as allocated with view option for allocation details, if not it must show a button for allocation, and a there must be a separate modal pop up opened with list of events in the staff region venue as a dropdown ad the admin will choose an event and allocate that staff for it.

    => ✅ Venue Registration: Admin is responsible for registering a new venue by mapping them with Region(dropdown with api call), name, address, hourly price.

    => ✅ Helpdesk page: This is the page where the admin can respond to a support ticket raised by the users, they can esclate a support ticket by filling this following details: Action (REF - refund, EVT - Event, ACC - Account, GEN - General), Target Type (ATD - Attendee /ORG - Organizer). Note: All this types of population must be stored in a json file in the backend business/assets under admin folder and must get retrived by the api call to the frontend, the frontend must resolve the abbrivation of each and send the short form alone to the backend.

    => ✅ Profile page: Similar to user, admin must also have profile page with proper edit, password reset with tfa option.

    => ✅ UI: The admin page is a quite administrative page, so the UI here must be more standard as compared to a casual UI of the User page, no need of any unnecessary emojies, proper design principle must be followed from docs/design/design-principle.md, the color palette must be the same but it must look like a military enterprice level application page.

    => ✅ This page also contains the similar navigation bar and about section like user side but some modifications must be done in the navigation bar by removing unnecessary things.

    => ✅ All the features must be properly mapped with an api call in the backend, if not create a new api call in the controller and its services.

    => ✅ Once done, mark each of the requirements with a tick mark beside the sentence in this admin.md file one by one, update modifications.md properly with suitable api calls and logics, append client.md and server.md


