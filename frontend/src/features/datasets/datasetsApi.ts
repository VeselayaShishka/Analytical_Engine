import { apiClient } from '../../api/client';
import type {
    CreateDatasetDto,
    DataRecordResponseDto,
    DatasetColumnResponseDto,
    DatasetResponseDto,
    DatasetVersionResponseDto,
    PagedResult,
    UpdateDatasetDto,
} from '../../api/types';

export interface GetDatasetsParams {
    pageNumber: number;
    pageSize: number;
    search?: string;
}

export interface GetRecordsParams {
    pageNumber: number;
    pageSize: number;
}

/**
 * Helper to normalize responses that may arrive either as a raw array
 * or wrapped inside a PagedResult object.
 */
function unwrapList<T>(payload: T[] | PagedResult<T>): T[] {
    if (Array.isArray(payload)) {
        return payload;
    }
    return payload?.items ?? [];
}

/**
 * Fetches a paginated and optionally filtered list of datasets.
 */
export async function getDatasets(
    params: GetDatasetsParams
): Promise<PagedResult<DatasetResponseDto>> {
    const response = await apiClient.get<PagedResult<DatasetResponseDto>>(
        '/datasets',
        {
            params: {
                pageNumber: params.pageNumber,
                pageSize: params.pageSize,
                search: params.search?.trim() || undefined,
            },
        }
    );
    return response.data;
}

/**
 * Fetches metadata for a single dataset by its ID.
 */
export async function getDatasetById(
    datasetId: string
): Promise<DatasetResponseDto> {
    const response = await apiClient.get<DatasetResponseDto>(
        `/datasets/${datasetId}`
    );
    return response.data;
}

/**
 * Creates a new dataset container. Throws ApiError (409) if name already exists.
 */
export async function createDataset(
    payload: CreateDatasetDto
): Promise<DatasetResponseDto> {
    const response = await apiClient.post<DatasetResponseDto>(
        '/datasets',
        payload
    );
    return response.data;
}

/**
 * Updates an existing dataset's name and description.
 */
export async function updateDataset(
    datasetId: string,
    payload: UpdateDatasetDto
): Promise<DatasetResponseDto> {
    const response = await apiClient.put<DatasetResponseDto>(
        `/datasets/${datasetId}`,
        payload
    );
    return response.data;
}

/**
 * Deletes a dataset along with all its versions, records, and reports.
 */
export async function deleteDataset(datasetId: string): Promise<void> {
    await apiClient.delete(`/datasets/${datasetId}`);
}

/**
 * Fetches all imported versions belonging to a specific dataset.
 */
export async function getDatasetVersions(
    datasetId: string
): Promise<DatasetVersionResponseDto[]> {
    const response = await apiClient.get<
        DatasetVersionResponseDto[] | PagedResult<DatasetVersionResponseDto>
    >(`/datasets/${datasetId}/versions`);
    return unwrapList(response.data);
}

/**
 * Fetches column schema definitions for a specific dataset version,
 * sorted strictly by their defined Position index.
 */
export async function getVersionColumns(
    datasetId: string,
    versionId: string
): Promise<DatasetColumnResponseDto[]> {
    const response = await apiClient.get<
        DatasetColumnResponseDto[] | PagedResult<DatasetColumnResponseDto>
    >(`/datasets/${datasetId}/versions/${versionId}/columns`);

    const columns = unwrapList(response.data);
    return [...columns].sort((a, b) => a.position - b.position);
}

/**
 * Fetches paginated data records for a specific dataset version.
 */
export async function getVersionRecords(
    datasetId: string,
    versionId: string,
    params: GetRecordsParams
): Promise<PagedResult<DataRecordResponseDto>> {
    const response = await apiClient.get<PagedResult<DataRecordResponseDto>>(
        `/datasets/${datasetId}/versions/${versionId}/records`,
        {
            params: {
                pageNumber: params.pageNumber,
                pageSize: params.pageSize,
            },
        }
    );
    return response.data;
}