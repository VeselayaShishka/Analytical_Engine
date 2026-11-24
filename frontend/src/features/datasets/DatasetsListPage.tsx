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
    Stack,
    Table,
    Text,
    TextInput,
    Title,
    Tooltip,
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import {
    IconAlertCircle,
    IconDatabase,
    IconEdit,
    IconEye,
    IconPlus,
    IconSearch,
    IconTrash,
    IconUpload,
} from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import type { DatasetResponseDto } from '../../api/types';
import { useAuth } from '../auth/AuthContext';
import { DatasetFormModal } from './DatasetFormModal';
import { getDatasets } from './datasetsApi';
import { DeleteDatasetModal } from './DeleteDatasetModal';

const PAGE_SIZE = 10;

export function DatasetsListPage() {
    const { isAdmin } = useAuth();
    const navigate = useNavigate();

    const [page, setPage] = useState(1);
    const [search, setSearch] = useState('');
    const [debouncedSearch] = useDebouncedValue(search, 300);

    // State controlling Create/Edit and Delete modals
    const [formModalOpen, setFormModalOpen] = useState(false);
    const [editingDataset, setEditingDataset] =
        useState<DatasetResponseDto | null>(null);
    const [deletingDataset, setDeletingDataset] =
        useState<DatasetResponseDto | null>(null);

    const { data, isLoading, isError, error } = useQuery({
        queryKey: ['datasets', { page, pageSize: PAGE_SIZE, search: debouncedSearch }],
        queryFn: () =>
            getDatasets({
                pageNumber: page,
                pageSize: PAGE_SIZE,
                search: debouncedSearch,
            }),
    });

    const handleSearchChange = (value: string) => {
        setSearch(value);
        // Reset pagination to page 1 whenever search query changes
        setPage(1);
    };

    const openCreateModal = () => {
        setEditingDataset(null);
        setFormModalOpen(true);
    };

    const openEditModal = (dataset: DatasetResponseDto) => {
        setEditingDataset(dataset);
        setFormModalOpen(true);
    };

    return (
        <Stack gap="md">
            <Group justify="space-between" align="center" wrap="wrap">
                <div>
                    <Title order={2}>Datasets</Title>
                    <Text c="dimmed" size="sm">
                        Manage open data catalogs, inspect versions, and import files
                    </Text>
                </div>

                <Group>
                    <Button
                        variant="light"
                        leftSection={<IconUpload size={16} />}
                        component={Link}
                        to="/import"
                    >
                        Import File
                    </Button>
                    <Button leftSection={<IconPlus size={16} />} onClick={openCreateModal}>
                        New Dataset
                    </Button>
                </Group>
            </Group>

            <Paper withBorder p="md" radius="md">
                <Group justify="space-between" mb="md" wrap="wrap">
                    <TextInput
                        placeholder="Search datasets by name..."
                        leftSection={<IconSearch size={16} />}
                        value={search}
                        onChange={(e) => handleSearchChange(e.currentTarget.value)}
                        style={{ flex: 1, minWidth: 240, maxWidth: 400 }}
                        aria-label="Search datasets"
                    />

                    {data && (
                        <Text size="sm" c="dimmed">
                            Total: {data.totalCount} dataset{data.totalCount === 1 ? '' : 's'}
                        </Text>
                    )}
                </Group>

                {isLoading && (
                    <Center py="xl">
                        <Loader />
                    </Center>
                )}

                {isError && (
                    <Alert
                        icon={<IconAlertCircle size={16} />}
                        title="Failed to load datasets"
                        color="red"
                        variant="light"
                    >
                        {error instanceof Error ? error.message : 'Unexpected server error.'}
                    </Alert>
                )}

                {!isLoading && !isError && data && data.items.length === 0 && (
                    <Center py="xl">
                        <Stack align="center" gap="xs">
                            <IconDatabase size={36} stroke={1.5} color="gray" />
                            <Text fw={500}>
                                {debouncedSearch
                                    ? 'No datasets match your search'
                                    : 'No datasets yet'}
                            </Text>
                            <Text size="sm" c="dimmed" ta="center">
                                {debouncedSearch
                                    ? 'Try adjusting your search query or clear the filter.'
                                    : 'Create your first dataset or import a CSV/JSON file to get started.'}
                            </Text>
                            {!debouncedSearch && (
                                <Button
                                    mt="xs"
                                    leftSection={<IconPlus size={16} />}
                                    onClick={openCreateModal}
                                >
                                    Create Dataset
                                </Button>
                            )}
                        </Stack>
                    </Center>
                )}

                {!isLoading && !isError && data && data.items.length > 0 && (
                    <>
                        {/* ScrollContainer prevents table clipping on narrow viewports (360px/768px) */}
                        <Table.ScrollContainer minWidth={680}>
                            <Table striped highlightOnHover verticalSpacing="sm">
                                <Table.Thead>
                                    <Table.Tr>
                                        <Table.Th>Name</Table.Th>
                                        <Table.Th>Description</Table.Th>
                                        <Table.Th>Versions</Table.Th>
                                        {isAdmin && <Table.Th>Owner ID</Table.Th>}
                                        <Table.Th>Last Updated</Table.Th>
                                        <Table.Th ta="right">Actions</Table.Th>
                                    </Table.Tr>
                                </Table.Thead>
                                <Table.Tbody>
                                    {data.items.map((dataset) => {
                                        const timestamp = dataset.modifiedAt || dataset.createdAt;
                                        return (
                                            <Table.Tr key={dataset.id}>
                                                <Table.Td>
                                                    <Text
                                                        component={Link}
                                                        to={`/datasets/${dataset.id}`}
                                                        fw={600}
                                                        c="blue"
                                                        style={{ textDecoration: 'none' }}
                                                    >
                                                        {dataset.name}
                                                    </Text>
                                                </Table.Td>
                                                <Table.Td maw={300}>
                                                    <Text size="sm" c="dimmed" lineClamp={1}>
                                                        {dataset.description || '—'}
                                                    </Text>
                                                </Table.Td>
                                                <Table.Td>
                                                    <Badge variant="light" color="teal">
                                                        {dataset.versionsCount}
                                                    </Badge>
                                                </Table.Td>
                                                {isAdmin && (
                                                    <Table.Td>
                                                        <Text size="xs" ff="monospace" c="dimmed">
                                                            {dataset.userId.slice(0, 8)}...
                                                        </Text>
                                                    </Table.Td>
                                                )}
                                                <Table.Td>
                                                    <Text size="sm">
                                                        {new Date(timestamp).toLocaleDateString()}
                                                    </Text>
                                                </Table.Td>
                                                <Table.Td>
                                                    <Group gap={6} justify="flex-end" wrap="nowrap">
                                                        <Tooltip label="Open details & versions">
                                                            <ActionIcon
                                                                variant="subtle"
                                                                color="blue"
                                                                component={Link}
                                                                to={`/datasets/${dataset.id}`}
                                                                aria-label={`View ${dataset.name}`}
                                                            >
                                                                <IconEye size={16} />
                                                            </ActionIcon>
                                                        </Tooltip>
                                                        <Tooltip label="Edit metadata">
                                                            <ActionIcon
                                                                variant="subtle"
                                                                color="gray"
                                                                onClick={() => openEditModal(dataset)}
                                                                aria-label={`Edit ${dataset.name}`}
                                                            >
                                                                <IconEdit size={16} />
                                                            </ActionIcon>
                                                        </Tooltip>
                                                        <Tooltip label="Delete dataset">
                                                            <ActionIcon
                                                                variant="subtle"
                                                                color="red"
                                                                onClick={() => setDeletingDataset(dataset)}
                                                                aria-label={`Delete ${dataset.name}`}
                                                            >
                                                                <IconTrash size={16} />
                                                            </ActionIcon>
                                                        </Tooltip>
                                                    </Group>
                                                </Table.Td>
                                            </Table.Tr>
                                        );
                                    })}
                                </Table.Tbody>
                            </Table>
                        </Table.ScrollContainer>

                        {data.totalPages > 1 && (
                            <Group justify="flex-end" mt="md">
                                <Pagination
                                    total={data.totalPages}
                                    value={page}
                                    onChange={setPage}
                                />
                            </Group>
                        )}
                    </>
                )}
            </Paper>

            <DatasetFormModal
                opened={formModalOpen}
                onClose={() => setFormModalOpen(false)}
                dataset={editingDataset}
                onCreated={(created) => navigate(`/datasets/${created.id}`)}
            />

            <DeleteDatasetModal
                dataset={deletingDataset}
                onClose={() => setDeletingDataset(null)}
            />
        </Stack>
    );
}