namespace MappingAgent.Domain.Entities;

public class IngestionJob
{
    public Guid Id { get; set; }
    public string FolderPath { get; set; } = string.Empty;
    public string Status { get; set; } = "Created";
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public int TotalFileCount { get; set; }
    public int CompletedFileCount { get; set; }
    public int FailedFileCount { get; set; }
    public int NeedsReviewFileCount { get; set; }
}
