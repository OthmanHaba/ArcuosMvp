using Arceus.Domain.Enums;

namespace Arceus.Domain.Entities;

public class Contractor
{
    public long Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public ContractorType ContractorType { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<Account> _accounts = new();
    public IReadOnlyList<Account> Accounts => _accounts.AsReadOnly();

    private Contractor() { }

    public Contractor(string fullName, ContractorType contractorType)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        FullName = fullName;
        ContractorType = contractorType;
        CreatedAt = DateTime.UtcNow;
    }

    public Account CreateAccount(AccountType accountType)
    {
        var account = new Account(Id, accountType);
        _accounts.Add(account);
        return account;
    }

    public Account GetAccount(AccountType accountType)
    {
        return _accounts.FirstOrDefault(a => a.AccountType == accountType)
            ?? throw new InvalidOperationException($"Account of type {accountType} not found for contractor {Id}");
    }
}