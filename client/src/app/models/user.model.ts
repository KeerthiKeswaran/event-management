export interface UserModel {
  id: number;
  name: string;
  email: string;
  mobile_Number?: string;
  status?: string;
  consented_Terms_Id?: number;
  has_Marketing_Consent?: boolean;
  interested_Region_Id?: string;
}

export interface AuthResponse {
  token: string;
  message?: string;
}
