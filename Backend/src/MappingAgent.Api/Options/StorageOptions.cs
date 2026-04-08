namespace MappingAgent.Api.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string IngestionProvider { get; set; } = "InMemory";
    public string ApprovedProvider { get; set; } = "Sqlite";
    public string ApprovedConnectionString { get; set; } = "Data Source=mappingagent.db";
    public string MongoConnectionString { get; set; } = "mongodb://localhost:27017";
    public string MongoDatabaseName { get; set; } = "mapping-agent";
}
