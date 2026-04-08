using MappingAgent.Api.Models;
using MappingAgent.Domain.Entities;

namespace MappingAgent.Api.Services;

public interface IAccountingDocumentAnalysisService
{
    Task<AccountingAnalysisResult> AnalyzeAsync(
        SourceFile sourceFile,
        ExtractedDocument extractedDocument,
        CancellationToken cancellationToken);
}
