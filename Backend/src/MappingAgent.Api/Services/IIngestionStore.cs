using MappingAgent.Domain.Entities;

namespace MappingAgent.Api.Services;

public interface IIngestionStore
{
    Task<IReadOnlyList<IngestionJob>> GetJobsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SourceFile>> GetFilesByJobIdAsync(Guid ingestionJobId, CancellationToken cancellationToken);
    Task<SourceFile?> GetSourceFileAsync(Guid sourceFileId, CancellationToken cancellationToken);
    Task<IngestionJob?> GetJobAsync(Guid ingestionJobId, CancellationToken cancellationToken);
    Task<IngestionJob> CreateJobAsync(IngestionJob ingestionJob, CancellationToken cancellationToken);
    Task UpdateJobAsync(IngestionJob ingestionJob, CancellationToken cancellationToken);
    Task ReplaceFilesAsync(Guid ingestionJobId, IReadOnlyList<SourceFile> files, CancellationToken cancellationToken);
    Task UpdateSourceFileAsync(SourceFile sourceFile, CancellationToken cancellationToken);
    Task<ExtractedDocument?> GetExtractedDocumentBySourceFileIdAsync(Guid sourceFileId, CancellationToken cancellationToken);
    Task SaveExtractedDocumentAsync(ExtractedDocument extractedDocument, CancellationToken cancellationToken);
    Task<MappedAccountingDocument?> GetMappedDocumentBySourceFileIdAsync(Guid sourceFileId, CancellationToken cancellationToken);
    Task SaveMappedDocumentAsync(MappedAccountingDocument mappedAccountingDocument, CancellationToken cancellationToken);
}
