using MappingAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MappingAgent.Api.Data;

public sealed class AccountingDbContext(DbContextOptions<AccountingDbContext> options) : DbContext(options)
{
    public DbSet<AccountingDocument> AccountingDocuments => Set<AccountingDocument>();
    public DbSet<AccountingDocumentLine> AccountingDocumentLines => Set<AccountingDocumentLine>();
    public DbSet<Counterparty> Counterparties => Set<Counterparty>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Counterparty>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Type).HasMaxLength(50);
            entity.Property(x => x.TaxIdentifier).HasMaxLength(100);
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.Property(x => x.DefaultCategory).HasMaxLength(100);
            entity.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<AccountingDocument>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.InvoiceNumber).HasMaxLength(100);
            entity.Property(x => x.Currency).HasMaxLength(16);
            entity.Property(x => x.SuggestedCategory).HasMaxLength(100);
            entity.Property(x => x.ApprovedCategory).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.Property(x => x.ConfidenceScore).HasPrecision(5, 4);
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.TaxAmount).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.CounterpartyId, x.InvoiceNumber }).IsUnique();
            entity.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.AccountingDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccountingDocumentLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.Quantity).HasPrecision(18, 2);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.LineAmount).HasPrecision(18, 2);
            entity.Property(x => x.TaxRate).HasPrecision(5, 2);
        });
    }
}
