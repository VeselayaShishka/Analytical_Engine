import { useEffect, useMemo, useState } from 'react';
import {
    ActionIcon,
    Alert,
    Badge,
    Button,
    Center,
    Divider,
    Group,
    Loader,
    Paper,
    SegmentedControl,
    Select,
    Stack,
    Text,
    TextInput,
    Title,
} from '@mantine/core';
import {
    IconAlertCircle,
    IconChartBar,
    IconDeviceFloppy,
    IconFilter,
    IconPlayerPlay,
    IconPlus,
    IconTrash,
} from '@tabler/icons-react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { ApiError } from '../../api/errors';
import type {
    AggregationFunction,
    AnalysisAggregateDto,
    AnalysisFilterDto,
    AnalysisQueryRequestDto,
    AnalysisQueryResponseDto,
    ColumnDataType,
    DatasetColumnResponseDto,
    FilterOperator,
    ReportType,
    StatisticsRequestDto,
    StatisticsResponseDto,
} from '../../api/types';
import {
    getDatasets,
    getDatasetVersions,
    getVersionColumns,
} from '../datasets/datasetsApi';
import {
    AnalysisChartPanel,
    type ChartDataPoint,
} from './AnalysisChartPanel';
import { runAnalysisQuery, runStatisticsQuery } from './analyticsApi';
import {
    generateAggregationNarrative,
    generateStatisticsNarrative,
} from './narrativeGenerator';
import { SaveReportModal } from './SaveReportModal';

type AnalysisMode = 'aggregation' | 'statistics';

interface FilterRowState {
    id: string;
    column: string;
    operator: FilterOperator;
    value: string;
}

interface AggregateRowState {
    id: string;
    func: AggregationFunction;
    column: string;
    alias: string;
}

const NUMERIC_TYPES: ColumnDataType[] = [
    'int',
    'integer',
    'long',
    'decimal',
    'double',
];

const FILTER_OPERATOR_OPTIONS: { value: FilterOperator; label: string }[] = [
    { value: 'Eq', label: '= Equals' },
    { value: 'Neq', label: '≠ Not Equals' },
    { value: 'Contains', label: 'Contains (text)' },
    { value: 'StartsWith', label: 'Starts With' },
    { value: 'EndsWith', label: 'Ends With' },
    { value: 'Gt', label: '> Greater Than' },
    { value: 'Gte', label: '≥ Greater or Equal' },
    { value: 'Lt', label: '< Less Than' },
    { value: 'Lte', label: '≤ Less or Equal' },
    { value: 'IsNull', label: 'Is Null (Empty)' },
    { value: 'IsNotNull', label: 'Is Not Null' },
];

const AGGREGATION_OPTIONS: { value: AggregationFunction; label: string }[] = [
    { value: 'Count', label: 'COUNT (Rows)' },
    { value: 'Sum', label: 'SUM' },
    { value: 'Avg', label: 'AVG (Mean)' },
    { value: 'Min', label: 'MIN' },
    { value: 'Max', label: 'MAX' },
];

function isNumericColumn(col?: DatasetColumnResponseDto): boolean {
    if (!col) return false;
    return NUMERIC_TYPES.includes(col.dataType.toLowerCase() as ColumnDataType);
}

/**
 * Converts a raw text input from a filter row into the typed value expected by the backend.
 */
function coerceFilterValue(
    rawValue: string,
    operator: FilterOperator,
    column?: DatasetColumnResponseDto
): string | number | boolean | null {
    if (operator === 'IsNull' || operator === 'IsNotNull') {
        return null;
    }

    const trimmed = rawValue.trim();
    if (!trimmed) return '';

    if (column && isNumericColumn(column)) {
        // Support both '.' and ',' decimal separators
        const normalizedNum = Number(trimmed.replace(',', '.'));
        if (!Number.isNaN(normalizedNum)) {
            return normalizedNum;
        }
    }

    if (
        column &&
        (column.dataType.toLowerCase() === 'bool' ||
            column.dataType.toLowerCase() === 'boolean')
    ) {
        if (trimmed.toLowerCase() === 'true') return true;
        if (trimmed.toLowerCase() === 'false') return false;
    }

    return trimmed;
}

export function AnalyticsBuilderPage() {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    const initialDatasetId = searchParams.get('datasetId');
    const initialVersionId = searchParams.get('versionId');

    // Target dataset & version state
    const [selectedDatasetId, setSelectedDatasetId] = useState<string | null>(
        initialDatasetId
    );
    const [selectedVersionId, setSelectedVersionId] = useState<string | null>(
        initialVersionId
    );

    // Builder configuration state
    const [mode, setMode] = useState<AnalysisMode>('aggregation');
    const [filters, setFilters] = useState<FilterRowState[]>([]);
    const [groupByColumn, setGroupByColumn] = useState<string | null>(null);

    // Aggregation mode state
    const [aggregates, setAggregates] = useState<AggregateRowState[]>([
        { id: 'agg-1', func: 'Count', column: '', alias: 'total_count' },
    ]);

    // Statistics mode state
    const [statsColumn, setStatsColumn] = useState<string | null>(null);

    // Executed query & results state
    const [validationError, setValidationError] = useState<string | null>(null);
    const [lastQueryRequest, setLastQueryRequest] =
        useState<AnalysisQueryRequestDto | null>(null);
    const [lastStatsRequest, setLastStatsRequest] =
        useState<StatisticsRequestDto | null>(null);
    const [aggregationResult, setAggregationResult] =
        useState<AnalysisQueryResponseDto | null>(null);
    const [statisticsResult, setStatisticsResult] =
        useState<StatisticsResponseDto | null>(null);
    const [selectedChartMetric, setSelectedChartMetric] = useState<string>('');
    const [saveModalOpen, setSaveModalOpen] = useState(false);

    // 1. Fetch available datasets
    const datasetsQuery = useQuery({
        queryKey: ['datasets', { page: 1, pageSize: 100 }],
        queryFn: () => getDatasets({ pageNumber: 1, pageSize: 100 }),
    });

    // 2. Fetch versions for the selected dataset
    const versionsQuery = useQuery({
        queryKey: ['datasets', selectedDatasetId, 'versions'],
        queryFn: () => getDatasetVersions(selectedDatasetId!),
        enabled: Boolean(selectedDatasetId),
    });

    // Auto-select latest version when dataset changes without an explicit versionId
    useEffect(() => {
        const versions = versionsQuery.data;
        if (!versions || versions.length === 0) return;

        const exists = versions.some((v) => v.id === selectedVersionId);
        if (!exists) {
            const latest = [...versions].sort(
                (a, b) => b.versionNumber - a.versionNumber
            )[0];
            setSelectedVersionId(latest.id);
        }
    }, [versionsQuery.data, selectedVersionId]);

    // 3. Fetch column schema for the selected version
    const columnsQuery = useQuery({
        queryKey: [
            'datasets',
            selectedDatasetId,
            'versions',
            selectedVersionId,
            'columns',
        ],
        queryFn: () => getVersionColumns(selectedDatasetId!, selectedVersionId!),
        enabled: Boolean(selectedDatasetId && selectedVersionId),
    });

    const columns = columnsQuery.data ?? [];
    const numericColumns = useMemo(
        () => columns.filter(isNumericColumn),
        [columns]
    );

    // Reset builder selections when version changes
    const handleDatasetChange = (nextDatasetId: string | null) => {
        setSelectedDatasetId(nextDatasetId);
        setSelectedVersionId(null);
        setFilters([]);
        setGroupByColumn(null);
        setStatsColumn(null);
        setAggregationResult(null);
        setStatisticsResult(null);
        setValidationError(null);
    };

    const handleVersionChange = (nextVersionId: string | null) => {
        setSelectedVersionId(nextVersionId);
        setFilters([]);
        setGroupByColumn(null);
        setStatsColumn(null);
        setAggregationResult(null);
        setStatisticsResult(null);
        setValidationError(null);
    };

    // Auto-select first numeric column for Statistics mode when schema loads
    useEffect(() => {
        if (numericColumns.length > 0 && !statsColumn) {
            setStatsColumn(numericColumns[0].name);
        }
    }, [numericColumns, statsColumn]);

    // Filter rows handlers
    const addFilterRow = () => {
        const defaultCol = columns[0]?.name ?? '';
        setFilters((prev) => [
            ...prev,
            {
                id: `flt-${Date.now()}-${Math.random().toString(36).slice(2, 6)}`,
                column: defaultCol,
                operator: 'Eq',
                value: '',
            },
        ]);
    };

    const updateFilterRow = (
        id: string,
        patch: Partial<Omit<FilterRowState, 'id'>>
    ) => {
        setFilters((prev) =>
            prev.map((row) => (row.id === id ? { ...row, ...patch } : row))
        );
    };

    const removeFilterRow = (id: string) => {
        setFilters((prev) => prev.filter((row) => row.id !== id));
    };

    // Aggregate rows handlers
    const addAggregateRow = () => {
        const defaultNumCol = numericColumns[0]?.name ?? '';
        const nextIndex = aggregates.length + 1;
        setAggregates((prev) => [
            ...prev,
            {
                id: `agg-${Date.now()}-${Math.random().toString(36).slice(2, 6)}`,
                func: defaultNumCol ? 'Sum' : 'Count',
                column: defaultNumCol,
                alias: defaultNumCol ? `sum_${defaultNumCol}` : `metric_${nextIndex}`,
            },
        ]);
    };

    const updateAggregateRow = (
        id: string,
        patch: Partial<Omit<AggregateRowState, 'id'>>
    ) => {
        setAggregates((prev) =>
            prev.map((row) => {
                if (row.id !== id) return row;
                const updated = { ...row, ...patch };
                // Auto-suggest clean alias when function or column changes
                if (patch.func || patch.column !== undefined) {
                    const colPart = updated.column ? `_${updated.column}` : '_all';
                    updated.alias = `${updated.func.toLowerCase()}${colPart}`;
                }
                return updated;
            })
        );
    };

    const removeAggregateRow = (id: string) => {
        setAggregates((prev) => prev.filter((row) => row.id !== id));
    };

    // Mutation to execute either Aggregation or Statistics query
    const executeMutation = useMutation({
        mutationFn: async () => {
            if (!selectedVersionId) {
                throw new Error('Please select a dataset and version first.');
            }

            // Build validated filter DTOs
            const builtFilters: AnalysisFilterDto[] = filters.map((f) => {
                if (!f.column) {
                    throw new Error('Every filter row must have a column selected.');
                }
                if (
                    f.operator !== 'IsNull' &&
                    f.operator !== 'IsNotNull' &&
                    f.value.trim() === ''
                ) {
                    throw new Error(
                        `Filter value is required for column "${f.column}" with operator "${f.operator}".`
                    );
                }
                const colSchema = columns.find((c) => c.name === f.column);
                return {
                    column: f.column,
                    operator: f.operator,
                    value: coerceFilterValue(f.value, f.operator, colSchema),
                };
            });

            if (mode === 'aggregation') {
                if (aggregates.length === 0) {
                    throw new Error('Add at least one aggregation metric.');
                }

                const builtAggregates: AnalysisAggregateDto[] = aggregates.map((agg) => {
                    const trimmedAlias = agg.alias.trim();
                    if (!trimmedAlias) {
                        throw new Error('Every aggregation metric must have an alias.');
                    }
                    if (agg.func !== 'Count' && !agg.column) {
                        throw new Error(
                            `Aggregation function "${agg.func}" requires a numeric target column.`
                        );
                    }
                    return {
                        function: agg.func,
                        column: agg.column || null,
                        alias: trimmedAlias,
                    };
                });

                const payload: AnalysisQueryRequestDto = {
                    datasetVersionId: selectedVersionId,
                    filters: builtFilters,
                    groupBy: groupByColumn || null,
                    aggregates: builtAggregates,
                };

                const response = await runAnalysisQuery(payload);
                return { kind: 'aggregation' as const, payload, response };
            } else {
                if (!statsColumn) {
                    throw new Error(
                        'Select a numeric column to compute descriptive statistics.'
                    );
                }

                const payload: StatisticsRequestDto = {
                    datasetVersionId: selectedVersionId,
                    column: statsColumn,
                    filters: builtFilters,
                    groupBy: groupByColumn || null,
                };

                const response = await runStatisticsQuery(payload);
                return { kind: 'statistics' as const, payload, response };
            }
        },
        onSuccess: (outcome) => {
            setValidationError(null);
            if (outcome.kind === 'aggregation') {
                setLastQueryRequest(outcome.payload);
                setLastStatsRequest(null);
                setAggregationResult(outcome.response);
                setStatisticsResult(null);
                setSelectedChartMetric(outcome.payload.aggregates[0]?.alias ?? '');
            } else {
                setLastStatsRequest(outcome.payload);
                setLastQueryRequest(null);
                setStatisticsResult(outcome.response);
                setAggregationResult(null);
                setSelectedChartMetric('Mean');
            }
        },
        onError: (err) => {
            if (err instanceof ApiError) {
                setValidationError(err.detail);
            } else if (err instanceof Error) {
                setValidationError(err.message);
            } else {
                setValidationError('Failed to execute analysis query.');
            }
        },
    });

    // Transform active backend response into unified ChartDataPoint[] for AnalysisChartPanel
    const chartPresentation = useMemo(() => {
        if (aggregationResult) {
            const metricKeys =
                lastQueryRequest?.aggregates.map((a) => a.alias) ?? [];
            const dataPoints: ChartDataPoint[] = aggregationResult.result.map(
                (item) => {
                    const metrics: Record<string, number | null> = {};
                    for (const key of metricKeys) {
                        const raw = item.values[key];
                        metrics[key] =
                            raw === null || raw === undefined ? null : Number(raw);
                    }
                    return {
                        label: item.key ?? 'Overall Total',
                        metrics,
                    };
                }
            );

            const narrative = generateAggregationNarrative(
                aggregationResult,
                lastQueryRequest?.groupBy,
                selectedChartMetric || metricKeys[0] || ''
            );

            return {
                isEmpty:
                    aggregationResult.recordsProcessed === 0 ||
                    aggregationResult.result.length === 0,
                title: lastQueryRequest?.groupBy
                    ? `Grouped Analysis by ${lastQueryRequest.groupBy}`
                    : 'Overall Aggregation Results',
                subtitle: `Operation: ${aggregationResult.operation}`,
                dataPoints,
                metricKeys,
                narrative,
                recordsProcessed: aggregationResult.recordsProcessed,
            };
        }

        if (statisticsResult) {
            // If ungrouped (1 row), plot quantile distribution [Min, Q1, Median, Mean, Q3, Max]
            const targetColName = lastStatsRequest?.column ?? statsColumn ?? 'Target Column';
            
            if (!lastStatsRequest?.groupBy && statisticsResult.result.length === 1) {
                const s = statisticsResult.result[0];
                
                const distributionPoints: ChartDataPoint[] = [
                    { label: 'Min', metrics: { Value: s.min } },
                    { label: 'Q1 (25%)', metrics: { Value: s.q1 } },
                    { label: 'Median (50%)', metrics: { Value: s.median } },
                    { label: 'Mean', metrics: { Value: s.mean } },
                    { label: 'Q3 (75%)', metrics: { Value: s.q3 } },
                    { label: 'Max', metrics: { Value: s.max } },
                ];

                return {
                    isEmpty: statisticsResult.recordsProcessed === 0,
                    title: `Distribution Summary: ${targetColName}`,
                    subtitle: `Count: ${s.count} | Nulls: ${s.nullCount} | StdDev: ${s.stdDev?.toFixed(4) ?? 'N/A'} | IQR: ${s.iqr?.toFixed(4) ?? 'N/A'}`,
                    dataPoints: distributionPoints,
                    metricKeys: ['Value'],
                    narrative: generateStatisticsNarrative(statisticsResult, lastStatsRequest?.column ?? statsColumn ?? 'Target Column', null),
                    recordsProcessed: statisticsResult.recordsProcessed,
                };
            }

            // If grouped, plot groups across selectable statistical metrics
            const statMetricKeys = [
                'Mean',
                'Median',
                'Sum',
                'Min',
                'Max',
                'StdDev',
                'IQR',
                'Count',
            ];
            const dataPoints: ChartDataPoint[] = statisticsResult.result.map((s) => ({
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
            }));

            return {
                isEmpty:
                    statisticsResult.recordsProcessed === 0 ||
                    statisticsResult.result.length === 0,
                title: `Statistics for "${targetColName}" by ${lastStatsRequest?.groupBy}`,
                subtitle: 'Compare central tendency and spread across groups',
                dataPoints,
                metricKeys: statMetricKeys,
                narrative: generateStatisticsNarrative(
                    statisticsResult,
                lastStatsRequest?.column ?? statsColumn ?? 'Target Column',
                    lastStatsRequest?.groupBy
                ),
                recordsProcessed: statisticsResult.recordsProcessed,
            };
        }

        return null;
    }, [
        aggregationResult,
        statisticsResult,
        lastQueryRequest,
        lastStatsRequest,
        selectedChartMetric,
    ]);

    const currentReportType: ReportType =
        mode === 'statistics'
            ? 'Statistics'
            : lastQueryRequest?.groupBy
                ? 'Grouping'
                : 'Aggregation';

    const allColumnOptions = columns.map((c) => ({
        value: c.name,
        label: `${c.name} (${c.dataType})`,
    }));

    const numericColumnOptions = numericColumns.map((c) => ({
        value: c.name,
        label: `${c.name} (${c.dataType})`,
    }));

    return (
        <Stack gap="md">
            <Group justify="space-between" align="center" wrap="wrap">
                <div>
                    <Title order={2}>Analytics Builder</Title>
                    <Text c="dimmed" size="sm">
                        Filter records, group categories, compute aggregations or
                        descriptive statistics, and save visual reports
                    </Text>
                </div>

                {chartPresentation && !chartPresentation.isEmpty && selectedVersionId && (
                    <Button
                        variant="light"
                        color="teal"
                        leftSection={<IconDeviceFloppy size={16} />}
                        onClick={() => setSaveModalOpen(true)}
                    >
                        Save as Report
                    </Button>
                )}
            </Group>

            {/* Step 1: Dataset & Version Selection */}
            <Paper withBorder p="md" radius="md">
                <Stack gap="sm">
                    <Text fw={600} size="sm">
                        1. Data Source Selection
                    </Text>

                    <Group grow align="flex-start" wrap="wrap">
                        <Select
                            label="Dataset"
                            placeholder={
                                datasetsQuery.isLoading
                                    ? 'Loading datasets...'
                                    : 'Select dataset'
                            }
                            data={(datasetsQuery.data?.items ?? []).map((ds) => ({
                                value: ds.id,
                                label: ds.name,
                            }))}
                            value={selectedDatasetId}
                            onChange={handleDatasetChange}
                            searchable
                            clearable
                        />

                        <Select
                            label="Dataset Version"
                            placeholder={
                                !selectedDatasetId
                                    ? 'Select a dataset first'
                                    : versionsQuery.isLoading
                                        ? 'Loading versions...'
                                        : 'Select version'
                            }
                            disabled={!selectedDatasetId}
                            data={[...(versionsQuery.data ?? [])]
                                .sort((a, b) => b.versionNumber - a.versionNumber)
                                .map((v) => ({
                                    value: v.id,
                                    label: `v${v.versionNumber} — ${v.sourceFileName} (${v.recordCount.toLocaleString()} rows)`,
                                }))}
                            value={selectedVersionId}
                            onChange={handleVersionChange}
                        />
                    </Group>
                </Stack>
            </Paper>

            {/* Step 2: Query Configuration */}
            {selectedVersionId && (
                <Paper withBorder p="md" radius="md">
                    {columnsQuery.isLoading ? (
                        <Center py="lg">
                            <Loader />
                        </Center>
                    ) : (
                        <Stack gap="md">
                            <Group justify="space-between" align="center" wrap="wrap">
                                <Text fw={600} size="sm">
                                    2. Analysis Mode &amp; Parameters
                                </Text>

                                <SegmentedControl
                                    value={mode}
                                    onChange={(val) => {
                                        setMode(val as AnalysisMode);
                                        setValidationError(null);
                                    }}
                                    data={[
                                        {
                                            value: 'aggregation',
                                            label: 'Aggregations & Grouping',
                                        },
                                        {
                                            value: 'statistics',
                                            label: 'Descriptive Statistics',
                                        },
                                    ]}
                                />
                            </Group>

                            <Divider />

                            {/* Dynamic Filters Section */}
                            <Stack gap="xs">
                                <Group justify="space-between" align="center">
                                    <Group gap={6}>
                                        <IconFilter size={16} />
                                        <Text fw={500} size="sm">
                                            Filters (Optional)
                                        </Text>
                                        {filters.length > 0 && (
                                            <Badge size="xs" variant="light">
                                                {filters.length}
                                            </Badge>
                                        )}
                                    </Group>

                                    <Button
                                        size="xs"
                                        variant="subtle"
                                        leftSection={<IconPlus size={14} />}
                                        onClick={addFilterRow}
                                        disabled={columns.length === 0}
                                    >
                                        Add Filter
                                    </Button>
                                </Group>

                                {filters.length === 0 && (
                                    <Text size="xs" c="dimmed">
                                        No filters applied — all records in the selected version
                                        will be included.
                                    </Text>
                                )}

                                {filters.map((filterRow) => {
                                    const hideValue =
                                        filterRow.operator === 'IsNull' ||
                                        filterRow.operator === 'IsNotNull';
                                    return (
                                        <Group key={filterRow.id} gap="xs" wrap="wrap">
                                            <Select
                                                placeholder="Column"
                                                data={allColumnOptions}
                                                value={filterRow.column}
                                                onChange={(val) =>
                                                    updateFilterRow(filterRow.id, { column: val ?? '' })
                                                }
                                                style={{ flex: 2, minWidth: 160 }}
                                                aria-label="Filter column"
                                            />

                                            <Select
                                                data={FILTER_OPERATOR_OPTIONS}
                                                value={filterRow.operator}
                                                onChange={(val) =>
                                                    updateFilterRow(filterRow.id, {
                                                        operator: (val as FilterOperator) ?? 'Equals',
                                                    })
                                                }
                                                style={{ flex: 1.5, minWidth: 150 }}
                                                aria-label="Filter operator"
                                            />

                                            {!hideValue && (
                                                <TextInput
                                                    placeholder="Value..."
                                                    value={filterRow.value}
                                                    onChange={(e) =>
                                                        updateFilterRow(filterRow.id, {
                                                            value: e.currentTarget.value,
                                                        })
                                                    }
                                                    style={{ flex: 2, minWidth: 150 }}
                                                    aria-label="Filter value"
                                                />
                                            )}

                                            <ActionIcon
                                                color="red"
                                                variant="subtle"
                                                onClick={() => removeFilterRow(filterRow.id)}
                                                aria-label="Remove filter"
                                            >
                                                <IconTrash size={16} />
                                            </ActionIcon>
                                        </Group>
                                    );
                                })}
                            </Stack>

                            <Divider />

                            {/* Grouping Selector (Shared by both modes) */}
                            <Select
                                label="Group By Column (Optional)"
                                description="Segment results by unique values of a categorical or date column"
                                placeholder="No grouping (compute overall total)"
                                data={allColumnOptions}
                                value={groupByColumn}
                                onChange={setGroupByColumn}
                                searchable
                                clearable
                                maw={420}
                            />

                            <Divider />

                            {/* Mode-specific Controls */}
                            {mode === 'aggregation' ? (
                                <Stack gap="xs">
                                    <Group justify="space-between" align="center">
                                        <Text fw={500} size="sm">
                                            Aggregation Metrics
                                        </Text>

                                        <Button
                                            size="xs"
                                            variant="subtle"
                                            leftSection={<IconPlus size={14} />}
                                            onClick={addAggregateRow}
                                        >
                                            Add Metric
                                        </Button>
                                    </Group>

                                    {aggregates.map((aggRow) => {
                                        const isCount = aggRow.func === 'Count';
                                        const targetOptions = isCount
                                            ? [
                                                { value: '', label: '* (All Rows)' },
                                                ...allColumnOptions,
                                            ]
                                            : numericColumnOptions;

                                        return (
                                            <Group key={aggRow.id} gap="xs" wrap="wrap">
                                                <Select
                                                    data={AGGREGATION_OPTIONS}
                                                    value={aggRow.func}
                                                    onChange={(val) =>
                                                        updateAggregateRow(aggRow.id, {
                                                            func: (val as AggregationFunction) ?? 'Count',
                                                        })
                                                    }
                                                    style={{ flex: 1.4, minWidth: 140 }}
                                                    aria-label="Aggregation function"
                                                />

                                                <Select
                                                    placeholder={
                                                        isCount
                                                            ? '* (All Rows)'
                                                            : 'Select numeric column...'
                                                    }
                                                    data={targetOptions}
                                                    value={aggRow.column}
                                                    onChange={(val) =>
                                                        updateAggregateRow(aggRow.id, {
                                                            column: val ?? '',
                                                        })
                                                    }
                                                    style={{ flex: 2, minWidth: 170 }}
                                                    aria-label="Target column"
                                                />

                                                <TextInput
                                                    placeholder="Output alias"
                                                    value={aggRow.alias}
                                                    onChange={(e) =>
                                                        updateAggregateRow(aggRow.id, {
                                                            alias: e.currentTarget.value,
                                                        })
                                                    }
                                                    style={{ flex: 1.6, minWidth: 140 }}
                                                    aria-label="Metric alias"
                                                />

                                                <ActionIcon
                                                    color="red"
                                                    variant="subtle"
                                                    disabled={aggregates.length <= 1}
                                                    onClick={() => removeAggregateRow(aggRow.id)}
                                                    aria-label="Remove metric"
                                                >
                                                    <IconTrash size={16} />
                                                </ActionIcon>
                                            </Group>
                                        );
                                    })}
                                </Stack>
                            ) : (
                                <Stack gap="xs">
                                    <Select
                                        label="Target Numeric Column"
                                        description="Computes Count, NullCount, Sum, Mean, Min, Max, Median, Variance, StdDev, Q1, Q3, and IQR"
                                        placeholder={
                                            numericColumnOptions.length === 0
                                                ? 'No numeric columns available in this version'
                                                : 'Select numeric column'
                                        }
                                        data={numericColumnOptions}
                                        value={statsColumn}
                                        onChange={setStatsColumn}
                                        disabled={numericColumnOptions.length === 0}
                                        required
                                        maw={420}
                                    />
                                </Stack>
                            )}

                            {validationError && (
                                <Alert
                                    icon={<IconAlertCircle size={16} />}
                                    title="Query Validation Error"
                                    color="red"
                                    variant="light"
                                >
                                    {validationError}
                                </Alert>
                            )}

                            <Group justify="flex-end" mt="xs">
                                <Button
                                    size="md"
                                    leftSection={<IconPlayerPlay size={16} />}
                                    loading={executeMutation.isPending}
                                    onClick={() => executeMutation.mutate()}
                                >
                                    Run Analysis
                                </Button>
                            </Group>
                        </Stack>
                    )}
                </Paper>
            )}

            {/* Step 3: Results Visualization (Empty Selection or Chart + Narrative + Table) */}
            {chartPresentation && (
                <>
                    {chartPresentation.isEmpty ? (
                        <Alert
                            icon={<IconChartBar size={18} />}
                            title="Empty Selection (0 Matching Records)"
                            color="yellow"
                            variant="light"
                        >
                            {chartPresentation.narrative}
                        </Alert>
                    ) : (
                        <AnalysisChartPanel
                            title={chartPresentation.title}
                            subtitle={chartPresentation.subtitle}
                            dataPoints={chartPresentation.dataPoints}
                            metricKeys={chartPresentation.metricKeys}
                            narrative={chartPresentation.narrative}
                            recordsProcessed={chartPresentation.recordsProcessed}
                            selectedMetric={selectedChartMetric}
                            onMetricChange={setSelectedChartMetric}
                        />
                    )}
                </>
            )}

            {selectedVersionId && (
                <SaveReportModal
                    opened={saveModalOpen}
                    onClose={() => setSaveModalOpen(false)}
                    datasetVersionId={selectedVersionId}
                    reportType={currentReportType}
                    queryRequest={lastQueryRequest}
                    statisticsRequest={lastStatsRequest}
                    onSaved={(reportId) => navigate(`/reports/${reportId}`)}
                />
            )}
        </Stack>
    );
}