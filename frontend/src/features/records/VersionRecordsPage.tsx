import { useState } from 'react';
import {
    Alert,
    Anchor,
    Badge,
    Breadcrumbs,
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
} from '@mantine/core';
import {
    IconAlertCircle,
    IconChartBar,
    IconInfoCircle,
    IconTableOff,
} from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
    getDatasetById,
    getDatasetVersions,
    getVersionColumns,
    getVersionRecords,
} from '../datasets/datasetsApi';
import { formatCellValue, getCaseInsensitiveValue } from './formatCellValue';

const PAGE_SIZE_OPTIONS = [
    { value: '20', label: '20 rows / page' },
    { value: '50', label: '50 rows / page' },
    { value: '100', label: '100 rows / page' },
];

export function VersionRecordsPage() {
    const { id: datasetId, versionId } = useParams<{
        id: string;
        versionId: string;
    }>();
    const navigate = useNavigate();

    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState<number>(20);

    const datasetQuery = useQuery({
        queryKey: ['datasets', datasetId],
        queryFn: () => getDatasetById(datasetId!),
        enabled: Boolean(datasetId),
    });

    const versionsQuery = useQuery({
        queryKey: ['datasets', datasetId, 'versions'],
        queryFn: () => getDatasetVersions(datasetId!),
        enabled: Boolean(datasetId),
    });

    const columnsQuery = useQuery({
        queryKey: ['datasets', datasetId, 'versions', versionId, 'columns'],
        queryFn: () => getVersionColumns(datasetId!, versionId!),
        enabled: Boolean(datasetId && versionId),
    });

    const recordsQuery = useQuery({
        queryKey: [
            'datasets',
            datasetId,
            'versions',
            versionId,
            'records',
            { page, pageSize },
        ],
        queryFn: () =>
            getVersionRecords(datasetId!, versionId!, {
                pageNumber: page,
                pageSize,
            }),
        enabled: Boolean(datasetId && versionId),
    });

    const versions = [...(versionsQuery.data ?? [])].sort(
        (a, b) => b.versionNumber - a.versionNumber
    );
    const currentVersion = versions.find((v) => v.id === versionId);
    const columns = columnsQuery.data ?? [];
    const recordsPage = recordsQuery.data;

    const handleVersionChange = (nextVersionId: string | null) => {
        if (!nextVersionId || !datasetId || nextVersionId === versionId) return;
        setPage(1);
        navigate(`/datasets/${datasetId}/versions/${nextVersionId}`);
    };

    const handlePageSizeChange = (nextSize: string | null) => {
        if (!nextSize) return;
        setPageSize(Number(nextSize));
        setPage(1);
    };

    const isLoading = columnsQuery.isLoading || recordsQuery.isLoading;
    const isError = columnsQuery.isError || recordsQuery.isError;

    return (
        <Stack gap="md">
            <Breadcrumbs>
                <Anchor component={Link} to="/datasets" size="sm">
                    Datasets
                </Anchor>
                <Anchor component={Link} to={`/datasets/${datasetId}`} size="sm">
                    {datasetQuery.data?.name || 'Dataset'}
                </Anchor>
                <Text size="sm">
                    {currentVersion ? `Version ${currentVersion.versionNumber}` : 'Records'}
                </Text>
            </Breadcrumbs>

            <Group justify="space-between" align="center" wrap="wrap">
                <div>
                    <Title order={2}>
                        {datasetQuery.data?.name || 'Dataset'} — Data Records
                    </Title>
                    {currentVersion && (
                        <Text size="sm" c="dimmed">
                            File: {currentVersion.sourceFileName} ({currentVersion.fileFormat}) •{' '}
                            {currentVersion.recordCount.toLocaleString()} total records
                        </Text>
                    )}
                </div>

                <Group gap="sm" wrap="wrap">
                    {versions.length > 0 && (
                        <Select
                            aria-label="Select dataset version"
                            value={versionId ?? null}
                            onChange={handleVersionChange}
                            data={versions.map((v) => ({
                                value: v.id,
                                label: `v${v.versionNumber} — ${v.sourceFileName}`,
                            }))}
                            w={240}
                        />
                    )}

                    <Button
                        leftSection={<IconChartBar size={16} />}
                        component={Link}
                        to={`/analytics?datasetId=${datasetId}&versionId=${versionId}`}
                    >
                        Analyze Version
                    </Button>
                </Group>
            </Group>

            <Alert
                icon={<IconInfoCircle size={16} />}
                variant="light"
                color="blue"
            >
                Showing raw imported rows ordered by row number. To filter records,
                group values, or calculate aggregations, open this version in{' '}
                <Anchor
                    component={Link}
                    to={`/analytics?datasetId=${datasetId}&versionId=${versionId}`}
                    fw={600}
                >
                    Analytics Builder
                </Anchor>
                .
            </Alert>

            <Paper withBorder p="md" radius="md">
                <Group justify="space-between" mb="md" wrap="wrap">
                    <Text size="sm" c="dimmed">
                        {recordsPage
                            ? `Showing page ${recordsPage.pageNumber} of ${Math.max(
                                recordsPage.totalPages,
                                1
                            )} (${recordsPage.totalCount.toLocaleString()} records)`
                            : 'Loading records...'}
                    </Text>

                    <Select
                        aria-label="Rows per page"
                        value={String(pageSize)}
                        onChange={handlePageSizeChange}
                        data={PAGE_SIZE_OPTIONS}
                        w={160}
                    />
                </Group>

                {isLoading && (
                    <Center py="xl">
                        <Loader size="lg" />
                    </Center>
                )}

                {isError && (
                    <Alert
                        icon={<IconAlertCircle size={16} />}
                        title="Failed to load table data"
                        color="red"
                        variant="light"
                    >
                        Unable to retrieve columns or records for this dataset version.
                    </Alert>
                )}

                {!isLoading &&
                    !isError &&
                    recordsPage &&
                    recordsPage.items.length === 0 && (
                        <Center py="xl">
                            <Stack align="center" gap="xs">
                                <IconTableOff size={36} stroke={1.5} color="gray" />
                                <Text fw={500}>No records in this version</Text>
                                <Text size="sm" c="dimmed">
                                    This dataset version does not contain any data rows.
                                </Text>
                            </Stack>
                        </Center>
                    )}

                {!isLoading &&
                    !isError &&
                    recordsPage &&
                    recordsPage.items.length > 0 && (
                        <>
                            {/* Horizontal scroll container with sticky header for wide/tall tables */}
                            <Table.ScrollContainer minWidth={700}
                                                   maxHeight={600}
                                                   scrollAreaProps={{ scrollbars: 'xy' }}
                            >
                                <Table
                                    striped
                                    highlightOnHover
                                    withTableBorder
                                    withColumnBorders
                                    stickyHeader
                                    verticalSpacing="xs"
                                >
                                    <Table.Thead>
                                        <Table.Tr>
                                            <Table.Th w={70}>#</Table.Th>
                                            {columns.map((col) => (
                                                <Table.Th key={col.id} miw={140}>
                                                    <Stack gap={2}>
                                                        <Text size="xs" fw={700}>
                                                            {col.name}
                                                        </Text>
                                                        <Badge
                                                            size="xs"
                                                            variant="light"
                                                            color="gray"
                                                            w="fit-content"
                                                        >
                                                            {col.dataType}
                                                        </Badge>
                                                    </Stack>
                                                </Table.Th>
                                            ))}
                                        </Table.Tr>
                                    </Table.Thead>
                                    <Table.Tbody>
                                        {recordsPage.items.map((record) => (
                                            <Table.Tr key={record.id}>
                                                <Table.Td>
                                                    <Text size="xs" c="dimmed" ff="monospace">
                                                        {record.rowNumber}
                                                    </Text>
                                                </Table.Td>
                                                {columns.map((col) => {
                                                    const cellVal = getCaseInsensitiveValue(
                                                        record.data,
                                                        col.name
                                                    );
                                                    return (
                                                        <Table.Td key={col.id}>
                                                            {formatCellValue(cellVal, col.dataType)}
                                                        </Table.Td>
                                                    );
                                                })}
                                            </Table.Tr>
                                        ))}
                                    </Table.Tbody>
                                </Table>
                            </Table.ScrollContainer>

                            {recordsPage.totalPages > 1 && (
                                <Group justify="flex-end" mt="md">
                                    <Pagination
                                        total={recordsPage.totalPages}
                                        value={page}
                                        onChange={setPage}
                                    />
                                </Group>
                            )}
                        </>
                    )}
            </Paper>
        </Stack>
    );
}