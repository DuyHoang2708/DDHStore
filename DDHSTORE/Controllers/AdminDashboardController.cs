using DDHSTORE.Data;
using DDHSTORE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.IO;

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

        [HttpGet]
        public async Task<IActionResult> ExportRevenue()
        {
            var completedOrders = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.Status == "COMPLETED")
                .OrderBy(o => o.OrderDate)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("DoanhThu");
                var currentRow = 1;

                // Header
                worksheet.Cell(currentRow, 1).Value = "Mã Đơn Hàng";
                worksheet.Cell(currentRow, 2).Value = "Khách Hàng";
                worksheet.Cell(currentRow, 3).Value = "Ngày Đặt";
                worksheet.Cell(currentRow, 4).Value = "Tổng Tiền";
                worksheet.Cell(currentRow, 5).Value = "Trạng Thái";

                worksheet.Row(currentRow).Style.Font.Bold = true;

                // Data
                foreach (var order in completedOrders)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = $"#ORD-{order.OrderId}";
                    worksheet.Cell(currentRow, 2).Value = order.User?.Username ?? "Khách lẻ";
                    worksheet.Cell(currentRow, 3).Value = order.OrderDate.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(currentRow, 4).Value = order.TotalAmount;
                    worksheet.Cell(currentRow, 5).Value = order.Status;
                }

                // Format amount column
                worksheet.Column(4).Style.NumberFormat.Format = "#,##0 ₫";
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
            }
        }
    }
}
