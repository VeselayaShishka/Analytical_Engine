import { apiClient } from '../../api/client';
import type { ImportResultDto } from '../../api/types';
import type { InferredColumn } from './fileParser';

export interface UploadDatasetFileParams {
    datasetId: string;
    file: File;
    format: 'CSV' | 'JSON';
    columns: InferredColumn[];
    onProgress?: (percent: number) => void;
}

/**
 * Uploads a CSV or JSON file to POST /api/import/dataset using multipart/form-data.
 * Serializes the column schema in the exact { fields: { col: { type, isNullable, description } } }
 * structure expected by the backend Schema deserializer.
 */
export async function uploadDatasetFile({
                                            datasetId,
                                            file,
                                            format,
                                            columns,
                                            onProgress,
                                        }: UploadDatasetFileParams): Promise<ImportResultDto> {
    const formData = new FormData();
    formData.append('datasetId', datasetId);
    formData.append('file', file);
    formData.append('format', format);

    const fieldsMap: Record<
        string,
        { type: string; isNullable: boolean; description: string | null }
    > = {};

    for (const col of columns) {
        fieldsMap[col.name] = {
            type: col.dataType,
            isNullable: col.isNullable,
            description: null,
        };
    }

    formData.append('schemaJson', JSON.stringify({ fields: fieldsMap }));

    const response = await apiClient.post<ImportResultDto>(
        '/import/dataset',
        formData,
        {
            onUploadProgress: (progressEvent) => {
                if (!onProgress || !progressEvent.total) return;
                const percent = Math.min(
                    100,
                    Math.round((progressEvent.loaded * 100) / progressEvent.total)
                );
                onProgress(percent);
            },
        }
    );

    return response.data;
}