using MappingAgent.Contracts.Enums;

namespace MappingAgent.Contracts.Dtos;

public sealed record SourceFileDetailDto(
    Guid Id,
    Guid IngestionJobId,
    string FileName,
    string AbsolutePath,
    string Extension,
    long SizeBytes,
    DateTimeOffset LastModifiedUtc,
    FileProcessingStatus Status,
    string? FailureReason,
    string? FailureMessage,
    MappedDocumentDto? MappedDocument);
