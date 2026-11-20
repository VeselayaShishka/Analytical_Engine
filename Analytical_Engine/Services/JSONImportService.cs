using System.Text.Json;
using Analytical_Engine.Data;
using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models;

namespace Analytical_Engine.Services;

public class JSONImportService : BaseImportService
{
    public JSONImportService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        
    }
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

    protected override void ValidateFileStructure(
        Stream fileStream,
        Schema schema)
    {
        try
        {
            using var document = JsonDocument.Parse(fileStream);

            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException(
                    "JSON root element must be an array.");
            }

            int rowNumber = 0;

            foreach (var element in root.EnumerateArray())
            {
                rowNumber++;

                if (element.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidDataException(
                        $"JSON element at row {rowNumber} must be an object.");
                }
            }
        }
        finally
        {
            fileStream.Position = 0;
        }
    }

    protected override IEnumerable<RecordData> ReadRecords(Stream fileStream)
    {
        fileStream.Position = 0;

        using var document = JsonDocument.Parse(fileStream);

        int rowNumber = 0;

        foreach (var element in document.RootElement.EnumerateArray())
        {
            rowNumber++;

            var content = new Dictionary<string, object?>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var property in element.EnumerateObject())
            {
                content[property.Name] = ConvertJsonValue(property.Value);
            }

            yield return new RecordData(rowNumber, content);
        }
    }
    
    private object? ConvertJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),

            JsonValueKind.Number when value.TryGetInt64(out var longValue)
                => longValue,

            JsonValueKind.Number when value.TryGetDecimal(out var decimalValue)
                => decimalValue,

            JsonValueKind.Number => value.GetDouble(),

            JsonValueKind.True => true,

            JsonValueKind.False => false,

            JsonValueKind.Null => null,

            JsonValueKind.Undefined => null,

            JsonValueKind.Object or JsonValueKind.Array
                => value.GetRawText(),

            _ => value.ToString()
        };
    }
}