using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models;

namespace Analytical_Engine.Services;

public class CSVImportService : BaseImportService
{
    public CSVImportService(IUserRepository userRepository,
        IDatasetVersionRepository datasetVersionRepository,
        IDatasetRepository datasetRepository,
        IDataRecordRepository dataRecordRepository) : base(userRepository, datasetVersionRepository, datasetRepository, dataRecordRepository)
    {
        
    }
        
    protected override void ValidateFileFormat(Stream fileStream)
    {
        try
        {
            using var reader = new StreamReader(fileStream, leaveOpen: true);

            var firstLine = reader.ReadLine();
            if (firstLine == null)
            {
                throw new InvalidDataException("Invalid CSV file format: File is empty.");
            }
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException("Invalid CSV file format.", ex);
        }
        finally
        {
            fileStream.Position = 0;
        }
    }

    protected override void ValidateFileStructure(Stream fileStream, Schema schema)
    {
        try
        {
            using var reader = new StreamReader(fileStream, leaveOpen: true);

            var headerLine = reader.ReadLine();

            var headers = headerLine.Split(',')
                                    .Select(h => h.Trim().Trim('"'))
                                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var requiredField in schema.Fields.Keys)
            {
                if (!headers.Contains(requiredField))
                {
                    throw new InvalidDataException($"Invalid structure: Missing required schema field '{requiredField}'.");
                }
            }
        }
        catch (Exception ex) when (ex is not InvalidDataException)
        {
            throw new InvalidDataException("CSV file structure doesn't match defined schema.", ex);
        }
        finally
        {
            fileStream.Position = 0;
        }
    }

    protected override IEnumerable<RecordData> ReadRecords(Stream fileStream)
    {
        fileStream.Position = 0;
        using var reader = new StreamReader(fileStream, leaveOpen: true);

        int rowNumber = 1;
        string? line;
        reader.ReadLine();

        while ((line = reader.ReadLine()) != null)
        {
            rowNumber++;
            var values = line.Split(',');
            var content = new Dictionary<string, object?>();

            for (int i = 0; i < values.Length; i++)
            {
                content[$"Column_{i}"] = values[i].Trim();
            }

            yield return new RecordData(rowNumber, content);
        }
    }
}