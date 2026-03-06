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
public class TransactionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TransactionsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? month,
        [FromQuery] int? year,
        [FromQuery] string? type,
        [FromQuery] int? category,
        [FromQuery] int? account,
        [FromQuery] string? search,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var query = _db.Transactions
                .Include(t => t.Account)
                .Include(t => t.DestinationAccount)
                .Include(t => t.Category)
                .Where(t => t.UserId == userId.Value);

            // Date range takes priority over month/year
            if (dateFrom.HasValue && dateTo.HasValue)
            {
                query = query.Where(t => t.Date >= dateFrom.Value && t.Date < dateTo.Value.Date.AddDays(1));
            }
            else if (month.HasValue && year.HasValue)
            {
                query = query.Where(t => t.Date.Month == month.Value && t.Date.Year == year.Value);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(t => t.Type == type);
            }

            if (category.HasValue)
            {
                query = query.Where(t => t.CategoryId == category.Value);
            }

            if (account.HasValue)
            {
                query = query.Where(t => t.AccountId == account.Value || t.DestinationAccountId == account.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(t =>
                    (t.Description != null && t.Description.ToLower().Contains(searchLower)) ||
                    (t.Detail != null && t.Detail.ToLower().Contains(searchLower)));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 200) pageSize = 200;

            var transactions = await query
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = transactions.Select(ToDto).ToArray();

            return Ok(new PaginatedResponse<TransactionDto>(
                Data: dtos,
                Page: page,
                PageSize: pageSize,
                TotalCount: totalCount,
                TotalPages: totalPages
            ));
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

            var transaction = await _db.Transactions
                .Include(t => t.Account)
                .Include(t => t.DestinationAccount)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId.Value);

            if (transaction is null)
                return NotFound(new { message = "Transaction not found" });

            return Ok(ToDto(transaction));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var validTypes = new[] { "Ingreso", "Egreso", "Transferencia" };
            if (!validTypes.Contains(request.Type))
                return BadRequest(new { message = "Type must be Ingreso, Egreso, or Transferencia" });

            if (request.Amount <= 0)
                return BadRequest(new { message = "Amount must be greater than zero" });

            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId.Value);
            if (account is null)
                return BadRequest(new { message = "Source account not found" });

            Account? destAccount = null;
            if (request.Type == "Transferencia")
            {
                if (request.DestinationAccountId is null)
                    return BadRequest(new { message = "Destination account is required for transfers" });

                destAccount = await _db.Accounts
                    .FirstOrDefaultAsync(a => a.Id == request.DestinationAccountId && a.UserId == userId.Value);
                if (destAccount is null)
                    return BadRequest(new { message = "Destination account not found" });
            }

            if (request.CategoryId is not null)
            {
                var categoryExists = await _db.Categories
                    .AnyAsync(c => c.Id == request.CategoryId && c.UserId == userId.Value);
                if (!categoryExists)
                    return BadRequest(new { message = "Category not found" });
            }

            using var dbTransaction = await _db.Database.BeginTransactionAsync();

            var transaction = new Transaction
            {
                UserId = userId.Value,
                AccountId = request.AccountId,
                DestinationAccountId = request.DestinationAccountId,
                Type = request.Type,
                Amount = request.Amount,
                Description = request.Description,
                Detail = request.Detail,
                CategoryId = request.CategoryId,
                Date = request.Date,
                CreatedAt = DateTime.UtcNow
            };

            _db.Transactions.Add(transaction);

            ApplyBalanceChange(account, destAccount, request.Type, request.Amount);

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            var created = await _db.Transactions
                .Include(t => t.Account)
                .Include(t => t.DestinationAccount)
                .Include(t => t.Category)
                .FirstAsync(t => t.Id == transaction.Id);

            return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, ToDto(created));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var validTypes = new[] { "Ingreso", "Egreso", "Transferencia" };
            if (!validTypes.Contains(request.Type))
                return BadRequest(new { message = "Type must be Ingreso, Egreso, or Transferencia" });

            if (request.Amount <= 0)
                return BadRequest(new { message = "Amount must be greater than zero" });

            var existing = await _db.Transactions
                .Include(t => t.Account)
                .Include(t => t.DestinationAccount)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId.Value);

            if (existing is null)
                return NotFound(new { message = "Transaction not found" });

            var newAccount = await _db.Accounts
                .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.UserId == userId.Value);
            if (newAccount is null)
                return BadRequest(new { message = "Source account not found" });

            Account? newDestAccount = null;
            if (request.Type == "Transferencia")
            {
                if (request.DestinationAccountId is null)
                    return BadRequest(new { message = "Destination account is required for transfers" });

                newDestAccount = await _db.Accounts
                    .FirstOrDefaultAsync(a => a.Id == request.DestinationAccountId && a.UserId == userId.Value);
                if (newDestAccount is null)
                    return BadRequest(new { message = "Destination account not found" });
            }

            if (request.CategoryId is not null)
            {
                var categoryExists = await _db.Categories
                    .AnyAsync(c => c.Id == request.CategoryId && c.UserId == userId.Value);
                if (!categoryExists)
                    return BadRequest(new { message = "Category not found" });
            }

            using var dbTransaction = await _db.Database.BeginTransactionAsync();

            // Reverse the old balance change
            var oldAccount = existing.Account!;
            var oldDestAccount = existing.DestinationAccount;
            ReverseBalanceChange(oldAccount, oldDestAccount, existing.Type, existing.Amount);

            // If the accounts changed, reload the new ones to get fresh balances
            if (newAccount.Id == oldAccount.Id)
                newAccount = oldAccount;

            if (newDestAccount is not null && oldDestAccount is not null && newDestAccount.Id == oldDestAccount.Id)
                newDestAccount = oldDestAccount;

            // Apply the new balance change
            ApplyBalanceChange(newAccount, newDestAccount, request.Type, request.Amount);

            existing.AccountId = request.AccountId;
            existing.DestinationAccountId = request.DestinationAccountId;
            existing.Type = request.Type;
            existing.Amount = request.Amount;
            existing.Description = request.Description;
            existing.Detail = request.Detail;
            existing.CategoryId = request.CategoryId;
            existing.Date = request.Date;

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            var updated = await _db.Transactions
                .Include(t => t.Account)
                .Include(t => t.DestinationAccount)
                .Include(t => t.Category)
                .FirstAsync(t => t.Id == id);

            return Ok(ToDto(updated));
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

            var transaction = await _db.Transactions
                .Include(t => t.Account)
                .Include(t => t.DestinationAccount)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId.Value);

            if (transaction is null)
                return NotFound(new { message = "Transaction not found" });

            using var dbTransaction = await _db.Database.BeginTransactionAsync();

            ReverseBalanceChange(transaction.Account!, transaction.DestinationAccount, transaction.Type, transaction.Amount);

            _db.Transactions.Remove(transaction);

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    private static void ApplyBalanceChange(Account source, Account? destination, string type, decimal amount)
    {
        switch (type)
        {
            case "Ingreso":
                source.Balance += amount;
                break;
            case "Egreso":
                source.Balance -= amount;
                break;
            case "Transferencia":
                source.Balance -= amount;
                if (destination is not null)
                    destination.Balance += amount;
                break;
        }
    }

    private static void ReverseBalanceChange(Account source, Account? destination, string type, decimal amount)
    {
        switch (type)
        {
            case "Ingreso":
                source.Balance -= amount;
                break;
            case "Egreso":
                source.Balance += amount;
                break;
            case "Transferencia":
                source.Balance += amount;
                if (destination is not null)
                    destination.Balance -= amount;
                break;
        }
    }

    private static TransactionDto ToDto(Transaction t) => new(
        Id: t.Id,
        AccountId: t.AccountId,
        AccountName: t.Account?.Name ?? "",
        DestinationAccountId: t.DestinationAccountId,
        DestinationAccountName: t.DestinationAccount?.Name,
        Type: t.Type,
        Amount: t.Amount,
        Description: t.Description,
        Detail: t.Detail,
        CategoryId: t.CategoryId,
        CategoryName: t.Category?.Name,
        CategoryColor: t.Category?.Color,
        Date: t.Date,
        CreatedAt: t.CreatedAt
    );

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
