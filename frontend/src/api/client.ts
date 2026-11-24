import axios from 'axios';
import { notifications } from '@mantine/notifications';
import { getStoredAuth } from '../features/auth/authStorage';
import { normalizeApiError } from './errors';

type UnauthorizedHandler = () => void;

let onUnauthorizedCallback: UnauthorizedHandler | null = null;

export function setUnauthorizedHandler(handler: UnauthorizedHandler | null): void {
    onUnauthorizedCallback = handler;
}

export const apiClient = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
    headers: {
        'Content-Type': 'application/json',
    },
});

apiClient.interceptors.request.use((config) => {
    const session = getStoredAuth();
    if (session?.token) {
        config.headers.Authorization = `Bearer ${session.token}`;
    }

    // Let the browser automatically set multipart/form-data with boundary for FormData
    if (config.data instanceof FormData) {
        delete config.headers['Content-Type'];
    }

    return config;
});

apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        const normalized = normalizeApiError(error);
        const requestUrl: string = error?.config?.url ?? '';

        // Do not trigger session-expired logout on credential check endpoints
        const isAuthAttempt =
            requestUrl.includes('/auth/login') ||
            requestUrl.includes('/auth/register');

        if (normalized.status === 401 && !isAuthAttempt && onUnauthorizedCallback) {
            onUnauthorizedCallback();
        }

        if (normalized.status === 403) {
            notifications.show({
                title: 'Access Denied (403)',
                message:
                    normalized.detail ||
                    'You do not have permission to perform this action.',
                color: 'red',
            });
        }

        return Promise.reject(normalized);
    }
);