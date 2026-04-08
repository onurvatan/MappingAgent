namespace MappingAgent.Domain.Entities;

public class AccountingDocumentLine
{
    public Guid Id { get; set; }
    public Guid AccountingDocumentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? LineAmount { get; set; }
    public decimal? TaxRate { get; set; }
}
