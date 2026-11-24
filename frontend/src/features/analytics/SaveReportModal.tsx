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
import type {
    AnalysisQueryRequestDto,
    ReportType,
    StatisticsRequestDto,
} from '../../api/types';
import { createReport } from './analyticsApi';

const saveReportSchema = z.object({
    name: z
        .string()
        .trim()
        .min(1, 'Report name is required')
        .max(200, 'Report name must not exceed 200 characters'),
    description: z
        .string()
        .max(1000, 'Description must not exceed 1000 characters')
        .optional(),
});

type SaveReportFormValues = z.infer<typeof saveReportSchema>;

interface SaveReportModalProps {
    opened: boolean;
    onClose: () => void;
    datasetVersionId: string;
    reportType: ReportType;
    queryRequest?: AnalysisQueryRequestDto | null;
    statisticsRequest?: StatisticsRequestDto | null;
    onSaved?: (reportId: string) => void;
}

export function SaveReportModal({
                                    opened,
                                    onClose,
                                    datasetVersionId,
                                    reportType,
                                    queryRequest,
                                    statisticsRequest,
                                    onSaved,
                                }: SaveReportModalProps) {
    const queryClient = useQueryClient();

    const {
        register,
        handleSubmit,
        reset,
        setError,
        formState: { errors },
    } = useForm<SaveReportFormValues>({
        resolver: zodResolver(saveReportSchema),
        defaultValues: {
            name: '',
            description: '',
        },
    });

    useEffect(() => {
        if (opened) {
            reset({ name: '', description: '' });
        }
    }, [opened, reset]);

    const mutation = useMutation({
        mutationFn: async (values: SaveReportFormValues) => {
            return createReport({
                name: values.name.trim(),
                description: values.description?.trim() || null,
                reportType,
                datasetVersionId,
                queryRequest: reportType !== 'Statistics' ? queryRequest : null,
                statisticsRequest:
                    reportType === 'Statistics' ? statisticsRequest : null,
            });
        },
        onSuccess: (createdReport) => {
            queryClient.invalidateQueries({ queryKey: ['reports'] });
            queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] });

            notifications.show({
                title: 'Report Saved',
                message: `Report "${createdReport.name}" has been saved to your reports.`,
                color: 'green',
            });

            onClose();
            onSaved?.(createdReport.id);
        },
        onError: (err) => {
            if (err instanceof ApiError) {
                if (err.fieldErrors.name) {
                    setError('name', { message: err.fieldErrors.name[0] });
                }
                notifications.show({
                    title: err.title,
                    message: err.detail,
                    color: 'red',
                });
            } else {
                notifications.show({
                    title: 'Save Failed',
                    message: 'Unable to save the report. Please try again.',
                    color: 'red',
                });
            }
        },
    });

    return (
        <Modal
            opened={opened}
            onClose={onClose}
            title="Save Analysis as Report"
            centered
        >
            <form
                onSubmit={handleSubmit((values) => mutation.mutate(values))}
                noValidate
            >
                <Stack>
                    <TextInput
                        label="Report Name"
                        placeholder="e.g., Q4 Regional Revenue Breakdown"
                        required
                        data-autofocus
                        error={errors.name?.message}
                        {...register('name')}
                    />

                    <Textarea
                        label="Analytical Notes / Description"
                        placeholder="Optional description of filters, methodology, or findings"
                        minRows={3}
                        maxRows={5}
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
                            Save Report
                        </Button>
                    </Group>
                </Stack>
            </form>
        </Modal>
    );
}