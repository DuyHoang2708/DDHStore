using DDHSTORE.Data;
using DDHSTORE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDHSTORE.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : Controller
    {
        private readonly AppDbContext _context;

        public AdminOrdersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminOrders
        public async Task<IActionResult> Index(int? userId, string status, string search)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Payment)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(o => o.UserId == userId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);

            if (!string.IsNullOrEmpty(search))
            {
                if (int.TryParse(search, out int orderId))
                {
                    query = query.Where(o => o.OrderId == orderId);
                }
                else
                {
                    query = query.Where(o => o.User.Username.Contains(search) || o.User.Email.Contains(search));
                }
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentUserId = userId;
            ViewBag.Search = search;

            return View(orders);
        }

        // GET: AdminOrders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Address)
                .Include(o => o.Payment)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: AdminOrders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            // validate status
            if (!Enum.TryParse(typeof(OrderStatus), status, out var parsedStatus))
            {
                TempData["Error"] = "Trạng thái không hợp lệ!";
                return RedirectToAction(nameof(Index));
            }

            order.Status = parsedStatus.ToString();
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đơn #{orderId} → {status}";
            return RedirectToAction(nameof(Index));
        }
    }
}
