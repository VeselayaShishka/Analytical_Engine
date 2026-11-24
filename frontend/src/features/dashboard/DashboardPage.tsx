import {
    Alert,
    Badge,
    Button,
    Center,
    Group,
    Loader,
    Paper,
    SimpleGrid,
    Stack,
    Table,
    Text,
    ThemeIcon,
    Title,
} from '@mantine/core';
import {
    IconAlertCircle,
    IconChartBar,
    IconDatabase,
    IconFileAnalytics,
    IconListNumbers,
    IconStack2,
    IconUpload,
} from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import type { ComponentType } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext.tsx';
import { getDashboardSummary } from './dashboardApi';

function StatCard({
                      label, value, icon: Icon, color,
                  }: {
    label: string;
    value: number;
    icon: ComponentType<{ size?: number }>;
    color: string;
}) {
    return (
        <Paper withBorder p="md" radius="md">
            <Group justify="space-between" wrap="nowrap">
                <div>
                    <Text size="xs" c="dimmed" tt="uppercase" fw={700}>{label}</Text>
                    <Text fz={28} fw={700} lh={1.2}>{value.toLocaleString()}</Text>
                </div>
                <ThemeIcon variant="light" color={color} size={44} radius="md">
                    <Icon size={24} />
                </ThemeIcon>
            </Group>
        </Paper>
    );
}

export function DashboardPage() {
    const { user, isAdmin } = useAuth();

    const { data, isLoading, isError, error } = useQuery({
        queryKey: ['dashboard-summary'],
        queryFn: getDashboardSummary,
    });

    return (
        <Stack gap="md">
            <Group justify="space-between" align="center" wrap="wrap">
                <div>
                    <Title order={2}>Dashboard</Title>
                    <Text c="dimmed" size="sm">
                        {isAdmin ? 'Overview of all datasets and reports' : 'Overview of your datasets and reports'}
                        {user?.email ? ` · ${user.email}` : ''}
                    </Text>
                </div>
                <Group>
                    <Button variant="light" leftSection={<IconUpload size={16} />} component={Link} to="/import">
                        Import File
                    </Button>
                    <Button leftSection={<IconChartBar size={16} />} component={Link} to="/analytics">
                        New Analysis
                    </Button>
                </Group>
            </Group>

            {isLoading && <Center py="xl"><Loader /></Center>}

            {isError && (
                <Alert icon={<IconAlertCircle size={16} />} color="red" variant="light" title="Failed to load dashboard">
                    {error instanceof Error ? error.message : 'Unexpected server error.'}
                </Alert>
            )}

            {data && (
                <>
                    <SimpleGrid cols={{ base: 1, xs: 2, md: 4 }}>
                        <StatCard label="Datasets" value={data.totalDatasets} icon={IconDatabase} color="blue" />
                        <StatCard label="Versions" value={data.totalVersions} icon={IconStack2} color="teal" />
                        <StatCard label="Records" value={data.totalRecords} icon={IconListNumbers} color="grape" />
                        <StatCard label="Reports" value={data.totalReports} icon={IconFileAnalytics} color="orange" />
                    </SimpleGrid>

                    <SimpleGrid cols={{ base: 1, md: 2 }} spacing="md">
                        <Paper withBorder p="md" radius="md">
                            <Group justify="space-between" mb="sm">
                                <Title order={4}>Recent datasets</Title>
                                <Button variant="subtle" size="xs" component={Link} to="/datasets">View all</Button>
                            </Group>
                            {data.recentDatasets.length === 0 ? (
                                <Stack align="center" gap="xs" py="md">
                                    <Text c="dimmed" size="sm">No datasets yet.</Text>
                                    <Button size="xs" variant="light" component={Link} to="/datasets">Create a dataset</Button>
                                </Stack>
                            ) : (
                                <Table verticalSpacing="xs" highlightOnHover>
                                    <Table.Tbody>
                                        {data.recentDatasets.map((d) => (
                                            <Table.Tr key={d.id}>
                                                <Table.Td>
                                                    <Text component={Link} to={`/datasets/${d.id}`} size="sm" fw={500}>
                                                        {d.name}
                                                    </Text>
                                                    <Text size="xs" c="dimmed">
                                                        {new Date(d.modifiedAt ?? d.createdAt).toLocaleDateString()}
                                                    </Text>
                                                </Table.Td>
                                                <Table.Td ta="right">
                                                    <Badge variant="light" color="gray">
                                                        {d.versionsCount} version{d.versionsCount === 1 ? '' : 's'}
                                                    </Badge>
                                                </Table.Td>
                                            </Table.Tr>
                                        ))}
                                    </Table.Tbody>
                                </Table>
                            )}
                        </Paper>

                        <Paper withBorder p="md" radius="md">
                            <Group justify="space-between" mb="sm">
                                <Title order={4}>Recent reports</Title>
                                <Button variant="subtle" size="xs" component={Link} to="/reports">View all</Button>
                            </Group>
                            {data.recentReports.length === 0 ? (
                                <Stack align="center" gap="xs" py="md">
                                    <Text c="dimmed" size="sm">No saved reports yet.</Text>
                                    <Button size="xs" variant="light" component={Link} to="/analytics">Run an analysis</Button>
                                </Stack>
                            ) : (
                                <Table verticalSpacing="xs" highlightOnHover>
                                    <Table.Tbody>
                                        {data.recentReports.map((r) => (
                                            <Table.Tr key={r.id}>
                                                <Table.Td>
                                                    <Text component={Link} to={`/reports/${r.id}`} size="sm" fw={500}>
                                                        {r.name}
                                                    </Text>
                                                    <Text size="xs" c="dimmed">
                                                        {new Date(r.createdAt).toLocaleDateString()}
                                                    </Text>
                                                </Table.Td>
                                                <Table.Td ta="right">
                                                    <Badge variant="light">{r.reportType}</Badge>
                                                </Table.Td>
                                            </Table.Tr>
                                        ))}
                                    </Table.Tbody>
                                </Table>
                            )}
                        </Paper>
                    </SimpleGrid>
                </>
            )}
        </Stack>
    );
}
