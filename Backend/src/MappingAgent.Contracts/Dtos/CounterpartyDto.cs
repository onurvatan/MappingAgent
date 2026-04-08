namespace MappingAgent.Contracts.Dtos;

public sealed record CounterpartyDto(
    Guid Id,
    string Name,
    string Type,
    string TaxIdentifier,
    string Email,
    string DefaultCategory);
