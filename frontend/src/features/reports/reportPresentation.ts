import type {
    AnalysisQueryRequestDto,
    AnalysisQueryResponseDto,
    ReportEnvelopeDto,
    StatisticsRequestDto,
    StatisticsResponseDto,
} from '../../api/types';
import type { ChartDataPoint } from '../analytics/AnalysisChartPanel.tsx';
import {
    generateAggregationNarrative,
    generateStatisticsNarrative,
} from '../analytics/narrativeGenerator.ts';

export interface ReportPresentation {
    isEmpty: boolean;
    title: string;
    subtitle: string;
    dataPoints: ChartDataPoint[];
    metricKeys: string[];
    narrative: string;
    recordsProcessed: number;
    defaultMetric: string;
}

function isStatisticsRequest(
    req: AnalysisQueryRequestDto | StatisticsRequestDto
): req is StatisticsRequestDto {
    return 'column' in req && typeof req.column === 'string';
}

/**
 * Rebuilds chart data and narrative from the stored report envelope
 * ({ request, result }) exactly like the analytics builder does for live results.
 */
export function buildReportPresentation(
    envelope: ReportEnvelopeDto | undefined | null
): ReportPresentation | null {
    if (!envelope || !envelope.request || !envelope.result) return null;

    const { request } = envelope;
    // Stored result is the backend AnalysisResultDto; "column" is absent for statistics.
    const result = envelope.result as Partial<AnalysisQueryResponseDto>;
    const rows: unknown[] = Array.isArray(result.result) ? result.result : [];
    const statsColumn = isStatisticsRequest(request)
        ? request.column
        : 'Target Column';

    if (isStatisticsRequest(request)) {
        // Backend result has no "column" field, so take it from the stored request.
        const stats: StatisticsResponseDto = {
            recordsProcessed: result.recordsProcessed ?? 0,
            result: rows as StatisticsResponseDto['result'],
        };
        const groupBy = request.groupBy || null;

        if (!groupBy && stats.result.length === 1) {
            const s = stats.result[0];
            return {
                isEmpty: stats.recordsProcessed === 0,
                title: `Distribution Summary: ${statsColumn}`,
                subtitle: `Count: ${s.count} | Nulls: ${s.nullCount} | StdDev: ${
                    s.stdDev?.toFixed(4) ?? 'N/A'
                } | IQR: ${s.iqr?.toFixed(4) ?? 'N/A'}`,
                dataPoints: [
                    { label: 'Min', metrics: { Value: s.min } },
                    { label: 'Q1 (25%)', metrics: { Value: s.q1 } },
                    { label: 'Median (50%)', metrics: { Value: s.median } },
                    { label: 'Mean', metrics: { Value: s.mean } },
                    { label: 'Q3 (75%)', metrics: { Value: s.q3 } },
                    { label: 'Max', metrics: { Value: s.max } },
                ],
                metricKeys: ['Value'],
                narrative: generateStatisticsNarrative(stats, statsColumn,null),
                recordsProcessed: stats.recordsProcessed,
                defaultMetric: 'Value',
            };
        }

        const metricKeys = [
            'Mean', 'Median', 'Sum', 'Min', 'Max', 'StdDev', 'IQR', 'Count',
        ];
        return {
            isEmpty: stats.recordsProcessed === 0 || stats.result.length === 0,
            title: groupBy
                ? `Statistics for "${statsColumn}" by ${groupBy}`
                : `Statistics for "${statsColumn}"`,
            subtitle: 'Compare central tendency and spread across groups',
            dataPoints: stats.result.map((s) => ({
                label: s.key ?? '(Null)',
                metrics: {
                    Mean: s.mean,
                    Median: s.median,
                    Sum: s.sum,
                    Min: s.min,
                    Max: s.max,
                    StdDev: s.stdDev,
                    IQR: s.iqr,
                    Count: s.count,
                },
            })),
            metricKeys,
            narrative: generateStatisticsNarrative(stats, statsColumn, groupBy),
            recordsProcessed: stats.recordsProcessed,
            defaultMetric: 'Mean',
        };
    }

    // Aggregation / Grouping
    const query = request as AnalysisQueryRequestDto;
    const response: AnalysisQueryResponseDto = {
        operation: result.operation ?? '',
        recordsProcessed: result.recordsProcessed ?? 0,
        result: rows as AnalysisQueryResponseDto['result'],
    };
    const metricKeys = query.aggregates.map((a) => a.alias);

    const dataPoints: ChartDataPoint[] = response.result.map((item) => {
        const metrics: Record<string, number | null> = {};
        for (const key of metricKeys) {
            const raw = item.values?.[key];
            metrics[key] = raw === null || raw === undefined ? null : Number(raw);
        }
        return { label: item.key ?? 'Overall Total', metrics };
    });

    return {
        isEmpty: response.recordsProcessed === 0 || response.result.length === 0,
        title: query.groupBy
            ? `Grouped Analysis by ${query.groupBy}`
            : 'Overall Aggregation Results',
        subtitle: `Operation: ${response.operation}`,
        dataPoints,
        metricKeys,
        narrative: generateAggregationNarrative(
            response,
            query.groupBy,
            metricKeys[0] ?? ''
        ),
        recordsProcessed: response.recordsProcessed,
        defaultMetric: metricKeys[0] ?? '',
    };
}
