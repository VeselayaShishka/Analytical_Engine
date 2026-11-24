import { useState } from 'react';
import {
    Alert,
    Anchor,
    Button,
    Container,
    Paper,
    PasswordInput,
    Stack,
    Text,
    TextInput,
    Title,
} from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { ApiError } from '../../api/errors';
import { useAuth } from './AuthContext';

const loginSchema = z.object({
    email: z.string().min(1, 'Email is required').email('Invalid email address'),
    password: z.string().min(1, 'Password is required'),
});

type LoginFormValues = z.infer<typeof loginSchema>;

export function LoginPage() {
    const { isAuthenticated, login } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();
    const [serverError, setServerError] = useState<string | null>(null);

    const from =
        (location.state as { from?: { pathname?: string } } | null)?.from
            ?.pathname || '/';

    const {
        register,
        handleSubmit,
        setError,
        formState: { errors, isSubmitting },
    } = useForm<LoginFormValues>({
        resolver: zodResolver(loginSchema),
    });

    if (isAuthenticated) {
        return <Navigate to={from} replace />;
    }

    const onSubmit = async (values: LoginFormValues) => {
        setServerError(null);
        try {
            await login(values);
            navigate(from, { replace: true });
        } catch (err) {
            if (err instanceof ApiError) {
                if (err.fieldErrors.email) {
                    setError('email', { message: err.fieldErrors.email[0] });
                }
                if (err.fieldErrors.password) {
                    setError('password', { message: err.fieldErrors.password[0] });
                }
                setServerError(err.detail);
            } else {
                setServerError('Failed to sign in. Please try again.');
            }
        }
    };

    return (
        <Container size={420} my={60}>
            <Title ta="center" order={2}>
                Open Data Analytics Platform
            </Title>
            <Text c="dimmed" size="sm" ta="center" mt={5}>
                Sign in to access datasets, analytics builder, and reports
            </Text>

            <Paper withBorder shadow="sm" p={30} mt={24} radius="md">
                <form onSubmit={handleSubmit(onSubmit)} noValidate>
                    <Stack>
                        {serverError && (
                            <Alert
                                icon={<IconAlertCircle size={16} />}
                                title="Authentication Error"
                                color="red"
                                variant="light"
                            >
                                {serverError}
                            </Alert>
                        )}

                        <TextInput
                            label="Email"
                            placeholder="analyst@example.com"
                            required
                            autoComplete="email"
                            error={errors.email?.message}
                            {...register('email')}
                        />

                        <PasswordInput
                            label="Password"
                            placeholder="Enter your password"
                            required
                            autoComplete="current-password"
                            error={errors.password?.message}
                            {...register('password')}
                        />

                        <Button type="submit" fullWidth mt="sm" loading={isSubmitting}>
                            Sign in
                        </Button>
                    </Stack>
                </form>

                <Text c="dimmed" size="sm" ta="center" mt="md">
                    Don't have an account?{' '}
                    <Anchor component={Link} to="/register" size="sm">
                        Create account
                    </Anchor>
                </Text>
            </Paper>
        </Container>
    );
}