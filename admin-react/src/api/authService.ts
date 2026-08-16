import api from './axiosConfig';

export interface LoginRequest {
  login: string;
  password: string;
}

export interface UserDto {
  id: string;
  username: string;
  email: string;
}

export interface TokenDto {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface AuthPayload {
  user: UserDto;
  tokens: TokenDto;
}

export interface ResultResponse<T> {
  isSuccess: boolean;
  isFailure: boolean;
  error: {
    code: string;
    description: string;
    type: number;
  };
  value?: T;
}

export const authService = {
  login: async (credentials: LoginRequest): Promise<AuthPayload> => {
    const payload = {
      Login: credentials.login,
      Password: credentials.password
    };
    const { data } = await api.post<ResultResponse<AuthPayload>>('/auth/login', payload);

    if (!data.isSuccess || !data.value) {
      throw new Error(data.error?.description || 'Ошибка входа');
    }

    localStorage.setItem('accessToken', data.value.tokens.accessToken);
    localStorage.setItem('refreshToken', data.value.tokens.refreshToken);
    localStorage.setItem('user', JSON.stringify(data.value.user));

    return data.value;
  },

  logout: () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
  },
};
