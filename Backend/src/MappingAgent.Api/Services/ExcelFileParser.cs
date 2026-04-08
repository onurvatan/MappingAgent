using ClosedXML.Excel;
using MappingAgent.Api.Models;
using System.Text;
using System.Text.Json;

namespace MappingAgent.Api.Services;

public sealed class ExcelFileParser : IFileParser
{
    public bool CanParse(string extension) =>
        string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase);

    public Task<ParsedDocument> ParseAsync(string path, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(path);
        var warnings = new List<string>();
        var sheets = new List<object>();
        var rawText = new StringBuilder();

        foreach (var worksheet in workbook.Worksheets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var range = worksheet.RangeUsed();
            if (range is null)
            {
                warnings.Add($"Worksheet '{worksheet.Name}' is empty.");
                continue;
            }

            var rows = new List<string[]>();
            foreach (var row in range.RowsUsed())
            {
                var values = row.CellsUsed()
                    .Select(cell => cell.GetFormattedString())
                    .ToArray();

                if (values.Length == 0)
                {
                    continue;
                }

                rows.Add(values);
                rawText.AppendLine($"[{worksheet.Name}] {string.Join(" | ", values)}");
            }

            sheets.Add(new
            {
                worksheet = worksheet.Name,
                rows
            });
        }

        return Task.FromResult(new ParsedDocument(
            "ExcelFileParser",
            rawText.ToString().Trim(),
            JsonSerializer.Serialize(sheets),
            warnings));
    }
}
