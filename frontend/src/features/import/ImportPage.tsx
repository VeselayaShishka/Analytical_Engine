import { useEffect, useState } from 'react';
import {
    Alert,
    Badge,
    Button,
    Checkbox,
    Group,
    Paper,
    Progress,
    Select,
    Stack,
    Table,
    Tabs,
    Text,
    Title,
} from '@mantine/core';
import { Dropzone, type FileRejection } from '@mantine/dropzone';
import { notifications } from '@mantine/notifications';
import {
    IconAlertCircle,
    IconChartBar,
    IconCheck,
    IconFileAnalytics,
    IconFileCode,
    IconFileSpreadsheet,
    IconPlus,
    IconTable,
    IconUpload,
    IconX,
} from '@tabler/icons-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { ApiError } from '../../api/errors';
import type {
    ColumnDataType,
    ImportResultDto,
    ImportRowErrorDto,
} from '../../api/types';
import { DatasetFormModal } from '../datasets/DatasetFormModal';
import { getDatasets } from '../datasets/datasetsApi';
import {
    parseFileForPreview,
    type InferredColumn,
    type ParsedFilePreview,
} from './fileParser';
import { uploadDatasetFile } from './importApi';
import { ImportErrorsTable } from './ImportErrorsTable';

const MAX_FILE_SIZE_BYTES = 50 * 1024 * 1024; // 50 MB limit

const DATA_TYPE_OPTIONS: { value: ColumnDataType; label: string }[] = [
    { value: 'string', label: 'String (Text)' },
    { value: 'integer', label: 'Integer (Whole Number)' },
    { value: 'double', label: 'Double (Decimal)' },
    { value: 'boolean', label: 'Boolean (True/False)' },
    { value: 'datetime', label: 'DateTime' },
];

export function ImportPage() {
    const [searchParams] = useSearchParams();
    const initialDatasetId = searchParams.get('datasetId');
    const queryClient = useQueryClient();

    const [selectedDatasetId, setSelectedDatasetId] = useState<string | null>(
        initialDatasetId
    );
    const [createDatasetOpen, setCreateDatasetOpen] = useState(false);

    // File selection & client-side preview state
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [preview, setPreview] = useState<ParsedFilePreview | null>(null);
    const [editableColumns, setEditableColumns] = useState<InferredColumn[]>([]);
    const [parseError, setParseError] = useState<string | null>(null);
    const [isParsing, setIsParsing] = useState(false);

    // Upload progress & server response state
    const [uploadProgress, setUploadProgress] = useState<number>(0);
    const [importResult, setImportResult] = useState<ImportResultDto | null>(
        null
    );
    const [serverError, setServerError] = useState<{
        title: string;
        detail: string;
        rowErrors: ImportRowErrorDto[];
    } | null>(null);

    // Fetch user datasets for the target selector
    const datasetsQuery = useQuery({
        queryKey: ['datasets', { page: 1, pageSize: 100 }],
        queryFn: () => getDatasets({ pageNumber: 1, pageSize: 100 }),
    });

    useEffect(() => {
        if (initialDatasetId) {
            setSelectedDatasetId(initialDatasetId);
        }
    }, [initialDatasetId]);

    const handleFileDrop = async (files: File[]) => {
        const file = files[0];
        if (!file) return;

        setSelectedFile(file);
        setParseError(null);
        setPreview(null);
        setEditableColumns([]);
        setImportResult(null);
        setServerError(null);
        setUploadProgress(0);
        setIsParsing(true);

        try {
            const parsed = await parseFileForPreview(file);
            setPreview(parsed);
            setEditableColumns(parsed.columns);
        } catch (err) {
            setParseError(
                err instanceof Error
                    ? err.message
                    : 'Unable to parse the selected file.'
            );
        } finally {
            setIsParsing(false);
        }
    };

    const handleFileReject = (rejections: FileRejection[]) => {
        const firstError = rejections[0]?.errors[0];
        if (!firstError) return;

        if (firstError.code === 'file-too-large') {
            setParseError('File exceeds the maximum allowed size of 50 MB.');
        } else if (firstError.code === 'file-invalid-type') {
            setParseError('Unsupported file format. Only .csv and .json are allowed.');
        } else {
            setParseError(firstError.message);
        }
    };

    const handleColumnTypeChange = (
        index: number,
        nextType: ColumnDataType | null
    ) => {
        if (!nextType) return;
        setEditableColumns((prev) =>
            prev.map((col, idx) =>
                idx === index ? { ...col, dataType: nextType } : col
            )
        );
    };

    const handleColumnNullableChange = (index: number, checked: boolean) => {
        setEditableColumns((prev) =>
            prev.map((col, idx) =>
                idx === index ? { ...col, isNullable: checked } : col
            )
        );
    };

    const uploadMutation = useMutation({
        mutationFn: async () => {
            if (!selectedDatasetId || !selectedFile || !preview) {
                throw new Error('Please select a target dataset and a valid file.');
            }

            return uploadDatasetFile({
                datasetId: selectedDatasetId,
                file: selectedFile,
                format: preview.format,
                columns: editableColumns,
                onProgress: setUploadProgress,
            });
        },
        onSuccess: (result) => {
            setImportResult(result);
            setServerError(null);

            // Invalidate dataset & dashboard caches so version counts update immediately
            queryClient.invalidateQueries({ queryKey: ['datasets'] });
            queryClient.invalidateQueries({
                queryKey: ['datasets', selectedDatasetId],
            });
            queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] });

            if (result.isSuccess) {
                notifications.show({
                    title: 'Import Completed',
                    message:
                        result.message ||
                        `Successfully imported ${result.importedRecords} records.`,
                    color: 'green',
                });
            } else {
                notifications.show({
                    title: 'Import Finished with Errors',
                    message:
                        result.message || 'Some rows could not be imported. Review details below.',
                    color: 'orange',
                });
            }
        },
        onError: (err) => {
            setImportResult(null);
            if (err instanceof ApiError) {
                const title =
                    err.status === 409
                        ? 'Duplicate File Version (409 Conflict)'
                        : err.title;
                setServerError({
                    title,
                    detail: err.detail,
                    rowErrors: err.importErrors,
                });
            } else {
                setServerError({
                    title: 'Import Failed',
                    detail:
                        err instanceof Error
                            ? err.message
                            : 'An unexpected error occurred during upload.',
                    rowErrors: [],
                });
            }
        },
    });

    const handleResetWizard = () => {
        setSelectedFile(null);
        setPreview(null);
        setEditableColumns([]);
        setParseError(null);
        setImportResult(null);
        setServerError(null);
        setUploadProgress(0);
    };

    const datasetOptions = (datasetsQuery.data?.items ?? []).map((ds) => ({
        value: ds.id,
        label: `${ds.name} (${ds.versionsCount} version${ds.versionsCount === 1 ? '' : 's'})`,
    }));

    const canStartImport =
        Boolean(selectedDatasetId) &&
        Boolean(selectedFile) &&
        Boolean(preview) &&
        !parseError &&
        !uploadMutation.isPending;

    return (
        <Stack gap="md">
            <div>
                <Title order={2}>Import Data</Title>
                <Text c="dimmed" size="sm">
                    Upload CSV or JSON open data files, verify inferred column types, and
                    create a new dataset version
                </Text>
            </div>

            {/* Step 1: Target Dataset Selection */}
            <Paper withBorder p="md" radius="md">
                <Stack gap="sm">
                    <Group justify="space-between" align="center" wrap="wrap">
                        <div>
                            <Text fw={600} size="sm">
                                Step 1. Select Target Dataset
                            </Text>
                            <Text size="xs" c="dimmed">
                                Choose an existing dataset to add a new version, or create a new
                                container
                            </Text>
                        </div>

                        <Button
                            size="xs"
                            variant="light"
                            leftSection={<IconPlus size={14} />}
                            onClick={() => setCreateDatasetOpen(true)}
                        >
                            New Dataset
                        </Button>
                    </Group>

                    <Select
                        placeholder={
                            datasetsQuery.isLoading
                                ? 'Loading your datasets...'
                                : 'Select a dataset...'
                        }
                        data={datasetOptions}
                        value={selectedDatasetId}
                        onChange={setSelectedDatasetId}
                        searchable
                        clearable
                        nothingFoundMessage="No datasets found. Click 'New Dataset' to create one."
                        aria-label="Target dataset"
                    />
                </Stack>
            </Paper>

            {/* Step 2: File Dropzone */}
            <Paper withBorder p="md" radius="md">
                <Stack gap="sm">
                    <Group justify="space-between" align="center">
                        <div>
                            <Text fw={600} size="sm">
                                Step 2. Upload Source File (.CSV or .JSON)
                            </Text>
                            <Text size="xs" c="dimmed">
                                Maximum file size: 50 MB. Headers are validated automatically on
                                selection.
                            </Text>
                        </div>

                        {selectedFile && (
                            <Button
                                size="xs"
                                variant="subtle"
                                color="gray"
                                onClick={handleResetWizard}
                                disabled={uploadMutation.isPending}
                            >
                                Clear File
                            </Button>
                        )}
                    </Group>

                    <Dropzone
                        onDrop={handleFileDrop}
                        onReject={handleFileReject}
                        maxSize={MAX_FILE_SIZE_BYTES}
                        maxFiles={1}
                        accept={{
                            'text/csv': ['.csv'],
                            'application/vnd.ms-excel': ['.csv'],
                            'application/json': ['.json'],
                        }}
                        loading={isParsing}
                        disabled={uploadMutation.isPending}
                    >
                        <Group
                            justify="center"
                            gap="xl"
                            mih={130}
                            style={{ pointerEvents: 'none' }}
                        >
                            <Dropzone.Accept>
                                <IconUpload size={42} stroke={1.5} color="var(--mantine-color-blue-6)" />
                            </Dropzone.Accept>
                            <Dropzone.Reject>
                                <IconX size={42} stroke={1.5} color="var(--mantine-color-red-6)" />
                            </Dropzone.Reject>
                            <Dropzone.Idle>
                                <IconFileAnalytics size={42} stroke={1.5} color="gray" />
                            </Dropzone.Idle>

                            <div>
                                <Text size="md" inline fw={500}>
                                    Drag a CSV or JSON file here, or click to select
                                </Text>
                                <Text size="xs" c="dimmed" inline mt={7}>
                                    Supports comma (,) or semicolon (;) delimited CSV and JSON
                                    object arrays up to 50 MB
                                </Text>
                            </div>
                        </Group>
                    </Dropzone>

                    {selectedFile && (
                        <Group gap="xs" wrap="wrap">
                            <Badge
                                variant="light"
                                color="blue"
                                leftSection={
                                    selectedFile.name.toLowerCase().endsWith('.json') ? (
                                        <IconFileCode size={12} />
                                    ) : (
                                        <IconFileSpreadsheet size={12} />
                                    )
                                }
                            >
                                {selectedFile.name}
                            </Badge>
                            <Badge variant="outline" color="gray">
                                {(selectedFile.size / 1024).toFixed(1)} KB
                            </Badge>
                            {preview?.detectedDelimiter && (
                                <Badge variant="outline" color="teal">
                                    Delimiter: &quot;{preview.detectedDelimiter}&quot;
                                </Badge>
                            )}
                        </Group>
                    )}

                    {parseError && (
                        <Alert
                            icon={<IconAlertCircle size={16} />}
                            title="File Validation Error"
                            color="red"
                            variant="light"
                        >
                            {parseError}
                        </Alert>
                    )}
                </Stack>
            </Paper>

            {/* Step 3: Schema Configuration & Data Preview */}
            {preview && editableColumns.length > 0 && (
                <Paper withBorder p="md" radius="md">
                    <Stack gap="md">
                        <div>
                            <Text fw={600} size="sm">
                                Step 3. Verify Schema &amp; Preview Parsed Rows
                            </Text>
                            <Text size="xs" c="dimmed">
                                Inferred from {preview.sampledRowCount} sampled row
                                {preview.sampledRowCount === 1 ? '' : 's'}. You can adjust
                                column data types before importing.
                            </Text>
                        </div>

                        <Tabs defaultValue="schema">
                            <Tabs.List>
                                <Tabs.Tab value="schema">
                                    Column Schema ({editableColumns.length})
                                </Tabs.Tab>
                                <Tabs.Tab value="preview">
                                    Data Preview (First {preview.previewRows.length} rows)
                                </Tabs.Tab>
                            </Tabs.List>

                            <Tabs.Panel value="schema" pt="sm">
                                <Table.ScrollContainer minWidth={560} mah={340}>
                                    <Table striped withTableBorder stickyHeader verticalSpacing="xs">
                                        <Table.Thead>
                                            <Table.Tr>
                                                <Table.Th w={60}>#</Table.Th>
                                                <Table.Th>Column Name</Table.Th>
                                                <Table.Th w={230}>Data Type</Table.Th>
                                                <Table.Th w={110}>Nullable</Table.Th>
                                            </Table.Tr>
                                        </Table.Thead>
                                        <Table.Tbody>
                                            {editableColumns.map((col, index) => (
                                                <Table.Tr key={col.name}>
                                                    <Table.Td>
                                                        <Text size="xs" c="dimmed" ff="monospace">
                                                            {col.position + 1}
                                                        </Text>
                                                    </Table.Td>
                                                    <Table.Td>
                                                        <Text size="sm" fw={500}>
                                                            {col.name}
                                                        </Text>
                                                    </Table.Td>
                                                    <Table.Td>
                                                        <Select
                                                            size="xs"
                                                            value={col.dataType}
                                                            onChange={(val) =>
                                                                handleColumnTypeChange(
                                                                    index,
                                                                    val as ColumnDataType | null
                                                                )
                                                            }
                                                            data={DATA_TYPE_OPTIONS}
                                                            disabled={uploadMutation.isPending}
                                                            aria-label={`Data type for ${col.name}`}
                                                        />
                                                    </Table.Td>
                                                    <Table.Td>
                                                        <Checkbox
                                                            checked={col.isNullable}
                                                            onChange={(e) =>
                                                                handleColumnNullableChange(
                                                                    index,
                                                                    e.currentTarget.checked
                                                                )
                                                            }
                                                            disabled={uploadMutation.isPending}
                                                            aria-label={`Nullable flag for ${col.name}`}
                                                        />
                                                    </Table.Td>
                                                </Table.Tr>
                                            ))}
                                        </Table.Tbody>
                                    </Table>
                                </Table.ScrollContainer>
                            </Tabs.Panel>

                            <Tabs.Panel value="preview" pt="sm">
                                <Table.ScrollContainer minWidth={600} mah={340}>
                                    <Table
                                        striped
                                        withTableBorder
                                        withColumnBorders
                                        stickyHeader
                                        verticalSpacing="xs"
                                    >
                                        <Table.Thead>
                                            <Table.Tr>
                                                {editableColumns.map((col) => (
                                                    <Table.Th key={col.name} miw={130}>
                                                        <Text size="xs" fw={700}>
                                                            {col.name}
                                                        </Text>
                                                    </Table.Th>
                                                ))}
                                            </Table.Tr>
                                        </Table.Thead>
                                        <Table.Tbody>
                                            {preview.previewRows.map((row, rowIdx) => (
                                                <Table.Tr key={rowIdx}>
                                                    {editableColumns.map((col) => {
                                                        const val = row[col.name];
                                                        return (
                                                            <Table.Td key={col.name}>
                                                                {val === null || val === undefined ? (
                                                                    <Text span c="dimmed" fs="italic" size="xs">
                                                                        null
                                                                    </Text>
                                                                ) : (
                                                                    <Text size="xs">{String(val)}</Text>
                                                                )}
                                                            </Table.Td>
                                                        );
                                                    })}
                                                </Table.Tr>
                                            ))}
                                        </Table.Tbody>
                                    </Table>
                                </Table.ScrollContainer>
                            </Tabs.Panel>
                        </Tabs>
                    </Stack>
                </Paper>
            )}

            {/* Step 4: Upload Action, Progress Bar & Result Summary */}
            <Paper withBorder p="md" radius="md">
                <Stack gap="md">
                    <Group justify="space-between" align="center" wrap="wrap">
                        <div>
                            <Text fw={600} size="sm">
                                Step 4. Run Server Import
                            </Text>
                            <Text size="xs" c="dimmed">
                                {!selectedDatasetId
                                    ? 'Select a target dataset in Step 1 to enable import.'
                                    : !preview
                                        ? 'Upload a valid CSV or JSON file in Step 2 to continue.'
                                        : 'Ready to upload and validate records on the server.'}
                            </Text>
                        </div>

                        <Button
                            size="md"
                            leftSection={<IconUpload size={18} />}
                            disabled={!canStartImport}
                            loading={uploadMutation.isPending}
                            onClick={() => uploadMutation.mutate()}
                        >
                            Start Import
                        </Button>
                    </Group>

                    {uploadMutation.isPending && (
                        <Stack gap={6}>
                            <Group justify="space-between">
                                <Text size="xs" fw={500}>
                                    {uploadProgress < 100
                                        ? `Uploading file... ${uploadProgress}%`
                                        : 'File uploaded. Validating and saving records in PostgreSQL...'}
                                </Text>
                                <Text size="xs" c="dimmed">
                                    {uploadProgress}%
                                </Text>
                            </Group>
                            <Progress
                                value={uploadProgress}
                                animated
                                size="md"
                                radius="xl"
                            />
                        </Stack>
                    )}

                    {/* Server Error Alert (e.g., 400 Invalid Header or 409 Duplicate Hash) */}
                    {serverError && (
                        <Stack gap="md">
                            <Alert
                                icon={<IconAlertCircle size={18} />}
                                title={serverError.title}
                                color="red"
                                variant="light"
                            >
                                {serverError.detail}
                            </Alert>

                            {serverError.rowErrors.length > 0 && (
                                <ImportErrorsTable errors={serverError.rowErrors} />
                            )}
                        </Stack>
                    )}

                    {/* Import Result Summary (Full or Partial Success) */}
                    {importResult && (
                        <Stack gap="md">
                            <Alert
                                icon={
                                    importResult.isSuccess ? (
                                        <IconCheck size={18} />
                                    ) : (
                                        <IconAlertCircle size={18} />
                                    )
                                }
                                title={
                                    importResult.isSuccess
                                        ? `Import Succeeded${importResult.version ? ` (Version ${importResult.version})` : ''}`
                                        : 'Import Completed with Row Validation Errors'
                                }
                                color={importResult.isSuccess ? 'green' : 'orange'}
                                variant="light"
                            >
                                <Stack gap="xs">
                                    <Text size="sm">{importResult.message}</Text>
                                    <Text size="sm" fw={600}>
                                        Imported records: {importResult.importedRecords.toLocaleString()}
                                    </Text>

                                    {importResult.datasetVersionId && selectedDatasetId && (
                                        <Group gap="sm" mt={4} wrap="wrap">
                                            <Button
                                                size="xs"
                                                variant="filled"
                                                color="teal"
                                                leftSection={<IconTable size={14} />}
                                                component={Link}
                                                to={`/datasets/${selectedDatasetId}/versions/${importResult.datasetVersionId}`}
                                            >
                                                View Imported Table
                                            </Button>
                                            <Button
                                                size="xs"
                                                variant="light"
                                                leftSection={<IconChartBar size={14} />}
                                                component={Link}
                                                to={`/analytics?datasetId=${selectedDatasetId}&versionId=${importResult.datasetVersionId}`}
                                            >
                                                Open in Analytics Builder
                                            </Button>
                                        </Group>
                                    )}
                                </Stack>
                            </Alert>

                            {importResult.errors && importResult.errors.length > 0 && (
                                <ImportErrorsTable
                                    errors={importResult.errors}
                                    summaryMessage="Rows listed below were rejected due to validation errors."
                                />
                            )}
                        </Stack>
                    )}
                </Stack>
            </Paper>

            <DatasetFormModal
                opened={createDatasetOpen}
                onClose={() => setCreateDatasetOpen(false)}
                onCreated={(created) => setSelectedDatasetId(created.id)}
            />
        </Stack>
    );
}