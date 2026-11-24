import {
    createContext,
    useCallback,
    useContext,
    useEffect,
    useMemo,
    useState,
    type ReactNode,
} from 'react';
import { notifications } from '@mantine/notifications';
import { apiClient, setUnauthorizedHandler } from '../../api/client';
import type {
    AuthResponseDto,
    LoginRequestDto,
    RegisterRequestDto,
    UserDto,
} from '../../api/types';
import {
    clearStoredAuth,
    getStoredAuth,
    setStoredAuth,
    type StoredAuthSession,
} from './authStorage';

interface AuthContextValue {
    user: UserDto | null;
    token: string | null;
    expiresAt: string | null;
    isAuthenticated: boolean;
    isAdmin: boolean;
    isLoading: boolean;
    login: (payload: LoginRequestDto) => Promise<void>;
    register: (payload: RegisterRequestDto) => Promise<void>;
    logout: (reason?: 'manual' | 'expired') => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function extractUserFromAuthResponse(data: AuthResponseDto): UserDto {
    if (data.user) {
        return data.user;
    }
    return {
        id: data.userId ?? '',
        username: data.username ?? '',
        email: data.email ?? '',
        role: data.role ?? 'Analyst',
    };
}

export function AuthProvider({ children }: { children: ReactNode }) {
    const [session, setSession] = useState<StoredAuthSession | null>(() =>
        getStoredAuth()
    );
    const [isLoading, setIsLoading] = useState<boolean>(() =>
        Boolean(getStoredAuth())
    );

    const logout = useCallback((reason: 'manual' | 'expired' = 'manual') => {
        clearStoredAuth();
        setSession(null);

        if (reason === 'expired') {
            notifications.show({
                title: 'Session Expired',
                message: 'Your session has expired. Please sign in again.',
                color: 'orange',
            });
        }
    }, []);

    // global interceptor for 401
    useEffect(() => {
        setUnauthorizedHandler(() => logout('expired'));
        return () => setUnauthorizedHandler(null);
    }, [logout]);

    // token valid check at start of the app
    useEffect(() => {
        const existing = getStoredAuth();
        if (!existing) {
            setIsLoading(false);
            return;
        }

        let isMounted = true;
        apiClient
            .get<UserDto>('/auth/me')
            .then((res) => {
                if (!isMounted) return;
                const updated: StoredAuthSession = {
                    ...existing,
                    user: res.data,
                };
                setStoredAuth(updated);
                setSession(updated);
            })
            .catch(() => {
                if (!isMounted) return;
                clearStoredAuth();
                setSession(null);
            })
            .finally(() => {
                if (isMounted) setIsLoading(false);
            });

        return () => {
            isMounted = false;
        };
    }, []);

    // Aler before ExpiresAt
    useEffect(() => {
        if (!session?.expiresAt) return;

        const expiryMs = new Date(session.expiresAt).getTime();
        const remainingMs = expiryMs - Date.now();

        if (remainingMs <= 0) {
            logout('expired');
            return;
        }

        const warningOffsetMs = 2 * 60 * 1000; 
        const warningTimer =
            remainingMs > warningOffsetMs
                ? window.setTimeout(() => {
                    notifications.show({
                        title: 'Session Expiring Soon',
                        message: 'Your session will expire in 2 minutes.',
                        color: 'yellow',
                    });
                }, remainingMs - warningOffsetMs)
                : undefined;

        const logoutTimer = window.setTimeout(() => {
            logout('expired');
        }, remainingMs);

        return () => {
            if (warningTimer) window.clearTimeout(warningTimer);
            window.clearTimeout(logoutTimer);
        };
    }, [session?.expiresAt, logout]);

    const handleAuthSuccess = useCallback(async (authData: AuthResponseDto) => {
        const initialUser = extractUserFromAuthResponse(authData);
        const initialSession: StoredAuthSession = {
            token: authData.token,
            expiresAt: authData.expiresAt,
            user: initialUser,
        };

        setStoredAuth(initialSession);
        setSession(initialSession);

        if (!initialUser.email || !initialUser.id) {
            const meResponse = await apiClient.get<UserDto>('/auth/me');
            const fullSession: StoredAuthSession = {
                ...initialSession,
                user: meResponse.data,
            };
            setStoredAuth(fullSession);
            setSession(fullSession);
        }
    }, []);

    const login = useCallback(
        async (payload: LoginRequestDto) => {
            const response = await apiClient.post<AuthResponseDto>(
                '/auth/login',
                payload
            );
            await handleAuthSuccess(response.data);
        },
        [handleAuthSuccess]
    );

    const register = useCallback(
        async (payload: RegisterRequestDto) => {
            // Backend /auth/register returns UserResponseDto without a JWT token
            await apiClient.post<UserDto>('/auth/register', payload);

            // Immediately sign in with the created credentials to obtain JWT session
            await login({
                email: payload.email,
                password: payload.password,
            });
        },
        [login]
    );

    const value = useMemo<AuthContextValue>(
        () => ({
            user: session?.user ?? null,
            token: session?.token ?? null,
            expiresAt: session?.expiresAt ?? null,
            isAuthenticated: Boolean(session?.token),
            isAdmin: session?.user.role === 'DataAdministrator',
            isLoading,
            login,
            register,
            logout,
        }),
        [session, isLoading, login, register, logout]
    );

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
    const ctx = useContext(AuthContext);
    if (!ctx) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return ctx;
}