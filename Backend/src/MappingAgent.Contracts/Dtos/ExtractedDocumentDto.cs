namespace MappingAgent.Contracts.Dtos;

public sealed record ExtractedDocumentDto(
    Guid Id,
    Guid SourceFileId,
    string RawText,
    string TablesJson,
    string ParserName,
    string ParserWarningsJson);
