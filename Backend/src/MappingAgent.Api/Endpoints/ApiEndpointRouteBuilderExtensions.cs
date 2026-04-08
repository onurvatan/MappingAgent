using MappingAgent.Api.Data;
using MappingAgent.Api.Models;
using MappingAgent.Api.Services;
using MappingAgent.Contracts.Dtos;
using MappingAgent.Contracts.Enums;
using MappingAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MappingAgent.Api.Endpoints;

public static class ApiEndpointRouteBuilderExtensions
{
    private static readonly string[] SupportedExtensions = [".pdf", ".xlsx", ".csv", ".txt"];

    private static readonly string[] ExpenseCategories =
    [
        "Rent", "Utilities", "Software", "OfficeSupplies", "Travel",
        "Marketing", "ProfessionalServices", "Tax", "OtherExpense"
    ];

    private static readonly string[] IncomeCategories =
    [
        "ProductSales", "ServiceRevenue", "SubscriptionRevenue",
        "ConsultingRevenue", "OtherIncome"
    ];

    public static IEndpointRouteBuilder MapMappingAgentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/health", GetHealth).WithName("GetHealth");
        endpoints.MapGet("/api/meta/bootstrap", GetBootstrap).WithName("GetBootstrap");
        endpoints.MapPost("/api/folders/scan", ScanFolderAsync).WithName("ScanFolder");
        endpoints.MapPost("/api/ingestion-jobs/{jobId:guid}/parse", ParseIngestionJobAsync).WithName("ParseIngestionJob");
        endpoints.MapPost("/api/ingestion-jobs/{jobId:guid}/map", MapIngestionJobAsync).WithName("MapIngestionJob");
        endpoints.MapGet("/api/counterparties", GetCounterpartiesAsync).WithName("GetCounterparties");
        endpoints.MapGet("/api/accounting-documents", GetAccountingDocumentsAsync).WithName("GetAccountingDocuments");
        endpoints.MapGet("/api/ingestion-jobs", GetIngestionJobsAsync).WithName("GetIngestionJobs");
        endpoints.MapGet("/api/ingestion-jobs/{jobId:guid}/files", GetIngestionJobFilesAsync).WithName("GetIngestionJobFiles");

        return endpoints;
    }

    private static IResult GetHealth() =>
        Results.Ok(new
        {
            status = "ok",
            utcTime = DateTimeOffset.UtcNow
        });

    private static IResult GetBootstrap()
    {
        var response = new AppBootstrapResponse(
            "MappingAgent",
            "v0.1.0-phase1",
            SupportedExtensions,
            Enum.GetValues<DocumentKind>(),
            Enum.GetValues<FileProcessingStatus>(),
            Enum.GetValues<ReviewStatus>(),
            ExpenseCategories,
            IncomeCategories);

        return Results.Ok(response);
    }

    private static async Task<IResult> ScanFolderAsync(
        FolderScanRequest request,
        IIngestionStore ingestionStore,
        IHashingService hashingService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("MappingAgent.Ingestion");

        if (string.IsNullOrWhiteSpace(request.FolderPath))
        {
            logger.LogWarning("Folder scan rejected because folder path was empty.");
            return Results.BadRequest(new { error = "FolderPath is required." });
        }

        if (!Directory.Exists(request.FolderPath))
        {
            logger.LogWarning("Folder scan failed because folder was not found. Path: {FolderPath}", request.FolderPath);
            return Results.NotFound(new { error = $"Folder not found: {request.FolderPath}" });
        }

        logger.LogInformation("Scanning folder {FolderPath}", request.FolderPath);

        var job = await ingestionStore.CreateJobAsync(new IngestionJob
        {
            Id = Guid.NewGuid(),
            FolderPath = request.FolderPath,
            Status = "Scanned",
            StartedAtUtc = DateTimeOffset.UtcNow
        }, cancellationToken);

        var sourceFiles = new List<SourceFile>();
        foreach (var path in Directory.EnumerateFiles(request.FolderPath, "*.*", SearchOption.AllDirectories)
                     .Where(path => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)))
        {
            var fileInfo = new FileInfo(path);
            sourceFiles.Add(new SourceFile
            {
                Id = Guid.NewGuid(),
                IngestionJobId = job.Id,
                AbsolutePath = fileInfo.FullName,
                FileName = fileInfo.Name,
                Extension = fileInfo.Extension,
                SizeBytes = fileInfo.Length,
                LastModifiedUtc = fileInfo.LastWriteTimeUtc,
                ContentHash = await hashingService.ComputeSha256Async(fileInfo.FullName, cancellationToken),
                Status = FileProcessingStatus.Discovered
            });
        }

        await ingestionStore.ReplaceFilesAsync(job.Id, sourceFiles, cancellationToken);

        job.TotalFileCount = sourceFiles.Count;
        job.CompletedAtUtc = DateTimeOffset.UtcNow;
        await ingestionStore.UpdateJobAsync(job, cancellationToken);

        logger.LogInformation(
            "Folder scan completed for job {JobId}. Discovered {FileCount} supported files.",
            job.Id,
            sourceFiles.Count);

        var files = sourceFiles
            .Select(file => new DiscoveredFileDto(
                file.FileName,
                file.AbsolutePath,
                file.Extension,
                file.SizeBytes,
                file.LastModifiedUtc,
                file.Status))
            .OrderBy(file => file.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Results.Ok(new FolderScanResponse(
            job.Id,
            request.FolderPath,
            files.Length,
            SupportedExtensions,
            files));
    }

    private static async Task<IResult> ParseIngestionJobAsync(
        Guid jobId,
        IIngestionStore ingestionStore,
        IFileParsingService fileParsingService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("MappingAgent.Ingestion");
        var job = await ingestionStore.GetJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            logger.LogWarning("Parse requested for missing ingestion job {JobId}", jobId);
            return Results.NotFound(new { error = $"Ingestion job not found: {jobId}" });
        }

        var files = await ingestionStore.GetFilesByJobIdAsync(jobId, cancellationToken);
        if (files.Count == 0)
        {
            logger.LogWarning("Parse requested for job {JobId} but no files were discovered.", jobId);
            return Results.BadRequest(new { error = "The ingestion job has no discovered files." });
        }

        logger.LogInformation("Parsing started for job {JobId} with {FileCount} files.", jobId, files.Count);

        job.Status = "Parsing";
        job.CompletedFileCount = 0;
        job.FailedFileCount = 0;
        await ingestionStore.UpdateJobAsync(job, cancellationToken);

        foreach (var file in files)
        {
            file.Status = FileProcessingStatus.Parsing;
            file.FailureReason = null;
            file.FailureMessage = null;
            await ingestionStore.UpdateSourceFileAsync(file, cancellationToken);

            try
            {
                var parsedDocument = await fileParsingService.ParseAsync(file.AbsolutePath, cancellationToken);
                await ingestionStore.SaveExtractedDocumentAsync(new ExtractedDocument
                {
                    Id = Guid.NewGuid(),
                    SourceFileId = file.Id,
                    RawText = parsedDocument.RawText,
                    TablesJson = parsedDocument.TablesJson,
                    ParserName = parsedDocument.ParserName,
                    ParserWarningsJson = System.Text.Json.JsonSerializer.Serialize(parsedDocument.Warnings)
                }, cancellationToken);

                file.Status = FileProcessingStatus.Queued;
                job.CompletedFileCount++;
                logger.LogInformation(
                    "Parsed file {FileName} for job {JobId} with parser {ParserName}.",
                    file.FileName,
                    jobId,
                    parsedDocument.ParserName);
            }
            catch (Exception ex)
            {
                file.Status = FileProcessingStatus.Failed;
                file.FailureReason = "ParseFailed";
                file.FailureMessage = ex.Message;
                job.FailedFileCount++;
                logger.LogWarning(
                    ex,
                    "Parsing failed for file {FileName} in job {JobId}.",
                    file.FileName,
                    jobId);
            }

            await ingestionStore.UpdateSourceFileAsync(file, cancellationToken);
        }

        job.Status = "Parsed";
        job.CompletedAtUtc = DateTimeOffset.UtcNow;
        await ingestionStore.UpdateJobAsync(job, cancellationToken);

        logger.LogInformation(
            "Parsing completed for job {JobId}. Parsed {ParsedCount}, failed {FailedCount}.",
            jobId,
            job.CompletedFileCount,
            job.FailedFileCount);

        return Results.Ok(new
        {
            ingestionJobId = job.Id,
            job.Status,
            totalFiles = job.TotalFileCount,
            parsedFiles = job.CompletedFileCount,
            failedFiles = job.FailedFileCount
        });
    }

    private static async Task<IResult> MapIngestionJobAsync(
        Guid jobId,
        IIngestionStore ingestionStore,
        IAccountingDocumentAnalysisService analysisService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("MappingAgent.Ingestion");
        var job = await ingestionStore.GetJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            logger.LogWarning("Map requested for missing ingestion job {JobId}", jobId);
            return Results.NotFound(new { error = $"Ingestion job not found: {jobId}" });
        }

        var files = await ingestionStore.GetFilesByJobIdAsync(jobId, cancellationToken);
        if (files.Count == 0)
        {
            logger.LogWarning("Map requested for job {JobId} but no files were discovered.", jobId);
            return Results.BadRequest(new { error = "The ingestion job has no discovered files." });
        }

        logger.LogInformation("Mapping started for job {JobId} with {FileCount} files.", jobId, files.Count);

        job.Status = "Mapping";
        job.CompletedFileCount = 0;
        job.FailedFileCount = 0;
        job.NeedsReviewFileCount = 0;
        await ingestionStore.UpdateJobAsync(job, cancellationToken);

        foreach (var file in files.Where(file => file.Status != FileProcessingStatus.Failed))
        {
            var extractedDocument = await ingestionStore.GetExtractedDocumentBySourceFileIdAsync(file.Id, cancellationToken);
            if (extractedDocument is null)
            {
                file.Status = FileProcessingStatus.Failed;
                file.FailureReason = "ParseMissing";
                file.FailureMessage = "No extracted document exists for this source file.";
                job.FailedFileCount++;
                logger.LogWarning(
                    "Mapping skipped for file {FileName} in job {JobId} because extracted content was missing.",
                    file.FileName,
                    jobId);
                await ingestionStore.UpdateSourceFileAsync(file, cancellationToken);
                continue;
            }

            file.Status = FileProcessingStatus.Matching;
            await ingestionStore.UpdateSourceFileAsync(file, cancellationToken);

            AccountingAnalysisResult analysisResult;
            try
            {
                analysisResult = await analysisService.AnalyzeAsync(file, extractedDocument, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                file.Status = FileProcessingStatus.Failed;
                file.FailureReason = "AgentNotConfigured";
                file.FailureMessage = ex.Message;
                job.FailedFileCount++;
                logger.LogWarning(
                    ex,
                    "Mapping failed for file {FileName} in job {JobId} because the agent runtime was unavailable.",
                    file.FileName,
                    jobId);
                await ingestionStore.UpdateSourceFileAsync(file, cancellationToken);
                continue;
            }

            await ingestionStore.SaveMappedDocumentAsync(new MappedAccountingDocument
            {
                Id = Guid.NewGuid(),
                SourceFileId = file.Id,
                DocumentKind = analysisResult.DocumentKind,
                Direction = analysisResult.Direction,
                SuggestedCategory = analysisResult.SuggestedCategory,
                ConfidenceScore = analysisResult.ConfidenceScore,
                MappedDataJson = analysisResult.MappedDataJson,
                ValidationIssuesJson = analysisResult.ValidationIssuesJson,
                MatchResultsJson = analysisResult.MatchResultsJson,
                ReviewStatus = analysisResult.ReviewStatus
            }, cancellationToken);

            file.Status = analysisResult.FileStatus;
            file.FailureReason = analysisResult.FailureReason;
            file.FailureMessage = analysisResult.FailureMessage;
            await ingestionStore.UpdateSourceFileAsync(file, cancellationToken);

            switch (analysisResult.FileStatus)
            {
                case FileProcessingStatus.Completed:
                    job.CompletedFileCount++;
                    logger.LogInformation(
                        "Mapped file {FileName} in job {JobId} successfully. Category: {Category}.",
                        file.FileName,
                        jobId,
                        analysisResult.SuggestedCategory);
                    break;
                case FileProcessingStatus.NeedsReview:
                    job.NeedsReviewFileCount++;
                    logger.LogInformation(
                        "Mapped file {FileName} in job {JobId} requires review. Category: {Category}.",
                        file.FileName,
                        jobId,
                        analysisResult.SuggestedCategory);
                    break;
                case FileProcessingStatus.Failed:
                    job.FailedFileCount++;
                    logger.LogWarning(
                        "Mapping failed for file {FileName} in job {JobId}. Reason: {Reason}",
                        file.FileName,
                        jobId,
                        analysisResult.FailureReason);
                    break;
            }
        }

        job.Status = "Mapped";
        job.CompletedAtUtc = DateTimeOffset.UtcNow;
        await ingestionStore.UpdateJobAsync(job, cancellationToken);

        logger.LogInformation(
            "Mapping completed for job {JobId}. Completed {CompletedCount}, review {ReviewCount}, failed {FailedCount}.",
            jobId,
            job.CompletedFileCount,
            job.NeedsReviewFileCount,
            job.FailedFileCount);

        return Results.Ok(new
        {
            ingestionJobId = job.Id,
            job.Status,
            totalFiles = job.TotalFileCount,
            completedFiles = job.CompletedFileCount,
            needsReviewFiles = job.NeedsReviewFileCount,
            failedFiles = job.FailedFileCount
        });
    }

    private static async Task<IResult> GetCounterpartiesAsync(
        AccountingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var counterparties = await dbContext.Counterparties
            .OrderBy(counterparty => counterparty.Name)
            .Select(counterparty => new CounterpartyDto(
                counterparty.Id,
                counterparty.Name,
                counterparty.Type,
                counterparty.TaxIdentifier,
                counterparty.Email,
                counterparty.DefaultCategory))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(counterparties);
    }

    private static async Task<IResult> GetAccountingDocumentsAsync(
        AccountingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var counterparties = await dbContext.Counterparties
            .AsNoTracking()
            .ToDictionaryAsync(counterparty => counterparty.Id, cancellationToken);

        var documents = (await dbContext.AccountingDocuments
            .AsNoTracking()
            .ToArrayAsync(cancellationToken))
            .OrderByDescending(document => document.ApprovedAtUtc)
            .Select(document => new AccountingDocumentSummaryDto(
                document.Id,
                document.InvoiceNumber,
                document.DocumentKind,
                document.Direction,
                document.CounterpartyId.HasValue &&
                counterparties.TryGetValue(document.CounterpartyId.Value, out var counterparty)
                    ? counterparty.Name
                    : "Unmatched",
                document.Currency,
                document.TotalAmount,
                document.ApprovedCategory,
                document.Status,
                document.ConfidenceScore,
                document.ApprovedAtUtc))
            .ToArray();

        return Results.Ok(documents);
    }

    private static async Task<IResult> GetIngestionJobsAsync(
        IIngestionStore ingestionStore,
        CancellationToken cancellationToken)
    {
        var jobs = await ingestionStore.GetJobsAsync(cancellationToken);

        var response = jobs
            .OrderByDescending(job => job.StartedAtUtc)
            .Select(job => new IngestionJobSummaryDto(
                job.Id,
                job.FolderPath,
                job.Status,
                job.StartedAtUtc,
                job.CompletedAtUtc,
                job.TotalFileCount,
                job.CompletedFileCount,
                job.FailedFileCount,
                job.NeedsReviewFileCount))
            .ToArray();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetIngestionJobFilesAsync(
        Guid jobId,
        IIngestionStore ingestionStore,
        CancellationToken cancellationToken)
    {
        var files = await ingestionStore.GetFilesByJobIdAsync(jobId, cancellationToken);

        var response = new List<SourceFileDetailDto>(files.Count);
        foreach (var file in files.OrderBy(file => file.FileName, StringComparer.OrdinalIgnoreCase))
        {
            var mappedDocument = await ingestionStore.GetMappedDocumentBySourceFileIdAsync(file.Id, cancellationToken);
            var extractedDocument = await ingestionStore.GetExtractedDocumentBySourceFileIdAsync(file.Id, cancellationToken);

            response.Add(new SourceFileDetailDto(
                file.Id,
                file.IngestionJobId,
                file.FileName,
                file.AbsolutePath,
                file.Extension,
                file.SizeBytes,
                file.LastModifiedUtc,
                file.Status,
                file.FailureReason,
                file.FailureMessage,
                extractedDocument is null
                    ? null
                    : new ExtractedDocumentDto(
                        extractedDocument.Id,
                        extractedDocument.SourceFileId,
                        extractedDocument.RawText,
                        extractedDocument.TablesJson,
                        extractedDocument.ParserName,
                        extractedDocument.ParserWarningsJson),
                mappedDocument is null
                    ? null
                    : new MappedDocumentDto(
                        mappedDocument.Id,
                        mappedDocument.SourceFileId,
                        mappedDocument.DocumentKind,
                        mappedDocument.Direction,
                        mappedDocument.SuggestedCategory,
                        mappedDocument.ConfidenceScore,
                        mappedDocument.MappedDataJson,
                        mappedDocument.ValidationIssuesJson,
                        mappedDocument.MatchResultsJson,
                        mappedDocument.ReviewStatus)));
        }

        return Results.Ok(response);
    }
}
