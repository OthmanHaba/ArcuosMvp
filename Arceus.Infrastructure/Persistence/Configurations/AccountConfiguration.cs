using Arceus.Domain.Entities;
using Arceus.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arceus.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(a => a.AccountType)
            .HasColumnName("account_type")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.Balance)
            .HasColumnName("balance")
            .IsRequired()
            .HasColumnType("decimal(18,4)")
            .HasDefaultValue(0.00m)
            .HasConversion(
                money => money.Amount,
                amount => new Domain.ValueObjects.Money(amount));

        builder.Property(a => a.Version)
            .HasColumnName("version")
            .IsRowVersion();

        builder.HasOne(a => a.Owner)
            .WithMany(c => c.Accounts)
            .HasForeignKey(a => a.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.OwnerId);
        builder.HasIndex(a => new { a.OwnerId, a.AccountType }).IsUnique();
    }
}