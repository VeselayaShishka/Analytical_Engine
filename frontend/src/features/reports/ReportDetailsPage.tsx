import { useMemo, useState } from 'react';
import {
    Alert,
    Badge,
    Button,
    Center,
    Code,
    Group,
    Loader,
    Paper,
    Stack,
    Text,
    Title,
} from '@mantine/core';
import { IconAlertCircle, IconArrowLeft, IconTrash } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ApiError } from '../../api/errors';
import { AnalysisChartPanel } from '../analytics/AnalysisChartPanel.tsx';
import { DeleteReportModal } from './DeleteReportModal';
import { buildReportPresentation } from './reportPresentation';
import { getReportById } from './reportsApi';

export function ReportDetailsPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const [deleteOpen, setDeleteOpen] = useState(false);
    const [metric, setMetric] = useState<string | undefined>(undefined);

    const { data: report, isLoading, isError, error } = useQuery({
        queryKey: ['reports', 'detail', id],
        queryFn: () => getReportById(id!),
        enabled: Boolean(id),
        retry: (count, err) => !(err instanceof ApiError && err.status === 404) && count < 1,
    });

    const presentation = useMemo(
        () => buildReportPresentation(report?.resultData),
        [report]
    );

    if (isLoading) return <Center py="xl"><Loader /></Center>;

    if (isError || !report) {
        const notFound = error instanceof ApiError && error.status === 404;
        return (
            <Stack>
                <Button variant="subtle" leftSection={<IconArrowLeft size={16} />} component={Link} to="/reports" w="fit-content">
                    Back to reports
                </Button>
                <Alert icon={<IconAlertCircle size={16} />} color="red" variant="light"
                       title={notFound ? 'Report not found' : 'Failed to load report'}>
                    {notFound
                        ? 'This report does not exist or you do not have access to it.'
                        : error instanceof Error ? error.message : 'Unexpected server error.'}
                </Alert>
            </Stack>
        );
    }

    return (
        <Stack gap="md">
            <Button variant="subtle" leftSection={<IconArrowLeft size={16} />} component={Link} to="/reports" w="fit-content">
                Back to reports
            </Button>

            <Group justify="space-between" align="flex-start" wrap="wrap">
                <div>
                    <Group gap="xs">
                        <Title order={2}>{report.name}</Title>
                        <Badge variant="light">{report.reportType}</Badge>
                    </Group>
                    {report.description && <Text c="dimmed" size="sm" mt={4}>{report.description}</Text>}
                    <Text size="xs" c="dimmed" mt={4}>
                        Saved {new Date(report.createdAt).toLocaleString()} · dataset version{' '}
                        <Code>{report.datasetVersionId}</Code>
                    </Text>
                </div>
                <Button color="red" variant="light" leftSection={<IconTrash size={16} />} onClick={() => setDeleteOpen(true)}>
                    Delete
                </Button>
            </Group>

            {!presentation && (
                <Alert color="yellow" variant="light" icon={<IconAlertCircle size={16} />} title="Unsupported report data">
                    The stored report payload has an unexpected structure and cannot be visualized.
                </Alert>
            )}

            {presentation?.isEmpty && (
                <Alert color="blue" variant="light" title="Empty result">
                    {presentation.narrative}
                </Alert>
            )}

            {presentation && !presentation.isEmpty && (
                <AnalysisChartPanel
                    title={presentation.title}
                    subtitle={presentation.subtitle}
                    dataPoints={presentation.dataPoints}
                    metricKeys={presentation.metricKeys}
                    narrative={presentation.narrative}
                    recordsProcessed={presentation.recordsProcessed}
                    selectedMetric={metric ?? presentation.defaultMetric}
                    onMetricChange={setMetric}
                />
            )}

            {report.resultData?.request && (
                <Paper withBorder p="md" radius="md">
                    <Title order={5} mb="xs">Saved query parameters</Title>
                    <Code block>{JSON.stringify(report.resultData.request, null, 2)}</Code>
                </Paper>
            )}

            <DeleteReportModal
                report={deleteOpen ? report : null}
                onClose={() => setDeleteOpen(false)}
                onDeleted={() => navigate('/reports', { replace: true })}
            />
        </Stack>
    );
}
