using MappingAgent.Domain.Entities;

namespace MappingAgent.Api.Services;

public sealed class UnavailableAccountingMappingAgentRunner : IAccountingMappingAgentRunner
{
    public Task<string> RunAsync(
        SourceFile sourceFile,
        ExtractedDocument extractedDocument,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(
            "Accounting mapping agent is not configured. Set Foundry:Endpoint and Foundry:Deployment before running the mapping workflow.");
    }
}
