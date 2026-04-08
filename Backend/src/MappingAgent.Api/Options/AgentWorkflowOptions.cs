namespace MappingAgent.Api.Options;

public sealed class AgentWorkflowOptions
{
    public const string SectionName = "AgentWorkflow";

    public string ChatModel { get; set; } = "gpt-4o";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public int MaxParallelFiles { get; set; } = 2;
    public bool RequireReviewBeforeApproval { get; set; } = true;
}
