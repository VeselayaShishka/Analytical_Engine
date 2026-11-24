import type { ReactNode } from 'react';
import { Badge, Text } from '@mantine/core';
import type { ColumnDataType } from '../../api/types';

/**
 * Looks up a property value inside a record's JSON dictionary without
 * regard to key casing (e.g., matches "Amount" against "amount").
 */
export function getCaseInsensitiveValue(
    recordData: Record<string, unknown>,
    columnName: string
): unknown {
    if (Object.prototype.hasOwnProperty.call(recordData, columnName)) {
        return recordData[columnName];
    }

    const targetLower = columnName.toLowerCase();
    for (const key of Object.keys(recordData)) {
        if (key.toLowerCase() === targetLower) {
            return recordData[key];
        }
    }

    return undefined;
}

/**
 * Formats a raw cell value according to its schema DataType for table rendering.
 */
export function formatCellValue(
    rawValue: unknown,
    dataType: ColumnDataType
): ReactNode {
    if (rawValue === null || rawValue === undefined) {
        return (
            <Text span c="dimmed" fs="italic" size="xs">
                null
            </Text>
        );
    }

    const normalizedType = dataType.toLowerCase();

    if (
        normalizedType === 'int' ||
        normalizedType === 'integer' ||
        normalizedType === 'long' ||
        normalizedType === 'decimal' ||
        normalizedType === 'double'
    ) {
        const numeric =
            typeof rawValue === 'number' ? rawValue : Number(String(rawValue));
        if (!Number.isNaN(numeric)) {
            return (
                <Text span ff="monospace" size="sm">
                    {numeric.toLocaleString(undefined, {
                        maximumFractionDigits: 6,
                    })}
                </Text>
            );
        }
    }

    if (normalizedType === 'bool' || normalizedType === 'boolean') {
        const boolVal =
            typeof rawValue === 'boolean'
                ? rawValue
                : String(rawValue).toLowerCase() === 'true';
        return (
            <Badge size="xs" variant="light" color={boolVal ? 'green' : 'gray'}>
                {boolVal ? 'true' : 'false'}
            </Badge>
        );
    }

    if (normalizedType === 'datetime') {
        const parsedDate = new Date(String(rawValue));
        if (!Number.isNaN(parsedDate.getTime())) {
            return (
                <Text span size="sm">
                    {parsedDate.toLocaleString()}
                </Text>
            );
        }
    }

    return (
        <Text span size="sm">
            {String(rawValue)}
        </Text>
    );
}