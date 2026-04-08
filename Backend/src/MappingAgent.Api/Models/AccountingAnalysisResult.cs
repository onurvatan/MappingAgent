using MappingAgent.Contracts.Enums;

namespace MappingAgent.Api.Models;

public sealed record AccountingAnalysisResult(
    DocumentKind DocumentKind,
    DocumentDirection Direction,
    string SuggestedCategory,
    decimal ConfidenceScore,
    string MappedDataJson,
    string ValidationIssuesJson,
    string MatchResultsJson,
    ReviewStatus ReviewStatus,
    FileProcessingStatus FileStatus,
    string? FailureReason,
    string? FailureMessage);
