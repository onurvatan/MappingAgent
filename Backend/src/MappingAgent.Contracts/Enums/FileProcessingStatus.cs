namespace MappingAgent.Contracts.Enums;

public enum FileProcessingStatus
{
    Discovered = 0,
    Queued = 1,
    Parsing = 2,
    Classifying = 3,
    Extracting = 4,
    Matching = 5,
    NeedsReview = 6,
    Completed = 7,
    Failed = 8
}
