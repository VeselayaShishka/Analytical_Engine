import { Alert, Button, Group, Modal, Stack, Text } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { IconAlertTriangle } from '@tabler/icons-react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiError } from '../../api/errors';
import type { DatasetResponseDto } from '../../api/types';
import { deleteDataset } from './datasetsApi';

interface DeleteDatasetModalProps {
    dataset: DatasetResponseDto | null;
    onClose: () => void;
    /** Optional callback executed after deletion (e.g., redirecting to /datasets). */
    onDeleted?: () => void;
}

export function DeleteDatasetModal({
                                       dataset,
                                       onClose,
                                       onDeleted,
                                   }: DeleteDatasetModalProps) {
    const queryClient = useQueryClient();

    const mutation = useMutation({
        mutationFn: async () => {
            if (!dataset) return;
            await deleteDataset(dataset.id);
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['datasets'] });
            queryClient.invalidateQueries({ queryKey: ['reports'] });
            queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] });

            notifications.show({
                title: 'Dataset Deleted',
                message: `Dataset "${dataset?.name}" and its dependent data were removed.`,
                color: 'green',
            });

            onClose();
            onDeleted?.();
        },
        onError: (err) => {
            const message =
                err instanceof ApiError ? err.detail : 'Failed to delete dataset.';
            notifications.show({
                title: 'Deletion Failed',
                message,
                color: 'red',
            });
        },
    });

    return (
        <Modal
            opened={Boolean(dataset)}
            onClose={onClose}
            title="Delete Dataset"
            centered
        >
            <Stack>
                <Text size="sm">
                    Are you sure you want to delete dataset{' '}
                    <Text span fw={600}>
                        {dataset?.name}
                    </Text>
                    ?
                </Text>

                <Alert
                    icon={<IconAlertTriangle size={16} />}
                    color="red"
                    variant="light"
                    title="Permanent Action"
                >
                    All imported versions, data records, and saved reports linked to this
                    dataset will be permanently deleted.
                </Alert>

                <Group justify="flex-end" mt="sm">
                    <Button
                        variant="default"
                        onClick={onClose}
                        disabled={mutation.isPending}
                    >
                        Cancel
                    </Button>
                    <Button
                        color="red"
                        loading={mutation.isPending}
                        onClick={() => mutation.mutate()}
                    >
                        Delete Dataset
                    </Button>
                </Group>
            </Stack>
        </Modal>
    );
}