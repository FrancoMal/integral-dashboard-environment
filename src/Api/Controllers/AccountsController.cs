using System.Security.Claims;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AccountsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var usdToArs = await GetExchangeRate("USD", "ARS");

            var accounts = await _db.Accounts
                .Where(a => a.UserId == userId.Value)
                .OrderBy(a => a.Name)
                .ToListAsync();

            var dtos = accounts.Select(a => ToDto(a, usdToArs)).ToArray();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId.Value);

            if (account is null)
                return NotFound(new { message = "Account not found" });

            var usdToArs = await GetExchangeRate("USD", "ARS");
            return Ok(ToDto(account, usdToArs));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var account = new Account
            {
                UserId = userId.Value,
                Name = request.Name,
                Type = request.Type,
                Currency = request.Currency,
                Balance = 0,
                Color = request.Color,
                Icon = request.Icon,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            var usdToArs = await GetExchangeRate("USD", "ARS");
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, ToDto(account, usdToArs));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAccountRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId.Value);

            if (account is null)
                return NotFound(new { message = "Account not found" });

            if (request.Name is not null) account.Name = request.Name;
            if (request.Type is not null) account.Type = request.Type;
            if (request.Currency is not null) account.Currency = request.Currency;
            if (request.Color is not null) account.Color = request.Color;
            if (request.Icon is not null) account.Icon = request.Icon;
            if (request.IsActive is not null) account.IsActive = request.IsActive.Value;

            await _db.SaveChangesAsync();

            var usdToArs = await GetExchangeRate("USD", "ARS");
            return Ok(ToDto(account, usdToArs));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId.Value);

            if (account is null)
                return NotFound(new { message = "Account not found" });

            var hasTransactions = await _db.Transactions
                .AnyAsync(t => t.AccountId == id || t.DestinationAccountId == id);

            if (hasTransactions)
            {
                account.IsActive = false;
                await _db.SaveChangesAsync();
                return Ok(new { message = "Account deactivated (has existing transactions)" });
            }

            _db.Accounts.Remove(account);
            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    private async Task<decimal> GetExchangeRate(string from, string to)
    {
        var rate = await _db.ExchangeRates
            .Where(r => r.FromCurrency == from && r.ToCurrency == to)
            .OrderByDescending(r => r.Date)
            .FirstOrDefaultAsync();

        return rate?.Rate ?? 0;
    }

    private static AccountDto ToDto(Account a, decimal usdToArs)
    {
        decimal? balanceUsd = null;
        if (a.Currency == "ARS" && usdToArs > 0)
            balanceUsd = Math.Round(a.Balance / usdToArs, 2);
        else if (a.Currency == "USD")
            balanceUsd = a.Balance;

        return new AccountDto(
            Id: a.Id,
            Name: a.Name,
            Type: a.Type,
            Currency: a.Currency,
            Balance: a.Balance,
            BalanceUsd: balanceUsd,
            Color: a.Color,
            Icon: a.Icon,
            IsActive: a.IsActive
        );
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
