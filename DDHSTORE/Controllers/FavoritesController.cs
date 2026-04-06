using DDHSTORE.Data;
using DDHSTORE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDHSTORE.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly AppDbContext _context;

        public FavoritesController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy UserId từ claim
        private int? GetUserId()
        {
            var claim = User.FindFirst("UserId")?.Value;
            return string.IsNullOrEmpty(claim) ? null : int.TryParse(claim, out int id) ? id : null;
        }

        // Thêm/Xóa yêu thích (AJAX)
        [HttpPost]
        [AllowAnonymous]
        public IActionResult Toggle(int productId)
        {
            var userId = GetUserId();
            if (userId == null)
                return Json(new { status = "unauthorized" });

            var favorite = _context.Favorites
                .FirstOrDefault(f => f.UserId == userId && f.ProductId == productId);

            if (favorite == null)
            {
                _context.Favorites.Add(new Favorite
                {
                    UserId = userId.Value,
                    ProductId = productId,
                    CreatedAt = DateTime.Now
                });
                _context.SaveChanges();
                return Json(new { status = "added" });
            }
            else
            {
                _context.Favorites.Remove(favorite);
                _context.SaveChanges();
                return Json(new { status = "removed" });
            }
        }

        // Xóa yêu thích (form submit)
        [HttpPost]
        public IActionResult Remove(int productId)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var favorite = _context.Favorites
                .FirstOrDefault(f => f.UserId == userId && f.ProductId == productId);

            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                _context.SaveChanges();
            }

            return RedirectToAction("MyFavorites");
        }

        // Danh sách yêu thích
        public IActionResult MyFavorites()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var favorites = _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Brand)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Category)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            return View(favorites);
        }
        // Xóa tất cả yêu thích
        [HttpPost]
        public IActionResult RemoveAll()
        {
            var userId = GetUserId();
            if (userId == null)
                return Json(new { success = false, redirect = true });

            var favorites = _context.Favorites.Where(f => f.UserId == userId);
            _context.Favorites.RemoveRange(favorites);
            _context.SaveChanges();

            return Json(new { success = true });
        }
    }
}