using MappingAgent.Api.Options;
using MappingAgent.Api.Data;
using MappingAgent.Api.Services;
using MappingAgent.Contracts.Dtos;
using MappingAgent.Contracts.Enums;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<AgentWorkflowOptions>(
    builder.Configuration.GetSection(AgentWorkflowOptions.SectionName));
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddDbContext<AccountingDbContext>((serviceProvider, options) =>
{
    var storageOptions = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>()
        .Value;

    options.UseSqlite(storageOptions.ApprovedConnectionString);
});
builder.Services.AddSingleton<IIngestionStore, InMemoryIngestionStore>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();
    await SeedData.InitializeAsync(dbContext, CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("frontend");

var supportedExtensions = new[] { ".pdf", ".xlsx", ".csv", ".txt" };
var expenseCategories = new[]
{
    "Rent", "Utilities", "Software", "OfficeSupplies", "Travel",
    "Marketing", "ProfessionalServices", "Tax", "OtherExpense"
};
var incomeCategories = new[]
{
    "ProductSales", "ServiceRevenue", "SubscriptionRevenue",
    "ConsultingRevenue", "OtherIncome"
};

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    utcTime = DateTimeOffset.UtcNow
}))
.WithName("GetHealth");

app.MapGet("/api/meta/bootstrap", () =>
{
    var response = new AppBootstrapResponse(
        "MappingAgent",
        "v0.1.0-phase1",
        supportedExtensions,
        Enum.GetValues<DocumentKind>(),
        Enum.GetValues<FileProcessingStatus>(),
        Enum.GetValues<ReviewStatus>(),
        expenseCategories,
        incomeCategories);

    return Results.Ok(response);
})
.WithName("GetBootstrap");

app.MapPost("/api/folders/scan", (FolderScanRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.FolderPath))
    {
        return Results.BadRequest(new { error = "FolderPath is required." });
    }

    if (!Directory.Exists(request.FolderPath))
    {
        return Results.NotFound(new { error = $"Folder not found: {request.FolderPath}" });
    }

    var files = Directory.EnumerateFiles(request.FolderPath, "*.*", SearchOption.AllDirectories)
        .Where(path => supportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
        .Select(path =>
        {
            var fileInfo = new FileInfo(path);
            return new DiscoveredFileDto(
                fileInfo.Name,
                fileInfo.FullName,
                fileInfo.Extension,
                fileInfo.Length,
                fileInfo.LastWriteTimeUtc,
                FileProcessingStatus.Discovered);
        })
        .OrderBy(file => file.FileName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    var response = new FolderScanResponse(
        request.FolderPath,
        files.Length,
        supportedExtensions,
        files);

    return Results.Ok(response);
})
.WithName("ScanFolder");

app.MapGet("/api/counterparties", async (AccountingDbContext dbContext, CancellationToken cancellationToken) =>
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
})
.WithName("GetCounterparties");

app.MapGet("/api/accounting-documents", async (AccountingDbContext dbContext, CancellationToken cancellationToken) =>
{
    var documents = await dbContext.AccountingDocuments
        .AsNoTracking()
        .OrderByDescending(document => document.ApprovedAtUtc)
        .Select(document => new AccountingDocumentSummaryDto(
            document.Id,
            document.InvoiceNumber,
            document.DocumentKind,
            document.Direction,
            dbContext.Counterparties
                .Where(counterparty => counterparty.Id == document.CounterpartyId)
                .Select(counterparty => counterparty.Name)
                .FirstOrDefault() ?? "Unmatched",
            document.Currency,
            document.TotalAmount,
            document.ApprovedCategory,
            document.Status,
            document.ConfidenceScore,
            document.ApprovedAtUtc))
        .ToArrayAsync(cancellationToken);

    return Results.Ok(documents);
})
.WithName("GetAccountingDocuments");

app.MapGet("/api/ingestion-jobs", async (IIngestionStore ingestionStore, CancellationToken cancellationToken) =>
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
})
.WithName("GetIngestionJobs");

app.MapGet("/api/ingestion-jobs/{jobId:guid}/files", async (Guid jobId, IIngestionStore ingestionStore, CancellationToken cancellationToken) =>
{
    var files = await ingestionStore.GetFilesByJobIdAsync(jobId, cancellationToken);

    var response = new List<SourceFileDetailDto>(files.Count);
    foreach (var file in files.OrderBy(file => file.FileName, StringComparer.OrdinalIgnoreCase))
    {
        var mappedDocument = await ingestionStore.GetMappedDocumentBySourceFileIdAsync(file.Id, cancellationToken);
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
})
.WithName("GetIngestionJobFiles");

app.Run();
