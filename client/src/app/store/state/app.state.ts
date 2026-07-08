import { UserModel } from '../../models/user.model';
import { RegionModel } from '../../models/region.model';
import { BrowsedEventResponse, RegionPopularResponse } from '../../models/event.model';

export interface AppState {
  auth: {
    user: UserModel | null;
    token: string | null;
    loading: boolean;
    error: string | null;
  };
  events: {
    items: BrowsedEventResponse[];
    trending: BrowsedEventResponse[];
    recommended: BrowsedEventResponse[];
    totalCount: number;
    loading: boolean;
    error: string | null;
  };
  regions: {
    items: RegionModel[];
    popularItems: RegionPopularResponse[];
    currentRegionId: string;
    loading: boolean;
    error: string | null;
  };
}

export const initialAppState: AppState = {
  auth: {
    user: null,
    token: typeof window !== 'undefined' ? localStorage.getItem('user_token') || localStorage.getItem('admin_token') || localStorage.getItem('finance_token') : null,
    loading: false,
    error: null,
  },
  events: {
    items: [],
    trending: [],
    recommended: [],
    totalCount: 0,
    loading: false,
    error: null,
  },
  regions: {
    items: [],
    popularItems: [],
    currentRegionId: typeof window !== 'undefined' ? localStorage.getItem('currentRegionId') || 'REG01' : 'REG01',
    loading: false,
    error: null,
  }
};
