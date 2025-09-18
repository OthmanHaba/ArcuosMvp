using Arceus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arceus.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.OrderId)
            .HasColumnName("order_id");

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasMany(t => t.JournalEntries)
            .WithOne(je => je.Transaction)
            .HasForeignKey(je => je.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.OrderId);
        builder.HasIndex(t => t.CreatedAt);

        // Ignore domain events - they are not persisted
        builder.Ignore(t => t.DomainEvents);
    }
}