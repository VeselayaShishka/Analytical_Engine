import axios from 'axios';
import type { ImportResultDto, ImportRowErrorDto, ProblemDetails } from './types';

export class ApiError extends Error {
    public readonly status: number;
    public readonly title: string;
    public readonly detail: string;
    public readonly fieldErrors: Record<string, string[]>;
    public readonly importErrors: ImportRowErrorDto[];

    constructor(params: {
        status: number;
        title: string;
        detail: string;
        fieldErrors?: Record<string, string[]>;
        importErrors?: ImportRowErrorDto[];
    }) {
        super(params.detail || params.title);
        this.name = 'ApiError';
        this.status = params.status;
        this.title = params.title;
        this.detail = params.detail;
        this.fieldErrors = params.fieldErrors ?? {};
        this.importErrors = params.importErrors ?? [];
    }
}

function normalizeFieldKeys(
    errors?: Record<string, string[]>
): Record<string, string[]> {
    if (!errors) return {};

    const normalized: Record<string, string[]> = {};
    for (const [key, messages] of Object.entries(errors)) {
        const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
        normalized[camelKey] = messages;
    }
    return normalized;
}

export function normalizeApiError(error: unknown): ApiError {
    if (error instanceof ApiError) {
        return error;
    }

    if (axios.isAxiosError(error)) {
        if (!error.response) {
            return new ApiError({
                status: 0,
                title: 'Network Error',
                detail: 'Unable to reach the server. Please check your connection.',
            });
        }

        const status = error.response.status;
        const data = error.response.data as unknown;

        if (typeof data === 'string' && data.trim().length > 0) {
            return new ApiError({
                status,
                title: error.response.statusText || 'Request Error',
                detail: data,
            });
        }

        if (data && typeof data === 'object') {
            // Проверка формата ImportResultDto (ошибки строк при импорте)
            if ('errors' in data && Array.isArray((data as ImportResultDto).errors)) {
                const importResult = data as ImportResultDto;
                return new ApiError({
                    status,
                    title: 'Import Validation Failed',
                    detail: importResult.message || 'Some rows in the file contain errors.',
                    importErrors: importResult.errors,
                });
            }

            // Формат RFC 9457 ProblemDetails / ValidationProblemDetails
            const problem = data as ProblemDetails;
            return new ApiError({
                status,
                title: problem.title || error.response.statusText || 'Request Error',
                detail:
                    problem.detail ||
                    problem.title ||
                    'An unexpected error occurred while processing the request.',
                fieldErrors: normalizeFieldKeys(problem.errors),
            });
        }

        return new ApiError({
            status,
            title: error.response.statusText || 'HTTP Error',
            detail: `Server returned status code ${status}.`,
        });
    }

    return new ApiError({
        status: 500,
        title: 'Unexpected Error',
        detail: error instanceof Error ? error.message : 'Unknown error occurred.',
    });
}