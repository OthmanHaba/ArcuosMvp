using Arceus.Application.Common.Interfaces;
using Arceus.Domain.Entities;
using Arceus.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Arceus.Controllers.Integration;

[ApiController]
[Route("api/integration/customers")]
public class CustomersController(
    IContractorRepository contractorRepository,
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateCustomerResponse>> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create contractor (customer)
            var contractor = new Contractor(request.FullName, ContractorType.Customer);
            await contractorRepository.AddAsync(contractor, cancellationToken);

            // Create wallet account for customer
            var walletAccount = contractor.CreateAccount(AccountType.Wallet);
            await accountRepository.AddAsync(walletAccount, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(
                nameof(GetCustomer),
                new { id = contractor.Id },
                new CreateCustomerResponse(contractor.Id, walletAccount.Idddd));
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
            var contractor = await contractorRepository.GetByIdAsync(id, cancellationToken);
            if (contractor == null)
            {
                return NotFound(new { error = "Customer not found" });
            }

            // dont know what to do exactly 
            // For now, this is a placeholder

            contractorRepository.Update(contractor);
            await unitOfWork.SaveChangesAsync(cancellationToken);

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
        var contractor = await contractorRepository.GetByIdAsync(id, cancellationToken);
        if (contractor == null)
        {
            return NotFound(new { error = "Customer not found" });
        }

        var walletAccount = contractor.Accounts.FirstOrDefault(a => a.AccountType == AccountType.Wallet);

        return Ok(new GetCustomerResponse(
            contractor.Id,
            contractor.FullName,
            contractor.CreatedAt,
            walletAccount?.Idddd,
            walletAccount?.Balance.Amount ?? 0
        ));
    }

    [HttpGet("{id}/wallet-balance")]
    public async Task<ActionResult<GetWalletBalanceResponse>> GetWalletBalance(
        long id,
        CancellationToken cancellationToken)
    {
        var walletAccount = await accountRepository.GetByOwnerAndTypeAsync(id, AccountType.Wallet, cancellationToken);
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