using MappingAgent.Contracts.Enums;

namespace MappingAgent.Contracts.Dtos;

public sealed record MappedDocumentDto(
    Guid Id,
    Guid SourceFileId,
    DocumentKind DocumentKind,
    DocumentDirection Direction,
    string SuggestedCategory,
    decimal ConfidenceScore,
    string MappedDataJson,
    string ValidationIssuesJson,
    string MatchResultsJson,
    ReviewStatus ReviewStatus);
