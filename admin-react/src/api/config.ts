/**
 * Единая точка задания API URL-ов для админ-панели.
 * Значения берутся из переменных окружения Vite (см. .env.example).
 * Дефолты соответствуют launchSettings.json бэкенд-сервисов.
 */
const trimSlash = (url: string) => url.replace(/\/+$/, '');

export const API_CONFIG = {
  authApiUrl: trimSlash(import.meta.env.VITE_AUTH_API_URL ?? 'http://localhost:5166'),
  billingApiUrl: trimSlash(import.meta.env.VITE_BILLING_API_URL ?? 'http://localhost:5191'),
  sessionApiUrl: trimSlash(import.meta.env.VITE_SESSION_API_URL ?? 'http://localhost:5268'),
  gatewayUrl: trimSlash(import.meta.env.VITE_GATEWAY_URL ?? ''),
} as const;
