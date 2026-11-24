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
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { ApiError } from '../../api/errors';
import { useAuth } from './AuthContext';

const registerSchema = z
    .object({
        username: z
            .string()
            .trim()
            .min(3, 'Username must be at least 3 characters')
            .max(50, 'Username must not exceed 50 characters'),
        email: z
            .string()
            .min(1, 'Email is required')
            .email('Invalid email address'),
        password: z
            .string()
            .min(15, 'Password must be at least 15 characters long'),
        confirmPassword: z.string().min(1, 'Please confirm your password'),
    })
    .refine((data) => data.password === data.confirmPassword, {
        message: 'Passwords do not match',
        path: ['confirmPassword'],
    });

type RegisterFormValues = z.infer<typeof registerSchema>;

export function RegisterPage() {
    const { isAuthenticated, register: registerUser } = useAuth();
    const navigate = useNavigate();
    const [serverError, setServerError] = useState<string | null>(null);

    const {
        register,
        handleSubmit,
        setError,
        formState: { errors, isSubmitting },
    } = useForm<RegisterFormValues>({
        resolver: zodResolver(registerSchema),
    });

    if (isAuthenticated) {
        return <Navigate to="/" replace />;
    }

    const onSubmit = async (values: RegisterFormValues) => {
        setServerError(null);
        try {
            await registerUser({
                username: values.username.trim(),
                email: values.email.trim(),
                password: values.password,
            });
            navigate('/', { replace: true });
        } catch (err) {
            if (err instanceof ApiError) {
                if (err.fieldErrors.username) {
                    setError('username', { message: err.fieldErrors.username[0] });
                }
                if (err.fieldErrors.email) {
                    setError('email', { message: err.fieldErrors.email[0] });
                }
                if (err.fieldErrors.password) {
                    setError('password', { message: err.fieldErrors.password[0] });
                }
                setServerError(err.detail);
            } else {
                setServerError('Registration failed. Please try again.');
            }
        }
    };

    return (
        <Container size={440} my={60}>
            <Title ta="center" order={2}>
                Create an Account
            </Title>
            <Text c="dimmed" size="sm" ta="center" mt={5}>
                Register as an Analyst to import and analyze open datasets
            </Text>

            <Paper withBorder shadow="sm" p={30} mt={24} radius="md">
                <form onSubmit={handleSubmit(onSubmit)} noValidate>
                    <Stack>
                        {serverError && (
                            <Alert
                                icon={<IconAlertCircle size={16} />}
                                title="Registration Error"
                                color="red"
                                variant="light"
                            >
                                {serverError}
                            </Alert>
                        )}

                        <TextInput
                            label="Username"
                            placeholder="data_analyst"
                            required
                            autoComplete="username"
                            error={errors.username?.message}
                            {...register('username')}
                        />

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
                            description="Minimum 15 characters required by security policy"
                            placeholder="At least 15 characters"
                            required
                            autoComplete="new-password"
                            error={errors.password?.message}
                            {...register('password')}
                        />

                        <PasswordInput
                            label="Confirm Password"
                            placeholder="Repeat your password"
                            required
                            autoComplete="new-password"
                            error={errors.confirmPassword?.message}
                            {...register('confirmPassword')}
                        />

                        <Button type="submit" fullWidth mt="sm" loading={isSubmitting}>
                            Create account
                        </Button>
                    </Stack>
                </form>

                <Text c="dimmed" size="sm" ta="center" mt="md">
                    Already have an account?{' '}
                    <Anchor component={Link} to="/login" size="sm">
                        Sign in
                    </Anchor>
                </Text>
            </Paper>
        </Container>
    );
}