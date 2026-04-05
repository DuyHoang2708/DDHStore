using DDHSTORE.Data;
using DDHSTORE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var products = _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.Status == 1)
            .OrderByDescending(p => p.ProductId)
            .Take(8)
            .ToList();

        // 🔥 THÊM ĐOẠN NÀY
        var userIdStr = User.FindFirst("UserId")?.Value;

        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
        {

            ViewBag.UserFavorites = _context.Favorites
                .Where(f => f.UserId == userId)
                .Select(f => f.ProductId)
                .ToList();
        }
        else
        {
            ViewBag.UserFavorites = new List<int>();
        }

        return View(products);
    }
    [HttpGet]
    public IActionResult Products(string? q)
    {
        var query = _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.Status == 1);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(p =>
                p.ProductName.Contains(q) ||
                (p.Brand != null && p.Brand.BrandName.Contains(q)) ||
                (p.Category != null && p.Category.CategoryName.Contains(q)));
        }

        var products = query
            .OrderByDescending(p => p.ProductId)
            .ToList();

        // 🔥 THÊM ĐOẠN NÀY
        var userIdStr = User.FindFirst("UserId")?.Value;

        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
        {

            ViewBag.UserFavorites = _context.Favorites
                .Where(f => f.UserId == userId)
                .Select(f => f.ProductId)
                .ToList();
        }
        else
        {
            ViewBag.UserFavorites = new List<int>();
        }

        ViewBag.Query = q;

        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> ProductDetail(int id)
    {
        var product = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Colors)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
        {
            return NotFound();
        }

        // Check if favorited
        var userIdStr = User.FindFirst("UserId")?.Value;
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
        {
            ViewBag.IsFavorite = await _context.Favorites
                .CountAsync(f => f.UserId == userId && f.ProductId == id) > 0;
        }
        else
        {
            ViewBag.IsFavorite = false;
        }

        // Get related products (same category, excluding current)
        var relatedProducts = await _context.Products
            .Where(p => p.CategoryId == product.CategoryId && p.ProductId != id && p.Status == 1)
            .Take(4)
            .ToListAsync();
        ViewBag.RelatedProducts = relatedProducts;

        return View(product);
    }
}