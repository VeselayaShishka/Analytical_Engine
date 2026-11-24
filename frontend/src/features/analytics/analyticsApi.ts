import { apiClient } from '../../api/client';
import type {
    AnalysisQueryRequestDto,
    AnalysisQueryResponseDto,
    CreateReportDto,
    ReportDetailDto,
    StatisticsRequestDto,
    StatisticsResponseDto,
} from '../../api/types';

/**
 * Executes a dynamic filtering, grouping, and aggregation query
 * against a specific dataset version on the backend.
 */
export async function runAnalysisQuery(
    payload: AnalysisQueryRequestDto
): Promise<AnalysisQueryResponseDto> {
    const response = await apiClient.post<AnalysisQueryResponseDto>(
        '/analysis/query',
        payload
    );
    return response.data;
}

/**
 * Computes extended descriptive statistics (mean, median, stdDev, quartiles)
 * for a numeric column, optionally segmented by a grouping column.
 */
export async function runStatisticsQuery(
    payload: StatisticsRequestDto
): Promise<StatisticsResponseDto> {
    const response = await apiClient.post<StatisticsResponseDto>(
        '/analysis/statistics',
        payload
    );
    return response.data;
}

/**
 * Persists an executed analysis or statistics configuration and its results
 * as a reusable report in PostgreSQL.
 */
export async function createReport(
    payload: CreateReportDto
): Promise<ReportDetailDto> {
    const response = await apiClient.post<ReportDetailDto>('/reports', payload);
    return response.data;
}