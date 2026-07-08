import { AppState, initialAppState } from '../state/app.state';
import { ActionTypes } from '../actions/app.actions';

export function appReducer(state: AppState = initialAppState, action: { type: string; payload?: any }): AppState {
  switch (action.type) {
    case ActionTypes.LOGIN_START:
    case ActionTypes.LOAD_USER_PROFILE:
      return {
        ...state,
        auth: { ...state.auth, loading: true, error: null }
      };
    case ActionTypes.LOGIN_SUCCESS:
      return {
        ...state,
        auth: { ...state.auth, loading: false, token: action.payload.token, user: action.payload.user || null }
      };
    case ActionTypes.LOAD_USER_PROFILE_SUCCESS:
      return {
        ...state,
        auth: { ...state.auth, loading: false, user: action.payload }
      };
    case ActionTypes.LOGIN_FAIL:
    case ActionTypes.LOAD_USER_PROFILE_FAIL:
      return {
        ...state,
        auth: { ...state.auth, loading: false, error: action.payload }
      };
    case ActionTypes.LOGOUT:
      return {
        ...state,
        auth: { ...state.auth, user: null, token: null, loading: false, error: null }
      };

    case ActionTypes.LOAD_EVENTS_START:
      return {
        ...state,
        events: { ...state.events, loading: true, error: null }
      };
    case ActionTypes.LOAD_EVENTS_SUCCESS:
      return {
        ...state,
        events: { ...state.events, loading: false, items: action.payload.items, totalCount: action.payload.totalCount }
      };
    case ActionTypes.LOAD_EVENTS_FAIL:
      return {
        ...state,
        events: { ...state.events, loading: false, error: action.payload }
      };

    case ActionTypes.LOAD_TRENDING_START:
      return {
        ...state,
        events: { ...state.events, loading: true, error: null }
      };
    case ActionTypes.LOAD_TRENDING_SUCCESS:
      return {
        ...state,
        events: { ...state.events, loading: false, trending: action.payload }
      };
    case ActionTypes.LOAD_TRENDING_FAIL:
      return {
        ...state,
        events: { ...state.events, loading: false, error: action.payload }
      };

    case ActionTypes.LOAD_RECOMMENDED_START:
      return {
        ...state,
        events: { ...state.events, loading: true, error: null }
      };
    case ActionTypes.LOAD_RECOMMENDED_SUCCESS:
      return {
        ...state,
        events: { ...state.events, loading: false, recommended: action.payload }
      };
    case ActionTypes.LOAD_RECOMMENDED_FAIL:
      return {
        ...state,
        events: { ...state.events, loading: false, error: action.payload }
      };

    case ActionTypes.LOAD_REGIONS_START:
      return {
        ...state,
        regions: { ...state.regions, loading: true, error: null }
      };
    case ActionTypes.LOAD_REGIONS_SUCCESS:
      return {
        ...state,
        regions: { ...state.regions, loading: false, items: action.payload }
      };
    case ActionTypes.LOAD_REGIONS_FAIL:
      return {
        ...state,
        regions: { ...state.regions, loading: false, error: action.payload }
      };
    case ActionTypes.SET_REGION:
      return {
        ...state,
        regions: { ...state.regions, currentRegionId: action.payload }
      };

    case ActionTypes.LOAD_POPULAR_REGIONS_START:
      return {
        ...state,
        regions: { ...state.regions, loading: true, error: null }
      };
    case ActionTypes.LOAD_POPULAR_REGIONS_SUCCESS:
      return {
        ...state,
        regions: { ...state.regions, loading: false, popularItems: action.payload }
      };
    case ActionTypes.LOAD_POPULAR_REGIONS_FAIL:
      return {
        ...state,
        regions: { ...state.regions, loading: false, error: action.payload }
      };

    default:
      return state;
  }
}
