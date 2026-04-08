using MappingAgent.Api.Models;
using System.Text.Json;

namespace MappingAgent.Api.Services;

public sealed class TextFileParser : IFileParser
{
    public bool CanParse(string extension) =>
        string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase);

    public async Task<ParsedDocument> ParseAsync(string path, CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync(path, cancellationToken);
        return new ParsedDocument(
            "TextFileParser",
            text,
            "[]",
            []);
    }
}
