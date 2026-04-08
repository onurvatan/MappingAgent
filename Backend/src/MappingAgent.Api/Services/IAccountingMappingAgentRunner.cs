using MappingAgent.Domain.Entities;

namespace MappingAgent.Api.Services;

public interface IAccountingMappingAgentRunner
{
    Task<string> RunAsync(
        SourceFile sourceFile,
        ExtractedDocument extractedDocument,
        CancellationToken cancellationToken);
}
