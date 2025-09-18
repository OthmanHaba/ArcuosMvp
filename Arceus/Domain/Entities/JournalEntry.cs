using Arceus.Domain.ValueObjects;

namespace Arceus.Domain.Entities;

public class JournalEntry
{
    public long Id { get; private set; }
    public long TransactionId { get; private set; }
    public long AccountId { get; private set; }
    public Money Debit { get; private set; }
    public Money Credit { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Transaction Transaction { get; private set; } = null!;
    public Account Account { get; private set; } = null!;

    private JournalEntry() { }

    public JournalEntry(long transactionId, long accountId, Money debit, Money credit)
    {
        if (debit < Money.Zero || credit < Money.Zero)
            throw new ArgumentException("Debit and credit amounts cannot be negative");

        if (debit > Money.Zero && credit > Money.Zero)
            throw new ArgumentException("Journal entry cannot have both debit and credit amounts");

        if (debit == Money.Zero && credit == Money.Zero)
            throw new ArgumentException("Journal entry must have either debit or credit amount");

        TransactionId = transactionId;
        AccountId = accountId;
        Debit = debit;
        Credit = credit;
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsDebit => Debit > Money.Zero;
    public bool IsCredit => Credit > Money.Zero;
    public Money Amount => IsDebit ? Debit : Credit;
}