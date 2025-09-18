using Arceus.Domain.Events;
using Arceus.Domain.ValueObjects;

namespace Arceus.Domain.Entities;

public class Transaction
{
    public long Id { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public long? OrderId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<JournalEntry> _journalEntries = new();
    public IReadOnlyList<JournalEntry> JournalEntries => _journalEntries.AsReadOnly();

    private readonly List<object> _domainEvents = new();
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private Transaction() { }

    public Transaction(string description, long? orderId = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        Description = description;
        OrderId = orderId;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddJournalEntry(long accountId, Money debit, Money credit)
    {
        if (debit < Money.Zero || credit < Money.Zero)
            throw new ArgumentException("Debit and credit amounts cannot be negative");

        if (debit > Money.Zero && credit > Money.Zero)
            throw new ArgumentException("Journal entry cannot have both debit and credit amounts");

        if (debit == Money.Zero && credit == Money.Zero)
            throw new ArgumentException("Journal entry must have either debit or credit amount");

        var journalEntry = new JournalEntry(Id, accountId, debit, credit);
        _journalEntries.Add(journalEntry);
    }

    public void ValidateDoubleEntry()
    {
        if (!_journalEntries.Any())
            throw new InvalidOperationException("Transaction must have at least one journal entry");

        var totalDebits = _journalEntries.Sum(je => je.Debit.Amount);
        var totalCredits = _journalEntries.Sum(je => je.Credit.Amount);

        if (Math.Abs(totalDebits - totalCredits) > 0.0001m)
            throw new InvalidOperationException("Total debits must equal total credits in double-entry accounting");
    }

    public Money GetTotalAmount()
    {
        return new Money(_journalEntries.Sum(je => Math.Max(je.Debit.Amount, je.Credit.Amount)));
    }

    public void MarkComplete()
    {
        ValidateDoubleEntry();

        var totalAmount = GetTotalAmount();
        var @event = new TransactionCreatedEvent(Id, Description, totalAmount, OrderId, CreatedAt);
        _domainEvents.Add(@event);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}