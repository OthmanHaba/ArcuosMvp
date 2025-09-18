using Arceus.Application.Common.Interfaces;
using Arceus.Domain.Entities;
using Arceus.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Arceus.Controllers.Integration;

[ApiController]
[Route("api/integration/customers")]
public class CustomersController : ControllerBase
{
    private readonly IContractorRepository _contractorRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomersController(
        IContractorRepository contractorRepository,
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork)
    {
        _contractorRepository = contractorRepository;
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpPost]
    public async Task<ActionResult<CreateCustomerResponse>> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create contractor (customer)
            var contractor = new Contractor(request.FullName, ContractorType.Customer);
            await _contractorRepository.AddAsync(contractor, cancellationToken);

            // Create wallet account for customer
            var walletAccount = contractor.CreateAccount(AccountType.Wallet);
            await _accountRepository.AddAsync(walletAccount, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetCustomer),
                new { id = contractor.Id },
                new CreateCustomerResponse(contractor.Id, walletAccount.Id));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateCustomer(
        long id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var contractor = await _contractorRepository.GetByIdAsync(id, cancellationToken);
            if (contractor == null)
            {
                return NotFound(new { error = "Customer not found" });
            }

            // Update would require adding update methods to the domain entity
            // For now, this is a placeholder

            _contractorRepository.Update(contractor);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GetCustomerResponse>> GetCustomer(
        long id,
        CancellationToken cancellationToken)
    {
        var contractor = await _contractorRepository.GetByIdAsync(id, cancellationToken);
        if (contractor == null)
        {
            return NotFound(new { error = "Customer not found" });
        }

        var walletAccount = contractor.Accounts.FirstOrDefault(a => a.AccountType == AccountType.Wallet);

        return Ok(new GetCustomerResponse(
            contractor.Id,
            contractor.FullName,
            contractor.CreatedAt,
            walletAccount?.Id,
            walletAccount?.Balance.Amount ?? 0
        ));
    }

    [HttpGet("{id}/wallet-balance")]
    public async Task<ActionResult<GetWalletBalanceResponse>> GetWalletBalance(
        long id,
        CancellationToken cancellationToken)
    {
        var walletAccount = await _accountRepository.GetByOwnerAndTypeAsync(id, AccountType.Wallet, cancellationToken);
        if (walletAccount == null)
        {
            return NotFound(new { error = "Customer wallet not found" });
        }

        return Ok(new GetWalletBalanceResponse(walletAccount.Balance.Amount));
    }
}

public record CreateCustomerRequest(string FullName);

public record CreateCustomerResponse(long CustomerId, long WalletAccountId);

public record UpdateCustomerRequest(string FullName);

public record GetCustomerResponse(
    long Id,
    string FullName,
    DateTime CreatedAt,
    long? WalletAccountId,
    decimal WalletBalance
);

public record GetWalletBalanceResponse(decimal Balance);