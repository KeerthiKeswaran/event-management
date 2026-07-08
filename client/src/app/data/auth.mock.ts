import { UserModel, AuthResponse } from '../models/user.model';

export const mockUser: UserModel = {
  id: 10001,
  name: 'Keerthi Keswaran',
  email: 'keshwarankeerthi@gmail.com',
  mobile_Number: '9876543210',
  interested_Region_Id: 'REG01'
};

export const mockLoginResponse: AuthResponse = {
  token: 'mock-jwt-token-keshwaran-keerthi-2026',
  message: 'Mock Login Successful'
};

export const mockRegisterResponse: AuthResponse = {
  token: 'mock-jwt-token-registered-user',
  message: 'User registered successfully.'
};
