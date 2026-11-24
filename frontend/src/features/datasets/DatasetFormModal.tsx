import { useEffect } from 'react';
import {
    Button,
    Group,
    Modal,
    Stack,
    Textarea,
    TextInput,
} from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { ApiError } from '../../api/errors';
import type { DatasetResponseDto } from '../../api/types';
import { createDataset, updateDataset } from './datasetsApi';

// Client-side validation rules matching backend DataAnnotations
const datasetFormSchema = z.object({
    name: z
        .string()
        .trim()
        .min(1, 'Dataset name is required')
        .max(150, 'Dataset name must not exceed 150 characters'),
    description: z
        .string()
        .max(1000, 'Description must not exceed 1000 characters')
        .optional(),
});

type DatasetFormValues = z.infer<typeof datasetFormSchema>;

interface DatasetFormModalProps {
    opened: boolean;
    onClose: () => void;
    /** If provided, the modal operates in Edit mode; otherwise in Create mode. */
    dataset?: DatasetResponseDto | null;
    /** Optional callback triggered after a new dataset is successfully created. */
    onCreated?: (created: DatasetResponseDto) => void;
}

export function DatasetFormModal({
                                     opened,
                                     onClose,
                                     dataset,
                                     onCreated,
                                 }: DatasetFormModalProps) {
    const isEditMode = Boolean(dataset);
    const queryClient = useQueryClient();

    const {
        register,
        handleSubmit,
        reset,
        setError,
        formState: { errors },
    } = useForm<DatasetFormValues>({
        resolver: zodResolver(datasetFormSchema),
        defaultValues: {
            name: '',
            description: '',
        },
    });

    // Synchronize form fields whenever the modal opens or target dataset changes
    useEffect(() => {
        if (opened) {
            reset({
                name: dataset?.name ?? '',
                description: dataset?.description ?? '',
            });
        }
    }, [opened, dataset, reset]);

    const mutation = useMutation({
        mutationFn: async (values: DatasetFormValues) => {
            const payload = {
                name: values.name.trim(),
                description: values.description?.trim() || null,
            };

            if (isEditMode && dataset) {
                return updateDataset(dataset.id, payload);
            }
            return createDataset(payload);
        },
        onSuccess: (savedDataset) => {
            // Refresh cached dataset lists and detail views
            queryClient.invalidateQueries({ queryKey: ['datasets'] });
            queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] });

            notifications.show({
                title: isEditMode ? 'Dataset Updated' : 'Dataset Created',
                message: `Dataset "${savedDataset.name}" has been saved.`,
                color: 'green',
            });

            onClose();
            if (!isEditMode && onCreated) {
                onCreated(savedDataset);
            }
        },
        onError: (err) => {
            if (err instanceof ApiError) {
                // 409 Conflict indicates a duplicate dataset name on the server
                if (err.status === 409) {
                    setError('name', {
                        message: err.detail || 'A dataset with this name already exists.',
                    });
                    return;
                }

                // Map backend ValidationProblemDetails errors to specific form inputs
                if (err.fieldErrors.name) {
                    setError('name', { message: err.fieldErrors.name[0] });
                }
                if (err.fieldErrors.description) {
                    setError('description', { message: err.fieldErrors.description[0] });
                }

                notifications.show({
                    title: err.title,
                    message: err.detail,
                    color: 'red',
                });
            }
        },
    });

    const onSubmit = (values: DatasetFormValues) => {
        mutation.mutate(values);
    };

    return (
        <Modal
            opened={opened}
            onClose={onClose}
            title={isEditMode ? 'Edit Dataset' : 'Create New Dataset'}
            centered
        >
            <form onSubmit={handleSubmit(onSubmit)} noValidate>
                <Stack>
                    <TextInput
                        label="Name"
                        placeholder="e.g., Municipal Budget 2026"
                        required
                        data-autofocus
                        error={errors.name?.message}
                        {...register('name')}
                    />

                    <Textarea
                        label="Description"
                        placeholder="Optional summary of data source, license, and coverage"
                        minRows={3}
                        maxRows={6}
                        autosize
                        error={errors.description?.message}
                        {...register('description')}
                    />

                    <Group justify="flex-end" mt="sm">
                        <Button
                            variant="default"
                            onClick={onClose}
                            disabled={mutation.isPending}
                        >
                            Cancel
                        </Button>
                        <Button type="submit" loading={mutation.isPending}>
                            {isEditMode ? 'Save Changes' : 'Create Dataset'}
                        </Button>
                    </Group>
                </Stack>
            </form>
        </Modal>
    );
}