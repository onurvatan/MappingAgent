namespace MappingAgent.Contracts.Dtos;

public sealed record IngestionJobSummaryDto(
    Guid Id,
    string FolderPath,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    int TotalFileCount,
    int CompletedFileCount,
    int FailedFileCount,
    int NeedsReviewFileCount);
