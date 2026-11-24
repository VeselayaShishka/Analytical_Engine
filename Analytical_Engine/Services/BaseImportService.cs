
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models;
using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Services;

public abstract class BaseImportService : IImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private static readonly ColumnValueConverter ValueConverter = new();

    protected BaseImportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ImportResult> ImportDataset(
        Stream fileStream,
        Schema schema,
        Metadata metadata)
    {
        try
        {
            if (!fileStream.CanSeek)
                throw new InvalidOperationException(
                    "File stream must support seeking.");

            await CheckUserPermissionsAsync(metadata);

            fileStream.Position = 0;
            ValidateFileFormat(fileStream);

            string hash = ComputeSHA256(fileStream);

            var existingVersion =
                await _unitOfWork.DatasetVersions
                    .GetByDatasetIdAndHashAsync(metadata.DatasetId, hash);

            if (existingVersion != null)
            {
                return new ImportResult(
                    false,
                    "This file has already been imported into this dataset.",
                    existingVersion,
                    new List<ImportError>());
            }

            fileStream.Position = 0;
            ValidateFileStructure(fileStream, schema);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                DatasetVersion version =
                    await CreateNewVersionAsync(metadata, hash);

                var columns = schema.Fields
                    .Select((field, index) => new DatasetColumn
                    {
                        Id = Guid.NewGuid(),
                        DatasetVersionId = version.Id,
                        Name = field.Key,
                        DataType = field.Value.Type,
                        IsNullable = field.Value.IsNullable,
                        Position = index,
                        Description = field.Value.Description
                    })
                    .ToList();

                foreach (var column in columns)
                {
                    await _unitOfWork.DatasetColumns.AddAsync(column);
                }

                version.Columns = columns;

                var errors = new List<ImportError>();
                long importedRecords = 0;

                fileStream.Position = 0;

                foreach (var record in ReadRecords(fileStream))
                {
                    var recordErrors = ValidateRecord(record, schema);

                    if (recordErrors.Count > 0)
                    {
                        errors.Add(
                            new ImportError(record.RowNumber, recordErrors));

                        continue;
                    }

                    await SaveRecordAsync(version, record);
                    importedRecords++;
                }

                version.RecordCount = importedRecords;

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ReturnImportResult(version, errors);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            HandleError(ex);

            _unitOfWork.ClearTracker();

            try
            {
                await _unitOfWork.ErrorLogs.AddAsync(new ErrorLog
                {
                    Id = Guid.NewGuid(),
                    DatasetId = metadata.DatasetId,
                    Operation = "DatasetImport",
                    ErrorType = ex.GetType().Name,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    OccurredAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception loggingException)
            {
                Console.WriteLine(
                    $"[ErrorLog persistence failed]: {loggingException}");
            }

            return new ImportResult(
                false,
                $"Import error: {ex.Message}",
                null,
                new List<ImportError>());
        }
    }

    protected async Task CheckUserPermissionsAsync(Metadata metadata)
    {
        var user = await _unitOfWork.Users
            .GetByEmailAsync(metadata.Email);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException(
                "User does not exist or is inactive.");

        var dataset = await _unitOfWork.Datasets
            .GetByIdAsync(metadata.DatasetId);

        if (dataset == null)
            throw new KeyNotFoundException("Dataset not found.");

        if (user.Role != Roles.DataAdministrator && dataset.UserId != user.Id)
            throw new UnauthorizedAccessException(
                "User does not have access to this dataset.");
    }

    private string ComputeSHA256(Stream stream)
    {
        stream.Position = 0;

        using var sha256 = SHA256.Create();

        byte[] hashBytes = sha256.ComputeHash(stream);

        stream.Position = 0;

        return Convert.ToHexString(hashBytes);
    }

    protected virtual async Task<DatasetVersion> CreateNewVersionAsync(
        Metadata metadata,
        string hash)
    {
        var latestVersion =
            await _unitOfWork.DatasetVersions
                .GetLatestVersionAsync(metadata.DatasetId);

        var dataset =
            await _unitOfWork.Datasets
                .GetByIdAsync(metadata.DatasetId);

        if (dataset != null)
        {
            dataset.ModifiedAt = DateTime.UtcNow;
        }

        var version = new DatasetVersion
        {
            Id = Guid.NewGuid(),
            DatasetId = metadata.DatasetId,
            Version = (latestVersion?.Version ?? 0) + 1,
            Format = metadata.Format,
            FileName = metadata.SourceName,
            FileHash = hash,
            RecordCount = 0,
            Created = DateTime.UtcNow
        };

        await _unitOfWork.DatasetVersions.AddAsync(version);

        return version;
    }

    protected List<string> ValidateRecord(
        RecordData record,
        Schema schema)
    {
        var errors = new List<string>();

        foreach (var field in schema.Fields)
        {
            var fieldName = field.Key;
            var fieldSchema = field.Value;

            if (!record.Content.TryGetValue(fieldName, out var value) ||
                value == null)
            {
                if (!fieldSchema.IsNullable)
                {
                    errors.Add(
                        $"Field '{fieldName}' is absent or NULL.");
                }

                continue;
            }

            if (!IsValidType(value, fieldSchema.Type))
            {
                errors.Add(
                    $"Field '{fieldName}' has invalid type. " +
                    $"Expected: {fieldSchema.Type}.");
            }
        }

        return errors;
    }

    private bool IsValidType(object value, string expectedType)
    {
        return ValueConverter.IsValidRawValue(value, expectedType);
    }

    protected async Task SaveRecordAsync(
        DatasetVersion version,
        RecordData record)
    {
        var dataRecord = new DataRecord
        {
            Id = Guid.NewGuid(),
            DatasetVersionId = version.Id,
            RowNumber = record.RowNumber,
            Data = JsonSerializer.Serialize(record.Content)
        };

        await _unitOfWork.DataRecords.AddAsync(dataRecord);
    }

    protected virtual ImportResult ReturnImportResult(
        DatasetVersion version,
        List<ImportError> errors)
    {
        string message = errors.Count == 0
            ? "Import completed successfully."
            : $"Import completed with {errors.Count} invalid rows.";

        return new ImportResult(
            true,
            message,
            version,
            errors);
    }

    private void HandleError(Exception exception)
    {
        Console.WriteLine($"[Import Error]: {exception}");
    }

    protected abstract void ValidateFileFormat(Stream fileStream);

    protected abstract void ValidateFileStructure(
        Stream fileStream,
        Schema schema);

    protected abstract IEnumerable<RecordData> ReadRecords(
        Stream fileStream);
}
