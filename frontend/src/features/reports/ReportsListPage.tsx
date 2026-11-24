import { useState } from 'react';
import {
    ActionIcon,
    Alert,
    Badge,
    Button,
    Center,
    Group,
    Loader,
    Pagination,
    Paper,
    Select,
    Stack,
    Table,
    Text,
    Title,
    Tooltip,
} from '@mantine/core';
import { IconAlertCircle, IconChartBar, IconEye, IconTrash } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import type { ReportListItemDto, ReportType } from '../../api/types';
import { DeleteReportModal } from './DeleteReportModal';
import { getReports } from './reportsApi';

const PAGE_SIZE = 10;

const TYPE_COLORS: Record<string, string> = {
    Aggregation: 'blue',
    Grouping: 'teal',
    Statistics: 'violet',
};

export function ReportsListPage() {
    const [page, setPage] = useState(1);
    const [reportType, setReportType] = useState<ReportType | null>(null);
    const [deleting, setDeleting] = useState<ReportListItemDto | null>(null);

    const { data, isLoading, isError, error } = useQuery({
        queryKey: ['reports', { page, pageSize: PAGE_SIZE, reportType }],
        queryFn: () =>
            getReports({
                pageNumber: page,
                pageSize: PAGE_SIZE,
                reportType: reportType ?? undefined,
            }),
    });

    // If the last item on the last page was deleted, step back one page.
    const totalPages = data?.totalPages ?? 0;
    if (data && page > 1 && page > totalPages) {
        setPage(Math.max(1, totalPages));
    }

    return (
        <Stack gap="md">
            <Group justify="space-between" align="center" wrap="wrap">
                <div>
                    <Title order={2}>Saved Reports</Title>
                    <Text c="dimmed" size="sm">
                        Reports saved from the analytics builder
                    </Text>
                </div>
                <Button leftSection={<IconChartBar size={16} />} component={Link} to="/analytics">
                    New Analysis
                </Button>
            </Group>

            <Paper withBorder p="md" radius="md">
                <Group justify="space-between" mb="md" wrap="wrap">
                    <Select
                        placeholder="All report types"
                        clearable
                        w={220}
                        aria-label="Filter by report type"
                        value={reportType}
                        onChange={(v) => {
                            setReportType((v as ReportType | null) ?? null);
                            setPage(1);
                        }}
                        data={['Aggregation', 'Grouping', 'Statistics']}
                    />
                    {data && (
                        <Text size="sm" c="dimmed">
                            Total: {data.totalCount} report{data.totalCount === 1 ? '' : 's'}
                        </Text>
                    )}
                </Group>

                {isLoading && (
                    <Center py="xl"><Loader /></Center>
                )}

                {isError && (
                    <Alert icon={<IconAlertCircle size={16} />} title="Failed to load reports" color="red" variant="light">
                        {error instanceof Error ? error.message : 'Unexpected server error.'}
                    </Alert>
                )}

                {data && data.items.length === 0 && (
                    <Center py="xl">
                        <Stack align="center" gap="xs">
                            <Text c="dimmed">
                                {reportType ? 'No reports of this type.' : 'No saved reports yet.'}
                            </Text>
                            {!reportType && (
                                <Button variant="light" component={Link} to="/analytics">
                                    Create your first report
                                </Button>
                            )}
                        </Stack>
                    </Center>
                )}

                {data && data.items.length > 0 && (
                    <>
                        <Table.ScrollContainer minWidth={600}>
                            <Table highlightOnHover verticalSpacing="sm">
                                <Table.Thead>
                                    <Table.Tr>
                                        <Table.Th>Name</Table.Th>
                                        <Table.Th>Type</Table.Th>
                                        <Table.Th>Created</Table.Th>
                                        <Table.Th ta="right">Actions</Table.Th>
                                    </Table.Tr>
                                </Table.Thead>
                                <Table.Tbody>
                                    {data.items.map((r) => (
                                        <Table.Tr key={r.id}>
                                            <Table.Td>
                                                <Text component={Link} to={`/reports/${r.id}`} fw={500} size="sm">
                                                    {r.name}
                                                </Text>
                                                {r.description && (
                                                    <Text size="xs" c="dimmed" lineClamp={1}>
                                                        {r.description}
                                                    </Text>
                                                )}
                                            </Table.Td>
                                            <Table.Td>
                                                <Badge variant="light" color={TYPE_COLORS[r.reportType] ?? 'gray'}>
                                                    {r.reportType}
                                                </Badge>
                                            </Table.Td>
                                            <Table.Td>
                                                <Text size="sm">{new Date(r.createdAt).toLocaleString()}</Text>
                                            </Table.Td>
                                            <Table.Td>
                                                <Group gap="xs" justify="flex-end" wrap="nowrap">
                                                    <Tooltip label="Open report">
                                                        <ActionIcon variant="subtle" component={Link} to={`/reports/${r.id}`} aria-label="Open report">
                                                            <IconEye size={18} />
                                                        </ActionIcon>
                                                    </Tooltip>
                                                    <Tooltip label="Delete">
                                                        <ActionIcon variant="subtle" color="red" onClick={() => setDeleting(r)} aria-label="Delete report">
                                                            <IconTrash size={18} />
                                                        </ActionIcon>
                                                    </Tooltip>
                                                </Group>
                                            </Table.Td>
                                        </Table.Tr>
                                    ))}
                                </Table.Tbody>
                            </Table>
                        </Table.ScrollContainer>

                        {totalPages > 1 && (
                            <Group justify="center" mt="md">
                                <Pagination value={page} onChange={setPage} total={totalPages} />
                            </Group>
                        )}
                    </>
                )}
            </Paper>

            <DeleteReportModal report={deleting} onClose={() => setDeleting(null)} />
        </Stack>
    );
}
