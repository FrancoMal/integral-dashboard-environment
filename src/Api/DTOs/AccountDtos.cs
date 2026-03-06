using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public record AccountDto(
    int Id,
    string Name,
    string Type,
    string Currency,
    decimal Balance,
    decimal? BalanceUsd,
    string? Color,
    string? Icon,
    bool IsActive
);

public record CreateAccountRequest(
    [Required] string Name,
    [Required] string Type,
    string Currency = "ARS",
    string? Color = null,
    string? Icon = null
);

public record UpdateAccountRequest(
    string? Name = null,
    string? Type = null,
    string? Currency = null,
    string? Color = null,
    string? Icon = null,
    bool? IsActive = null
);
