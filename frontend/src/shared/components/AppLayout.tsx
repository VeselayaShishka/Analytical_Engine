import {
    AppShell,
    Badge,
    Burger,
    Button,
    Divider,
    Group,
    NavLink,
    ScrollArea,
    Stack,
    Text,
    Title,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import {
    IconChartBar,
    IconDatabase,
    IconFileAnalytics,
    IconLayoutDashboard,
    IconLogout,
    IconUpload,
} from '@tabler/icons-react';
import { Link, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../../features/auth/AuthContext';

const NAV_ITEMS = [
    { label: 'Dashboard', to: '/', icon: IconLayoutDashboard, exact: true },
    { label: 'Datasets', to: '/datasets', icon: IconDatabase, exact: false },
    { label: 'Import Data', to: '/import', icon: IconUpload, exact: false },
    {
        label: 'Analytics Builder',
        to: '/analytics',
        icon: IconChartBar,
        exact: false,
    },
    { label: 'Reports', to: '/reports', icon: IconFileAnalytics, exact: false },
];

export function AppLayout() {
    const [opened, { toggle, close }] = useDisclosure();
    const { user, isAdmin, logout } = useAuth();
    const location = useLocation();

    const isRouteActive = (to: string, exact: boolean) => {
        if (exact) return location.pathname === to;
        return (
            location.pathname === to || location.pathname.startsWith(`${to}/`)
        );
    };

    return (
        <AppShell
            header={{ height: 60 }}
            navbar={{
                width: 250,
                breakpoint: 'sm',
                collapsed: { mobile: !opened },
            }}
            padding="md"
        >
            <AppShell.Header>
                <Group h="100%" px="md" justify="space-between" wrap="nowrap">
                    <Group gap="sm" wrap="nowrap">
                        <Burger
                            opened={opened}
                            onClick={toggle}
                            hiddenFrom="sm"
                            size="sm"
                            aria-label="Toggle navigation"
                        />
                        <Title order={4} lineClamp={1}>
                            Analytical Engine
                        </Title>
                    </Group>

                    <Group gap="sm" wrap="nowrap">
                        {user && (
                            <>
                                <Text size="sm" c="dimmed" visibleFrom="xs" lineClamp={1}>
                                    {user.email}
                                </Text>
                                <Badge
                                    color={isAdmin ? 'grape' : 'blue'}
                                    variant="light"
                                    size="sm"
                                >
                                    {isAdmin ? 'Admin' : 'Analyst'}
                                </Badge>
                            </>
                        )}
                        <Button
                            variant="subtle"
                            color="gray"
                            size="xs"
                            leftSection={<IconLogout size={16} />}
                            onClick={() => logout('manual')}
                        >
                            Logout
                        </Button>
                    </Group>
                </Group>
            </AppShell.Header>

            <AppShell.Navbar p="sm">
                <AppShell.Section grow component={ScrollArea}>
                    <Stack gap={4}>
                        {NAV_ITEMS.map((item) => {
                            const Icon = item.icon;
                            return (
                                <NavLink
                                    key={item.to}
                                    component={Link}
                                    to={item.to}
                                    label={item.label}
                                    leftSection={<Icon size={18} stroke={1.75} />}
                                    active={isRouteActive(item.to, item.exact)}
                                    onClick={close}
                                />
                            );
                        })}
                    </Stack>
                </AppShell.Section>

                <AppShell.Section>
                    <Divider my="xs" />
                    <Text size="xs" c="dimmed" px="xs">
                        Open Data Analytics Platform
                    </Text>
                </AppShell.Section>
            </AppShell.Navbar>

            <AppShell.Main>
                <Outlet />
            </AppShell.Main>
        </AppShell>
    );
}