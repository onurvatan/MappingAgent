using MappingAgent.Contracts.Enums;

namespace MappingAgent.Domain.Entities;

public class AccountingDocument
{
    public Guid Id { get; set; }
    public Guid SourceFileId { get; set; }
    public Guid? CounterpartyId { get; set; }
    public DocumentKind DocumentKind { get; set; }
    public DocumentDirection Direction { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly? InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Currency { get; set; } = "GBP";
    public decimal? Subtotal { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? TotalAmount { get; set; }
    public string SuggestedCategory { get; set; } = string.Empty;
    public string ApprovedCategory { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public decimal ConfidenceScore { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public List<AccountingDocumentLine> Lines { get; set; } = [];
}
