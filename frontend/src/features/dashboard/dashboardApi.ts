import { apiClient } from '../../api/client';
import type { DashboardSummaryDto } from '../../api/types';

export async function getDashboardSummary(): Promise<DashboardSummaryDto> {
    const response = await apiClient.get<DashboardSummaryDto>(
        '/dashboard/summary'
    );
    return response.data;
}
