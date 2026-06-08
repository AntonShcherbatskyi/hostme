export interface ApiResponse<T> {
  data: T | null;
  errors: string[];
  isError: boolean;
}

export interface UserDto {
  id: string;
  username: string;
  email: string;
  createdAt: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  user: UserDto;
}

export interface RegisterResponse {
  id: string;
  username: string;
  email: string;
  createdAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RevokeTokenRequest {
  refreshToken: string;
}
