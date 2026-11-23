using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Exceptions;
using Analytical_Engine.Extensions;
using Analytical_Engine.Models.DTOs.Common;
using Analytical_Engine.Models.DTOs.Reports;
using Analytical_Engine.Models.Entities;
using Analytical_Engine.Services.Interfaces;

namespace Analytical_Engine.Services;

public class ReportService : IReportService
{
    private const int MaxPageSize = 100;
    private const int MaxResultDataBytes = 1_048_576; 

    private static readonly JsonSerializerOptions StorageJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAnalysisService _analysisService;

    public ReportService(IUnitOfWork unitOfWork, IAnalysisService analysisService)
    {
        _unitOfWork = unitOfWork;
        _analysisService = analysisService;
    }

    public async Task<ReportResponseDto> CreateAsync(
        ReportCreateDto dto,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = dto.Name.Trim();
        if (string.IsNullOrEmpty(normalizedName))
        {
            throw new ArgumentException("Report name cannot be empty or whitespace.");
        }

        var version = await DatasetAccessHelper.GetAccessibleVersionWithColumnsAsync(
            _unitOfWork,
            dto.DatasetVersionId,
            userId,
            isAdmin,
            cancellationToken);

        object requestPayload;
        object resultPayload;

        if (dto.QueryRequest is not null)
        {
            if (dto.QueryRequest.DatasetVersionId != Guid.Empty &&
                dto.QueryRequest.DatasetVersionId != version.Id)
            {
                throw new RequestValidationException(
                    "QueryRequest.DatasetVersionId",
                    "QueryRequest.DatasetVersionId must match the report's DatasetVersionId.");
            }

            dto.QueryRequest.DatasetVersionId = version.Id;

            requestPayload = dto.QueryRequest;
            resultPayload = await _analysisService.ExecuteAsync(
                dto.QueryRequest,
                userId,
                isAdmin,
                cancellationToken);
        }
        else if (dto.StatisticsRequest is not null)
        {
            if (dto.StatisticsRequest.DatasetVersionId != Guid.Empty &&
                dto.StatisticsRequest.DatasetVersionId != version.Id)
            {
                throw new RequestValidationException(
                    "StatisticsRequest.DatasetVersionId",
                    "StatisticsRequest.DatasetVersionId must match the report's DatasetVersionId.");
            }

            dto.StatisticsRequest.DatasetVersionId = version.Id;

            requestPayload = dto.StatisticsRequest;
            resultPayload = await _analysisService.GetStatisticsAsync(
                dto.StatisticsRequest,
                userId,
                isAdmin,
                cancellationToken);
        }
        else
        {
            throw new RequestValidationException(
                nameof(dto.QueryRequest),
                "Either QueryRequest or StatisticsRequest must be provided.");
        }

        var storedDocument = new
        {
            request = requestPayload,
            result = resultPayload
        };

        var serializedResult = JsonSerializer.Serialize(storedDocument, StorageJsonOptions);
        if (Encoding.UTF8.GetByteCount(serializedResult) > MaxResultDataBytes)
        {
            throw new RequestValidationException(
                nameof(Report.ResultData),
                $"Generated report payload exceeds the maximum allowed size of {MaxResultDataBytes} bytes.");
        }

        var report = new Report
        {
            Id = Guid.NewGuid(),
            DatasetVersionId = version.Id,
            UserId = userId,
            Name = normalizedName,
            ReportType = dto.ReportType.ToString(),
            ResultData = serializedResult,
            CreatedAt = DateTime.UtcNow,
            Description = dto.Description?.Trim()
        };

        await _unitOfWork.Reports.AddAsync(report, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return report.ToResponseDto();
    }

    public async Task<PagedResultDto<ReportListItemDto>> GetListAsync(
        Guid userId,
        bool isAdmin,
        Guid? datasetVersionId = null,
        ReportType? reportType = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pageNumber, pageSize);

        Guid? ownerFilter = isAdmin ? null : userId;
        string? reportTypeFilter = reportType?.ToString();

        return await _unitOfWork.Reports.GetPagedAsync(
            ownerFilter,
            datasetVersionId,
            reportTypeFilter,
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public async Task<ReportResponseDto> GetByIdAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var report = await GetAccessibleReportEntityAsync(id, userId, isAdmin, cancellationToken);
        return report.ToResponseDto();
    }

    public async Task DeleteAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var report = await GetAccessibleReportEntityAsync(id, userId, isAdmin, cancellationToken);

        _unitOfWork.Reports.Delete(report);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Report> GetAccessibleReportEntityAsync(
        Guid id,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var report = await _unitOfWork.Reports.GetByIdAsync(id, cancellationToken);

        if (report is null || (!isAdmin && report.UserId != userId))
        {
            throw new NotFoundException($"Report with id '{id}' was not found.");
        }

        return report;
    }

    private static void ValidatePagination(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                "PageNumber must be greater than or equal to 1.");
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                $"PageSize must be between 1 and {MaxPageSize}.");
        }
    }
}