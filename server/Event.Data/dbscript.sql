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


Select * from "Users";
Select * from "UserInterestedRegions";
Select * from "TermsAndConditions";
Select * from "Bookings";
Select * from "BookingDetails";

Select * from "BookingPayments";


Select * from "Venues";
Select * from "Management";
Select * from "Admins";
Select * from "Events" Where "Status" = 'Activation Pending';

Select * from "Events" Where "Event_Id" = '10013';

Select * from "PlatformSettings";
Select * from "Transactions";
Select * from "EventReports";
Select * from "EventTicketTiers";
Select * from "VenueSeatCapacities";
Select * from "AdminActions";
Select * from "OrganizerUpfrontPayments";
Select * from "SupportTickets";
Select * from "TermsAndConditions";

Select * from "EventFeedbacks";

Select count(*) from "Staffs"
Where "IsAllocated" = true;

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

Update "AdminActions"
Set "ActionStatus" = 'Pending'
Where "ActionId" = 10001;

Update "Admins"
Set "Name" = 'Srinath T'
Where "Admin_Id" = 'FIN01';


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
