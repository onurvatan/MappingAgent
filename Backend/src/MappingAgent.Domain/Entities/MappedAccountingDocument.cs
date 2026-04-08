using MappingAgent.Contracts.Enums;

namespace MappingAgent.Domain.Entities;

public class MappedAccountingDocument
{
    public Guid Id { get; set; }
    public Guid SourceFileId { get; set; }
    public DocumentKind DocumentKind { get; set; }
    public DocumentDirection Direction { get; set; }
    public string SuggestedCategory { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public string MappedDataJson { get; set; } = "{}";
    public string ValidationIssuesJson { get; set; } = "[]";
    public string MatchResultsJson { get; set; } = "[]";
    public ReviewStatus ReviewStatus { get; set; }
}
