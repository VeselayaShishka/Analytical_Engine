using Analytical_Engine.Data;
using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models;
using CsvHelper;

namespace Analytical_Engine.Services;

public class CSVImportService : BaseImportService
{
    public CSVImportService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        
    }

    private static CsvReaderOptions CreateCsvOptions()
    {
        return new CsvReaderOptions
        {
            HasHeader = true,
            Separator = ',',
            TrimWhitespace = true
        };
    }

    private static IReadOnlyList<string> ReadHeaders(CsvReader csv)
    {
        var headers = csv.Headers;

        if (headers.Count == 0)
        {
            throw new InvalidDataException(
                "CSV file is empty or header is missing.");
        }

        var normalizedHeaders = headers
            .Select(header => header.Trim())
            .ToArray();

        if (normalizedHeaders.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException(
                "CSV contains an empty column name.");
        }

        if (normalizedHeaders
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != normalizedHeaders.Length)
        {
            throw new InvalidDataException("CSV contains duplicate column names.");
        }

        return normalizedHeaders;
    }

    protected override void ValidateFileFormat(Stream fileStream)
    {
        try
        {
            fileStream.Position = 0;

            using var csv = CsvReader.Create(
                fileStream,
                CreateCsvOptions(),
                leaveOpen: true);

            ReadHeaders(csv);

            while (csv.Read())
            {
            }
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException(
                "Invalid CSV file format.",
                ex);
        }
        finally
        {
            fileStream.Position = 0;
        }
    }

    protected override void ValidateFileStructure(
        Stream fileStream,
        Schema schema)
    {
        try
        {
            fileStream.Position = 0;

            using var csv = CsvReader.Create(
                fileStream,
                CreateCsvOptions(),
                leaveOpen: true);

            var headers = ReadHeaders(csv);

            var headerSet = headers.ToHashSet(
                StringComparer.OrdinalIgnoreCase);

            foreach (var field in schema.Fields)
            {
                if (field.Value.IsNullable)
                    continue;

                if (!headerSet.Contains(field.Key))
                {
                    throw new InvalidDataException(
                        $"Invalid structure: Missing required schema field '{field.Key}'.");
                }
            }
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException(
                "CSV file structure doesn't match defined schema.",
                ex);
        }
        finally
        {
            fileStream.Position = 0;
        }
    }

    protected override IEnumerable<RecordData> ReadRecords(
        Stream fileStream)
    {
        fileStream.Position = 0;

        using var csv = CsvReader.Create(
            fileStream,
            CreateCsvOptions(),
            leaveOpen: true);

        var headers = ReadHeaders(csv);

        while (csv.Read())
        {
            var content = new Dictionary<string, object?>(
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Count; i++)
            {
                string? value = i < csv.FieldCount
                    ? csv.GetString(i)
                    : null;

                content[headers[i]] = string.IsNullOrWhiteSpace(value)
                    ? null
                    : value;
            }

            yield return new RecordData(
                (int)csv.RowNumber + 1,
                content);
        }
    }
}