using DDHSTORE.Data;
using DDHSTORE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DDHSTORE.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetUserId()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdStr, out int id) ? id : (int?)null;
        }

        // ================= GET: Orders =================
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Payment)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ================= GET: Orders/Details/5 =================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Address)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null) return NotFound();

            return View(order);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            // S? d?ng th?ng nh?t hàm GetUserId
            var userId = GetUserId();

            // N?u không tìm th?y ID ng??i dùng trong Identity
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Tìm ??n hàng thu?c v? user ?ó
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == "PENDING")
            {
                order.Status = "CANCELLED";
                _context.Update(order);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"??n hàng #{id} ?ã ???c h?y thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không th? h?y ??n hàng này.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}