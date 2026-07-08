export interface TicketTierDetail {
  tier_Name: string;
  price: number;
  tickets_Sold: number;
  capacity?: number;
}

export interface TicketTier {
  tierName: string;
  price: number;
  totalSeats?: number;
}

export interface EventModel {
  event_Id: number;
  eventType: 'Physical' | 'Virtual' | 'Hybrid';
  title: string;
  descriptionUrl?: string;
  imageUrl?: string;
  dateTime: string;
  durationHours: number;
  requiresStaff?: boolean;
  venueId?: number;
  regionId?: string;
  hasAcceptedPolicy?: boolean;
  ticketTiers?: TicketTier[];
  organizerId?: number;
  status?: 'Pending' | 'Live' | 'Cancelled';
}

// Maps server: BrowsedEventResponse
export interface BrowsedEventResponse {
  event_Id: number;
  event_Type: string;     // server: Event_Type
  title: string;
  category?: string;        // server: Category
  description_Url?: string; // server: Description_Url
  image_Url?: string;       // server: Image_Url
  date_Time: string;        // server: Date_Time
  duration_Hours: number;   // server: Duration_Hours
  venue_Name?: string;      // server: Venue_Name
  address?: string;         // server: Address
  venue_Region_Name?: string; // server: Venue_Region_Name
  region_Id?: string;       // used for local client-side sorting only
  status?: string;
  organizer_Name?: string;  // server: Organizer_Name
  organizer_Email?: string; // server: Organizer_Email
  ticketTiers?: TicketTierDetail[]; // server: TicketTiers
  has_Reported?: boolean | null;
  // Computed client-side
  minPrice?: number;
}

// Maps server: RegionResponse (used for popular regions)
export interface RegionPopularResponse {
  region_Id: string;        // server: Region_Id
  region_Name: string;      // server: Region_Name
  no_Of_Staffs: number;     // server: No_Of_Staffs
}

// Maps server: PagedResult<T> (Items / TotalCount / Page / PageSize / TotalPages)
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;    // server uses PageSize not size
  totalPages: number;
  maxPrice?: number;
}
