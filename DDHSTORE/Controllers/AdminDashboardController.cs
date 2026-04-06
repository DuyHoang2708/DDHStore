using DDHSTORE.Data;
using DDHSTORE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDHSTORE.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _context;

        public AdminDashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Thống kê tổng hợp
            ViewBag.TotalRevenue = await _context.Orders
                .Where(o => o.Status == "COMPLETED")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.LowStockCount = await _context.Products.CountAsync(p => p.Quantity < 10);

            // 5 đơn hàng gần nhất
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // 5 sản phẩm mới nhất
            ViewBag.RecentProducts = await _context.Products
                .OrderByDescending(p => p.ProductId)
                .Take(5)
                .ToListAsync();

            // 5 người dùng mới nhất
            ViewBag.RecentUsers = await _context.Users
                .Include(u => u.Role)
                .OrderByDescending(u => u.UserId)
                .Take(5)
                .ToListAsync();

            return View(recentOrders);
        }
    }
}
