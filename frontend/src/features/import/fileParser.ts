import Papa from 'papaparse';
import type { ColumnDataType } from '../../api/types';

export interface InferredColumn {
    name: string;
    dataType: ColumnDataType;
    isNullable: boolean;
    position: number;
}

export interface ParsedFilePreview {
    format: 'CSV' | 'JSON';
    columns: InferredColumn[];
    previewRows: Record<string, unknown>[];
    sampledRowCount: number;
    detectedDelimiter?: string;
}

const MAX_SAMPLE_ROWS = 100;
const PREVIEW_DISPLAY_ROWS = 5;

/**
 * Validates that column headers are non-empty and unique (case-insensitive).
 * Throws an Error with a user-friendly explanation if validation fails.
 */
function validateHeaders(rawHeaders: string[]): string[] {
    if (!rawHeaders || rawHeaders.length === 0) {
        throw new Error('File header is missing or the file is empty.');
    }

    const normalized: string[] = [];
    const seenLower = new Set<string>();

    for (const raw of rawHeaders) {
        const trimmed = raw.trim();
        if (!trimmed) {
            throw new Error('File contains an empty column header.');
        }

        const lower = trimmed.toLowerCase();
        if (seenLower.has(lower)) {
            throw new Error(`Duplicate column header detected: "${trimmed}".`);
        }

        seenLower.add(lower);
        normalized.push(trimmed);
    }

    return normalized;
}

/**
 * Infers the most specific ColumnDataType for a column based on sample values.
 * Supports both dot (.) and comma (,) decimal separators for numeric values.
 */
function inferColumnType(values: unknown[]): {
    dataType: ColumnDataType;
    isNullable: boolean;
} {
    let isNullable = false;
    const nonNullValues: string[] = [];

    for (const val of values) {
        if (val === null || val === undefined || String(val).trim() === '') {
            isNullable = true;
        } else {
            nonNullValues.push(String(val).trim());
        }
    }

    if (nonNullValues.length === 0) {
        return { dataType: 'string', isNullable: true };
    }

    const isAllBoolean = nonNullValues.every((v) =>
        ['true', 'false'].includes(v.toLowerCase())
    );
    if (isAllBoolean) {
        return { dataType: 'boolean', isNullable };
    }

    const isAllInteger = nonNullValues.every((v) => /^-?\d+$/.test(v));
    if (isAllInteger) {
        return { dataType: 'integer', isNullable };
    }

    // Match dot-separated decimal numbers only (aligned with backend InvariantCulture)
    const isAllDouble = nonNullValues.every((v) => /^-?\d+(\.\d+)?$/.test(v));
    if (isAllDouble) {
        return { dataType: 'double', isNullable };
    }

    // Match ISO 8601 date/datetime strings only (yyyy-MM-dd)
    const isAllDateTime = nonNullValues.every(
        (v) => /^\d{4}-\d{2}-\d{2}/.test(v) && !Number.isNaN(Date.parse(v))
    );
    if (isAllDateTime) {
        return { dataType: 'datetime', isNullable };
    }

    return { dataType: 'string', isNullable };
}

/**
 * Builds inferred column definitions from validated header names and sample rows.
 */
function buildInferredColumns(
    headers: string[],
    rows: Record<string, unknown>[]
): InferredColumn[] {
    return headers.map((header, index) => {
        const columnValues = rows.map((row) => row[header]);
        const { dataType, isNullable } = inferColumnType(columnValues);

        return {
            name: header,
            dataType,
            isNullable,
            position: index,
        };
    });
}

/**
 * Parses a CSV file on the client using PapaParse to validate headers,
 * infer column types, and extract preview rows before uploading.
 */
function parseCsvPreview(file: File): Promise<ParsedFilePreview> {
    return new Promise((resolve, reject) => {
        Papa.parse<Record<string, unknown>>(file, {
            header: true,
            skipEmptyLines: 'greedy',
            preview: MAX_SAMPLE_ROWS,
            delimitersToGuess: [','],
            complete: (results) => {
                try {
                    const rawHeaders = results.meta.fields ?? [];
                    const headers = validateHeaders(rawHeaders);

                    if (!results.data || results.data.length === 0) {
                        throw new Error(
                            'CSV file contains headers but no data rows to import.'
                        );
                    }

                    // Normalize row keys to trimmed header names
                    const normalizedRows = results.data.map((rawRow) => {
                        const cleanRow: Record<string, unknown> = {};
                        for (const header of headers) {
                            const value = rawRow[header];
                            cleanRow[header] =
                                value === undefined ||
                                value === null ||
                                String(value).trim() === ''
                                    ? null
                                    : value;
                        }
                        return cleanRow;
                    });

                    const columns = buildInferredColumns(headers, normalizedRows);

                    resolve({
                        format: 'CSV',
                        columns,
                        previewRows: normalizedRows.slice(0, PREVIEW_DISPLAY_ROWS),
                        sampledRowCount: normalizedRows.length,
                        detectedDelimiter: results.meta.delimiter,
                    });
                } catch (err) {
                    reject(err);
                }
            },
            error: (err) => {
                reject(new Error(`Failed to parse CSV file: ${err.message}`));
            },
        });
    });
}

/**
 * Parses a JSON file on the client, supporting top-level arrays of objects
 * as well as wrapped objects containing a `records`, `items`, or `data` array.
 */
async function parseJsonPreview(file: File): Promise<ParsedFilePreview> {
    const text = await file.text();
    if (!text.trim()) {
        throw new Error('JSON file is empty.');
    }

    let parsed: unknown;
    try {
        parsed = JSON.parse(text);
    } catch {
        throw new Error('Invalid JSON syntax. Please check the file structure.');
    }

    let recordsArray: unknown[] | null = null;
    if (Array.isArray(parsed)) {
        recordsArray = parsed;
    } else if (parsed && typeof parsed === 'object') {
        const obj = parsed as Record<string, unknown>;
        const candidate = obj.records ?? obj.items ?? obj.data;
        if (Array.isArray(candidate)) {
            recordsArray = candidate;
        }
    }

    if (!recordsArray || recordsArray.length === 0) {
        throw new Error(
            'JSON file must contain a non-empty array of data objects.'
        );
    }

    const firstItem = recordsArray[0];
    if (!firstItem || typeof firstItem !== 'object' || Array.isArray(firstItem)) {
        throw new Error('Each item in the JSON array must be a key-value object.');
    }

    // Collect union of all keys across the sampled rows
    const sampleSlice = recordsArray.slice(0, MAX_SAMPLE_ROWS);
    const rawKeys = new Set<string>();
    for (const item of sampleSlice) {
        if (item && typeof item === 'object' && !Array.isArray(item)) {
            for (const key of Object.keys(item)) {
                rawKeys.add(key);
            }
        }
    }

    const headers = validateHeaders(Array.from(rawKeys));

    const normalizedRows = sampleSlice.map((item) => {
        const recordObj = (item as Record<string, unknown>) ?? {};
        const cleanRow: Record<string, unknown> = {};
        for (const header of headers) {
            const val = recordObj[header];
            cleanRow[header] = val === undefined ? null : val;
        }
        return cleanRow;
    });

    const columns = buildInferredColumns(headers, normalizedRows);

    return {
        format: 'JSON',
        columns,
        previewRows: normalizedRows.slice(0, PREVIEW_DISPLAY_ROWS),
        sampledRowCount: normalizedRows.length,
    };
}

/**
 * Entry point for client-side file validation and schema inference.
 */
export async function parseFileForPreview(
    file: File
): Promise<ParsedFilePreview> {
    const lowerName = file.name.toLowerCase();

    if (lowerName.endsWith('.csv')) {
        return parseCsvPreview(file);
    }

    if (lowerName.endsWith('.json')) {
        return parseJsonPreview(file);
    }

    throw new Error('Unsupported file format. Please upload a .csv or .json file.');
}