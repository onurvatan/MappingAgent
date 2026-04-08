namespace MappingAgent.Contracts.Dtos;

public sealed record FolderScanResponse(
    string FolderPath,
    int TotalSupportedFiles,
    IReadOnlyList<string> SupportedExtensions,
    IReadOnlyList<DiscoveredFileDto> Files);
