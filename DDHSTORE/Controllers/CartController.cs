using DDHSTORE.Data;
using DDHSTORE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDHSTORE.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // ================= GET USER ID =================
        private int? GetUserId()
        {
            var claim = User.FindFirst("UserId")?.Value;
            return int.TryParse(claim, out int id) ? id : null;
        }

        // ================= TRANG GIỎ HÀNG =================
        public IActionResult Index()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var cart = _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToList();

            return View(cart); // ⚠️ đổi View sang List<Cart>
        }

        // ================= THÊM GIỎ =================
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Add(int id)
        {
            var userId = GetUserId();
            if (userId == null)
                return Json(new { success = false, message = "Chưa đăng nhập" });

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null)
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });

            var item = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == id);

            if (item != null)
            {
                if (item.Quantity < product.Quantity)
                    item.Quantity++;
                else
                    return Json(new { success = false, message = "Kho không đủ hoặc đã hết hàng" });
            }
            else
            {
                if (product.Quantity <= 0)
                    return Json(new { success = false, message = "Sản phẩm hiện đang hết hàng. Vui lòng liên hệ hỗ trợ." });

                _context.Carts.Add(new Cart
                {
                    UserId = userId.Value,
                    ProductId = id,
                    Quantity = 1,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã thêm vào giỏ hàng" });
        }

        // ================= ADD AJAX =================
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> AddAjax(int id)
        {
            return await Add(id); // dùng chung logic
        }

        // ================= UPDATE =================
        [HttpPost]
        public async Task<IActionResult> Update(int id, int quantity)
        {
            var userId = GetUserId();

            var item = await _context.Carts
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == id);

            if (item == null)
                return Json(new { success = false });

            if (quantity <= 0)
            {
                _context.Carts.Remove(item);
            }
            else
            {
                var maxQty = item.Product?.Quantity ?? 0;

                if (quantity > maxQty)
                    return Json(new { success = false, message = $"Kho chỉ còn {maxQty}" });

                item.Quantity = quantity;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                itemTotal = (item.Product.Price * item.Quantity).ToString("N0")
            });
        }

        // ================= XÓA 1 SP =================
        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = GetUserId();

            var item = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == id);

            if (item != null)
            {
                _context.Carts.Remove(item);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // ================= XÓA TẤT =================
        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            var userId = GetUserId();

            var items = _context.Carts.Where(c => c.UserId == userId);
            _context.Carts.RemoveRange(items);

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ================= ĐẾM =================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetCartCount()
        {
            var userId = GetUserId();

            var count = _context.Carts
                .Where(c => c.UserId == userId)
                .Sum(c => (int?)c.Quantity) ?? 0;

            return Json(new { count });
        }
    }
}