namespace MappingAgent.Api.Models;

public sealed record ParsedDocument(
    string ParserName,
    string RawText,
    string TablesJson,
    IReadOnlyList<string> Warnings);
