using MappingAgent.Contracts.Enums;

namespace MappingAgent.Contracts.Dtos;

public sealed record AccountingDocumentSummaryDto(
    Guid Id,
    string InvoiceNumber,
    DocumentKind DocumentKind,
    DocumentDirection Direction,
    string CounterpartyName,
    string Currency,
    decimal? TotalAmount,
    string ApprovedCategory,
    string Status,
    decimal ConfidenceScore,
    DateTimeOffset? ApprovedAtUtc);
