using System.Security.Claims;
using Api.Data;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/exchange-rates")]
[Authorize]
public class ExchangeRatesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExchangeRatesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var rates = await _db.ExchangeRates
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            var grouped = rates
                .GroupBy(r => new { r.FromCurrency, r.ToCurrency })
                .Select(g => g.First())
                .Select(r => new
                {
                    r.Id,
                    r.FromCurrency,
                    r.ToCurrency,
                    r.Rate,
                    r.Date
                })
                .ToList();

            return Ok(grouped);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateExchangeRateRequest request)
    {
        try
        {
            var existing = await _db.ExchangeRates
                .Where(r => r.FromCurrency == request.FromCurrency && r.ToCurrency == request.ToCurrency)
                .OrderByDescending(r => r.Date)
                .FirstOrDefaultAsync();

            if (existing is not null)
            {
                existing.Rate = request.Rate;
                existing.Date = DateTime.UtcNow;
            }
            else
            {
                var newRate = new ExchangeRate
                {
                    FromCurrency = request.FromCurrency,
                    ToCurrency = request.ToCurrency,
                    Rate = request.Rate,
                    Date = DateTime.UtcNow
                };
                _db.ExchangeRates.Add(newRate);
            }

            // Also update the inverse rate
            var inverse = await _db.ExchangeRates
                .Where(r => r.FromCurrency == request.ToCurrency && r.ToCurrency == request.FromCurrency)
                .OrderByDescending(r => r.Date)
                .FirstOrDefaultAsync();

            if (inverse is not null && request.Rate > 0)
            {
                inverse.Rate = Math.Round(1m / request.Rate, 4);
                inverse.Date = DateTime.UtcNow;
            }
            else if (request.Rate > 0)
            {
                var newInverse = new ExchangeRate
                {
                    FromCurrency = request.ToCurrency,
                    ToCurrency = request.FromCurrency,
                    Rate = Math.Round(1m / request.Rate, 4),
                    Date = DateTime.UtcNow
                };
                _db.ExchangeRates.Add(newInverse);
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                FromCurrency = request.FromCurrency,
                ToCurrency = request.ToCurrency,
                Rate = request.Rate,
                InverseRate = request.Rate > 0 ? Math.Round(1m / request.Rate, 4) : 0m,
                Date = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}

public record UpdateExchangeRateRequest(
    string FromCurrency,
    string ToCurrency,
    decimal Rate
);
