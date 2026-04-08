using MappingAgent.Contracts.Enums;
using MappingAgent.Domain.Entities;

namespace MappingAgent.Api.Services;

public sealed class InMemoryIngestionStore : IIngestionStore
{
    private readonly List<IngestionJob> _jobs;
    private readonly List<SourceFile> _files;
    private readonly List<MappedAccountingDocument> _mappedDocuments;

    public InMemoryIngestionStore()
    {
        var jobId = Guid.Parse("94dfc415-e0db-4b81-a0e6-c381a4d6a0a4");
        var completedFileId = Guid.Parse("830eff74-fef4-45b4-a7d5-286fa638c7c7");
        var reviewFileId = Guid.Parse("d0303b8c-0410-4ce2-b3f6-d5959f58e42f");
        var failedFileId = Guid.Parse("45ef0f18-f413-4f1c-9d93-a0033c4957e1");

        _jobs =
        [
            new IngestionJob
            {
                Id = jobId,
                FolderPath = @"C:\DemoData\AccountingBatch-April",
                Status = "Completed",
                StartedAtUtc = new DateTimeOffset(2026, 4, 7, 8, 30, 0, TimeSpan.Zero),
                CompletedAtUtc = new DateTimeOffset(2026, 4, 7, 8, 35, 0, TimeSpan.Zero),
                TotalFileCount = 3,
                CompletedFileCount = 1,
                FailedFileCount = 1,
                NeedsReviewFileCount = 1
            }
        ];

        _files =
        [
            new SourceFile
            {
                Id = completedFileId,
                IngestionJobId = jobId,
                AbsolutePath = @"C:\DemoData\AccountingBatch-April\acme-office\invoice-ac-2026-005.pdf",
                FileName = "invoice-ac-2026-005.pdf",
                Extension = ".pdf",
                SizeBytes = 184_012,
                LastModifiedUtc = new DateTimeOffset(2026, 4, 7, 8, 29, 0, TimeSpan.Zero),
                ContentHash = "hash-acme-005",
                Status = FileProcessingStatus.Completed
            },
            new SourceFile
            {
                Id = reviewFileId,
                IngestionJobId = jobId,
                AbsolutePath = @"C:\DemoData\AccountingBatch-April\helios\consulting-march.xlsx",
                FileName = "consulting-march.xlsx",
                Extension = ".xlsx",
                SizeBytes = 91_640,
                LastModifiedUtc = new DateTimeOffset(2026, 4, 7, 8, 29, 10, TimeSpan.Zero),
                ContentHash = "hash-helios-031",
                Status = FileProcessingStatus.NeedsReview
            },
            new SourceFile
            {
                Id = failedFileId,
                IngestionJobId = jobId,
                AbsolutePath = @"C:\DemoData\AccountingBatch-April\unknown\scan-14.txt",
                FileName = "scan-14.txt",
                Extension = ".txt",
                SizeBytes = 2_304,
                LastModifiedUtc = new DateTimeOffset(2026, 4, 7, 8, 29, 22, TimeSpan.Zero),
                ContentHash = "hash-unknown-014",
                Status = FileProcessingStatus.Failed,
                FailureReason = "UnknownDocumentType",
                FailureMessage = "The extracted content does not match an invoice-like accounting document."
            }
        ];

        _mappedDocuments =
        [
            new MappedAccountingDocument
            {
                Id = Guid.Parse("c4ed8835-5433-4884-8af6-7c94d28f5709"),
                SourceFileId = completedFileId,
                DocumentKind = DocumentKind.ExpenseInvoice,
                Direction = DocumentDirection.Expense,
                SuggestedCategory = "OfficeSupplies",
                ConfidenceScore = 0.94m,
                ReviewStatus = ReviewStatus.Approved,
                MappedDataJson = """
                {
                  "invoiceNumber": "AC-2026-005",
                  "counterpartyName": "Acme Office Supplies Ltd",
                  "totalAmount": 156.00,
                  "currency": "GBP"
                }
                """,
                ValidationIssuesJson = "[]",
                MatchResultsJson = """
                [
                  { "type": "counterparty", "result": "matched", "name": "Acme Office Supplies Ltd" }
                ]
                """
            },
            new MappedAccountingDocument
            {
                Id = Guid.Parse("e2d74175-fc85-4490-812f-f1a73cb6d0e9"),
                SourceFileId = reviewFileId,
                DocumentKind = DocumentKind.IncomeInvoice,
                Direction = DocumentDirection.Income,
                SuggestedCategory = "ConsultingRevenue",
                ConfidenceScore = 0.71m,
                ReviewStatus = ReviewStatus.NeedsReview,
                MappedDataJson = """
                {
                  "invoiceNumber": "HC-2026-032",
                  "counterpartyName": "Helios Consulting Group",
                  "totalAmount": 4200.00,
                  "currency": "GBP",
                  "warnings": [ "Due date inferred from sheet notes" ]
                }
                """,
                ValidationIssuesJson = """
                [
                  "Due date was inferred rather than explicitly parsed."
                ]
                """,
                MatchResultsJson = """
                [
                  { "type": "counterparty", "result": "matched", "name": "Helios Consulting Group" },
                  { "type": "duplicate", "result": "clear" }
                ]
                """
            }
        ];
    }

    public Task<IReadOnlyList<IngestionJob>> GetJobsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<IngestionJob>>(_jobs);

    public Task<IReadOnlyList<SourceFile>> GetFilesByJobIdAsync(Guid ingestionJobId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SourceFile>>(
            _files.Where(file => file.IngestionJobId == ingestionJobId).ToArray());

    public Task<SourceFile?> GetSourceFileAsync(Guid sourceFileId, CancellationToken cancellationToken) =>
        Task.FromResult(_files.SingleOrDefault(file => file.Id == sourceFileId));

    public Task<MappedAccountingDocument?> GetMappedDocumentBySourceFileIdAsync(Guid sourceFileId, CancellationToken cancellationToken) =>
        Task.FromResult(_mappedDocuments.SingleOrDefault(document => document.SourceFileId == sourceFileId));
}
