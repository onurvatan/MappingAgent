using MappingAgent.Domain.Entities;

namespace MappingAgent.Api.Services;

public interface IIngestionStore
{
    Task<IReadOnlyList<IngestionJob>> GetJobsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SourceFile>> GetFilesByJobIdAsync(Guid ingestionJobId, CancellationToken cancellationToken);
    Task<SourceFile?> GetSourceFileAsync(Guid sourceFileId, CancellationToken cancellationToken);
    Task<MappedAccountingDocument?> GetMappedDocumentBySourceFileIdAsync(Guid sourceFileId, CancellationToken cancellationToken);
}
