using System.ComponentModel.DataAnnotations;

namespace MappingAgent.Api.Configuration;

public sealed class FoundrySettings
{
    public const string SectionName = "Foundry";

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string Deployment { get; set; } = string.Empty;

    public string? ApiKey { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(Deployment);
}
