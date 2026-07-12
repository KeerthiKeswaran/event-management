#### Finance Page:
    => ✅ Similar to the admin, the finance team will also have their own login page finance/login, they'll be having a OTP verification during their login, so place a otp input with proper resend and verification icon option similar to the registration page of the user.

    => ✅ Finance team profile: Same like admin they also have their own profile page with all the necessary options like users and admin.

    => ✅ Dashboard: This is the main home page of the finance team, that shows the transactions occured, this must project the master transactions with proper page based retrival, it must also have some statistics (only necessary ones) projected,this must contain. a standard high level filter and sorting like a payment page.

    => ✅ Upfront, Booking Payments and Payouts transaction: This must show the payments occured for the specific bookings, upfronts and payouts, similar to the previous page, this must also have a high level filetrs and sortings. Note: Add a suitable title for this page.

    => ✅ Esclations: This is the part that contains all the esclations made by the admin (retrived from admin actions table), the finance team will approve the action by choosing the refund type ( "FUL", "DYN", "REM", "NOR") Note: its the job of the frontend to show full form instead of abbrevation but the short forms only sent to the backend api call. The finance team can also decline the esclation with suitable remarks.

    => ✅ UI: The admin page is a quite administrative page, so the UI here must be more standard as compared to a casual UI of the User page, no need of any unnecessary emojies, proper design principle must be followed from docs/design/design-principle.md, the color palette must be the same but it must look like a military enterprice level application page.

    => ✅ This page also contains the similar navigation bar and about section like user side but some modifications must be done in the navigation bar by removing unnecessary things.

    => ✅ All the features must be properly mapped with an api call in the backend, if not create a new api call in the controller and its services.

     => ✅ Once done, mark each of the requirements with a tick mark beside the sentence in this finance.md file one by one, update modifications.md properly with suitable api calls and logics, append client.md and server.md