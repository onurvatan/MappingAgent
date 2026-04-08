using MappingAgent.Api.Models;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

namespace MappingAgent.Api.Services;

public sealed class PdfFileParser : IFileParser
{
    public bool CanParse(string extension) =>
        string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase);

    public Task<ParsedDocument> ParseAsync(string path, CancellationToken cancellationToken)
    {
        var pages = new List<object>();
        var rawText = new StringBuilder();

        using var document = PdfDocument.Open(path);
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = page.Text;
            pages.Add(new
            {
                pageNumber = page.Number,
                text
            });

            rawText.AppendLine($"[Page {page.Number}]");
            rawText.AppendLine(text);
        }

        return Task.FromResult(new ParsedDocument(
            "PdfFileParser",
            rawText.ToString().Trim(),
            JsonSerializer.Serialize(pages),
            []));
    }
}
