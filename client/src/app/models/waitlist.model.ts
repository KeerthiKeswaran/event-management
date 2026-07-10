export interface JoinWaitlistRequest {
  eventId: number;
  tierName: string;
  quantity: number;
}

export interface WaitlistStatusResponse {
  waitlist_Id: number;
  event_Id: number;
  event_Title: string;
  tier_Name: string;
  quantity: number;
  status: string; // "Waiting" | "Notified" | "Booked" | "Expired" | "Cancelled"
  position: number;
  joined_At: string;
  expires_At: string | null;
}
