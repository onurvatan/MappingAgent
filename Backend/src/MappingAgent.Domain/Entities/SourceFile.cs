using MappingAgent.Contracts.Enums;

namespace MappingAgent.Domain.Entities;

public class SourceFile
{
    public Guid Id { get; set; }
    public Guid IngestionJobId { get; set; }
    public string AbsolutePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset LastModifiedUtc { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public FileProcessingStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public string? FailureMessage { get; set; }
}
