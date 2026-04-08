namespace MappingAgent.Contracts.Dtos;

public sealed record FolderScanResponse(
    Guid IngestionJobId,
    string FolderPath,
    int TotalSupportedFiles,
    IReadOnlyList<string> SupportedExtensions,
    IReadOnlyList<DiscoveredFileDto> Files);
