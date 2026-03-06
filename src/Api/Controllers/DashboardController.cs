using System.Security.Claims;
using Api.Data;
using Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int? month, [FromQuery] int? year)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var now = DateTime.UtcNow;
            var targetMonth = month ?? now.Month;
            var targetYear = year ?? now.Year;

            var transactions = await _db.Transactions
                .Where(t => t.UserId == userId.Value
                    && t.Date.Month == targetMonth
                    && t.Date.Year == targetYear)
                .Include(t => t.Category)
                .ToListAsync();

            var totalIncome = transactions
                .Where(t => t.Type == "Ingreso")
                .Sum(t => t.Amount);

            var totalExpenses = transactions
                .Where(t => t.Type == "Egreso")
                .Sum(t => t.Amount);

            var balance = totalIncome - totalExpenses;
            var totalTransactions = transactions.Count;

            var incomeByCategory = transactions
                .Where(t => t.Type == "Ingreso" && t.Category is not null)
                .GroupBy(t => new { t.CategoryId, t.Category!.Name, t.Category.Color })
                .Select(g => new CategorySummary(
                    CategoryId: g.Key.CategoryId!.Value,
                    CategoryName: g.Key.Name,
                    Color: g.Key.Color,
                    Total: g.Sum(t => t.Amount),
                    Percentage: totalIncome > 0 ? Math.Round(g.Sum(t => t.Amount) / totalIncome * 100, 2) : 0
                ))
                .OrderByDescending(c => c.Total)
                .ToArray();

            var expensesByCategory = transactions
                .Where(t => t.Type == "Egreso" && t.Category is not null)
                .GroupBy(t => new { t.CategoryId, t.Category!.Name, t.Category.Color })
                .Select(g => new CategorySummary(
                    CategoryId: g.Key.CategoryId!.Value,
                    CategoryName: g.Key.Name,
                    Color: g.Key.Color,
                    Total: g.Sum(t => t.Amount),
                    Percentage: totalExpenses > 0 ? Math.Round(g.Sum(t => t.Amount) / totalExpenses * 100, 2) : 0
                ))
                .OrderByDescending(c => c.Total)
                .ToArray();

            var usdToArs = await GetExchangeRate("USD", "ARS");

            var accounts = await _db.Accounts
                .Where(a => a.UserId == userId.Value && a.IsActive)
                .ToListAsync();

            var accountBalances = new List<AccountBalance>();
            foreach (var account in accounts)
            {
                var changes = await CalculateAccountChanges(account.Id, userId.Value);
                decimal? balanceUsd = null;
                if (account.Currency == "ARS" && usdToArs > 0)
                    balanceUsd = Math.Round(account.Balance / usdToArs, 2);
                else if (account.Currency == "USD")
                    balanceUsd = account.Balance;

                accountBalances.Add(new AccountBalance(
                    AccountId: account.Id,
                    AccountName: account.Name,
                    Type: account.Type,
                    Balance: account.Balance,
                    Currency: account.Currency,
                    Color: account.Color,
                    ChangePercent1D: changes.day,
                    ChangePercent7D: changes.week,
                    ChangePercent1M: changes.month
                ));
            }

            var monthlyTrend = await GetMonthlyTrend(userId.Value, 12);

            return Ok(new DashboardStats(
                TotalIncome: totalIncome,
                TotalExpenses: totalExpenses,
                Balance: balance,
                TotalTransactions: totalTransactions,
                AccountBalances: accountBalances.ToArray(),
                IncomeByCategory: incomeByCategory,
                ExpensesByCategory: expensesByCategory,
                MonthlyTrend: monthlyTrend
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccountBalances()
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var usdToArs = await GetExchangeRate("USD", "ARS");

            var accounts = await _db.Accounts
                .Where(a => a.UserId == userId.Value && a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();

            var result = new List<AccountBalance>();
            foreach (var account in accounts)
            {
                var changes = await CalculateAccountChanges(account.Id, userId.Value);
                result.Add(new AccountBalance(
                    AccountId: account.Id,
                    AccountName: account.Name,
                    Type: account.Type,
                    Balance: account.Balance,
                    Currency: account.Currency,
                    Color: account.Color,
                    ChangePercent1D: changes.day,
                    ChangePercent7D: changes.week,
                    ChangePercent1M: changes.month
                ));
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    [HttpGet("expenses-by-category")]
    public async Task<IActionResult> GetExpensesByCategory([FromQuery] int? month, [FromQuery] int? year)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var now = DateTime.UtcNow;
            var targetMonth = month ?? now.Month;
            var targetYear = year ?? now.Year;

            var expenses = await _db.Transactions
                .Where(t => t.UserId == userId.Value
                    && t.Type == "Egreso"
                    && t.Date.Month == targetMonth
                    && t.Date.Year == targetYear
                    && t.CategoryId != null)
                .Include(t => t.Category)
                .ToListAsync();

            var totalExpenses = expenses.Sum(t => t.Amount);

            var result = expenses
                .GroupBy(t => new { t.CategoryId, t.Category!.Name, t.Category.Color })
                .Select(g => new CategorySummary(
                    CategoryId: g.Key.CategoryId!.Value,
                    CategoryName: g.Key.Name,
                    Color: g.Key.Color,
                    Total: g.Sum(t => t.Amount),
                    Percentage: totalExpenses > 0 ? Math.Round(g.Sum(t => t.Amount) / totalExpenses * 100, 2) : 0
                ))
                .OrderByDescending(c => c.Total)
                .ToArray();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    [HttpGet("monthly-trend")]
    public async Task<IActionResult> GetMonthlyTrend([FromQuery] int months = 12)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            if (months < 1) months = 1;
            if (months > 36) months = 36;

            var result = await GetMonthlyTrend(userId.Value, months);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    private async Task<MonthlyTrendItem[]> GetMonthlyTrend(int userId, int months)
    {
        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-(months - 1));

        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId && t.Date >= startDate)
            .Include(t => t.Category)
            .ToListAsync();

        var result = new List<MonthlyTrendItem>();

        for (int i = 0; i < months; i++)
        {
            var targetDate = startDate.AddMonths(i);
            var monthTransactions = transactions
                .Where(t => t.Date.Month == targetDate.Month && t.Date.Year == targetDate.Year)
                .ToList();

            var income = monthTransactions.Where(t => t.Type == "Ingreso").Sum(t => t.Amount);
            var expense = monthTransactions.Where(t => t.Type == "Egreso").Sum(t => t.Amount);

            var categories = monthTransactions
                .Where(t => t.Type == "Egreso" && t.Category is not null)
                .GroupBy(t => new { t.CategoryId, t.Category!.Name, t.Category.Color })
                .Select(g => new CategorySummary(
                    CategoryId: g.Key.CategoryId!.Value,
                    CategoryName: g.Key.Name,
                    Color: g.Key.Color,
                    Total: g.Sum(t => t.Amount),
                    Percentage: expense > 0 ? Math.Round(g.Sum(t => t.Amount) / expense * 100, 2) : 0
                ))
                .OrderByDescending(c => c.Total)
                .ToArray();

            result.Add(new MonthlyTrendItem(
                Month: targetDate.Month,
                Year: targetDate.Year,
                Income: income,
                Expense: expense,
                Categories: categories
            ));
        }

        return result.ToArray();
    }

    private async Task<(decimal day, decimal week, decimal month)> CalculateAccountChanges(int accountId, int userId)
    {
        var now = DateTime.UtcNow;
        var dayAgo = now.AddDays(-1);
        var weekAgo = now.AddDays(-7);
        var monthAgo = now.AddMonths(-1);

        var recentTransactions = await _db.Transactions
            .Where(t => t.UserId == userId
                && (t.AccountId == accountId || t.DestinationAccountId == accountId)
                && t.Date >= monthAgo)
            .ToListAsync();

        decimal CalculateNetChange(DateTime since)
        {
            decimal net = 0;
            foreach (var t in recentTransactions.Where(t => t.Date >= since))
            {
                if (t.AccountId == accountId)
                {
                    switch (t.Type)
                    {
                        case "Ingreso": net += t.Amount; break;
                        case "Egreso": net -= t.Amount; break;
                        case "Transferencia": net -= t.Amount; break;
                    }
                }
                if (t.DestinationAccountId == accountId && t.Type == "Transferencia")
                {
                    net += t.Amount;
                }
            }
            return net;
        }

        var dayChange = CalculateNetChange(dayAgo);
        var weekChange = CalculateNetChange(weekAgo);
        var monthChange = CalculateNetChange(monthAgo);

        return (dayChange, weekChange, monthChange);
    }

    private async Task<decimal> GetExchangeRate(string from, string to)
    {
        var rate = await _db.ExchangeRates
            .Where(r => r.FromCurrency == from && r.ToCurrency == to)
            .OrderByDescending(r => r.Date)
            .FirstOrDefaultAsync();

        return rate?.Rate ?? 0;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
