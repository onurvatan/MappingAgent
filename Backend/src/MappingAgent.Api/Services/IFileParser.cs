using MappingAgent.Api.Models;

namespace MappingAgent.Api.Services;

public interface IFileParser
{
    bool CanParse(string extension);
    Task<ParsedDocument> ParseAsync(string path, CancellationToken cancellationToken);
}
