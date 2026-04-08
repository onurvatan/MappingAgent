namespace MappingAgent.Domain.Entities;

public class ExtractedDocument
{
    public Guid Id { get; set; }
    public Guid SourceFileId { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string TablesJson { get; set; } = "[]";
    public string ParserName { get; set; } = string.Empty;
    public string ParserWarningsJson { get; set; } = "[]";
}
