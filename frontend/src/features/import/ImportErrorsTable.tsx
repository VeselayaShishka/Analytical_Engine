import { useMemo, useState } from 'react';
import {
    Alert,
    Badge,
    Group,
    List,
    Paper,
    Stack,
    Table,
    Text,
    TextInput,
} from '@mantine/core';
import { IconAlertTriangle, IconSearch } from '@tabler/icons-react';
import type { ImportRowErrorDto } from '../../api/types';

interface ImportErrorsTableProps {
    errors: ImportRowErrorDto[];
    title?: string;
    summaryMessage?: string;
}

export function ImportErrorsTable({
                                      errors,
                                      title = 'Row-Level Validation Errors',
                                      summaryMessage,
                                  }: ImportErrorsTableProps) {
    const [filterText, setFilterText] = useState('');

    const filteredErrors = useMemo(() => {
        const query = filterText.trim().toLowerCase();
        if (!query) return errors;

        return errors.filter(
            (item) =>
                String(item.rowNumber).includes(query) ||
                item.errorMessages.some((msg) => msg.toLowerCase().includes(query))
        );
    }, [errors, filterText]);

    if (errors.length === 0) {
        return null;
    }

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="md">
                <Alert
                    icon={<IconAlertTriangle size={18} />}
                    color="orange"
                    variant="light"
                    title={`${title} (${errors.length} row${errors.length === 1 ? '' : 's'})`}
                >
                    {summaryMessage ||
                        'The following rows failed schema or data-type validation. Fix the issues in your source file and re-import.'}
                </Alert>

                <Group justify="space-between" wrap="wrap">
                    <TextInput
                        placeholder="Filter by row number or error text..."
                        leftSection={<IconSearch size={16} />}
                        value={filterText}
                        onChange={(e) => setFilterText(e.currentTarget.value)}
                        style={{ flex: 1, minWidth: 240, maxWidth: 400 }}
                        aria-label="Filter import errors"
                    />
                    <Text size="sm" c="dimmed">
                        Showing {filteredErrors.length} of {errors.length} error rows
                    </Text>
                </Group>

                <Table.ScrollContainer minWidth={520} mah={400}>
                    <Table striped highlightOnHover withTableBorder stickyHeader>
                        <Table.Thead>
                            <Table.Tr>
                                <Table.Th w={130}>Row Number</Table.Th>
                                <Table.Th>Validation Error Details</Table.Th>
                            </Table.Tr>
                        </Table.Thead>
                        <Table.Tbody>
                            {filteredErrors.map((rowErr) => (
                                <Table.Tr key={rowErr.rowNumber}>
                                    <Table.Td>
                                        <Badge color="red" variant="light" ff="monospace">
                                            Row #{rowErr.rowNumber}
                                        </Badge>
                                    </Table.Td>
                                    <Table.Td>
                                        {rowErr.errorMessages.length === 1 ? (
                                            <Text size="sm">{rowErr.errorMessages[0]}</Text>
                                        ) : (
                                            <List size="sm" spacing={2}>
                                                {rowErr.errorMessages.map((msg, idx) => (
                                                    <List.Item key={idx}>{msg}</List.Item>
                                                ))}
                                            </List>
                                        )}
                                    </Table.Td>
                                </Table.Tr>
                            ))}
                        </Table.Tbody>
                    </Table>
                </Table.ScrollContainer>
            </Stack>
        </Paper>
    );
}