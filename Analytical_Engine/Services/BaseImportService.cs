using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models;
using Analytical_Engine.Models.Entities;

namespace Analytical_Engine.Services;

public abstract class BaseImportService : IImportService
{
    private readonly IUserRepository _userRepository;
    private readonly IDatasetVersionRepository _datasetVersionRepository;
    private readonly IDatasetRepository _datasetRepository;
    private readonly IDataRecordRepository _dataRecordRepository;

    protected BaseImportService(
        IUserRepository userRepository,
        IDatasetVersionRepository datasetVersionRepository,
        IDatasetRepository datasetRepository,
        IDataRecordRepository dataRecordRepository)
    {
        _userRepository = userRepository;
        _datasetVersionRepository = datasetVersionRepository;
        _datasetRepository = datasetRepository;
        _dataRecordRepository = dataRecordRepository;
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

            // 1. Проверка пользователя и принадлежности датасета
            await CheckUserPermissionsAsync(metadata);

            // 2. Проверка формата файла
            fileStream.Position = 0;
            ValidateFileFormat(fileStream);

            // 3. Вычисление SHA256
            string hash = ComputeSHA256(fileStream);

            // 4. Проверка повторного импорта
            var duplicateResult = await VersionExistsAsync(hash);

            if (duplicateResult != null)
                return duplicateResult;

            // 5. Проверка структуры файла
            fileStream.Position = 0;
            ValidateFileStructure(fileStream, schema);

            // 6. Создание новой версии
            DatasetVersion version =
                await CreateNewVersionAsync(metadata, hash);

            var errors = new List<ImportError>();

            long importedRecords = 0;

            // 7. Чтение, проверка и сохранение записей
            fileStream.Position = 0;

            foreach (var record in ReadRecords(fileStream))
            {
                var recordErrors = ValidateRecord(record, schema);

                if (recordErrors.Count > 0)
                {
                    WriteErrors(version, record.RowNumber, recordErrors);
                    errors.Add(new ImportError(
                        record.RowNumber,
                        recordErrors));

                    continue;
                }

                await SaveRecordAsync(version, record);

                importedRecords++;
            }

            // 8. Обновление статистики
            version.RecordCount = importedRecords;

            // 9. Сохранение изменений в БД
            await _datasetVersionRepository.SaveChangesAsync();

            // 10. Возвращение результата
            return ReturnImportResult(version, errors);
        }
        catch (Exception ex)
        {
            HandleError(ex);

            return new ImportResult(
                false,
                $"Import error: {ex.Message}");
        }
    }

    protected async Task CheckUserPermissionsAsync(Metadata metadata)
    {
        //TODO valid or being checked earlier
        var user = await _userRepository.GetByEmailAsync(metadata.Email);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException(
                "User does not exist or is inactive.");

        var dataset = await _datasetRepository.GetByIdAsync(
            metadata.DatasetId);

        if (dataset == null)
            throw new KeyNotFoundException("Dataset not found.");

        if (dataset.UserId != user.Id)
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

    protected async Task<ImportResult?> VersionExistsAsync(string hash)
    {
        var existingVersion =
            await _datasetVersionRepository.GetByHashAsync(hash);

        if (existingVersion != null)
            return ReturnDuplicateImportMessage(hash);

        return null;
    }

    protected virtual ImportResult ReturnDuplicateImportMessage(
        string hash)
    {
        return new ImportResult(
            false,
            $"File with hash '{hash}' already exists.");
    }

    protected virtual async Task<DatasetVersion> CreateNewVersionAsync(
        Metadata metadata,
        string hash)
    {
        var latestVersion =
            await _datasetVersionRepository.GetLatestVersionAsync(
                metadata.DatasetId);

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

        await _datasetVersionRepository.AddAsync(version);

        return version;
    }

    protected List<string> ValidateRecord(
        RecordData record,
        Schema schema)
    {
        var errors = new List<string>();

        foreach (var field in schema.Fields)
        {
            if (!record.Content.TryGetValue(
                    field.Key, out var value) || value == null)
            {
                errors.Add(
                    $"Field '{field.Key}' is absent or NULL.");

                continue;
            }

            if (!IsValidType(value, field.Value))
            {
                errors.Add(
                    $"Field '{field.Key}' has invalid type. " +
                    $"Expected: {field.Value}.");
            }
        }

        return errors;
    }

    private bool IsValidType(object value, string expectedType)
    {
        string stringValue = value.ToString() ?? string.Empty;

        return expectedType.ToLowerInvariant() switch
        {
            "string" => true,
            "int" or "integer" =>
                int.TryParse(stringValue, out _),
            "long" =>
                long.TryParse(stringValue, out _),
            "decimal" =>
                decimal.TryParse(
                    stringValue,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out _),
            "double" =>
                double.TryParse(
                    stringValue,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out _),
            "bool" or "boolean" =>
                bool.TryParse(stringValue, out _),
            "datetime" =>
                DateTime.TryParse(stringValue, out _),
            _ => false
        };
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

        await _dataRecordRepository.AddAsync(dataRecord);
    }

    protected void WriteErrors(
        DatasetVersion version,
        int rowNumber,
        List<string> errors)
    {
        Console.WriteLine(
            $"Import error in version {version.Version}, row {rowNumber}:");

        foreach (var error in errors)
            Console.WriteLine(error);
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