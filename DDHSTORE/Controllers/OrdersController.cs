using DDHSTORE.Data;
using DDHSTORE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDHSTORE.Controllers
{
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // ================= Create - GET =================
        // Truyền 1 productId để "Mua ngay"
        [HttpGet]
        public async Task<IActionResult> Create(int? productId)
        {
            if (productId == null) return BadRequest();

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            ViewBag.Product = product;

            // Giả sử UserId tạm thời là 1, thay bằng session nếu có login
            ViewBag.UserId = 1;

            return View();
        }

        // ================= Create - POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int userId, int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            // Tạo order mới
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = "PENDING",
                TotalAmount = product.Price * quantity,
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        ProductId = product.ProductId,
                        Quantity = quantity,
                        Price = product.Price
                    }
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Chuyển hướng sang giỏ hàng hoặc trang thanh toán
            return RedirectToAction("Index", "Cart"); // giả sử bạn có CartController
        }
    }
}