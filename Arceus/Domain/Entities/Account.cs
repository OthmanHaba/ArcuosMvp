using Arceus.Domain.Enums;
using Arceus.Domain.ValueObjects;

namespace Arceus.Domain.Entities;

public class Account
{
    public long Id { get; private set; }
    public long OwnerId { get; private set; }
    public AccountType AccountType { get; private set; }
    public Money Balance { get; private set; }
    public byte[] Version { get; private set; } = Array.Empty<byte>();

    public Contractor Owner { get; private set; } = null!;

    private Account() { }

    public Account(long ownerId, AccountType accountType)
    {
        OwnerId = ownerId;
        AccountType = accountType;
        Balance = Money.Zero;
    }

    public void Debit(Money amount)
    {
        if (amount <= Money.Zero)
            throw new ArgumentException("Debit amount must be positive", nameof(amount));

        if (AccountType == AccountType.Wallet && Balance < amount)
            throw new InvalidOperationException("Insufficient funds in wallet account");

        Balance -= amount;
    }

    public void Credit(Money amount)
    {
        if (amount <= Money.Zero)
            throw new ArgumentException("Credit amount must be positive", nameof(amount));

        Balance += amount;
    }

    public bool HasSufficientFunds(Money amount)
    {
        if (AccountType != AccountType.Wallet)
            return true;

        return Balance >= amount;
    }
}