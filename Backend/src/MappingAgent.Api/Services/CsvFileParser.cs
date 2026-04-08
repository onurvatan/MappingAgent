using CsvHelper;
using MappingAgent.Api.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MappingAgent.Api.Services;

public sealed class CsvFileParser : IFileParser
{
    public bool CanParse(string extension) =>
        string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase);

    public async Task<ParsedDocument> ParseAsync(string path, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<string[]>();
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fieldCount = csv.Parser.Count;
            var fields = new string[fieldCount];
            for (var index = 0; index < fieldCount; index++)
            {
                fields[index] = csv.GetField(index) ?? string.Empty;
            }

            rows.Add(fields);
        }

        var rawText = new StringBuilder();
        foreach (var row in rows)
        {
            rawText.AppendLine(string.Join(" | ", row));
        }

        return new ParsedDocument(
            "CsvFileParser",
            rawText.ToString().Trim(),
            JsonSerializer.Serialize(rows),
            []);
    }
}
