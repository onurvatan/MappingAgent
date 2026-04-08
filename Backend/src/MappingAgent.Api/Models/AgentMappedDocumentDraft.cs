namespace MappingAgent.Api.Models;

public sealed class AgentMappedDocumentDraft
{
    public string DocumentKind { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string? CounterpartyName { get; set; }
    public string? CounterpartyTaxIdentifier { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? InvoiceDate { get; set; }
    public string? DueDate { get; set; }
    public string? Currency { get; set; }
    public decimal? Subtotal { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? SuggestedCategory { get; set; }
    public decimal? Confidence { get; set; }
    public List<string>? Warnings { get; set; }
}
