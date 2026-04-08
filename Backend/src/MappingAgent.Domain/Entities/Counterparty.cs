namespace MappingAgent.Domain.Entities;

public class Counterparty
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TaxIdentifier { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DefaultCategory { get; set; } = string.Empty;
}
