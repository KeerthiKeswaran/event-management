-- Database Creation Script & Reference Data Seeding

-- 1. DATABASE SYSTEM INITIALIZATION
-- CREATE DATABASE event_management_db;

----- Select all tables ------

SELECT schemaname, tablename 
FROM pg_catalog.pg_tables 
WHERE schemaname NOT IN ('pg_catalog', 'information_schema');


------- Clear all records --------

DO $$ 
DECLARE 
    r RECORD;
BEGIN
    -- Loop through all tables in the public schema except the EF Migrations table
    FOR r IN (
        SELECT tablename 
        FROM pg_tables 
        WHERE schemaname = 'public' 
          AND tablename NOT IN ('__EFMigrationsHistory', '__efmigrationshistory')
    ) LOOP
        -- Safe dynamic SQL execution using format()
        EXECUTE format('TRUNCATE TABLE %I CASCADE;', r.tablename);
    END LOOP;
END $$;



--------- Query ----------

SELECT setval('"BookingPayments_Booking_Payment_Id_seq"', COALESCE((SELECT MAX("Booking_Payment_Id") + 1 FROM "BookingPayments"), 10001), false);

SELECT setval('"Transactions_Transaction_Id_seq"', COALESCE((SELECT MAX("Transaction_Id") + 1 FROM "Transactions"), 10001), false);

SELECT setval('"AdminActions_Action_Id_seq"', COALESCE((SELECT MAX("ActionId") + 1 FROM "AdminActions"), 10001), false);

SELECT setval('"SupportTickets_Ticket_Id_seq"', COALESCE((SELECT MAX("Ticket_Id") + 1 FROM "SupportTickets"), 10001), false);

SELECT setval('"Notifications_Notification_Id_seq"', COALESCE((SELECT MAX("Notification_Id") + 1 FROM "Notifications"), 10001), false);

SELECT setval('"OrganizerUpfrontPayments_Upfront_Payment_Id_seq"', COALESCE((SELECT MAX("Upfront_Payment_Id") + 1 FROM "OrganizerUpfrontPayments"), 10001), false);

SELECT setval('"OrganizerPayouts_Payout_Id_seq"', COALESCE((SELECT MAX("Payout_Id") + 1 FROM "OrganizerPayouts"), 10001), false);



-------
SELECT setval('"AdminActions_Action_Id_seq"', COALESCE((SELECT MAX("ActionId") + 1 FROM "AdminActions"), 10001), false);

SELECT setval('"Users_User_Id_seq"', COALESCE((SELECT MAX("User_Id") + 1 FROM "Users"), 10001), false);
SELECT setval('"Staffs_Employee_ID_seq"', COALESCE((SELECT MAX("Employee_ID") + 1 FROM "Staffs"), 10001), false);

SELECT setval('"Events_Event_Id_seq"', COALESCE((SELECT MAX("Event_Id") + 1 FROM "Events"), 10001), false);
SELECT setval('"Venues_Venue_Id_seq"', COALESCE((SELECT MAX("Venue_Id") + 1 FROM "Venues"), 10001), false);

SELECT setval('"Bookings_Booking_Id_seq"', COALESCE((SELECT MAX("Booking_Id") + 1 FROM "Bookings"), 10001), false);
SELECT setval('"Waitlists_Waitlist_Id_seq"', COALESCE((SELECT MAX("Waitlist_Id") + 1 FROM "Waitlists"), 10001), false);

SELECT setval('"Transactions_Transaction_Id_seq"', COALESCE((SELECT MAX("Transaction_Id") + 1 FROM "Transactions"), 10001), false);
SELECT setval('"BookingPayments_Booking_Payment_Id_seq"', COALESCE((SELECT MAX("Booking_Payment_Id") + 1 FROM "BookingPayments"), 10001), false);
SELECT setval('"OrganizerUpfrontPayments_Upfront_Payment_Id_seq"', COALESCE((SELECT MAX("Upfront_Payment_Id") + 1 FROM "OrganizerUpfrontPayments"), 10001), false);
SELECT setval('"OrganizerPayouts_Payout_Id_seq"', COALESCE((SELECT MAX("Payout_Id") + 1 FROM "OrganizerPayouts"), 10001), false);

SELECT setval('"SupportTickets_Ticket_Id_seq"', COALESCE((SELECT MAX("Ticket_Id") + 1 FROM "SupportTickets"), 10001), false);
SELECT setval('"EventReports_Report_Id_seq"', COALESCE((SELECT MAX("Report_Id") + 1 FROM "EventReports"), 10001), false);
SELECT setval('"EventFeedbacks_Feedback_Id_seq"', COALESCE((SELECT MAX("Feedback_Id") + 1 FROM "EventFeedbacks"), 10001), false);
SELECT setval('"Notifications_Notification_Id_seq"', COALESCE((SELECT MAX("Notification_Id") + 1 FROM "Notifications"), 10001), false);
SELECT setval('"PlatformSettings_Settings_Id_seq"', COALESCE((SELECT MAX("Settings_Id") + 1 FROM "PlatformSettings"), 10001), false);

-----


Select * from "OrganizerUpfrontPayments";
Select * from "Users" Where "Email" Like '%gmail.com';

Select * from "Events" where "Event_Id" = 10006;
Select * from "Venues" where "Venue_Id" = 10006;
Select * from "VenueSeatCapacities" where "Venue_Id" = 10006;
Select * from "EventTicketTiers" where "Event_Id" = 10006;

Update "EventTicketTiers"
Set "Tickets_Sold" = 49
Where "Tier_Name" = 'VIP Access' and "Event_Id" = 10006;

Select * from "Admins";
Select * from "Bookings" Where "Attendee_Id" = 10031;
Update "Users"
Set "Status" = 'Active'
Where "User_Id" = 10662;

Select * from "Venues"  Where "Is_Available" = true;

Delete from "Transactions" Where "Status" = 'Failed';
Select * from "BookingPayments";

Select * from "UserInterestedRegions";
Select * from "TermsAndConditions";
Select * from "Bookings";
Select * from "BookingDetails";

Select * from "BookingPayments";


Select * from "Venues";
Select * from "Management";
Select * from "Admins";
Select * from "Events" Where "Status" = 'Activation Pending';
Update "Events" 
Set "Status" = 'Cancelled'
Where "Title" LIKE 'Madurai Chithirai%';

Select * from "Events" Where "Status" = 'Cancelled';
Update "Venues" 
Set "Is_Available" = true
Where "Venue_Id" = 10017;

Select * from "PlatformSettings";
Select * from "Transactions";
Select * from "EventReports";
Select * from "EventTicketTiers";
Select * from "VenueSeatCapacities";
Select * from "AdminActions";
Select * from "OrganizerUpfrontPayments";
Select * from "SupportTickets";
Select * from "TermsAndConditions";
Select * from "Events";
Select * from "EventFeedbacks";

Select count(*) from "Staffs"
Where "IsAllocated" = true;

UPDATE "Users"
SET "Created_At" = '2026-06-15'::timestamp 
                  + (random() * 20) * interval '1 day' 
                  + (random() * 24) * interval '1 hour'
WHERE "Created_At" < '2026-06-15'::timestamp;


Select * from "Events";
Select * from "Venues";
Select * from "Management";

Select * from "Events" as "e"
JOIN "Venues" "v" on "v"."Venue_Id" = "e"."Event_Id"
Join "Management" "r" on "r"."Region_Id" = "v"."Region_Id"
Where "r"."Region_Name" = 'Chennai';

Select * from "Venues"
Where "Is_Available" = true;

Update "Venues"
Set "Is_Available" = false
where "Venue_Id" = '10049';

Update "SupportTickets"
Set "TargetType" = 'ATD'
Where "Ticket_Id" in (10001, 10006, 10329);

Delete from "SupportTickets"
Where "Ticket_Id" = 10328;

Delete from "AdminActions"
Where "ActionId" in (10008, 10009, 10010);

Update "EventReports"
Set "ResponseAction" = 'Upholds'
Where "Report_Id" = 10060;


Select * from "OrganizerUpfrontPayments";

Update "Events"
Set "Image_Url" = 'https://www.stayvista.com/blog/wp-content/uploads/2026/04/fevestival.png'
Where "Event_Id" = 10018;
----- Deleting -----

Update "PlatformSettings"
Set "Staff_Flat_Rate" = '200';

Delete from "OrganizerUpfrontPayments"
Where "Event_Id" = 11330;
Delete from "Transactions"
Where "Related_Id" = 11330;
Delete from "Events"
Where "Event_Id" = 11330;

Select * from "Bookings"
Where "Attendee_Id" = '10659' and "Booking_Status" = 'Cancelled';

Select * from "Events"
Where "Event_Id" in (
Select "Event_Id" from "Bookings"
Where "Attendee_Id" = '10659' and "Booking_Status" = 'Cancelled');

Select * from "Transactions"
Where "Related_Id" in (
	Select "Booking_Id" from "Bookings"
	Where "Attendee_Id" = '10659' and "Booking_Status" = 'Cancelled'
);
