using MappingAgent.Api.Models;

namespace MappingAgent.Api.Services;

public sealed class FileParsingService(IEnumerable<IFileParser> parsers) : IFileParsingService
{
    private readonly IReadOnlyList<IFileParser> _parsers = parsers.ToArray();

    public Task<ParsedDocument> ParseAsync(string path, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(path);
        var parser = _parsers.FirstOrDefault(candidate => candidate.CanParse(extension));

        if (parser is null)
        {
            throw new InvalidOperationException($"No parser registered for extension '{extension}'.");
        }

        return parser.ParseAsync(path, cancellationToken);
    }
}
