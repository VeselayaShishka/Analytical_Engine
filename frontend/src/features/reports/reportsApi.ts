import { apiClient } from '../../api/client';
import type {
    PagedResult,
    ReportDetailDto,
    ReportListItemDto,
    ReportType,
} from '../../api/types';

export interface GetReportsParams {
    pageNumber: number;
    pageSize: number;
    reportType?: ReportType;
    datasetVersionId?: string;
}

export async function getReports(
    params: GetReportsParams
): Promise<PagedResult<ReportListItemDto>> {
    const response = await apiClient.get<PagedResult<ReportListItemDto>>(
        '/reports',
        {
            params: {
                pageNumber: params.pageNumber,
                pageSize: params.pageSize,
                reportType: params.reportType || undefined,
                datasetVersionId: params.datasetVersionId || undefined,
            },
        }
    );
    return response.data;
}

export async function getReportById(id: string): Promise<ReportDetailDto> {
    const response = await apiClient.get<ReportDetailDto>(`/reports/${id}`);
    return response.data;
}

export async function deleteReport(id: string): Promise<void> {
    await apiClient.delete(`/reports/${id}`);
}
