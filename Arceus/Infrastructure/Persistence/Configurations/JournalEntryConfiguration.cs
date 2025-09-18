using Arceus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arceus.Infrastructure.Persistence.Configurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("journal_entries");

        builder.HasKey(je => je.Id);

        builder.Property(je => je.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(je => je.TransactionId)
            .HasColumnName("transaction_id")
            .IsRequired();

        builder.Property(je => je.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(je => je.Debit)
            .HasColumnName("debit")
            .IsRequired()
            .HasColumnType("decimal(18,4)")
            .HasConversion(
                money => money.Amount,
                amount => new Domain.ValueObjects.Money(amount));

        builder.Property(je => je.Credit)
            .HasColumnName("credit")
            .IsRequired()
            .HasColumnType("decimal(18,4)")
            .HasConversion(
                money => money.Amount,
                amount => new Domain.ValueObjects.Money(amount));

        builder.Property(je => je.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(je => je.Transaction)
            .WithMany(t => t.JournalEntries)
            .HasForeignKey(je => je.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(je => je.Account)
            .WithMany()
            .HasForeignKey(je => je.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(je => je.TransactionId);
        builder.HasIndex(je => je.AccountId);
        builder.HasIndex(je => je.CreatedAt);
    }
}