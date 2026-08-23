import axios from 'axios';
import { API_CONFIG } from './config';

const createClient = (baseURL: string) => {
  const client = axios.create({
    baseURL,
    headers: {
      'Content-Type': 'application/json',
    },
  });

  client.interceptors.request.use((config) => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });

  return client;
};

export const authApi = createClient(`${API_CONFIG.authApiUrl}/api`);
export const billingApi = createClient(`${API_CONFIG.billingApiUrl}/api`);
export const sessionApi = createClient(`${API_CONFIG.sessionApiUrl}/api`);

export default authApi;
