import { AppState } from '../state/app.state';

export const selectAuth = (state: AppState) => state.auth;
export const selectCurrentUser = (state: AppState) => state.auth.user;
export const selectIsLoggedIn = (state: AppState) => !!state.auth.token;
export const selectAuthLoading = (state: AppState) => state.auth.loading;
export const selectAuthError = (state: AppState) => state.auth.error;

export const selectEvents = (state: AppState) => state.events.items;
export const selectTrendingEvents = (state: AppState) => state.events.trending;
export const selectEventsLoading = (state: AppState) => state.events.loading;
export const selectEventsError = (state: AppState) => state.events.error;

export const selectRegions = (state: AppState) => state.regions.items;
export const selectCurrentRegionId = (state: AppState) => state.regions.currentRegionId;
export const selectRegionsLoading = (state: AppState) => state.regions.loading;
