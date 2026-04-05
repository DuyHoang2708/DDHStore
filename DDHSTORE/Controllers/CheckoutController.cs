using DDHSTORE.Data;
using DDHSTORE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDHSTORE.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;

        public CheckoutController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetUserId()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdStr, out int id) ? id : null;
        }

        // GET: Checkout
        [HttpGet]
        public async Task<IActionResult> Index(int? productId, int quantity = 1)
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId);
            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId && a.Status == 1)
                .OrderByDescending(a => a.AddressId)
                .ToListAsync();

            var model = new CheckoutViewModel
            {
                UserId = userId.Value,
                Username = user?.Username ?? "Khách hàng",
                UserAddresses = addresses,
                RecipientName = user?.Username ?? "",
                Phone = user?.Phone ?? ""
            };

            if (productId == null || productId == 0)
            {
                // Checkout từ Giỏ hàng
                var cartItems = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Include(c => c.Product)
                    .Select(c => new CheckoutItemViewModel
                    {
                        ProductId = c.ProductId,
                        ProductName = c.Product.ProductName,
                        ImageUrl = c.Product.ImageUrl,
                        Price = c.Product.Price,
                        Quantity = c.Quantity,
                        StockQuantity = c.Product.Quantity
                    })
                    .ToListAsync();

                if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

                model.Items = cartItems;
                model.IsCartCheckout = true;
                
                // Gán giá trị sp đầu tiên để demo UI cũ nếu cần, hoặc cứ để trống
                var first = cartItems.First();
                model.ProductId = first.ProductId;
                model.ProductName = first.ProductName;
                model.ImageUrl = first.ImageUrl;
                model.Price = first.Price;
                model.Quantity = first.Quantity;
                model.StockQuantity = first.StockQuantity;
            }
            else
            {
                // Checkout trực tiếp (Buy Now)
                var product = await _context.Products.FindAsync(productId.Value);
                if (product == null) return NotFound();

                model.IsCartCheckout = false;
                model.ProductId = product.ProductId;
                model.ProductName = product.ProductName;
                model.ImageUrl = product.ImageUrl;
                model.Price = product.Price;
                model.Quantity = quantity > product.Quantity ? product.Quantity : (quantity < 1 ? 1 : quantity);
                model.StockQuantity = product.Quantity;
                
                model.Items.Add(new CheckoutItemViewModel
                {
                    ProductId = model.ProductId,
                    ProductName = model.ProductName,
                    ImageUrl = model.ImageUrl,
                    Price = model.Price,
                    Quantity = model.Quantity,
                    StockQuantity = model.StockQuantity
                });
            }

            return View(model);
        }

        // POST: Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            List<OrderDetail> orderDetails = new List<OrderDetail>();
            decimal totalAmount = 0;

            if (model.IsCartCheckout)
            {
                // Lấy từ giỏ hàng để đảm bảo chính xác
                var cartItems = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Include(c => c.Product)
                    .ToListAsync();

                if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

                foreach (var item in cartItems)
                {
                    if (item.Quantity > item.Product.Quantity)
                    {
                        ModelState.AddModelError("", $"Sản phẩm {item.Product.ProductName} không đủ hàng ({item.Product.Quantity} còn lại).");
                        // Tái nạp dữ liệu cho view
                        return await ReloadCheckoutView(model, userId.Value);
                    }

                    orderDetails.Add(new OrderDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Product.Price
                    });
                    totalAmount += item.Product.Price * item.Quantity;
                    
                    // Cập nhật tồn kho
                    item.Product.Quantity -= item.Quantity;
                }
            }
            else
            {
                // Checkout đơn lẻ
                var product = await _context.Products.FindAsync(model.ProductId);
                if (product == null) return NotFound();

                if (model.Quantity > product.Quantity)
                {
                    ModelState.AddModelError("Quantity", $"Số lượng vượt quá tồn kho ({product.Quantity})!");
                    return await ReloadCheckoutView(model, userId.Value);
                }

                orderDetails.Add(new OrderDetail
                {
                    ProductId = model.ProductId,
                    Quantity = model.Quantity,
                    Price = model.Price
                });
                totalAmount = model.Price * model.Quantity;
                
                // Cập nhật tồn kho
                product.Quantity -= model.Quantity;
            }

            int addressId;
            // Xử lý địa chỉ
            if (model.AddressId.HasValue && model.AddressId.Value > 0)
            {
                addressId = model.AddressId.Value;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.Province) || string.IsNullOrWhiteSpace(model.District) || string.IsNullOrWhiteSpace(model.Ward))
                {
                    ModelState.AddModelError("", "Vui lòng chọn đầy đủ thông tin địa chỉ.");
                    return await ReloadCheckoutView(model, userId.Value);
                }

                var newAddress = new Address
                {
                    UserId = userId.Value,
                    RecipientName = model.RecipientName ?? "Người nhận",
                    Phone = model.Phone ?? "",
                    Province = model.Province,
                    District = model.District,
                    Ward = model.Ward,
                    Detail = model.Detail,
                    Status = model.SaveAddress ? 1 : 0
                };
                _context.Addresses.Add(newAddress);
                await _context.SaveChangesAsync();
                addressId = newAddress.AddressId;
            }

            // Tạo Order
            var order = new Order
            {
                UserId = userId.Value,
                AddressId = addressId,
                OrderDate = DateTime.Now,
                Status = "PENDING",
                TotalAmount = totalAmount,
                OrderDetails = orderDetails
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Tạo Payment
            var payment = new Payment
            {
                OrderId = order.OrderId,
                Method = model.PaymentMethod ?? "Cash",
                PaymentDate = DateTime.Now,
                Status = "PENDING"
            };
            _context.Payments.Add(payment);
            
            // Xóa giỏ hàng nếu checkout từ cart
            if (model.IsCartCheckout)
            {
                var cartItems = _context.Carts.Where(c => c.UserId == userId);
                _context.Carts.RemoveRange(cartItems);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đơn hàng đã được đặt thành công!";
            return RedirectToAction("Success", new { id = order.OrderId });
        }

        private async Task<IActionResult> ReloadCheckoutView(CheckoutViewModel model, int userId)
        {
            model.UserAddresses = await _context.Addresses
                .Where(a => a.UserId == userId && a.Status == 1).ToListAsync();
            
            if (model.IsCartCheckout)
            {
                model.Items = await _context.Carts.Where(c => c.UserId == userId).Include(c => c.Product)
                    .Select(c => new CheckoutItemViewModel {
                        ProductId = c.ProductId, ProductName = c.Product.ProductName, 
                        ImageUrl = c.Product.ImageUrl, Price = c.Product.Price, 
                        Quantity = c.Quantity, StockQuantity = c.Product.Quantity
                    }).ToListAsync();
            }
            return View(model);
        }

        // GET: Checkout/Success
        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Address)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}