using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public record TransactionDto(
    int Id,
    int AccountId,
    string AccountName,
    int? DestinationAccountId,
    string? DestinationAccountName,
    string Type,
    decimal Amount,
    string? Description,
    string? Detail,
    int? CategoryId,
    string? CategoryName,
    string? CategoryColor,
    DateTime Date,
    DateTime CreatedAt
);

public record CreateTransactionRequest(
    [Required] int AccountId,
    int? DestinationAccountId,
    [Required] string Type,
    [Required] decimal Amount,
    string? Description,
    string? Detail,
    int? CategoryId,
    [Required] DateTime Date
);

public record UpdateTransactionRequest(
    [Required] int AccountId,
    int? DestinationAccountId,
    [Required] string Type,
    [Required] decimal Amount,
    string? Description,
    string? Detail,
    int? CategoryId,
    [Required] DateTime Date
);

public record PaginatedResponse<T>(
    T[] Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
