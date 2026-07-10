# Database Schema Document

This document outlines the database schema and architecture for the **Event Management & Ticketing Platform**.

---

### Users
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `User_Id` | `int` | `PK` | Unique identifier for the user. |
| `Name` | `str` | | Full name of the user. |
| `Email` | `str` | `Unique` | Unique email address. |
| `Mobile_Number` | `str` | | Mobile contact number. |
| `Password_Hash` | `str` | | Hashed password for login. |
| `Has_Terms_Consent` | `bool` | `Mandatory` | True if the user accepted the Terms of Service. |
| `Has_Data_Share_Consent` | `bool` | `Mandatory` | True if the user accepted sharing contact details with organizers. |
| `Has_Marketing_Consent` | `bool` | `Optional` | True if the user opted-in to regional recommendation broadcasts. |

### Admins
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Admin_Id` | `str` | `PK` | Company-assigned alphanumeric ID (e.g., `'ADM_1001'`). |
| `Name` | `str` | | Full name of the administrator. |
| `Password_Hash` | `str` | | Hashed password for administrator verification. |

### UserInterestedRegions
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `User_Id` | `int` | `PK, FK` | References `Users(User_Id)`. |
| `Region_Id` | `str` | `PK, FK` | References `Management(Region_Id)`. |

### Management (Regions)
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Region_Id` | `str` | `PK` | Unique identifier allocated for each region (e.g., `'CHE_1021'`). |
| `Region_Name`| `str` | `Mandatory` | Name of the region (e.g., `'Chennai'`). |
| `No_Of_Staffs` | `int` | | Total count of support staff available in that region. |

### Staffs
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Employee_ID` | `int` | `PK` | Unique identifier for the staff member. |
| `Region_Id` | `str` | `FK` | References `Management(Region_Id)`. |
| `IsAllocated` | `bool` | | True if the staff member is currently allocated to an active event. |

### Venues
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Venue_Id` | `int` | `PK` | Unique identifier for the physical venue. |
| `Region_Id` | `str` | `FK` | References `Management(Region_Id)`. |
| `Name` | `str` | | Name of the venue (e.g., `'City Auditorium'`). |
| `Address` | `str` | | Physical address of the venue. |
| `Hourly_Price` | `decimal` | | Hourly rental price charged to organizers. |
| `Is_Available` | `bool` | | True if the venue is active and available for bookings. |

### VenueSeatCapacities
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Venue_Id` | `int` | `PK, FK` | References `Venues(Venue_Id)`. |
| `Tier_Name` | `str` | `PK` | The physical tier name (`'Elite'`, `'Gold'`, `'Silver'`). |
| `Total_Seats` | `int` | | Total physical seats available in this tier for this venue. |

### Events
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Event_Id` | `int` | `PK` | Unique identifier for the event. |
| `Organizer_Id` | `int` | `FK` | References `Users(User_Id)`. |
| `Venue_Id` | `int?` | `FK, Nullable` | References `Venues(Venue_Id)`. `NULL` if event is pure virtual. |
| `Event_Type` | `str` | | Type of the event: `'Physical'`, `'Virtual'`, or `'Hybrid'`. |
| `Title` | `str` | | Title of the event. |
| `Description` | `str` | | Detailed description of the event. |
| `Date_Time` | `timestamp`| | Scheduled date and time of the event. |
| `Duration_Hours` | `decimal` | | Duration of the event in hours, used to calculate venue rental cost. |
| `Status` | `str` | | Lifecycle status: `'Live'`, `'Cancelled'`, or `'Completed'`. |
| `Requires_Staff` | `bool` | | True if the organizer requested platform support staff. |
| `Virtual_Url` | `str?` | `Nullable` | Streaming URL for Virtual/Hybrid events (hidden from non-attendees). |
| `Virtual_Password_Hash`| `str?` | `Nullable` | Hashed passcode for private meeting/streaming access. |

### EventTicketTiers
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Event_Id` | `int` | `PK, FK` | References `Events(Event_Id)`. |
| `Tier_Name` | `str` | `PK` | Ticket tier name: (`'Elite'`, `'Gold'`, `'Silver'`). |
| `Price` | `decimal` | | Ticket price set by the organizer for this tier. |
| `Tickets_Sold` | `int` | | Tickets sold in this tier. Validated against `VenueSeatCapacities.Total_Seats` during booking. |

### Bookings
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Booking_Id` | `int` | `PK` | Unique identifier for the booking. |
| `Attendee_Id` | `int` | `FK` | References `Users(User_Id)`. |
| `Event_Id` | `int` | `FK` | References `Events(Event_Id)`. |
| `Booking_Status` | `str` | | Booking state: `'Payment Pending'`, `'Confirmed'`, `'Cancelled'`, `'Refunded'`, or `'Payment Failed'`. |
| `Qr_Code_Path` | `str?` | `Nullable` | File path or URL to the generated ticket QR code image (`NULL` until paid). |
| `Created_At` | `timestamp`| | Timestamp of when the booking was initiated. |

### BookingDetails
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Booking_Id` | `int` | `PK, FK` | References `Bookings(Booking_Id)`. |
| `Tier_Name` | `str` | `PK` | The booked ticket tier name: (`'Elite'`, `'Gold'`, `'Silver'`). |
| `Quantity` | `int` | | Number of tickets purchased in this tier for this booking. |

### BookingPayments
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Booking_Payment_Id`| `int` | `PK` | Unique identifier for the booking payment record. |
| `Booking_Id` | `int` | `FK` | References `Bookings(Booking_Id)`. |
| `Transaction_Id` | `int` | `FK` | References `Transactions(Transaction_Id)`. |
| `Amount` | `decimal` | | Total ticket sales amount paid by the attendee. |
| `Platform_Fee_Cut` | `decimal` | | The commission fee amount retained by the platform. |
| `Payment_Status` | `str` | | Status of payment: `'Success'`, `'Failed'`, or `'Refunded'`. |
| `Created_At` | `timestamp`| | Timestamp of the payment transaction. |

### OrganizerUpfrontPayments
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Upfront_Payment_Id`| `int` | `PK` | Unique identifier for the organizer's upfront payment. |
| `Event_Id` | `int` | `FK` | References `Events(Event_Id)`. |
| `Transaction_Id` | `int` | `FK` | References `Transactions(Transaction_Id)`. |
| `Amount` | `decimal` | | Total upfront payment (Platform activation, venue rental, or staff fees). |
| `Payment_Status` | `str` | | Status of payment: `'Success'`, `'Failed'`, or `'Refunded'`. |
| `Created_At` | `timestamp`| | Timestamp of the payment transaction. |

### OrganizerPayouts
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Payout_Id` | `int` | `PK` | Unique identifier for the organizer payout. |
| `Event_Id` | `int` | `FK` | References `Events(Event_Id)`. |
| `Transaction_Id` | `int?` | `FK, Nullable` | References `Transactions(Transaction_Id)`. `NULL` until processed/attempted. |
| `Total_Ticket_Sales`| `decimal` | | Total ticket sales revenue collected from attendees for this event. |
| `Platform_Commission`| `decimal` | | Total platform fees deducted from sales. |
| `Payout_Amount` | `decimal` | | Net amount paid to the organizer: `Total_Ticket_Sales - Platform_Commission`. |
| `Payout_Status` | `str` | | Status of transfer: `'Success'`, `'Failed'`, or `'Refunded'`. |
| `Processed_At` | `timestamp`| | Timestamp of when the payout was processed. |

### Transactions (Audit Ledger)
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Transaction_Id` | `int` | `PK` | Unique identifier for the audit transaction entry. |
| `Sender_Id` | `str` | | Sender identifiers (e.g., `'Attendee_User_12'` or `'Platform_Escrow'`). |
| `Receiver_Id` | `str` | | Receiver identifiers (e.g., `'Platform_Escrow'` or `'Organizer_User_8'`). |
| `Stripe_Sender_Id` | `str?` | `Nullable` | Raw Stripe Customer ID (`cus_...`) or card token for debugging lookup. |
| `Stripe_Receiver_Id` | `str?` | `Nullable` | Raw Stripe Connected Account ID (`acct_...`) for debugging lookup. |
| `Transaction_Type` | `str` | | Type of transaction: `'BookingPayment'`, `'BookingRefund'`, `'OrganizerUpfrontPayment'`, or `'OrganizerPayout'`. |
| `Related_Id` | `int` | | Links back to the source ID (e.g. `Booking_Id`, `Event_Id`, or `Payout_Id`). |
| `Amount` | `decimal` | | Total value transacted. |
| `Currency` | `str` | | Three-letter currency code (e.g., `'INR'`, `'USD'`). |
| `Payment_Method_Details`| `str?` | `Nullable` | Type and details of payment source (e.g., `'Card: Visa ****4242'`). |
| `Status` | `str` | | Transaction state: `'Pending'`, `'Success'`, `'Failed'`, or `'Refunded'`. |
| `Refunded_Amount` | `decimal` | | Amount refunded so far. Defaults to `0.0`. |
| `Remarks` | `str?` | `Nullable` | Gateway response messages or logs (e.g., Stripe decline codes). |
| `Transaction_Reference`| `str?` | `Nullable` | Stripe charge/transfer ID reference token (e.g., `'ch_3MxsJf...'`). |
| `Created_At` | `timestamp`| | Timestamp of when the transaction entry was logged. |

### PlatformSettings
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Settings_Id` | `int` | `PK` | Enforces a single global configuration row (value is always `1`). |
| `Staff_Flat_Rate` | `decimal` | | Flat fee charged to organizers per allocated support staff employee. |
| `Virtual_Event_Activation_Fee`| `decimal` | | Flat platform hosting/publishing fee for virtual events. |
| `Physical_Event_Activation_Fee`| `decimal` | | Flat platform base activation fee for physical/hybrid events. |
| `Ticket_Commission_Percentage`| `decimal` | | Percentage fee cut taken from attendee ticket sales (e.g., `5.00`). |
| `Ticket_Fixed_Fee` | `decimal` | | Flat fee cut taken from attendee ticket sales (e.g., `0.99`). |
| `Max_Tickets_Per_Booking`| `int` | | Maximum tickets allowed per individual booking transaction (default: `10`). |
| `Updated_At` | `timestamp`| | Timestamp of when settings were last modified. |
| `Updated_By_Admin_Id`| `str` | `FK` | References `Admins(Admin_Id)`. Tracks the admin who changed these settings. |

### EventStaffAllocations
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Event_Id` | `int` | `PK, FK` | References `Events(Event_Id)`. |
| `Employee_ID` | `int` | `PK, FK` | References `Staffs(Employee_ID)`. |

### SupportQueries
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Query_Id` | `int` | `PK` | Unique identifier for the customer support ticket. |
| `User_Id` | `int` | `FK` | References `Users(User_Id)`. |
| `Subject` | `str` | | Brief summary/topic of the customer support query. |
| `Message` | `str` | | Detailed query message submitted by the user. |
| `Response` | `str?` | `Nullable` | Response message or solution provided by support/admin staff. |
| `Status` | `str` | | Ticket state: `'Open'` or `'Resolved'`. |

### EventFeedback
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Feedback_Id` | `int` | `PK` | Unique identifier for the review. |
| `Event_Id` | `int` | `FK` | References `Events(Event_Id)`. |
| `Attendee_Id` | `int` | `FK` | References `Users(User_Id)`. |
| `Rating` | `int` | | Event rating score, e.g. from `1` to `5`. |
| `Review` | `str` | | Optional written feedback comments from the attendee. |

### EventReports
| Column Name | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Report_Id` | `int` | `PK` | Unique identifier for the flag report. |
| `Event_Id` | `int` | `FK` | References `Events(Event_Id)`. |
| `Reporter_Id` | `int` | `FK` | References `Users(User_Id)`. |
| `Reason` | `str` | | Reason for flagging the event (e.g., spam, scam, policy violation). |
| `Created_At` | `timestamp`| | Timestamp of when the report was submitted. |






I will view the contents of the `AdminUIDesign.md` file to see the admin pages, workflows, and specifications that need to be implemented.

Viewed AdminUIDesign.md:1-391

I will view the complete `AdminAuthService.cs` file to see what methods are currently implemented for admin authentication and password resets.

Viewed AdminAuthService.cs:1-75

I will search for `AdminAuthController.cs` to see what controller endpoints are already defined for the admin authentication flow.

Viewed AdminAuthController.cs:1-34

I will list all the files in `Event.API/Controllers` to see the controllers currently present.

Listed directory Controllers

I will view the `UserAuthController.cs` file to see how OTP is sent for password resets/registrations.

Viewed UserAuthController.cs:1-61

I will search for any existing Admin-related service files (other than the auth service) to see if some of these methods are already partially implemented.

Searched for "AdminService"

I will check `SupportController.cs` to see if some of the user query resolution endpoint is already partially defined there.

Viewed SupportController.cs:1-41

I will search for `Support` service implementation or interface files to see what methods currently exist there.

Searched for "SupportQuery"

Here is the comprehensive list of backend features, API endpoints, and service/repository methods we need to implement for the **Admin Portal**, derived from [blueprint.md](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/docs/blueprint.md) and [AdminUIDesign.md](file:///Users/keerthikeswaran/Documents/GenSpark%20Internship/event-management/docs/AdminUIDesign.md):

---
