import { Button, Group, Modal, Stack, Text } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiError } from '../../api/errors';
import type { ReportListItemDto } from '../../api/types';
import { deleteReport } from './reportsApi';

interface DeleteReportModalProps {
    report: Pick<ReportListItemDto, 'id' | 'name'> | null;
    onClose: () => void;
    onDeleted?: () => void;
}

export function DeleteReportModal({
                                      report,
                                      onClose,
                                      onDeleted,
                                  }: DeleteReportModalProps) {
    const queryClient = useQueryClient();

    const mutation = useMutation({
        mutationFn: async () => {
            if (report) await deleteReport(report.id);
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['reports'] });
            queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] });
            notifications.show({
                title: 'Report Deleted',
                message: `Report "${report?.name}" was removed.`,
                color: 'green',
            });
            onClose();
            onDeleted?.();
        },
        onError: (err) => {
            notifications.show({
                title: 'Deletion Failed',
                message: err instanceof ApiError ? err.detail : 'Failed to delete report.',
                color: 'red',
            });
        },
    });

    return (
        <Modal opened={Boolean(report)} onClose={onClose} title="Delete Report" centered>
            <Stack>
                <Text size="sm">
                    Delete report{' '}
                    <Text span fw={600}>{report?.name}</Text>? This cannot be undone.
                </Text>
                <Group justify="flex-end">
                    <Button variant="default" onClick={onClose} disabled={mutation.isPending}>
                        Cancel
                    </Button>
                    <Button color="red" loading={mutation.isPending} onClick={() => mutation.mutate()}>
                        Delete
                    </Button>
                </Group>
            </Stack>
        </Modal>
    );
}
