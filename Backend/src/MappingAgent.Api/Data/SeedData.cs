using MappingAgent.Contracts.Enums;
using MappingAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MappingAgent.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AccountingDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (await dbContext.Counterparties.AnyAsync(cancellationToken))
        {
            return;
        }

        var acmeVendorId = Guid.Parse("7f5969a2-e75e-4719-85db-af3d2b684214");
        var northwindVendorId = Guid.Parse("b53f7ccc-fad9-420b-90be-a88b7d891f95");
        var heliosCustomerId = Guid.Parse("243e89f0-d1ca-4d76-9232-a5f934d76054");
        var blueRetailCustomerId = Guid.Parse("f9616136-b542-4c17-a349-c4633e095f0c");

        dbContext.Counterparties.AddRange(
            new Counterparty
            {
                Id = acmeVendorId,
                Name = "Acme Office Supplies Ltd",
                Type = "Vendor",
                TaxIdentifier = "GB-ACME-204",
                Email = "ap@acme-office.example",
                DefaultCategory = "OfficeSupplies"
            },
            new Counterparty
            {
                Id = northwindVendorId,
                Name = "Northwind Hosting",
                Type = "Vendor",
                TaxIdentifier = "GB-NW-889",
                Email = "billing@northwind-hosting.example",
                DefaultCategory = "Software"
            },
            new Counterparty
            {
                Id = heliosCustomerId,
                Name = "Helios Consulting Group",
                Type = "Customer",
                TaxIdentifier = "GB-HEL-992",
                Email = "finance@helios-group.example",
                DefaultCategory = "ConsultingRevenue"
            },
            new Counterparty
            {
                Id = blueRetailCustomerId,
                Name = "Blue Retail PLC",
                Type = "Customer",
                TaxIdentifier = "GB-BRP-122",
                Email = "accounts@blueretail.example",
                DefaultCategory = "ProductSales"
            });

        dbContext.AccountingDocuments.AddRange(
            new AccountingDocument
            {
                Id = Guid.Parse("a8a80577-a665-4920-8239-f83edab62be6"),
                SourceFileId = Guid.Parse("1cb498c6-5a42-4d4c-a25f-af15f8d195b2"),
                CounterpartyId = acmeVendorId,
                DocumentKind = DocumentKind.ExpenseInvoice,
                Direction = DocumentDirection.Expense,
                InvoiceNumber = "AC-2026-004",
                InvoiceDate = new DateOnly(2026, 3, 18),
                DueDate = new DateOnly(2026, 4, 17),
                Currency = "GBP",
                Subtotal = 240.00m,
                TaxAmount = 48.00m,
                TotalAmount = 288.00m,
                SuggestedCategory = "OfficeSupplies",
                ApprovedCategory = "OfficeSupplies",
                Status = "Approved",
                ConfidenceScore = 0.96m,
                ApprovedBy = "seed",
                ApprovedAtUtc = new DateTimeOffset(2026, 3, 19, 9, 0, 0, TimeSpan.Zero),
                Lines =
                [
                    new AccountingDocumentLine
                    {
                        Id = Guid.Parse("81ee30e2-1d11-4f03-98cf-46b7233eff47"),
                        Description = "Printer cartridges",
                        Quantity = 4,
                        UnitPrice = 60.00m,
                        LineAmount = 240.00m,
                        TaxRate = 20.00m
                    }
                ]
            },
            new AccountingDocument
            {
                Id = Guid.Parse("b6b7db47-5cb0-49ba-9f84-62f558090e1b"),
                SourceFileId = Guid.Parse("d39d3946-06eb-4ae7-8e9a-45ec5ccbeebf"),
                CounterpartyId = heliosCustomerId,
                DocumentKind = DocumentKind.IncomeInvoice,
                Direction = DocumentDirection.Income,
                InvoiceNumber = "HC-2026-031",
                InvoiceDate = new DateOnly(2026, 3, 24),
                DueDate = new DateOnly(2026, 4, 23),
                Currency = "GBP",
                Subtotal = 3500.00m,
                TaxAmount = 700.00m,
                TotalAmount = 4200.00m,
                SuggestedCategory = "ConsultingRevenue",
                ApprovedCategory = "ConsultingRevenue",
                Status = "Approved",
                ConfidenceScore = 0.98m,
                ApprovedBy = "seed",
                ApprovedAtUtc = new DateTimeOffset(2026, 3, 24, 11, 30, 0, TimeSpan.Zero),
                Lines =
                [
                    new AccountingDocumentLine
                    {
                        Id = Guid.Parse("ca91518b-9a9c-4b63-8834-cbfbe21536f3"),
                        Description = "Monthly advisory retainer",
                        Quantity = 1,
                        UnitPrice = 3500.00m,
                        LineAmount = 3500.00m,
                        TaxRate = 20.00m
                    }
                ]
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
