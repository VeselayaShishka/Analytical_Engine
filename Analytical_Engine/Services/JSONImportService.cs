using System.Text.Json;
using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models;

namespace Analytical_Engine.Services;

public class JSONImportService : BaseImportService
{
    public JSONImportService(IUserRepository userRepository,
        IDatasetVersionRepository datasetVersionRepository,
        IDatasetRepository datasetRepository,
        IDataRecordRepository dataRecordRepository) : base(userRepository, datasetVersionRepository, datasetRepository, dataRecordRepository)
    {}
    protected override void ValidateFileFormat(Stream fileStream)
    {
        try
        {
            using var document = JsonDocument.Parse(fileStream);
        }
        catch (JsonException)
        {
            throw new InvalidDataException("Invalid JSON file format.");
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
           using var document = JsonDocument.Parse(fileStream);
           
           var root = document.RootElement;
           
           var firstElement = root.EnumerateArray().FirstOrDefault();
           
           foreach (var requiredField in schema.Fields.Keys)
           {
               if (!firstElement.TryGetProperty(requiredField, out _))
               {
                   throw new InvalidDataException($"Invalid structure: Missing required schema field '{requiredField}'.");
               }
           }
        }
        catch (JsonException)
        {
            throw new InvalidDataException("JSON file structure doesn't match defined schema.");
        } 
        finally{ fileStream.Position = 0; }
    }

    protected override IEnumerable<RecordData> ReadRecords(Stream fileStream)
    {
        fileStream.Position = 0;
        using var document = JsonDocument.Parse(fileStream);

        int rowNumber = 0;
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in document.RootElement.EnumerateArray())
            {
                rowNumber++;
                var content = new Dictionary<string, object?>();

                foreach (var property in element.EnumerateObject())
                {
                    content[property.Name] = property.Value.ToString();
                }

                yield return new RecordData(rowNumber, content);
            }
        }
    }
}