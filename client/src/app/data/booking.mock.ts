import { BookingModel } from '../models/booking.model';
import { environment } from '../../environments/environment';


export const mockBookings: BookingModel[] = [
  {
    booking_Id: 10001,
    attendee_Id: 10000,
    event_Id: 105,
    event_Title: 'A.R. Rahman Live Symphony Concert',
    event_Type: 'Physical',
    event_Date_Time: '2026-10-18T19:00:00Z',
    event_Image_Url: 'https://images.unsplash.com/photo-1470225620780-dba8ba36b745?w=500&auto=format&fit=crop&q=80',
    event_Venue: 'YMCA Grounds Nandanam',
    event_Region: 'Chennai',
    booking_Status: 'Confirmed',
    event_Status: 'Live',
    qr_Code_Path: `${environment.serverUrl}/assets/dummy-qr.png`,
    checkIn_Status: 'Pending',
    created_At: '2026-06-15T14:30:00Z',
    total_Amount: 2997.00,
    details: [
      { tier_Name: 'General', quantity: 2, price: 999.00 },
      { tier_Name: 'VIP', quantity: 1, price: 999.00 }
    ]
  },
  {
    booking_Id: 10002,
    attendee_Id: 10000,
    event_Id: 303,
    event_Title: 'Madurai Startup & Innovation Pitch',
    event_Type: 'Virtual',
    event_Date_Time: '2026-10-05T10:00:00Z',
    event_Image_Url: 'https://images.unsplash.com/photo-1515187029135-18ee286d815b?w=500&auto=format&fit=crop&q=80',
    event_Venue: 'Virtual Room Madurai',
    event_Region: 'Madurai',
    booking_Status: 'Confirmed',
    event_Status: 'Completed',
    qr_Code_Path: `${environment.serverUrl}/assets/dummy-qr.png`,
    checkIn_Status: 'Pending',
    created_At: '2026-06-20T09:15:00Z',
    virtual_Url: 'https://meet.jit.si/madurai-startup-pitch-2026',
    total_Amount: 199.00,
    details: [
      { tier_Name: 'General', quantity: 1, price: 199.00 }
    ]
  },
  {
    booking_Id: 10003,
    attendee_Id: 10000,
    event_Id: 201,
    event_Title: 'Coimbatore SaaS & DeepTech Summit',
    event_Type: 'Physical',
    event_Date_Time: '2026-08-25T09:30:00Z',
    event_Image_Url: 'https://images.unsplash.com/photo-1504384308090-c894fdcc538d?w=500&auto=format&fit=crop&q=80',
    event_Venue: 'CODISSIA Hall A',
    event_Region: 'Coimbatore',
    booking_Status: 'Cancelled',
    checkIn_Status: 'Missed',
    created_At: '2026-06-01T11:00:00Z',
    total_Amount: 700.00,
    details: [
      { tier_Name: 'General', quantity: 2, price: 350.00 }
    ]
  },
  {
    booking_Id: 10004,
    attendee_Id: 10000,
    event_Id: 403,
    event_Title: 'Kaveri Musical Concert',
    event_Type: 'Physical',
    event_Date_Time: '2026-11-15T18:30:00Z',
    event_Image_Url: 'https://images.unsplash.com/photo-1465847899084-d164df4dedc6?w=1200&auto=format&fit=crop&q=80',
    event_Venue: 'Trichy Arts Auditorium',
    event_Region: 'Trichy',
    booking_Status: 'Confirmed',
    qr_Code_Path: `${environment.serverUrl}/assets/dummy-qr.png`,
    checkIn_Status: 'Pending',
    created_At: '2026-06-22T08:45:00Z',
    total_Amount: 400.00,
    details: [
      { tier_Name: 'General', quantity: 2, price: 200.00 }
    ]
  }
];
