using MappingAgent.Api.Data;
using MappingAgent.Api.Agents;
using MappingAgent.Api.Models;
using MappingAgent.Contracts.Enums;
using MappingAgent.Domain.Entities;
using Microsoft.Extensions.AI;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace MappingAgent.Api.Services;

public sealed class AccountingDocumentAnalysisService(
    AccountingDbContext dbContext,
    AccountingMappingWorkflowAgent workflowAgent) : IAccountingDocumentAnalysisService
{
    public async Task<AccountingAnalysisResult> AnalyzeAsync(
        SourceFile sourceFile,
        ExtractedDocument extractedDocument,
        CancellationToken cancellationToken)
    {
        var agent = workflowAgent.CreateAgent(AccountingMappingWorkflowAgent.AgentName);
        var prompt = BuildPrompt(sourceFile, extractedDocument);
        var result = await agent.RunAsync(
            [new ChatMessage(ChatRole.User, prompt)],
            null,
            null,
            cancellationToken);

        var payload = result.Text ?? string.Empty;
        var draft = JsonSerializer.Deserialize<AgentMappedDocumentDraft>(
            payload,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
            ?? throw new InvalidOperationException("The mapping workflow returned an empty or invalid JSON payload.");

        var counterparties = await dbContext.Counterparties
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        var matchedCounterparty = counterparties
            .OrderByDescending(counterparty => counterparty.Name.Length)
            .FirstOrDefault(counterparty =>
                (!string.IsNullOrWhiteSpace(draft.CounterpartyName) &&
                 draft.CounterpartyName.Contains(counterparty.Name, StringComparison.OrdinalIgnoreCase)) ||
                extractedDocument.RawText.Contains(counterparty.Name, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(counterparty.TaxIdentifier) &&
                 ((draft.CounterpartyTaxIdentifier?.Contains(counterparty.TaxIdentifier, StringComparison.OrdinalIgnoreCase) ?? false) ||
                  extractedDocument.RawText.Contains(counterparty.TaxIdentifier, StringComparison.OrdinalIgnoreCase))));

        var documentKind = ParseDocumentKind(draft.DocumentKind);
        var direction = ParseDirection(draft.Direction);
        var invoiceDate = ParseDate(draft.InvoiceDate);
        var dueDate = ParseDate(draft.DueDate);
        var subtotal = draft.Subtotal;
        var taxAmount = draft.TaxAmount;
        var totalAmount = draft.TotalAmount;
        var currency = string.IsNullOrWhiteSpace(draft.Currency) ? "GBP" : draft.Currency!.ToUpperInvariant();
        var counterpartyName = matchedCounterparty?.Name ?? draft.CounterpartyName;
        var suggestedCategory = string.IsNullOrWhiteSpace(draft.SuggestedCategory)
            ? InferFallbackCategory(direction, matchedCounterparty)
            : draft.SuggestedCategory!;

        var validationIssues = new List<string>();
        var matchResults = new List<object>();
        if (draft.Warnings is { Count: > 0 })
        {
            validationIssues.AddRange(draft.Warnings);
        }

        var confidence = decimal.Clamp(draft.Confidence ?? 0.6m, 0.05m, 0.99m);

        if (matchedCounterparty is not null)
        {
            matchResults.Add(new
            {
                matchType = "counterparty",
                result = "matched",
                counterpartyId = matchedCounterparty.Id,
                counterpartyName = matchedCounterparty.Name,
                counterpartyRecordType = matchedCounterparty.Type
            });
        }
        else
        {
            validationIssues.Add("Counterparty could not be matched to the existing database.");
            matchResults.Add(new { matchType = "counterparty", result = "unmatched" });
        }

        if (documentKind is not DocumentKind.UnknownAccountingDocument)
        {
            confidence = decimal.Min(0.99m, confidence + 0.05m);
        }

        if (string.IsNullOrWhiteSpace(draft.InvoiceNumber))
        {
            validationIssues.Add("Invoice number could not be extracted.");
        }

        if (!totalAmount.HasValue)
        {
            validationIssues.Add("Total amount could not be extracted.");
        }

        bool duplicateInvoice = false;
        if (matchedCounterparty is not null && !string.IsNullOrWhiteSpace(draft.InvoiceNumber))
        {
            duplicateInvoice = await dbContext.AccountingDocuments
                .AsNoTracking()
                .AnyAsync(document =>
                    document.CounterpartyId == matchedCounterparty.Id &&
                    document.InvoiceNumber == draft.InvoiceNumber,
                    cancellationToken);

            matchResults.Add(new
            {
                matchType = "duplicate",
                result = duplicateInvoice ? "duplicate" : "clear",
                invoiceNumber = draft.InvoiceNumber
            });

            if (duplicateInvoice)
            {
                validationIssues.Add("A document with the same counterparty and invoice number already exists.");
            }
        }

        if (subtotal.HasValue && taxAmount.HasValue && totalAmount.HasValue)
        {
            var expectedTotal = subtotal.Value + taxAmount.Value;
            if (Math.Abs(expectedTotal - totalAmount.Value) > 0.01m)
            {
                validationIssues.Add("Subtotal and tax do not reconcile with the total amount.");
            }
        }

        if (documentKind is DocumentKind.UnknownAccountingDocument)
        {
            validationIssues.Add("Document could not be classified as an income invoice, expense invoice, or credit note.");
        }

        var mappedPayload = new
        {
            sourceFileId = sourceFile.Id,
            documentKind,
            direction,
            counterparty = new
            {
                matchedCounterpartyId = matchedCounterparty?.Id,
                counterpartyName,
                counterpartyType = matchedCounterparty?.Type,
                counterpartyTaxIdentifier = draft.CounterpartyTaxIdentifier
            },
            invoice = new
            {
                invoiceNumber = draft.InvoiceNumber,
                invoiceDate,
                dueDate,
                currency,
                subtotal,
                taxAmount,
                totalAmount
            },
            category = suggestedCategory
        };

        if (documentKind is DocumentKind.UnknownAccountingDocument)
        {
            return new AccountingAnalysisResult(
                documentKind,
                direction,
                suggestedCategory,
                confidence,
                JsonSerializer.Serialize(mappedPayload),
                JsonSerializer.Serialize(validationIssues),
                JsonSerializer.Serialize(matchResults),
                ReviewStatus.NeedsReview,
                FileProcessingStatus.Failed,
                "UnknownDocumentType",
                "The parsed content could not be mapped to a supported accounting document.");
        }

        var requiresReview =
            validationIssues.Count > 0 ||
            confidence < 0.85m ||
            matchedCounterparty is null;

        return new AccountingAnalysisResult(
            documentKind,
            direction,
            suggestedCategory,
            confidence,
            JsonSerializer.Serialize(mappedPayload),
            JsonSerializer.Serialize(validationIssues),
            JsonSerializer.Serialize(matchResults),
            requiresReview ? ReviewStatus.NeedsReview : ReviewStatus.Pending,
            requiresReview ? FileProcessingStatus.NeedsReview : FileProcessingStatus.Completed,
            requiresReview ? null : null,
            requiresReview ? null : null);
    }

    private static string BuildPrompt(SourceFile sourceFile, ExtractedDocument extractedDocument)
    {
        return $$"""
Analyze this parsed accounting document and return structured JSON only.

Supported document kinds:
- ExpenseInvoice
- IncomeInvoice
- CreditNote
- UnknownAccountingDocument

Supported directions:
- Expense
- Income
- Unknown

Parsed input:
{
  "sourceFileName": "{{sourceFile.FileName}}",
  "extension": "{{sourceFile.Extension}}",
  "parserName": "{{extractedDocument.ParserName}}",
  "parserWarningsJson": {{extractedDocument.ParserWarningsJson}},
  "rawText": {{JsonSerializer.Serialize(extractedDocument.RawText)}}
}
""";
    }

    private static DocumentKind ParseDocumentKind(string? value)
    {
        return Enum.TryParse<DocumentKind>(value, ignoreCase: true, out var parsed)
            ? parsed
            : DocumentKind.UnknownAccountingDocument;
    }

    private static DocumentDirection ParseDirection(string? value)
    {
        return Enum.TryParse<DocumentDirection>(value, ignoreCase: true, out var parsed)
            ? parsed
            : DocumentDirection.Unknown;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var invariant))
        {
            return invariant;
        }

        if (DateOnly.TryParse(value, CultureInfo.GetCultureInfo("en-GB"), out var gb))
        {
            return gb;
        }

        return null;
    }

    private static string InferFallbackCategory(DocumentDirection direction, Counterparty? matchedCounterparty)
    {
        if (!string.IsNullOrWhiteSpace(matchedCounterparty?.DefaultCategory))
        {
            return matchedCounterparty.DefaultCategory;
        }

        return direction == DocumentDirection.Income ? "OtherIncome" : "OtherExpense";
    }
}
