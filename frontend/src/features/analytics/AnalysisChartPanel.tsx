import { useEffect, useMemo, useState } from 'react';
import {
    Badge,
    Blockquote,
    Group,
    Paper,
    SegmentedControl,
    Select,
    Stack,
    Table,
    Text,
    Title,
} from '@mantine/core';
import {
    IconChartBar,
    IconChartLine,
    IconChartPie,
    IconInfoCircle,
} from '@tabler/icons-react';
import {
    Bar,
    BarChart,
    CartesianGrid,
    Cell,
    Legend,
    Line,
    LineChart,
    Pie,
    PieChart,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis,
} from 'recharts';

export interface ChartDataPoint {
    label: string;
    metrics: Record<string, number | null>;
}

interface AnalysisChartPanelProps {
    title: string;
    subtitle?: string;
    dataPoints: ChartDataPoint[];
    metricKeys: string[];
    narrative: string;
    recordsProcessed: number;
    selectedMetric?: string;
    onMetricChange?: (metric: string) => void;
}

type ChartType = 'bar' | 'line' | 'pie';

const CHART_COLORS = [
    '#228be6',
    '#12b886',
    '#fab005',
    '#fa5252',
    '#7950f2',
    '#15aabf',
    '#fd7e14',
    '#40c057',
];

function formatCellNumber(val: number | null | undefined): string {
    if (val === null || val === undefined || Number.isNaN(val)) {
        return '—';
    }
    return val.toLocaleString(undefined, { maximumFractionDigits: 4 });
}

export function AnalysisChartPanel({
                                       title,
                                       subtitle,
                                       dataPoints,
                                       metricKeys,
                                       narrative,
                                       recordsProcessed,
                                       selectedMetric: controlledMetric,
                                       onMetricChange,
                                   }: AnalysisChartPanelProps) {
    const [chartType, setChartType] = useState<ChartType>('bar');
    const [internalMetric, setInternalMetric] = useState<string>(
        metricKeys[0] ?? ''
    );

    // Sync internal metric if metricKeys list changes
    useEffect(() => {
        if (metricKeys.length > 0 && !metricKeys.includes(internalMetric)) {
            setInternalMetric(metricKeys[0]);
        }
    }, [metricKeys, internalMetric]);

    const activeMetric = controlledMetric ?? internalMetric;

    const handleMetricSelect = (nextMetric: string | null) => {
        if (!nextMetric) return;
        setInternalMetric(nextMetric);
        onMetricChange?.(nextMetric);
    };

    // Transform generic dataPoints into flat objects expected by Recharts
    const chartSeriesData = useMemo(() => {
        return dataPoints.map((dp) => ({
            name: dp.label,
            value: dp.metrics[activeMetric] ?? 0,
        }));
    }, [dataPoints, activeMetric]);

    return (
        <Stack gap="md">
            <Paper withBorder p="md" radius="md">
                <Stack gap="md">
                    {/* Header & Chart Controls */}
                    <Group justify="space-between" align="flex-start" wrap="wrap">
                        <div>
                            <Group gap="xs">
                                <Title order={4}>{title}</Title>
                                <Badge variant="light" color="blue">
                                    {recordsProcessed.toLocaleString()} records processed
                                </Badge>
                            </Group>
                            {subtitle && (
                                <Text size="xs" c="dimmed" mt={2}>
                                    {subtitle}
                                </Text>
                            )}
                        </div>

                        <Group gap="sm" wrap="wrap">
                            {metricKeys.length > 1 && (
                                <Select
                                    size="xs"
                                    aria-label="Chart metric"
                                    value={activeMetric}
                                    onChange={handleMetricSelect}
                                    data={metricKeys.map((m) => ({ value: m, label: m }))}
                                    w={170}
                                />
                            )}

                            <SegmentedControl
                                size="xs"
                                value={chartType}
                                onChange={(val) => setChartType(val as ChartType)}
                                data={[
                                    {
                                        value: 'bar',
                                        label: (
                                            <Group gap={4} wrap="nowrap">
                                                <IconChartBar size={14} />
                                                <span>Bar</span>
                                            </Group>
                                        ),
                                    },
                                    {
                                        value: 'line',
                                        label: (
                                            <Group gap={4} wrap="nowrap">
                                                <IconChartLine size={14} />
                                                <span>Line</span>
                                            </Group>
                                        ),
                                    },
                                    {
                                        value: 'pie',
                                        label: (
                                            <Group gap={4} wrap="nowrap">
                                                <IconChartPie size={14} />
                                                <span>Pie</span>
                                            </Group>
                                        ),
                                    },
                                ]}
                            />
                        </Group>
                    </Group>

                    {/* Interactive Recharts Container */}
                    {chartSeriesData.length > 0 && activeMetric && (
                        <div style={{ width: '100%', height: 340 }}>
                            <ResponsiveContainer width="100%" height="100%">
                                {chartType === 'bar' ? (
                                    <BarChart
                                        data={chartSeriesData}
                                        margin={{ top: 10, right: 20, left: 10, bottom: 30 }}
                                    >
                                        <CartesianGrid strokeDasharray="3 3" />
                                        <XAxis
                                            dataKey="name"
                                            tick={{ fontSize: 12 }}
                                            interval={0}
                                            angle={chartSeriesData.length > 6 ? -25 : 0}
                                            textAnchor={chartSeriesData.length > 6 ? 'end' : 'middle'}
                                        />
                                        <YAxis tick={{ fontSize: 12 }} />
                                        <Tooltip
                                            formatter={(val: unknown) => [
                                                formatCellNumber(Number(val)),
                                                activeMetric,
                                            ]}
                                        />
                                        <Legend />
                                        <Bar
                                            dataKey="value"
                                            name={activeMetric}
                                            fill="#228be6"
                                            radius={[4, 4, 0, 0]}
                                        />
                                    </BarChart>
                                ) : chartType === 'line' ? (
                                    <LineChart
                                        data={chartSeriesData}
                                        margin={{ top: 10, right: 20, left: 10, bottom: 30 }}
                                    >
                                        <CartesianGrid strokeDasharray="3 3" />
                                        <XAxis dataKey="name" tick={{ fontSize: 12 }} />
                                        <YAxis tick={{ fontSize: 12 }} />
                                        <Tooltip
                                            formatter={(val: unknown) => [
                                                formatCellNumber(Number(val)),
                                                activeMetric,
                                            ]}
                                        />
                                        <Legend />
                                        <Line
                                            type="monotone"
                                            dataKey="value"
                                            name={activeMetric}
                                            stroke="#228be6"
                                            strokeWidth={2.5}
                                            dot={{ r: 4 }}
                                            activeDot={{ r: 6 }}
                                        />
                                    </LineChart>
                                ) : (
                                    <PieChart>
                                        <Tooltip
                                            formatter={(val: unknown) => [
                                                formatCellNumber(Number(val)),
                                                activeMetric,
                                            ]}
                                        />
                                        <Legend />
                                        <Pie
                                            data={chartSeriesData}
                                            dataKey="value"
                                            nameKey="name"
                                            cx="50%"
                                            cy="50%"
                                            outerRadius={115}
                                            label={({ name }) => String(name)}
                                        >
                                            {chartSeriesData.map((entry, idx) => (
                                                <Cell
                                                    key={`${entry.name}-${idx}`}
                                                    fill={CHART_COLORS[idx % CHART_COLORS.length]}
                                                />
                                            ))}
                                        </Pie>
                                    </PieChart>
                                )}
                            </ResponsiveContainer>
                        </div>
                    )}

                    {/* Mandatory Textual Explanation Block below the chart */}
                    <Blockquote
                        color="blue"
                        icon={<IconInfoCircle size={18} />}
                        p="sm"
                        mt="xs"
                    >
                        <Text size="xs" fw={700} tt="uppercase" c="blue" mb={4}>
                            Analytical Summary
                        </Text>
                        <Text size="sm">{narrative}</Text>
                    </Blockquote>
                </Stack>
            </Paper>

            {/* Mandatory Numerical Data Table accompanying the chart */}
            <Paper withBorder p="md" radius="md">
                <Stack gap="xs">
                    <Title order={5}>Underlying Numerical Data</Title>
                    <Text size="xs" c="dimmed">
                        Exact computed values used to render the visualization above
                    </Text>

                    <Table.ScrollContainer minWidth={500} mah={380}>
                        <Table
                            striped
                            highlightOnHover
                            withTableBorder
                            withColumnBorders
                            stickyHeader
                            verticalSpacing="xs"
                        >
                            <Table.Thead>
                                <Table.Tr>
                                    <Table.Th>Group / Category</Table.Th>
                                    {metricKeys.map((metric) => (
                                        <Table.Th key={metric} ta="right">
                                            {metric}
                                        </Table.Th>
                                    ))}
                                </Table.Tr>
                            </Table.Thead>
                            <Table.Tbody>
                                {dataPoints.map((dp, idx) => (
                                    <Table.Tr key={`${dp.label}-${idx}`}>
                                        <Table.Td>
                                            <Text size="sm" fw={500}>
                                                {dp.label}
                                            </Text>
                                        </Table.Td>
                                        {metricKeys.map((metric) => (
                                            <Table.Td key={metric} ta="right">
                                                <Text size="sm" ff="monospace">
                                                    {formatCellNumber(dp.metrics[metric])}
                                                </Text>
                                            </Table.Td>
                                        ))}
                                    </Table.Tr>
                                ))}
                            </Table.Tbody>
                        </Table>
                    </Table.ScrollContainer>
                </Stack>
            </Paper>
        </Stack>
    );
}