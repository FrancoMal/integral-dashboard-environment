using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public record CategoryDto(
    int Id,
    string Name,
    string? Color,
    string? Icon,
    int? ParentId,
    string? ParentName,
    CategoryDto[] SubCategories
);

public record CreateCategoryRequest(
    [Required] string Name,
    string? Color = null,
    string? Icon = null,
    int? ParentId = null
);

public record UpdateCategoryRequest(
    string? Name = null,
    string? Color = null,
    string? Icon = null,
    int? ParentId = null,
    bool? IsActive = null
);
