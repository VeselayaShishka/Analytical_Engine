import type {
    AnalysisQueryResponseDto,
    StatisticsResponseDto,
} from '../../api/types';

function formatNum(value: number | null | undefined): string {
    if (value === null || value === undefined || Number.isNaN(value)) {
        return 'N/A';
    }
    return value.toLocaleString(undefined, { maximumFractionDigits: 4 });
}

/**
 * Generates a human-readable textual explanation for an aggregation/grouping result.
 * Ensures every chart is accompanied by an accessible narrative summary.
 */
export function generateAggregationNarrative(
    response: AnalysisQueryResponseDto,
    groupByColumn: string | null | undefined,
    primaryMetricAlias: string
): string {
    if (response.recordsProcessed === 0 || response.result.length === 0) {
        return 'No records matched the specified filter criteria (empty selection). Adjust or clear active filters to analyze available records.';
    }

    // Case 1: Overall aggregation without GroupBy
    if (!groupByColumn) {
        const row = response.result[0];
        const metricEntries = Object.entries(row.values)
            .map(([alias, val]) => `${alias} = ${formatNum(Number(val))}`)
            .join(', ');

        return `Analyzed ${response.recordsProcessed.toLocaleString()} matching record(s) across the entire dataset version without grouping. Computed overall metrics: ${metricEntries || `Count = ${row.count}`}.`;
    }

    // Case 2: Grouped aggregation across multiple categories
    const sortedByMetric = [...response.result]
        .map((item) => ({
            label: item.key ?? '(Null / Unspecified)',
            value: Number(item.values[primaryMetricAlias] ?? item.count ?? 0),
            count: item.count,
        }))
        .filter((item) => !Number.isNaN(item.value))
        .sort((a, b) => b.value - a.value);

    if (sortedByMetric.length === 0) {
        return `Processed ${response.recordsProcessed.toLocaleString()} record(s) grouped by "${groupByColumn}" into ${response.result.length} group(s).`;
    }

    const top = sortedByMetric[0];
    const bottom = sortedByMetric[sortedByMetric.length - 1];

    if (sortedByMetric.length === 1) {
        return `Processed ${response.recordsProcessed.toLocaleString()} record(s) grouped by "${groupByColumn}". All matching records belong to a single group "${top.label}" with ${primaryMetricAlias} = ${formatNum(top.value)} (${top.count.toLocaleString()} records).`;
    }

    return `Processed ${response.recordsProcessed.toLocaleString()} record(s) grouped by "${groupByColumn}" across ${sortedByMetric.length} distinct groups. For metric "${primaryMetricAlias}", the highest value is observed in "${top.label}" (${formatNum(top.value)}), while the lowest is in "${bottom.label}" (${formatNum(bottom.value)}).`;
}

/**
 * Generates a human-readable textual explanation for a descriptive statistics result.
 */
export function generateStatisticsNarrative(
    response: StatisticsResponseDto,
    columnName: string,
    groupByColumn: string | null | undefined
): string {
    if (response.recordsProcessed === 0 || response.result.length === 0) {
        return `No records matched the specified filter criteria when analyzing column "${columnName}" (empty selection).`;
    }

    if (!groupByColumn) {
        const stat = response.result[0];
        return `Descriptive statistics for "${columnName}" across ${response.recordsProcessed.toLocaleString()} processed record(s) (${stat.count.toLocaleString()} valid, ${stat.nullCount.toLocaleString()} null): values range from ${formatNum(stat.min)} to ${formatNum(stat.max)}, with an arithmetic mean of ${formatNum(stat.mean)} and a median of ${formatNum(stat.median)}. Standard deviation is ${formatNum(stat.stdDev)} and interquartile range (IQR) is ${formatNum(stat.iqr)}.`;
    }

    const validGroups = response.result.filter((g) => g.mean !== null);
    if (validGroups.length === 0) {
        return `Computed statistics for "${columnName}" grouped by "${groupByColumn}" across ${response.result.length} group(s), but all target values were null.`;
    }

    const sortedByMean = [...validGroups].sort(
        (a, b) => (b.mean ?? 0) - (a.mean ?? 0)
    );
    const highest = sortedByMean[0];
    const lowest = sortedByMean[sortedByMean.length - 1];

    return `Computed descriptive statistics for "${columnName}" grouped by "${groupByColumn}" across ${response.result.length} group(s) (${response.recordsProcessed.toLocaleString()} total records). Group "${highest.key ?? '(Null)'}" has the highest mean (${formatNum(highest.mean)}, median ${formatNum(highest.median)}), whereas "${lowest.key ?? '(Null)'}" has the lowest mean (${formatNum(lowest.mean)}, median ${formatNum(lowest.median)}).`;
}