using MappingAgent.Api.Data;
using MappingAgent.Api.Models;
using MappingAgent.Contracts.Enums;
using MappingAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MappingAgent.Api.Services;

public sealed partial class AccountingDocumentAnalysisService(AccountingDbContext dbContext) : IAccountingDocumentAnalysisService
{
    public async Task<AccountingAnalysisResult> AnalyzeAsync(
        SourceFile sourceFile,
        ExtractedDocument extractedDocument,
        CancellationToken cancellationToken)
    {
        var rawText = extractedDocument.RawText ?? string.Empty;
        var normalizedText = rawText.ToLowerInvariant();
        var counterparties = await dbContext.Counterparties
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        var matchedCounterparty = counterparties
            .OrderByDescending(counterparty => counterparty.Name.Length)
            .FirstOrDefault(counterparty =>
                normalizedText.Contains(counterparty.Name, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(counterparty.TaxIdentifier) &&
                 normalizedText.Contains(counterparty.TaxIdentifier, StringComparison.OrdinalIgnoreCase)));

        var documentKind = ClassifyDocumentKind(normalizedText, matchedCounterparty);
        var direction = InferDirection(documentKind, normalizedText, matchedCounterparty);

        var invoiceNumber = ExtractInvoiceNumber(rawText);
        var invoiceDate = ExtractDate(rawText, "invoice date", "date");
        var dueDate = ExtractDate(rawText, "due date", "payment due");
        var subtotal = ExtractAmount(rawText, "subtotal", "net amount");
        var taxAmount = ExtractAmount(rawText, "vat", "tax");
        var totalAmount = ExtractAmount(rawText, "total due", "invoice total", "total");
        var currency = DetectCurrency(rawText);
        var counterpartyName = matchedCounterparty?.Name ?? ExtractCounterpartyName(rawText, direction);
        var suggestedCategory = SuggestCategory(normalizedText, direction, matchedCounterparty);

        var validationIssues = new List<string>();
        var matchResults = new List<object>();
        var confidence = 0.55m;

        if (matchedCounterparty is not null)
        {
            confidence += 0.15m;
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
            confidence += 0.10m;
        }

        if (!string.IsNullOrWhiteSpace(invoiceNumber))
        {
            confidence += 0.10m;
        }
        else
        {
            validationIssues.Add("Invoice number could not be extracted.");
        }

        if (totalAmount.HasValue)
        {
            confidence += 0.10m;
        }
        else
        {
            validationIssues.Add("Total amount could not be extracted.");
        }

        if (invoiceDate.HasValue)
        {
            confidence += 0.05m;
        }

        bool duplicateInvoice = false;
        if (matchedCounterparty is not null && !string.IsNullOrWhiteSpace(invoiceNumber))
        {
            duplicateInvoice = await dbContext.AccountingDocuments
                .AsNoTracking()
                .AnyAsync(document =>
                    document.CounterpartyId == matchedCounterparty.Id &&
                    document.InvoiceNumber == invoiceNumber,
                    cancellationToken);

            matchResults.Add(new
            {
                matchType = "duplicate",
                result = duplicateInvoice ? "duplicate" : "clear",
                invoiceNumber
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

        confidence = decimal.Clamp(confidence, 0.05m, 0.99m);

        var mappedPayload = new
        {
            sourceFileId = sourceFile.Id,
            documentKind,
            direction,
            counterparty = new
            {
                matchedCounterpartyId = matchedCounterparty?.Id,
                counterpartyName,
                counterpartyType = matchedCounterparty?.Type
            },
            invoice = new
            {
                invoiceNumber,
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

    private static DocumentKind ClassifyDocumentKind(string normalizedText, Counterparty? matchedCounterparty)
    {
        if (normalizedText.Contains("credit note", StringComparison.Ordinal) ||
            normalizedText.Contains("credit memo", StringComparison.Ordinal))
        {
            return DocumentKind.CreditNote;
        }

        if (matchedCounterparty?.Type.Equals("Vendor", StringComparison.OrdinalIgnoreCase) == true)
        {
            return DocumentKind.ExpenseInvoice;
        }

        if (matchedCounterparty?.Type.Equals("Customer", StringComparison.OrdinalIgnoreCase) == true)
        {
            return DocumentKind.IncomeInvoice;
        }

        if (normalizedText.Contains("bill to", StringComparison.Ordinal) ||
            normalizedText.Contains("customer", StringComparison.Ordinal) ||
            normalizedText.Contains("consulting", StringComparison.Ordinal) ||
            normalizedText.Contains("service revenue", StringComparison.Ordinal))
        {
            return DocumentKind.IncomeInvoice;
        }

        if (normalizedText.Contains("supplier", StringComparison.Ordinal) ||
            normalizedText.Contains("vendor", StringComparison.Ordinal) ||
            normalizedText.Contains("amount due", StringComparison.Ordinal) ||
            normalizedText.Contains("purchase", StringComparison.Ordinal))
        {
            return DocumentKind.ExpenseInvoice;
        }

        if (normalizedText.Contains("invoice", StringComparison.Ordinal))
        {
            return DocumentKind.ExpenseInvoice;
        }

        return DocumentKind.UnknownAccountingDocument;
    }

    private static DocumentDirection InferDirection(
        DocumentKind documentKind,
        string normalizedText,
        Counterparty? matchedCounterparty)
    {
        if (matchedCounterparty?.Type.Equals("Vendor", StringComparison.OrdinalIgnoreCase) == true)
        {
            return DocumentDirection.Expense;
        }

        if (matchedCounterparty?.Type.Equals("Customer", StringComparison.OrdinalIgnoreCase) == true)
        {
            return DocumentDirection.Income;
        }

        return documentKind switch
        {
            DocumentKind.ExpenseInvoice => DocumentDirection.Expense,
            DocumentKind.IncomeInvoice => DocumentDirection.Income,
            DocumentKind.CreditNote when normalizedText.Contains("refund", StringComparison.Ordinal) => DocumentDirection.Expense,
            DocumentKind.CreditNote => DocumentDirection.Income,
            _ => DocumentDirection.Unknown
        };
    }

    private static string SuggestCategory(string normalizedText, DocumentDirection direction, Counterparty? matchedCounterparty)
    {
        if (!string.IsNullOrWhiteSpace(matchedCounterparty?.DefaultCategory))
        {
            return matchedCounterparty.DefaultCategory;
        }

        if (direction == DocumentDirection.Income)
        {
            if (normalizedText.Contains("consult", StringComparison.Ordinal))
            {
                return "ConsultingRevenue";
            }

            if (normalizedText.Contains("subscription", StringComparison.Ordinal))
            {
                return "SubscriptionRevenue";
            }

            return "ServiceRevenue";
        }

        if (normalizedText.Contains("hosting", StringComparison.Ordinal) ||
            normalizedText.Contains("license", StringComparison.Ordinal) ||
            normalizedText.Contains("subscription", StringComparison.Ordinal))
        {
            return "Software";
        }

        if (normalizedText.Contains("office", StringComparison.Ordinal) ||
            normalizedText.Contains("printer", StringComparison.Ordinal) ||
            normalizedText.Contains("stationery", StringComparison.Ordinal))
        {
            return "OfficeSupplies";
        }

        if (normalizedText.Contains("travel", StringComparison.Ordinal) ||
            normalizedText.Contains("hotel", StringComparison.Ordinal) ||
            normalizedText.Contains("flight", StringComparison.Ordinal))
        {
            return "Travel";
        }

        return direction == DocumentDirection.Income ? "OtherIncome" : "OtherExpense";
    }

    private static string? ExtractInvoiceNumber(string rawText)
    {
        var match = InvoiceNumberRegex().Match(rawText);
        return match.Success ? match.Groups[2].Value.Trim() : null;
    }

    private static DateOnly? ExtractDate(string rawText, params string[] labels)
    {
        foreach (var label in labels)
        {
            var regex = new Regex($@"{Regex.Escape(label)}\s*[:#-]?\s*([A-Za-z0-9/\- ]+)", RegexOptions.IgnoreCase);
            var match = regex.Match(rawText);
            if (!match.Success)
            {
                continue;
            }

            var candidate = match.Groups[1].Value.Trim();
            if (DateOnly.TryParse(candidate, CultureInfo.GetCultureInfo("en-GB"), out var parsedGb))
            {
                return parsedGb;
            }

            if (DateOnly.TryParse(candidate, CultureInfo.GetCultureInfo("en-US"), out var parsedUs))
            {
                return parsedUs;
            }
        }

        return null;
    }

    private static decimal? ExtractAmount(string rawText, params string[] labels)
    {
        foreach (var label in labels)
        {
            var regex = new Regex($@"{Regex.Escape(label)}\s*[:#-]?\s*(GBP|USD|EUR|£|\$|€)?\s*([0-9,]+(?:\.[0-9]{{2}})?)", RegexOptions.IgnoreCase);
            var match = regex.Match(rawText);
            if (match.Success &&
                decimal.TryParse(match.Groups[2].Value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var amount))
            {
                return amount;
            }
        }

        return null;
    }

    private static string DetectCurrency(string rawText)
    {
        if (rawText.Contains("GBP", StringComparison.OrdinalIgnoreCase) || rawText.Contains('£'))
        {
            return "GBP";
        }

        if (rawText.Contains("EUR", StringComparison.OrdinalIgnoreCase) || rawText.Contains('€'))
        {
            return "EUR";
        }

        if (rawText.Contains("USD", StringComparison.OrdinalIgnoreCase) || rawText.Contains('$'))
        {
            return "USD";
        }

        return "GBP";
    }

    private static string? ExtractCounterpartyName(string rawText, DocumentDirection direction)
    {
        var lines = rawText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var anchorPatterns = direction == DocumentDirection.Income
            ? new[] { "bill to", "customer", "client" }
            : new[] { "supplier", "vendor", "from" };

        for (var index = 0; index < lines.Length; index++)
        {
            if (!anchorPatterns.Any(pattern => lines[index].Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (index + 1 < lines.Length && !string.IsNullOrWhiteSpace(lines[index + 1]))
            {
                return lines[index + 1];
            }
        }

        return lines.FirstOrDefault(line =>
            !line.Contains("invoice", StringComparison.OrdinalIgnoreCase) &&
            !line.Contains("total", StringComparison.OrdinalIgnoreCase) &&
            !line.Contains("date", StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex(@"(invoice(?:\s*(number|no|#))?|inv(?:oice)?\s*#?)\s*[:#-]?\s*([A-Z0-9\-/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex InvoiceNumberRegex();
}
