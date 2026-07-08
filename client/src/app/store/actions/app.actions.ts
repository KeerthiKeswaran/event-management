export enum ActionTypes {
  LOGIN_START = '[Auth] Login Start',
  LOGIN_SUCCESS = '[Auth] Login Success',
  LOGIN_FAIL = '[Auth] Login Fail',
  LOGOUT = '[Auth] Logout',
  
  LOAD_USER_PROFILE = '[Auth] Load User Profile',
  LOAD_USER_PROFILE_SUCCESS = '[Auth] Load User Profile Success',
  LOAD_USER_PROFILE_FAIL = '[Auth] Load User Profile Fail',
  
  LOAD_EVENTS_START = '[Events] Load Events Start',
  LOAD_EVENTS_SUCCESS = '[Events] Load Events Success',
  LOAD_EVENTS_FAIL = '[Events] Load Events Fail',
  
  LOAD_TRENDING_START = '[Events] Load Trending Start',
  LOAD_TRENDING_SUCCESS = '[Events] Load Trending Success',
  LOAD_TRENDING_FAIL = '[Events] Load Trending Fail',

  LOAD_RECOMMENDED_START = '[Events] Load Recommended Start',
  LOAD_RECOMMENDED_SUCCESS = '[Events] Load Recommended Success',
  LOAD_RECOMMENDED_FAIL = '[Events] Load Recommended Fail',
  
  LOAD_REGIONS_START = '[Regions] Load Regions Start',
  LOAD_REGIONS_SUCCESS = '[Regions] Load Regions Success',
  LOAD_REGIONS_FAIL = '[Regions] Load Regions Fail',
  SET_REGION = '[Regions] Set Region',

  LOAD_POPULAR_REGIONS_START = '[Regions] Load Popular Regions Start',
  LOAD_POPULAR_REGIONS_SUCCESS = '[Regions] Load Popular Regions Success',
  LOAD_POPULAR_REGIONS_FAIL = '[Regions] Load Popular Regions Fail',
}

