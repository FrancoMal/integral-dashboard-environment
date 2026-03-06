namespace Api.DTOs;

public record DashboardStats(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal Balance,
    int TotalTransactions,
    AccountBalance[] AccountBalances,
    CategorySummary[] IncomeByCategory,
    CategorySummary[] ExpensesByCategory,
    MonthlyTrendItem[] MonthlyTrend
);

public record AccountBalance(
    int AccountId,
    string AccountName,
    string Type,
    decimal Balance,
    string Currency,
    string? Color,
    decimal ChangePercent1D,
    decimal ChangePercent7D,
    decimal ChangePercent1M
);

public record CategorySummary(
    int CategoryId,
    string CategoryName,
    string? Color,
    decimal Total,
    decimal Percentage
);

public record MonthlyTrendItem(
    int Month,
    int Year,
    decimal Income,
    decimal Expense,
    CategorySummary[] Categories
);

public record UserDto(
    int Id,
    string Username,
    string Email,
    string Role,
    DateTime CreatedAt,
    bool IsActive
);
