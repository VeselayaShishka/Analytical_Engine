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
    Paper,
    Stack,
    Table,
    Text,
    Title,
} from '@mantine/core';
import {
    IconAlertCircle,
    IconChartBar,
    IconEdit,
    IconFileSpreadsheet,
    IconTable,
    IconTrash,
    IconUpload,
} from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { DatasetFormModal } from './DatasetFormModal';
import { getDatasetById, getDatasetVersions } from './datasetsApi';
import { DeleteDatasetModal } from './DeleteDatasetModal';

export function DatasetDetailsPage() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const [editOpen, setEditOpen] = useState(false);
    const [deleteOpen, setDeleteOpen] = useState(false);

    const datasetQuery = useQuery({
        queryKey: ['datasets', id],
        queryFn: () => getDatasetById(id!),
        enabled: Boolean(id),
    });

    const versionsQuery = useQuery({
        queryKey: ['datasets', id, 'versions'],
        queryFn: () => getDatasetVersions(id!),
        enabled: Boolean(id),
    });

    if (datasetQuery.isLoading) {
        return (
            <Center py="xl">
                <Loader size="lg" />
            </Center>
        );
    }

    if (datasetQuery.isError || !datasetQuery.data) {
        return (
            <Alert
                icon={<IconAlertCircle size={16} />}
                title="Dataset Not Found"
                color="red"
                variant="light"
            >
                The requested dataset could not be loaded or you do not have access to
                it.
            </Alert>
        );
    }

    const dataset = datasetQuery.data;
    const versions = [...(versionsQuery.data ?? [])].sort(
        (a, b) => b.versionNumber - a.versionNumber
    );

    return (
        <Stack gap="md">
            <Breadcrumbs>
                <Anchor component={Link} to="/datasets" size="sm">
                    Datasets
                </Anchor>
                <Text size="sm">{dataset.name}</Text>
            </Breadcrumbs>

            {/* Dataset Metadata Card */}
            <Paper withBorder p="lg" radius="md">
                <Group justify="space-between" align="flex-start" wrap="wrap">
                    <Stack gap={4} style={{ flex: 1, minWidth: 250 }}>
                        <Group gap="sm">
                            <Title order={2}>{dataset.name}</Title>
                            <Badge variant="light" color="blue">
                                {versions.length} version{versions.length === 1 ? '' : 's'}
                            </Badge>
                        </Group>
                        <Text c="dimmed" size="sm">
                            {dataset.description || 'No description provided.'}
                        </Text>
                        <Group gap="lg" mt="xs">
                            <Text size="xs" c="dimmed">
                                Created: {new Date(dataset.createdAt).toLocaleString()}
                            </Text>
                            {dataset.modifiedAt && (
                                <Text size="xs" c="dimmed">
                                    Updated: {new Date(dataset.modifiedAt).toLocaleString()}
                                </Text>
                            )}
                        </Group>
                    </Stack>

                    <Group gap="xs" wrap="wrap">
                        <Button
                            leftSection={<IconUpload size={16} />}
                            component={Link}
                            to={`/import?datasetId=${dataset.id}`}
                        >
                            Import New Version
                        </Button>
                        <Button
                            variant="default"
                            leftSection={<IconEdit size={16} />}
                            onClick={() => setEditOpen(true)}
                        >
                            Edit
                        </Button>
                        <Button
                            variant="light"
                            color="red"
                            leftSection={<IconTrash size={16} />}
                            onClick={() => setDeleteOpen(true)}
                        >
                            Delete
                        </Button>
                    </Group>
                </Group>
            </Paper>

            {/* Versions List */}
            <Paper withBorder p="md" radius="md">
                <Title order={4} mb="md">
                    Imported Versions
                </Title>

                {versionsQuery.isLoading && (
                    <Center py="lg">
                        <Loader />
                    </Center>
                )}

                {versionsQuery.isError && (
                    <Alert
                        icon={<IconAlertCircle size={16} />}
                        color="red"
                        title="Failed to load versions"
                    >
                        Could not retrieve version history for this dataset.
                    </Alert>
                )}

                {!versionsQuery.isLoading &&
                    !versionsQuery.isError &&
                    versions.length === 0 && (
                        <Center py="xl">
                            <Stack align="center" gap="xs">
                                <IconFileSpreadsheet size={36} stroke={1.5} color="gray" />
                                <Text fw={500}>No versions imported yet</Text>
                                <Text size="sm" c="dimmed">
                                    Upload a CSV or JSON file to create Version 1 of this dataset.
                                </Text>
                                <Button
                                    mt="xs"
                                    leftSection={<IconUpload size={16} />}
                                    component={Link}
                                    to={`/import?datasetId=${dataset.id}`}
                                >
                                    Import First Version
                                </Button>
                            </Stack>
                        </Center>
                    )}

                {!versionsQuery.isLoading &&
                    !versionsQuery.isError &&
                    versions.length > 0 && (
                        <Table.ScrollContainer minWidth={650}>
                            <Table striped highlightOnHover verticalSpacing="sm">
                                <Table.Thead>
                                    <Table.Tr>
                                        <Table.Th>Version</Table.Th>
                                        <Table.Th>Source File</Table.Th>
                                        <Table.Th>Format</Table.Th>
                                        <Table.Th>Records</Table.Th>
                                        <Table.Th>Imported At</Table.Th>
                                        <Table.Th ta="right">Actions</Table.Th>
                                    </Table.Tr>
                                </Table.Thead>
                                <Table.Tbody>
                                    {versions.map((v) => (
                                        <Table.Tr key={v.id}>
                                            <Table.Td>
                                                <Badge variant="filled" color="indigo">
                                                    v{v.versionNumber}
                                                </Badge>
                                            </Table.Td>
                                            <Table.Td>
                                                <Text size="sm" fw={500}>
                                                    {v.sourceFileName}
                                                </Text>
                                            </Table.Td>
                                            <Table.Td>
                                                <Badge variant="outline" size="sm">
                                                    {v.fileFormat}
                                                </Badge>
                                            </Table.Td>
                                            <Table.Td>
                                                <Text size="sm" fw={500}>
                                                    {v.recordCount.toLocaleString()}
                                                </Text>
                                            </Table.Td>
                                            <Table.Td>
                                                <Text size="sm">
                                                    {new Date(v.createdAt).toLocaleString()}
                                                </Text>
                                            </Table.Td>
                                            <Table.Td>
                                                <Group gap="xs" justify="flex-end" wrap="nowrap">
                                                    <Button
                                                        size="xs"
                                                        variant="light"
                                                        leftSection={<IconTable size={14} />}
                                                        component={Link}
                                                        to={`/datasets/${dataset.id}/versions/${v.id}`}
                                                    >
                                                        View Table
                                                    </Button>
                                                    <Button
                                                        size="xs"
                                                        variant="subtle"
                                                        leftSection={<IconChartBar size={14} />}
                                                        component={Link}
                                                        to={`/analytics?datasetId=${dataset.id}&versionId=${v.id}`}
                                                    >
                                                        Analyze
                                                    </Button>
                                                </Group>
                                            </Table.Td>
                                        </Table.Tr>
                                    ))}
                                </Table.Tbody>
                            </Table>
                        </Table.ScrollContainer>
                    )}
            </Paper>

            <DatasetFormModal
                opened={editOpen}
                onClose={() => setEditOpen(false)}
                dataset={dataset}
            />

            <DeleteDatasetModal
                dataset={deleteOpen ? dataset : null}
                onClose={() => setDeleteOpen(false)}
                onDeleted={() => navigate('/datasets')}
            />
        </Stack>
    );
}