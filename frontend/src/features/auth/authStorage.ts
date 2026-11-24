import type { UserDto } from '../../api/types';

const AUTH_STORAGE_KEY = 'analytical_engine_auth';

export interface StoredAuthSession {
    token: string;
    expiresAt: string;
    user: UserDto;
}

export function getStoredAuth(): StoredAuthSession | null {
    try {
        const raw = localStorage.getItem(AUTH_STORAGE_KEY);
        if (!raw) return null;

        const parsed = JSON.parse(raw) as StoredAuthSession;
        if (!parsed.token || !parsed.expiresAt || !parsed.user) {
            clearStoredAuth();
            return null;
        }

        if (new Date(parsed.expiresAt).getTime() <= Date.now()) {
            clearStoredAuth();
            return null;
        }

        return parsed;
    } catch {
        clearStoredAuth();
        return null;
    }
}

export function setStoredAuth(session: StoredAuthSession): void {
    localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session));
}

export function clearStoredAuth(): void {
    localStorage.removeItem(AUTH_STORAGE_KEY);
}