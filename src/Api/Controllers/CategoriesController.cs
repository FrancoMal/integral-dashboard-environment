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
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
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

            var categories = await _db.Categories
                .Where(c => c.UserId == userId.Value && c.IsActive && c.ParentId == null)
                .Include(c => c.SubCategories.Where(sc => sc.IsActive))
                .OrderBy(c => c.Name)
                .ToListAsync();

            var dtos = categories.Select(c => ToDto(c)).ToArray();

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

            var category = await _db.Categories
                .Include(c => c.SubCategories.Where(sc => sc.IsActive))
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId.Value);

            if (category is null)
                return NotFound(new { message = "Category not found" });

            return Ok(ToDto(category));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            if (request.ParentId is not null)
            {
                var parent = await _db.Categories
                    .FirstOrDefaultAsync(c => c.Id == request.ParentId && c.UserId == userId.Value);
                if (parent is null)
                    return BadRequest(new { message = "Parent category not found" });
            }

            var category = new Category
            {
                UserId = userId.Value,
                Name = request.Name,
                Color = request.Color,
                Icon = request.Icon,
                ParentId = request.ParentId,
                IsActive = true
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            var created = await _db.Categories
                .Include(c => c.Parent)
                .Include(c => c.SubCategories)
                .FirstAsync(c => c.Id == category.Id);

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, ToDto(created));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var category = await _db.Categories
                .Include(c => c.SubCategories.Where(sc => sc.IsActive))
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId.Value);

            if (category is null)
                return NotFound(new { message = "Category not found" });

            if (request.Name is not null) category.Name = request.Name;
            if (request.Color is not null) category.Color = request.Color;
            if (request.Icon is not null) category.Icon = request.Icon;
            if (request.IsActive is not null) category.IsActive = request.IsActive.Value;

            if (request.ParentId is not null)
            {
                if (request.ParentId == id)
                    return BadRequest(new { message = "A category cannot be its own parent" });

                var parent = await _db.Categories
                    .FirstOrDefaultAsync(c => c.Id == request.ParentId && c.UserId == userId.Value);
                if (parent is null)
                    return BadRequest(new { message = "Parent category not found" });

                category.ParentId = request.ParentId;
            }

            await _db.SaveChangesAsync();

            return Ok(ToDto(category));
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

            var category = await _db.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId.Value);

            if (category is null)
                return NotFound(new { message = "Category not found" });

            var hasTransactions = await _db.Transactions.AnyAsync(t => t.CategoryId == id);
            var hasSubCategories = await _db.Categories.AnyAsync(c => c.ParentId == id && c.IsActive);

            if (hasTransactions || hasSubCategories)
            {
                category.IsActive = false;
                await _db.SaveChangesAsync();
                return Ok(new { message = "Category deactivated (has existing transactions or subcategories)" });
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    private static CategoryDto ToDto(Category c) => new(
        Id: c.Id,
        Name: c.Name,
        Color: c.Color,
        Icon: c.Icon,
        ParentId: c.ParentId,
        ParentName: c.Parent?.Name,
        SubCategories: c.SubCategories
            .Where(sc => sc.IsActive)
            .OrderBy(sc => sc.Name)
            .Select(sc => new CategoryDto(
                Id: sc.Id,
                Name: sc.Name,
                Color: sc.Color,
                Icon: sc.Icon,
                ParentId: sc.ParentId,
                ParentName: c.Name,
                SubCategories: Array.Empty<CategoryDto>()
            ))
            .ToArray()
    );

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}
