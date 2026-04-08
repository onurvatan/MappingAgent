using MappingAgent.Contracts.Enums;

namespace MappingAgent.Contracts.Dtos;

public sealed record DiscoveredFileDto(
    string FileName,
    string AbsolutePath,
    string Extension,
    long SizeBytes,
    DateTimeOffset LastModifiedUtc,
    FileProcessingStatus Status);
