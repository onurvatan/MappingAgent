using MappingAgent.Api.Models;

namespace MappingAgent.Api.Services;

public interface IFileParsingService
{
    Task<ParsedDocument> ParseAsync(string path, CancellationToken cancellationToken);
}
