using DDHSTORE.Data;
using DDHSTORE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DDHSTORE.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ================= LIST =================
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Colors)
                .ToListAsync();

            ViewBag.TotalProducts = products.Count;
            ViewBag.ActiveProducts = products.Count(p => p.Status == 1);
            ViewBag.HiddenProducts = products.Count(p => p.Status == 0);
            ViewBag.OutOfStockProducts = products.Count(p => p.Quantity <= 0);

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdown();
            return View(new Product());
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            Product p,
            IFormFile imageFile,
            List<string> Colors = null)
        {
            await LoadDropdown();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin.";
                return View(p);
            }

            try
            {
                // Upload ảnh nếu có
                if (imageFile != null && imageFile.Length > 0)
                    p.ImageUrl = await UploadImageAsync(imageFile);

                // Trạng thái mặc định
                if (p.Status != 0 && p.Status != 1) p.Status = 1;

                _context.Products.Add(p);
                await _context.SaveChangesAsync();

                // Thêm màu sắc
                if (Colors != null)
                {
                    foreach (var c in Colors.Where(x => !string.IsNullOrWhiteSpace(x)))
                        _context.ProductColors.Add(new ProductColor
                        {
                            ProductId = p.ProductId,
                            ColorName = c
                        });
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException;
                string oracleMessage = inner?.Message ?? dbEx.Message;
                TempData["ErrorMessage"] = "Lỗi Oracle: " + oracleMessage;
                return View(p);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return View(p);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Colors)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            await LoadDropdown();
            ViewBag.Colors = product.Colors?.Select(c => c.ColorName).ToList();

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(
            Product p,
            IFormFile imageFile,
            List<string> Colors = null)
        {
            await LoadDropdown();

            if (!ModelState.IsValid) return View(p);

            try
            {
                var existing = await _context.Products
                    .Include(x => x.Colors)
                    .FirstOrDefaultAsync(x => x.ProductId == p.ProductId);

                if (existing == null) return NotFound();

                // ===== UPDATE PRODUCT =====
                existing.ProductName = p.ProductName;
                existing.Price = p.Price;
                existing.Description = p.Description;
                existing.CategoryId = p.CategoryId;
                existing.BrandId = p.BrandId;
                existing.Status = p.Status;
                existing.CPU = p.CPU;
                existing.RAM = p.RAM;
                existing.Storage = p.Storage;
                existing.Screen = p.Screen;
                existing.OS = p.OS;
                existing.Battery = p.Battery;
                existing.Quantity = p.Quantity;
                existing.LastUpdate = DateTime.Now;

                // Upload ảnh
                if (imageFile != null && imageFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var allowedMimeTypes = new[] { "image/jpeg", "image/png" };
                    var ext = Path.GetExtension(imageFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(ext) || !allowedMimeTypes.Contains(imageFile.ContentType))
                    {
                        ModelState.AddModelError("", "Chỉ chấp nhận ảnh JPG hoặc PNG!");
                        return View(p);
                    }

                    existing.ImageUrl = await UploadImageAsync(imageFile);
                }

                // ===== COLORS =====
                if (existing.Colors != null && existing.Colors.Any())
                    _context.ProductColors.RemoveRange(existing.Colors);

                if (Colors != null)
                {
                    foreach (var c in Colors.Where(x => !string.IsNullOrWhiteSpace(x)))
                        _context.ProductColors.Add(new ProductColor
                        {
                            ProductId = existing.ProductId,
                            ColorName = c
                        });
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException dbEx)
            {   
                string message = dbEx.InnerException?.Message ?? dbEx.Message;
                TempData["ErrorMessage"] = message;
                return View(p);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Colors)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product != null)
                {
                    if (product.Colors != null && product.Colors.Any())
                        _context.ProductColors.RemoveRange(product.Colors);

                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ================= TOGGLE STATUS =================
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product != null)
            {
                product.Status = product.Status == 1 ? 0 : 1;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // ================= DROPDOWN =================
        private async Task LoadDropdown()
        {
            var cates = await _context.Categories
                                      .Where(c => c.Status == 1)
                                      .ToListAsync();
            var brands = await _context.Brands
                                       .Where(b => b.Status == 1)
                                       .ToListAsync();
            var colors = await _context.ProductColors
                               .Select(c => c.ColorName)
                               .Distinct()
                               .ToListAsync();

            ViewBag.Categories = new SelectList(cates, "CategoryId", "CategoryName");
            ViewBag.Brands = new SelectList(brands, "BrandId", "BrandName");
            ViewBag.Colors = new MultiSelectList(colors);
        }

        // ================= UPLOAD IMAGE =================
        private async Task<string> UploadImageAsync(IFormFile file)
        {
            var folder = Path.Combine(_env.WebRootPath, "images", "products");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/images/products/" + fileName;
        }
    }
}