// Booking & Ticket Models

export interface TicketTierSelection {
  tierName: string;
  price: number;
  quantity: number;
  totalSeats: number;
  availableSeats: number;
}

export interface BookingDetail {
  tier_Name: string;
  quantity: number;
  price: number;
}

export interface BookingModel {
  booking_Id: number;
  attendee_Id: number;
  event_Id: number;
  event_Title: string;
  event_Type: 'Physical' | 'Virtual' | 'Hybrid';
  event_Date_Time: string;
  event_Image_Url?: string;
  event_Venue?: string;
  event_Region?: string;
  booking_Status: 'Pending' | 'Confirmed' | 'Cancelled';
  qr_Code_Path?: string;
  checkIn_Status: 'Pending' | 'CheckedIn' | 'Missed';
  created_At: string;
  virtual_Url?: string;
  event_Status?: string;
  total_Amount: number;
  amount_Paid?: number;
  refunded_Amount?: number;
  details: BookingDetail[];
  has_Reported?: boolean | null;
  feedback_Rating?: number | null;
  feedback_Review?: string | null;
}

export interface InitiateBookingRequest {
  eventId: number;
  tierQuantities: Record<string, number>;
}

export interface ConfirmBookingRequest {
  stripeChargeId: string;
  paymentMethod: string;
}

export interface BookingStep {
  id: number;
  label: string;
  completed: boolean;
  active: boolean;
}

export interface InitiateBookingResponse {
  booking_Id: number;
  attendee_Id: number;
  event_Id: number;
  event_Title: string;
  event_Type: string;
  event_Date_Time: string;
  total_Price: number;
  fixed_Fee_Rate: number;
  commission_Percentage: number;
}

export interface ConfirmBookingResponse {
  booking_Id: number;
  attendee_Id: number;
  event_Id: number;
  event_Title: string;
  event_Type: string;
  event_Date_Time: string;
  qr_Code_Path: string;
  virtual_Url: string;
  event_Image_Url?: string;
  total_Amount: number;
  details: { tier_Name: string; quantity: number; price?: number }[];
}

export interface ActiveVirtualLinkResponse {
  booking_Id: number;
  event_Id: number;
  virtual_Url: string;
  link_Status: string;
}
