using DDHSTORE.Data;
using DDHSTORE.Helpers;
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
        private readonly IConfiguration _configuration;

        public CheckoutController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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

            // ===== XỬ LÝ VNPAY =====
            if (model.PaymentMethod == "VNPay")
            {
                var vnpay = new VnPayLibrary();
                var vnp_TmnCode = _configuration["VnPay:TmnCode"];
                var vnp_HashSecret = _configuration["VnPay:HashSecret"];
                var vnp_BaseUrl = _configuration["VnPay:BaseUrl"];
                var vnp_ReturnUrl = _configuration["VnPay:ReturnUrl"];

                // Số tiền VNPay yêu cầu nhân 100 (đơn vị VND x100)
                var vnpAmount = (long)(totalAmount * 100);

                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode ?? "");
                vnpay.AddRequestData("vnp_Amount", vnpAmount.ToString());
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_TxnRef", order.OrderId.ToString());
                vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang #{order.OrderId}");
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl ?? "");
                vnpay.AddRequestData("vnp_IpAddr", GetIpAddress());
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));

                var paymentUrl = vnpay.CreateRequestUrl(vnp_BaseUrl ?? "", vnp_HashSecret ?? "");
                return Redirect(paymentUrl);
            }

            // COD: redirect thẳng tới Success
            TempData["SuccessMessage"] = "Đơn hàng đã được đặt thành công!";
            return RedirectToAction("Success", new { id = order.OrderId });
        }

        // GET: VNPay Callback
        [HttpGet]
        public async Task<IActionResult> VnPayCallback()
        {
            var vnpay = new VnPayLibrary();
            var vnp_HashSecret = _configuration["VnPay:HashSecret"];

            // Lấy tất cả query params từ VNPay trả về
            foreach (var (key, value) in Request.Query)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            // Lấy thông tin kết quả
            var vnp_orderId = vnpay.GetResponseData("vnp_TxnRef");
            var vnp_TransactionId = vnpay.GetResponseData("vnp_TransactionNo");
            var vnp_SecureHash = Request.Query["vnp_SecureHash"].ToString();
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");

            // Validate chữ ký
            bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret ?? "");

            if (isValidSignature)
            {
                if (int.TryParse(vnp_orderId, out int orderId))
                {
                    var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
                    var order = await _context.Orders.FindAsync(orderId);

                    if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                    {
                        // Thanh toán thành công
                        if (payment != null)
                        {
                            payment.Status = "PAID";
                            payment.PaymentDate = DateTime.Now;
                        }
                        if (order != null)
                        {
                            order.Status = "PROCESSING";
                        }
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Thanh toán VNPay thành công!";
                        return RedirectToAction("Success", new { id = orderId });
                    }
                    else
                    {
                        // Thanh toán thất bại — hoàn lại tồn kho
                        if (payment != null)
                        {
                            payment.Status = "FAILED";
                        }
                        if (order != null)
                        {
                            order.Status = "CANCELLED";

                            // Hoàn lại tồn kho
                            var orderDetails = await _context.OrderDetails
                                .Where(od => od.OrderId == orderId)
                                .ToListAsync();
                            foreach (var detail in orderDetails)
                            {
                                var product = await _context.Products.FindAsync(detail.ProductId);
                                if (product != null)
                                {
                                    product.Quantity += detail.Quantity;
                                }
                            }
                        }
                        await _context.SaveChangesAsync();

                        TempData["ErrorMessage"] = "Thanh toán VNPay thất bại. Đơn hàng đã bị hủy.";
                        return RedirectToAction("PaymentFailed", new { id = orderId });
                    }
                }
            }

            TempData["ErrorMessage"] = "Chữ ký không hợp lệ. Giao dịch bị từ chối.";
            return RedirectToAction("PaymentFailed");
        }

        // GET: Payment Failed
        [HttpGet]
        public async Task<IActionResult> PaymentFailed(int? id)
        {
            ViewBag.ErrorMessage = TempData["ErrorMessage"]?.ToString() ?? "Thanh toán không thành công.";
            
            if (id.HasValue)
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);
                return View(order);
            }

            return View();
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

        /// <summary>
        /// Lấy IP Address của client
        /// </summary>
        private string GetIpAddress()
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress;
                if (ipAddress != null)
                {
                    if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        ipAddress = System.Net.Dns.GetHostEntry(ipAddress).AddressList
                            .FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    }
                    return ipAddress?.ToString() ?? "127.0.0.1";
                }
            }
            catch { }
            return "127.0.0.1";
        }
    }
}