export type UserRole = 'Analyst' | 'DataAdministrator';

export interface UserDto {
    id: string;
    username: string;
    email: string;
    role: UserRole;
}

export interface AuthResponseDto {
    token: string;
    expiresAt: string;
    user?: UserDto;
    userId?: string;
    username?: string;
    email?: string;
    role?: UserRole;
}

export interface LoginRequestDto {
    email: string;
    password: string;
}

export interface RegisterRequestDto {
    username: string;
    email: string;
    password: string;
}

export interface PagedResult<T> {
    items: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
}

export interface DatasetResponseDto {
    id: string;
    name: string;
    description?: string | null;
    userId: string;
    versionsCount: number;
    createdAt: string;
    modifiedAt?: string | null;
}

export interface CreateDatasetDto {
    name: string;
    description?: string | null;
}

export interface UpdateDatasetDto {
    name: string;
    description?: string | null;
}

export interface DatasetVersionResponseDto {
    id: string;
    datasetId: string;
    versionNumber: number;
    sourceFileName: string;
    fileFormat: string;
    recordCount: number;
    createdAt: string;
}

export type ColumnDataType =
    | 'string'
    | 'int'
    | 'integer'
    | 'long'
    | 'decimal'
    | 'double'
    | 'bool'
    | 'boolean'
    | 'datetime';

export interface DatasetColumnResponseDto {
    id: string;
    name: string;
    dataType: ColumnDataType;
    isNullable: boolean;
    position: number;
    description?: string | null;
}

export interface DataRecordResponseDto {
    id: string;
    rowNumber: number;
    data: Record<string, unknown>;
}

export interface ImportRowErrorDto {
    rowNumber: number;
    errorMessages: string[];
}

export interface ImportResultDto {
    isSuccess: boolean;
    message: string;
    datasetVersionId?: string | null;
    version?: number | null;
    importedRecords: number;
    errors: ImportRowErrorDto[];
}

export type FilterOperator =
    | 'Eq'
    | 'Neq'
    | 'Contains'
    | 'StartsWith'
    | 'EndsWith'
    | 'Gt'
    | 'Gte'
    | 'Lt'
    | 'Lte'
    | 'IsNull'
    | 'IsNotNull';

export type AggregationFunction = 'Count' | 'Sum' | 'Avg' | 'Min' | 'Max';

export interface AnalysisFilterDto {
    column: string;
    operator: FilterOperator;
    value?: string | number | boolean | null;
}

export interface AnalysisAggregateDto {
    function: AggregationFunction;
    column?: string | null;
    alias: string;
}

export interface AnalysisQueryRequestDto {
    datasetVersionId: string;
    filters: AnalysisFilterDto[];
    groupBy?: string | null;
    aggregates: AnalysisAggregateDto[];
}

export interface GroupResultItemDto {
    key: string | null;
    values: Record<string, unknown>;
    count: number;
}

export interface AnalysisQueryResponseDto {
    operation: string;
    recordsProcessed: number;
    result: GroupResultItemDto[];
}

export interface StatisticsRequestDto {
    datasetVersionId: string;
    column: string;
    filters: AnalysisFilterDto[];
    groupBy?: string | null;
}

export interface StatisticsItemDto {
    key: string | null;
    count: number;
    nullCount: number;
    sum: number | null;
    mean: number | null;
    min: number | null;
    max: number | null;
    median: number | null;
    variance: number | null;
    stdDev: number | null;
    q1: number | null;
    q3: number | null;
    iqr: number | null;
}

export interface StatisticsResponseDto {
    datasetVersionId?: string;
    operation?: string;
    recordsProcessed: number;
    result: StatisticsItemDto[];
}

export type ReportType = 'Aggregation' | 'Grouping' | 'Statistics' | 'Custom';

export interface ReportListItemDto {
    id: string;
    name: string;
    description?: string | null;
    reportType: ReportType;
    datasetVersionId: string;
    userId: string;
    createdAt: string;
}

export interface ReportEnvelopeDto {
    schemaVersion?: number;
    request: AnalysisQueryRequestDto | StatisticsRequestDto;
    result: AnalysisQueryResponseDto | StatisticsResponseDto;
}

export interface ReportDetailDto extends ReportListItemDto {
    resultData: ReportEnvelopeDto;
}

export interface CreateReportDto {
    name: string;
    description?: string | null;
    reportType: ReportType;
    datasetVersionId: string;
    queryRequest?: AnalysisQueryRequestDto | null;
    statisticsRequest?: StatisticsRequestDto | null;
}

export interface DashboardSummaryDto {
    totalDatasets: number;
    totalVersions: number;
    totalRecords: number;
    totalReports: number;
    recentDatasets: DatasetResponseDto[];
    recentReports: ReportListItemDto[];
}

export interface ProblemDetails {
    type?: string;
    title?: string;
    status?: number;
    detail?: string;
    instance?: string;
    errors?: Record<string, string[]>;
}